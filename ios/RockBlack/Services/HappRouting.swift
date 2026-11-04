import Foundation

/// Happ-compatible routing profile import (same mapping as RockBlack Android):
/// `happ://routing/add/{base64}` stores a profile, `happ://routing/onadd/{base64}`
/// stores and activates it. Links come from a subscription `routing` header,
/// a `routing: happ://...` body line, a deep link or pasted text.
enum HappRouting {
    static let prefix = "happ://routing/"

    struct Link: Equatable {
        var activate: Bool
        var json: String
    }

    /// First `happ://routing/...` link in a header value or subscription body.
    static func findLink(_ text: String?) -> String? {
        guard let text, !text.isEmpty else { return nil }
        for raw in text.components(separatedBy: .newlines) {
            var line = raw.trimmingCharacters(in: .whitespaces)
            if line.hasPrefix("#") { line = String(line.dropFirst()).trimmingCharacters(in: .whitespaces) }
            if line.lowercased().hasPrefix("routing:") {
                line = String(line.dropFirst("routing:".count)).trimmingCharacters(in: .whitespaces)
            }
            if line.lowercased().hasPrefix(prefix) { return line }
        }
        return nil
    }

    static func parseLink(_ link: String?) -> Link? {
        guard var value = link?.trimmingCharacters(in: .whitespacesAndNewlines) else { return nil }
        if value.lowercased().hasPrefix("rockblack://routing/") {
            value = prefix + value.dropFirst("rockblack://routing/".count)
        }
        guard value.lowercased().hasPrefix(prefix) else { return nil }
        let rest = value.dropFirst(prefix.count)
        guard let slash = rest.firstIndex(of: "/") else { return nil }
        let action = rest[..<slash].lowercased()
        guard action == "add" || action == "onadd" else { return nil }
        var payload = String(rest[rest.index(after: slash)...])
        if let cut = payload.firstIndex(where: { $0 == "?" || $0 == "#" }) { payload = String(payload[..<cut]) }
        payload = payload.removingPercentEncoding ?? payload
        guard let json = LinkImporter.decodeBase64(payload) else { return nil }
        return Link(activate: action == "onadd", json: json)
    }

    /// Happ JSON → profile (unsanitized). Nil when the JSON is not a routing profile.
    static func profile(fromJSON json: String, fallbackName: String = "Imported") -> CustomRouting? {
        guard let object = JSONText.object(json) else { return nil }
        let lists = ["BlockSites", "BlockIp", "ProxySites", "ProxyIp", "DirectSites", "DirectIp"]
        let values = lists.map { stringList(object[$0]) }
        if values.allSatisfy(\.isEmpty) && object["GlobalProxy"] == nil { return nil }
        let rawName = (object["Name"] as? String)?.trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        var profile = CustomRouting(name: rawName.isEmpty ? fallbackName : String(rawName.prefix(60)))
        profile.blockSites = values[0]
        profile.blockIp = values[1]
        profile.proxySites = values[2]
        profile.proxyIp = values[3]
        profile.directSites = values[4]
        profile.directIp = values[5]
        profile.globalProxy = boolValue(object["GlobalProxy"]) ?? true
        if let strategy = object["DomainStrategy"] as? String, CustomRouting.domainStrategies.contains(strategy) {
            profile.domainStrategy = strategy
        }
        return profile
    }

    /// Parsed + sanitized profile from a link, or nil.
    static func profile(fromLink link: String) -> (profile: CustomRouting, activate: Bool)? {
        guard let parsed = parseLink(link), let profile = profile(fromJSON: parsed.json) else { return nil }
        return (profile.sanitized(), parsed.activate)
    }

    private static func stringList(_ any: Any?) -> [String] {
        let raw: [String]
        if let array = any as? [Any] {
            raw = array.compactMap { $0 as? String }
        } else if let text = any as? String {
            raw = text.components(separatedBy: CharacterSet(charactersIn: ",\n"))
        } else {
            raw = []
        }
        var seen = Set<String>()
        return raw.map { $0.trimmingCharacters(in: .whitespaces) }
            .filter { !$0.isEmpty && seen.insert($0).inserted }
    }

    private static func boolValue(_ any: Any?) -> Bool? {
        if let value = any as? Bool { return value }
        if let value = any as? NSNumber { return value.boolValue }
        if let value = any as? String { return value.trimmingCharacters(in: .whitespaces).lowercased() == "true" }
        return nil
    }
}
