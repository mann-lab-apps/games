#!/usr/bin/env node

import { spawn } from "node:child_process";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import net from "node:net";
import { resolve } from "node:path";
import { inflateSync } from "node:zlib";

const repoRoot = resolve(import.meta.dirname, "..");
const buildDir = process.env.ONE_EQUALS_ONE_WEBGL_BUILD_DIR
  ? resolve(process.env.ONE_EQUALS_ONE_WEBGL_BUILD_DIR)
  : resolve(repoRoot, "prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const screenshotDir = process.env.ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR ?? resolve("/tmp", "one-equals-one-webgl-viewports");

const qaDevices = [
  {
    name: "iphone-se",
    width: 320,
    height: 568,
    deviceScaleFactor: 2,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    name: "iphone-standard",
    width: 390,
    height: 844,
    deviceScaleFactor: 3,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    name: "iphone-large",
    width: 430,
    height: 932,
    deviceScaleFactor: 3,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    name: "android-20x9",
    width: 412,
    height: 915,
    deviceScaleFactor: 2.75,
    mobile: true,
    userAgent: "Mozilla/5.0 (Linux; Android 15; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36",
  },
  {
    name: "desktop",
    width: 1440,
    height: 1024,
    deviceScaleFactor: 1,
    mobile: false,
    userAgent: "Mozilla/5.0 (Macintosh; Intel Mac OS X 15_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
  },
];

const storeDevices = [
  {
    name: "iphone-6-5",
    width: 428,
    height: 926,
    deviceScaleFactor: 3,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    name: "iphone-6-9",
    width: 440,
    height: 956,
    deviceScaleFactor: 3,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    name: "ipad-13",
    width: 1032,
    height: 1376,
    deviceScaleFactor: 2,
    mobile: true,
    userAgent: "Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
];

const devices = process.env.ONE_EQUALS_ONE_VIEWPORT_SET === "store" ? storeDevices : qaDevices;

function delay(ms) {
  return new Promise((resolveDelay) => setTimeout(resolveDelay, ms));
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
    this.events = [];
    this.socket = new WebSocket(webSocketUrl);
  }

  async open() {
    await new Promise((resolveOpen, rejectOpen) => {
      this.socket.addEventListener("open", resolveOpen, { once: true });
      this.socket.addEventListener("error", rejectOpen, { once: true });
      this.socket.addEventListener("message", (event) => this.handleMessage(event));
    });
  }

  handleMessage(event) {
    const message = JSON.parse(event.data);
    if (message.id && this.pending.has(message.id)) {
      const { resolveCommand, rejectCommand } = this.pending.get(message.id);
      this.pending.delete(message.id);
      if (message.error) {
        rejectCommand(new Error(`${message.error.message}: ${message.error.data ?? ""}`));
        return;
      }

      resolveCommand(message.result ?? {});
      return;
    }

    if (message.method === "Runtime.consoleAPICalled" || message.method === "Log.entryAdded") {
      this.events.push(message);
    }
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

async function waitForGame(client, timeoutMs = 120000) {
  const deadline = Date.now() + timeoutMs;
  let lastStatus = null;
  while (Date.now() < deadline) {
    const result = await client.send("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const loading = document.querySelector("#unity-loading-bar");
        const warning = document.querySelector("#unity-warning");
        const canvas = document.querySelector("#unity-canvas");
        const loadingDisplay = loading ? getComputedStyle(loading).display : "";
        const warningText = warning ? warning.innerText : "";
        const rect = canvas ? canvas.getBoundingClientRect() : null;
        return {
          ready: Boolean(canvas) && loadingDisplay === "none" && !warningText.includes("게임을 실행하지 못했어요"),
          failed: warningText.includes("게임을 실행하지 못했어요") || warningText.includes("does not support WebGL"),
          loadingDisplay,
          warningText,
          canvasWidth: rect?.width ?? 0,
          canvasHeight: rect?.height ?? 0,
          viewportWidth: window.innerWidth,
          viewportHeight: window.innerHeight
        };
      })()`,
    });
    lastStatus = result.result?.value;
    if (lastStatus?.failed) throw new Error(`WebGL page failed: ${lastStatus.warningText}`);
    if (lastStatus?.ready) {
      await delay(2500);
      return lastStatus;
    }

    await delay(500);
  }

  const logs = client.events.slice(-10).map((event) => JSON.stringify(event.params)).join("\n");
  throw new Error(`Timed out waiting for Unity WebGL. Last status: ${JSON.stringify(lastStatus)}\nRecent logs:\n${logs}`);
}

async function smokeDevice(client, appUrl, device) {
  if (device.userAgent) {
    await client.send("Network.enable");
    await client.send("Network.setUserAgentOverride", { userAgent: device.userAgent });
  }

  await client.send("Emulation.setDeviceMetricsOverride", {
    width: device.width,
    height: device.height,
    deviceScaleFactor: device.deviceScaleFactor,
    mobile: device.mobile,
    screenWidth: device.width,
    screenHeight: device.height,
  });
  const touchParams = { enabled: device.mobile };
  if (device.mobile) {
    touchParams.maxTouchPoints = 5;
  }

  await client.send("Emulation.setTouchEmulationEnabled", touchParams);
  await client.send("Page.navigate", { url: appUrl });
  const status = await waitForGame(client);
  if (status.canvasWidth < device.width * 0.9 || status.canvasHeight < device.height * 0.9) {
    throw new Error(`${device.name} canvas is too small: ${status.canvasWidth}x${status.canvasHeight}`);
  }

  const capture = await client.send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: false,
  });
  const screenshotBytes = Buffer.from(capture.data, "base64");
  const screenshotPath = resolve(screenshotDir, `${device.name}.png`);
  writeFileSync(screenshotPath, screenshotBytes);
  assertScreenshotHasGamePixels(screenshotBytes, device.name);
  return screenshotPath;
}

function assertScreenshotHasGamePixels(pngBytes, deviceName) {
  const stats = readPngStats(pngBytes);
  if (stats.width < 1 || stats.height < 1) {
    throw new Error(`${deviceName} screenshot has invalid dimensions.`);
  }

  if (stats.darkRatio < 0.0015) {
    throw new Error(`${deviceName} screenshot looks blank: dark pixel ratio ${stats.darkRatio.toFixed(5)}`);
  }

  if (stats.lightRatio < 0.35) {
    throw new Error(`${deviceName} screenshot looks too dark: light pixel ratio ${stats.lightRatio.toFixed(5)}`);
  }

  if (/iphone|android/i.test(deviceName) && stats.firstContentYRatio > 0.22) {
    throw new Error(
      `${deviceName} screenshot content starts too low: first content y ratio ${stats.firstContentYRatio.toFixed(3)}`,
    );
  }
}

function readPngStats(pngBytes) {
  const signature = "89504e470d0a1a0a";
  if (pngBytes.subarray(0, 8).toString("hex") !== signature) {
    throw new Error("Screenshot is not a PNG.");
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
    if (dataEnd + 4 > pngBytes.length) throw new Error("PNG chunk is truncated.");

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
    throw new Error(`Unsupported screenshot PNG format: bitDepth=${bitDepth}, colorType=${colorType}`);
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
      throw new Error(`Unsupported PNG filter: ${filter}`);
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

async function main() {
  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);
  if (!existsSync(resolve(buildDir, "index.html"))) throw new Error(`WebGL build not found: ${buildDir}`);
  mkdirSync(screenshotDir, { recursive: true });

  const httpPort = await findFreePort();
  const debugPort = await findFreePort();
  const appUrl = `http://127.0.0.1:${httpPort}/index.html${process.env.ONE_EQUALS_ONE_WEBGL_QUERY ?? ""}`;
  const userDataDir = resolve(repoRoot, `tmp/chrome-one-equals-one-viewport-smoke-${debugPort}`);
  mkdirSync(userDataDir, { recursive: true });

  const server = spawn("python3", ["-m", "http.server", String(httpPort), "--bind", "127.0.0.1"], {
    cwd: buildDir,
    stdio: "ignore",
  });
  const chrome = spawn(chromePath, [
    `--remote-debugging-port=${debugPort}`,
    `--user-data-dir=${userDataDir}`,
    "--disable-extensions",
    "--headless=new",
    "--no-first-run",
    "--no-default-browser-check",
    "--autoplay-policy=no-user-gesture-required",
    "about:blank",
  ], { stdio: "ignore" });

  try {
    await waitForHttp(appUrl);
    await waitForHttp(`http://127.0.0.1:${debugPort}/json/version`);

    const target = await openDebugTab(debugPort, appUrl);
    const client = new CdpClient(target.webSocketDebuggerUrl);
    await client.open();
    try {
      await client.send("Page.enable");
      await client.send("Runtime.enable");
      await client.send("Log.enable");
      const failures = [];
      for (const device of devices) {
        try {
          const screenshotPath = await smokeDevice(client, appUrl, device);
          console.log(`${device.name}: ${screenshotPath}`);
        } catch (error) {
          const screenshotPath = resolve(screenshotDir, `${device.name}.png`);
          const message = error instanceof Error ? error.message : String(error);
          failures.push(`${device.name}: ${message}`);
          console.error(`${device.name}: FAIL (${message}); screenshot: ${screenshotPath}`);
        }
      }

      if (failures.length > 0) {
        throw new Error(`Viewport smoke failed:\n- ${failures.join("\n- ")}`);
      }
    } finally {
      client.close();
    }
  } finally {
    chrome.kill("SIGTERM");
    server.kill("SIGTERM");
  }
}

try {
  await main();
} catch (error) {
  if (error?.code === "EPERM" && error?.syscall === "listen") {
    console.error(
      `Unable to open local WebGL smoke port ${error.address ?? "127.0.0.1"}:${error.port ?? "?"}. ` +
        "Run this smoke test outside the filesystem/network sandbox so it can bind a local HTTP server.",
    );
    process.exit(2);
  }

  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}
