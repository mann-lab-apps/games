#!/usr/bin/env python3
"""Generate lightweight transparent character PNG assets for Stop & Snow."""

from __future__ import annotations

import math
import random
import shutil
import struct
import uuid
import zlib
from pathlib import Path


Color = tuple[int, int, int, int]

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "prototypes/gather-and-shot/Assets/Resources/GatherAndShot"
APP_ICON_DIR = ROOT / "prototypes/gather-and-shot/Assets/_Project/Art/AppStore"
POLISHED_ICON = APP_ICON_DIR / "Concepts/AppIcon-polished-snow-survival-concept-1024.png"

INK: Color = (40, 39, 36, 255)
PAPER: Color = (250, 247, 239, 255)
SNOW: Color = (239, 249, 255, 230)
SNOW_SHADOW: Color = (159, 211, 226, 150)
BLUE: Color = (73, 150, 202, 230)
TEAL: Color = (70, 151, 174, 230)
PINK: Color = (236, 94, 123, 230)
PURPLE: Color = (107, 92, 130, 230)
PARKA: Color = (38, 156, 170, 240)
PARKA_DARK: Color = (30, 94, 112, 245)
SKIN: Color = (248, 188, 130, 230)
HAIR: Color = (54, 43, 38, 230)
LINE_NOISE = random.Random(10337)
TEXTURE_NOISE = random.Random(24019)


def unity_guid() -> str:
    return uuid.uuid4().hex


def write_folder_meta_if_missing(path: Path) -> None:
    meta_path = path.with_suffix(path.suffix + ".meta") if path.suffix else Path(f"{path}.meta")
    if meta_path.exists():
        return

    meta_path.write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid()}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
""",
        encoding="utf-8",
    )


def write_texture_meta_if_missing(path: Path) -> None:
    meta_path = Path(f"{path}.meta")
    if meta_path.exists():
        return

    meta_path.write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid()}
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
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 0
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
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
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""",
        encoding="utf-8",
    )


def canvas(width: int, height: int, color: Color = (0, 0, 0, 0)) -> list[list[Color]]:
    return [[color for _ in range(width)] for _ in range(height)]


def blend(dst: Color, src: Color) -> Color:
    sr, sg, sb, sa = src
    if sa >= 255:
        return src
    if sa <= 0:
        return dst
    dr, dg, db, da = dst
    a = sa / 255
    ia = 1 - a
    out_a = sa + da * ia
    if out_a <= 0:
        return (0, 0, 0, 0)
    return (
        int(sr * a + dr * ia),
        int(sg * a + dg * ia),
        int(sb * a + db * ia),
        int(out_a),
    )


def put(img: list[list[Color]], x: int, y: int, color: Color) -> None:
    if 0 <= y < len(img) and 0 <= x < len(img[0]):
        img[y][x] = blend(img[y][x], color)


def ellipse(img: list[list[Color]], cx: int, cy: int, rx: int, ry: int, color: Color) -> None:
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if ((x - cx) / max(1, rx)) ** 2 + ((y - cy) / max(1, ry)) ** 2 <= 1:
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
    wobble: int = 6,
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


def sketch_ellipse(
    img: list[list[Color]],
    cx: int,
    cy: int,
    rx: int,
    ry: int,
    fill: Color,
    width: int = 5,
) -> None:
    ellipse(img, cx + 4, cy + 6, rx, max(2, ry - 2), SNOW_SHADOW)
    ellipse(img, cx, cy, rx, ry, fill)
    for _ in range(2):
        prev: tuple[int, int] | None = None
        for i in range(25):
            angle = math.tau * i / 24
            x = jitter(int(cx + math.cos(angle) * rx), 4)
            y = jitter(int(cy + math.sin(angle) * ry), 4)
            if prev is not None:
                sketch_line(img, prev[0], prev[1], x, y, INK, width, 1, 3)
            prev = (x, y)


def soft_ellipse(
    img: list[list[Color]],
    cx: int,
    cy: int,
    rx: int,
    ry: int,
    fill: Color,
    outline: Color = INK,
    width: int = 4,
) -> None:
    ellipse(img, cx + 3, cy + 5, rx, max(2, ry - 2), SNOW_SHADOW)
    ellipse(img, cx, cy, rx, ry, fill)
    for ring in range(width):
        steps = max(30, int((rx + ry) * 1.1))
        for i in range(steps):
            angle = math.tau * i / steps
            x = int(cx + math.cos(angle) * (rx - ring * 0.36))
            y = int(cy + math.sin(angle) * (ry - ring * 0.36))
            put(img, x, y, outline)


