import XCTest
@testable import RockBlack

final class ImportTests: XCTestCase {
    func testVlessRealityLinkThroughLibXray() throws {
        let link = "vless://11111111-2222-3333-4444-555555555555@de.example.com:443?encryption=none&flow=xtls-rprx-vision"
            + "&security=reality&sni=www.microsoft.com&fp=chrome&pbk=SbVKOEMjK0sIlbwg4akyBg5mL5KZwwB-ed4eEE7YnRc&sid=6ba85179e30d4fc2&type=tcp#%F0%9F%87%A9%F0%9F%87%AA%20Germany"
        let server = try XCTUnwrap(LinkImporter.server(fromLink: link))
        XCTAssertEqual(server.name, "🇩🇪 Germany")
        XCTAssertEqual(server.protocolName, "VLESS · Reality")
        XCTAssertEqual(server.endpoint?.host, "de.example.com")
        XCTAssertEqual(server.endpoint?.port, 443)
        XCTAssertEqual(Flags.flag(for: server.name), "🇩🇪")
        XCTAssertEqual(Flags.strippedName(server.name), "Germany")
    }

    func testLegacyVmessJSONLink() throws {
        let json = #"{"v":"2","ps":"Netherlands","add":"nl.example.com","port":"443","id":"11111111-2222-3333-4444-555555555555","aid":"0","scy":"auto","net":"ws","type":"none","host":"nl.example.com","path":"/ws","tls":"tls","sni":"nl.example.com"}"#
        let link = "vmess://" + Data(json.utf8).base64EncodedString()
        let server = try XCTUnwrap(LinkImporter.server(fromLink: link))
        XCTAssertEqual(server.name, "Netherlands")
        XCTAssertEqual(server.protocolName, "VMess · TLS · WS")
        XCTAssertEqual(Flags.flag(for: server.name), "🇳🇱")
    }

    func testBase64SubscriptionBody() {
        let body = [
            "trojan://secret@fi.example.com:443?security=tls&sni=fi.example.com#Finland",
            "ss://" + Data("aes-256-gcm:password".utf8).base64EncodedString() + "@1.2.3.4:8388#SS",
            "tuic://unsupported@1.2.3.4:443#TUIC",
        ].joined(separator: "\n")
        let servers = LinkImporter.servers(from: Data(body.utf8).base64EncodedString())
        XCTAssertEqual(servers.map(\.name), ["Finland", "SS"])
    }

    func testWireGuardLink() throws {
        let link = "wireguard://cHJpdmF0ZWtleXByaXZhdGVrZXlwcml2YXRla2V5MDA9@162.159.192.1:2408"
            + "?publickey=bmXOC%2BF1FxEMF9dyiK2H5%2F1SUtzH0JuVo51h2wPfgyo%3D&address=172.16.0.2%2F32&mtu=1280#WARP"
        let server = try XCTUnwrap(LinkImporter.server(fromLink: link))
        XCTAssertEqual(server.name, "WARP")
        XCTAssertEqual(server.protocolName, "WireGuard")
        XCTAssertEqual(server.endpoint?.host, "162.159.192.1")
        XCTAssertEqual(server.endpoint?.port, 2408)
    }

    func testXrayJSONConfigArray() {
        let text = #"[{"remarks":"Latvia","outbounds":[{"tag":"proxy","protocol":"vless","settings":{"address":"lv.example.com","port":443,"id":"11111111-2222-3333-4444-555555555555","encryption":"none"}},{"tag":"direct","protocol":"freedom"}]}]"#
        let servers = LinkImporter.servers(from: text)
        XCTAssertEqual(servers.map(\.name), ["Latvia"])
        XCTAssertEqual(servers.first?.endpoint?.host, "lv.example.com")
    }

    func testSubscriptionHeaders() {
        let headers = [
            "subscription-userinfo": "upload=1073741824; download=2147483648; total=0; expire=1791100800",
            "profile-title": "base64:" + Data("My subscription".utf8).base64EncodedString(),
            "support-url": "https://t.me/rockblack_vpn",
            "profile-update-interval": "12",
        ]
        let result = SubscriptionFetcher.parse(body: "#announce: Hello\nvless://x", header: { headers[$0] })
        XCTAssertEqual(result.title, "My subscription")
        XCTAssertEqual(result.info.used, 3_221_225_472)
        XCTAssertEqual(result.info.total, 0)
        XCTAssertEqual(result.info.expire, Date(timeIntervalSince1970: 1_791_100_800))
        XCTAssertEqual(result.info.supportURL, "https://t.me/rockblack_vpn")
        XCTAssertEqual(result.info.announce, "Hello")
        XCTAssertEqual(result.updateHours, 12)
    }

