#!/usr/bin/env bash
# Signed archive + export → RockBlack.ipa (ad-hoc, development, enterprise or App Store).
#
# Environment (GitHub secrets):
#   IOS_CERT_P12_BASE64, IOS_CERT_PASSWORD      distribution certificate (.p12)
#   IOS_PROVISION_APP_BASE64                    profile for tech.rockblack.app
#   IOS_PROVISION_TUNNEL_BASE64                 profile for tech.rockblack.app.tunnel
#   IOS_TEAM_ID                                 Apple Developer team ID
#   IOS_EXPORT_METHOD (optional)                ad-hoc | app-store-connect | development | enterprise
#                                               (default: derived from the app profile)
#   BUILD_NUMBER, OUT                           CFBundleVersion and output directory
set -euo pipefail
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${OUT:?}"
BUILD_NUMBER="${BUILD_NUMBER:-1}"
WORK="$RUNNER_TEMP/rockblack-signing"
mkdir -p "$WORK" "$OUT"
pretty() { if command -v xcbeautify >/dev/null; then xcbeautify --renderer github-actions; else cat; fi; }

# Keychain with the distribution identity.
KEYCHAIN="$WORK/build.keychain-db"
KEYCHAIN_PASSWORD="$(uuidgen)"
printf '%s' "$IOS_CERT_P12_BASE64" | base64 --decode > "$WORK/cert.p12"
security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN"
security set-keychain-settings -lut 21600 "$KEYCHAIN"
security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN"
security import "$WORK/cert.p12" -k "$KEYCHAIN" -P "$IOS_CERT_PASSWORD" -A -t cert -f pkcs12
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$KEYCHAIN_PASSWORD" "$KEYCHAIN" >/dev/null
security list-keychains -d user -s "$KEYCHAIN" $(security list-keychains -d user | tr -d '"')
IDENTITY="$(security find-identity -v -p codesigning "$KEYCHAIN" | awk 'NR==1 {print $2}')"
[ -n "$IDENTITY" ] || { echo "::error::No signing identity in IOS_CERT_P12_BASE64"; exit 1; }
echo "Signing identity: $(security find-identity -v -p codesigning "$KEYCHAIN" | head -n 1)"

# Provisioning profiles.
install_profile() {
  local data="$1" file="$WORK/$2.mobileprovision"
  printf '%s' "$data" | base64 --decode > "$file"
  security cms -D -i "$file" > "$WORK/$2.plist"
  local uuid
  uuid="$(/usr/libexec/PlistBuddy -c 'Print :UUID' "$WORK/$2.plist")"
  for dir in "$HOME/Library/MobileDevice/Provisioning Profiles" \
             "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"; do
    mkdir -p "$dir"
    cp "$file" "$dir/$uuid.mobileprovision"
  done
  /usr/libexec/PlistBuddy -c 'Print :Name' "$WORK/$2.plist"
}
APP_PROFILE="$(install_profile "$IOS_PROVISION_APP_BASE64" app)"
TUNNEL_PROFILE="$(install_profile "$IOS_PROVISION_TUNNEL_BASE64" tunnel)"
echo "Profiles: app='$APP_PROFILE' tunnel='$TUNNEL_PROFILE'"

METHOD="${IOS_EXPORT_METHOD:-}"
if [ -z "$METHOD" ]; then
  if /usr/libexec/PlistBuddy -c 'Print :ProvisionsAllDevices' "$WORK/app.plist" >/dev/null 2>&1; then
    METHOD=enterprise
  elif /usr/libexec/PlistBuddy -c 'Print :ProvisionedDevices' "$WORK/app.plist" >/dev/null 2>&1; then
    if [ "$(/usr/libexec/PlistBuddy -c 'Print :Entitlements:get-task-allow' "$WORK/app.plist" 2>/dev/null)" = "true" ]; then
      METHOD=development
    else
      METHOD=ad-hoc
    fi
  else
    METHOD=app-store-connect
  fi
fi
echo "Export method: $METHOD"
echo "method=$METHOD" >> "${GITHUB_OUTPUT:-/dev/null}"

ARCHIVE="$WORK/RockBlack.xcarchive"
xcodebuild archive \
  -project "$IOS_DIR/RockBlack.xcodeproj" -scheme RockBlack -configuration Release \
  -destination 'generic/platform=iOS' -archivePath "$ARCHIVE" \
  CURRENT_PROJECT_VERSION="$BUILD_NUMBER" \
  CODE_SIGN_STYLE=Manual DEVELOPMENT_TEAM="$IOS_TEAM_ID" CODE_SIGN_IDENTITY="$IDENTITY" \
  OTHER_CODE_SIGN_FLAGS="--keychain $KEYCHAIN" \
  RB_PROFILE_APP="$APP_PROFILE" RB_PROFILE_TUNNEL="$TUNNEL_PROFILE" \
  | pretty

cat > "$WORK/ExportOptions.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key><string>$METHOD</string>
  <key>teamID</key><string>$IOS_TEAM_ID</string>
  <key>signingStyle</key><string>manual</string>
  <key>signingCertificate</key><string>$IDENTITY</string>
  <key>provisioningProfiles</key>
  <dict>
    <key>tech.rockblack.app</key><string>$APP_PROFILE</string>
    <key>tech.rockblack.app.tunnel</key><string>$TUNNEL_PROFILE</string>
  </dict>
  <key>stripSwiftSymbols</key><true/>
  <key>uploadSymbols</key><false/>
</dict>
</plist>
EOF

xcodebuild -exportArchive -archivePath "$ARCHIVE" -exportPath "$WORK/export" \
  -exportOptionsPlist "$WORK/ExportOptions.plist"
cp "$WORK/export/RockBlack.ipa" "$OUT/RockBlack.ipa"
security delete-keychain "$KEYCHAIN" || true
ls -la "$OUT"
