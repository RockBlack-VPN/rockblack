#!/usr/bin/env bash
# Build the RockBlack Linux packages from a published application directory
# (dotnet publish output of v2rayN.Desktop + AmazTool, cores already bundled in bin/).
#
# usage: package-linux.sh <arch: x64|arm64> <version> <app_dir> <out_dir>
# produces in <out_dir>:
#   rockblack-linux-<arch>.deb
#   rockblack-linux-<arch>.rpm
#   rockblack-linux-<arch>-portable.zip   (top folder rockblack-linux-<arch>/, used by self-update)
#
# Requires: dpkg-deb, fakeroot (optional), rpmbuild, zip.
set -euo pipefail

ARCH="${1:?arch required}"
VERSION="${2:?version required}"
APP_DIR="$(cd "${3:?app dir required}" && pwd)"
OUT_DIR="${4:?out dir required}"
mkdir -p "$OUT_DIR"
OUT_DIR="$(cd "$OUT_DIR" && pwd)"

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ICON="$SCRIPT_DIR/../v2rayN/v2rayN.Desktop/v2rayN.png"

case "$ARCH" in
  x64)   DEB_ARCH=amd64; RPM_ARCH=x86_64 ;;
  arm64) DEB_ARCH=arm64; RPM_ARCH=aarch64 ;;
  *) echo "unknown arch $ARCH" >&2; exit 1 ;;
esac

