# RockBlack UI 2.0 — design system

A redesign of the visual layer only. Behaviour, settings, protocols and data
formats stay exactly as upstream. The goal is that RockBlack no longer looks
like v2rayN / v2rayNG at first glance.

## Principles

1. **Connection first.** The main screen is built around one large round
   connect button and a status card. The server list comes second.
2. **Dark graphite by default**, with teal as the only accent. Light theme is
   supported with the same structure.
3. **Cards instead of tables and rows.** Soft rounded surfaces, generous
   spacing, no hard grid lines.
4. **Calm chrome.** Flat surfaces with 1 px outlines instead of shadows; icons
   are outlined and monochrome; teal is used only for state and primary actions.

## Colors

| Token | Dark | Light | Use |
|:--|:--|:--|:--|
| `bg` | `#0F1011` | `#F3F5F6` | window / screen background |
| `surface` | `#18191B` | `#FFFFFF` | cards, sidebar, sheets |
| `surface2` | `#212326` | `#EEF1F2` | inputs, hovered rows, chips |
| `outline` | `#2D3034` | `#DCE1E4` | 1 px borders, dividers |
| `primary` | `#26D6B0` | `#0FA383` | accent, connected state, primary buttons |
| `onPrimary` | `#04231C` | `#FFFFFF` | text/icons on primary |
| `primarySoft` | `#26D6B0` @ 14 % | `#0FA383` @ 12 % | selected row, active nav item |
| `text` | `#F1F3F4` | `#14171A` | primary text |
| `textMuted` | `#8E959C` | `#5E666D` | secondary text, captions |
| `danger` | `#FF5C6C` | `#D93848` | errors, delete, timeout |
| `warning` | `#FFB547` | `#B87400` | slow latency |
| `graphite` | `#5F6062` | `#5F6062` | brand neutral (logo) |

Latency colors: `< 300 ms` primary, `300–800 ms` warning, `> 800 ms / timeout` danger.

## Shape and spacing

- Corner radius: cards 16, buttons and inputs 12, chips and tags fully rounded (pill).
- Spacing scale: 4, 8, 12, 16, 24, 32. Card padding 16 (mobile), 20 (desktop).
- Outlines 1 px `outline`; no drop shadows except the glow of the connect button.

## Typography

System font (Segoe UI Variable / SF / Roboto / Inter). Sizes: title 22 semibold,
section 15 semibold, body 14, caption 12 `textMuted`, numbers (latency, speed)
use tabular figures where available.

## The connect button

- Circle, 148 px desktop / 160 dp Android, centred in the status card.
- Disconnected: `surface2` fill, 3 px `outline` ring, power icon `textMuted`,
  caption "Not connected".
- Connected: `primary` fill, power icon `onPrimary`, soft outer glow
  (`primary` @ 35 %, blur 24), caption "Connected" in `primary`.
- Transition 200 ms. The status card also shows the active server name, protocol
  chip, and live up/down speed.

## Layout

**Desktop:** left sidebar (logo + nav: Servers, Subscriptions, Routing,
Settings, Logs, Help) instead of the top menu bar. The main area has a status
card with the connect button and mode switches (System proxy, TUN, Routing)
at the top, the server list as cards below, and logs in a collapsible bottom panel.

**Android:** transparent top bar with the logo wordmark, a status card with the
connect button instead of the floating action button, and a server list of
rounded cards (selected = `primarySoft` background + 3 dp `primary` accent bar
on the left). Bottom sheets and dialogs use `surface` with radius 24 top
corners.
