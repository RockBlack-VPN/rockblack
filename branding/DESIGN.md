# RockBlack UI 3.0 — design system

UI 3.0 follows the interaction model of **Happ** (one screen: power button on
top, subscriptions as collapsible cards with servers below) while keeping the
RockBlack brand palette (graphite + teal). Every client — Android, iOS,
Windows (WPF), macOS / Linux (Avalonia) — implements the same structure.

Languages: **English (default), Russian, German** only. No other locale files,
language options, store metadata or user-visible text in other languages.

## Main screen (all platforms)

```
┌──────────────────────────────────────┐
│ ⚙                RockBlack        +  │  top bar: settings (left), add (right)
│                                      │
│              ╭────────╮              │
│           ╭──│   ⏻    │──╮           │  power button with 2 soft rings
│           │  ╰────────╯  │           │
│              CONNECTED               │  state caption
│               00:16:28               │  session timer (connected only)
│        ↓ 1.2 MB/s   ↑ 140 KB/s       │  live speed (connected only)
│                                      │
│  [ Routing: Russia ▾ ]  [ TUN ◉ ]    │  chips: routing profile, mode (desktop: Proxy/TUN)
│                                      │
│ ╭──────────────────────────────────╮ │
│ │ ˄ My subscription      ⟳  ◔  ⋯   │ │  group header: collapse, refresh, ping all, menu
│ │   09.11.2026 14:06 · auto 12 h   │ │  updated time · auto-update interval
│ ├──────────────────────────────────┤ │
│ │ ⓘ [███ 3.1 GB / 50 GB ]  Exp 16.10│ │  info bar: traffic progress + expiry + support link
│ ├──────────────────────────────────┤ │
│ │▌🇩🇪 Germany              42 ms  › │ │  selected row: 3 px primary bar on the left
│ │   VLESS · Reality                │ │
│ │ 🇳🇱 Netherlands          n/a   › │ │
│ │   VLESS                          │ │
│ ╰──────────────────────────────────╯ │
│ ╭──────────────────────────────────╮ │
│ │ ˅ Server-List (manual)    ◔  ⋯   │ │  servers without subscription
│ ╰──────────────────────────────────╯ │
└──────────────────────────────────────┘
```

Desktop uses the same column, centred (max width ~560 px) in a resizable
window, or a two-pane layout on wide windows (power/status pane left,
subscription cards right). Advanced upstream windows stay reachable from the
settings menu.

### Behaviour

- **Server switching:** tapping a row selects it. If the tunnel is running, the
  client reconnects to the new server automatically (no extra prompt). The
  chevron `›` / long-press / right-click opens the server actions
  (edit, share/QR, ping, copy link, delete).
- **Ping:**
  - Speedometer `◔` in a group header pings all servers of that group in
    parallel; results appear in place as they arrive (a small spinner while
    running).
  - Single-server ping from the row actions.
  - Settings → Ping: *method* = `Via proxy (GET)` (default, real delay through
    the core), `Via proxy (HEAD)` where supported, `TCP`; *display* = time
    (`42 ms`) or status icon; *ping on launch* toggle; test URL.
  - Colours: `< 300 ms` primary, `300–800 ms` warning, `> 800 ms` danger,
    failure shows `n/a` (never tested) or `Timeout` in danger.
  - Group menu: sort by ping / by name / as delivered; “Select fastest”.
- **Routing:** the routing chip opens a sheet with profiles:
  1. **Global** — everything through the proxy (LAN/private direct).
  2. **Russia** *(default)* — Russian sites and IPs direct: `geosite:category-ru`,
     `geoip:ru`, `domain:ru`, `domain:su`, `domain:xn--p1ai` (.рф), private
     ranges direct; ads (`geosite:category-ads-all`) blocked.
  3. **Block ads only** — global proxy + ad block.
  4. **Direct** — no proxy (core runs, everything direct), useful for testing.
  5. Custom profiles (the upstream routing editor stays available) and
     profiles imported from a subscription (`routing: happ://routing/add/{base64}`
     header/body line, Happ JSON format) where implemented.
  Changing the profile while connected reconnects automatically.
  No China-specific presets (`geosite:cn`, `geoip:cn`, Chinese DNS such as
  `223.5.5.5`/`119.29.29.29`/`alidns`/`dnspod`) remain as defaults; domestic DNS
  defaults are `77.88.8.8` (Yandex) / `1.1.1.1`, remote DNS `1.1.1.1` / DoH
  Cloudflare.
- **Subscription info bar:** from the `subscription-userinfo` header
  (`upload; download; total; expire`), `profile-title`, `support-url`
  (Telegram icon when the link is `t.me`), `profile-web-page-url`, `announce`.
  `total = 0` shows `∞`. Expiry ≤ 3 days shows a danger banner
  “Your subscription expires in N days” / “Subscription has expired”.
- **Flags:** a leading flag emoji in the server name is shown as the row icon;
  otherwise derive it from a country name/ISO code in the name; otherwise a
  neutral globe icon.

## Colors

| Token | Dark (default) | Light | Use |
|:--|:--|:--|:--|
| `bgTop` → `bgBottom` | `#0E1A1B` → `#0B0C0D` | `#EAF6F3` → `#F5F7F8` | vertical background gradient |
| `surface` | `#17191B` @ 92 % | `#FFFFFF` | subscription cards, sheets |
| `surface2` | `#202326` | `#EEF1F2` | group header, info bar, chips, inputs |
| `outline` | `#2B2F33` | `#DCE1E4` | 1 px dividers between rows |
| `primary` | `#26D6B0` | `#0FA383` | accent, connected state, selected bar |
| `onPrimary` | `#04231C` | `#FFFFFF` | icon/text on primary |
| `primarySoft` | `#26D6B0` @ 14 % | `#0FA383` @ 12 % | selected row background |
| `text` | `#F1F3F4` | `#14171A` | titles |
| `textMuted` | `#8E959C` | `#5E666D` | subtitles (protocol), captions, ping text |
| `danger` | `#FF5C6C` | `#D93848` | timeout, expiry banner, delete |
| `warning` | `#FFB547` | `#B87400` | slow latency |

## The power button

- Diameter 168 (mobile, dp/pt) / 150 (desktop px); two concentric rings at
  +28 and +56 diameter, 1 px, `primary` @ 25 % / 12 %.
- Disconnected: `surface2` fill, power icon `textMuted`, rings `outline`;
  caption “Not connected”.
- Connecting: rings pulse (opacity 0.2 ↔ 0.6, 900 ms), caption “Connecting…”.
- Connected: `primary` fill, power icon `onPrimary`, outer glow
  (`primary` @ 35 %, blur 32), rings `primary`; caption “Connected” and
  timer `HH:MM:SS`.
- Transition 200 ms.

## Shape, spacing, type

- Corner radius: cards 18, buttons/inputs 12, chips pill; info bar progress pill.
- Row height 64 (title 16 semibold + subtitle 13 `textMuted`), flag 28 rounded 6.
- Spacing scale 4 / 8 / 12 / 16 / 24 / 32; card margin 16.
- System font; numbers (ping, speed, timer) tabular figures.
