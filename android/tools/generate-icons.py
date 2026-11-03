#!/usr/bin/env python3
"""Regenerate RockBlack Android launcher/notification/banner icons.

Sources live in ../../branding (see branding/README.md). Requires Pillow.
Run from anywhere:  python android/tools/generate-icons.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ANDROID = HERE.parent
BRANDING = ANDROID.parent / "branding"
RES = ANDROID / "V2rayNG" / "app" / "src" / "main" / "res"

GRAPHITE = (0x5F, 0x60, 0x62)

DENSITIES = {"mdpi": 1.0, "hdpi": 1.5, "xhdpi": 2.0, "xxhdpi": 3.0, "xxxhdpi": 4.0}

icon_round = Image.open(BRANDING / "icon.png").convert("RGBA")
# Transparent symbol, already sized for the 66dp safe zone of a 108dp canvas.
icon_fg = Image.open(BRANDING / "icon-foreground.png").convert("RGBA")


def resize(img: Image.Image, size) -> Image.Image:
    if isinstance(size, int):
        size = (size, size)
    return img.resize(size, Image.LANCZOS)


def save(img: Image.Image, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, "PNG", optimize=True)
    print("wrote", path.relative_to(ANDROID))


mask_full = icon_fg.getchannel("A")
mono_full = Image.new("RGBA", icon_fg.size, (255, 255, 255, 0))
mono_full.putalpha(mask_full)
black_full = Image.new("RGBA", icon_fg.size, (0, 0, 0, 0))
black_full.putalpha(mask_full)
bbox = mask_full.getbbox()

for name, k in DENSITIES.items():
    d = RES / f"mipmap-{name}"
    legacy = round(48 * k)
    adaptive = round(108 * k)
    save(resize(icon_round, legacy), d / "ic_launcher.png")
    save(resize(icon_round, legacy), d / "ic_launcher_round.png")
    save(resize(icon_fg, adaptive), d / "ic_launcher_foreground.png")
    save(resize(mono_full, adaptive), d / "ic_launcher_monochrome.png")

    # Status-bar / quick-settings icon: white symbol silhouette, 24dp canvas.
    stat = round(24 * k)
    for fname, src in (("ic_stat_name.png", mono_full), ("ic_stat_name_black.png", black_full)):
        if fname == "ic_stat_name_black.png" and name == "hdpi":
            continue  # upstream ships no hdpi variant of the black icon
        crop = src.crop(bbox)
        cw, ch = crop.size
        target = stat * 22 / 24
        f = target / max(cw, ch)
        crop = crop.resize((max(1, round(cw * f)), max(1, round(ch * f))), Image.LANCZOS)
        out = Image.new("RGBA", (stat, stat), (0, 0, 0, 0))
        out.alpha_composite(crop, ((stat - crop.width) // 2, (stat - crop.height) // 2))
        save(out, RES / f"drawable-{name}" / fname)

# In-app logo (navigation drawer header).
save(resize(icon_round, 384), RES / "drawable-nodpi" / "ic_brand_logo.png")

# Android TV banner (xhdpi 320x180): round icon + wordmark, on graphite.
def load_font(size):
    for f in ("C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf",
              "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            continue
    return ImageFont.load_default()

banner_fg = Image.new("RGBA", (320, 180), (0, 0, 0, 0))
logo = resize(icon_round, 88)
banner_fg.alpha_composite(logo, (22, 46))
draw = ImageDraw.Draw(banner_fg)
font = load_font(34)
text = "RockBlack"
tb = draw.textbbox((0, 0), text, font=font)
draw.text((122, 90 - (tb[1] + tb[3]) // 2), text, font=font, fill=(255, 255, 255, 255))
save(banner_fg, RES / "mipmap-xhdpi" / "ic_banner_foreground.png")
banner = Image.new("RGBA", (320, 180), GRAPHITE + (255,))
inset = banner_fg.resize((256, 144), Image.LANCZOS)  # matches the 10% inset of ic_banner.xml
banner.alpha_composite(inset, (32, 18))
save(banner, RES / "mipmap-xhdpi" / "ic_banner.png")

# Web / store icons.
save(resize(icon_round, 512), ANDROID / "V2rayNG" / "app" / "src" / "main" / "ic_launcher-web.png")
save(resize(icon_round, 512), ANDROID / "fastlane" / "metadata" / "android" / "en-US" / "images" / "icon.png")
