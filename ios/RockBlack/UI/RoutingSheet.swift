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
                        Button {
                            store.setRouting(profile)
                            dismiss()
                        } label: {
                            HStack(alignment: .top, spacing: 12) {
                                Image(systemName: store.routing == profile ? "checkmark.circle.fill" : "circle")
                                    .foregroundColor(store.routing == profile ? Palette.primary : Palette.textMuted)
                                    .font(.system(size: 20))
                                VStack(alignment: .leading, spacing: 3) {
                                    Text(Self.title(profile)).font(.body.weight(.semibold)).foregroundColor(Palette.text)
                                    Text(Self.details(profile)).font(.footnote).foregroundColor(Palette.textMuted)
                                }
                            }
                            .padding(.vertical, 4)
                        }
                        .listRowBackground(Palette.surface)
                    }
                } footer: {
                    Text("Changing the profile while connected reconnects automatically.")
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
}
