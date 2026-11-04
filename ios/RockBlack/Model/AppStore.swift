import Foundation
import SwiftUI
import UIKit

/// App state: subscriptions, selection, routing, ping results.
@MainActor
final class AppStore: ObservableObject {
    @Published var subscriptions: [Subscription] = []
    @Published var selectedServerID: UUID?
    @Published var routing: RoutingProfile = .russia
    @Published var pings: [UUID: PingResult] = [:]
    @Published var pingingGroups: Set<UUID> = []
    @Published var refreshingGroups: Set<UUID> = []
    @Published var message: String?

    let vpn = VPNController()
    private var persist = true
    private var testing = false

    init() {
        let arguments = ProcessInfo.processInfo.arguments
        if ProcessInfo.processInfo.environment["XCTestConfigurationFilePath"] != nil {
            persist = false  // unit-test host: no network, no NetworkExtension, no stored data
            testing = true
        } else if arguments.contains("-demo") {
            persist = false
            DemoData.apply(to: self, connected: arguments.contains("-demoConnected"))
        } else {
            load()
        }
    }

    // MARK: - Lifecycle

    func onLaunch() async {
        guard !testing else { return }
        await vpn.load()
        guard persist else { return }
        await refreshStale()
        if Settings.pingOnLaunch {
            for subscription in subscriptions { pingGroup(subscription.id) }
        }
    }

    func refreshStale() async {
        guard persist else { return }
        let now = Date()
        for subscription in subscriptions where !subscription.isManual {
            let age = subscription.updatedAt.map { now.timeIntervalSince($0) } ?? .infinity
            if age > Double(max(subscription.autoUpdateHours, 1)) * 3600 {
                await refresh(subscription.id, quiet: true)
            }
        }
    }

    // MARK: - Queries

    var allServers: [Server] { subscriptions.flatMap(\.servers) }

    var selectedServer: Server? {
        guard let id = selectedServerID else { return nil }
        return allServers.first { $0.id == id }
    }

