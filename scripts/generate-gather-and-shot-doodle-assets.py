#!/usr/bin/env python3
"""Generate lightweight transparent doodle PNG assets for Gather & Shot."""

from __future__ import annotations

import math
import random
import struct
import zlib
from pathlib import Path


Color = tuple[int, int, int, int]

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "prototypes/gather-and-shot/Assets/Resources/GatherAndShot"

INK: Color = (40, 39, 36, 255)
PAPER: Color = (250, 247, 239, 255)
SNOW: Color = (239, 249, 255, 230)
SNOW_SHADOW: Color = (159, 211, 226, 150)
BLUE: Color = (73, 150, 202, 230)
TEAL: Color = (70, 151, 174, 230)
PINK: Color = (236, 94, 123, 230)
PURPLE: Color = (107, 92, 130, 230)
SKIN: Color = (248, 188, 130, 230)
HAIR: Color = (54, 43, 38, 230)
LINE_NOISE = random.Random(10337)


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
    sketch_line(img, cx - 9, body_y + body_ry - 1, cx - leg_spread, 120, INK, 7)
    sketch_line(img, cx + 9, body_y + body_ry - 1, cx + leg_spread, 120, INK, 7)
    sketch_line(img, cx - body_rx + 4, body_y - 8, cx - 42 if runner else cx - 32, body_y + 16, INK, 6)
    sketch_line(img, cx + body_rx - 4, body_y - 9, cx + 42 if runner else cx + 32, body_y + 12, INK, 6)
    if heavy:
        sketch_line(img, cx - 22, body_y + 2, cx + 22, body_y - 4, INK, 4, 1, 4)


def draw_player() -> list[list[Color]]:
    img = canvas(128, 128)
    doodle_person(img, BLUE)
    sketch_ellipse(img, 89, 57, 12, 10, SNOW, 4)
    sketch_line(img, 70, 64, 84, 58, INK, 5)
    return img


def draw_walker() -> list[list[Color]]:
    img = canvas(128, 128)
    doodle_person(img, TEAL)
    return img


def draw_runner() -> list[list[Color]]:
    img = canvas(128, 128)
    doodle_person(img, PINK, runner=True)
    sketch_line(img, 22, 111, 8, 116, INK, 3, 1, 4)
    sketch_line(img, 106, 111, 122, 116, INK, 3, 1, 4)
    return img


def draw_heavy() -> list[list[Color]]:
    img = canvas(128, 128)
    doodle_person(img, PURPLE, heavy=True)
    return img


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
    print(f"Generated {len(assets)} Gather & Shot doodle assets in {OUT_DIR}")


if __name__ == "__main__":
    main()
