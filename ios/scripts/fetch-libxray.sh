#!/usr/bin/env bash
# Installs ios/Frameworks/LibXray.xcframework (iOS device + simulator slices).
#
# Default: the official XTLS/libXray release asset `libxray-apple-cgo.zip`
# (built by libXray's own CI with `python3 build/main.py apple go`), pinned by
# tag and SHA-256. LIBXRAY_FROM_SOURCE=1 builds the same tag from source
# instead (needs Go, Python 3 and the iOS simulator runtime).
set -euo pipefail

TAG="${LIBXRAY_TAG:-v26.10.31}"
SHA256="${LIBXRAY_SHA256:-2666babc1a91d8570a49bc817487338e7a71f17618e02a601e861928d254b95a}"
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$IOS_DIR/Frameworks/LibXray.xcframework"

if [ -f "$DEST/Info.plist" ]; then
  echo "LibXray.xcframework already present"
  exit 0
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

if [ "${LIBXRAY_FROM_SOURCE:-0}" = "1" ]; then
  git clone --depth 1 --branch "$TAG" https://github.com/XTLS/libXray.git "$WORK/libXray"
  (cd "$WORK/libXray" && python3 build/main.py apple go)
  SRC="$(find "$WORK/libXray" -maxdepth 3 -name 'LibXray.xcframework' -type d | head -n 1)"
else
  curl -fsSL --retry 3 -o "$WORK/libxray.zip" \
    "https://github.com/XTLS/libXray/releases/download/$TAG/libxray-apple-cgo.zip"
  echo "$SHA256  $WORK/libxray.zip" | shasum -a 256 -c -
  unzip -q "$WORK/libxray.zip" -d "$WORK"
  SRC="$WORK/libxray-apple-cgo/LibXray.xcframework"
fi
[ -d "$SRC" ] || { echo "::error::LibXray.xcframework not found"; exit 1; }

# Keep only the iOS slices (the release also ships macOS and tvOS).
mkdir -p "$IOS_DIR/Frameworks"
xcodebuild -create-xcframework \
  -library "$SRC/ios-arm64/libXray.a" -headers "$SRC/ios-arm64/Headers" \
  -library "$SRC/ios-arm64_x86_64-simulator/libXray.a" -headers "$SRC/ios-arm64_x86_64-simulator/Headers" \
  -output "$DEST"
echo "$TAG" > "$IOS_DIR/Frameworks/LIBXRAY_VERSION"
ls -la "$DEST"
