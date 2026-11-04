import Foundation

/// Identifiers and shared storage between the app and the packet tunnel extension.
enum AppGroup {
    static let identifier = "group.tech.rockblack.app"
    static let tunnelBundleIdentifier = "tech.rockblack.app.tunnel"

    /// Shared container. Unsigned / simulator builds may run without the
    /// app-group entitlement; then each process falls back to its own storage.
    static var container: URL {
        if let url = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: identifier) {
            return url
        }
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        let url = base.appendingPathComponent("RockBlack", isDirectory: true)
        try? FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)
        return url
    }

    /// Last Xray config started by the app (used when iOS starts the tunnel itself).
    static var tunnelConfigURL: URL { container.appendingPathComponent("tunnel-config.json") }
    /// Xray error log written by the tunnel.
    static var tunnelLogURL: URL { container.appendingPathComponent("tunnel.log") }
    /// Persisted subscriptions and servers.
    static var storeURL: URL { container.appendingPathComponent("store.json") }

    static var defaults: UserDefaults { UserDefaults(suiteName: identifier) ?? .standard }
}

/// Messages exchanged with the tunnel through `sendProviderMessage`.
enum TunnelMessage {
    static let reload = "reload"
    static let stats = "stats"
}
