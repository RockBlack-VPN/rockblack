"""Keep versioned update feeds (RELEASE.json) and permanent README download links."""
import argparse
import json
import re
from pathlib import Path

REPO = "RockBlack-VPN/rockblack"
BRAND = "RockBlack"
TAG_RE = re.compile(r"(android|desktop|ios)-v(\d+\.\d+\.\d+)")
LABELS = {"android": "Android", "desktop": "Desktop", "ios": "iOS"}

ANDROID_APK = "RockBlack.apk"
# iOS releases carry exactly one IPA: the signed one when the Apple signing
# secrets were configured, otherwise the unsigned (TrollStore / re-sign) build.
IOS_IPA = "RockBlack.ipa"
IOS_UNSIGNED_IPA = "RockBlack-unsigned.ipa"
ANDROID_ABIS = {
    "arm64-v8a": "RockBlack-arm64-v8a.apk",
    "armeabi-v7a": "RockBlack-armeabi-v7a.apk",
    "x86_64": "RockBlack-x86_64.apk",
    "x86": "RockBlack-x86.apk",
}
DESKTOP = {
    "windows": {
        "x64": "rockblack-windows-x64-setup.exe",
        "x64_portable": "rockblack-windows-x64-portable.zip",
        "arm64_portable": "rockblack-windows-arm64-portable.zip",
    },
    "macos": {
        "arm64": "rockblack-macos-arm64.dmg",
        "intel": "rockblack-macos-intel.dmg",
    },
    "linux": {
        "x64_deb": "rockblack-linux-x64.deb",
        "arm64_deb": "rockblack-linux-arm64.deb",
        "x64_rpm": "rockblack-linux-x64.rpm",
        "arm64_rpm": "rockblack-linux-arm64.rpm",
        "x64_portable": "rockblack-linux-x64-portable.zip",
        "arm64_portable": "rockblack-linux-arm64-portable.zip",
    },
}


def ios_ipa(names):
    """The IPA an iOS release advertises: signed if present, else unsigned."""
    return IOS_UNSIGNED_IPA if IOS_UNSIGNED_IPA in names and IOS_IPA not in names else IOS_IPA


def required_assets(platform, names=()):
    """Assets that must exist before a release may be advertised.

    `names` (the uploaded asset names) is only consulted for iOS, whose
    release contains either the signed or the unsigned IPA.
    """
    if platform == "android":
        return {ANDROID_APK}
    if platform == "ios":
        return {ios_ipa(set(names))}
    return {name for group in DESKTOP.values() for name in group.values()}


def version_key(version):
    return tuple(map(int, version.split(".")))


def parse_tag(release):
    """Return (platform, version) for a published stable platform release, else None."""
    match = TAG_RE.fullmatch(release.get("tag_name", ""))
    if not match or release.get("draft") or release.get("prerelease"):
        return None
    return match.groups()


def asset_url(tag, name):
    return f"https://github.com/{REPO}/releases/download/{tag}/{name}"


def update(manifest, release):
    """Point one platform of the manifest at `release`. Returns {asset: url} or {} when unchanged."""
    parsed = parse_tag(release)
    if not parsed:
        raise ValueError("Expected a published stable android-vX.Y.Z, desktop-vX.Y.Z or ios-vX.Y.Z release")
    platform, version = parsed
    old = manifest.get(platform, {}).get("version") or "0.0.0"
    if version_key(version) < version_key(old):
        return {}  # Publishing an older version must not roll back clients.
    names = {a["name"] for a in release.get("assets", []) if a.get("size", 0) > 0}
    required = required_assets(platform, names)
    if not required <= names:
        raise ValueError(f"Release assets not ready: missing {sorted(required - names)}")
    tag = release["tag_name"]
    urls = {n: asset_url(tag, n) for n in required}
    entry = dict(manifest.get(platform, {}))
    entry.update(version=version, tag=tag, name=f"{BRAND} {LABELS[platform]} {version}")
    if platform == "android":
        entry["download"] = urls[ANDROID_APK]
        entry["abi"] = {abi: asset_url(tag, n) for abi, n in ANDROID_ABIS.items() if n in names}
    elif platform == "ios":
        ipa = ios_ipa(names)
        entry["download"] = urls[ipa]
        entry["signed"] = ipa == IOS_IPA
    else:
        for os_name, assets in DESKTOP.items():
            entry[os_name] = {key: urls[name] for key, name in assets.items()}
    manifest[platform] = entry
    return urls


def newest_releases(releases):
    """Newest published stable release per platform."""
    newest = {}
    for release in releases:
        parsed = parse_tag(release)
        if not parsed:
            continue
        platform, version = parsed
        key = version_key(version)
        if platform not in newest or key > newest[platform][0]:
            newest[platform] = (key, release)
    return {platform: release for platform, (_, release) in newest.items()}


def rewrite_readme(text, names):
    """README download buttons always use the permanent `latest` tag."""
    for name in names:
        pattern = (rf"https://github\.com/{re.escape(REPO)}/releases/(?:latest/download|download/[^/\s)]+)/"
                   rf"{re.escape(name)}(?=[\s)\"<>]|$)")
        text = re.sub(pattern, lambda _, n=name: f"https://github.com/{REPO}/releases/download/latest/{n}", text)
    return text


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("release", type=Path, help="JSON file with one release object or a list of releases")
    args = parser.parse_args()
    path = Path("RELEASE.json")
    manifest = json.loads(path.read_text(encoding="utf-8"))
    payload = json.loads(args.release.read_text(encoding="utf-8"))
    releases = payload if isinstance(payload, list) else [payload]
    # Reconcile both channels: GitHub concurrency can replace pending runs,
    # so a later event must also apply a previously queued platform update.
    newest = newest_releases(releases)
    if not newest:
        raise ValueError("No published stable platform release found")
    urls = {}
    for release in newest.values():
        urls.update(update(manifest, release))
    if not urls:
        print("Older release: manifest unchanged")
        return
    readme = Path("README.md")
    readme.write_text(rewrite_readme(readme.read_text(encoding="utf-8"), urls), encoding="utf-8")
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
