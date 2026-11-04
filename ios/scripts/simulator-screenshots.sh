#!/usr/bin/env bash
# Builds RockBlack for the iOS simulator, runs the unit tests and captures
# screenshots of the main screen with demo data (launch argument -demo).
#   simulator-screenshots.sh <output dir>
set -euo pipefail
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$1"
mkdir -p "$OUT"
DERIVED="$RUNNER_TEMP/rockblack-sim"
pretty() { if command -v xcbeautify >/dev/null; then xcbeautify --renderer github-actions; else cat; fi; }

UDID="$(xcrun simctl list devices available --json | python3 -c '
import json, re, sys
data = json.load(sys.stdin)["devices"]
best = None
for runtime, devices in data.items():
    if "iOS" not in runtime:
        continue
    version = tuple(int(x) for x in re.findall(r"\d+", runtime.split("iOS")[-1]))
    for d in devices:
        if d["name"].startswith("iPhone") and "Pro" in d["name"] and "Max" not in d["name"]:
            key = (version, d["name"])
            if best is None or key > best[0]:
                best = (key, d["udid"])
print(best[1] if best else "")
')"
[ -n "$UDID" ] || { echo "::error::No iPhone simulator available"; xcrun simctl list devices available; exit 1; }
echo "Simulator: $UDID"
xcrun simctl boot "$UDID" || true
xcrun simctl bootstatus "$UDID" -b

xcodebuild build-for-testing \
  -project "$IOS_DIR/RockBlack.xcodeproj" -scheme RockBlack -configuration Debug \
  -destination "id=$UDID" -derivedDataPath "$DERIVED" CODE_SIGNING_ALLOWED=NO | pretty

# Unit tests (link import, headers, routing configs validated by Xray-core).
set +e
xcodebuild test-without-building \
  -project "$IOS_DIR/RockBlack.xcodeproj" -scheme RockBlack \
  -destination "id=$UDID" -derivedDataPath "$DERIVED" -resultBundlePath "$OUT/tests.xcresult" | pretty
TEST_STATUS=${PIPESTATUS[0]}
set -e
echo "unit tests exit code: $TEST_STATUS"

APP="$(find "$DERIVED/Build/Products/Debug-iphonesimulator" -maxdepth 1 -name 'RockBlack.app' | head -n 1)"
xcrun simctl install "$UDID" "$APP"
xcrun simctl status_bar "$UDID" override --time "9:41" --batteryState charged --batteryLevel 100 \
  --cellularBars 4 --wifiBars 3 || true

shot() {
  local name="$1"; shift
  xcrun simctl terminate "$UDID" tech.rockblack.app >/dev/null 2>&1 || true
  xcrun simctl launch "$UDID" tech.rockblack.app -demo "$@"
  sleep 6
  xcrun simctl io "$UDID" screenshot "$OUT/$name.png"
  echo "captured $name.png"
}
shot main-dark-connected-en -demoConnected -rbTheme dark -AppleLanguages "(en)" -AppleLocale en_US
shot main-dark-disconnected-ru -rbTheme dark -AppleLanguages "(ru)" -AppleLocale ru_RU
shot main-light-connected-de -demoConnected -rbTheme light -AppleLanguages "(de)" -AppleLocale de_DE
ls -la "$OUT"
exit "$TEST_STATUS"
