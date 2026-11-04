import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("geo_subset", Path(__file__).parents[1] / "ios/scripts/geo_subset.py")
g = importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)


def varint(n):
    out = bytearray()
    while True:
        byte = n & 0x7F
        n >>= 7
        out.append(byte | (0x80 if n else 0))
        if not n:
            return bytes(out)


def field(number, payload):
    return varint(number << 3 | 2) + varint(len(payload)) + payload


def entry(code, *domains):
    body = field(1, code.encode())
    for domain in domains:
        # Domain { type = 1 (varint); value = 2 (string) }
        body += field(2, varint(1 << 3) + varint(2) + field(2, domain.encode()))
    return field(1, body)


class GeoSubsetTest(unittest.TestCase):
    def test_keeps_requested_entries_byte_for_byte(self):
        ru, ads, cn = entry("CATEGORY-RU", "yandex.ru"), entry("CATEGORY-ADS-ALL", "ads.example"), entry("CN", "x.cn")
        data = ru + cn + ads
        self.assertEqual(ru + ads, g.subset(data, ["category-ru", "category-ads-all"]))
        self.assertEqual(["category-ru", "cn", "category-ads-all"], [code for code, _ in g.entries(data)])

    def test_missing_category_is_an_error(self):
        with self.assertRaises(ValueError):
            g.subset(entry("RU", "1"), ["ru", "private"])

    def test_large_entries_use_multi_byte_lengths(self):
        big = entry("RU", *[f"host{i}.ru" for i in range(500)])
        self.assertEqual(big, g.subset(big + entry("CN"), ["ru"]))


if __name__ == "__main__":
    unittest.main()
