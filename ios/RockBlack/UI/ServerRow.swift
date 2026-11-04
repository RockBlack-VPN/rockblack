import SwiftUI

/// One server: flag, name, protocol, ping, chevron (DESIGN.md → row 64 pt).
struct ServerRow: View {
    @EnvironmentObject var store: AppStore
    let server: Server
    let selected: Bool
    var onSelect: () -> Void
    var details: () -> Void

    var body: some View {
        HStack(spacing: 12) {
            FlagView(name: server.name)
            VStack(alignment: .leading, spacing: 2) {
                Text(Flags.strippedName(server.name))
                    .font(.system(size: 16, weight: .semibold))
                    .foregroundColor(Palette.text)
                    .lineLimit(1)
                Text(server.protocolName)
                    .font(.system(size: 13))
                    .foregroundColor(Palette.textMuted)
                    .lineLimit(1)
            }
            Spacer(minLength: 8)
            PingLabel(result: store.pings[server.id])
            Button(action: details) {
                Image(systemName: "chevron.right")
                    .font(.system(size: 14, weight: .semibold))
                    .foregroundColor(Palette.textMuted)
                    .frame(width: 28, height: Metrics.rowHeight)
                    .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
            .accessibilityLabel(Text("Server actions"))
        }
        .padding(.leading, 16)
        .padding(.trailing, 6)
        .frame(height: Metrics.rowHeight)
        .background(selected ? Palette.primarySoft : Color.clear)
        .overlay(alignment: .leading) {
            if selected { Rectangle().fill(Palette.primary).frame(width: 3) }
        }
        .contentShape(Rectangle())
        .onTapGesture(perform: onSelect)
        .onLongPressGesture(perform: details)
        .accessibilityElement(children: .combine)
        .accessibilityAddTraits(selected ? AccessibilityTraits([.isButton, .isSelected]) : AccessibilityTraits.isButton)
    }
}

struct FlagView: View {
    let name: String

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 6, style: .continuous).fill(Palette.surface2)
            if let flag = Flags.flag(for: name) {
                Text(flag).font(.system(size: 30)).minimumScaleFactor(0.5)
            } else {
                Image(systemName: "globe").font(.system(size: 16)).foregroundColor(Palette.textMuted)
            }
        }
        .frame(width: 28, height: 28)
        .clipShape(RoundedRectangle(cornerRadius: 6, style: .continuous))
    }
}

struct PingLabel: View {
    let result: PingResult?
    @AppStorage(SettingsKey.pingDisplay, store: AppGroup.defaults) private var display = PingDisplay.time.rawValue

    var body: some View {
        Group {
            switch result {
            case .none:
                Text("n/a").foregroundColor(Palette.textMuted)
            case .testing:
                ProgressView().controlSize(.small)
            case .delay(let ms):
                if display == PingDisplay.icon.rawValue {
                    Image(systemName: "circle.fill").font(.system(size: 10)).foregroundColor(Palette.pingColor(ms))
                } else {
                    Text("\(ms) ms").foregroundColor(Palette.pingColor(ms))
                }
            case .timeout:
                Text("Timeout").foregroundColor(Palette.danger)
            case .failed:
                Text("n/a").foregroundColor(Palette.danger)
            }
        }
        .font(.system(size: 14, weight: .medium).monospacedDigit())
        .lineLimit(1)
        .fixedSize()
    }
}
