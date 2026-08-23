import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const uploadDir = resolve(repoRoot, "prototypes/2048-blink/Assets/_Project/Art/AppStore/Upload");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const tmpDir = resolve("/tmp", "2048-blink-app-store-screenshots");

const devices = [
  { name: "iPhone-6.5", width: 1284, height: 2778 },
  { name: "iPad-13", width: 2064, height: 2752 },
];

const shots = [
  {
    file: "01-start-board.png",
    score: 0,
    best: 256,
    cross: "Cross 1/4",
    board: [
      [0, 0, 0, 0],
      [0, 2, 0, 0],
      [0, 0, 2, 0],
      [0, 0, 0, 0],
    ],
    hidden: { row: 0, col: 2 },
  },
  {
    file: "02-cross-memory.png",
    score: 140,
    best: 512,
    cross: "Cross 2/4",
    board: [
      [4, 8, 16, 2],
      [2, 32, 64, 4],
      [0, 8, 4, 0],
      [2, 0, 0, 0],
    ],
    hidden: { row: 1, col: 3 },
  },
  {
    file: "03-game-over.png",
    score: 1852,
    best: 1024,
    cross: "Cross 4/4",
    board: [
      [2, 4, 8, 16],
      [32, 64, 128, 256],
      [4, 8, 16, 32],
      [2, 4, 8, 64],
    ],
    hidden: { row: 3, col: 1 },
    gameOver: true,
    result: ["Tile 256", "Score 1852"],
  },
];

function unityGuid() {
  return randomUUID().replaceAll("-", "");
}

function writeFolderMetaIfMissing(dirPath) {
  const metaPath = `${dirPath}.meta`;
  if (existsSync(metaPath)) return;
  writeFileSync(metaPath, `fileFormatVersion: 2
guid: ${unityGuid()}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
`);
}

function writeTextureMetaIfMissing(imagePath) {
  const metaPath = `${imagePath}.meta`;
  if (existsSync(metaPath)) return;
  writeFileSync(metaPath, `fileFormatVersion: 2
guid: ${unityGuid()}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
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
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
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
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
  userData:
  assetBundleName:
  assetBundleVariant:
`);
}

function tileClass(value) {
  if (value === 0) return "empty";
  if (value >= 1024) return "n1024";
  if (value >= 128) return `n${value}`;
  return `n${value}`;
}

function tileHtml(value, row, col, hidden) {
  const isHidden = hidden.row === row || hidden.col === col;
  const classes = ["tile", tileClass(value)];
  if (isHidden) classes.push(value === 0 ? "hidden-empty" : "hidden");
  return `<div class="${classes.join(" ")}">${value === 0 || isHidden ? "" : value}</div>`;
}

function buildBoard(shot) {
  return shot.board
    .map((row, rowIndex) => row.map((value, colIndex) => tileHtml(value, rowIndex, colIndex, shot.hidden)).join(""))
    .join("");
}

