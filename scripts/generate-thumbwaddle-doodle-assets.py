#!/usr/bin/env python3
"""Generate lightweight sketch PNG assets for the Thumbwaddle prototype."""

from __future__ import annotations

import math
import random
import struct
import zlib
from pathlib import Path


Color = tuple[int, int, int, int]

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "prototypes/walking/Assets/Resources/Thumbwaddle"
LINE_NOISE = random.Random(20260831)

INK: Color = (38, 37, 34, 255)
INK_SOFT: Color = (63, 72, 75, 190)
PAPER: Color = (250, 247, 239, 255)
ICE: Color = (231, 248, 250, 255)
ICE_BLUE: Color = (173, 221, 236, 255)
ICE_DARK: Color = (95, 153, 181, 230)
SKY: Color = (224, 244, 249, 235)
SEA: Color = (139, 195, 216, 210)
SNOW: Color = (255, 254, 247, 255)
ORANGE: Color = (239, 152, 55, 255)
WARM_LINE: Color = (247, 181, 71, 220)
BLACK: Color = (35, 39, 40, 255)
WHITE: Color = (255, 253, 246, 255)
CRACK: Color = (69, 127, 154, 235)


def canvas(width: int, height: int, color: Color = (0, 0, 0, 0)) -> list[list[Color]]:
    return [[color for _ in range(width)] for _ in range(height)]


def blend(dst: Color, src: Color) -> Color:
    sr, sg, sb, sa = src
    if sa <= 0:
        return dst
    if sa >= 255:
        return src
    dr, dg, db, da = dst
    a = sa / 255
    ia = 1 - a
    out_a = sa + da * ia
    return (
        int(sr * a + dr * ia),
        int(sg * a + dg * ia),
        int(sb * a + db * ia),
        int(out_a),
    )


def put(img: list[list[Color]], x: int, y: int, color: Color) -> None:
    if 0 <= y < len(img) and 0 <= x < len(img[0]):
        img[y][x] = blend(img[y][x], color)


def rect(img: list[list[Color]], x: int, y: int, w: int, h: int, color: Color) -> None:
    for yy in range(max(0, y), min(len(img), y + h)):
        for xx in range(max(0, x), min(len(img[0]), x + w)):
            put(img, xx, yy, color)


def ellipse(img: list[list[Color]], cx: int, cy: int, rx: int, ry: int, color: Color) -> None:
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if ((x - cx) / max(1, rx)) ** 2 + ((y - cy) / max(1, ry)) ** 2 <= 1:
                put(img, x, y, color)


def polygon(img: list[list[Color]], points: list[tuple[int, int]], color: Color) -> None:
    min_x = max(0, min(x for x, _ in points))
    max_x = min(len(img[0]) - 1, max(x for x, _ in points))
    min_y = max(0, min(y for _, y in points))
    max_y = min(len(img) - 1, max(y for _, y in points))
    for y in range(min_y, max_y + 1):
        xs: list[int] = []
        previous = points[-1]
        for current in points:
            x1, y1 = previous
            x2, y2 = current
            if (y1 > y) != (y2 > y):
                xs.append(int((x2 - x1) * (y - y1) / max(1, y2 - y1) + x1))
            previous = current
        xs.sort()
        for i in range(0, len(xs), 2):
            if i + 1 >= len(xs):
                break
            for x in range(max(min_x, xs[i]), min(max_x, xs[i + 1]) + 1):
                put(img, x, y, color)


def line(img: list[list[Color]], x1: int, y1: int, x2: int, y2: int, color: Color, width: int = 5) -> None:
    min_x, max_x = sorted((x1, x2))
    min_y, max_y = sorted((y1, y2))
    dx = x2 - x1
    dy = y2 - y1
    length_sq = max(1, dx * dx + dy * dy)
    radius = width / 2
    for y in range(min_y - width, max_y + width + 1):
        for x in range(min_x - width, max_x + width + 1):
            t = max(0, min(1, ((x - x1) * dx + (y - y1) * dy) / length_sq))
            px = x1 + t * dx
            py = y1 + t * dy
            if math.hypot(x - px, y - py) <= radius:
                put(img, x, y, color)


def jitter(value: int, amount: int) -> int:
    return value + LINE_NOISE.randint(-amount, amount)


