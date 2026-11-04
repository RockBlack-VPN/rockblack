import SwiftUI
import UIKit

/// RockBlack UI 3.0 palette (branding/DESIGN.md → Colors).
enum Palette {
    static let bgTop = dynamic(0x0E1A1B, 0xEAF6F3)
    static let bgBottom = dynamic(0x0B0C0D, 0xF5F7F8)
    static let surface = dynamic(0x17191B, 0xFFFFFF, darkAlpha: 0.92)
    static let surface2 = dynamic(0x202326, 0xEEF1F2)
    static let outline = dynamic(0x2B2F33, 0xDCE1E4)
    static let primary = dynamic(0x26D6B0, 0x0FA383)
    static let onPrimary = dynamic(0x04231C, 0xFFFFFF)
    static let primarySoft = dynamic(0x26D6B0, 0x0FA383, darkAlpha: 0.14, lightAlpha: 0.12)
    static let text = dynamic(0xF1F3F4, 0x14171A)
    static let textMuted = dynamic(0x8E959C, 0x5E666D)
    static let danger = dynamic(0xFF5C6C, 0xD93848)
    static let warning = dynamic(0xFFB547, 0xB87400)

    static func dynamic(_ dark: UInt32, _ light: UInt32, darkAlpha: CGFloat = 1, lightAlpha: CGFloat = 1) -> Color {
        Color(UIColor { traits in
            traits.userInterfaceStyle == .dark
                ? UIColor(rgb: dark, alpha: darkAlpha)
                : UIColor(rgb: light, alpha: lightAlpha)
        })
    }

    static func pingColor(_ ms: Int) -> Color {
        ms < 300 ? primary : (ms <= 800 ? warning : danger)
    }

    static var background: some View {
        LinearGradient(colors: [bgTop, bgBottom], startPoint: .top, endPoint: .bottom).ignoresSafeArea()
    }
}

enum Metrics {
    static let cardRadius: CGFloat = 18
    static let controlRadius: CGFloat = 12
    static let rowHeight: CGFloat = 64
    static let margin: CGFloat = 16
    static let powerDiameter: CGFloat = 168
}

extension UIColor {
    convenience init(rgb: UInt32, alpha: CGFloat = 1) {
        self.init(red: CGFloat((rgb >> 16) & 0xFF) / 255,
                  green: CGFloat((rgb >> 8) & 0xFF) / 255,
                  blue: CGFloat(rgb & 0xFF) / 255,
                  alpha: alpha)
    }
}

enum Format {
    static func bytes(_ value: Int64) -> String {
        let formatter = ByteCountFormatter()
        formatter.countStyle = .binary
        formatter.allowedUnits = [.useKB, .useMB, .useGB, .useTB]
        formatter.allowsNonnumericFormatting = false
        return formatter.string(fromByteCount: value)
    }

    static func rate(_ bytesPerSecond: Double) -> String {
        bytes(Int64(bytesPerSecond)) + "/s"
    }

    static func timer(_ interval: TimeInterval) -> String {
        let total = max(Int(interval), 0)
        return String(format: "%02d:%02d:%02d", total / 3600, (total / 60) % 60, total % 60)
    }

    static func date(_ date: Date) -> String {
        date.formatted(.dateTime.day(.twoDigits).month(.twoDigits).year())
    }

    static func dateTime(_ date: Date) -> String {
        date.formatted(.dateTime.day(.twoDigits).month(.twoDigits).year().hour(.twoDigits(amPM: .omitted)).minute(.twoDigits))
    }
}

struct PressableStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .opacity(configuration.isPressed ? 0.6 : 1)
    }
}
