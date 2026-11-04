"""Cut Xray geosite/geoip .dat files down to the categories RockBlack iOS uses.

The iOS Network Extension has a ~50 MB memory limit, so the tunnel ships only
the lists its routing profiles reference instead of the full upstream files.

Both files are a protobuf `GeoSiteList` / `GeoIPList`: a repeated field 1 of
length-delimited entries whose own field 1 is the (upper-case) country code.
Kept entries are copied byte-for-byte, so no protobuf library is needed.

    python3 geo_subset.py dlc.dat geosite.dat category-ru category-ads-all
    python3 geo_subset.py geoip.dat geoip.dat ru private
"""
import sys
from pathlib import Path


def read_varint(data, pos):
    result = shift = 0
    while True:
        if pos >= len(data):
            raise ValueError("truncated varint")
        byte = data[pos]
        pos += 1
        result |= (byte & 0x7F) << shift
        if not byte & 0x80:
            return result, pos
        shift += 7


def entries(data):
    """Yield (code, raw_bytes) for every top-level entry."""
    pos = 0
    while pos < len(data):
        start = pos
        key, pos = read_varint(data, pos)
        if key & 7 != 2:
            raise ValueError("unexpected wire type in list")
        length, pos = read_varint(data, pos)
        body = data[pos:pos + length]
        if len(body) != length:
            raise ValueError("truncated entry")
        pos += length
        yield entry_code(body), data[start:pos]


def entry_code(body):
    pos = 0
    while pos < len(body):
        key, pos = read_varint(body, pos)
        wire = key & 7
        if wire == 2:
            length, pos = read_varint(body, pos)
            if key >> 3 == 1:
                return body[pos:pos + length].decode("utf-8").lower()
            pos += length
        elif wire == 0:
            _, pos = read_varint(body, pos)
        elif wire == 1:
            pos += 8
        elif wire == 5:
            pos += 4
        else:
            raise ValueError("unsupported wire type")
    return ""


def subset(data, codes):
    wanted = {c.lower() for c in codes}
    kept, found = [], set()
    for code, raw in entries(data):
        if code in wanted:
            kept.append(raw)
            found.add(code)
    missing = wanted - found
    if missing:
        raise ValueError(f"categories not found: {sorted(missing)}")
    return b"".join(kept)


def main(argv):
    if len(argv) < 4:
        print(__doc__)
        return 2
    source, target, codes = Path(argv[1]), Path(argv[2]), argv[3:]
    out = subset(source.read_bytes(), codes)
    target.write_bytes(out)
    print(f"{target}: {len(out)} bytes ({', '.join(codes)})")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
