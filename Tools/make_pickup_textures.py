#!/usr/bin/env python3
"""Draws the pickup textures (Assets/Resources/Textures/Pickups) with Pillow and the game's Barlow fonts.

Every texture is laid out for Unity's cube primitive, which maps the whole image onto each face:
ticket (ČD fare ticket), medkit (first-aid case), tvaruzky (Olomoucké tvarůžky box), suitcase (hard shell) and
bag (inspector's leather bag). Run: python3 Tools/make_pickup_textures.py
"""
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets/Resources/Textures/Pickups"
FONTS = ROOT / "Assets/Resources/Fonts"


def font(name, size):
    return ImageFont.truetype(str(FONTS / name), size)


def ticket():
    w, h = 1024, 640
    im = Image.new("RGB", (w, h), (247, 244, 236))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, w, 150), fill=(0, 82, 158))
    d.text((48, 26), "ČD", font=font("BarlowCondensed-ExtraBold.ttf", 110), fill=(255, 255, 255))
    d.text((230, 48), "JÍZDENKA  /  TICKET", font=font("BarlowCondensed-Bold.ttf", 64), fill=(255, 255, 255))
    d.rectangle((0, 150, w, 172), fill=(244, 182, 81))
    body = font("BarlowCondensed-Bold.ttf", 58)
    small = font("Barlow-Medium.ttf", 34)
    d.text((48, 200), "OLOMOUC hl. n.  –  PŘEDNÁDRAŽÍ", font=body, fill=(30, 40, 50))
    d.text((48, 280), "2. třída   ·   1 dospělý   ·   Os 4601", font=small, fill=(70, 78, 86))
    d.text((48, 330), "Platí 19.11.  ·  cena 42 Kč", font=small, fill=(70, 78, 86))
    random.seed(4)
    x = 48
    while x < 640:
        bar = random.choice((4, 6, 9, 12))
        d.rectangle((x, 430, x + bar, 590), fill=(25, 25, 30))
        x += bar + random.choice((5, 7, 10))
    d.rectangle((700, 420, 970, 600), outline=(0, 82, 158), width=6)
    d.text((725, 470), "SuperCigos", font=font("BarlowCondensed-Bold.ttf", 54), fill=(0, 82, 158))
    d.rectangle((0, 0, w - 1, h - 1), outline=(200, 196, 186), width=8)
    im.save(OUT / "Ticket.png")


def medkit():
    s = 512
    im = Image.new("RGB", (s, s), (240, 242, 238))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, s - 1, s - 1), outline=(190, 194, 190), width=18)
    d.rectangle((s * .4, s * .16, s * .6, s * .84), fill=(20, 150, 80))
    d.rectangle((s * .16, s * .4, s * .84, s * .6), fill=(20, 150, 80))
    d.rectangle((0, s * .9, s, s), fill=(205, 208, 204))
    im.save(OUT / "Medkit.png")


def tvaruzky():
    w, h = 768, 512
    im = Image.new("RGB", (w, h), (243, 197, 72))
    d = ImageDraw.Draw(im)
    for i in range(0, w, 32):
        d.line((i, 0, i - 120, h), fill=(236, 184, 58), width=10)
    d.rounded_rectangle((40, 60, w - 40, h - 60), 40, fill=(250, 245, 228), outline=(176, 58, 36), width=10)
    d.text((80, 90), "OLOMOUCKÉ", font=font("BarlowCondensed-ExtraBold.ttf", 120), fill=(176, 58, 36))
    d.text((80, 220), "TVARŮŽKY", font=font("BarlowCondensed-ExtraBold.ttf", 150), fill=(176, 58, 36))
    d.text((84, 390), "pravé  ·  zrající sýr  ·  125 g", font=font("Barlow-Medium.ttf", 36), fill=(110, 70, 40))
    im.save(OUT / "Tvaruzky.png")


def suitcase():
    s = 512
    im = Image.new("RGB", (s, s), (217, 88, 123))
    d = ImageDraw.Draw(im)
    for y in range(40, s, 64):
        d.rectangle((0, y, s, y + 18), fill=(196, 70, 104))
        d.rectangle((0, y + 18, s, y + 22), fill=(236, 128, 158))
    d.rectangle((0, 0, s - 1, s - 1), outline=(120, 40, 62), width=16)
    d.rectangle((s * .42, 0, s * .58, s * .1), fill=(60, 60, 64))
    im = im.filter(ImageFilter.GaussianBlur(1.2))
    im.save(OUT / "Suitcase.png")


def bag():
    s = 512
    random.seed(9)
    im = Image.new("RGB", (s, s), (92, 58, 36))
    px = im.load()
    for y in range(s):
        for x in range(s):
            n = random.randint(-10, 10)
            r, g, b = px[x, y]
            px[x, y] = (r + n, g + n, b + n)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((20, 20, s - 20, s * .62), 40, fill=(78, 48, 30), outline=(52, 32, 20), width=8)
    d.rectangle((s * .44, s * .55, s * .56, s * .7), fill=(196, 160, 72))
    d.line((24, s * .66, s - 24, s * .66), fill=(52, 32, 20), width=6)
    d.text((s * .3, s * .74), "ČD", font=font("BarlowCondensed-ExtraBold.ttf", 110), fill=(196, 160, 72))
    im = im.filter(ImageFilter.GaussianBlur(.8))
    im.save(OUT / "Bag.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for make in (ticket, medkit, tvaruzky, suitcase, bag):
        make()
    print("PICKUP_TEXTURES", sorted(p.name for p in OUT.glob("*.png")))


if __name__ == "__main__":
    main()
