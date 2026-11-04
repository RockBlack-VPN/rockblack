# RockBlack for Android

RockBlack VPN client for Android (Xray core).

It is a modified version of [v2rayNG](https://github.com/2dust/v2rayNG) by 2dust,
imported from upstream commit
[`57907cf`](https://github.com/2dust/v2rayNG/commit/57907cf) and distributed
under the [GPL-3.0](LICENSE) license, like the original.

Native components are git submodules, unchanged:

- [`AndroidLibXrayLite`](https://github.com/2dust/AndroidLibXrayLite) — tag `v26.10.31`
  (the CI downloads the matching prebuilt `libv2ray.aar` release);
- [`hev-socks5-tunnel`](https://github.com/heiher/hev-socks5-tunnel) — `2.19.3`
  (built by [`compile-hevtun.sh`](compile-hevtun.sh)).

## Download

Releases are tagged `android-vX.Y.Z` (the version is the upstream
`versionName`): <https://github.com/RockBlack-VPN/rockblack/releases>.

- `RockBlack.apk` — universal APK (recommended, used by Obtainium / RELEASE.json);
- `RockBlack-arm64-v8a.apk`, `RockBlack-armeabi-v7a.apk`, `RockBlack-x86_64.apk`, `RockBlack-x86.apk`;
- `SHA256SUMS`.

All release APKs are signed with the same RockBlack production key.

## Modifications from v2rayNG

- applicationId `tech.rockblack.app` (F-Droid flavor: `tech.rockblack.app.fdroid`);
  the Kotlin namespace/packages stay `com.v2ray.ang`, so the app installs side
  by side with v2rayNG. Shortcuts updated accordingly.
- App name `RockBlack` in all locales; RockBlack launcher, adaptive/monochrome,
  round, notification, TV-banner, drawer and store icons (generated from
  `../branding` by [`tools/generate-icons.py`](tools/generate-icons.py));
  accent color teal `#26D6B0` instead of orange.
- RockBlack UI 3.0 ([`branding/DESIGN.md`](../branding/DESIGN.md)): dark
  graphite theme by default (light theme and "follow system" remain selectable;
  dynamic color is not applied), teal accent. One main screen in the style of
  Happ: settings (opens the drawer with every upstream destination and tools)
  and "+" (upstream import menu) in the top bar, a power button with rings,
  glow and connecting pulse, session timer and live speed, routing and mode
  chips, then one scrolling list of collapsible subscription cards (header with
  update / ping-all / menu, traffic + expiry info bar, panel announcement,
  expiry ≤ 3 days banner, server rows with flag, protocol and colored ping;
  the chevron opens edit / QR / copy / ping / delete). Servers without a
  subscription are in the "Server-List" card. Tapping a row selects it and
  reconnects a running tunnel. Settings → Ping: method (real delay via proxy or
  TCP), display (time or icon), ping on launch, test URL.
- Routing profiles (`RoutingProfileManager`): Russia (default — Russian
  domains/IPs direct via `geosite:category-ru`, `geoip:ru`, `.ru/.su/.рф`, ads
  blocked), Global, Block ads only, Direct, plus the upstream rule editor
  ("Custom") and profiles imported from a subscription
  (`routing: happ://routing/add|onadd/{base64}` header or body line, or a
  `happ://routing/...` link). The China/Iran presets and the AliDNS domestic
  DNS default are gone (domestic DNS `77.88.8.8`). Changing the profile while
  connected reconnects.
- Screenshot tests: `RockBlackScreenshotTest` (Robolectric; PNGs in
  `app/build/outputs/screenshots`, CI artifact `android-screenshots`).
- Links (source code, issues, Telegram, privacy policy, website) point to
  RockBlack; the drawer "Promotion" entry opens <https://my.rockblack.tech>.
  About shows a "Based on v2rayNG" credit linking to upstream.
- In-app update check uses `RockBlack-VPN/rockblack` releases, considering only
  `android-v*` tags and the `RockBlack-<abi>.apk` / `RockBlack.apk` assets.
- Additional deep-link scheme `rockblack://install-config` / `rockblack://install-sub`
  (`v2rayng://` keeps working).
- Gradle outputs `RockBlack-<abi>.apk` / `RockBlack-universal.apk`; optional
  debug signing of release builds for CI without the production key.
- Own [privacy policy](PRIVACY.md); upstream AI-assistant files removed.
- The HTTP User-Agent for subscription downloads stays `v2rayNG/<version>`, so
  subscription panels keep serving the v2rayNG-compatible format.

## Building

CI: [`.github/workflows/android-build.yml`](../.github/workflows/android-build.yml).
Locally (JDK 21, Android SDK + NDK):

```sh
git submodule update --init --recursive
export NDK_HOME=/path/to/ndk
bash android/compile-hevtun.sh && cp -r android/libs android/V2rayNG/app/
# download libv2ray.aar from https://github.com/2dust/AndroidLibXrayLite/releases/tag/v26.10.31
# into android/V2rayNG/app/libs/
cd android/V2rayNG && ./gradlew assemblePlaystoreRelease
```
