"""Cut the painted citadel battlefield into reusable terrain pieces and a backdrop.

Each piece has a loose outline polygon (includes props such as fences, torches and gallows)
and a solid core polygon (rock body). Inside the outline, pixels are kept when they are not
bluish mist/waterfall/sky; the core is always kept. Pixels go to the first piece that claims them.

Outputs (paths relative to the Unity project):
  Assets/FortressContent/Art/Terrain/<Piece>.png   RGBA, cropped to the piece
  Assets/FortressContent/Art/CitadelBackdrop.png   painting with the pieces removed and filled
  Assets/FortressContent/Art/Terrain/pieces.json   pixel rectangles (top-left origin) per piece

Run:  python Tools/slice_citadel.py   (from the game folder)
"""
import json
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
SOURCE = os.path.join(ROOT, "Assets/FortressContent/Art/Citadel.png")
OUT = os.path.join(ROOT, "Assets/FortressContent/Art/Terrain")
BACKDROP = os.path.join(ROOT, "Assets/FortressContent/Art/CitadelBackdrop.png")

# name: (outline, core). Image pixels, x right / y down. Order = claim priority.
PIECES = {
    "Stairs": (
        [(895, 338), (1045, 338), (1045, 445), (895, 445)],
        [(905, 408), (955, 408), (955, 372), (1040, 372), (1040, 442), (905, 442)]),
    "Bridge": (
        [(340, 330), (430, 330), (430, 385), (895, 385), (895, 445), (1035, 445), (1035, 560), (1000, 600),
         (875, 600), (875, 548), (650, 548), (650, 605), (430, 605), (425, 578), (385, 578), (385, 610), (340, 610)],
        [(430, 447), (1030, 447), (1030, 468), (430, 468)]),
    "RightCliff": (
        [(1030, 180), (1330, 180), (1360, 300), (1350, 560), (1335, 580), (1335, 941), (1095, 941), (1090, 800),
         (1050, 760), (990, 700), (990, 560), (1025, 445), (1030, 300)],
        [(1045, 308), (1340, 308), (1340, 941), (1110, 941), (1105, 800), (1010, 700), (1010, 560), (1035, 445)]),
    "LeftCliff": (
        [(0, 0), (70, 0), (120, 80), (280, 80), (330, 330), (380, 340), (380, 420), (355, 480), (350, 640), (300, 660), (300, 941), (0, 941)],
        [(0, 368), (365, 368), (345, 470), (340, 640), (300, 660), (300, 941), (0, 941)]),
    "LowerLeft": (
        [(290, 585), (580, 585), (578, 700), (545, 760), (535, 941), (300, 941)],
        [(300, 645), (566, 645), (560, 700), (530, 760), (525, 941), (300, 941)]),
    "CenterPillar": (
        [(645, 550), (875, 550), (875, 760), (935, 860), (940, 941), (645, 941)],
        [(658, 616), (866, 616), (855, 700), (860, 780), (900, 860), (900, 941), (655, 941)]),
    "RightLedge": (
        [(1488, 360), (1672, 360), (1672, 628), (1500, 628), (1488, 560)],
        [(1508, 492), (1672, 492), (1672, 626), (1512, 626)]),
    "LowerRight": (
        [(1335, 578), (1500, 578), (1500, 628), (1672, 628), (1672, 941), (1335, 941)],
        [(1335, 645), (1672, 645), (1672, 941), (1335, 941)]),
}


def polygon_mask(size, points):
    image = Image.new("L", size, 0)
    ImageDraw.Draw(image).polygon(points, fill=255)
    return np.asarray(image) > 0


