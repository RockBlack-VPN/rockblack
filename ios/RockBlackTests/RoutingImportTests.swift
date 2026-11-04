import XCTest
@testable import RockBlack

final class RoutingImportTests: XCTestCase {
    private let happJSON = """
    {"Name":"Panel RU","GlobalProxy":"true","DirectSites":["geosite:category-ru","domain:ya.ru","geosite:youtube"],
     "DirectIp":["geoip:ru","10.10.0.0/16","geoip:cn","not-an-ip"],"ProxySites":["domain:youtube.com","ext:custom.dat:x"],
     "ProxyIp":[],"BlockSites":["geosite:category-ads-all","regexp:(unclosed"],"BlockIp":[],"DomainStrategy":"IPIfNonMatch"}
    """

    private func base64URL(_ text: String) -> String {
        Data(text.utf8).base64EncodedString()
            .replacingOccurrences(of: "+", with: "-").replacingOccurrences(of: "/", with: "_")
            .replacingOccurrences(of: "=", with: "")
    }

    func testFindsAndParsesLinksFromHeaderAndBody() throws {
        let b64 = base64URL(happJSON)
        let header = try XCTUnwrap(HappRouting.parseLink(HappRouting.findLink("happ://routing/onadd/\(b64)")))
        XCTAssertTrue(header.activate)
        XCTAssertEqual(header.json, happJSON)

        let body = "vless://abc@host:443#A\nrouting: happ://routing/add/\(b64)\nvmess://xyz"
        let found = try XCTUnwrap(HappRouting.findLink(body))
        XCTAssertEqual(found, "happ://routing/add/\(b64)")
        XCTAssertEqual(HappRouting.parseLink(found)?.activate, false)
        XCTAssertNotNil(HappRouting.parseLink("rockblack://routing/add/\(b64)"))

        XCTAssertNil(HappRouting.parseLink("happ://routing/remove/\(b64)"))
        XCTAssertNil(HappRouting.findLink("vless://abc@host:443"))

        let result = SubscriptionFetcher.parse(body: body, header: { _ in nil })
        XCTAssertEqual(result.routingLink, found)
    }

    func testMapsHappJSONAndDropsUnsupportedEntries() throws {
        let raw = try XCTUnwrap(HappRouting.profile(fromJSON: happJSON))
        let profile = raw.sanitized()
        XCTAssertEqual(profile.name, "Panel RU")
        XCTAssertEqual(profile.domainStrategy, "IPIfNonMatch")
        XCTAssertTrue(profile.globalProxy)
        XCTAssertEqual(profile.directSites, ["geosite:category-ru", "domain:ya.ru"])
        XCTAssertEqual(profile.directIp, ["geoip:ru", "10.10.0.0/16"])
        XCTAssertEqual(profile.proxySites, ["domain:youtube.com"])
        XCTAssertEqual(profile.blockSites, ["geosite:category-ads-all"])
        XCTAssertEqual(Set(profile.skipped),
                       ["geosite:youtube", "geoip:cn", "not-an-ip", "ext:custom.dat:x", "regexp:(unclosed"])

        let config = XrayConfig.build(outbound: ["protocol": "vless"], custom: profile)
        let rules = try XCTUnwrap((config["routing"] as? [String: Any])?["rules"] as? [[String: Any]])
        let tags = rules.compactMap { $0["outboundTag"] as? String }
        // dns hijack → block → private direct → proxy → direct → final proxy
        XCTAssertEqual(tags, ["dns-out", "block", "direct", "proxy", "direct", "direct", "proxy"])
        XCTAssertEqual((config["routing"] as? [String: Any])?["domainStrategy"] as? String, "IPIfNonMatch")
    }

    func testNonGlobalProfileEndsDirect() throws {
        let profile = try XCTUnwrap(HappRouting.profile(fromJSON: #"{"GlobalProxy":false,"ProxySites":["domain:x.com"]}"#))
        XCTAssertFalse(profile.globalProxy)
        let config = XrayConfig.build(outbound: ["protocol": "vless"], custom: profile.sanitized())
        let rules = try XCTUnwrap((config["routing"] as? [String: Any])?["rules"] as? [[String: Any]])
        XCTAssertEqual(rules.last?["outboundTag"] as? String, "direct")
        XCTAssertNil(HappRouting.profile(fromJSON: "not json"))
        XCTAssertNil(HappRouting.profile(fromJSON: #"{"Name":"Empty"}"#))
    }

    /// The sanitized profile must build in Xray-core with the geo files bundled in the tunnel.
    @MainActor
    func testImportedProfileBuildsWithBundledGeoData() async throws {
        let link = "happ://routing/onadd/" + base64URL(happJSON)
        let parsed = try XCTUnwrap(HappRouting.profile(fromLink: link))
        let validated = await AppStore.validated(parsed.profile)
        let profile = try XCTUnwrap(validated)
        let appex = try XCTUnwrap(Bundle.main.builtInPlugInsURL?.appendingPathComponent("RockBlackTunnel.appex"))
        let outbound: [String: Any] = [
            "protocol": "vless",
            "settings": ["address": "a.example.com", "port": 443, "id": "11111111-2222-3333-4444-555555555555",
                         "encryption": "none"],
            "streamSettings": ["network": "tcp", "security": "tls", "tlsSettings": ["serverName": "a.example.com"]],
        ]
        var config = XrayConfig.build(outbound: outbound, custom: profile)
        config["inbounds"] = [[String: Any]]()
        config["env"] = ["xray.location.asset": appex.path]
        let response = XrayCore.invoke("testXray", payload: ["xrayJson": JSONText.encode(config) ?? ""])
        XCTAssertTrue(response.success, response.error)

        let store = AppStore()
        let imported = await store.importRouting(link, activate: false, interactive: true)
        XCTAssertTrue(imported)
        XCTAssertEqual(store.customRoutings.count, 1)
        XCTAssertEqual(store.activeCustomRouting?.name, "Panel RU")  // onadd activates
        XCTAssertEqual(store.routingTitle, "Panel RU")
        // Re-import replaces by name; deleting falls back to the preset.
        _ = await store.importRouting(link, activate: false, interactive: false)
        XCTAssertEqual(store.customRoutings.count, 1)
        let activeID = try XCTUnwrap(store.customRoutingID)
        store.deleteCustomRouting(activeID)
        XCTAssertNil(store.activeCustomRouting)
        XCTAssertTrue(store.customRoutings.isEmpty)
    }
}