def sketch_line(
    img: list[list[Color]],
    x1: int,
    y1: int,
    x2: int,
    y2: int,
    color: Color = INK,
    width: int = 5,
    passes: int = 2,
    wobble: int = 7,
) -> None:
    for _ in range(passes):
        points: list[tuple[int, int]] = []
        for i in range(6):
            t = i / 5
            x = int(x1 + (x2 - x1) * t)
            y = int(y1 + (y2 - y1) * t)
            if i not in (0, 5):
                x = jitter(x, wobble)
                y = jitter(y, wobble)
            points.append((x, y))
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            line(img, ax, ay, bx, by, color, width)


def sketch_polygon(
    img: list[list[Color]],
    points: list[tuple[int, int]],
    fill: Color,
    width: int = 6,
    wobble: int = 8,
) -> None:
    polygon(img, [(jitter(x, wobble), jitter(y, wobble)) for x, y in points], (fill[0], fill[1], fill[2], 210))
    for _ in range(2):
        warped = [(jitter(x, wobble), jitter(y, wobble)) for x, y in points]
        for i, (x1, y1) in enumerate(warped):
            x2, y2 = warped[(i + 1) % len(warped)]
            sketch_line(img, x1, y1, x2, y2, INK, width, 1, wobble)


def sketch_ellipse(img: list[list[Color]], cx: int, cy: int, rx: int, ry: int, fill: Color, width: int = 7) -> None:
    ellipse(img, cx, cy, rx, ry, (fill[0], fill[1], fill[2], 225))
    for _ in range(2):
        previous = None
        for i in range(25):
            angle = math.tau * i / 24
            x = jitter(int(cx + math.cos(angle) * rx), 5)
            y = jitter(int(cy + math.sin(angle) * ry), 5)
            if previous is not None:
                sketch_line(img, previous[0], previous[1], x, y, INK, width, 1, 4)
            previous = (x, y)


def save_png(img: list[list[Color]], path: Path) -> None:
    height = len(img)
    width = len(img[0])
    raw = b"".join(b"\x00" + b"".join(bytes(px) for px in row) for row in img)

    def chunk(kind: bytes, data: bytes) -> bytes:
        return (
            struct.pack(">I", len(data))
            + kind
            + data
            + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)
        )

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw, 9))
        + chunk(b"IEND", b"")
    )


def penguin_pose(step: str) -> list[list[Color]]:
    img = canvas(512, 640)
    left_step = step == "left"
    right_step = step == "right"
    stumble = step == "stumble"
    happy = step == "happy"
    lean = -28 if stumble else -18 if left_step else 18 if right_step else 0
    head_x = 256 + lean // 3
    body_x = 256 + lean // 4

    sketch_ellipse(img, 168 if stumble else 190 if left_step else 178, 562 if left_step or stumble else 538, 74, 34, ORANGE, 7)
    sketch_ellipse(img, 344 if stumble else 322 if right_step else 334, 562 if right_step or stumble else 538, 74, 34, ORANGE, 7)
    sketch_ellipse(img, body_x, 382 + (8 if happy else 0), 128, 178 - (12 if happy else 0), BLACK, 9)
    sketch_ellipse(img, body_x, 468, 48, 32, (238, 243, 238, 220), 5)
    sketch_ellipse(img, head_x, 172 + (-8 if happy else 0), 104, 98, BLACK, 9)

    left_wing = [(body_x - 136, 274), (body_x - 196, 360), (body_x - 156, 474), (body_x - 96, 394)]
    right_wing = [(body_x + 136, 274), (body_x + 196, 360), (body_x + 156, 474), (body_x + 96, 394)]
    if happy:
        left_wing = [(body_x - 118, 284), (body_x - 214, 232), (body_x - 196, 334), (body_x - 110, 386)]
        right_wing = [(body_x + 118, 284), (body_x + 214, 232), (body_x + 196, 334), (body_x + 110, 386)]
    elif stumble:
        left_wing = [(body_x - 132, 278), (body_x - 218, 330), (body_x - 178, 438), (body_x - 94, 386)]
        right_wing = [(body_x + 126, 276), (body_x + 182, 390), (body_x + 132, 486), (body_x + 88, 394)]

    sketch_polygon(img, left_wing, BLACK, 8, 11)
    sketch_polygon(img, right_wing, BLACK, 8, 11)
    sketch_line(img, head_x - 52, 132, head_x - 18, 108, INK_SOFT, 7, 1, 8)
    sketch_line(img, head_x + 18, 116, head_x + 54, 95, INK_SOFT, 7, 1, 8)
    sketch_line(img, body_x - 64, 310, body_x - 18, 300, (75, 82, 84, 155), 6, 1, 8)
    sketch_line(img, body_x + 16, 292, body_x + 72, 306, (75, 82, 84, 155), 6, 1, 8)
    if happy:
        sketch_line(img, 142, 92, 106, 62, WARM_LINE, 6, 1, 6)
        sketch_line(img, 370, 92, 408, 58, WARM_LINE, 6, 1, 6)
        sketch_line(img, 256, 68, 256, 30, WARM_LINE, 6, 1, 5)
    if stumble:
        sketch_line(img, 106, 188, 62, 172, CRACK, 6, 1, 8)
        sketch_line(img, 84, 220, 46, 226, CRACK, 6, 1, 8)
        sketch_line(img, 392, 128, 444, 104, CRACK, 6, 1, 8)
    return img


