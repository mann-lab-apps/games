#!/usr/bin/env node

import { existsSync, readFileSync } from "node:fs";
import { basename, resolve } from "node:path";
import { inflateSync } from "node:zlib";

const repoRoot = resolve(import.meta.dirname, "..");
const projectRoot = resolve(repoRoot, "prototypes/one-plus-one-minus-one");
const sourceIcon = resolve(projectRoot, "Assets/_Project/Art/AppIcon-1024.png");
const webglIcon = resolve(projectRoot, "Builds/WebGL/one-plus-one-minus-one/app-icon.png");
const targets = [
  { label: "source icon", path: sourceIcon },
  { label: "WebGL icon", path: webglIcon, optional: true },
];
const sizes = [180, 120, 64, 32];
const failures = [];

for (const target of targets) {
  if (!existsSync(target.path)) {
    if (!target.optional) failures.push(`Missing ${target.label}: ${target.path}`);
    continue;
  }

  let png;
  try {
    png = readPng(readFileSync(target.path));
  } catch (error) {
    failures.push(`${target.label} is unreadable: ${error.message}`);
    continue;
  }

  if (png.width !== 1024 || png.height !== 1024) {
    failures.push(`${target.label} must be 1024x1024 before small-icon checks; found ${png.width}x${png.height}.`);
    continue;
  }

  checkIconStats(`${target.label} full-size`, measureSmallIcon(png, 1024), {
    minDarkRatio: 0.025,
    maxDarkRatio: 0.18,
    minContentWidthRatio: 0.72,
    minContentHeightRatio: 0.68,
    minRegionDarkRatio: 0.006,
    minCenterDarkRatio: 0.0025,
  });

  for (const size of sizes) {
    checkIconStats(`${target.label} ${size}px`, measureSmallIcon(png, size), {
      minDarkRatio: size <= 32 ? 0.02 : 0.024,
      maxDarkRatio: 0.22,
      minContentWidthRatio: size <= 32 ? 0.66 : 0.7,
      minContentHeightRatio: size <= 32 ? 0.62 : 0.66,
      minRegionDarkRatio: size <= 32 ? 0.004 : 0.005,
      minCenterDarkRatio: size <= 32 ? 0.0015 : 0.002,
    });
  }
}

if (failures.length > 0) {
  for (const failure of failures) console.error(`- ${failure}`);
  console.error(`1 = 1 small-icon visual verification failed with ${failures.length} issue(s).`);
  process.exit(1);
}

console.log(`1 = 1 small-icon visual verification passed for ${targets.map(target => basename(target.path)).join(", ")}.`);

function checkIconStats(label, stats, policy) {
  if (stats.darkRatio < policy.minDarkRatio) {
    failures.push(`${label} may be too faint: dark ratio ${stats.darkRatio.toFixed(4)}.`);
  }

  if (stats.darkRatio > policy.maxDarkRatio) {
    failures.push(`${label} may be too dense: dark ratio ${stats.darkRatio.toFixed(4)}.`);
  }

  if (stats.contentWidthRatio < policy.minContentWidthRatio) {
    failures.push(`${label} content is too narrow: width ratio ${stats.contentWidthRatio.toFixed(3)}.`);
  }

  if (stats.contentHeightRatio < policy.minContentHeightRatio) {
    failures.push(`${label} content is too short: height ratio ${stats.contentHeightRatio.toFixed(3)}.`);
  }

  for (const region of ["left", "center", "right"]) {
    const minRatio = region === "center" ? policy.minCenterDarkRatio : policy.minRegionDarkRatio;
    if (stats.regionDarkRatio[region] < minRatio) {
      failures.push(
        `${label} has weak ${region} content: dark ratio ${stats.regionDarkRatio[region].toFixed(4)}.`,
      );
    }
  }
}

function measureSmallIcon(png, targetSize) {
  const pixels = downsample(png, targetSize);
  let dark = 0;
  let total = 0;
  let minX = targetSize;
  let maxX = -1;
  let minY = targetSize;
  let maxY = -1;
  const regionDark = { left: 0, center: 0, right: 0 };
  const regionTotal = { left: 0, center: 0, right: 0 };

  for (let y = 0; y < targetSize; y++) {
    for (let x = 0; x < targetSize; x++) {
      const luminance = pixels[y * targetSize + x];
      const isDark = luminance < 210;
      const isContent = luminance < 245;
      const region = x < targetSize / 3 ? "left" : x >= (targetSize * 2) / 3 ? "right" : "center";

      if (isDark) {
        dark++;
        regionDark[region]++;
      }

      if (isContent) {
        minX = Math.min(minX, x);
        maxX = Math.max(maxX, x);
        minY = Math.min(minY, y);
        maxY = Math.max(maxY, y);
      }

      regionTotal[region]++;
      total++;
    }
  }

  return {
    darkRatio: dark / total,
    contentWidthRatio: maxX >= minX ? (maxX - minX + 1) / targetSize : 0,
    contentHeightRatio: maxY >= minY ? (maxY - minY + 1) / targetSize : 0,
    regionDarkRatio: {
      left: regionDark.left / regionTotal.left,
      center: regionDark.center / regionTotal.center,
      right: regionDark.right / regionTotal.right,
    },
  };
}

