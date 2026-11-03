# RockBlack Desktop

RockBlack VPN client for Windows, macOS and Linux, built on
[Xray-core](https://github.com/XTLS/Xray-core) (sing-box and mihomo are bundled as well).

It is a rebranded build of **[v2rayN](https://github.com/2dust/v2rayN)** by 2dust,
imported from upstream commit
[`281f5ee`](https://github.com/2dust/v2rayN/commit/281f5ee) (v2rayN 7.25.6), and is
distributed under the same license, **GPL-3.0** (see [LICENSE](LICENSE)).
The desktop version always equals the upstream v2rayN version it is built from
(`<Version>` in [`v2rayN/Directory.Build.props`](v2rayN/Directory.Build.props)); releases are
tagged `desktop-vX.Y.Z` in [RockBlack-VPN/rockblack](https://github.com/RockBlack-VPN/rockblack/releases).

| Asset | Platform |
|:--|:--|
| `rockblack-windows-x64-setup.exe` | Windows x64 installer (per-user, no admin rights) |
| `rockblack-windows-x64-portable.zip`, `rockblack-windows-arm64-portable.zip` | Windows portable |
| `rockblack-macos-arm64.dmg`, `rockblack-macos-intel.dmg` | macOS (unsigned: right click > Open on first start) |
| `rockblack-linux-{x64,arm64}.deb`, `rockblack-linux-{x64,arm64}.rpm` | Linux packages (`/opt/rockblack`, command `rockblack`) |
| `rockblack-linux-{x64,arm64}-portable.zip` | Linux portable |
| `SHA256SUMS` | checksums |

Built by [`.github/workflows/desktop-build.yml`](../.github/workflows/desktop-build.yml)
(run it manually with `release = true` to publish `desktop-v<version>`).

## Modifications to v2rayN

- **Identity**: product name RockBlack in window titles, tray tooltip, message boxes,
  assembly metadata (Product/Company/Title) and all localized strings that showed "v2rayN"
  (az, fa, fr, hu, id, ru, zh-Hans, zh-Hant, en; AmazTool updater too).
- **Executable**: `RockBlack.exe` (WPF, Windows) / `RockBlack` (Avalonia, Linux/macOS) via
  `AssemblyName`; C# namespaces, project names and root namespaces are unchanged (`v2rayN.*`).
  Avalonia asset URIs moved to `avares://RockBlack/`; the updater (AmazTool) restarts `RockBlack`.
- **Icons**: application, window and tray icons (`.ico`, `.png`, `.icns`, upstream file names kept)
  regenerated from `branding/icon.png` by [`scripts/generate-icons.py`](scripts/generate-icons.py);
  the four tray states keep upstream semantics with ring colors teal / red / purple / yellow.
- **Self-update** of the app checks `RockBlack-VPN/rockblack` releases, newest `desktop-v*` tag
  (the repository also hosts Android releases), and downloads
  `rockblack-{windows,linux}-{x64,arm64}-portable.zip`. macOS DMG and Linux deb/rpm installs are
  not self-updated (the update dialog points to the releases page). Core and geo-file updates
  (Xray-core, sing-box, mihomo, rule sets) still come from their upstream projects.
- **Links**: Help menu shows the RockBlack website (https://my.rockblack.tech), GitHub, issues and
  Telegram, plus "Based on v2rayN by 2dust (GPL-3.0)" linking to upstream; the upstream
  "Promotion" button opens the RockBlack website instead of the upstream ad link.
- **Subscriptions**: the default subscription User-Agent stays `v2rayN/<version>` so panels keep
  returning the v2rayN subscription format.
- **Separation from an installed v2rayN**: own autostart entry name, Linux single-instance mutex,
  user data folder (`RockBlack`) and Linux install prefix `/opt/rockblack`.
- **Packaging**: upstream CI and `package-*.sh` were replaced by
  [`packaging/`](packaging) (`fetch-cores.sh` bundles the upstream
  [v2rayN-core-bin](https://github.com/2dust/v2rayN-core-bin) cores like upstream packages do,
  `package-linux.sh` for deb/rpm/zip, `package-macos.sh` for `RockBlack.app`
  (`tech.rockblack.desktop`) + DMG, `rockblack.iss` Inno Setup installer).
  Upstream workflows are kept for reference in `upstream-ci/desktop/`.

The `v2rayN.WebAPI` project is not built or shipped and was left as in upstream.
`v2rayN/GlobalHotKeys` is the upstream git submodule, unmodified.
