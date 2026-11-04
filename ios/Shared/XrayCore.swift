import Foundation
import LibXray

/// Thin wrapper over libXray's single `CGoInvoke` JSON entry point (API version 3).
enum XrayCore {
    struct Response {
        let success: Bool
        let data: Any?
        let error: String
    }

    static func invoke(_ method: String, payload: [String: Any] = [:]) -> Response {
        let request: [String: Any] = ["apiVersion": 3, "method": method, "payload": payload]
        guard let body = try? JSONSerialization.data(withJSONObject: request),
              let text = String(data: body, encoding: .utf8) else {
            return Response(success: false, data: nil, error: "request encoding failed")
        }
        let raw: String = text.withCString { pointer -> String in
            guard let result = CGoInvoke(UnsafeMutablePointer(mutating: pointer)) else { return "" }
            defer { CGoFree(result) }
            return String(cString: result)
        }
        guard let data = raw.data(using: .utf8),
              let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            return Response(success: false, data: nil, error: "invalid core response")
        }
        return Response(success: object["success"] as? Bool ?? false,
                        data: object["data"],
                        error: object["error"] as? String ?? "")
    }

    static var version: String {
        (invoke("xrayVersion").data as? [String: Any])?["version"] as? String ?? "?"
    }

    /// Share links / Base64 subscription / `{"outbounds": [...]}` → validated outbounds.
    static func convertShareLinks(_ text: String) -> [[String: Any]] {
        let response = invoke("convertShareLinksToXrayJson", payload: ["text": text])
        guard response.success, let data = response.data as? [String: Any] else { return [] }
        return data["outbounds"] as? [[String: Any]] ?? []
    }

    /// Outbound JSON → share link (vless/vmess/trojan/ss/socks/hysteria2).
    static func shareLink(outbound: [String: Any]) -> String? {
        guard let json = JSONText.encode(["outbounds": [outbound]]) else { return nil }
        let response = invoke("convertXrayJsonToShareLinks", payload: ["xrayJson": json])
        guard response.success, let data = response.data as? [String: Any],
              let links = data["links"] as? String else { return nil }
        return links.split(whereSeparator: \.isNewline).first.map(String.init)
    }

    static func freePort() -> Int? {
        let response = invoke("getFreePorts", payload: ["count": 1])
        return ((response.data as? [String: Any])?["ports"] as? [Int])?.first
    }
}

enum JSONText {
    static func encode(_ object: Any, pretty: Bool = false) -> String? {
        guard JSONSerialization.isValidJSONObject(object),
              let data = try? JSONSerialization.data(withJSONObject: object,
                                                     options: pretty ? [.prettyPrinted, .sortedKeys] : [.sortedKeys])
        else { return nil }
        return String(data: data, encoding: .utf8)
    }

    static func object(_ text: String) -> [String: Any]? {
        guard let data = text.data(using: .utf8) else { return nil }
        return (try? JSONSerialization.jsonObject(with: data)) as? [String: Any]
    }
}