def push_pull_fill(rgb, known):
    """Fill unknown pixels with a smooth, mist-like blend of the surrounding known pixels."""
    levels = [(rgb * known[..., None], known.astype(np.float32))]
    while min(levels[-1][1].shape) > 2:
        color, weight = levels[-1]
        h, w = weight.shape
        h2, w2 = (h + 1) // 2, (w + 1) // 2
        pad_c = np.zeros((h2 * 2, w2 * 2, 3), np.float32); pad_c[:h, :w] = color
        pad_w = np.zeros((h2 * 2, w2 * 2), np.float32); pad_w[:h, :w] = weight
        levels.append((pad_c.reshape(h2, 2, w2, 2, 3).sum((1, 3)), pad_w.reshape(h2, 2, w2, 2).sum((1, 3))))
    color, weight = levels[-1]
    estimate = color / np.maximum(weight, 1e-6)[..., None]
    for color, weight in reversed(levels[:-1]):
        h, w = weight.shape
        up = np.asarray(Image.fromarray(np.clip(estimate, 0, 255).astype(np.uint8)).resize((w, h), Image.BILINEAR), np.float32)
        own = color / np.maximum(weight, 1e-6)[..., None]
        blend = np.clip(weight, 0, 1)[..., None]
        estimate = own * blend + up * (1 - blend)
    return estimate


def main():
    source = Image.open(SOURCE).convert("RGBA")
    pixels = np.asarray(source).astype(np.float32)
    rgb = pixels[..., :3]
    height, width = rgb.shape[:2]
    blueness = rgb[..., 2] - (rgb[..., 0] + rgb[..., 1]) / 2
    foreground = blueness < 18

    claimed = np.zeros((height, width), bool)
    masks = {}
    os.makedirs(OUT, exist_ok=True)
    manifest = {"source": "Citadel.png", "width": width, "height": height, "pieces": []}
    for name, (outline, core) in PIECES.items():
        region = polygon_mask((width, height), outline) & ~claimed
        solid = polygon_mask((width, height), core) & ~claimed
        mask = (region & foreground) | solid
        mask = ndimage.binary_closing(mask, iterations=2) & region | solid
        # Fill only small enclosed gaps; large ones (e.g. between bridge trusses) are real background.
        holes = ndimage.binary_fill_holes(mask) & ~mask
        labels, count = ndimage.label(holes)
        if count:
            sizes = ndimage.sum(holes, labels, range(1, count + 1))
            mask |= np.isin(labels, np.nonzero(sizes < 500)[0] + 1)
        labels, count = ndimage.label(mask)
        if count:
            sizes = ndimage.sum(mask, labels, range(1, count + 1))
            mask = np.isin(labels, np.nonzero(sizes >= 300)[0] + 1)
        claimed |= mask
        masks[name] = mask
    for name, mask in masks.items():
        # Soften edges against the backdrop, but overlap neighbouring pieces fully so no seam shows.
        alpha = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7)))
        alpha = np.minimum(alpha, (ndimage.binary_dilation(mask) * 255).astype(np.uint8))
        overlap = ndimage.binary_dilation(mask, iterations=2) & claimed & ~mask
        alpha = np.where(overlap | mask & ndimage.binary_erosion(mask), 255, alpha)
        ys, xs = np.nonzero(alpha > 0)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        piece = pixels.copy(); piece[..., 3] = alpha
        Image.fromarray(piece[y0:y1, x0:x1].astype(np.uint8), "RGBA").save(os.path.join(OUT, name + ".png"))
        manifest["pieces"].append({"name": name, "x": int(x0), "y": int(y0), "width": int(x1 - x0), "height": int(y1 - y0)})

    removed = ndimage.binary_dilation(claimed, iterations=3)
    filled = push_pull_fill(rgb, ~removed)
    # Deepen the filled abyss toward the bottom so it reads as distant mist rather than a smear.
    depth = np.linspace(0, 1, height, dtype=np.float32)[:, None, None] ** 1.5
    filled = filled * (1 - .35 * depth) + np.array([18, 26, 48], np.float32) * .35 * depth
    grain = np.random.default_rng(7).normal(0, 3, filled.shape).astype(np.float32)
    backdrop = np.where(removed[..., None], filled + grain, rgb)
    Image.fromarray(np.clip(backdrop, 0, 255).astype(np.uint8), "RGB").save(BACKDROP)
    with open(os.path.join(OUT, "pieces.json"), "w", encoding="utf-8") as file:
        json.dump(manifest, file, indent=2)
    print("pieces:", ", ".join(piece["name"] for piece in manifest["pieces"]))


if __name__ == "__main__":
    main()
