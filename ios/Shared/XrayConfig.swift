import Foundation

/// Routing presets (DESIGN.md → Routing). Russia is the default.
enum RoutingProfile: String, CaseIterable, Codable, Identifiable {
    case global
    case russia
    case adBlock
    case direct

    var id: String { rawValue }
}

/// Builds the Xray JSON the tunnel runs. The tunnel adds the runtime-only parts
/// (TUN fd, asset path, metrics, log) through `XrayConfig.prepareForTunnel`.
enum XrayConfig {
    static let tunTag = "tun"
    static let proxyTag = "proxy"
    static let directTag = "direct"
    static let blockTag = "block"
    static let dnsTag = "dns-out"
    static let tunMTU = 1500

    static let russianDomains = ["geosite:category-ru", "domain:ru", "domain:su", "domain:xn--p1ai"]
    static let adDomains = ["geosite:category-ads-all"]

    static func build(outbound: [String: Any]?, profile: RoutingProfile) -> [String: Any] {
        let useProxy = profile != .direct && outbound != nil
        var outbounds: [[String: Any]] = []
        if useProxy, var proxy = outbound {
            proxy["tag"] = proxyTag
            outbounds.append(proxy)
        }
        outbounds.append(["tag": directTag, "protocol": "freedom", "settings": [String: Any]()])
        outbounds.append(["tag": blockTag, "protocol": "blackhole", "settings": [String: Any]()])
        outbounds.append(["tag": dnsTag, "protocol": "dns", "settings": [String: Any]()])

        let fallback = useProxy ? proxyTag : directTag
        var rules: [[String: Any]] = [
            ["type": "field", "inboundTag": [tunTag], "port": "53", "outboundTag": dnsTag],
            ["type": "field", "ip": ["geoip:private"], "outboundTag": directTag],
        ]
        switch profile {
        case .russia:
            rules.append(["type": "field", "domain": adDomains, "outboundTag": blockTag])
            rules.append(["type": "field", "domain": russianDomains, "outboundTag": directTag])
            rules.append(["type": "field", "ip": ["geoip:ru"], "outboundTag": directTag])
        case .adBlock:
            rules.append(["type": "field", "domain": adDomains, "outboundTag": blockTag])
        case .global, .direct:
            break
        }
        rules.append(["type": "field", "network": "tcp,udp", "outboundTag": fallback])

        var servers: [Any]
        switch profile {
        case .russia:
            servers = [["address": "77.88.8.8", "domains": russianDomains, "skipFallback": true] as [String: Any],
                       "1.1.1.1", "8.8.8.8"]
        case .direct:
            servers = ["77.88.8.8", "1.1.1.1"]
        case .global, .adBlock:
            servers = ["1.1.1.1", "8.8.8.8"]
        }
        if !useProxy { servers = ["77.88.8.8", "1.1.1.1"] }

        return [
            "log": ["loglevel": "warning"],
            "dns": ["servers": servers, "queryStrategy": "UseIPv4"],
            "inbounds": [[
                "tag": tunTag,
                "port": 0,
                "protocol": "tun",
                "settings": ["name": "utun", "mtu": tunMTU],
                "sniffing": ["enabled": true, "destOverride": ["http", "tls", "quic"], "routeOnly": true],
            ] as [String: Any]],
            "outbounds": outbounds,
            "routing": ["domainStrategy": "IPIfNonMatch", "rules": rules],
        ]
    }

    /// Adds what only the tunnel knows: TUN fd, geo asset directory, metrics port,
    /// log file and pre-resolved proxy server addresses (`hosts`).
    static func prepareForTunnel(_ config: [String: Any], tunFd: Int32, assetDir: String,
                                 metricsPort: Int?, logPath: String?, hosts: [String: [String]]) -> [String: Any] {
        var root = config
        var env = root["env"] as? [String: String] ?? [:]
        env["xray.tun.fd"] = String(tunFd)
        env["xray.location.asset"] = assetDir
        root["env"] = env
        var log: [String: Any] = ["loglevel": "warning"]
        if let logPath { log["error"] = logPath }
        root["log"] = log
        if let metricsPort {
            root["metrics"] = ["listen": "127.0.0.1:\(metricsPort)"]
            root["stats"] = [String: Any]()
            root["policy"] = ["system": ["statsOutboundUplink": true, "statsOutboundDownlink": true]]
        }
        if !hosts.isEmpty {
            var dns = root["dns"] as? [String: Any] ?? [:]
            var existing = dns["hosts"] as? [String: Any] ?? [:]
            for (host, ips) in hosts { existing[host] = ips }
            dns["hosts"] = existing
            root["dns"] = dns
            // Resolve the server name through Xray's DNS (and therefore `hosts`)
            // instead of the system resolver, which points into the tunnel.
            if var outbounds = root["outbounds"] as? [[String: Any]] {
                for index in outbounds.indices where outbounds[index]["tag"] as? String == proxyTag {
                    guard let host = serverHost(outbounds[index]), hosts[host] != nil else { continue }
                    var stream = outbounds[index]["streamSettings"] as? [String: Any] ?? [:]
                    var sockopt = stream["sockopt"] as? [String: Any] ?? [:]
                    if sockopt["domainStrategy"] == nil { sockopt["domainStrategy"] = "UseIP" }
                    stream["sockopt"] = sockopt
                    outbounds[index]["streamSettings"] = stream
                }
                root["outbounds"] = outbounds
            }
        }
        return root
    }

    /// Host name of an outbound's server (nil for IP literals).
    static func serverHost(_ outbound: [String: Any]) -> String? {
        guard let host = serverEndpoint(outbound)?.host, !host.isEmpty, !isIPAddress(host) else { return nil }
        return host
    }

    /// Server address and port for TCP ping / display. Handles both the flat
    /// (`settings.address`) and legacy (`vnext` / `servers` / `peers`) layouts.
    static func serverEndpoint(_ outbound: [String: Any]) -> (host: String, port: Int)? {
        guard let settings = outbound["settings"] as? [String: Any] else { return nil }
        func endpoint(_ dict: [String: Any]) -> (String, Int)? {
            guard let address = dict["address"] as? String else { return nil }
            return (address, intValue(dict["port"]) ?? 443)
        }
        if let value = endpoint(settings) { return value }
        for key in ["vnext", "servers"] {
            if let first = (settings[key] as? [[String: Any]])?.first, let value = endpoint(first) { return value }
        }
        if let peer = (settings["peers"] as? [[String: Any]])?.first, let raw = peer["endpoint"] as? String {
            return splitHostPort(raw)
        }
        return nil
    }

    static func splitHostPort(_ raw: String) -> (host: String, port: Int)? {
        if raw.hasPrefix("["), let close = raw.firstIndex(of: "]") {
            let host = String(raw[raw.index(after: raw.startIndex)..<close])
            let rest = raw[raw.index(after: close)...]
            let port = rest.hasPrefix(":") ? Int(rest.dropFirst()) : nil
            return (host, port ?? 443)
        }
        let parts = raw.split(separator: ":")
        if parts.count == 2, let port = Int(parts[1]) { return (String(parts[0]), port) }
        return raw.isEmpty ? nil : (raw, 443)
    }

    static func intValue(_ any: Any?) -> Int? {
        if let value = any as? Int { return value }
        if let value = any as? String { return Int(value) }
        if let value = any as? Double { return Int(value) }
        return nil
    }

    static func isIPAddress(_ host: String) -> Bool {
        var v4 = in_addr()
        var v6 = in6_addr()
        return inet_pton(AF_INET, host, &v4) == 1 || inet_pton(AF_INET6, host, &v6) == 1
    }
}
