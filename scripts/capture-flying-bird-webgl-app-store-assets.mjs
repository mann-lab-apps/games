import { spawn } from "node:child_process";
import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import net from "node:net";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const publicDir = resolve(repoRoot, "web/mannlab-games/public");
const uploadDir = resolve(repoRoot, "prototypes/flying-bird/Assets/_Project/Art/AppStore/Upload");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const metaOnly = process.argv.includes("--meta-only");

const devices = [
  { name: "iPhone-6.9", width: 1320, height: 2868, mobile: true, userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1" },
  { name: "iPhone-6.5", width: 1284, height: 2778, mobile: true, userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1" },
  { name: "iPad-13", width: 2064, height: 2752, mobile: true, userAgent: "Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1" },
];

const shots = [
  {
    file: "01-start-near-clouds.png",
    actions: [{ type: "wait", ms: 650 }],
  },
  {
    file: "02-first-flaps.png",
    actions: [{ type: "hold", ms: 1150 }],
  },
  {
    file: "03-glide-and-read-wind.png",
    actions: [{ type: "glide", ms: 1150 }],
  },
  {
    file: "04-climb-above-clouds.png",
    actions: [{ type: "hold", ms: 1700 }],
  },
  {
    file: "05-long-glide.png",
    actions: [{ type: "glide", ms: 1450 }],
  },
  {
    file: "06-final-push.png",
    actions: [{ type: "hold", ms: 1500 }, { type: "glide", ms: 250 }],
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

async function findFreePort() {
  return await new Promise((resolvePort, reject) => {
    const server = net.createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      server.close(() => resolvePort(port));
    });
  });
}

function delay(ms) {
  return new Promise((resolveDelay) => setTimeout(resolveDelay, ms));
}

async function waitForHttp(url, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch {
      // Keep waiting.
    }
    await delay(250);
  }
  throw new Error(`Timed out waiting for ${url}`);
}

class CdpClient {
  constructor(webSocketUrl) {
    this.nextId = 1;
    this.pending = new Map();
    this.socket = new WebSocket(webSocketUrl);
  }

  async open() {
    await new Promise((resolveOpen, reject) => {
      this.socket.addEventListener("open", resolveOpen, { once: true });
      this.socket.addEventListener("error", reject, { once: true });
      this.socket.addEventListener("message", (event) => this.handleMessage(event));
    });
  }

  handleMessage(event) {
    const message = JSON.parse(event.data);
    if (!message.id || !this.pending.has(message.id)) return;
    const { resolveCommand, rejectCommand } = this.pending.get(message.id);
    this.pending.delete(message.id);
    if (message.error) {
      rejectCommand(new Error(`${message.error.message}: ${message.error.data ?? ""}`));
      return;
    }
    resolveCommand(message.result ?? {});
  }

  async send(method, params = {}) {
    const id = this.nextId++;
    const result = new Promise((resolveCommand, rejectCommand) => {
      this.pending.set(id, { resolveCommand, rejectCommand });
    });
    this.socket.send(JSON.stringify({ id, method, params }));
    return await result;
  }

  close() {
    this.socket.close();
  }
}

async function openDebugTab(debugPort, url) {
  const encodedUrl = encodeURIComponent(url);
  let response = await fetch(`http://127.0.0.1:${debugPort}/json/new?${encodedUrl}`, { method: "PUT" });
  if (!response.ok) response = await fetch(`http://127.0.0.1:${debugPort}/json/new?${encodedUrl}`);
  if (!response.ok) throw new Error(`Failed to open Chrome debugging tab: ${response.status}`);
  return await response.json();
}

async function waitForGame(client) {
  const deadline = Date.now() + 60000;
  let lastStatus = null;
  let lastLog = 0;
  while (Date.now() < deadline) {
    const result = await client.send("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const loading = document.querySelector("#unity-loading-bar");
        const warning = document.querySelector("#unity-warning");
        const canvas = document.querySelector("#unity-canvas");
        const loadingDisplay = loading ? getComputedStyle(loading).display : "";
        const canvasRect = canvas ? canvas.getBoundingClientRect() : null;
        const warningText = warning ? warning.innerText : "";
        return {
          ready: Boolean(canvas) && canvas.width > 0 && canvas.height > 0 && canvasRect.width > 0 && canvasRect.height > 0 && !warningText.includes("does not support WebGL") && !warningText.includes("게임을 실행하지 못했어요"),
          failed: warningText.includes("does not support WebGL") || warningText.includes("게임을 실행하지 못했어요"),
          canvasWidth: canvas ? canvas.width : 0,
          canvasHeight: canvas ? canvas.height : 0,
          rectWidth: canvasRect ? Math.round(canvasRect.width) : 0,
          rectHeight: canvasRect ? Math.round(canvasRect.height) : 0,
          loadingDisplay,
          warningText
        };
      })()`,
    });
    lastStatus = result.result?.value;
    if (lastStatus?.failed) throw new Error(`WebGL page failed to start: ${lastStatus.warningText}`);
    if (lastStatus?.ready) {
      await delay(3200);
      return;
    }
    if (Date.now() - lastLog > 10000) {
      console.log(`Waiting for Unity WebGL: ${JSON.stringify(lastStatus)}`);
      lastLog = Date.now();
    }
    await delay(500);
  }
  throw new Error(`Timed out waiting for Unity WebGL. Last status: ${JSON.stringify(lastStatus)}`);
}

async function setSpace(client, pressed) {
  await client.send("Input.dispatchKeyEvent", {
    type: pressed ? "keyDown" : "keyUp",
    key: " ",
    code: "Space",
    windowsVirtualKeyCode: 32,
    nativeVirtualKeyCode: 32,
  });
}

async function runAction(client, action) {
  if (action.type === "hold") {
    await setSpace(client, true);
    await delay(action.ms);
    return;
  }

  await setSpace(client, false);
  await delay(action.ms);
}

async function captureDevice(client, device, appUrl) {
  const deviceDir = resolve(uploadDir, device.name);
  mkdirSync(deviceDir, { recursive: true });
  writeFolderMetaIfMissing(deviceDir);

  await client.send("Page.enable");
  await client.send("Runtime.enable");
  await client.send("Emulation.setUserAgentOverride", { userAgent: device.userAgent });
  await client.send("Emulation.setTouchEmulationEnabled", { enabled: device.mobile });
  await client.send("Emulation.setDeviceMetricsOverride", {
    width: device.width,
    height: device.height,
    deviceScaleFactor: 1,
    mobile: device.mobile,
    screenWidth: device.width,
    screenHeight: device.height,
  });
  await client.send("Page.navigate", { url: appUrl });
  await waitForGame(client);
  await client.send("Runtime.evaluate", {
    expression: `(() => {
      const style = document.createElement("style");
      style.textContent = "*, #unity-canvas { outline: none !important; -webkit-tap-highlight-color: transparent !important; } body { margin: 0 !important; overflow: hidden !important; }";
      document.head.appendChild(style);
      document.querySelector("#unity-canvas")?.focus();
    })()`,
  });

  await client.send("Input.dispatchMouseEvent", { type: "mousePressed", x: device.width / 2, y: device.height / 2, button: "left", clickCount: 1 });
  await delay(80);
  await client.send("Input.dispatchMouseEvent", { type: "mouseReleased", x: device.width / 2, y: device.height / 2, button: "left", clickCount: 1 });
  await delay(500);

  for (const shot of shots) {
    for (const action of shot.actions) await runAction(client, action);
    const pngPath = resolve(deviceDir, shot.file);
    const capture = await client.send("Page.captureScreenshot", {
      format: "png",
      fromSurface: true,
      captureBeyondViewport: false,
    });
    writeFileSync(pngPath, Buffer.from(capture.data, "base64"));
    writeTextureMetaIfMissing(pngPath);
    console.log(`Captured ${pngPath}`);
  }

  await setSpace(client, false);
}

async function main() {
  mkdirSync(uploadDir, { recursive: true });
  writeFolderMetaIfMissing(uploadDir);

  if (metaOnly) {
    for (const device of devices) {
      writeFolderMetaIfMissing(resolve(uploadDir, device.name));
      for (const shot of shots) {
        const pngPath = resolve(uploadDir, device.name, shot.file);
        if (existsSync(pngPath)) writeTextureMetaIfMissing(pngPath);
      }
    }
    console.log(`Generated missing Unity metadata for Wind Gull App Store screenshots in ${uploadDir}`);
    return;
  }

  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);

  const httpPort = await findFreePort();
  const debugPort = await findFreePort();
  const appUrl = `http://127.0.0.1:${httpPort}/games/flying-bird/`;
  const userDataDir = resolve(repoRoot, `tmp/chrome-wind-gull-capture-${debugPort}`);
  mkdirSync(userDataDir, { recursive: true });

  const server = spawn("python3", ["-m", "http.server", String(httpPort), "--bind", "127.0.0.1"], {
    cwd: publicDir,
    stdio: "ignore",
  });
  const chrome = spawn(chromePath, [
    `--remote-debugging-port=${debugPort}`,
    `--user-data-dir=${userDataDir}`,
    "--disable-extensions",
    "--no-first-run",
    "--no-default-browser-check",
    "--autoplay-policy=no-user-gesture-required",
    `--window-size=${devices[0].width},${devices[0].height}`,
    "about:blank",
  ], { stdio: "ignore" });

  try {
    await waitForHttp(`http://127.0.0.1:${httpPort}/games/flying-bird/index.html`);
    await waitForHttp(`http://127.0.0.1:${debugPort}/json/version`);

    for (const device of devices) {
      console.log(`Capturing ${device.name} (${device.width}x${device.height})`);
      const target = await openDebugTab(debugPort, "about:blank");
      const client = new CdpClient(target.webSocketDebuggerUrl);
      await client.open();
      try {
        await captureDevice(client, device, appUrl);
      } finally {
        client.close();
      }
    }
  } finally {
    chrome.kill("SIGTERM");
    server.kill("SIGTERM");
  }

  console.log(`Generated real WebGL App Store screenshots in ${uploadDir}`);
}

await main();
