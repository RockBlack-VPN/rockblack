"""Mirror validated platform releases into the permanent `latest` download tag."""
import hashlib
import json
import re
import subprocess
import tempfile
from pathlib import Path

from update_manifest import BRAND, LABELS, REPO, required_assets

LATEST_TITLE = f"{BRAND} — Latest downloads"
PLATFORMS = ("desktop", "android", "ios")


def gh(*args):
    return subprocess.check_output(["gh", *args], text=True)


def plan(manifest, read_release):
    """Validate the releases RELEASE.json points at. Platforms not released yet are skipped."""
    result = []
    for platform in PLATFORMS:
        version = manifest.get(platform, {}).get("version")
        if not version:
            continue
        if not re.fullmatch(r"\d+\.\d+\.\d+", version):
            raise ValueError("Invalid platform version")
        tag = f"{platform}-v{version}"
        release = read_release(tag)
        if release.get("draft") or release.get("prerelease") or release["tag_name"] != tag:
            raise ValueError(f"Not a published stable release: {tag}")
        assets = {a["name"]: a for a in release["assets"] if a.get("size", 0) > 0}
        names = required_assets(platform, assets.keys())
        if not names <= assets.keys():
            raise ValueError(f"Incomplete release {tag}: {sorted(names - assets.keys())}")
        result.append((tag, {name: assets[name] for name in sorted(names)}))
    if not result:
        raise ValueError("No platform has been released yet")
    return result


def verify(path, asset):
    if path.stat().st_size != asset["size"]:
        raise ValueError(f"Asset size mismatch: {path.name}")
    checksum = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            checksum.update(chunk)
    digest = checksum.hexdigest()
    expected = asset.get("digest")
    if expected and expected != f"sha256:{digest}":
        raise ValueError(f"Asset digest mismatch: {path.name}")
    return digest


def notes_text(manifest):
    parts = []
    for platform in PLATFORMS:
        version = manifest.get(platform, {}).get("version")
        if version:
            parts.append(f"{LABELS[platform]} **{version}**")
    return (
        "Latest stable downloads: " + ", ".join(parts) + ".\n\n"
        "Permanent download tag; files are updated independently by platform. "
        "For version history and Obtainium use `desktop-v*` / `android-v*` / `ios-v*` releases.\n"
    )


def main():
    manifest = json.loads(Path("RELEASE.json").read_text(encoding="utf-8"))
    releases = plan(manifest, lambda tag: json.loads(gh("api", f"repos/{REPO}/releases/tags/{tag}")))
    with tempfile.TemporaryDirectory(prefix="rockblack-latest-") as directory:
        root = Path(directory)
        paths, sums = [], []
        # Download and verify every channel before touching the shared release.
        for tag, assets in releases:
            dest = root / tag
            dest.mkdir()
            patterns = [arg for name in assets for arg in ("--pattern", name)]
            gh("release", "download", tag, "--repo", REPO, "--dir", str(dest), *patterns)
            for name, asset in assets.items():
                path = dest / name
                sums.append(f"{verify(path, asset)}  {name}")
                paths.append(str(path))
        checksums = root / "SHA256SUMS"
        checksums.write_text("\n".join(sorted(sums)) + "\n", encoding="utf-8")
        paths.append(str(checksums))
        notes = root / "notes.md"
        notes.write_text(notes_text(manifest), encoding="utf-8")
        # Listing distinguishes a missing tag from authentication/network errors.
        existing = json.loads(gh("api", f"repos/{REPO}/releases?per_page=100", "--paginate", "--slurp"))
        latest = next((r for page in existing for r in page if r["tag_name"] == "latest"), None)
        if latest:
            gh("release", "upload", "latest", *paths, "--repo", REPO, "--clobber")
            gh("release", "edit", "latest", "--repo", REPO, "--title", LATEST_TITLE,
               "--notes-file", str(notes), "--draft=false", "--prerelease=false", "--latest")
        else:
            gh("release", "create", "latest", *paths, "--repo", REPO, "--target", "main",
               "--title", LATEST_TITLE, "--notes-file", str(notes), "--latest")
        published = json.loads(gh("api", f"repos/{REPO}/releases/tags/latest"))
        actual = {a["name"]: a for a in published["assets"]}
        for file in paths:
            path = Path(file)
            verify(path, actual[path.name])
        print("latest verified: " + ", ".join(tag for tag, _ in releases))


if __name__ == "__main__":
    main()
