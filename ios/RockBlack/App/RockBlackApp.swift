import SwiftUI

@main
struct RockBlackApp: App {
    @StateObject private var store = AppStore()
    @AppStorage(SettingsKey.theme, store: AppGroup.defaults) private var theme = AppTheme.dark.rawValue
    @Environment(\.scenePhase) private var scenePhase

    private var colorScheme: ColorScheme? {
        let arguments = ProcessInfo.processInfo.arguments
        if let index = arguments.firstIndex(of: "-rbTheme"), index + 1 < arguments.count {
            return AppTheme(rawValue: arguments[index + 1])?.colorScheme
        }
        return AppTheme(rawValue: theme)?.colorScheme
    }

    var body: some Scene {
        WindowGroup {
            MainView()
                .environmentObject(store)
                .environmentObject(store.vpn)
                .tint(Palette.primary)
                .preferredColorScheme(colorScheme)
                .task { await store.onLaunch() }
                .onOpenURL { url in
                    Task { await store.add(url.absoluteString) }
                }
                .onChange(of: scenePhase) { phase in
                    if phase == .active { Task { await store.refreshStale() } }
                }
        }
    }
}
