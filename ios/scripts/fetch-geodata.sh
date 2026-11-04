#!/usr/bin/env bash
# Builds the small geosite.dat / geoip.dat bundled in the tunnel extension.
#
# The Network Extension has a ~50 MB memory limit, so only the categories the
# routing profiles use are kept (~0.4 MB instead of ~26 MB):
#   geosite: category-ru, category-ads-all   (v2fly/domain-list-community)
#   geoip:   ru, private                     (v2fly/geoip)
set -euo pipefail

GEOSITE_TAG="${GEOSITE_TAG:-20261103223032}"
GEOSITE_SHA256="${GEOSITE_SHA256:-9c1b21ba6aef9d9a077642bf6bed52d736dce68eb46865bc9867f8eb73c4a410}"
GEOIP_TAG="${GEOIP_TAG:-202611032005}"
GEOIP_SHA256="${GEOIP_SHA256:-eedb24b3b9fcba27e06b92c0428dab90d9496083091ae4cf39e11387fd99d0e2}"

IOS_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$IOS_DIR/Tunnel/Geo"
mkdir -p "$OUT"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

curl -fsSL --retry 3 -o "$WORK/dlc.dat" \
  "https://github.com/v2fly/domain-list-community/releases/download/$GEOSITE_TAG/dlc.dat"
curl -fsSL --retry 3 -o "$WORK/geoip.dat" \
  "https://github.com/v2fly/geoip/releases/download/$GEOIP_TAG/geoip.dat"
echo "$GEOSITE_SHA256  $WORK/dlc.dat" | shasum -a 256 -c -
echo "$GEOIP_SHA256  $WORK/geoip.dat" | shasum -a 256 -c -

python3 "$IOS_DIR/scripts/geo_subset.py" "$WORK/dlc.dat" "$OUT/geosite.dat" category-ru category-ads-all
python3 "$IOS_DIR/scripts/geo_subset.py" "$WORK/geoip.dat" "$OUT/geoip.dat" ru private
ls -la "$OUT"