    func sortedServers(_ subscription: Subscription) -> [Server] {
        switch subscription.sort {
        case .delivered:
            return subscription.servers
        case .name:
            return subscription.servers.sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }
        case .ping:
            return subscription.servers.sorted { pingRank($0.id) < pingRank($1.id) }
        }
    }

    private func pingRank(_ id: UUID) -> Int {
        if case .delay(let ms) = pings[id] { return ms }
        return Int.max
    }

    // MARK: - Import

    /// Adds a subscription URL, or servers from links / JSON / Base64 text.
    @discardableResult
    func add(_ input: String) async -> Bool {
        let text = input.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty else { return false }
        if let url = URL(string: text), ["http", "https"].contains(url.scheme?.lowercased() ?? ""), !text.contains("\n") {
            return await addSubscription(url)
        }
        if text.lowercased().hasPrefix("rockblack://") || text.lowercased().hasPrefix("happ://add/") {
            if let inner = Self.deepLinkPayload(text) { return await add(inner) }
        }
        let servers = LinkImporter.servers(from: text)
        guard !servers.isEmpty else {
            message = L("No supported servers found")
            return false
        }
        var manual = manualGroup()
        manual.servers.append(contentsOf: servers)
        upsert(manual)
        if selectedServerID == nil { selectedServerID = servers.first?.id }
        message = String(format: L("Servers added: %lld"), servers.count)
        save()
        return true
    }

    static func deepLinkPayload(_ text: String) -> String? {
        for prefix in ["rockblack://add/", "rockblack://import/", "happ://add/"] where text.lowercased().hasPrefix(prefix) {
            let rest = String(text.dropFirst(prefix.count))
            return rest.removingPercentEncoding ?? rest
        }
        if let components = URLComponents(string: text),
           let url = components.queryItems?.first(where: { $0.name == "url" })?.value {
            return url
        }
        return nil
    }

    func addSubscription(_ url: URL) async -> Bool {
        if let existing = subscriptions.first(where: { $0.url == url.absoluteString }) {
            await refresh(existing.id)
            return true
        }
        let subscription = Subscription(name: url.host ?? url.absoluteString, url: url.absoluteString)
        subscriptions.insert(subscription, at: subscriptions.firstIndex(where: \.isManual) ?? subscriptions.count)
        let ok = await refresh(subscription.id)
        if !ok, subscriptions.first(where: { $0.id == subscription.id })?.servers.isEmpty == true {
            subscriptions.removeAll { $0.id == subscription.id }
        }
        save()
        return ok
    }

    @discardableResult
    func refresh(_ id: UUID, quiet: Bool = false) async -> Bool {
        guard let subscription = subscriptions.first(where: { $0.id == id }),
              let urlText = subscription.url, let url = URL(string: urlText),
              !refreshingGroups.contains(id) else { return false }
        refreshingGroups.insert(id)
        defer { refreshingGroups.remove(id) }
        do {
            let result = try await SubscriptionFetcher.fetch(url)
            guard var current = subscriptions.first(where: { $0.id == id }) else { return false }
            let selectedName = selectedServer.flatMap { server in
                current.servers.contains(server) ? server.name : nil
            }
            // Keep ids (and thus ping results / selection) for unchanged servers.
            var previous = Dictionary(current.servers.map { ($0.outbound, $0.id) }, uniquingKeysWith: { first, _ in first })
            current.servers = result.servers.map { server in
                var copy = server
                if let id = previous.removeValue(forKey: server.outbound) { copy.id = id }
                return copy
            }
            current.info = result.info
            current.updatedAt = Date()
            if let title = result.title, !title.isEmpty { current.name = title }
            if let hours = result.updateHours, hours > 0 { current.autoUpdateHours = hours }
            upsert(current)
            if let selectedName, selectedServer == nil,
               let match = current.servers.first(where: { $0.name == selectedName }) {
                selectedServerID = match.id
            }
            if selectedServerID == nil { selectedServerID = current.servers.first?.id }
            save()
            return true
        } catch {
            if !quiet { message = error.localizedDescription }
            return false
        }
    }

    // MARK: - Editing

    func manualGroup() -> Subscription {
        subscriptions.first(where: \.isManual) ?? Subscription(name: L("Server list"), url: nil)
    }

    func upsert(_ subscription: Subscription) {
        if let index = subscriptions.firstIndex(where: { $0.id == subscription.id }) {
            subscriptions[index] = subscription
        } else {
            subscriptions.append(subscription)
        }
    }

    func update(_ id: UUID, _ change: (inout Subscription) -> Void) {
        guard let index = subscriptions.firstIndex(where: { $0.id == id }) else { return }
        change(&subscriptions[index])
        save()
    }

    func deleteSubscription(_ id: UUID) {
        let removed = subscriptions.first { $0.id == id }?.servers.map(\.id) ?? []
        subscriptions.removeAll { $0.id == id }
        if let selected = selectedServerID, removed.contains(selected) { selectedServerID = allServers.first?.id }
        save()
    }

    func updateServer(_ server: Server) {
        for index in subscriptions.indices {
            if let row = subscriptions[index].servers.firstIndex(where: { $0.id == server.id }) {
                subscriptions[index].servers[row] = server
            }
        }
        save()
        if server.id == selectedServerID { Task { await applyIfConnected() } }
    }

    func deleteServer(_ id: UUID) {
        for index in subscriptions.indices {
            subscriptions[index].servers.removeAll { $0.id == id }
        }
        subscriptions.removeAll { $0.isManual && $0.servers.isEmpty }
        if selectedServerID == id { selectedServerID = allServers.first?.id }
        pings[id] = nil
        save()
    }

    // MARK: - Selection / routing / connection

    func select(_ server: Server) {
        guard selectedServerID != server.id else { return }
        selectedServerID = server.id
        save()
        Task { await applyIfConnected() }
    }

    func setRouting(_ profile: RoutingProfile) {
        guard routing != profile else { return }
        routing = profile
        save()
        Task { await applyIfConnected() }
    }

    func selectFastest(in id: UUID) {
        guard let subscription = subscriptions.first(where: { $0.id == id }) else { return }
        let best = subscription.servers.compactMap { server -> (Server, Int)? in
            if case .delay(let ms) = pings[server.id] { return (server, ms) }
            return nil
        }.min { $0.1 < $1.1 }
        if let best { select(best.0) } else { message = L("Ping the servers first") }
    }

    func currentConfig() -> String? {
        let outbound = selectedServer?.outboundObject
        if outbound == nil && routing != .direct { return nil }
        return JSONText.encode(XrayConfig.build(outbound: outbound, profile: routing))
    }

    func toggleConnection() async {
        switch vpn.state {
        case .connected, .connecting:
            vpn.disconnect()
        case .disconnected, .disconnecting:
            guard let config = currentConfig() else {
                message = L("Add a server first")
                return
            }
            await vpn.connect(config: config)
            if let error = vpn.lastError { message = error }
        }
    }

    private func applyIfConnected() async {
        guard vpn.isActive, let config = currentConfig() else { return }
        await vpn.reload(config: config)
    }

    // MARK: - Ping

    func pingGroup(_ id: UUID) {
        guard let subscription = subscriptions.first(where: { $0.id == id }), !pingingGroups.contains(id) else { return }
        ping(subscription.servers, group: id)
    }

    func ping(_ servers: [Server], group: UUID? = nil) {
        guard !servers.isEmpty else { return }
        if let group { pingingGroups.insert(group) }
        for server in servers { pings[server.id] = .testing }
        let method = Settings.pingMethod
        let url = Settings.testURL
        Task {
            await PingService.run(servers, method: method, url: url) { [weak self] id, result in
                self?.pings[id] = result
            }
            if let group { pingingGroups.remove(group) }
        }
    }

    // MARK: - Persistence

    func load() {
        guard let data = try? Data(contentsOf: AppGroup.storeURL),
              let snapshot = try? JSONDecoder().decode(StoreSnapshot.self, from: data) else { return }
        subscriptions = snapshot.subscriptions
        selectedServerID = snapshot.selectedServerID
        routing = snapshot.routing
    }

    func save() {
        guard persist else { return }
        let snapshot = StoreSnapshot(subscriptions: subscriptions, selectedServerID: selectedServerID, routing: routing)
        if let data = try? JSONEncoder().encode(snapshot) {
            try? data.write(to: AppGroup.storeURL, options: .atomic)
        }
    }

    // MARK: - Share

    func shareLink(for server: Server) -> String? {
        if let link = server.link { return link }
        return server.outboundObject.flatMap(XrayCore.shareLink)
    }

    func copy(_ text: String) {
        UIPasteboard.general.string = text
        message = L("Copied")
    }
}
