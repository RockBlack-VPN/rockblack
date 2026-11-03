import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
from sync_latest import notes_text, plan, verify  # noqa: E402
from update_manifest import DESKTOP  # noqa: E402

DESKTOP_NAMES = {n for group in DESKTOP.values() for n in group.values()}


class LatestTest(unittest.TestCase):
    def fixture(self):
        manifest = {"desktop": {"version": "7.25.6"}, "android": {"version": "2.3.15"}}
        releases = {tag: {"tag_name": tag, "assets": [{"name": n, "size": 3} for n in names]}
                    for tag, names in [("desktop-v7.25.6", DESKTOP_NAMES),
                                       ("android-v2.3.15", {"RockBlack.apk", "RockBlack-x86.apk"})]}
        return manifest, releases

    def test_combines_complete_independent_channels(self):
        manifest, releases = self.fixture()
        result = plan(manifest, releases.__getitem__)
        self.assertEqual([tag for tag, _ in result], ["desktop-v7.25.6", "android-v2.3.15"])
        self.assertEqual(len(result[0][1]), 11)
        self.assertEqual(set(result[1][1]), {"RockBlack.apk"})

    def test_unreleased_platform_is_skipped(self):
        manifest, releases = self.fixture()
        manifest["desktop"]["version"] = None
        result = plan(manifest, releases.__getitem__)
        self.assertEqual([tag for tag, _ in result], ["android-v2.3.15"])
        self.assertNotIn("Desktop", notes_text(manifest))

    def test_nothing_released_is_an_error(self):
        with self.assertRaises(ValueError):
            plan({"desktop": {"version": None}, "android": {}}, lambda tag: {})

    def test_rejects_partial_draft_and_prerelease_before_publication(self):
        for kind in ["missing", "draft", "prerelease"]:
            manifest, releases = self.fixture()
            release = releases["desktop-v7.25.6"]
            if kind == "missing":
                release["assets"].pop()
            else:
                release[kind] = True
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                plan(manifest, releases.__getitem__)

    def test_asset_integrity(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp, "RockBlack.apk")
            path.write_bytes(b"apk")
            digest = hashlib.sha256(b"apk").hexdigest()
            self.assertEqual(verify(path, {"size": 3, "digest": "sha256:" + digest}), digest)
            for asset in [{"size": 4}, {"size": 3, "digest": "sha256:incorrect"}]:
                with self.assertRaises(ValueError):
                    verify(path, asset)

    def test_manifest_stays_pinned_but_readme_stays_latest_after_updates(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            root.joinpath("RELEASE.json").write_text('{"desktop":{"version":"7.25.6"}}', encoding="utf-8")
            root.joinpath("README.md").write_text(
                "[APK](https://github.com/RockBlack-VPN/rockblack/releases/download/android-v2.3.0/RockBlack.apk)",
                encoding="utf-8")
            release = root / "event.json"
            for version in ["2.3.9", "2.3.15"]:
                release.write_text(json.dumps({"tag_name": f"android-v{version}",
                                               "assets": [{"name": "RockBlack.apk", "size": 3}]}), encoding="utf-8")
                subprocess.run([sys.executable, str(ROOT / "scripts/update_manifest.py"), str(release)],
                               cwd=root, check=True)
                self.assertIn("/download/latest/RockBlack.apk", root.joinpath("README.md").read_text(encoding="utf-8"))
                manifest = json.loads(root.joinpath("RELEASE.json").read_text(encoding="utf-8"))
                self.assertIn(f"/android-v{version}/", manifest["android"]["download"])
                self.assertEqual(manifest["desktop"]["version"], "7.25.6")


if __name__ == "__main__":
    unittest.main()
