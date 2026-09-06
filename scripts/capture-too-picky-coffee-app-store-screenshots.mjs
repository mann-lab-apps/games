#!/usr/bin/env node
import http from "node:http";
import { spawn } from "node:child_process";
import { createReadStream, existsSync, mkdirSync, statSync, writeFileSync } from "node:fs";
import { extname, join, normalize, relative, resolve, sep } from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const repoRoot = resolve(import.meta.dirname, "..");
const buildDir = resolve(repoRoot, "prototypes/sensitive-barista/Builds/WebGL/sensitive-barista");
const outputDir = resolve(repoRoot, "prototypes/sensitive-barista/AppStore/Screenshots/Upload");
const chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

const devices = [
  {
    slug: "iphone-65",
    label: "iPhone 6.5",
    width: 1242,
    height: 2688,
    userAgent:
      "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
  {
    slug: "ipad-13",
    label: "iPad 13",
    width: 2064,
    height: 2752,
    userAgent:
      "Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
  },
];

const mimeTypes = new Map([
  [".html", "text/html; charset=utf-8"],
  [".js", "application/javascript; charset=utf-8"],
  [".css", "text/css; charset=utf-8"],
  [".json", "application/json; charset=utf-8"],
  [".wasm", "application/wasm"],
  [".data", "application/octet-stream"],
  [".png", "image/png"],
  [".jpg", "image/jpeg"],
  [".jpeg", "image/jpeg"],
  [".svg", "image/svg+xml"],
  [".ico", "image/x-icon"],
  [".br", "application/octet-stream"],
  [".gz", "application/gzip"],
]);

class CdpClient {
  constructor(socket) {
    this.socket = socket;
    this.nextId = 1;
    this.pending = new Map();
    socket.addEventListener("message", (event) => {
      const payload = JSON.parse(event.data);
      if (!payload.id) return;
      const entry = this.pending.get(payload.id);
      if (!entry) return;
      this.pending.delete(payload.id);
      if (payload.error) entry.reject(new Error(payload.error.message));
      else entry.resolve(payload.result);
    });
  }

  send(method, params = {}) {
    const id = this.nextId++;
    const message = JSON.stringify({ id, method, params });
    return new Promise((resolvePromise, reject) => {
      this.pending.set(id, { resolve: resolvePromise, reject });
      this.socket.send(message);
    });
  }

  close() {
    this.socket.close();
  }
}

function startStaticServer(rootDir) {
  const server = http.createServer((request, response) => {
    const requestUrl = new URL(request.url ?? "/", "http://127.0.0.1");
    const pathname = decodeURIComponent(requestUrl.pathname);
    const safePath = normalize(pathname).replace(/^(\.\.[/\\])+/, "");
    const candidate = resolve(rootDir, safePath === sep ? "index.html" : safePath.slice(1));
    const relativePath = relative(rootDir, candidate);

    if (relativePath.startsWith("..") || relativePath.includes(`..${sep}`) || !existsSync(candidate)) {
      response.writeHead(404);
      response.end("Not found");
      return;
    }

    const stat = statSync(candidate);
    if (stat.isDirectory()) {
      response.writeHead(301, { Location: `${pathname.replace(/\/$/, "")}/` });
      response.end();
      return;
    }

    const headers = {
      "Content-Type": mimeTypes.get(extname(candidate)) ?? "application/octet-stream",
      "Cross-Origin-Opener-Policy": "same-origin",
      "Cross-Origin-Embedder-Policy": "require-corp",
      "Cache-Control": "no-store",
    };
    if (candidate.endsWith(".br")) headers["Content-Encoding"] = "br";
    if (candidate.endsWith(".gz")) headers["Content-Encoding"] = "gzip";

    response.writeHead(200, headers);
    createReadStream(candidate).pipe(response);
  });

  return new Promise((resolvePromise, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => resolvePromise(server));
  });
}

async function launchChrome(width, height) {
  if (!existsSync(chromePath)) {
    throw new Error(`Chrome not found at ${chromePath}`);
  }

  const remoteDebuggingPort = await pickPort();
  const userDataDir = resolve("/tmp", `too-picky-coffee-capture-${process.pid}-${Date.now()}`);
  const chrome = spawn(chromePath, [
    "--headless=new",
    "--hide-scrollbars",
    "--mute-audio",
    "--disable-background-networking",
    "--disable-component-update",
    "--disable-default-apps",
    "--disable-extensions",
    "--disable-features=Translate",
    "--disable-popup-blocking",
    "--disable-sync",
    "--no-first-run",
    "--no-default-browser-check",
    `--remote-debugging-port=${remoteDebuggingPort}`,
    `--user-data-dir=${userDataDir}`,
    `--window-size=${width},${height}`,
    "about:blank",
  ]);

  await waitForChrome(remoteDebuggingPort);
  return { chrome, remoteDebuggingPort };
}

async function pickPort() {
  return new Promise((resolvePromise, reject) => {
    const server = http.createServer();
    server.listen(0, "127.0.0.1", () => {
      const address = server.address();
      const port = typeof address === "object" && address ? address.port : 0;
      server.close(() => resolvePromise(port));
    });
    server.once("error", reject);
  });
}

async function waitForChrome(port) {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(`http://127.0.0.1:${port}/json/version`);
      if (response.ok) return;
    } catch {
      await delay(100);
    }
  }
  throw new Error("Chrome did not expose DevTools in time");
}