def curved_snow_shadow(img: list[list[Color]], cx: int, cy: int, rx: int, ry: int) -> None:
    for i in range(18):
        angle = math.pi * (0.12 + i / 30)
        x = int(cx - rx * 0.18 + math.cos(angle) * rx * 0.58)
        y = int(cy + ry * 0.2 + math.sin(angle) * ry * 0.32)
        ellipse(img, x, y, 2, 1, (118, 185, 208, 80))


def doodle_person(img: list[list[Color]], tint: Color, heavy: bool = False, runner: bool = False) -> None:
    cx = len(img[0]) // 2
    head_y = 32 if runner else 34
    body_y = 72 if runner else 78
    body_rx = 24 if heavy else 18 if runner else 21
    body_ry = 28 if heavy else 23
    leg_spread = 19 if runner else 13

    sketch_ellipse(img, cx, head_y, 16 if heavy else 13, 14 if heavy else 12, SKIN, 4)
    ellipse(img, cx, head_y - 8, 15 if heavy else 12, 7, HAIR)
    sketch_ellipse(img, cx, body_y, body_rx, body_ry, tint, 5)
    sketch_line(img, cx - 9, body_y + body_ry - 1, cx - leg_spread, 120, PARKA, 7)
    sketch_line(img, cx + 9, body_y + body_ry - 1, cx + leg_spread, 120, PARKA, 7)
    sketch_line(img, cx - body_rx + 4, body_y - 8, cx - 42 if runner else cx - 32, body_y + 16, PARKA, 6)
    sketch_line(img, cx + body_rx - 4, body_y - 9, cx + 42 if runner else cx + 32, body_y + 12, PARKA, 6)
    if heavy:
        sketch_line(img, cx - 22, body_y + 2, cx + 22, body_y - 4, INK, 4, 1, 4)


def doodle_snowman(img: list[list[Color]], accent: Color, heavy: bool = False, runner: bool = False) -> None:
    cx = len(img[0]) // 2
    head_y = 34 if runner else 32
    belly_y = 83 if heavy else 78 if runner else 80
    base_y = 101 if heavy else 97
    head_rx = 16 if heavy else 13
    body_rx = 30 if heavy else 22 if runner else 24
    body_ry = 25 if heavy else 22
    base_rx = 35 if heavy else 25 if runner else 28
    base_ry = 18 if heavy else 15

    sketch_ellipse(img, cx, base_y, base_rx, base_ry, SNOW, 5)
    sketch_ellipse(img, cx, belly_y, body_rx, body_ry, SNOW, 5)
    sketch_ellipse(img, cx, head_y, head_rx, 14 if heavy else 12, SNOW, 4)
    sketch_line(img, cx - 15, belly_y - 11, cx + 18, belly_y - 5, accent, 6, 1, 4)
    sketch_line(img, cx - 8, head_y + 10, cx + 11, head_y + 12, accent, 5, 1, 3)
    ellipse(img, cx - 5, head_y - 2, 3, 3, INK)
    ellipse(img, cx + 6, head_y - 2, 3, 3, INK)
    sketch_line(img, cx - 4, head_y + 6, cx + 8, head_y + 4, INK, 2, 1, 2)
    ellipse(img, cx - 6, belly_y - 2, 3, 3, accent)
    ellipse(img, cx + 5, belly_y + 8, 3, 3, accent)

    if runner:
        sketch_line(img, cx - 18, belly_y - 5, cx - 44, belly_y + 12, INK, 4, 1, 4)
        sketch_line(img, cx + 18, belly_y - 5, cx + 42, belly_y + 8, INK, 4, 1, 4)
        sketch_line(img, 22, 111, 8, 116, accent, 3, 1, 4)
        sketch_line(img, 106, 111, 122, 116, accent, 3, 1, 4)
    elif heavy:
        sketch_line(img, cx - 27, belly_y, cx - 50, belly_y + 4, INK, 5, 1, 4)
        sketch_line(img, cx + 27, belly_y, cx + 50, belly_y + 4, INK, 5, 1, 4)
        sketch_line(img, cx - 24, base_y + 2, cx + 24, base_y - 3, accent, 5, 1, 4)
    else:
        sketch_line(img, cx - 21, belly_y - 4, cx - 42, belly_y + 8, INK, 4, 1, 4)
        sketch_line(img, cx + 21, belly_y - 4, cx + 42, belly_y + 8, INK, 4, 1, 4)


