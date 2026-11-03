#!/usr/bin/env python3
"""Regenerate the RockBlack desktop icons from ../branding/icon.png.

Run from the repository root:  python desktop/scripts/generate-icons.py

Upstream file names are kept (v2rayN.ico, NotifyIcon1-4.ico, v2rayN.png, v2rayN.icns)
so no project/XAML references have to change. NotifyIcon1..4 are the tray state icons
(1 = default, 2 = system proxy, 3 = PAC, 4 = TUN/other, same semantics as upstream);
they differ only by the colour of the outer ring.
"""
import colorsys
import math
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "branding", "icon.png")
WPF_DIR = os.path.join(ROOT, "desktop", "v2rayN", "v2rayN", "Resources")
AVA_DIR = os.path.join(ROOT, "desktop", "v2rayN", "v2rayN.Desktop")
TEAL = (0x26, 0xD6, 0xB0)
ICO_SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (256, 256)]
VARIANTS = {
    "NotifyIcon1": None,                # teal (brand)
    "NotifyIcon2": (0xE5, 0x39, 0x35),  # red
    "NotifyIcon3": (0x8E, 0x24, 0xAA),  # purple
    "NotifyIcon4": (0xFB, 0xC0, 0x2D),  # yellow
}


def recolor_ring(im: Image.Image, rgb) -> Image.Image:
    im = im.copy()
    w, h = im.size
    px = im.load()
    cx, cy, radius = w / 2, h / 2, w / 2
    th, ts, tv = colorsys.rgb_to_hsv(*[c / 255 for c in TEAL])
    nh, ns, nv = colorsys.rgb_to_hsv(*[c / 255 for c in rgb])
    for y in range(h):
        for x in range(w):
            if math.hypot(x + 0.5 - cx, y + 0.5 - cy) < radius * 0.86:
                continue
            p = px[x, y]
            if p[3] == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
            if s < 0.25 or abs(hh - th) > 0.08:
                continue
            r, g, b = colorsys.hsv_to_rgb(nh, min(1, s / ts * ns), min(1, v / tv * nv))
            px[x, y] = (int(r * 255), int(g * 255), int(b * 255), p[3])
    return im


def save_ico(im: Image.Image, path: str) -> None:
    im.resize((256, 256), Image.LANCZOS).save(path, format="ICO", sizes=ICO_SIZES)


def main() -> None:
    src = Image.open(SRC).convert("RGBA")
    for name, color in VARIANTS.items():
        variant = src if color is None else recolor_ring(src, color)
        for d in (WPF_DIR, os.path.join(AVA_DIR, "Assets")):
            save_ico(variant, os.path.join(d, name + ".ico"))
    for d in (WPF_DIR, os.path.join(AVA_DIR, "Assets")):
        save_ico(src, os.path.join(d, "v2rayN.ico"))
    src.resize((256, 256), Image.LANCZOS).save(os.path.join(AVA_DIR, "v2rayN.png"), optimize=True)
    src.save(os.path.join(AVA_DIR, "v2rayN.icns"), format="ICNS",
             sizes=[(16, 16), (32, 32), (64, 64), (128, 128), (256, 256), (512, 512), (1024, 1024)])
    print("icons regenerated from", SRC)


if __name__ == "__main__":
    main()
