import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const outDir = resolve(repoRoot, "prototypes/walking/Assets/_Project/Art/AppStore");
const htmlPath = resolve("/tmp", "walking-app-icon.html");
const iconPath = resolve(outDir, "AppIcon-1024.png");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

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
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
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
`);
}

mkdirSync(outDir, { recursive: true });
writeFolderMetaIfMissing(resolve(repoRoot, "prototypes/walking/Assets/_Project/Art"));
writeFolderMetaIfMissing(resolve(repoRoot, "prototypes/walking/Assets/_Project/Art/AppStore"));

writeFileSync(htmlPath, `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    * { box-sizing: border-box; }
    html, body {
      width: 1024px;
      height: 1024px;
      margin: 0;
      overflow: hidden;
      background: #f8f3e8;
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Helvetica Neue", Arial, sans-serif;
    }
    .icon {
      position: relative;
      width: 1024px;
      height: 1024px;
      overflow: hidden;
      background:
        radial-gradient(circle at 26% 18%, rgba(255,255,255,0.78), transparent 18%),
        linear-gradient(145deg, #fffaf0 0%, #eee6d5 100%);
    }
    .paper-noise {
      position: absolute;
      inset: 0;
      opacity: 0.14;
      background:
        repeating-linear-gradient(8deg, rgba(41,39,37,0.13) 0 1px, transparent 1px 23px),
        repeating-linear-gradient(98deg, rgba(41,39,37,0.08) 0 1px, transparent 1px 31px);
      mix-blend-mode: multiply;
    }
    .frame {
      position: absolute;
      inset: 66px;
      border: 18px solid #2a2826;
      border-radius: 92px;
      box-shadow:
        0 34px 0 rgba(42, 40, 38, 0.10),
        inset 0 0 0 8px rgba(255, 255, 255, 0.38);
    }
    .corridor {
      position: absolute;
      left: 112px;
      top: 118px;
      width: 800px;
      height: 788px;
      overflow: hidden;
      border-radius: 70px;
      clip-path: inset(0 round 70px);
    }
    .floor {
      position: absolute;
      inset: 0;
      background: #fffdf7;
    }
    .wall-left,
    .wall-right {
      position: absolute;
      top: 0;
      bottom: 0;
      background: #242320;
      filter: drop-shadow(0 18px 0 rgba(36, 35, 32, 0.08));
    }
    .wall-left {
      left: -48px;
      width: 410px;
      clip-path: polygon(0 0, 100% 0, 48% 50%, 100% 100%, 0 100%);
    }
    .wall-right {
      right: -48px;
      width: 410px;
      clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%, 52% 50%);
    }
    .front-wall {
      position: absolute;
      left: 318px;
      top: 180px;
      width: 388px;
      height: 410px;
      background: #2b2a26;
      border: 14px solid #1e1d1b;
      border-radius: 18px;
      box-shadow: inset 0 0 0 22px rgba(255, 255, 255, 0.02);
    }
    .opening {
      position: absolute;
      left: 318px;
      bottom: 0;
      width: 388px;
      height: 216px;
      background: #fffdf7;
      border-left: 14px solid #242320;
      border-right: 14px solid #242320;
    }
    .dash {
      position: absolute;
      width: 162px;
      height: 14px;
      border-radius: 999px;
      background: #bdb8ad;
      transform: rotate(-1deg);
    }
    .dash.a { left: 432px; top: 686px; width: 160px; }
    .dash.b { left: 370px; top: 778px; width: 122px; transform: rotate(-18deg); }
    .dash.c { left: 532px; top: 780px; width: 122px; transform: rotate(18deg); }
    .hint {
      position: absolute;
      bottom: 78px;
      width: 180px;
      height: 86px;
      border: 12px solid #2a2826;
      border-radius: 999px;
      background: #f4ad3d;
      box-shadow: 0 18px 0 rgba(42, 40, 38, 0.12);
    }
    .hint.left { left: 132px; transform: rotate(-6deg); }
    .hint.right { right: 132px; transform: rotate(6deg); }
    .thumb-mark {
      position: absolute;
      inset: 16px 34px;
      border-radius: 999px;
      background: rgba(255, 255, 255, 0.34);
    }
    .sketch-line {
      position: absolute;
      height: 10px;
      border-radius: 999px;
      background: #2a2826;
      opacity: 0.9;
    }
    .sketch-line.one { left: 158px; top: 124px; width: 690px; transform: rotate(-1.3deg); }
    .sketch-line.two { left: 174px; bottom: 122px; width: 660px; transform: rotate(1.4deg); }
  </style>
</head>
<body>
  <div class="icon" aria-label="Walking app icon">
    <div class="paper-noise"></div>
    <div class="corridor">
      <div class="floor"></div>
      <div class="dash a"></div>
      <div class="dash b"></div>
      <div class="dash c"></div>
      <div class="wall-left"></div>
      <div class="wall-right"></div>
      <div class="front-wall"></div>
      <div class="opening"></div>
      <div class="hint left"><div class="thumb-mark"></div></div>
      <div class="hint right"><div class="thumb-mark"></div></div>
    </div>
    <div class="frame"></div>
    <div class="sketch-line one"></div>
    <div class="sketch-line two"></div>
  </div>
</body>
</html>
`);

execFileSync(chromePath, [
  "--headless=new",
  "--hide-scrollbars",
  "--disable-gpu",
  "--screenshot=" + iconPath,
  "--window-size=1024,1024",
  `file://${htmlPath}`,
], { stdio: "inherit" });

writeTextureMetaIfMissing(iconPath);
console.log(`Generated ${iconPath}`);