def iceberg(level: int) -> list[list[Color]]:
    img = canvas(640, 420)
    sketch_polygon(img, [(74, 286), (162, 148), (272, 188), (354, 80), (512, 286)], ICE_BLUE, 8, 14)
    sketch_polygon(img, [(118, 278), (210, 118), (286, 188), (356, 104), (476, 278)], SNOW, 7, 12)
    sketch_polygon(img, [(74, 286), (512, 286), (568, 338), (46, 346)], (151, 209, 226, 255), 8, 15)
    sketch_line(img, 116, 316, 514, 314, ICE_DARK, 6, 1, 12)
    sketch_line(img, 174, 232, 238, 286, CRACK, 5, 1, 10)
    sketch_line(img, 384, 188, 344, 284, CRACK, 5, 1, 9)
    if level >= 1:
        sketch_line(img, 282, 128, 272, 226, CRACK, 8, 2, 9)
        sketch_line(img, 272, 226, 230, 286, CRACK, 7, 1, 8)
        sketch_line(img, 272, 226, 326, 276, CRACK, 6, 1, 8)
    if level >= 2:
        sketch_line(img, 180, 170, 150, 258, CRACK, 8, 2, 10)
        sketch_line(img, 432, 218, 494, 296, CRACK, 8, 2, 10)
        sketch_line(img, 96, 342, 532, 340, (69, 127, 154, 180), 9, 1, 15)
    return img


def ice_field_background() -> list[list[Color]]:
    img = canvas(1600, 1600, PAPER)
    rect(img, 0, 0, 1600, 1600, (235, 249, 251, 255))
    for i in range(10):
        y = 110 + i * 92 + LINE_NOISE.randint(-18, 18)
        color = (189, 220, 224, 54 if i % 3 else 34)
        sketch_line(img, -80, y, 1680, y + LINE_NOISE.randint(-22, 22), color, 3, 1, 14)
    for i in range(7):
        x = 120 + i * 220 + LINE_NOISE.randint(-30, 30)
        sketch_line(img, x, -90, x + LINE_NOISE.randint(-46, 46), 1690, (180, 214, 218, 42), 3, 1, 18)
    for _ in range(26):
        x = LINE_NOISE.randint(40, 1560)
        y = LINE_NOISE.randint(40, 1560)
        length = LINE_NOISE.randint(18, 58)
        angle = LINE_NOISE.uniform(-0.8, 0.8)
        sketch_line(
            img,
            x,
            y,
            int(x + math.cos(angle) * length),
            int(y + math.sin(angle) * length),
            (126, 177, 189, LINE_NOISE.randint(30, 62)),
            LINE_NOISE.randint(2, 3),
            1,
            6,
        )
    for _ in range(18):
        x = LINE_NOISE.randint(40, 1560)
        y = LINE_NOISE.randint(40, 1560)
        sketch_line(img, x, y, x + LINE_NOISE.randint(8, 24), y + LINE_NOISE.randint(-16, 16), (252, 254, 250, 125), 4, 1, 4)
    return img


def polar_backdrop() -> list[list[Color]]:
    img = canvas(1600, 1000)
    rect(img, 0, 0, 1600, 410, SKY)
    rect(img, 0, 318, 1600, 168, (211, 237, 244, 185))
    sketch_line(img, 0, 402, 1600, 390, (94, 151, 177, 120), 6, 1, 22)
    sketch_polygon(img, [(72, 410), (190, 292), (342, 414)], (246, 253, 252, 205), 6, 15)
    sketch_polygon(img, [(296, 414), (470, 238), (660, 414)], (235, 249, 252, 210), 6, 18)
    sketch_polygon(img, [(932, 416), (1114, 262), (1300, 416)], (243, 252, 252, 205), 6, 18)
    sketch_polygon(img, [(1250, 414), (1410, 286), (1542, 414)], (229, 246, 251, 205), 6, 16)
    for _ in range(22):
        x = LINE_NOISE.randint(40, 1560)
        y = LINE_NOISE.randint(48, 300)
        sketch_line(
            img,
            x,
            y,
            x + LINE_NOISE.randint(24, 86),
            y + LINE_NOISE.randint(-12, 12),
            (114, 172, 192, LINE_NOISE.randint(60, 95)),
            4,
            1,
            7,
        )
    return img


