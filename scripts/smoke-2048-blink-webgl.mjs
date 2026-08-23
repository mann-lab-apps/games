import { spawn } from "node:child_process";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import net from "node:net";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const publicDir = resolve(repoRoot, "web/mannlab-games/public");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const screenshotPath = resolve("/tmp", "2048-blink-webgl-smoke-ready.png");

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

async function waitForGame(client) {
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
          ready: Boolean(canvas) && loadingDisplay === "none" && !warningText.includes("게임을 실행하지 못했어요"),
          failed: warningText.includes("게임을 실행하지 못했어요") || warningText.includes("does not support WebGL"),
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
      await delay(15000);
      return;
    }
    await delay(500);
  }

  const logs = client.events.slice(-10).map((event) => JSON.stringify(event.params)).join("\n");
  throw new Error(`Timed out waiting for Unity WebGL. Last status: ${JSON.stringify(lastStatus)}\nRecent logs:\n${logs}`);
}

async function pageStatus(client) {
  const result = await client.send("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const warning = document.querySelector("#unity-warning");
      return {
        warningText: warning ? warning.innerText : "",
        warningDisplay: warning ? getComputedStyle(warning).display : ""
      };
    })()`,
  });
  return result.result?.value ?? {};
}

async function performSmokeMoves(client) {
  const keyCodes = {
    ArrowLeft: 37,
    ArrowUp: 38,
    ArrowRight: 39,
    ArrowDown: 40,
  };
  const keys = ["ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp"];
  for (const key of keys) {
    await client.send("Input.dispatchKeyEvent", {
      type: "keyDown",
      key,
      code: key,
      windowsVirtualKeyCode: keyCodes[key],
    });
    await client.send("Input.dispatchKeyEvent", {
      type: "keyUp",
      key,
      code: key,
      windowsVirtualKeyCode: keyCodes[key],
    });
    await delay(850);

    const status = await pageStatus(client);
    if (status.warningText.includes("게임을 실행하지 못했어요")) {
      throw new Error(`WebGL page failed after ${key}: ${status.warningText}`);
    }
  }
}

async function main() {
  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);

  const httpPort = await findFreePort();
  const debugPort = await findFreePort();
  const appUrl = `http://127.0.0.1:${httpPort}/games/2048-blink/`;
  const userDataDir = resolve(repoRoot, `tmp/chrome-2048-blink-smoke-${debugPort}`);
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
    "--window-size=1284,2778",
    "about:blank",
  ], { stdio: "ignore" });

  try {
    await waitForHttp(`http://127.0.0.1:${httpPort}/games/2048-blink/index.html`);
    await waitForHttp(`http://127.0.0.1:${debugPort}/json/version`);

    const target = await openDebugTab(debugPort, appUrl);
    const client = new CdpClient(target.webSocketDebuggerUrl);
    await client.open();
    try {
      await client.send("Page.enable");
      await client.send("Runtime.enable");
      await client.send("Log.enable");
      await client.send("Emulation.setDeviceMetricsOverride", {
        width: 1284,
        height: 2778,
        deviceScaleFactor: 1,
        mobile: true,
        screenWidth: 1284,
        screenHeight: 2778,
      });
      await client.send("Page.navigate", { url: appUrl });
      await waitForGame(client);
      await performSmokeMoves(client);
      const capture = await client.send("Page.captureScreenshot", {
        format: "png",
        fromSurface: true,
        captureBeyondViewport: false,
      });
      writeFileSync(screenshotPath, Buffer.from(capture.data, "base64"));
      console.log(`WebGL smoke screenshot: ${screenshotPath}`);
    } finally {
      client.close();
    }
  } finally {
    chrome.kill("SIGTERM");
    server.kill("SIGTERM");
  }
}

await main();
