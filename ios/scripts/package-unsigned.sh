#!/usr/bin/env bash
# Packs an archive built with CODE_SIGNING_ALLOWED=NO into RockBlack-unsigned.ipa.
#
# The binaries are ad-hoc signed with the entitlements (Network Extension +
# app group) so TrollStore keeps them; for AltStore/Sideloadly/re-sign tools
# the signature is replaced anyway.
#   package-unsigned.sh <path/to/RockBlack.xcarchive> <output dir>
set -euo pipefail
ARCHIVE="$1"
OUT="$2"
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
APP="$ARCHIVE/Products/Applications/RockBlack.app"
[ -d "$APP" ] || { echo "::error::$APP not found"; exit 1; }
APPEX="$APP/PlugIns/RockBlackTunnel.appex"
[ -d "$APPEX" ] || { echo "::error::$APPEX not found"; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
mkdir -p "$STAGE/Payload"
cp -R "$APP" "$STAGE/Payload/"
APP="$STAGE/Payload/RockBlack.app"
APPEX="$APP/PlugIns/RockBlackTunnel.appex"

codesign --force --sign - --timestamp=none \
  --entitlements "$IOS_DIR/Tunnel/RockBlackTunnel.entitlements" "$APPEX"
codesign --force --sign - --timestamp=none \
  --entitlements "$IOS_DIR/RockBlack/RockBlack.entitlements" "$APP"
codesign -d --entitlements - "$APPEX" || true

mkdir -p "$OUT"
(cd "$STAGE" && zip -qry "$OUT/RockBlack-unsigned.ipa" Payload)
ls -la "$OUT"
