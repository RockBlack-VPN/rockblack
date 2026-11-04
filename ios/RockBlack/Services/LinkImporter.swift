import Foundation

/// Turns subscription bodies, share links and Xray JSON into servers.
///
/// vless / vmess (AEAD) / trojan / ss / socks / hysteria2 links are converted
/// and validated by libXray. Legacy `vmess://base64(json)` and `wireguard://`
/// links are mapped here (libXray does not parse them). TUIC is not supported
/// by Xray-core and is skipped.
enum LinkImporter {
    static let ignoredProtocols: Set<String> = ["freedom", "blackhole", "dns", "loopback"]

    static func servers(from text: String) -> [Server] {
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else { return [] }
        if trimmed.hasPrefix("{") || trimmed.hasPrefix("[") {
            return jsonServers(trimmed)
        }
        let lines = trimmed.components(separatedBy: .newlines)
            .map { $0.trimmingCharacters(in: .whitespaces) }
        if !lines.contains(where: { $0.contains("://") }) {
            if let decoded = decodeBase64(trimmed), decoded != trimmed,
               decoded.contains("://") || decoded.hasPrefix("{") || decoded.hasPrefix("[") {
                return servers(from: decoded)
            }
            return []
        }
        var result: [Server] = []
        for line in lines where line.contains("://") && !line.hasPrefix("#") && !line.hasPrefix("//")
            && !line.lowercased().hasPrefix("routing:") && !line.lowercased().hasPrefix(HappRouting.prefix) {
            if let server = server(fromLink: line) { result.append(server) }
        }
        return result
    }

    static func server(fromLink link: String) -> Server? {
        let lower = link.lowercased()
        var outbound: [String: Any]?
        if lower.hasPrefix("vmess://"), let legacy = legacyVmess(link) {
            outbound = validated(legacy)
        } else if lower.hasPrefix("wireguard://") || lower.hasPrefix("wg://") {
            // libXray's share projection has no WireGuard format; the tunnel validates on start.
            outbound = wireguard(link)
        } else {
            outbound = XrayCore.convertShareLinks(link).first
        }
        guard let outbound, let json = JSONText.encode(outbound) else { return nil }
        return Server(name: displayName(outbound, link: link), outbound: json, link: link)
    }

    /// Run hand-built outbounds through libXray's builder check.
    static func validated(_ outbound: [String: Any]) -> [String: Any]? {
        guard let json = JSONText.encode(["outbounds": [outbound]]) else { return nil }
        guard let projected = XrayCore.convertShareLinks(json).first else { return nil }
        // Keep fields libXray's share projection does not know about.
        var merged = outbound
        for (key, value) in projected where merged[key] == nil { merged[key] = value }
        return merged
    }

    static func displayName(_ outbound: [String: Any], link: String?) -> String {
        if let tag = outbound["tag"] as? String, !tag.isEmpty { return tag }
        if let link, let hash = link.firstIndex(of: "#") {
            let fragment = String(link[link.index(after: hash)...])
            let name = fragment.removingPercentEncoding ?? fragment
            if !name.isEmpty { return name }
        }
        if let endpoint = XrayConfig.serverEndpoint(outbound) { return "\(endpoint.host):\(endpoint.port)" }
        return Server.describe(outbound)
    }

    // MARK: - Xray JSON

    static func jsonServers(_ text: String) -> [Server] {
        guard let data = text.data(using: .utf8), let root = try? JSONSerialization.jsonObject(with: data) else { return [] }
        let configs: [[String: Any]]
        if let array = root as? [[String: Any]] {
            configs = array
        } else if let object = root as? [String: Any] {
            configs = [object]
        } else {
            return []
        }
        var result: [Server] = []
        for config in configs {
            let remarks = config["remarks"] as? String
            let outbounds = (config["outbounds"] as? [[String: Any]] ?? []).filter {
                !ignoredProtocols.contains(($0["protocol"] as? String ?? "").lowercased())
            }
            // A full client config: take its proxy outbound. A node list: take every node.
            let picked: [[String: Any]]
            if let proxy = outbounds.first(where: { $0["tag"] as? String == XrayConfig.proxyTag }) {
                picked = [proxy]
            } else if remarks != nil, let first = outbounds.first {
                picked = [first]
            } else {
                picked = outbounds
            }
            for outbound in picked {
                guard let json = JSONText.encode(outbound) else { continue }
                let name = (picked.count == 1 ? remarks : nil) ?? displayName(outbound, link: nil)
                result.append(Server(name: name, outbound: json, link: nil))
            }
        }
        return result
    }

    // MARK: - Legacy VMess (v2rayN JSON)