function downsample(png, targetSize) {
  const luminance = new Float64Array(targetSize * targetSize);
  const samples = new Uint32Array(targetSize * targetSize);

  for (let y = 0; y < png.height; y++) {
    const targetY = Math.min(targetSize - 1, Math.floor((y * targetSize) / png.height));
    for (let x = 0; x < png.width; x++) {
      const targetX = Math.min(targetSize - 1, Math.floor((x * targetSize) / png.width));
      const source = (y * png.width + x) * 4;
      const destination = targetY * targetSize + targetX;
      const r = png.rgba[source];
      const g = png.rgba[source + 1];
      const b = png.rgba[source + 2];
      luminance[destination] += 0.2126 * r + 0.7152 * g + 0.0722 * b;
      samples[destination]++;
    }
  }

  for (let i = 0; i < luminance.length; i++) {
    luminance[i] = samples[i] > 0 ? luminance[i] / samples[i] : 255;
  }

  return luminance;
}

function readPng(pngBytes) {
  if (pngBytes.subarray(0, 8).toString("hex") !== "89504e470d0a1a0a") {
    throw new Error("not a PNG");
  }

  let offset = 8;
  let width = 0;
  let height = 0;
  let bitDepth = 0;
  let colorType = 0;
  const idatChunks = [];

  while (offset + 12 <= pngBytes.length) {
    const length = pngBytes.readUInt32BE(offset);
    const type = pngBytes.subarray(offset + 4, offset + 8).toString("ascii");
    const dataStart = offset + 8;
    const dataEnd = dataStart + length;
    if (dataEnd + 4 > pngBytes.length) throw new Error("PNG chunk is truncated");

    if (type === "IHDR") {
      width = pngBytes.readUInt32BE(dataStart);
      height = pngBytes.readUInt32BE(dataStart + 4);
      bitDepth = pngBytes[dataStart + 8];
      colorType = pngBytes[dataStart + 9];
    } else if (type === "IDAT") {
      idatChunks.push(pngBytes.subarray(dataStart, dataEnd));
    } else if (type === "IEND") {
      break;
    }

    offset = dataEnd + 4;
  }

  if (bitDepth !== 8 || (colorType !== 2 && colorType !== 6)) {
    throw new Error(`unsupported PNG format: bitDepth=${bitDepth}, colorType=${colorType}`);
  }

  const bytesPerPixel = colorType === 6 ? 4 : 3;
  const stride = width * bytesPerPixel;
  const inflated = inflateSync(Buffer.concat(idatChunks));
  const previous = Buffer.alloc(stride);
  const current = Buffer.alloc(stride);
  const rgba = new Uint8Array(width * height * 4);
  let sourceOffset = 0;

  for (let y = 0; y < height; y++) {
    const filter = inflated[sourceOffset++];
    inflated.copy(current, 0, sourceOffset, sourceOffset + stride);
    sourceOffset += stride;
    unfilterScanline(current, previous, filter, bytesPerPixel);

    for (let x = 0; x < width; x++) {
      const source = x * bytesPerPixel;
      const destination = (y * width + x) * 4;
      rgba[destination] = current[source];
      rgba[destination + 1] = current[source + 1];
      rgba[destination + 2] = current[source + 2];
      rgba[destination + 3] = colorType === 6 ? current[source + 3] : 255;
    }

    current.copy(previous);
  }

  return { width, height, rgba };
}

function unfilterScanline(scanline, previous, filter, bytesPerPixel) {
  for (let i = 0; i < scanline.length; i++) {
    const left = i >= bytesPerPixel ? scanline[i - bytesPerPixel] : 0;
    const up = previous[i];
    const upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;

    if (filter === 1) {
      scanline[i] = (scanline[i] + left) & 0xff;
    } else if (filter === 2) {
      scanline[i] = (scanline[i] + up) & 0xff;
    } else if (filter === 3) {
      scanline[i] = (scanline[i] + Math.floor((left + up) / 2)) & 0xff;
    } else if (filter === 4) {
      scanline[i] = (scanline[i] + paethPredictor(left, up, upLeft)) & 0xff;
    } else if (filter !== 0) {
      throw new Error(`unsupported PNG filter: ${filter}`);
    }
  }
}

function paethPredictor(left, up, upLeft) {
  const estimate = left + up - upLeft;
  const leftDistance = Math.abs(estimate - left);
  const upDistance = Math.abs(estimate - up);
  const upLeftDistance = Math.abs(estimate - upLeft);
  if (leftDistance <= upDistance && leftDistance <= upLeftDistance) return left;
  if (upDistance <= upLeftDistance) return up;
  return upLeft;
}
