import Foundation
import Network

/// Latency tests. "Via proxy" uses libXray `pingBatch` (a real request
/// through a temporary Xray instance, max 5 configs per call); "TCP" measures
/// the TCP handshake to the server.
enum PingService {
    static let timeoutSeconds = 5
    private static let queue = DispatchQueue(label: "tech.rockblack.ping", qos: .userInitiated)
    private static let tcpQueue = DispatchQueue(label: "tech.rockblack.ping.tcp", qos: .userInitiated, attributes: .concurrent)

    static func run(_ servers: [Server], method: PingMethod, url: String,
                    onResult: @escaping @MainActor (UUID, PingResult) -> Void) async {
        switch method {
        case .proxy:
            await proxyPing(servers, url: url, onResult: onResult)
        case .tcp:
            await withTaskGroup(of: Void.self) { group in
                for server in servers {
                    group.addTask {
                        let result = await tcpPing(server)
                        await onResult(server.id, result)
                    }
                }
            }
        }
    }

    private static func proxyPing(_ servers: [Server], url: String,
                                  onResult: @escaping @MainActor (UUID, PingResult) -> Void) async {
        var chunks: [[Server]] = []
        var index = 0
        while index < servers.count {
            chunks.append(Array(servers[index..<min(index + 5, servers.count)]))
            index += 5
        }
        for chunk in chunks {
            let results: [PingResult] = await withCheckedContinuation { continuation in
                queue.async { continuation.resume(returning: pingBatch(chunk, url: url)) }
            }
            for (server, result) in zip(chunk, results) {
                await onResult(server.id, result)
            }
        }
    }

    static func pingBatch(_ servers: [Server], url: String) -> [PingResult] {
        let configs: [[String: Any]] = servers.map { server in
            var outbound = server.outboundObject ?? [:]
            outbound["tag"] = XrayConfig.proxyTag
            return ["xrayJson": JSONText.encode(["outbounds": [outbound]]) ?? "{}", "outboundTag": XrayConfig.proxyTag]
        }
        let response = XrayCore.invoke("pingBatch", payload: ["configs": configs, "timeout": timeoutSeconds, "url": url])
        guard response.success, let data = response.data as? [String: Any],
              let items = data["results"] as? [[String: Any]] else {
            return servers.map { _ in .failed }
        }
        return servers.indices.map { index in
            guard index < items.count else { return .failed }
            let item = items[index]
            let delay = XrayConfig.intValue(item["delay"]) ?? 10_000
            if item["success"] as? Bool == true, delay < 10_000 { return .delay(max(delay, 1)) }
            return delay == 11_000 ? .timeout : .failed
        }
    }

    static func tcpPing(_ server: Server) async -> PingResult {
        guard let endpoint = server.endpoint, let port = NWEndpoint.Port(rawValue: UInt16(clamping: endpoint.port)) else {
            return .failed
        }
        let connection = NWConnection(host: NWEndpoint.Host(endpoint.host), port: port, using: .tcp)
        let start = DispatchTime.now()
        return await withCheckedContinuation { continuation in
            let once = Once(connection: connection, continuation: continuation)
            let finish: @Sendable (PingResult) -> Void = { once.finish($0) }
            connection.stateUpdateHandler = { state in
                switch state {
                case .ready:
                    let ms = Int((DispatchTime.now().uptimeNanoseconds - start.uptimeNanoseconds) / 1_000_000)
                    finish(.delay(max(ms, 1)))
                case .failed, .cancelled:
                    finish(.failed)
                case .waiting:
                    finish(.failed)
                default:
                    break
                }
            }
            connection.start(queue: tcpQueue)
            tcpQueue.asyncAfter(deadline: .now() + .seconds(timeoutSeconds)) { finish(.timeout) }
        }
    }
}

/// Resumes a ping continuation exactly once.
private final class Once: @unchecked Sendable {
    private let lock = NSLock()
    private var done = false
    private let connection: NWConnection
    private let continuation: CheckedContinuation<PingResult, Never>

    init(connection: NWConnection, continuation: CheckedContinuation<PingResult, Never>) {
        self.connection = connection
        self.continuation = continuation
    }

    func finish(_ result: PingResult) {
        lock.lock()
        defer { lock.unlock() }
        guard !done else { return }
        done = true
        connection.cancel()
        continuation.resume(returning: result)
    }
}
