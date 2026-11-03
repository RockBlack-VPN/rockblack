import copy
import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("manifest", Path(__file__).parents[1] / "scripts/update_manifest.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

DESKTOP_NAMES = {n for group in m.DESKTOP.values() for n in group.values()}


class ManifestTest(unittest.TestCase):
    def release(self, tag, names):
        return {"tag_name": tag, "assets": [{"name": n, "size": 1} for n in names]}

    def test_android_leaves_desktop_unchanged(self):
        data = {"desktop": {"version": "7.25.6"}}
        original = copy.deepcopy(data)
        m.update(data, self.release("android-v2.3.15", ["RockBlack.apk", "RockBlack-arm64-v8a.apk"]))
        self.assertEqual(original["desktop"], data["desktop"])
        self.assertIn("/android-v2.3.15/RockBlack.apk", data["android"]["download"])
        self.assertEqual(["arm64-v8a"], list(data["android"]["abi"]))
        self.assertEqual("RockBlack Android 2.3.15", data["android"]["name"])

    def test_desktop_leaves_android_unchanged(self):
        data = {"android": {"version": "2.3.15", "download": "kept"}}
        m.update(data, self.release("desktop-v7.25.6", DESKTOP_NAMES))
        self.assertEqual("kept", data["android"]["download"])
        self.assertIn("/desktop-v7.25.6/rockblack-windows-x64-setup.exe", data["desktop"]["windows"]["x64"])
        self.assertIn("/desktop-v7.25.6/rockblack-macos-arm64.dmg", data["desktop"]["macos"]["arm64"])

    def test_first_release_from_null_version(self):
        data = {"android": {"version": None}, "desktop": {"version": None}}
        m.update(data, self.release("android-v2.3.15", ["RockBlack.apk"]))
        self.assertEqual("2.3.15", data["android"]["version"])
        self.assertIsNone(data["desktop"]["version"])

    def test_incomplete_and_wrong_platform_releases_are_rejected(self):
        for tag, names in [("android-v2.3.15", ["setup.exe"]),
                           ("desktop-v7.25.6", ["RockBlack.apk"]),
                           ("desktop-v7.25.6", sorted(DESKTOP_NAMES)[1:]),
                           ("v2.3.15", ["RockBlack.apk"])]:
            data = {}
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                m.update(data, self.release(tag, names))
            self.assertEqual({}, data)

    def test_draft_and_prerelease_are_rejected(self):
        for flag in ("draft", "prerelease"):
            release = self.release("android-v2.3.15", ["RockBlack.apk"])
            release[flag] = True
            with self.subTest(flag=flag), self.assertRaises(ValueError):
                m.update({}, release)

    def test_old_release_cannot_downgrade(self):
        data = {"android": {"version": "2.4.0"}}
        self.assertEqual({}, m.update(data, self.release("android-v2.3.15", ["RockBlack.apk"])))
        self.assertEqual("2.4.0", data["android"]["version"])

    def test_newest_release_per_platform(self):
        releases = [self.release(t, []) for t in
                    ["android-v2.3.9", "android-v2.3.15", "desktop-v7.25.6", "latest", "desktop-v7.3.0"]]
        newest = m.newest_releases(releases)
        self.assertEqual("android-v2.3.15", newest["android"]["tag_name"])
        self.assertEqual("desktop-v7.25.6", newest["desktop"]["tag_name"])


if __name__ == "__main__":
    unittest.main()
