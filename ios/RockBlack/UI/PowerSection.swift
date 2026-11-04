import SwiftUI

/// Power button with two soft rings (DESIGN.md → The power button).
struct PowerButton: View {
    let state: ConnectionState
    let action: () -> Void
    @State private var pulse = false

    private var connected: Bool { state == .connected }
    private var busy: Bool { state == .connecting || state == .disconnecting }

    var body: some View {
        let diameter = Metrics.powerDiameter
        ZStack {
            Circle()
                .stroke(ringColor(outer: true), lineWidth: 1)
                .frame(width: diameter + 56, height: diameter + 56)
            Circle()
                .stroke(ringColor(outer: false), lineWidth: 1)
                .frame(width: diameter + 28, height: diameter + 28)
            Button(action: action) {
                ZStack {
                    Circle()
                        .fill(connected ? Palette.primary : Palette.surface2)
                        .shadow(color: connected ? Palette.primary.opacity(0.35) : .clear, radius: 32)
                    Image(systemName: "power")
                        .font(.system(size: 56, weight: .medium))
                        .foregroundColor(connected ? Palette.onPrimary : Palette.textMuted)
                }
                .frame(width: diameter, height: diameter)
                .contentShape(Circle())
            }
            .buttonStyle(PressableStyle())
            .accessibilityLabel(Text(connected ? LocalizedStringKey("Disconnect") : LocalizedStringKey("Connect")))
        }
        .frame(width: diameter + 60, height: diameter + 60)
        .animation(.easeInOut(duration: 0.2), value: state)
        .onAppear { updatePulse() }
        .onChange(of: state) { _ in updatePulse() }
    }

    private func updatePulse() {
        if busy {
            pulse = false
            withAnimation(.easeInOut(duration: 0.9).repeatForever(autoreverses: true)) { pulse = true }
        } else {
            withAnimation(.easeInOut(duration: 0.2)) { pulse = false }
        }
    }

    private func ringColor(outer: Bool) -> Color {
        if busy {
            return Palette.primary.opacity(pulse ? 0.6 : 0.2)
        }
        if connected {
            return Palette.primary.opacity(outer ? 0.12 : 0.25)
        }
        return Palette.outline
    }
}

/// Button, state caption, session timer and live speeds.
struct PowerSection: View {
    @EnvironmentObject var store: AppStore
    @EnvironmentObject var vpn: VPNController

    var body: some View {
        VStack(spacing: 8) {
            PowerButton(state: vpn.state) {
                Task { await store.toggleConnection() }
            }
            Text(caption)
                .font(.subheadline.weight(.semibold))
                .textCase(.uppercase)
                .kerning(1.2)
                .foregroundColor(vpn.state == .connected ? Palette.primary : Palette.textMuted)
                .padding(.top, 4)
            if vpn.state == .connected, let since = vpn.connectedDate {
                TimelineView(.periodic(from: .now, by: 1)) { context in
                    Text(Format.timer(context.date.timeIntervalSince(since)))
                        .font(.system(size: 22, weight: .semibold, design: .rounded).monospacedDigit())
                        .foregroundColor(Palette.text)
                }
                HStack(spacing: 18) {
                    Label(Format.rate(vpn.downloadRate), systemImage: "arrow.down")
                    Label(Format.rate(vpn.uploadRate), systemImage: "arrow.up")
                }
                .font(.footnote.monospacedDigit())
                .foregroundColor(Palette.textMuted)
            }
        }
        .frame(maxWidth: .infinity)
    }

    private var caption: LocalizedStringKey {
        switch vpn.state {
        case .disconnected: return "Not connected"
        case .connecting: return "Connecting…"
        case .connected: return "Connected"
        case .disconnecting: return "Disconnecting…"
        }
    }
}
