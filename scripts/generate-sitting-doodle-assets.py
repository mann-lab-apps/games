#!/usr/bin/env python3
"""Generate lightweight transparent doodle PNG assets for the Sitting prototype."""

from __future__ import annotations

import math
import random
import struct
import zlib
from pathlib import Path


Color = tuple[int, int, int, int]


ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "prototypes/sitting/Assets/Resources/Sitting"

INK: Color = (38, 36, 32, 255)
PAPER: Color = (250, 247, 239, 255)
FLOOR: Color = (232, 225, 210, 255)
WALL: Color = (255, 249, 232, 255)
TEAL: Color = (92, 166, 174, 255)
BLUE: Color = (57, 140, 202, 255)
PANTS: Color = (75, 86, 107, 255)
SKIN: Color = (248, 188, 130, 255)
HAIR: Color = (54, 43, 38, 255)
PINK: Color = (203, 74, 121, 255)
BEIGE: Color = (232, 201, 154, 255)
YELLOW: Color = (255, 218, 82, 255)
WOOD: Color = (214, 148, 65, 255)
WOOD_DARK: Color = (124, 79, 47, 255)
LINE_NOISE = random.Random(7428)


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


def rect(img: list[list[Color]], x: int, y: int, w: int, h: int, color: Color) -> None:
    for yy in range(max(0, y), min(len(img), y + h)):
        row = img[yy]
        for xx in range(max(0, x), min(len(row), x + w)):
            row[xx] = blend(row[xx], color)


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
    wobble: int = 7,
) -> None:
    for _ in range(passes):
        segments = 5
        points = []
        for i in range(segments + 1):
            t = i / segments
            x = int(x1 + (x2 - x1) * t)
            y = int(y1 + (y2 - y1) * t)
            if i not in (0, segments):
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
    loose = [(jitter(x, wobble), jitter(y, wobble)) for x, y in points]
    polygon(img, loose, (fill[0], fill[1], fill[2], 185))
    for _ in range(2):
        warped = [(jitter(x, wobble), jitter(y, wobble)) for x, y in points]
        for i, (x1, y1) in enumerate(warped):
            x2, y2 = warped[(i + 1) % len(warped)]
            sketch_line(img, x1, y1, x2, y2, INK, width, 1, wobble)


def sketch_rect(img: list[list[Color]], x: int, y: int, w: int, h: int, fill: Color, width: int = 6) -> None:
    sketch_polygon(img, [(x, y), (x + w, y), (x + w, y + h), (x, y + h)], fill, width)


def sketch_ellipse(
    img: list[list[Color]],
    cx: int,
    cy: int,
    rx: int,
    ry: int,
    fill: Color,
    width: int = 6,
) -> None:
    ellipse(img, cx, cy, rx, ry, (fill[0], fill[1], fill[2], 175))
    steps = 22
    for _ in range(2):
        prev = None
        for i in range(steps + 1):
            angle = math.tau * i / steps
            x = jitter(int(cx + math.cos(angle) * rx), 5)
            y = jitter(int(cy + math.sin(angle) * ry), 5)
            if prev is not None:
                sketch_line(img, prev[0], prev[1], x, y, INK, width, 1, 4)
            prev = (x, y)


def polygon(img: list[list[Color]], points: list[tuple[int, int]], color: Color) -> None:
    min_x = max(0, min(x for x, _ in points))
    max_x = min(len(img[0]) - 1, max(x for x, _ in points))
    min_y = max(0, min(y for _, y in points))
    max_y = min(len(img) - 1, max(y for _, y in points))
    for y in range(min_y, max_y + 1):
        inside = False
        j = len(points) - 1
        xs: list[int] = []
        for i, (xi, yi) in enumerate(points):
            xj, yj = points[j]
            if (yi > y) != (yj > y):
                x = int((xj - xi) * (y - yi) / max(1, yj - yi) + xi)
                xs.append(x)
            j = i
        xs.sort()
        for i in range(0, len(xs), 2):
            if i + 1 >= len(xs):
                break
            for x in range(max(min_x, xs[i]), min(max_x, xs[i + 1]) + 1):
                put(img, x, y, color)


def outline_polygon(img: list[list[Color]], points: list[tuple[int, int]], fill: Color, width: int = 8) -> None:
    polygon(img, points, fill)
    for i, (x1, y1) in enumerate(points):
        x2, y2 = points[(i + 1) % len(points)]
        line(img, x1, y1, x2, y2, INK, width)


def outline_rect(img: list[list[Color]], x: int, y: int, w: int, h: int, fill: Color, width: int = 8) -> None:
    rect(img, x, y, w, h, fill)
    rect(img, x, y, w, width, INK)
    rect(img, x, y + h - width, w, width, INK)
    rect(img, x, y, width, h, INK)
    rect(img, x + w - width, y, width, h, INK)


def outline_ellipse(img: list[list[Color]], cx: int, cy: int, rx: int, ry: int, fill: Color, width: int = 8) -> None:
    ellipse(img, cx, cy, rx, ry, INK)
    ellipse(img, cx, cy, max(1, rx - width), max(1, ry - width), fill)


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


