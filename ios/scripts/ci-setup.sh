#!/usr/bin/env bash
# Prepares ios/ for xcodebuild: libXray, geo data, XcodeGen project.
set -euo pipefail
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
XCODEGEN_VERSION="${XCODEGEN_VERSION:-2.46.0}"

xcodebuild -version
bash "$IOS_DIR/scripts/fetch-libxray.sh"
bash "$IOS_DIR/scripts/fetch-geodata.sh"

if ! command -v xcodegen >/dev/null; then
  TOOLS="${RUNNER_TEMP:-/tmp}/xcodegen-$XCODEGEN_VERSION"
  if [ ! -x "$TOOLS/xcodegen/bin/xcodegen" ]; then
    mkdir -p "$TOOLS"
    if curl -fsSL --retry 3 -o "$TOOLS/xcodegen.zip" \
        "https://github.com/yonaskolb/XcodeGen/releases/download/$XCODEGEN_VERSION/xcodegen.zip"; then
      unzip -q -o "$TOOLS/xcodegen.zip" -d "$TOOLS"
    fi
  fi
  if [ -x "$TOOLS/xcodegen/bin/xcodegen" ]; then
    export PATH="$TOOLS/xcodegen/bin:$PATH"
    [ -n "${GITHUB_PATH:-}" ] && echo "$TOOLS/xcodegen/bin" >> "$GITHUB_PATH"
  else
    brew install xcodegen
  fi
fi
xcodegen --version
(cd "$IOS_DIR" && xcodegen generate --spec project.yml)
