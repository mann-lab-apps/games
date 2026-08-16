#!/usr/bin/env python3
"""Generate lightweight transparent doodle PNG assets for the Sitting prototype."""

from __future__ import annotations

import math
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
    line(img, 155, 315, 105, 425, SKIN, 34)
    line(img, 265, 315, 315, 425, SKIN, 34)
    outline_rect(img, 138, 244, 144, 190, BLUE, 10)
    outline_ellipse(img, 210, 165, 76, 86, SKIN, 10)
    ellipse(img, 210, 125, 84, 54, HAIR)
    line(img, 170, 370, 166, 555, PANTS, 50)
    line(img, 250, 370, 254, 555, PANTS, 50)
    outline_rect(img, 122, 548, 86, 38, (55, 59, 63, 255), 8)
    outline_rect(img, 214, 548, 86, 38, (55, 59, 63, 255), 8)
    line(img, 180, 295, 238, 294, (95, 183, 232, 255), 8)
    return img


def customer() -> list[list[Color]]:
    img = canvas(460, 620)
    line(img, 173, 338, 116, 476, BEIGE, 44)
    line(img, 247, 338, 334, 484, BEIGE, 44)
    outline_rect(img, 166, 244, 150, 150, PINK, 10)
    outline_ellipse(img, 240, 160, 66, 72, SKIN, 10)
    ellipse(img, 215, 122, 80, 54, (133, 79, 46, 255))
    line(img, 178, 286, 108, 360, SKIN, 32)
    line(img, 290, 292, 355, 346, SKIN, 30)
    outline_rect(img, 62, 356, 76, 84, YELLOW, 8)
    line(img, 90, 354, 116, 318, INK, 6)
    outline_rect(img, 76, 486, 94, 42, (248, 248, 238, 255), 8)
    outline_rect(img, 304, 492, 94, 42, (248, 248, 238, 255), 8)
    return img


def desk() -> list[list[Color]]:
    img = canvas(920, 430)
    outline_polygon(img, [(120, 70), (800, 70), (875, 235), (45, 235)], (242, 195, 111, 255), 10)
    outline_rect(img, 72, 220, 776, 82, WOOD, 10)
    outline_rect(img, 280, 245, 360, 42, (255, 234, 172, 255), 7)
    outline_rect(img, 355, 72, 145, 98, TEAL, 8)
    outline_rect(img, 530, 122, 128, 42, (70, 80, 88, 255), 7)
    outline_ellipse(img, 705, 150, 22, 16, (82, 96, 106, 255), 6)
    outline_rect(img, 720, 88, 48, 64, (116, 164, 80, 255), 7)
    line(img, 744, 88, 725, 56, (116, 164, 80, 255), 12)
    line(img, 746, 88, 770, 58, (116, 164, 80, 255), 12)
    line(img, 145, 292, 145, 402, WOOD_DARK, 30)
    line(img, 775, 292, 775, 402, WOOD_DARK, 30)
    line(img, 130, 78, 790, 78, (255, 224, 147, 255), 8)
    return img


def lobby() -> list[list[Color]]:
    img = canvas(1080, 1920, WALL)
    rect(img, 0, 780, 1080, 1140, FLOOR)
    rect(img, 0, 560, 1080, 220, (235, 230, 215, 255))
    rect(img, 0, 520, 1080, 24, INK)
    outline_rect(img, 420, 180, 240, 340, (227, 214, 175, 255), 8)
    outline_rect(img, 470, 330, 140, 190, (119, 155, 136, 255), 8)
    outline_rect(img, 100, 250, 185, 150, (229, 222, 192, 255), 7)
    outline_rect(img, 805, 250, 175, 150, (185, 210, 182, 255), 7)
    outline_ellipse(img, 170, 330, 28, 38, (141, 171, 123, 255), 6)
    line(img, 54, 1000, 230, 920, (161, 122, 68, 255), 16)
    line(img, 1026, 1000, 850, 920, (161, 122, 68, 255), 16)
    line(img, 150, 780, 930, 780, (214, 198, 166, 255), 4)
    for x in range(0, 1080, 180):
        line(img, x, 780, x + 70, 1920, (218, 207, 185, 170), 3)
    for y in range(900, 1920, 170):
        line(img, 0, y, 1080, y, (218, 207, 185, 170), 3)
    return img


def main() -> None:
    save_png(employee(), OUT_DIR / "employee.png")
    save_png(customer(), OUT_DIR / "customer.png")
    save_png(desk(), OUT_DIR / "desk.png")
    save_png(lobby(), OUT_DIR / "lobby.png")


if __name__ == "__main__":
    main()