def employee() -> list[list[Color]]:
    img = canvas(420, 620)
    sketch_ellipse(img, 210, 144, 62, 70, SKIN, 7)
    sketch_polygon(img, [(145, 118), (176, 82), (238, 76), (282, 118), (268, 148), (150, 150)], HAIR, 6, 12)
    sketch_polygon(img, [(142, 242), (278, 238), (290, 404), (132, 412)], BLUE, 7, 11)
    sketch_line(img, 142, 292, 98, 382, SKIN, 18, 2, 11)
    sketch_line(img, 278, 292, 322, 382, SKIN, 18, 2, 11)
    sketch_line(img, 176, 392, 168, 540, PANTS, 28, 2, 10)
    sketch_line(img, 246, 392, 256, 540, PANTS, 28, 2, 10)
    sketch_line(img, 124, 550, 198, 550, INK, 9, 2, 9)
    sketch_line(img, 224, 550, 300, 550, INK, 9, 2, 9)
    sketch_line(img, 160, 274, 260, 274, (116, 190, 226, 170), 5, 1, 8)
    return img


def customer() -> list[list[Color]]:
    img = canvas(460, 620)
    sketch_ellipse(img, 238, 142, 54, 62, SKIN, 7)
    sketch_polygon(img, [(172, 116), (218, 78), (286, 94), (302, 136), (250, 154), (182, 150)], (118, 74, 47, 255), 6, 13)
    sketch_polygon(img, [(164, 240), (302, 240), (308, 392), (156, 392)], PINK, 7, 12)
    sketch_line(img, 172, 292, 112, 352, SKIN, 17, 2, 12)
    sketch_line(img, 294, 294, 354, 350, SKIN, 17, 2, 12)
    sketch_line(img, 182, 388, 128, 510, BEIGE, 25, 2, 13)
    sketch_line(img, 266, 388, 330, 510, BEIGE, 25, 2, 13)
    sketch_rect(img, 74, 352, 68, 82, YELLOW, 6)
    sketch_line(img, 98, 352, 126, 314, INK, 5, 1, 6)
    sketch_line(img, 82, 520, 168, 520, INK, 8, 2, 8)
    sketch_line(img, 292, 520, 380, 520, INK, 8, 2, 8)
    return img


def desk() -> list[list[Color]]:
    img = canvas(920, 430)
    sketch_polygon(img, [(122, 86), (790, 82), (870, 224), (54, 226)], (240, 196, 114, 255), 7, 15)
    sketch_polygon(img, [(80, 216), (842, 216), (828, 292), (90, 292)], WOOD, 7, 12)
    sketch_rect(img, 335, 236, 252, 32, (255, 236, 180, 255), 5)
    sketch_rect(img, 356, 88, 138, 92, TEAL, 6)
    sketch_rect(img, 548, 124, 112, 42, (68, 75, 80, 255), 5)
    sketch_rect(img, 706, 108, 46, 62, (116, 164, 80, 255), 5)
    sketch_line(img, 728, 104, 708, 72, (116, 164, 80, 255), 8, 1, 8)
    sketch_line(img, 732, 104, 762, 76, (116, 164, 80, 255), 8, 1, 8)
    sketch_line(img, 150, 288, 150, 400, WOOD_DARK, 18, 2, 8)
    sketch_line(img, 770, 288, 770, 400, WOOD_DARK, 18, 2, 8)
    sketch_line(img, 132, 92, 780, 88, (255, 226, 154, 210), 5, 1, 12)
    return img


def lobby() -> list[list[Color]]:
    img = canvas(1080, 1920, WALL)
    rect(img, 0, 780, 1080, 1140, FLOOR)
    rect(img, 0, 560, 1080, 220, (235, 230, 215, 165))
    sketch_line(img, 0, 542, 1080, 542, INK, 5, 1, 12)
    sketch_rect(img, 424, 184, 232, 330, (228, 216, 180, 255), 6)
    sketch_rect(img, 472, 334, 132, 178, (124, 158, 136, 255), 5)
    sketch_rect(img, 110, 266, 170, 132, (232, 224, 192, 255), 5)
    sketch_rect(img, 814, 266, 158, 132, (188, 212, 184, 255), 5)
    sketch_line(img, 154, 780, 930, 780, (214, 198, 166, 255), 4, 1, 10)
    sketch_line(img, 64, 1012, 226, 930, (161, 122, 68, 255), 8, 1, 12)
    sketch_line(img, 1016, 1012, 856, 930, (161, 122, 68, 255), 8, 1, 12)
    return img


def main() -> None:
    save_png(employee(), OUT_DIR / "employee.png")
    save_png(customer(), OUT_DIR / "customer.png")
    save_png(desk(), OUT_DIR / "desk.png")
    save_png(lobby(), OUT_DIR / "lobby.png")


if __name__ == "__main__":
    main()
