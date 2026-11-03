#!/usr/bin/env bash
# Download the upstream core bundle (Xray-core, sing-box, mihomo, geo files, rule sets)
# from 2dust/v2rayN-core-bin — the same bundle upstream v2rayN ships in its packages —
# and copy its bin/ folder into the given application directory.
#
# usage: fetch-cores.sh <bundle> <app_dir>
#   bundle: windows-64 | windows-arm64 | linux-64 | linux-arm64 | macos-64 | macos-arm64
set -euo pipefail

BUNDLE="${1:?bundle name required}"
APP_DIR="${2:?application directory required}"
URL="https://raw.githubusercontent.com/2dust/v2rayN-core-bin/refs/heads/master/v2rayN-${BUNDLE}.zip"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

echo "[cores] downloading $URL"
curl -fL --retry 5 --retry-delay 5 -o "$tmp/cores.zip" "$URL"

mkdir -p "$tmp/x"
if command -v unzip >/dev/null 2>&1; then
  unzip -q "$tmp/cores.zip" -d "$tmp/x"
else
  7z x -y -o"$tmp/x" "$tmp/cores.zip" >/dev/null
fi

bin_dir=""
if [[ -d "$tmp/x/bin" ]]; then
  bin_dir="$tmp/x/bin"
else
  bin_dir="$(find "$tmp/x" -mindepth 2 -maxdepth 2 -type d -name bin | head -n1 || true)"
fi
[[ -n "$bin_dir" && -d "$bin_dir" ]] || { echo "[cores] bin/ not found in bundle" >&2; exit 1; }

mkdir -p "$APP_DIR/bin"
cp -R "$bin_dir/." "$APP_DIR/bin/"

# Core binaries must be executable (packages install them root-owned).
for core in xray/xray sing_box/sing-box mihomo/mihomo; do
  [[ -f "$APP_DIR/bin/$core" ]] && chmod 0755 "$APP_DIR/bin/$core"
done

[[ -f "$APP_DIR/bin/xray/xray" || -f "$APP_DIR/bin/xray/xray.exe" ]] || { echo "[cores] Xray-core missing from bundle" >&2; exit 1; }
echo "[cores] bundled into $APP_DIR/bin:"
ls -la "$APP_DIR/bin"
