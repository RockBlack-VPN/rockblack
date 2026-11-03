import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).parents[1]


class ObtainiumConfigTest(unittest.TestCase):
    def test_import_schema_and_filters_only_match_android_releases(self):
        app = json.loads((ROOT / "obtainium.json").read_text(encoding="utf-8"))[0]
        self.assertEqual("tech.rockblack.app", app["id"])
        self.assertEqual("https://github.com/RockBlack-VPN/rockblack", app["url"])
        settings = json.loads(app["additionalSettings"])
        title = re.compile(settings["filterReleaseTitlesByRegEx"])
        self.assertIsNotNone(title.search("Android v2.3.15"))
        self.assertIsNone(title.search("Desktop v7.25.6"))
        self.assertIsNone(title.search("RockBlack — Latest downloads"))
        self.assertFalse(settings["verifyLatestTag"])
        self.assertTrue(settings["fallbackToOlderReleases"])
        apk = re.compile(settings["apkFilterRegEx"])
        self.assertIsNotNone(apk.search("RockBlack.apk"))
        self.assertIsNone(apk.search("RockBlack-arm64-v8a.apk"))
        self.assertIsNone(apk.search("rockblack-windows-x64-setup.exe"))
        self.assertIsNone(apk.search("another.apk"))
        self.assertEqual("2.3.15", re.search(settings["versionExtractionRegEx"], "android-v2.3.15")[0])

    def test_readme_obtainium_link_matches_config(self):
        readme = (ROOT / "README.md").read_text(encoding="utf-8")
        self.assertIn("obtainium://app/", readme)
        self.assertIn("tech.rockblack.app", readme)


if __name__ == "__main__":
    unittest.main()