def polished_player(img: list[list[Color]], pose: str) -> None:
    cx = 64
    lean = -3 if pose == "throw" else 3 if pose == "move" else 0
    bob = -3 if pose == "move" else 2 if pose == "gather" else 0
    face_y = 36 + bob
    torso_y = 76 + bob
    hood_y = face_y + 4

    if pose == "move":
        line(img, 29, 104, 53, 113, PARKA_DARK, 10)
        line(img, 75, 104, 102, 112, PARKA_DARK, 10)
        sketch_line(img, 18, 100, 40, 94, (88, 166, 206, 110), 4, 1, 4)
    else:
        line(img, 43, 101, 51, 116, PARKA_DARK, 10)
        line(img, 78, 101, 91, 114, PARKA_DARK, 10)

    soft_ellipse(img, cx + lean, torso_y, 29, 29, PARKA, INK, 4)
    soft_ellipse(img, cx + lean, hood_y, 28, 24, PARKA, INK, 4)
    ellipse(img, cx + lean, face_y, 21, 16, SKIN)
    soft_ellipse(img, cx + lean, face_y - 21, 23, 15, PARKA, INK, 4)
    ellipse(img, cx + lean, face_y - 34, 11, 8, SNOW)

    sketch_line(img, cx + lean - 26, face_y + 10, cx + lean - 38, face_y + 18, PARKA, 8, 1, 3)
    sketch_line(img, cx + lean + 22, face_y + 10, cx + lean + 36, face_y + 18, PARKA, 8, 1, 3)
    sketch_line(img, cx + lean - 17, face_y + 1, cx + lean - 8, face_y + 9, INK, 3, 1, 1)
    sketch_line(img, cx + lean + 14, face_y + 1, cx + lean + 5, face_y + 9, INK, 3, 1, 1)
    sketch_line(img, cx + lean - 6, face_y + 15, cx + lean + 5, face_y + 14, INK, 2, 1, 1)

    if pose == "gather":
        sketch_line(img, 42, 75, 54, 89, PARKA_DARK, 8, 1, 3)
        sketch_line(img, 86, 75, 74, 89, PARKA_DARK, 8, 1, 3)
        soft_ellipse(img, 64, 98, 20, 17, SNOW, INK, 4)
        curved_snow_shadow(img, 64, 98, 20, 17)
        for ox, oy, r in ((38, 98, 4), (91, 98, 5), (53, 116, 3), (73, 119, 3)):
            ellipse(img, ox, oy, r, r, SNOW)
    elif pose == "throw":
        sketch_line(img, 43, 70, 24, 58, PARKA_DARK, 9, 1, 3)
        sketch_line(img, 86, 67, 103, 50, PARKA_DARK, 9, 1, 3)
        soft_ellipse(img, 109, 44, 11, 10, SNOW, INK, 3)
        sketch_line(img, 95, 48, 116, 36, (88, 166, 206, 120), 3, 1, 2)
    elif pose == "hit":
        soft_ellipse(img, 36, 78, 9, 8, (239, 126, 87, 210), INK, 3)
        soft_ellipse(img, 96, 64, 8, 7, (239, 126, 87, 190), INK, 3)
        sketch_line(img, 41, 76, 22, 66, PARKA_DARK, 8, 1, 3)
        sketch_line(img, 88, 74, 105, 84, PARKA_DARK, 8, 1, 3)
    else:
        held_x = 90 if pose == "idle" else 88
        sketch_line(img, 43, 72, 29, 86, PARKA_DARK, 8, 1, 3)
        sketch_line(img, 84, 70, held_x, 86, PARKA_DARK, 8, 1, 3)
        soft_ellipse(img, held_x + 7, 87, 11, 10, SNOW, INK, 3)


def draw_player(pose: str = "idle") -> list[list[Color]]:
    img = canvas(128, 128)
    polished_player(img, pose)
    return img


def draw_walker() -> list[list[Color]]:
    img = canvas(128, 128)
    polished_snowman(img, TEAL)
    return img


def draw_runner() -> list[list[Color]]:
    img = canvas(128, 128)
    polished_snowman(img, PINK, runner=True)
    return img


def draw_heavy() -> list[list[Color]]:
    img = canvas(128, 128)
    polished_snowman(img, PURPLE, heavy=True)
    return img


