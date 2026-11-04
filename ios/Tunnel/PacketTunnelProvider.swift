import Foundation
import NetworkExtension
import os.log

/// Packet tunnel: Xray-core reads/writes the utun file descriptor directly
/// through its native TUN inbound (`env["xray.tun.fd"]`), so no tun2socks
/// layer is needed and only one Go runtime lives in this process.
final class PacketTunnelProvider: NEPacketTunnelProvider {
    private let log = OSLog(subsystem: "tech.rockblack.app.tunnel", category: "tunnel")
    private let core = DispatchQueue(label: "tech.rockblack.tunnel.core")
    private var tunFd: Int32?
    private var metricsPort: Int?

    enum TunnelError: LocalizedError {
        case noConfig, noTunnelDescriptor, core(String)
        var errorDescription: String? {
            switch self {
            case .noConfig: return "No server configuration. Open RockBlack and connect again."
            case .noTunnelDescriptor: return "The tunnel interface is not available."
            case .core(let message): return message
            }
        }
    }

    override func startTunnel(options: [String: NSObject]?, completionHandler: @escaping (Error?) -> Void) {
        let configText: String
        if let text = options?["config"] as? String {
            configText = text
            try? text.write(to: AppGroup.tunnelConfigURL, atomically: true, encoding: .utf8)
        } else if let text = try? String(contentsOf: AppGroup.tunnelConfigURL, encoding: .utf8) {
            configText = text
        } else {
            completionHandler(TunnelError.noConfig)
            return
        }
        guard let config = JSONText.object(configText) else {
            completionHandler(TunnelError.noConfig)
            return
        }
        // Resolve the proxy server before the tunnel routes take over DNS.
        let hosts = Self.resolveServerHosts(config)

        setTunnelNetworkSettings(Self.networkSettings()) { [weak self] error in
            guard let self else { return }
            if let error {
                completionHandler(error)
                return
            }
            guard let fd = self.findTunnelDescriptor() else {
                completionHandler(TunnelError.noTunnelDescriptor)
                return
            }
            self.tunFd = fd
            self.core.async {
                completionHandler(self.runCore(config, fd: fd, hosts: hosts))
            }
        }
    }

    override func stopTunnel(with reason: NEProviderStopReason, completionHandler: @escaping () -> Void) {
        core.async {
            _ = XrayCore.invoke("stopXray")
            completionHandler()
        }
    }

    override func handleAppMessage(_ messageData: Data, completionHandler: ((Data?) -> Void)?) {
        guard let message = (try? JSONSerialization.jsonObject(with: messageData)) as? [String: Any],
              let command = message["cmd"] as? String else {
            completionHandler?(nil)
            return
        }
        switch command {
        case TunnelMessage.reload:
            guard let text = message["config"] as? String, let config = JSONText.object(text), let fd = tunFd else {
                completionHandler?(Self.reply(["ok": false, "error": "bad reload request"]))
                return
            }
            try? text.write(to: AppGroup.tunnelConfigURL, atomically: true, encoding: .utf8)
            let hosts = Self.resolveServerHosts(config)
            core.async {
                _ = XrayCore.invoke("stopXray")
                let error = self.runCore(config, fd: fd, hosts: hosts)
                completionHandler?(Self.reply(["ok": error == nil, "error": error?.localizedDescription ?? ""]))
            }
        case TunnelMessage.stats:
            fetchStats { completionHandler?(Self.reply($0)) }
        default:
            completionHandler?(nil)
        }
    }

    // MARK: - Core

    private func runCore(_ config: [String: Any], fd: Int32, hosts: [String: [String]]) -> Error? {
        if metricsPort == nil { metricsPort = XrayCore.freePort() }
        let prepared = XrayConfig.prepareForTunnel(config, tunFd: fd, assetDir: Bundle.main.bundlePath,
                                                   metricsPort: metricsPort, logPath: AppGroup.tunnelLogURL.path,
                                                   hosts: hosts)
        try? Data().write(to: AppGroup.tunnelLogURL)
        guard let json = JSONText.encode(prepared) else { return TunnelError.core("config encoding failed") }
        let response = XrayCore.invoke("runXray", payload: ["xrayJson": json])
        if !response.success {
            os_log("runXray failed: %{public}@", log: log, type: .error, response.error)
            return TunnelError.core(response.error)
        }
        return nil
    }

