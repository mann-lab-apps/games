#!/usr/bin/env node

import fs from "node:fs";
import { deflateSync, inflateSync } from "node:zlib";

const [inputPath, outputPath = inputPath] = process.argv.slice(2);
if (!inputPath) {
  console.error("Usage: strip-png-alpha.mjs <input.png> [output.png]");
  process.exit(64);
}

const input = fs.readFileSync(inputPath);
const png = readPng(input);
if (png.bitDepth !== 8 || png.interlace !== 0 || (png.colorType !== 6 && png.colorType !== 2)) {
  throw new Error(`Unsupported PNG format: bitDepth=${png.bitDepth}, colorType=${png.colorType}, interlace=${png.interlace}`);
}

if (png.colorType === 2) {
  if (outputPath !== inputPath) fs.writeFileSync(outputPath, input);
  console.log(`PNG already has no alpha channel: ${inputPath}`);
  process.exit(0);
}

const rgba = inflateSync(Buffer.concat(png.idatChunks));
const rgbRows = [];
const sourceStride = png.width * 4;
const targetStride = png.width * 3;
const previous = Buffer.alloc(sourceStride);
const current = Buffer.alloc(sourceStride);
let sourceOffset = 0;

for (let y = 0; y < png.height; y += 1) {
  const filter = rgba[sourceOffset++];
  rgba.copy(current, 0, sourceOffset, sourceOffset + sourceStride);
  sourceOffset += sourceStride;
  unfilterScanline(current, previous, filter, 4);

  const row = Buffer.alloc(1 + targetStride);
  row[0] = 0;
  for (let x = 0; x < png.width; x += 1) {
    row[1 + x * 3] = current[x * 4];
    row[1 + x * 3 + 1] = current[x * 4 + 1];
    row[1 + x * 3 + 2] = current[x * 4 + 2];
  }

  rgbRows.push(row);
  current.copy(previous);
}

const output = Buffer.concat([
  Buffer.from("89504e470d0a1a0a", "hex"),
  makeChunk("IHDR", makeIhdr(png.width, png.height, 8, 2)),
  makeChunk("IDAT", deflateSync(Buffer.concat(rgbRows))),
  makeChunk("IEND", Buffer.alloc(0)),
]);
fs.writeFileSync(outputPath, output);
console.log(`Stripped PNG alpha channel: ${outputPath}`);

function readPng(bytes) {
  if (bytes.subarray(0, 8).toString("hex") !== "89504e470d0a1a0a") {
    throw new Error("Input is not a PNG.");
  }

  let offset = 8;
  const png = { width: 0, height: 0, bitDepth: 0, colorType: 0, interlace: 0, idatChunks: [] };
  while (offset + 12 <= bytes.length) {
    const length = bytes.readUInt32BE(offset);
    const type = bytes.subarray(offset + 4, offset + 8).toString("ascii");
    const dataStart = offset + 8;
    const dataEnd = dataStart + length;
    if (dataEnd + 4 > bytes.length) throw new Error("PNG chunk is truncated.");

    if (type === "IHDR") {
      png.width = bytes.readUInt32BE(dataStart);
      png.height = bytes.readUInt32BE(dataStart + 4);
      png.bitDepth = bytes[dataStart + 8];
      png.colorType = bytes[dataStart + 9];
      png.interlace = bytes[dataStart + 12];
    } else if (type === "IDAT") {
      png.idatChunks.push(bytes.subarray(dataStart, dataEnd));
    } else if (type === "IEND") {
      break;
    }

    offset = dataEnd + 4;
  }

  return png;
}

function makeIhdr(width, height, bitDepth, colorType) {
  const data = Buffer.alloc(13);
  data.writeUInt32BE(width, 0);
  data.writeUInt32BE(height, 4);
  data[8] = bitDepth;
  data[9] = colorType;
  data[10] = 0;
  data[11] = 0;
  data[12] = 0;
  return data;
}

function makeChunk(type, data) {
  const typeBytes = Buffer.from(type, "ascii");
  const chunk = Buffer.alloc(12 + data.length);
  chunk.writeUInt32BE(data.length, 0);
  typeBytes.copy(chunk, 4);
  data.copy(chunk, 8);
  chunk.writeUInt32BE(crc32(Buffer.concat([typeBytes, data])), 8 + data.length);
  return chunk;
}

function unfilterScanline(scanline, previous, filter, bytesPerPixel) {
  for (let i = 0; i < scanline.length; i += 1) {
    const left = i >= bytesPerPixel ? scanline[i - bytesPerPixel] : 0;
    const up = previous[i];
    const upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
    if (filter === 1) scanline[i] = (scanline[i] + left) & 0xff;
    else if (filter === 2) scanline[i] = (scanline[i] + up) & 0xff;
    else if (filter === 3) scanline[i] = (scanline[i] + Math.floor((left + up) / 2)) & 0xff;
    else if (filter === 4) scanline[i] = (scanline[i] + paethPredictor(left, up, upLeft)) & 0xff;
    else if (filter !== 0) throw new Error(`Unsupported PNG filter: ${filter}`);
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

function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit += 1) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }

  return (crc ^ 0xffffffff) >>> 0;
}
