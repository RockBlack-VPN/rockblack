import SwiftUI

struct SettingsView: View {
    @EnvironmentObject var store: AppStore
    @Environment(\.dismiss) private var dismiss
    @AppStorage(SettingsKey.pingMethod, store: AppGroup.defaults) private var pingMethod = PingMethod.proxy.rawValue
    @AppStorage(SettingsKey.pingDisplay, store: AppGroup.defaults) private var pingDisplay = PingDisplay.time.rawValue
    @AppStorage(SettingsKey.pingOnLaunch, store: AppGroup.defaults) private var pingOnLaunch = false
    @AppStorage(SettingsKey.testURL, store: AppGroup.defaults) private var testURL = SettingsKey.defaultTestURL
    @AppStorage(SettingsKey.theme, store: AppGroup.defaults) private var theme = AppTheme.dark.rawValue
    @State private var xrayVersion = ""

    private var appVersion: String {
        let info = Bundle.main.infoDictionary
        let version = info?["CFBundleShortVersionString"] as? String ?? "?"
        let build = info?["CFBundleVersion"] as? String ?? "?"
        return "\(version) (\(build))"
    }

    private var languageName: String {
        let code = Bundle.main.preferredLocalizations.first ?? "en"
        return Locale(identifier: code).localizedString(forLanguageCode: code)?.capitalized ?? code
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    Picker(selection: $pingMethod) {
                        Text("Via proxy (real delay)").tag(PingMethod.proxy.rawValue)
                        Text("TCP").tag(PingMethod.tcp.rawValue)
                    } label: { Text("Method") }
                    Picker(selection: $pingDisplay) {
                        Text("Time").tag(PingDisplay.time.rawValue)
                        Text("Status icon").tag(PingDisplay.icon.rawValue)
                    } label: { Text("Display") }
                    Toggle(isOn: $pingOnLaunch) { Text("Ping on launch") }
                    VStack(alignment: .leading, spacing: 4) {
                        Text("Test URL").font(.footnote).foregroundColor(Palette.textMuted)
                        TextField(SettingsKey.defaultTestURL, text: $testURL)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                            .keyboardType(.URL)
                    }
                } header: {
                    Text("Ping")
                } footer: {
                    Text("Via proxy measures a real request through each server. TCP only measures the connection to the server.")
                }
                .listRowBackground(Palette.surface)

                Section {
                    Picker(selection: $theme) {
                        Text("Dark").tag(AppTheme.dark.rawValue)
                        Text("Light").tag(AppTheme.light.rawValue)
                        Text("System").tag(AppTheme.system.rawValue)
                    } label: { Text("Theme") }
                    HStack {
                        Text("Language")
                        Spacer()
                        Text(languageName).foregroundColor(Palette.textMuted)
                    }
                    Button {
                        if let url = URL(string: UIApplication.openSettingsURLString) { UIApplication.shared.open(url) }
                    } label: {
                        Text("Change language in iOS Settings")
                    }
                } header: {
                    Text("Appearance")
                } footer: {
                    Text("Available languages: English, Русский, Deutsch.")
                }
                .listRowBackground(Palette.surface)

                Section {
                    NavigationLink { LogView() } label: { Text("Tunnel log") }
                } header: {
                    Text("VPN")
                } footer: {
                    Text("Connect on demand is off: the VPN starts only when you tap the power button.")
                }
                .listRowBackground(Palette.surface)

                Section {
                    LabeledContent(L("Version"), value: appVersion)
                    LabeledContent(L("Xray core"), value: xrayVersion)
                    Link(destination: URL(string: "https://my.rockblack.tech")!) { Text("Website") }
                    Link(destination: URL(string: "https://t.me/rockblack_vpn")!) { Text("Telegram") }
                    Link(destination: URL(string: "https://github.com/RockBlack-VPN/rockblack/issues")!) {
                        Text("Report a bug")
                    }
                } header: {
                    Text("About")
                }
                .listRowBackground(Palette.surface)
            }
            .scrollContentBackground(.hidden)
            .background(Palette.background)
            .navigationTitle(Text("Settings"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } }
            }
            .task {
                if xrayVersion.isEmpty { xrayVersion = XrayCore.version }
            }
        }
        .preferredColorScheme(AppTheme(rawValue: theme)?.colorScheme)
    }
}

struct LogView: View {
    @State private var text = ""

    var body: some View {
        ScrollView {
            Text(text.isEmpty ? L("The log is empty.") : text)
                .font(.system(.caption, design: .monospaced))
                .textSelection(.enabled)
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding()
        }
        .background(Palette.background)
        .navigationTitle(Text("Tunnel log"))
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                ShareLink(item: text) { Image(systemName: "square.and.arrow.up") }
            }
        }
        .task { load() }
        .refreshable { load() }
    }

    private func load() {
        let raw = (try? String(contentsOf: AppGroup.tunnelLogURL, encoding: .utf8)) ?? ""
        text = String(raw.suffix(60_000))
    }
}
