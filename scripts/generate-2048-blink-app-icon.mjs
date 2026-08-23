import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const outDir = resolve(repoRoot, "prototypes/2048-blink/Assets/_Project/Art/AppStore");
const htmlPath = resolve("/tmp", "2048-blink-app-icon.html");
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
writeFolderMetaIfMissing(resolve(repoRoot, "prototypes/2048-blink/Assets/_Project/Art/AppStore"));

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
    body {
      display: grid;
      place-items: center;
    }
    .icon {
      position: relative;
      width: 1024px;
      height: 1024px;
      overflow: hidden;
      background:
        radial-gradient(circle at 24% 18%, rgba(255,255,255,0.72), transparent 18%),
        linear-gradient(135deg, #fbf6ea 0%, #e6ded0 100%);
    }
    .board {
      position: absolute;
      left: 119px;
      top: 119px;
      width: 786px;
      height: 786px;
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      grid-template-rows: repeat(4, 1fr);
      gap: 22px;
      padding: 30px;
      border: 18px solid #292725;
      border-radius: 58px;
      background: #d6cbbb;
      box-shadow: 0 38px 0 rgba(41, 39, 37, 0.12);
    }
    .tile {
      position: relative;
      display: grid;
      place-items: center;
      border: 10px solid #292725;
      border-radius: 30px;
      background: #fffaf0;
      color: #292725;
      font-size: 66px;
      line-height: 1;
      font-weight: 900;
      letter-spacing: 0;
      box-shadow: inset 0 -9px 0 rgba(41, 39, 37, 0.08);
    }
    .tile.n4 { background: #f0deb4; }
    .tile.n8 { background: #eda860; }
    .tile.n16 { background: #ea8759; color: #fffdf8; }
    .tile.n32 { background: #dc6457; color: #fffdf8; }
    .tile.n64 { background: #4d82a3; color: #fffdf8; }
    .tile.gray {
      color: transparent;
      background: #34383a;
      border-color: #232526;
      box-shadow: inset 0 -12px 0 rgba(255, 255, 255, 0.07);
    }
    .tile.gray::after {
      content: "";
      position: absolute;
      inset: 26px;
      border-radius: 18px;
      background:
        repeating-linear-gradient(
          135deg,
          rgba(255, 255, 255, 0.18) 0,
          rgba(255, 255, 255, 0.18) 9px,
          transparent 9px,
          transparent 24px
        );
    }
    .cross-row,
    .cross-column {
      position: absolute;
      pointer-events: none;
      border-radius: 28px;
      background: rgba(46, 51, 53, 0.18);
      mix-blend-mode: multiply;
    }
    .cross-row {
      left: 149px;
      top: 325px;
      width: 726px;
      height: 171px;
    }
    .cross-column {
      left: 512px;
      top: 149px;
      width: 171px;
      height: 726px;
    }
    .badge {
      position: absolute;
      left: 364px;
      bottom: 54px;
      width: 296px;
      height: 96px;
      display: grid;
      place-items: center;
      border: 10px solid #292725;
      border-radius: 48px;
      background: #fffaf0;
      color: #292725;
      font-size: 47px;
      font-weight: 900;
      box-shadow: 0 16px 0 rgba(41, 39, 37, 0.11);
    }
  </style>
</head>
<body>
  <div class="icon" aria-label="2048 Blink app icon">
    <div class="board">
      <div class="tile n2">2</div>
      <div class="tile n4">4</div>
      <div class="tile gray">8</div>
      <div class="tile n16">16</div>
      <div class="tile gray">32</div>
      <div class="tile gray">64</div>
      <div class="tile gray">128</div>
      <div class="tile gray">256</div>
      <div class="tile n4">4</div>
      <div class="tile n8">8</div>
      <div class="tile gray">16</div>
      <div class="tile n32">32</div>
      <div class="tile n2">2</div>
      <div class="tile n4">4</div>
      <div class="tile gray">8</div>
      <div class="tile n64">64</div>
    </div>
    <div class="cross-row"></div>
    <div class="cross-column"></div>
    <div class="badge">2048</div>
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