    func testExpiryDays() {
        let now = Date()
        XCTAssertEqual(SubscriptionInfo(expire: now.addingTimeInterval(2.5 * 86_400)).daysLeft(now: now), 3)
        XCTAssertEqual(SubscriptionInfo(expire: now.addingTimeInterval(-10)).daysLeft(now: now), -1)
        XCTAssertNil(SubscriptionInfo().daysLeft(now: now))
    }

    func testRussiaProfileRouting() throws {
        let outbound: [String: Any] = ["protocol": "vless", "settings": ["address": "a.example.com", "port": 443]]
        let config = XrayConfig.build(outbound: outbound, profile: .russia)
        let routing = try XCTUnwrap(config["routing"] as? [String: Any])
        let rules = try XCTUnwrap(routing["rules"] as? [[String: Any]])
        let blob = JSONText.encode(rules) ?? ""
        for needle in ["geosite:category-ru", "geoip:ru", "domain:xn--p1ai", "geosite:category-ads-all", "geoip:private"] {
            XCTAssertTrue(blob.contains(needle), needle)
        }
        XCTAssertEqual(rules.last?["outboundTag"] as? String, "proxy")
        XCTAssertFalse((JSONText.encode(config) ?? "").contains(":cn"))

        let direct = XrayConfig.build(outbound: outbound, profile: .direct)
        let outbounds = try XCTUnwrap(direct["outbounds"] as? [[String: Any]])
        XCTAssertFalse(outbounds.contains { $0["tag"] as? String == "proxy" })
    }

    func testTunnelPreparationInjectsRuntimeValues() throws {
        let outbound: [String: Any] = ["protocol": "vless", "settings": ["address": "a.example.com", "port": 443]]
        let config = XrayConfig.prepareForTunnel(XrayConfig.build(outbound: outbound, profile: .global), tunFd: 7,
                                                 assetDir: "/geo", metricsPort: 1234, logPath: nil,
                                                 hosts: ["a.example.com": ["1.2.3.4"]])
        let env = try XCTUnwrap(config["env"] as? [String: String])
        XCTAssertEqual(env["xray.tun.fd"], "7")
        XCTAssertEqual(env["xray.location.asset"], "/geo")
        let proxy = try XCTUnwrap((config["outbounds"] as? [[String: Any]])?.first)
        let sockopt = (proxy["streamSettings"] as? [String: Any])?["sockopt"] as? [String: Any]
        XCTAssertEqual(sockopt?["domainStrategy"] as? String, "UseIP")
    }

    /// Every profile must build in Xray-core with the geo files bundled in the tunnel
    /// (this proves geosite:category-ru / category-ads-all and geoip:ru / private exist).
    func testProfilesBuildWithBundledGeoData() throws {
        let appex = try XCTUnwrap(Bundle.main.builtInPlugInsURL?.appendingPathComponent("RockBlackTunnel.appex"))
        XCTAssertTrue(FileManager.default.fileExists(atPath: appex.appendingPathComponent("geosite.dat").path))
        XCTAssertTrue(FileManager.default.fileExists(atPath: appex.appendingPathComponent("geoip.dat").path))
        let outbound: [String: Any] = [
            "protocol": "vless",
            "settings": ["address": "a.example.com", "port": 443, "id": "11111111-2222-3333-4444-555555555555",
                         "encryption": "none"],
            "streamSettings": ["network": "tcp", "security": "tls", "tlsSettings": ["serverName": "a.example.com"]],
        ]
        for profile in RoutingProfile.allCases {
            var config = XrayConfig.prepareForTunnel(XrayConfig.build(outbound: outbound, profile: profile), tunFd: 0,
                                                     assetDir: appex.path, metricsPort: nil, logPath: nil, hosts: [:])
            config["inbounds"] = [[String: Any]]()
            config["env"] = ["xray.location.asset": appex.path]
            let response = XrayCore.invoke("testXray", payload: ["xrayJson": JSONText.encode(config) ?? ""])
            XCTAssertTrue(response.success, "\(profile): \(response.error)")
        }
    }
}