    private func fetchStats(_ done: @escaping ([String: Any]) -> Void) {
        guard let port = metricsPort, let url = URL(string: "http://127.0.0.1:\(port)/debug/vars") else {
            done(["up": 0, "down": 0])
            return
        }
        var request = URLRequest(url: url, timeoutInterval: 2)
        request.cachePolicy = .reloadIgnoringLocalCacheData
        URLSession.shared.dataTask(with: request) { data, _, _ in
            var up: Int64 = 0, down: Int64 = 0
            if let data, let vars = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any],
               let stats = vars["stats"] as? [String: Any],
               let outbound = stats["outbound"] as? [String: Any] {
                for (tag, value) in outbound where tag == XrayConfig.proxyTag || tag == XrayConfig.directTag {
                    guard let counters = value as? [String: Any] else { continue }
                    up += (counters["uplink"] as? NSNumber)?.int64Value ?? 0
                    down += (counters["downlink"] as? NSNumber)?.int64Value ?? 0
                }
            }
            done(["up": up, "down": down])
        }.resume()
    }

    // MARK: - Network

    private static func networkSettings() -> NEPacketTunnelNetworkSettings {
        let settings = NEPacketTunnelNetworkSettings(tunnelRemoteAddress: "254.1.1.1")
        let ipv4 = NEIPv4Settings(addresses: ["198.18.0.1"], subnetMasks: ["255.255.255.0"])
        ipv4.includedRoutes = [NEIPv4Route.default()]
        ipv4.excludedRoutes = [
            NEIPv4Route(destinationAddress: "10.0.0.0", subnetMask: "255.0.0.0"),
            NEIPv4Route(destinationAddress: "172.16.0.0", subnetMask: "255.240.0.0"),
            NEIPv4Route(destinationAddress: "192.168.0.0", subnetMask: "255.255.0.0"),
            NEIPv4Route(destinationAddress: "169.254.0.0", subnetMask: "255.255.0.0"),
            NEIPv4Route(destinationAddress: "224.0.0.0", subnetMask: "240.0.0.0"),
        ]
        settings.ipv4Settings = ipv4
        let ipv6 = NEIPv6Settings(addresses: ["fd66:7262:6c6b::1"], networkPrefixLengths: [64])
        ipv6.includedRoutes = [NEIPv6Route.default()]
        ipv6.excludedRoutes = [
            NEIPv6Route(destinationAddress: "fe80::", networkPrefixLength: 10),
            NEIPv6Route(destinationAddress: "ff00::", networkPrefixLength: 8),
        ]
        settings.ipv6Settings = ipv6
        let dns = NEDNSSettings(servers: ["1.1.1.1"])
        dns.matchDomains = [""]
        settings.dnsSettings = dns
        settings.mtu = NSNumber(value: XrayConfig.tunMTU)
        return settings
    }

    /// The utun socket NetworkExtension created for this tunnel (Xray-core proxy/tun README, iOS section).
    private func findTunnelDescriptor() -> Int32? {
        var buffer = [CChar](repeating: 0, count: Int(IFNAMSIZ))
        for fd: Int32 in 0...1024 {
            var length = socklen_t(buffer.count)
            // SYSPROTO_CONTROL = 2, UTUN_OPT_IFNAME = 2
            if getsockopt(fd, 2, 2, &buffer, &length) == 0, String(cString: buffer).hasPrefix("utun") {
                return fd
            }
        }
        if let fd = packetFlow.value(forKeyPath: "socket.fileDescriptor") as? Int32 { return fd }
        return nil
    }

    private static func resolveServerHosts(_ config: [String: Any]) -> [String: [String]] {
        guard let outbounds = config["outbounds"] as? [[String: Any]],
              let proxy = outbounds.first(where: { $0["tag"] as? String == XrayConfig.proxyTag }),
              let host = XrayConfig.serverHost(proxy) else { return [:] }
        let addresses = resolve(host)
        return addresses.isEmpty ? [:] : [host: addresses]
    }

    private static func resolve(_ host: String) -> [String] {
        var hints = addrinfo(ai_flags: 0, ai_family: AF_INET, ai_socktype: SOCK_STREAM, ai_protocol: 0,
                             ai_addrlen: 0, ai_canonname: nil, ai_addr: nil, ai_next: nil)
        var result: UnsafeMutablePointer<addrinfo>?
        guard getaddrinfo(host, nil, &hints, &result) == 0, let first = result else { return [] }
        defer { freeaddrinfo(first) }
        var addresses: [String] = []
        var cursor: UnsafeMutablePointer<addrinfo>? = first
        while let info = cursor {
            if let addr = info.pointee.ai_addr, info.pointee.ai_family == AF_INET {
                var buffer = [CChar](repeating: 0, count: Int(INET_ADDRSTRLEN))
                addr.withMemoryRebound(to: sockaddr_in.self, capacity: 1) { pointer in
                    var sin = pointer.pointee.sin_addr
                    _ = inet_ntop(AF_INET, &sin, &buffer, socklen_t(INET_ADDRSTRLEN))
                }
                let text = String(cString: buffer)
                if !text.isEmpty, !addresses.contains(text) { addresses.append(text) }
            }
            cursor = info.pointee.ai_next
        }
        return addresses
    }

    private static func reply(_ object: [String: Any]) -> Data? {
        try? JSONSerialization.data(withJSONObject: object)
    }
}