    static func legacyVmess(_ link: String) -> [String: Any]? {
        let payload = String(link.dropFirst("vmess://".count))
        guard let decoded = decodeBase64(payload), let json = JSONText.object(decoded) else { return nil }
        func str(_ key: String) -> String {
            if let value = json[key] as? String { return value }
            if let value = json[key] as? Int { return String(value) }
            return ""
        }
        guard let port = Int(str("port")), !str("add").isEmpty, !str("id").isEmpty else { return nil }
        let network = str("net").isEmpty ? "tcp" : str("net").lowercased()
        var stream: [String: Any] = ["network": network == "h2" ? "xhttp" : network]
        let host = str("host"), path = str("path")
        switch network {
        case "ws":
            var ws: [String: Any] = ["path": path.isEmpty ? "/" : path]
            if !host.isEmpty { ws["host"] = host }
            stream["wsSettings"] = ws
        case "grpc":
            stream["grpcSettings"] = ["serviceName": path, "multiMode": str("type") == "multi"]
        case "httpupgrade":
            var upgrade: [String: Any] = ["path": path.isEmpty ? "/" : path]
            if !host.isEmpty { upgrade["host"] = host }
            stream["httpupgradeSettings"] = upgrade
        case "xhttp", "splithttp", "h2":
            var xhttp: [String: Any] = ["path": path.isEmpty ? "/" : path]
            if !host.isEmpty { xhttp["host"] = host }
            if network == "xhttp", !str("type").isEmpty { xhttp["mode"] = str("type") }
            stream["xhttpSettings"] = xhttp
        default:
            break
        }
        if str("tls").lowercased() == "tls" {
            var tls: [String: Any] = [:]
            let sni = str("sni").isEmpty ? host : str("sni")
            if !sni.isEmpty { tls["serverName"] = sni }
            if !str("alpn").isEmpty { tls["alpn"] = str("alpn").split(separator: ",").map(String.init) }
            if !str("fp").isEmpty { tls["fingerprint"] = str("fp") }
            stream["security"] = "tls"
            stream["tlsSettings"] = tls
        }
        var settings: [String: Any] = ["address": str("add"), "port": port, "id": str("id")]
        let security = str("scy")
        settings["security"] = security.isEmpty ? "auto" : security
        var outbound: [String: Any] = ["protocol": "vmess", "settings": settings, "streamSettings": stream]
        if !str("ps").isEmpty { outbound["tag"] = str("ps") }
        return outbound
    }

    // MARK: - WireGuard

    static func wireguard(_ link: String) -> [String: Any]? {
        let normalized = link.replacingOccurrences(of: "wg://", with: "wireguard://", options: [.anchored, .caseInsensitive])
        guard let components = URLComponents(string: normalized), let host = components.host,
              let secret = components.user?.removingPercentEncoding, !secret.isEmpty else { return nil }
        var query: [String: String] = [:]
        for item in components.queryItems ?? [] { query[item.name.lowercased()] = item.value ?? "" }
        guard let publicKey = query["publickey"] ?? query["public_key"] ?? query["peer"], !publicKey.isEmpty else { return nil }
        let port = components.port ?? 51820
        let endpointHost = host.contains(":") ? "[\(host)]" : host
        var peer: [String: Any] = ["publicKey": publicKey, "endpoint": "\(endpointHost):\(port)"]
        if let psk = query["presharedkey"] ?? query["psk"], !psk.isEmpty { peer["preSharedKey"] = psk }
        let addresses = (query["address"] ?? query["ip"] ?? "10.0.0.2/32")
            .split(separator: ",").map { $0.trimmingCharacters(in: .whitespaces) }
        var settings: [String: Any] = ["secretKey": secret, "address": addresses, "peers": [peer]]
        if let mtu = query["mtu"].flatMap(Int.init) { settings["mtu"] = mtu }
        if let reserved = query["reserved"], !reserved.isEmpty {
            let bytes = reserved.split(separator: ",").compactMap { Int($0.trimmingCharacters(in: .whitespaces)) }
            if bytes.count == 3 { settings["reserved"] = bytes }
        }
        var outbound: [String: Any] = ["protocol": "wireguard", "settings": settings]
        if let fragment = components.fragment, !fragment.isEmpty { outbound["tag"] = fragment }
        return outbound
    }

    // MARK: - Helpers

    static func decodeBase64(_ text: String) -> String? {
        var value = text.components(separatedBy: .whitespacesAndNewlines).joined()
            .replacingOccurrences(of: "-", with: "+")
            .replacingOccurrences(of: "_", with: "/")
        let padding = value.count % 4
        if padding != 0 { value += String(repeating: "=", count: 4 - padding) }
        guard let data = Data(base64Encoded: value) else { return nil }
        return String(data: data, encoding: .utf8)
    }
}
