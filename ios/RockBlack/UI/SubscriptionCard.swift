import SwiftUI

/// Collapsible subscription / server-list card (DESIGN.md → Main screen).
struct SubscriptionCard: View {
    @EnvironmentObject var store: AppStore
    let subscription: Subscription
    var onServerDetails: (Server) -> Void
    var onRename: (Subscription) -> Void

    var body: some View {
        VStack(spacing: 0) {
            header
            if !subscription.collapsed {
                if let info = subscription.info {
                    ExpiryBanner(info: info)
                    InfoBar(info: info)
                    if let announce = info.announce, !announce.isEmpty {
                        Text(announce)
                            .font(.caption)
                            .foregroundColor(Palette.textMuted)
                            .multilineTextAlignment(.center)
                            .frame(maxWidth: .infinity)
                            .padding(.horizontal, 12)
                            .padding(.vertical, 6)
                            .background(Palette.surface2.opacity(0.6))
                    }
                }
                let servers = store.sortedServers(subscription)
                ForEach(Array(servers.enumerated()), id: \.element.id) { index, server in
                    if index > 0 { Rectangle().fill(Palette.outline).frame(height: 1) }
                    ServerRow(server: server, selected: store.selectedServerID == server.id) {
                        store.select(server)
                    } details: {
                        onServerDetails(server)
                    }
                }
                if servers.isEmpty {
                    Text("No servers")
                        .font(.footnote)
                        .foregroundColor(Palette.textMuted)
                        .frame(maxWidth: .infinity, minHeight: Metrics.rowHeight)
                }
            }
        }
        .background(Palette.surface)
        .clipShape(RoundedRectangle(cornerRadius: Metrics.cardRadius, style: .continuous))
        .overlay(RoundedRectangle(cornerRadius: Metrics.cardRadius, style: .continuous)
            .stroke(Palette.outline.opacity(0.6), lineWidth: 1))
    }

    private var header: some View {
        HStack(spacing: 10) {
            Button {
                withAnimation(.easeInOut(duration: 0.2)) {
                    store.update(subscription.id) { $0.collapsed.toggle() }
                }
            } label: {
                HStack(spacing: 10) {
                    Image(systemName: subscription.collapsed ? "chevron.down" : "chevron.up")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundColor(Palette.textMuted)
                        .frame(width: 20)
                    VStack(alignment: .leading, spacing: 2) {
                        Text(subscription.isManual ? L("Server list") : subscription.name)
                            .font(.system(size: 17, weight: .semibold))
                            .foregroundColor(Palette.text)
                            .lineLimit(1)
                        if let subtitle { Text(subtitle).font(.caption).foregroundColor(Palette.textMuted).lineLimit(1) }
                    }
                    Spacer(minLength: 4)
                }
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)

            if !subscription.isManual {
                iconButton("arrow.triangle.2.circlepath", busy: store.refreshingGroups.contains(subscription.id)) {
                    Task { await store.refresh(subscription.id) }
                }
                .accessibilityLabel(Text("Update"))
            }
            iconButton("speedometer", busy: store.pingingGroups.contains(subscription.id)) {
                store.pingGroup(subscription.id)
            }
            .accessibilityLabel(Text("Ping all"))
            menu
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 10)
        .background(Palette.surface2)
    }

    private var subtitle: String? {
        guard !subscription.isManual else { return nil }
        guard let updated = subscription.updatedAt else { return nil }
        return Format.dateTime(updated) + " · " + String(format: L("auto %lld h"), subscription.autoUpdateHours)
    }

    private func iconButton(_ symbol: String, busy: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            ZStack {
                if busy {
                    ProgressView().controlSize(.small).tint(Palette.primary)
                } else {
                    Image(systemName: symbol)
                        .font(.system(size: 17, weight: .medium))
                        .foregroundColor(Palette.text)
                }
            }
            .frame(width: 34, height: 34)
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .disabled(busy)
    }