[[ -f "$APP_DIR/RockBlack" ]] || { echo "RockBlack executable not found in $APP_DIR" >&2; exit 1; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# ---------------------------------------------------------------- common payload
stage_payload() {
  local root="$1"
  mkdir -p "$root/opt/rockblack" "$root/usr/bin" "$root/usr/share/applications" \
           "$root/usr/share/icons/hicolor/256x256/apps" "$root/usr/share/doc/rockblack"
  cp -a "$APP_DIR/." "$root/opt/rockblack/"
  find "$root/opt/rockblack" -type d -exec chmod 0755 {} +
  find "$root/opt/rockblack" -type f -exec chmod 0644 {} +
  chmod 0755 "$root/opt/rockblack/RockBlack"
  [[ -f "$root/opt/rockblack/AmazTool" ]] && chmod 0755 "$root/opt/rockblack/AmazTool"
  local core
  for core in xray/xray sing_box/sing-box mihomo/mihomo; do
    [[ -f "$root/opt/rockblack/bin/$core" ]] && chmod 0755 "$root/opt/rockblack/bin/$core"
  done
  # Packaged installs keep configs in the user's data dir, not under /opt.
  echo "When this file exists, app will not store configs under this folder" > "$root/opt/rockblack/NotStoreConfigHere.txt"
  chmod 0644 "$root/opt/rockblack/NotStoreConfigHere.txt"

  cat > "$root/usr/bin/rockblack" <<'EOF'
#!/bin/sh
cd /opt/rockblack || exit 1
exec /opt/rockblack/RockBlack "$@"
EOF
  chmod 0755 "$root/usr/bin/rockblack"

  cat > "$root/usr/share/applications/rockblack.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=RockBlack
GenericName=VPN client
Comment=RockBlack VPN client (Xray-core)
Exec=rockblack
Icon=rockblack
Terminal=false
Categories=Network;
StartupWMClass=RockBlack
EOF
  chmod 0644 "$root/usr/share/applications/rockblack.desktop"

  install -m 0644 "$ICON" "$root/usr/share/icons/hicolor/256x256/apps/rockblack.png"
  install -m 0644 "$SCRIPT_DIR/../LICENSE" "$root/usr/share/doc/rockblack/copyright"
}

# ---------------------------------------------------------------- deb
build_deb() {
  local root="$work/deb"
  stage_payload "$root"
  mkdir -p "$root/DEBIAN"
  local size
  size="$(du -sk "$root/opt" | cut -f1)"
  cat > "$root/DEBIAN/control" <<EOF
Package: rockblack
Version: ${VERSION}
Architecture: ${DEB_ARCH}
Maintainer: RockBlack <noreply@rockblack.tech>
Homepage: https://my.rockblack.tech
Section: net
Priority: optional
Installed-Size: ${size}
Depends: libc6 (>= 2.31), libgcc-s1, libstdc++6, zlib1g, libfontconfig1, libfreetype6, xdg-utils
Recommends: libicu76 | libicu74 | libicu72 | libicu70
Description: RockBlack VPN desktop client
 RockBlack desktop client built on Xray-core (sing-box and mihomo also bundled).
 Based on v2rayN (https://github.com/2dust/v2rayN), GPL-3.0.
EOF
  cat > "$root/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
exit 0
EOF
  cp "$root/DEBIAN/postinst" "$root/DEBIAN/postrm"
  chmod 0755 "$root/DEBIAN/postinst" "$root/DEBIAN/postrm"
  dpkg-deb --root-owner-group -Zxz --build "$root" "$OUT_DIR/rockblack-linux-${ARCH}.deb"
  dpkg-deb --info "$OUT_DIR/rockblack-linux-${ARCH}.deb"
}

# ---------------------------------------------------------------- rpm
build_rpm() {
  local top="$work/rpmbuild"
  local payload="$work/rpm-payload"
  stage_payload "$payload"
  mkdir -p "$top"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS}
  local spec="$top/SPECS/rockblack.spec"
  # RPM versions may not contain '-'
  local rpm_version="${VERSION//-/_}"
  cat > "$spec" <<EOF
%global debug_package %{nil}
%global __os_install_post %{nil}
%global __strip /bin/true
%define _build_id_links none
%define _binaries_in_noarch_packages_terminate_build 0

Name:           rockblack
Version:        ${rpm_version}
Release:        1
Summary:        RockBlack VPN desktop client
License:        GPL-3.0-only
URL:            https://my.rockblack.tech
BugURL:         https://github.com/RockBlack-VPN/rockblack/issues
AutoReqProv:    no
Requires:       glibc, libgcc, libstdc++, zlib, fontconfig, freetype, libicu, xdg-utils

%description
RockBlack desktop client built on Xray-core (sing-box and mihomo also bundled).
Based on v2rayN (https://github.com/2dust/v2rayN), GPL-3.0.

%install
mkdir -p %{buildroot}
cp -a ${payload}/. %{buildroot}/

%post
/usr/bin/update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || :
/usr/bin/gtk-update-icon-cache -f %{_datadir}/icons/hicolor >/dev/null 2>&1 || :

%postun
/usr/bin/update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || :
/usr/bin/gtk-update-icon-cache -f %{_datadir}/icons/hicolor >/dev/null 2>&1 || :

%files
%defattr(-,root,root,-)
/opt/rockblack
%{_bindir}/rockblack
%{_datadir}/applications/rockblack.desktop
%{_datadir}/icons/hicolor/256x256/apps/rockblack.png
%doc %{_datadir}/doc/rockblack/copyright
EOF
  rpmbuild -bb --target "${RPM_ARCH}-linux" --define "_topdir $top" "$spec"
  local built
  built="$(find "$top/RPMS" -name "rockblack-*.rpm" | head -n1)"
  [[ -n "$built" ]] || { echo "rpm not produced" >&2; exit 1; }
  cp "$built" "$OUT_DIR/rockblack-linux-${ARCH}.rpm"
  rpm -qpi "$OUT_DIR/rockblack-linux-${ARCH}.rpm" || true
}

# ---------------------------------------------------------------- portable zip
build_zip() {
  local name="rockblack-linux-${ARCH}"
  local dir="$work/zip/$name"
  mkdir -p "$dir"
  cp -a "$APP_DIR/." "$dir/"
  chmod 0755 "$dir/RockBlack"
  [[ -f "$dir/AmazTool" ]] && chmod 0755 "$dir/AmazTool"
  (cd "$work/zip" && zip -q -r -y "$OUT_DIR/${name}-portable.zip" "$name")
}

build_deb
build_rpm
build_zip
ls -la "$OUT_DIR"
