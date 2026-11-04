import SwiftUI

struct MainView: View {
    @EnvironmentObject var store: AppStore
    @EnvironmentObject var vpn: VPNController
    @State private var showSettings = false
    @State private var showAdd = false
    @State private var showRouting = false
    @State private var detailServer: Server?
    @State private var renaming: Subscription?
    @State private var renameText = ""

    var body: some View {
        ZStack(alignment: .bottom) {
            Palette.background
            ScrollView {
                VStack(spacing: Metrics.margin) {
                    topBar
                    PowerSection()
                        .padding(.top, 8)
                    chips
                    if store.subscriptions.isEmpty {
                        emptyState
                    }
                    ForEach(store.subscriptions) { subscription in
                        SubscriptionCard(subscription: subscription,
                                         onServerDetails: { detailServer = $0 },
                                         onRename: { renameText = $0.name; renaming = $0 })
                    }
                }
                .padding(.horizontal, Metrics.margin)
                .padding(.bottom, 32)
                .frame(maxWidth: 600)
                .frame(maxWidth: .infinity)
            }
            .refreshable { await store.refreshStale() }
            if let message = store.message {
                Toast(text: message)
                    .padding(.bottom, 24)
                    .transition(.move(edge: .bottom).combined(with: .opacity))
                    .task(id: message) {
                        try? await Task.sleep(nanoseconds: 2_500_000_000)
                        withAnimation { store.message = nil }
                    }
            }
        }
        .animation(.easeInOut(duration: 0.2), value: store.message)
        .sheet(isPresented: $showSettings) { SettingsView().environmentObject(store).environmentObject(vpn) }
        .sheet(isPresented: $showAdd) { AddSheet().environmentObject(store) }
        .sheet(isPresented: $showRouting) { RoutingSheet().environmentObject(store).presentationDetents([.medium, .large]) }
        .sheet(item: $detailServer) { ServerDetailView(server: $0).environmentObject(store) }
        .alert(Text("Rename"), isPresented: Binding(get: { renaming != nil }, set: { if !$0 { renaming = nil } })) {
            TextField(L("Name"), text: $renameText)
            Button("Save") {
                if let id = renaming?.id, !renameText.isEmpty { store.update(id) { $0.name = renameText } }
                renaming = nil
            }
            Button("Cancel", role: .cancel) { renaming = nil }
        }
    }

    private var topBar: some View {
        HStack {
            Button { showSettings = true } label: {
                Image(systemName: "gearshape.fill").font(.system(size: 22)).foregroundColor(Palette.text)
                    .frame(width: 44, height: 44)
            }
            .accessibilityLabel(Text("Settings"))
            Spacer()
            HStack(spacing: 8) {
                Image("Symbol").resizable().scaledToFit().frame(height: 22)
                Text("RockBlack").font(.system(size: 18, weight: .bold)).foregroundColor(Palette.text)
            }
            Spacer()
            Button { showAdd = true } label: {
                Image(systemName: "plus").font(.system(size: 24, weight: .medium)).foregroundColor(Palette.text)
                    .frame(width: 44, height: 44)
            }
            .accessibilityLabel(Text("Add"))
        }
    }

    private var chips: some View {
        HStack(spacing: 8) {
            Button { showRouting = true } label: {
                HStack(spacing: 6) {
                    Image(systemName: "arrow.triangle.branch")
                    Text(String(format: L("Routing: %@"), store.routingTitle)).lineLimit(1)
                    Image(systemName: "chevron.down").font(.system(size: 11, weight: .bold))
                }
                .font(.subheadline.weight(.medium))
                .foregroundColor(Palette.text)
                .padding(.horizontal, 14)
                .padding(.vertical, 8)
                .background(Capsule().fill(Palette.surface2))
                .overlay(Capsule().stroke(Palette.outline, lineWidth: 1))
            }
            .buttonStyle(PressableStyle())
        }
    }

    private var emptyState: some View {
        VStack(spacing: 12) {
            Image(systemName: "tray").font(.system(size: 30)).foregroundColor(Palette.textMuted)
            Text("No servers yet").font(.headline).foregroundColor(Palette.text)
            Text("Add a subscription URL, a share link or scan a QR code.")
                .font(.subheadline).foregroundColor(Palette.textMuted).multilineTextAlignment(.center)
            Button { showAdd = true } label: {
                Text("Add subscription")
                    .font(.headline)
                    .foregroundColor(Palette.onPrimary)
                    .padding(.horizontal, 24).padding(.vertical, 12)
                    .background(RoundedRectangle(cornerRadius: Metrics.controlRadius).fill(Palette.primary))
            }
        }
        .padding(24)
        .frame(maxWidth: .infinity)
        .background(RoundedRectangle(cornerRadius: Metrics.cardRadius).fill(Palette.surface))
    }
}

struct Toast: View {
    let text: String
    var body: some View {
        Text(text)
            .font(.subheadline.weight(.medium))
            .foregroundColor(Palette.text)
            .padding(.horizontal, 16).padding(.vertical, 10)
            .background(Capsule().fill(Palette.surface2))
            .overlay(Capsule().stroke(Palette.outline, lineWidth: 1))
            .shadow(color: .black.opacity(0.25), radius: 12, y: 4)
    }
}