function buildHtml(device, shot) {
  const isPad = device.name.startsWith("iPad");
  const boardSize = isPad ? 1370 : 1030;
  const gap = isPad ? 24 : 18;
  const padding = isPad ? 38 : 28;
  const top = isPad ? 190 : 290;
  const titleFont = isPad ? 112 : 88;
  const headerFont = isPad ? 44 : 40;
  const tileFont = isPad ? 98 : 76;
  const resultWidth = isPad ? 780 : 650;
  const resultHeight = isPad ? 520 : 470;
  const safeX = isPad ? 120 : 72;

  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    * { box-sizing: border-box; }
    html, body {
      width: ${device.width}px;
      height: ${device.height}px;
      margin: 0;
      overflow: hidden;
      background: #faf7ef;
      font-family: ui-rounded, "SF Pro Rounded", "Arial Rounded MT Bold", ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Helvetica Neue", Arial, sans-serif;
      color: #2b2926;
    }
    body {
      position: relative;
      background:
        radial-gradient(circle at 80% 17%, rgba(255, 253, 247, 0.72), transparent 18%),
        linear-gradient(180deg, #fbf8f0 0%, #f2eadc 100%);
    }
    .title {
      position: absolute;
      left: ${safeX}px;
      right: ${safeX}px;
      top: ${top - 132}px;
      text-align: center;
      font-size: ${titleFont}px;
      line-height: 1;
      font-weight: 900;
      letter-spacing: 0;
    }
    .header {
      position: absolute;
      left: ${safeX}px;
      right: ${safeX}px;
      top: ${top - 2}px;
      height: ${isPad ? 82 : 76}px;
      display: grid;
      grid-template-columns: 1fr 1.18fr 1fr;
      align-items: center;
      font-size: ${headerFont}px;
      font-weight: 800;
    }
    .header div:nth-child(2) { text-align: center; }
    .header div:nth-child(3) { text-align: right; }
    .board {
      position: absolute;
      left: 50%;
      top: ${top + (isPad ? 320 : 520)}px;
      width: ${boardSize}px;
      height: ${boardSize}px;
      transform: translateX(-50%);
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      grid-template-rows: repeat(4, 1fr);
      gap: ${gap}px;
      padding: ${padding}px;
      background: #d7ccbd;
      border: ${isPad ? 7 : 6}px solid #2b2926;
      border-radius: ${isPad ? 34 : 28}px;
      box-shadow: 0 ${isPad ? 15 : 12}px 0 rgba(43, 41, 38, 0.08);
    }
    .tile {
      position: relative;
      display: grid;
      place-items: center;
      min-width: 0;
      min-height: 0;
      overflow: hidden;
      border: ${isPad ? 5 : 4}px solid #2b2926;
      border-radius: ${isPad ? 24 : 20}px;
      background: #fffdf7;
      color: #2b2926;
      font-size: ${tileFont}px;
      line-height: 1;
      font-weight: 900;
      letter-spacing: 0;
      box-shadow: inset 0 -${isPad ? 8 : 6}px 0 rgba(43, 41, 38, 0.06);
    }
    .empty { background: #e5dbcb; }
    .n2 { background: #fffdf7; }
    .n4 { background: #efddb7; }
    .n8 { background: #f1ae5f; }
    .n16 { background: #ee8e57; color: #fffdf8; }
    .n32 { background: #e26756; color: #fffdf8; }
    .n64 { background: #d44d43; color: #fffdf8; }
    .n128 { background: #6893ae; color: #fffdf8; font-size: ${tileFont * 0.82}px; }
    .n256 { background: #4c80a2; color: #fffdf8; font-size: ${tileFont * 0.82}px; }
    .n512 { background: #5b976f; color: #fffdf8; font-size: ${tileFont * 0.82}px; }
    .n1024 { background: #7c68a8; color: #fffdf8; font-size: ${tileFont * 0.7}px; }
    .hidden {
      color: transparent;
      background: #2f3335;
      border-color: #242628;
      box-shadow: inset 0 -${isPad ? 10 : 8}px 0 rgba(255, 255, 255, 0.07);
    }
    .hidden-empty {
      background: #ccd0cd;
    }
    .hidden::after {
      content: "";
      position: absolute;
      inset: ${isPad ? 34 : 26}px;
      border-radius: ${isPad ? 18 : 14}px;
      background:
        repeating-linear-gradient(135deg, rgba(255,255,255,0.18) 0, rgba(255,255,255,0.18) ${isPad ? 8 : 6}px, transparent ${isPad ? 8 : 6}px, transparent ${isPad ? 24 : 18}px);
    }
    .result {
      position: absolute;
      left: 50%;
      top: ${top + (isPad ? 682 : 874)}px;
      width: ${resultWidth}px;
      height: ${resultHeight}px;
      transform: translateX(-50%);
      display: grid;
      grid-template-rows: 1fr 1.1fr 0.9fr;
      align-items: center;
      justify-items: center;
      padding: ${isPad ? 46 : 38}px;
      background: rgba(255, 253, 247, 0.97);
      border: ${isPad ? 6 : 5}px solid #2b2926;
      box-shadow: 0 ${isPad ? 20 : 16}px 0 rgba(43, 41, 38, 0.10);
      font-weight: 900;
      z-index: 2;
    }
    .result-title {
      font-size: ${isPad ? 78 : 64}px;
      line-height: 1;
    }
    .result-score {
      text-align: center;
      font-size: ${isPad ? 50 : 42}px;
      line-height: 1.22;
    }
    .again {
      display: grid;
      place-items: center;
      width: ${isPad ? 310 : 260}px;
      height: ${isPad ? 98 : 82}px;
      border: ${isPad ? 5 : 4}px solid #2b2926;
      background: #fffdf7;
      font-size: ${isPad ? 42 : 36}px;
    }
  </style>
</head>
<body>
  <main>
    <div class="title">2048 Blink</div>
    <div class="header">
      <div>Score ${shot.score}</div>
      <div>${shot.cross}</div>
      <div>Best ${shot.best}</div>
    </div>
    <div class="board" aria-label="2048 Blink board">${buildBoard(shot)}</div>
    ${shot.gameOver ? `<div class="result"><div class="result-title">Game Over</div><div class="result-score">${shot.result.join("<br>")}</div><div class="again">Again</div></div>` : ""}
  </main>
</body>
</html>`;
}

function renderShot(device, shot) {
  const deviceDir = resolve(uploadDir, device.name);
  mkdirSync(deviceDir, { recursive: true });
  writeFolderMetaIfMissing(deviceDir);

  const htmlPath = resolve(tmpDir, `${device.name}-${shot.file}.html`);
  const imagePath = resolve(deviceDir, shot.file);
  writeFileSync(htmlPath, buildHtml(device, shot));

  execFileSync(chromePath, [
    "--headless=new",
    "--hide-scrollbars",
    "--disable-gpu",
    "--no-first-run",
    "--no-default-browser-check",
    "--force-device-scale-factor=1",
    `--window-size=${device.width},${device.height}`,
    `--screenshot=${imagePath}`,
    `file://${htmlPath}`,
  ], { stdio: "ignore" });

  writeTextureMetaIfMissing(imagePath);
  console.log(`Generated ${imagePath}`);
}

function main() {
  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);

  mkdirSync(tmpDir, { recursive: true });
  mkdirSync(uploadDir, { recursive: true });
  writeFolderMetaIfMissing(uploadDir);

  for (const device of devices) {
    for (const shot of shots) {
      renderShot(device, shot);
    }
  }

  console.log(`Generated 2048 Blink App Store screenshots in ${uploadDir}`);
}

main();