def snow_puff() -> list[list[Color]]:
    img = canvas(360, 220)
    sketch_ellipse(img, 104, 144, 72, 38, SNOW, 5)
    sketch_ellipse(img, 188, 124, 92, 54, SNOW, 5)
    sketch_ellipse(img, 262, 150, 70, 36, SNOW, 5)
    sketch_line(img, 70, 172, 310, 174, (156, 199, 211, 120), 5, 1, 9)
    return img


def small_ice_floe() -> list[list[Color]]:
    img = canvas(420, 260)
    sketch_polygon(img, [(48, 154), (102, 82), (214, 58), (346, 126), (370, 198), (126, 214)], ICE_BLUE, 7, 15)
    sketch_polygon(img, [(80, 146), (124, 96), (216, 78), (314, 132), (252, 166), (138, 170)], SNOW, 5, 10)
    sketch_line(img, 96, 192, 338, 182, ICE_DARK, 5, 1, 12)
    sketch_line(img, 198, 96, 240, 162, CRACK, 5, 1, 8)
    return img


def ice_chip() -> list[list[Color]]:
    img = canvas(220, 180)
    sketch_polygon(img, [(46, 128), (92, 42), (160, 100), (134, 148)], ICE_BLUE, 6, 10)
    sketch_polygon(img, [(74, 116), (96, 62), (134, 100), (118, 126)], SNOW, 4, 7)
    return img


def write_meta(path: Path, guid: str) -> None:
    if path.exists():
        return

    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spritePixelsToUnits: 100
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag:
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""",
        encoding="utf-8",
    )


def write_folder_meta(path: Path, guid: str) -> None:
    meta_path = path.with_suffix(path.suffix + ".meta")
    if meta_path.exists():
        return

    meta_path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
""",
        encoding="utf-8",
    )


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    resources_dir = OUT_DIR.parent
    if not resources_dir.with_suffix(resources_dir.suffix + ".meta").exists():
        write_folder_meta(resources_dir, "e12fb7ac64f64f55b3e7d0dbb5f38b41")
    write_folder_meta(OUT_DIR, "9d7fae5c9a1d4492b03d4e7f05412f4d")

    assets = {
        "penguin_back_idle.png": (penguin_pose("idle"), "5c76153d38184d488653e90f1f8d1481"),
        "penguin_back_left_step.png": (penguin_pose("left"), "279d79e57f01452cb53ff40ce6544f27"),
        "penguin_back_right_step.png": (penguin_pose("right"), "769a4b2a598546df9d678169d8768895"),
        "penguin_back_stumble.png": (penguin_pose("stumble"), "336c60763936414ab7b8f4af061a8a3b"),
        "penguin_back_happy.png": (penguin_pose("happy"), "ed447c43da974d069ff695065885e5a7"),
        "iceberg_intact.png": (iceberg(0), "cc83d6e07f084227b454bbd87c17fd5a"),
        "iceberg_cracked_1.png": (iceberg(1), "d95da2fe5e254b46a56426d330f6a10a"),
        "iceberg_cracked_2.png": (iceberg(2), "d5d8a663b312410aa849249898995678"),
        "ice_field_background.png": (ice_field_background(), "a8564bf0266c4746afe9aa5a886c46a2"),
        "polar_backdrop.png": (polar_backdrop(), "88e1091922ca449aae5f2dc7d91a2e17"),
        "snow_puff.png": (snow_puff(), "c988a38331da4085bc5600bc55c4dc8d"),
        "ice_floe_small.png": (small_ice_floe(), "ca31025f26b04c12a380a7d0f5a13267"),
        "ice_chip.png": (ice_chip(), "7f6044430f414b2397df245e0b20cf03"),
    }
    for file_name, (image, guid) in assets.items():
        path = OUT_DIR / file_name
        save_png(image, path)
        write_meta(path.with_suffix(path.suffix + ".meta"), guid)


if __name__ == "__main__":
    main()
