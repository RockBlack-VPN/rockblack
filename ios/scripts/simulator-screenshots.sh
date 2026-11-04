#!/usr/bin/env bash
# Simulator helpers for CI: unit tests and screenshots of the main screen with
# demo data (launch argument -demo).
#   simulator-screenshots.sh boot            pick + boot an iPhone simulator (exports SIM_UDID)
#   simulator-screenshots.sh build           build app + tests for the simulator
#   simulator-screenshots.sh test <out>      run the unit tests (xcresult in <out>)
#   simulator-screenshots.sh shots <out>     install the app and capture PNGs into <out>
set -euo pipefail
IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DERIVED="${RUNNER_TEMP:-/tmp}/rockblack-sim"
pretty() { if command -v xcbeautify >/dev/null; then xcbeautify --renderer github-actions; else cat; fi; }

case "${1:-}" in
  boot)
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
    xcrun simctl list devices | grep "$UDID"
    xcrun simctl boot "$UDID" || true
    xcrun simctl bootstatus "$UDID" -b
    echo "SIM_UDID=$UDID" >> "${GITHUB_ENV:-/dev/null}"
    echo "Simulator: $UDID"
    ;;
  build)
    xcodebuild build-for-testing \
      -project "$IOS_DIR/RockBlack.xcodeproj" -scheme RockBlack -configuration Debug \
      -destination "id=${SIM_UDID:?}" -derivedDataPath "$DERIVED" CODE_SIGNING_ALLOWED=NO | pretty
    ;;
  test)
    OUT="$2"; mkdir -p "$OUT"
    xcodebuild test-without-building \
      -project "$IOS_DIR/RockBlack.xcodeproj" -scheme RockBlack \
      -destination "id=${SIM_UDID:?}" -derivedDataPath "$DERIVED" \
      -resultBundlePath "$OUT/tests.xcresult" -test-timeouts-enabled YES -maximum-test-execution-time-allowance 120 \
      | pretty
    ;;
  shots)
    OUT="$2"; mkdir -p "$OUT"
    APP="$(find "$DERIVED/Build/Products/Debug-iphonesimulator" -maxdepth 1 -name 'RockBlack.app' | head -n 1)"
    [ -d "$APP" ] || { echo "::error::simulator app not built"; exit 1; }
    xcrun simctl install "${SIM_UDID:?}" "$APP"
    xcrun simctl status_bar "$SIM_UDID" override --time "9:41" --batteryState charged --batteryLevel 100 \
      --cellularBars 4 --wifiBars 3 || true
    shot() {
      local name="$1"; shift
      xcrun simctl terminate "$SIM_UDID" tech.rockblack.app >/dev/null 2>&1 || true
      xcrun simctl launch "$SIM_UDID" tech.rockblack.app -demo "$@"
      sleep 6
      xcrun simctl io "$SIM_UDID" screenshot "$OUT/$name.png"
      echo "captured $name.png"
    }
    shot main-dark-connected-en -demoConnected -rbTheme dark -AppleLanguages "(en)" -AppleLocale en_US
    shot main-dark-disconnected-ru -rbTheme dark -AppleLanguages "(ru)" -AppleLocale ru_RU
    shot main-light-connected-de -demoConnected -rbTheme light -AppleLanguages "(de)" -AppleLocale de_DE
    ls -la "$OUT"
    ;;
  *)
    echo "usage: $0 boot|build|test <out>|shots <out>"; exit 2
    ;;
esac
