# Notice of modifications

RockBlack is a modified version of the following GPL-3.0 programs. As required by
section 5(a) of the GNU General Public License v3, this file states that the
work has been modified and lists the changes.

| Component | Upstream | Imported commit | Imported on | License |
|:--|:--|:--|:--|:--|
| `desktop/` | [2dust/v2rayN](https://github.com/2dust/v2rayN) | [`281f5ee`](https://github.com/2dust/v2rayN/commit/281f5ee3c7109651f21508f17bd544bee81baa03) (v7.25.6) | 2026-11-03 | GPL-3.0 |
| `android/` | [2dust/v2rayNG](https://github.com/2dust/v2rayNG) | [`57907cf`](https://github.com/2dust/v2rayNG/commit/57907cf354d9769200ed4717b4430d605abe0952) (v2.3.15) | 2026-11-03 | GPL-3.0 |

Submodules are used unmodified at the pinned commits:
[2dust/GlobalHotKeys](https://github.com/2dust/GlobalHotKeys),
[2dust/AndroidLibXrayLite](https://github.com/2dust/AndroidLibXrayLite) (`v26.10.31`),
[heiher/hev-socks5-tunnel](https://github.com/heiher/hev-socks5-tunnel) (`2.19.3`).
Bundled cores (Xray-core, sing-box, mihomo, geo files) are downloaded unmodified
from their official releases at build time.

## Changes (2026-11-03, RockBlack-VPN)

- Product renamed to **RockBlack**; new application icons and logo (see `branding/`).
- Android `applicationId` changed to `tech.rockblack.app`; APK names `RockBlack*.apk`.
- Desktop packages, installers and bundles renamed to `rockblack-*`.
- Links in the UI (home page, issues, update checks) point to
  `https://github.com/RockBlack-VPN/rockblack` and `https://my.rockblack.tech`;
  upstream projects remain credited in the About screens.
- Monorepo CI: GitHub Actions workflows build all platforms and publish
  `desktop-vX.Y.Z` / `android-vX.Y.Z` releases plus a permanent `latest` mirror
  and the `RELEASE.json` update feed.

## Changes (2026-11-04, RockBlack-VPN) — UI 3.0

- New Happ-style main screen on Android, Windows (WPF) and macOS/Linux (Avalonia):
  power button, subscription cards with traffic/expiry info, per-subscription ping,
  one-tap server switching with automatic reconnect, routing profile selector.
- Built-in routing and DNS defaults changed from China-oriented to Russia-oriented
  presets (Global, Russia, Block ads, Direct); Happ routing profile import.
- Subscription metadata headers (`subscription-userinfo`, `profile-title`,
  `support-url`, `profile-web-page-url`, `announce`, `routing`) are parsed.
- Localizations reduced to English, Russian and German; German translation added;
  all other upstream translations removed.
- New `ios/` client (RockBlack's own code, GPL-3.0) using
  [XTLS/libXray](https://github.com/XTLS/libXray) unmodified.

The complete history of changes is the git history of this repository: the first
commit is the unmodified upstream import, every later commit is a RockBlack change.
Detailed per-platform notes: [desktop/README.md](desktop/README.md),
[android/README.md](android/README.md).
