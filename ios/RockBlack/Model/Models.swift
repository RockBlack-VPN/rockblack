import Foundation

struct Server: Identifiable, Codable, Hashable {
    var id = UUID()
    var name: String
    /// Xray outbound object as JSON text.
    var outbound: String
    /// Original share link, if the server was imported from one.
    var link: String?

    var outboundObject: [String: Any]? { JSONText.object(outbound) }

    var protocolName: String {
        guard let object = outboundObject else { return "?" }
        return Server.describe(object)
    }

    var endpoint: (host: String, port: Int)? {
        outboundObject.flatMap(XrayConfig.serverEndpoint)
    }

    static func describe(_ outbound: [String: Any]) -> String {
        let proto = (outbound["protocol"] as? String ?? "").lowercased()
        let names = ["vless": "VLESS", "vmess": "VMess", "trojan": "Trojan", "shadowsocks": "Shadowsocks",
                     "hysteria": "Hysteria2", "hysteria2": "Hysteria2", "wireguard": "WireGuard",
                     "socks": "SOCKS", "http": "HTTP"]
        var parts = [names[proto] ?? proto.uppercased()]
        if let stream = outbound["streamSettings"] as? [String: Any] {
            switch (stream["security"] as? String ?? "").lowercased() {
            case "reality": parts.append("Reality")
            case "tls" where proto != "hysteria" && proto != "hysteria2": parts.append("TLS")
            default: break
            }
            let networks = ["ws": "WS", "grpc": "gRPC", "xhttp": "XHTTP", "splithttp": "XHTTP",
                            "httpupgrade": "HTTPUpgrade", "kcp": "mKCP", "mkcp": "mKCP"]
            if let network = (stream["network"] as? String)?.lowercased(), let label = networks[network] {
                parts.append(label)
            }
        }
        return parts.joined(separator: " · ")
    }
}

/// Values from the `subscription-userinfo`, `support-url`, `profile-web-page-url`
/// and `announce` response headers (or `#key: value` body lines).
struct SubscriptionInfo: Codable, Hashable {
    var upload: Int64 = 0
    var download: Int64 = 0
    var total: Int64 = 0
    var expire: Date?
    var supportURL: String?
    var webPageURL: String?
    var announce: String?

    var used: Int64 { upload + download }
    var hasTraffic: Bool { used > 0 || total > 0 }

    /// Days until expiry (negative when expired), nil without an expiry date.
    func daysLeft(now: Date = Date()) -> Int? {
        guard let expire else { return nil }
        let seconds = expire.timeIntervalSince(now)
        if seconds <= 0 { return -1 }
        return Int((seconds / 86_400).rounded(.up))
    }
}

enum ServerSort: String, Codable, CaseIterable {
    case delivered, name, ping
}

struct Subscription: Identifiable, Codable, Hashable {
    var id = UUID()
    var name: String
    /// nil for the manual "Server list" group.
    var url: String?
    var servers: [Server] = []
    var updatedAt: Date?
    var autoUpdateHours: Int = 12
    var info: SubscriptionInfo?
    var collapsed = false
    var sort: ServerSort = .delivered

    var isManual: Bool { url == nil }
}

enum PingResult: Equatable {
    case testing
    case delay(Int)
    case timeout
    case failed
}

struct StoreSnapshot: Codable {
    var subscriptions: [Subscription]
    var selectedServerID: UUID?
    var routing: RoutingProfile
}