    private var menu: some View {
        Menu {
            Section {
                sortButton(.ping, "Sort by ping")
                sortButton(.name, "Sort by name")
                sortButton(.delivered, "As delivered")
            }
            Button { store.selectFastest(in: subscription.id) } label: { Label("Select fastest", systemImage: "bolt") }
            if !subscription.isManual {
                Button { onRename(subscription) } label: { Label("Rename", systemImage: "pencil") }
                if let url = subscription.url {
                    Button { store.copy(url) } label: { Label("Copy URL", systemImage: "doc.on.doc") }
                }
            }
            Button(role: .destructive) { store.deleteSubscription(subscription.id) } label: {
                Label("Delete", systemImage: "trash")
            }
        } label: {
            Image(systemName: "ellipsis")
                .font(.system(size: 17, weight: .semibold))
                .foregroundColor(Palette.text)
                .frame(width: 34, height: 34)
                .contentShape(Rectangle())
        }
    }

    private func sortButton(_ sort: ServerSort, _ title: LocalizedStringKey) -> some View {
        Button {
            store.update(subscription.id) { $0.sort = sort }
        } label: {
            if subscription.sort == sort { Label(title, systemImage: "checkmark") } else { Text(title) }
        }
    }
}

/// Traffic progress, expiry date and support link.
struct InfoBar: View {
    let info: SubscriptionInfo
    @State private var showDetails = false
    @Environment(\.openURL) private var openURL

    var body: some View {
        HStack(spacing: 10) {
            Button { showDetails = true } label: {
                Image(systemName: "info.circle").foregroundColor(Palette.primary)
            }
            .buttonStyle(.plain)
            if info.hasTraffic {
                trafficBar
            } else {
                Spacer()
            }
            if let expire = info.expire {
                Text(String(format: L("Exp %@"), Format.date(expire)))
                    .font(.caption.weight(.medium).monospacedDigit())
                    .foregroundColor(Palette.text)
                    .lineLimit(1)
                    .fixedSize()
            }
            if let support = info.supportURL, let url = URL(string: support) {
                Link(destination: url) {
                    Image(systemName: (url.host ?? "").contains("t.me") || support.hasPrefix("tg:") ? "paperplane.fill" : "link")
                        .foregroundColor(Palette.primary)
                }
            }
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
        .background(Palette.surface2.opacity(0.7))
        .overlay(Rectangle().fill(Palette.outline).frame(height: 1), alignment: .top)
        .alert(Text("Subscription"), isPresented: $showDetails) {
            if let page = info.webPageURL, let url = URL(string: page) {
                Button("Open website") { openURL(url) }
            }
            Button("OK", role: .cancel) {}
        } message: {
            Text(detailsText)
        }
    }

    private var totalText: String { info.total > 0 ? Format.bytes(info.total) : "∞" }

    private var trafficBar: some View {
        GeometryReader { proxy in
            ZStack(alignment: .leading) {
                Capsule().fill(Palette.bgBottom.opacity(0.6))
                Capsule().stroke(Palette.outline, lineWidth: 1)
                if info.total > 0 {
                    Capsule()
                        .fill(Palette.primary.opacity(0.45))
                        .frame(width: max(proxy.size.width * min(Double(info.used) / Double(info.total), 1), 8))
                }
                Text("\(Format.bytes(info.used)) / \(totalText)")
                    .font(.caption2.weight(.semibold).monospacedDigit())
                    .foregroundColor(Palette.text)
                    .frame(maxWidth: .infinity)
            }
        }
        .frame(height: 20)
    }

    private var detailsText: String {
        var lines = [String(format: L("Used: %@ of %@"), Format.bytes(info.used), totalText)]
        if let expire = info.expire { lines.append(String(format: L("Expires: %@"), Format.date(expire))) }
        if let announce = info.announce, !announce.isEmpty { lines.append(announce) }
        return lines.joined(separator: "\n")
    }
}

/// Danger banner when the subscription expires within 3 days.
struct ExpiryBanner: View {
    let info: SubscriptionInfo

    var body: some View {
        if let days = info.daysLeft(), days <= 3 {
            VStack(spacing: 6) {
                Text(days < 0 ? L("Subscription has expired") : String.localizedStringWithFormat(L("Your subscription expires in %lld days"), days))
                    .font(.subheadline.weight(.semibold))
                    .multilineTextAlignment(.center)
                if let page = info.webPageURL, let url = URL(string: page) {
                    Link(destination: url) {
                        Text("Renew")
                            .font(.subheadline.weight(.bold))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 8)
                            .background(Color.white.opacity(0.18))
                            .clipShape(RoundedRectangle(cornerRadius: Metrics.controlRadius))
                    }
                }
            }
            .foregroundColor(.white)
            .padding(12)
            .frame(maxWidth: .infinity)
            .background(Palette.danger)
        }
    }
}