async function openPage(port) {
  const response = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" });
  if (!response.ok) {
    throw new Error(`Unable to create Chrome tab: ${response.status}`);
  }
  const tab = await response.json();
  const socket = new WebSocket(tab.webSocketDebuggerUrl);
  await new Promise((resolvePromise, reject) => {
    socket.addEventListener("open", resolvePromise, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });
  return new CdpClient(socket);
}

async function preparePage(client, device, url) {
  await client.send("Page.enable");
  await client.send("Runtime.enable");
  await client.send("Emulation.setDeviceMetricsOverride", {
    width: device.width,
    height: device.height,
    deviceScaleFactor: 1,
    mobile: true,
    screenWidth: device.width,
    screenHeight: device.height,
  });
  await client.send("Emulation.setUserAgentOverride", {
    userAgent: device.userAgent,
    platform: device.slug.startsWith("ipad") ? "iPad" : "iPhone",
  });
  await client.send("Emulation.setTouchEmulationEnabled", {
    enabled: true,
    maxTouchPoints: 1,
  });
  await client.send("Page.navigate", { url });
  await waitForUnity(client);
  const metrics = await client.send("Runtime.evaluate", {
    expression: `({ width: window.innerWidth, height: window.innerHeight, dpr: window.devicePixelRatio })`,
    returnByValue: true,
  });
  device.cssWidth = metrics.result?.value?.width ?? device.width;
  device.cssHeight = metrics.result?.value?.height ?? device.height;
}

async function waitForUnity(client) {
  const deadline = Date.now() + 90_000;
  while (Date.now() < deadline) {
    const result = await client.send("Runtime.evaluate", {
      expression: `(() => {
        const canvas = document.querySelector("#unity-canvas");
        const loading = document.querySelector("#unity-loading-bar");
        const rect = canvas ? canvas.getBoundingClientRect() : null;
        return {
          ready: Boolean(canvas && rect && rect.width > 100 && rect.height > 100 && (!loading || getComputedStyle(loading).display === "none")),
          title: document.title,
          width: rect ? rect.width : 0,
          height: rect ? rect.height : 0
        };
      })()`,
      returnByValue: true,
    });
    if (result.result?.value?.ready) {
      await delay(5200);
      return;
    }
    await delay(250);
  }
  throw new Error("Unity WebGL did not finish loading in time");
}

async function click(client, device, xRatio, yRatio, holdMs = 80) {
  const x = Math.round((device.cssWidth ?? device.width) * xRatio);
  const y = Math.round((device.cssHeight ?? device.height) * yRatio);
  await client.send("Input.dispatchMouseEvent", { type: "mouseMoved", x, y, button: "none" });
  await client.send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", buttons: 1, clickCount: 1 });
  await delay(holdMs);
  await client.send("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", buttons: 0, clickCount: 1 });
  await delay(220);
}

function controlPoint(device, name) {
  const iphone = {
    ice: [0.18, 0.897],
    shot: [0.32, 0.897],
    water: [0.455, 0.897],
    milk: [0.592, 0.897],
    syrup: [0.728, 0.897],
    taste: [0.758, 0.962],
  };
  const ipad = {
    ice: [0.27, 0.884],
    shot: [0.385, 0.884],
    water: [0.50, 0.884],
    milk: [0.615, 0.884],
    syrup: [0.73, 0.884],
    taste: [0.876, 0.955],
  };
  return (device.slug === "iphone-65" ? iphone : ipad)[name];
}

async function pressControl(client, device, name, holdMs = 80) {
  const [x, y] = controlPoint(device, name);
  await click(client, device, x, y, holdMs);
}

async function makeDrink(client, device) {
  await pressControl(client, device, "ice", 850);
  await pressControl(client, device, "shot", 80);
  await pressControl(client, device, "shot", 80);
  await pressControl(client, device, "water", 600);
  await pressControl(client, device, "milk", 650);
  await pressControl(client, device, "syrup", 80);
  await delay(1200);
}

async function capture(client, path) {
  const screenshot = await client.send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: false,
  });
  writeFileSync(path, Buffer.from(screenshot.data, "base64"));
}

async function runShot(serverUrl, device, shot) {
  const { chrome, remoteDebuggingPort } = await launchChrome(device.width, device.height);
  const client = await openPage(remoteDebuggingPort);
  try {
    await preparePage(client, device, `${serverUrl}${shot.query}`);
    if (shot.setup) await shot.setup(client, device);
    const filePath = join(outputDir, `${device.slug}-${shot.file}`);
    await capture(client, filePath);
    console.log(`${device.label}: ${shot.name} -> ${relative(repoRoot, filePath)}`);
  } finally {
    try {
      await client.send("Browser.close");
    } catch {
      chrome.kill("SIGTERM");
    }
    client.close();
    await delay(250);
  }
}

const shots = [
  {
    file: "01-order-ready.png",
    name: "order ready",
    query: "?capture=ready&captureOrder=0",
  },
  {
    file: "02-mixing-cup.png",
    name: "mixing cup",
    query: "?capture=mix&captureOrder=0",
  },
  {
    file: "03-taste-result.png",
    name: "taste result",
    query: "?capture=result&captureOrder=0",
  },
];

async function main() {
  if (!existsSync(join(buildDir, "index.html"))) {
    throw new Error(`WebGL build not found at ${buildDir}`);
  }

  mkdirSync(outputDir, { recursive: true });
  const server = await startStaticServer(buildDir);
  const address = server.address();
  const port = typeof address === "object" && address ? address.port : 0;
  const serverUrl = `http://127.0.0.1:${port}/`;
  console.log(`Serving Too Picky Coffee from ${serverUrl}`);

  try {
    for (const device of devices) {
      for (const shot of shots) {
        await runShot(serverUrl, device, shot);
      }
    }
  } finally {
    server.close();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