def polished_snowman(img: list[list[Color]], accent: Color, heavy: bool = False, runner: bool = False) -> None:
    cx = 64
    head_y = 32 if runner else 30
    torso_y = 74 if runner else 78
    base_y = 102 if runner else 104
    head_rx = 18 if heavy else 15
    head_ry = 15 if heavy else 13
    torso_rx = 34 if heavy else 25 if runner else 27
    torso_ry = 29 if heavy else 22
    base_rx = 39 if heavy else 26 if runner else 30
    base_ry = 21 if heavy else 16

    if runner:
        sketch_line(img, 20, 101, 4, 106, (236, 94, 123, 120), 5, 1, 3)
        sketch_line(img, 108, 100, 126, 105, (236, 94, 123, 120), 5, 1, 3)

    soft_ellipse(img, cx, base_y, base_rx, base_ry, SNOW, INK, 4)
    soft_ellipse(img, cx, torso_y, torso_rx, torso_ry, SNOW, INK, 4)
    soft_ellipse(img, cx, head_y, head_rx, head_ry, SNOW, INK, 4)
    curved_snow_shadow(img, cx, torso_y, torso_rx, torso_ry)
    curved_snow_shadow(img, cx, base_y, base_rx, base_ry)

    sketch_line(img, cx - 23, head_y + 20, cx + 22, head_y + 24, accent, 7, 1, 3)
    sketch_line(img, cx + 9, head_y + 25, cx + 27, head_y + 38, accent, 6, 1, 3)
    ellipse(img, cx - 7, head_y - 2, 3, 4, INK)
    ellipse(img, cx + 8, head_y - 2, 3, 4, INK)
    sketch_line(img, cx - 6, head_y + 8, cx + 7, head_y + 7, INK, 2, 1, 1)
    ellipse(img, cx - 7, torso_y - 4, 3, 3, accent)
    ellipse(img, cx + 7, torso_y + 8, 3, 3, accent)

    if heavy:
        sketch_line(img, cx - 33, torso_y - 1, cx - 55, torso_y + 10, INK, 6, 1, 4)
        sketch_line(img, cx + 33, torso_y - 1, cx + 55, torso_y + 10, INK, 6, 1, 4)
        soft_ellipse(img, cx, head_y - 22, 23, 11, accent, INK, 4)
        ellipse(img, cx, head_y - 34, 11, 8, SNOW)
        sketch_line(img, cx - 30, base_y, cx + 30, base_y - 5, accent, 6, 1, 4)
    elif runner:
        sketch_line(img, cx - 23, torso_y - 4, cx - 48, torso_y + 13, INK, 5, 1, 4)
        sketch_line(img, cx + 23, torso_y - 4, cx + 46, torso_y + 10, INK, 5, 1, 4)
        soft_ellipse(img, cx, head_y - 20, 21, 10, accent, INK, 4)
        ellipse(img, cx, head_y - 32, 10, 7, SNOW)
    else:
        sketch_line(img, cx - 25, torso_y - 4, cx - 47, torso_y + 9, INK, 5, 1, 4)
        sketch_line(img, cx + 25, torso_y - 4, cx + 47, torso_y + 9, INK, 5, 1, 4)
        soft_ellipse(img, cx, head_y - 20, 20, 10, accent, INK, 4)
        ellipse(img, cx, head_y - 31, 10, 7, SNOW)


def draw_snowball() -> list[list[Color]]:
    img = canvas(96, 96)
    sketch_ellipse(img, 48, 47, 25, 22, SNOW, 5)
    sketch_line(img, 32, 43, 49, 36, SNOW_SHADOW, 4, 1, 4)
    return img


def draw_snowdrift() -> list[list[Color]]:
    img = canvas(128, 128)
    sketch_ellipse(img, 46, 74, 31, 22, SNOW, 5)
    sketch_ellipse(img, 77, 70, 34, 25, SNOW, 5)
    sketch_ellipse(img, 66, 55, 23, 18, SNOW, 4)
    return img


def draw_big_snowdrift() -> list[list[Color]]:
    img = canvas(160, 160)
    sketch_ellipse(img, 58, 99, 44, 30, SNOW, 6)
    sketch_ellipse(img, 98, 96, 46, 32, SNOW, 6)
    sketch_ellipse(img, 80, 75, 35, 26, SNOW, 5)
    sketch_ellipse(img, 113, 76, 26, 20, SNOW, 4)
    sketch_line(img, 38, 106, 74, 96, SNOW_SHADOW, 5, 1, 5)
    sketch_line(img, 78, 112, 126, 104, SNOW_SHADOW, 5, 1, 5)
    sketch_line(img, 49, 84, 102, 72, SNOW_SHADOW, 4, 1, 5)
    return img


