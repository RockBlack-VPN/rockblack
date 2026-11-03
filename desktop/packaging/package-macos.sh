#!/usr/bin/env bash
# Build RockBlack.app and a DMG from a published application directory
# (dotnet publish output of v2rayN.Desktop + AmazTool, cores already bundled in bin/).
#
# usage: package-macos.sh <version> <app_dir> <out_dmg>
set -euo pipefail

VERSION="${1:?version required}"
APP_DIR="$(cd "${2:?app dir required}" && pwd)"
OUT_DMG="${3:?output dmg required}"

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ICNS="$SCRIPT_DIR/../v2rayN/v2rayN.Desktop/v2rayN.icns"

[[ -f "$APP_DIR/RockBlack" ]] || { echo "RockBlack executable not found in $APP_DIR" >&2; exit 1; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

stage="$work/dmg"
app="$stage/RockBlack.app"
mkdir -p "$app/Contents/Resources"
cp -R "$APP_DIR" "$app/Contents/MacOS"
cp -f "$ICNS" "$app/Contents/Resources/AppIcon.icns"
# The bundle is read-only once installed: keep configs in the user's data dir.
echo "When this file exists, app will not store configs under this folder" > "$app/Contents/MacOS/NotStoreConfigHere.txt"
chmod +x "$app/Contents/MacOS/RockBlack"
[[ -f "$app/Contents/MacOS/AmazTool" ]] && chmod +x "$app/Contents/MacOS/AmazTool"
for core in xray/xray sing_box/sing-box mihomo/mihomo; do
  [[ -f "$app/Contents/MacOS/bin/$core" ]] && chmod +x "$app/Contents/MacOS/bin/$core"
done

cat > "$app/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleLocalizations</key>
  <array>
    <string>en</string>
    <string>ru</string>
    <string>zh-Hans</string>
    <string>zh-Hant</string>
    <string>fa</string>
    <string>fr</string>
    <string>hu</string>
  </array>
  <key>CFBundleDisplayName</key>
  <string>RockBlack</string>
  <key>CFBundleExecutable</key>
  <string>RockBlack</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>CFBundleIconName</key>
  <string>AppIcon</string>
  <key>CFBundleIdentifier</key>
  <string>tech.rockblack.desktop</string>
  <key>CFBundleName</key>
  <string>RockBlack</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>CSResourcesFileMapped</key>
  <true/>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>NSHumanReadableCopyright</key>
  <string>RockBlack — based on v2rayN by 2dust (GPL-3.0)</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.6</string>
</dict>
</plist>
EOF

# Not signed/notarized (same as upstream v2rayN): the .NET apphost and the Go cores are
# already linker-signed (ad-hoc), users open the app via right click > Open the first time.

ln -s /Applications "$stage/Applications"
rm -f "$OUT_DMG"
hdiutil create -volname "RockBlack" -srcfolder "$stage" -ov -format UDZO "$OUT_DMG"
ls -la "$OUT_DMG"
