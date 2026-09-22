import { spawn } from "node:child_process";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import net from "node:net";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const appUrl = process.env.CREEP_WEBGL_URL ?? "http://127.0.0.1:8094/?smoke=1";
const activeScreenshotPath = resolve("/tmp", "creep-webgl-smoke-active.png");
const gameOverScreenshotPath = resolve("/tmp", "creep-webgl-smoke-gameover.png");
const restartScreenshotPath = resolve("/tmp", "creep-webgl-smoke-restart.png");

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

async function waitForHttp(url, timeoutMs = 20000) {
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
    await new Promise((resolveOpen, reject) => {
      this.socket.addEventListener("open", resolveOpen, { once: true });
      this.socket.addEventListener("error", reject, { once: true });
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

async function waitForUnity(client) {
  const deadline = Date.now() + 120000;
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
          ready: Boolean(canvas) && loadingDisplay === "none" && rect.width > 300 && rect.height > 500,
          failed: warningText.includes("does not support WebGL") || warningText.includes("abort"),
          loadingDisplay,
          warningText,
          canvasWidth: rect?.width ?? 0,
          canvasHeight: rect?.height ?? 0
        };
      })()`,
    });
    lastStatus = result.result?.value;
    if (lastStatus?.failed) throw new Error(`WebGL page failed: ${lastStatus.warningText}`);
    if (lastStatus?.ready) {
      await delay(5000);
      return lastStatus;
    }
    await delay(500);
  }

  const logs = client.events.slice(-10).map((event) => JSON.stringify(event.params)).join("\n");
  throw new Error(`Timed out waiting for Unity WebGL. Last status: ${JSON.stringify(lastStatus)}\nRecent logs:\n${logs}`);
}

async function clickCanvas(client) {
  const result = await client.send("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const rect = document.querySelector("#unity-canvas").getBoundingClientRect();
      return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    })()`,
  });
  const { x, y } = result.result.value;
  await client.send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 });
  await client.send("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 });
}

async function pressKey(client, key, holdMs) {
  const codeByKey = {
    ArrowLeft: 37,
    ArrowUp: 38,
    ArrowRight: 39,
    ArrowDown: 40,
    g: 71,
    r: 82,
  };
  const code = key.length === 1 ? `Key${key.toUpperCase()}` : key;
  await client.send("Input.dispatchKeyEvent", {
    type: "keyDown",
    key,
    code,
    windowsVirtualKeyCode: codeByKey[key],
  });
  await delay(holdMs);
  await client.send("Input.dispatchKeyEvent", {
    type: "keyUp",
    key,
    code,
    windowsVirtualKeyCode: codeByKey[key],
  });
}

async function captureScreenshot(client, screenshotPath) {
  const screenshot = await client.send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const buffer = Buffer.from(screenshot.data, "base64");
  writeFileSync(screenshotPath, buffer);
  if (buffer.length < 10000) {
    throw new Error(`Screenshot looks unexpectedly small: ${buffer.length} bytes`);
  }
  return buffer.length;
}

async function main() {
  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);

  const debugPort = await findFreePort();
  const userDataDir = resolve(repoRoot, `tmp/chrome-creep-smoke-${debugPort}`);
  mkdirSync(userDataDir, { recursive: true });

  const chrome = spawn(chromePath, [
    `--remote-debugging-port=${debugPort}`,
    `--user-data-dir=${userDataDir}`,
    "--disable-extensions",
    "--no-first-run",
    "--no-default-browser-check",
    "--autoplay-policy=no-user-gesture-required",
    "--window-size=430,932",
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
      await client.send("Emulation.setDeviceMetricsOverride", {
        width: 430,
        height: 932,
        deviceScaleFactor: 2,
        mobile: true,
        screenWidth: 430,
        screenHeight: 932,
      });
      await client.send("Page.navigate", { url: appUrl });
      const status = await waitForUnity(client);
      await clickCanvas(client);
      await pressKey(client, "ArrowUp", 2200);
      await pressKey(client, "ArrowRight", 900);
      await pressKey(client, "ArrowUp", 1600);
      await pressKey(client, "ArrowLeft", 650);
      const activeScreenshotBytes = await captureScreenshot(client, activeScreenshotPath);
      await pressKey(client, "g", 120);
      await delay(1500);
      const gameOverScreenshotBytes = await captureScreenshot(client, gameOverScreenshotPath);
      await pressKey(client, "r", 120);
      await delay(1600);
      const restartScreenshotBytes = await captureScreenshot(client, restartScreenshotPath);
      const severeLogs = client.events.filter((event) => {
        const params = event.params ?? {};
        return params.level === "error" || params.entry?.level === "error";
      });
      if (severeLogs.length > 0) {
        throw new Error(`Browser reported errors:\n${severeLogs.map((event) => JSON.stringify(event.params)).join("\n")}`);
      }

      console.log(`Please Move to the Back of the Mart WebGL smoke passed: ${status.canvasWidth}x${status.canvasHeight}`);
      console.log(`Active screenshot: ${activeScreenshotPath} (${activeScreenshotBytes} bytes)`);
      console.log(`Game-over screenshot: ${gameOverScreenshotPath} (${gameOverScreenshotBytes} bytes)`);
      console.log(`Restart screenshot: ${restartScreenshotPath} (${restartScreenshotBytes} bytes)`);
    } finally {
      client.close();
    }
  } finally {
    chrome.kill();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
