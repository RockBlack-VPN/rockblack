import Foundation
import NetworkExtension

enum ConnectionState: Equatable {
    case disconnected, connecting, connected, disconnecting
}

/// Owns the NETunnelProviderManager for the RockBlack packet tunnel.
@MainActor
final class VPNController: ObservableObject {
    @Published private(set) var state: ConnectionState = .disconnected
    @Published private(set) var connectedDate: Date?
    @Published private(set) var downloadRate: Double = 0
    @Published private(set) var uploadRate: Double = 0
    @Published var lastError: String?

    private var manager: NETunnelProviderManager?
    private var statusObserver: NSObjectProtocol?
    private var statsTimer: Timer?
    private var lastTotals: (up: Int64, down: Int64, time: Date)?
    private var demo = false

    func load() async {
        guard !demo else { return }
        let managers = (try? await NETunnelProviderManager.loadAllFromPreferences()) ?? []
        manager = managers.first {
            ($0.protocolConfiguration as? NETunnelProviderProtocol)?.providerBundleIdentifier == AppGroup.tunnelBundleIdentifier
        }
        if let manager { observe(manager) }
    }

    /// Screenshot / preview mode: a fake connected session, no NetworkExtension.
    func enableDemo(connected: Bool) {
        demo = true
        state = connected ? .connected : .disconnected
        connectedDate = connected ? Date().addingTimeInterval(-(16 * 60 + 28)) : nil
        downloadRate = connected ? 1_258_291 : 0
        uploadRate = connected ? 143_360 : 0
    }

    var isActive: Bool { state == .connected || state == .connecting }

    func connect(config: String) async {
        guard !demo else { state = .connected; connectedDate = Date(); return }
        do {
            let manager = try await preparedManager()
            try config.write(to: AppGroup.tunnelConfigURL, atomically: true, encoding: .utf8)
            try manager.connection.startVPNTunnel(options: ["config": config as NSString])
            lastError = nil
        } catch {
            lastError = error.localizedDescription
        }
    }

    func disconnect() {
        guard !demo else { state = .disconnected; connectedDate = nil; return }
        manager?.connection.stopVPNTunnel()
    }

    /// Applies a new config to the running tunnel (server or routing change).
    func reload(config: String) async {
        guard !demo else { return }
        try? config.write(to: AppGroup.tunnelConfigURL, atomically: true, encoding: .utf8)
        guard let session = manager?.connection as? NETunnelProviderSession, state == .connected else {
            if state == .connecting { await restart(config: config) }
            return
        }
        let reply = await send(["cmd": TunnelMessage.reload, "config": config], session: session)
        if reply?["ok"] as? Bool != true {
            await restart(config: config)
        }
    }

    private func restart(config: String) async {
        disconnect()
        for _ in 0..<40 where state != .disconnected {
            try? await Task.sleep(nanoseconds: 100_000_000)
        }
        await connect(config: config)
    }

    private func preparedManager() async throws -> NETunnelProviderManager {
        let manager = self.manager ?? NETunnelProviderManager()
        let proto = (manager.protocolConfiguration as? NETunnelProviderProtocol) ?? NETunnelProviderProtocol()
        proto.providerBundleIdentifier = AppGroup.tunnelBundleIdentifier
        proto.serverAddress = "RockBlack"
        proto.disconnectOnSleep = false
        manager.protocolConfiguration = proto
        manager.localizedDescription = "RockBlack"
        manager.isEnabled = true
        manager.isOnDemandEnabled = false
        manager.onDemandRules = []
        try await manager.saveToPreferences()
        try await manager.loadFromPreferences()
        if self.manager !== manager {
            self.manager = manager
            observe(manager)
        }
        return manager
    }

    private func observe(_ manager: NETunnelProviderManager) {
        if let statusObserver { NotificationCenter.default.removeObserver(statusObserver) }
        statusObserver = NotificationCenter.default.addObserver(
            forName: .NEVPNStatusDidChange, object: manager.connection, queue: .main
        ) { [weak self] _ in
            Task { @MainActor in self?.updateStatus() }
        }
        updateStatus()
    }

    private func updateStatus() {
        guard let connection = manager?.connection else { return }
        switch connection.status {
        case .connected:
            state = .connected
            connectedDate = connection.connectedDate ?? Date()
            startStats()
        case .connecting, .reasserting:
            state = .connecting
        case .disconnecting:
            state = .disconnecting
        default:
            state = .disconnected
            connectedDate = nil
            stopStats()
        }
    }

    private func startStats() {
        guard statsTimer == nil else { return }
        lastTotals = nil
        statsTimer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { [weak self] _ in
            Task { @MainActor in await self?.pollStats() }
        }
    }

    private func stopStats() {
        statsTimer?.invalidate()
        statsTimer = nil
        downloadRate = 0
        uploadRate = 0
    }

    private func pollStats() async {
        guard let session = manager?.connection as? NETunnelProviderSession,
              let reply = await send(["cmd": TunnelMessage.stats], session: session) else { return }
        let up = (reply["up"] as? NSNumber)?.int64Value ?? 0
        let down = (reply["down"] as? NSNumber)?.int64Value ?? 0
        let now = Date()
        if let last = lastTotals {
            let seconds = max(now.timeIntervalSince(last.time), 0.5)
            uploadRate = max(Double(up - last.up) / seconds, 0)
            downloadRate = max(Double(down - last.down) / seconds, 0)
        }
        lastTotals = (up, down, now)
    }

    private func send(_ message: [String: Any], session: NETunnelProviderSession) async -> [String: Any]? {
        guard let data = try? JSONSerialization.data(withJSONObject: message) else { return nil }
        return await withCheckedContinuation { continuation in
            do {
                try session.sendProviderMessage(data) { response in
                    let object = response.flatMap { (try? JSONSerialization.jsonObject(with: $0)) as? [String: Any] }
                    continuation.resume(returning: object)
                }
            } catch {
                continuation.resume(returning: nil)
            }
        }
    }
}
