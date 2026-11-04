import Foundation

/// Downloads a subscription and parses its metadata headers.
enum SubscriptionFetcher {
    struct Result {
        var servers: [Server]
        var info: SubscriptionInfo
        var title: String?
        var updateHours: Int?
    }

    enum FetchError: LocalizedError {
        case http(Int), empty
        var errorDescription: String? {
            switch self {
            case .http(let code): return String(format: L("Server responded with HTTP %lld"), code)
            case .empty: return L("No supported servers found")
            }
        }
    }

    static var userAgent: String {
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "1.0"
        return "RockBlack/\(version) (iOS)"
    }

    static func fetch(_ url: URL) async throws -> Result {
        var request = URLRequest(url: url, cachePolicy: .reloadIgnoringLocalCacheData, timeoutInterval: 25)
        request.setValue(userAgent, forHTTPHeaderField: "User-Agent")
        request.setValue("*/*", forHTTPHeaderField: "Accept")
        let (data, response) = try await URLSession.shared.data(for: request)
        let http = response as? HTTPURLResponse
        if let code = http?.statusCode, !(200..<300).contains(code) { throw FetchError.http(code) }
        let body = String(data: data, encoding: .utf8) ?? ""
        var result = parse(body: body, header: { http?.value(forHTTPHeaderField: $0) })
        if result.title == nil, let disposition = http?.value(forHTTPHeaderField: "Content-Disposition") {
            result.title = filename(fromDisposition: disposition)
        }
        if result.servers.isEmpty { throw FetchError.empty }
        return result
    }

    /// Parses headers (or `#key: value` lines at the top of the body, Happ style).
    static func parse(body: String, header: (String) -> String?) -> Result {
        var meta: [String: String] = [:]
        for line in body.components(separatedBy: .newlines) {
            let trimmed = line.trimmingCharacters(in: .whitespaces)
            guard trimmed.hasPrefix("#"), let colon = trimmed.firstIndex(of: ":") else { continue }
            let key = trimmed[trimmed.index(after: trimmed.startIndex)..<colon]
                .trimmingCharacters(in: .whitespaces).lowercased()
            meta[key] = trimmed[trimmed.index(after: colon)...].trimmingCharacters(in: .whitespaces)
        }
        func value(_ name: String) -> String? {
            let raw = header(name) ?? meta[name]
            guard let raw, !raw.isEmpty else { return nil }
            return decodeMaybeBase64(raw)
        }
        var info = parseUserInfo(value("subscription-userinfo") ?? "")
        info.supportURL = value("support-url")
        info.webPageURL = value("profile-web-page-url")
        info.announce = value("announce")
        let hours = value("profile-update-interval").flatMap { Int($0.trimmingCharacters(in: .whitespaces)) }
        return Result(servers: LinkImporter.servers(from: body), info: info,
                      title: value("profile-title"), updateHours: hours)
    }

    /// `upload=1; download=2; total=3; expire=1700000000`
    static func parseUserInfo(_ text: String) -> SubscriptionInfo {
        var info = SubscriptionInfo()
        for part in text.split(separator: ";") {
            let pair = part.split(separator: "=", maxSplits: 1).map { $0.trimmingCharacters(in: .whitespaces) }
            guard pair.count == 2 else { continue }
            let number = Int64(pair[1]) ?? Int64(Double(pair[1]) ?? 0)
            switch pair[0].lowercased() {
            case "upload": info.upload = number
            case "download": info.download = number
            case "total": info.total = number
            case "expire": info.expire = number > 0 ? Date(timeIntervalSince1970: TimeInterval(number)) : nil
            default: break
            }
        }
        return info
    }

    static func decodeMaybeBase64(_ value: String) -> String {
        guard value.lowercased().hasPrefix("base64:") else { return value }
        return LinkImporter.decodeBase64(String(value.dropFirst(7))) ?? value
    }

    static func filename(fromDisposition value: String) -> String? {
        for part in value.split(separator: ";") {
            let item = part.trimmingCharacters(in: .whitespaces)
            if item.lowercased().hasPrefix("filename*=") {
                let raw = item.dropFirst(10).replacingOccurrences(of: "UTF-8''", with: "", options: .caseInsensitive)
                return raw.removingPercentEncoding
            }
            if item.lowercased().hasPrefix("filename=") {
                return item.dropFirst(9).trimmingCharacters(in: CharacterSet(charactersIn: "\""))
            }
        }
        return nil
    }
}
