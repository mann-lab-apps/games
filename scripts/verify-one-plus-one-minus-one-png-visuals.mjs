#!/usr/bin/env node

import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { inflateSync } from "node:zlib";

const repoRoot = resolve(import.meta.dirname, "..");
const qaRoundRoot = process.env.ONE_EQUALS_ONE_QA_ROUND_CAPTURE_DIR ?? "/tmp/one-equals-one-webgl-qa-rounds";
const qaRoundSelectRoot = process.env.ONE_EQUALS_ONE_ROUND_SELECT_CAPTURE_DIR ?? "/tmp/one-equals-one-round-select-pages";
const appStoreRoot =
  process.env.ONE_EQUALS_ONE_APP_STORE_CANDIDATE_DIR ??
  resolve(repoRoot, "prototypes/one-plus-one-minus-one/Builds/AppStoreScreenshots/Candidates");

const mode = parseMode(process.argv.slice(2));
const failures = [];

const qaDevices = [
  { name: "iphone-se", width: 640, height: 1136, mobile: true },
  { name: "iphone-standard", width: 1170, height: 2532, mobile: true },
  { name: "iphone-large", width: 1290, height: 2796, mobile: true },
  { name: "android-20x9", width: 1133, height: 2516, mobile: true },
  { name: "desktop", width: 1440, height: 1024, mobile: false },
];

const appStoreDevices = [
  { name: "iphone-6-5", width: 1284, height: 2778, mobile: true },
  { name: "iphone-6-9", width: 1320, height: 2868, mobile: true },
  { name: "ipad-13", width: 2064, height: 2752, mobile: true },
];

const qaRounds = [1, 5, 8, 9, 16, 30, 50, 75, 90, 100];
const qaPages = [1, 5, 9];
const appStoreShots = [
  "01-round-1-first-stick",
  "02-round-5-cross-multiply",
  "03-round-8-triple-one",
  "04-round-9-star-multiply",
  "05-round-30-medium-expression",
  "06-round-75-equality-puzzle",
  "07-round-100-finale",
  "08-round-select-progression",
];

if (mode === "qa" || mode === "all") {
  for (const round of qaRounds) {
    for (const device of qaDevices) {
      checkPng(resolve(qaRoundRoot, `round-${round}/${device.name}.png`), {
        ...device,
        label: `QA round ${round} ${device.name}`,
        minDarkRatio: 0.0015,
        minLightRatio: 0.35,
        maxContentStartRatio: device.mobile ? 0.3 : 0.35,
      });
    }
  }

  for (const page of qaPages) {
    for (const device of qaDevices) {
      checkPng(resolve(qaRoundSelectRoot, `page-${page}/${device.name}.png`), {
        ...device,
        label: `QA round-select page ${page} ${device.name}`,
        minDarkRatio: 0.0015,
        minLightRatio: 0.35,
        maxContentStartRatio: device.mobile ? 0.3 : 0.35,
      });
    }
  }
}

if (mode === "app-store" || mode === "all") {
  for (const shot of appStoreShots) {
    for (const device of appStoreDevices) {
      checkPng(resolve(appStoreRoot, `${shot}/${device.name}.png`), {
        ...device,
        label: `App Store ${shot} ${device.name}`,
        minDarkRatio: 0.0015,
        minLightRatio: 0.35,
        maxContentStartRatio: 0.3,
        forbidAlpha: true,
      });
    }
  }
}

if (failures.length > 0) {
  for (const failure of failures) {
    console.error(`- ${failure}`);
  }

  console.error(`1 = 1 PNG visual verification failed with ${failures.length} issue(s).`);
  process.exit(1);
}

console.log(`1 = 1 PNG visual verification passed: ${mode}.`);

function parseMode(args) {
  const arg = args[0] ?? "--all";
  if (arg === "--qa") return "qa";
  if (arg === "--app-store") return "app-store";
  if (arg === "--all") return "all";
  if (arg === "--help" || arg === "-h") {
    console.log("Usage: ./scripts/verify-one-plus-one-minus-one-png-visuals.mjs [--qa|--app-store|--all]");
    process.exit(0);
  }

  console.error("Usage: ./scripts/verify-one-plus-one-minus-one-png-visuals.mjs [--qa|--app-store|--all]");
  process.exit(64);
}

function checkPng(pngPath, options) {
  if (!existsSync(pngPath)) {
    failures.push(`${options.label} is missing: ${pngPath}`);
    return;
  }

  let stats;
  try {
    stats = readPngStats(readFileSync(pngPath));
  } catch (error) {
    failures.push(`${options.label} is unreadable: ${error.message}`);
    return;
  }

  if (stats.width !== options.width || stats.height !== options.height) {
    failures.push(
      `${options.label} has unexpected size ${stats.width}x${stats.height}; expected ${options.width}x${options.height}.`,
    );
  }

  if (options.forbidAlpha && stats.hasAlpha) {
    failures.push(`${options.label} must not have an alpha channel.`);
  }

  if (stats.darkRatio < options.minDarkRatio) {
    failures.push(`${options.label} looks blank: dark pixel ratio ${stats.darkRatio.toFixed(5)}.`);
  }

  if (stats.lightRatio < options.minLightRatio) {
    failures.push(`${options.label} looks too dark: light pixel ratio ${stats.lightRatio.toFixed(5)}.`);
  }

  if (stats.firstContentYRatio > options.maxContentStartRatio) {
    failures.push(
      `${options.label} content starts too low: first content y ratio ${stats.firstContentYRatio.toFixed(3)}.`,
    );
  }
}

function readPngStats(pngBytes) {
  const signature = "89504e470d0a1a0a";
  if (pngBytes.subarray(0, 8).toString("hex") !== signature) {
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
  let sourceOffset = 0;
  let dark = 0;
  let light = 0;
  let firstContentY = null;
  let total = 0;

  for (let y = 0; y < height; y++) {
    const filter = inflated[sourceOffset++];
    inflated.copy(current, 0, sourceOffset, sourceOffset + stride);
    sourceOffset += stride;
    unfilterScanline(current, previous, filter, bytesPerPixel);

    for (let x = 0; x < width; x++) {
      const pixel = x * bytesPerPixel;
      const r = current[pixel];
      const g = current[pixel + 1];
      const b = current[pixel + 2];
      const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
      if (luminance < 120) dark++;
      if (luminance > 220) light++;
      if (luminance < 180 && firstContentY === null) {
        firstContentY = y;
      }
      total++;
    }

    current.copy(previous);
  }

  return {
    width,
    height,
    hasAlpha: colorType === 6,
    darkRatio: dark / total,
    lightRatio: light / total,
    firstContentYRatio: (firstContentY ?? height) / height,
  };
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