def draw_puff() -> list[list[Color]]:
    img = canvas(96, 96)
    for cx, cy, rx, ry in ((38, 42, 18, 12), (54, 42, 16, 13), (48, 54, 24, 14), (29, 54, 11, 8)):
        sketch_ellipse(img, cx, cy, rx, ry, (239, 249, 255, 170), 3)
    return img


def blit_scaled(src: list[list[Color]], dst: list[list[Color]], left: int, top: int, scale: int) -> None:
    for sy, row in enumerate(src):
        for sx, color in enumerate(row):
            if color[3] <= 0:
                continue
            for yy in range(scale):
                for xx in range(scale):
                    put(dst, left + sx * scale + xx, top + sy * scale + yy, color)


def draw_paper_texture(img: list[list[Color]]) -> None:
    for _ in range(4200):
        x = TEXTURE_NOISE.randrange(len(img[0]))
        y = TEXTURE_NOISE.randrange(len(img))
        tint = TEXTURE_NOISE.randrange(-7, 8)
        alpha = TEXTURE_NOISE.randrange(16, 38)
        put(img, x, y, (244 + tint, 239 + tint, 225 + tint, alpha))

    for y in range(132, 960, 128):
        start = TEXTURE_NOISE.randrange(-80, 120)
        end = start + TEXTURE_NOISE.randrange(620, 1040)
        sketch_line(img, start, y + TEXTURE_NOISE.randrange(-22, 22), end, y + TEXTURE_NOISE.randrange(-22, 22), (183, 224, 232, 86), 11, 1, 8)


def draw_app_icon() -> list[list[Color]]:
    img = canvas(1024, 1024, PAPER)
    draw_paper_texture(img)

    sketch_ellipse(img, 772, 272, 96, 74, SNOW, 13)
    sketch_ellipse(img, 836, 320, 68, 52, SNOW, 11)
    sketch_ellipse(img, 712, 336, 58, 46, SNOW, 10)
    sketch_line(img, 710, 286, 834, 255, SNOW_SHADOW, 11, 1, 10)

    blit_scaled(draw_walker(), img, 90, 132, 2)
    blit_scaled(draw_runner(), img, 748, 644, 2)
    blit_scaled(draw_player(), img, 288, 332, 3)
    blit_scaled(draw_snowball(), img, 578, 302, 2)
    blit_scaled(draw_snowball(), img, 596, 478, 2)
    blit_scaled(draw_big_snowdrift(), img, 88, 666, 2)

    sketch_line(img, 506, 430, 632, 362, (40, 39, 36, 180), 9, 1, 9)
    sketch_line(img, 522, 462, 650, 530, (40, 39, 36, 150), 7, 1, 9)
    return img


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
    with path.open("wb") as fp:
        fp.write(b"\x89PNG\r\n\x1a\n")
        fp.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
        fp.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        fp.write(chunk(b"IEND", b""))


def main() -> None:
    assets = {
        "player.png": draw_player(),
        "player_idle.png": draw_player("idle"),
        "player_move.png": draw_player("move"),
        "player_gather.png": draw_player("gather"),
        "player_throw.png": draw_player("throw"),
        "player_hit.png": draw_player("hit"),
        "walker.png": draw_walker(),
        "runner.png": draw_runner(),
        "heavy.png": draw_heavy(),
        "snowball.png": draw_snowball(),
        "snowdrift.png": draw_snowdrift(),
        "puff.png": draw_puff(),
        "big_snowdrift.png": draw_big_snowdrift(),
    }
    for name, image in assets.items():
        save_png(image, OUT_DIR / name)
        write_texture_meta_if_missing(OUT_DIR / name)

    APP_ICON_DIR.mkdir(parents=True, exist_ok=True)
    write_folder_meta_if_missing(APP_ICON_DIR)
    icon_path = APP_ICON_DIR / "AppIcon-1024.png"
    if POLISHED_ICON.exists():
        shutil.copyfile(POLISHED_ICON, icon_path)
    else:
        save_png(draw_app_icon(), icon_path)
    write_texture_meta_if_missing(icon_path)
    print(f"Generated {len(assets)} Stop & Snow character assets in {OUT_DIR}")
    print(f"Generated Stop & Snow app icon at {icon_path}")


if __name__ == "__main__":
    main()
