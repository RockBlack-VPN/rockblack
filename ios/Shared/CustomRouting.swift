import Foundation

/// A routing profile imported in Happ's JSON format
/// (`happ://routing/add|onadd/{base64}` link, subscription `routing:` header or body line).
struct CustomRouting: Codable, Identifiable, Hashable {
    var id = UUID()
    var name: String
    var blockSites: [String] = []
    var blockIp: [String] = []
    var proxySites: [String] = []
    var proxyIp: [String] = []
    var directSites: [String] = []
    var directIp: [String] = []
    var globalProxy = true
    var domainStrategy: String?
    /// Entries removed because the bundled geo files or Xray-core cannot serve them.
    var skipped: [String] = []

    /// geosite / geoip categories in the tunnel's trimmed geo files
    /// (must match ios/scripts/fetch-geodata.sh).
    static let bundledGeosite: Set<String> = ["category-ru", "category-ads-all"]
    static let bundledGeoip: Set<String> = ["ru", "private"]
    static let domainStrategies: Set<String> = ["AsIs", "IPIfNonMatch", "IPOnDemand"]

    var ruleCount: Int {
        [blockSites, blockIp, proxySites, proxyIp, directSites, directIp].reduce(0) { $0 + $1.count }
    }

    /// Drops entries that would make Xray-core refuse the config, so the tunnel
    /// always starts: unknown geosite/geoip categories, `ext:` files, malformed
    /// IPs/CIDRs, unknown matcher prefixes and invalid regular expressions.
    func sanitized() -> CustomRouting {
        var copy = self
        var skipped: [String] = []
        func domains(_ list: [String]) -> [String] {
            list.filter { entry in
                let ok = Self.isSupportedDomain(entry)
                if !ok { skipped.append(entry) }
                return ok
            }
        }
        func ips(_ list: [String]) -> [String] {
            list.filter { entry in
                let ok = Self.isSupportedIP(entry)
                if !ok { skipped.append(entry) }
                return ok
            }
        }
        copy.blockSites = domains(blockSites)
        copy.proxySites = domains(proxySites)
        copy.directSites = domains(directSites)
        copy.blockIp = ips(blockIp)
        copy.proxyIp = ips(proxyIp)
        copy.directIp = ips(directIp)
        if let strategy = domainStrategy, !Self.domainStrategies.contains(strategy) {
            copy.domainStrategy = nil
        }
        copy.skipped = Array(Set(self.skipped + skipped)).sorted()
        return copy
    }

    static func isSupportedDomain(_ raw: String) -> Bool {
        let entry = raw.trimmingCharacters(in: .whitespaces)
        guard !entry.isEmpty, !entry.contains(" ") else { return false }
        guard let colon = entry.firstIndex(of: ":") else { return true }  // plain substring matcher
        let prefix = entry[..<colon].lowercased()
        let value = String(entry[entry.index(after: colon)...])
        guard !value.isEmpty else { return false }
        switch prefix {
        case "domain", "full", "keyword":
            return !value.contains(":")
        case "regexp":
            return (try? NSRegularExpression(pattern: value)) != nil
        case "geosite":
            // `geosite:name@attr` filters are allowed for bundled categories.
            let name = value.split(separator: "@").first.map { String($0).lowercased() } ?? ""
            return bundledGeosite.contains(name)
        default:
            return false  // ext:, unknown prefixes
        }
    }

    static func isSupportedIP(_ raw: String) -> Bool {
        let entry = raw.trimmingCharacters(in: .whitespaces)
        if entry.lowercased().hasPrefix("geoip:") {
            var name = String(entry.dropFirst(6)).lowercased()
            if name.hasPrefix("!") { name.removeFirst() }
            return bundledGeoip.contains(name)
        }
        let parts = entry.split(separator: "/", omittingEmptySubsequences: false)
        guard parts.count <= 2, let address = parts.first.map(String.init), XrayConfig.isIPAddress(address) else {
            return false
        }
        if parts.count == 2 {
            guard let bits = Int(parts[1]) else { return false }
            return bits >= 0 && bits <= (address.contains(":") ? 128 : 32)
        }
        return true
    }
}
