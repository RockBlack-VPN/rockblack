import Foundation

/// Sample content for CI screenshots (`-demo`, `-demoConnected` launch arguments).
enum DemoData {
    @MainActor
    static func apply(to store: AppStore, connected: Bool) {
        func server(_ name: String, _ proto: String, security: String? = nil, network: String = "tcp") -> Server {
            var stream: [String: Any] = ["network": network]
            if let security { stream["security"] = security }
            let outbound: [String: Any] = [
                "protocol": proto,
                "settings": ["address": "example.com", "port": 443],
                "streamSettings": stream,
            ]
            return Server(name: name, outbound: JSONText.encode(outbound) ?? "{}", link: nil)
        }
        let germany = server("🇩🇪 Germany", "vless", security: "reality")
        let netherlands = server("🇳🇱 Netherlands", "vless", security: "reality", network: "xhttp")
        let latvia = server("Latvia", "vless", security: "tls", network: "ws")
        let finland = server("Finland - 02", "trojan", security: "tls")
        var info = SubscriptionInfo(upload: 312_000_000, download: 3_020_000_000, total: 50_000_000_000)
        info.expire = Calendar.current.date(byAdding: .day, value: 42, to: Date())
        info.supportURL = "https://t.me/rockblack_vpn"
        info.webPageURL = "https://my.rockblack.tech"
        let main = Subscription(name: "RockBlack", url: "https://my.rockblack.tech/sub/demo",
                                servers: [germany, netherlands, latvia, finland],
                                updatedAt: Date().addingTimeInterval(-3600), autoUpdateHours: 12, info: info)
        let usa = server("USA - Seattle", "vmess", network: "ws")
        let manual = Subscription(name: L("Server list"), url: nil, servers: [usa])
        store.subscriptions = [main, manual]
        store.selectedServerID = germany.id
        store.routing = .russia
        store.pings = [germany.id: .delay(42), netherlands.id: .delay(118), latvia.id: .delay(564), finland.id: .timeout]
        store.vpn.enableDemo(connected: connected)
    }
}
