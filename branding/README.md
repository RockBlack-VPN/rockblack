# RockBlack branding & release conventions

Single source of truth for everything that is renamed from upstream
(v2rayN / v2rayNG) to RockBlack. Keep desktop, Android, CI and the
distribution files in sync with this document.

## Identity

| Item | Value |
|:--|:--|
| Display name | `RockBlack` |
| Slug (files, packages, binaries) | `rockblack` |
| Repository | `https://github.com/RockBlack-VPN/rockblack` (branch `main`) |
| Website | `https://my.rockblack.tech` |
| Bug reports | `https://github.com/RockBlack-VPN/rockblack/issues` |
| Telegram (placeholder) | `https://t.me/rockblack_vpn` |
| Update feed | `https://raw.githubusercontent.com/RockBlack-VPN/rockblack/main/RELEASE.json` |
| Android applicationId | `tech.rockblack.app` (Kotlin namespace stays `com.v2ray.ang`) |
| Primary color (teal) | `#26D6B0` |
| Secondary color (graphite) | `#5F6062` |

## Images (this directory)

| File | Use |
|:--|:--|
| `logo-source.jpg` | Original 460×460 avatar with the wordmark |
| `logo.png` | 1024×1024 round full logo with wordmark, transparent corners (README) |
| `icon.png` | 1024×1024 round app icon (symbol + teal ring), transparent corners |
| `icon-square.png` | 1024×1024 full-bleed graphite square with the symbol (macOS/iOS-style, store) |
| `icon-foreground.png` | 1024×1024 transparent symbol only, safe-zone sized for Android adaptive icon foreground |
| `symbol.png` | Transparent symbol cropped to its bounds (for tray/notification/custom sizes) |

Background for Android adaptive icons: `#5F6062`.

## Versions and tags

Platform versions follow the upstream version they are built from.

| Platform | Version source | Tag | Release title |
|:--|:--|:--|:--|
| Desktop | v2rayN app version | `desktop-vX.Y.Z` | `Desktop vX.Y.Z` |
| Android | v2rayNG `versionName` | `android-vX.Y.Z` | `Android vX.Y.Z` |

The permanent tag `latest` is a generated mirror of the newest asset of each
platform; never upload to it directly.

## Release asset names

Android (`android-vX.Y.Z`):

- `RockBlack.apk` — universal, signed (the asset Obtainium and RELEASE.json use)
- `RockBlack-arm64-v8a.apk`, `RockBlack-armeabi-v7a.apk`, `RockBlack-x86_64.apk`, `RockBlack-x86.apk`
- `SHA256SUMS`

Desktop (`desktop-vX.Y.Z`):

- `rockblack-windows-x64-setup.exe`
- `rockblack-windows-x64-portable.zip`
- `rockblack-windows-arm64-portable.zip`
- `rockblack-macos-arm64.dmg`
- `rockblack-macos-intel.dmg`
- `rockblack-linux-x64.deb`, `rockblack-linux-arm64.deb`
- `rockblack-linux-x64.rpm`, `rockblack-linux-arm64.rpm`
- `rockblack-linux-x64-portable.zip`, `rockblack-linux-arm64-portable.zip`
- `SHA256SUMS`

## Android signing secrets

`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`,
`ANDROID_KEY_PASSWORD`. Always sign with the same production key, otherwise
installed apps cannot be upgraded.
