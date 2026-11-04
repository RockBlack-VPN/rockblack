import SwiftUI

struct RoutingSheet: View {
    @EnvironmentObject var store: AppStore
    @Environment(\.dismiss) private var dismiss

    static func title(_ profile: RoutingProfile) -> String {
        switch profile {
        case .global: return L("Global")
        case .russia: return L("Russia")
        case .adBlock: return L("Block ads only")
        case .direct: return L("Direct")
        }
    }

    static func details(_ profile: RoutingProfile) -> String {
        switch profile {
        case .global: return L("Everything through the proxy; local network direct.")
        case .russia: return L("Russian sites and IPs direct, everything else through the proxy, ads blocked.")
        case .adBlock: return L("Everything through the proxy, ads blocked.")
        case .direct: return L("No proxy: all traffic goes direct. Useful for testing.")
        }
    }

    var body: some View {
        NavigationStack {
            List {
                Section {
                    ForEach(RoutingProfile.allCases) { profile in
                        row(title: Self.title(profile), details: Self.details(profile),
                            selected: store.customRoutingID == nil && store.routing == profile) {
                            store.setRouting(profile)
                            dismiss()
                        }
                    }
                } footer: {
                    Text("Changing the profile while connected reconnects automatically.")
                }

                if !store.customRoutings.isEmpty {
                    Section {
                        ForEach(store.customRoutings) { custom in
                            row(title: custom.name, details: Self.details(custom),
                                selected: store.customRoutingID == custom.id) {
                                store.setCustomRouting(custom.id)
                                dismiss()
                            }
                            .swipeActions {
                                Button(role: .destructive) { store.deleteCustomRouting(custom.id) } label: {
                                    Label("Delete", systemImage: "trash")
                                }
                            }
                            .contextMenu {
                                Button(role: .destructive) { store.deleteCustomRouting(custom.id) } label: {
                                    Label("Delete", systemImage: "trash")
                                }
                            }
                        }
                    } header: {
                        Text("Imported")
                    } footer: {
                        Text("Profiles from subscriptions or happ://routing links. Swipe left to delete.")
                    }
                }
            }
            .scrollContentBackground(.hidden)
            .background(Palette.background)
            .navigationTitle(Text("Routing"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } }
            }
        }
    }

    static func details(_ custom: CustomRouting) -> String {
        var text = custom.globalProxy ? L("Everything else through the proxy") : L("Everything else direct")
        text += " · " + String(format: L("Rules: %lld"), custom.ruleCount)
        if !custom.skipped.isEmpty {
            text += " · " + String(format: L("Skipped: %lld"), custom.skipped.count)
        }
        return text
    }

    private func row(title: String, details: String, selected: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            HStack(alignment: .top, spacing: 12) {
                Image(systemName: selected ? "checkmark.circle.fill" : "circle")
                    .foregroundColor(selected ? Palette.primary : Palette.textMuted)
                    .font(.system(size: 20))
                VStack(alignment: .leading, spacing: 3) {
                    Text(title).font(.body.weight(.semibold)).foregroundColor(Palette.text)
                    Text(details).font(.footnote).foregroundColor(Palette.textMuted)
                }
            }
            .padding(.vertical, 4)
        }
        .listRowBackground(Palette.surface)
    }
}