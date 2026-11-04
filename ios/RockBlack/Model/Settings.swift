import SwiftUI

enum PingMethod: String, CaseIterable, Identifiable {
    case proxy, tcp
    var id: String { rawValue }
}

enum PingDisplay: String, CaseIterable, Identifiable {
    case time, icon
    var id: String { rawValue }
}

enum AppTheme: String, CaseIterable, Identifiable {
    case dark, light, system
    var id: String { rawValue }

    var colorScheme: ColorScheme? {
        switch self {
        case .dark: return .dark
        case .light: return .light
        case .system: return nil
        }
    }
}

/// Keys for @AppStorage (stored in the app-group defaults).
enum SettingsKey {
    static let pingMethod = "pingMethod"
    static let pingDisplay = "pingDisplay"
    static let pingOnLaunch = "pingOnLaunch"
    static let testURL = "testURL"
    static let theme = "theme"
    static let defaultTestURL = "https://www.gstatic.com/generate_204"
}

enum Settings {
    static var pingMethod: PingMethod {
        PingMethod(rawValue: AppGroup.defaults.string(forKey: SettingsKey.pingMethod) ?? "") ?? .proxy
    }
    static var testURL: String {
        let value = AppGroup.defaults.string(forKey: SettingsKey.testURL) ?? ""
        return value.isEmpty ? SettingsKey.defaultTestURL : value
    }
    static var pingOnLaunch: Bool { AppGroup.defaults.bool(forKey: SettingsKey.pingOnLaunch) }
}

/// NSLocalizedString shortcut for strings built outside of SwiftUI `Text`.
func L(_ key: String) -> String {
    NSLocalizedString(key, comment: "")
}
