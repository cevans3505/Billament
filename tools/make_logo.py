# Draws the Billament pixel-art logo (docs/images/logo.png) from the grids below.
import sys
from PIL import Image

ICON = """
......gggg......
.....gGGGGg.....
.....gggggg.....
......gggg......
.......gg.......
.......oo.......
.wwwwwwoowwwwws.
.wwwwwoooowwwws.
.wwwwoowwoowwws.
.wwwwoowwwwwwws.
.wwwwwoooowwwws.
.wwwwwwwwoowwws.
.wwwwoowwoowwws.
.wwwwwoooowwwws.
.wwwwwwoowwwwws.
.wwwwwwwwwwwwws.
.wwddddwwdddwws.
.wwwwwwwwwwwwws.
.ww..ww..ww..ws.
"""

GLYPHS = {
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
    "i": ["#", ".", "#", "#", "#", "#", "#"],
    "l": ["#", "#", "#", "#", "#", "#", "#"],
    "a": [".....", ".....", ".###.", "....#", ".####", "#...#", ".####"],
    "m": [".....", ".....", "##.#.", "#.#.#", "#.#.#", "#.#.#", "#.#.#"],
    "e": [".....", ".....", ".###.", "#...#", "#####", "#....", ".###."],
    "n": [".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#"],
    "t": [".#..", ".#..", "####", ".#..", ".#..", ".#..", "..##"],
}

COLORS = {
    "g": (110, 116, 128), "G": (170, 176, 186), "o": (255, 138, 42), "w": (242, 242, 242),
    "s": (200, 204, 210), "d": (96, 104, 120), "#": (242, 242, 242),
}
BG = (53, 53, 53)
SCALE = 10

icon = [r for r in ICON.strip().splitlines()]
word = "Billament"
cols = []
for ch in word:
    g = GLYPHS[ch]
    cols += [[row[x] for row in g] for x in range(len(g[0]))] + [["."] * 7]
cols.pop()

pad, gap = 3, 4
text_top = pad + (len(icon) - 7 * 2) // 2
w = pad + len(icon[0]) + gap + len(cols) * 2 + pad
h = pad + len(icon) + pad
img = Image.new("RGB", (w, h), BG)
px = img.load()
for y, row in enumerate(icon):
    for x, c in enumerate(row):
        if c != ".":
            px[pad + x, pad + y] = COLORS[c]
x0 = pad + len(icon[0]) + gap
for x, col in enumerate(cols):
    for y, c in enumerate(col):
        if c != ".":
            for dx in range(2):
                for dy in range(2):
                    px[x0 + 2 * x + dx, text_top + 2 * y + dy] = COLORS[c]
# AUDIT: file write. The logo PNG, at the path given on the command line.
img.resize((w * SCALE, h * SCALE), Image.NEAREST).save(sys.argv[1])
print(img.size)
