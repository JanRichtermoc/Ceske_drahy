#!/usr/bin/env python3
"""Draws the application icon (Assets/Art/Icon/SuperCigos_1024.png): ČD blue tile, amber platform stripe and the
SC monogram in the game's Barlow Condensed. Run: python3 Tools/make_icon.py"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
S = 1024


def main():
    tile = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle((40, 40, S - 40, S - 40), 210, fill=255)
    # Vertical gradient from the HUD navy to ČD blue.
    gradient = Image.new("RGBA", (S, S))
    top, bottom = (29, 94, 158), (10, 34, 58)
    for y in range(S):
        t = y / (S - 1)
        gradient.paste(tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,), (0, y, S, y + 1))
    tile.paste(gradient, (0, 0), mask)
    draw = ImageDraw.Draw(tile)
    # Amber platform edge and a white safety line, like the station platforms.
    draw.rectangle((40, 690, S - 40, 760), fill=(245, 179, 53, 255))
    draw.rectangle((40, 784, S - 40, 800), fill=(232, 236, 230, 255))
    tile.putalpha(Image.composite(tile.getchannel("A"), Image.new("L", (S, S), 0), mask))
    font = ImageFont.truetype(str(ROOT / "Assets/Resources/Fonts/BarlowCondensed-ExtraBold.ttf"), 560)
    shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).text((S / 2 + 10, 400 + 18), "SC", font=font, fill=(0, 0, 0, 150), anchor="mm")
    tile.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(14)))
    ImageDraw.Draw(tile).text((S / 2, 400), "SC", font=font, fill=(250, 248, 240, 255), anchor="mm")
    out = ROOT / "Assets/Art/Icon/SuperCigos_1024.png"
    tile.save(out)
    print("ICON", out)


if __name__ == "__main__":
    main()
