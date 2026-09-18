#!/usr/bin/env node

import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import net from "node:net";
import { resolve } from "node:path";
import { assertNoRuntimeExceptions } from "./playtest-one-plus-one-minus-one-webgl.mjs";
import { CdpClient } from "./one-plus-one-minus-one-cdp-client.mjs";

const repoRoot = resolve(import.meta.dirname, "..");
const buildDir = process.env.ONE_EQUALS_ONE_WEBGL_BUILD_DIR
  ? resolve(process.env.ONE_EQUALS_ONE_WEBGL_BUILD_DIR)
  : resolve(repoRoot, "prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-qa");
const chromePath = process.env.CHROME_PATH ?? "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const outputDir = process.env.ONE_EQUALS_ONE_ROUND48_OUTPUT_DIR ?? resolve("/tmp", "one-equals-one-round48-webgl");

function delay(ms) {
  return new Promise(resolveDelay => setTimeout(resolveDelay, ms));
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

async function openDebugTab(debugPort, url) {
  const encodedUrl = encodeURIComponent(url);
  let response = await fetch(`http://127.0.0.1:${debugPort}/json/new?${encodedUrl}`, { method: "PUT" });
  if (!response.ok) response = await fetch(`http://127.0.0.1:${debugPort}/json/new?${encodedUrl}`);
  if (!response.ok) throw new Error(`Failed to open Chrome debugging tab: ${response.status}`);
  return await response.json();
}

async function waitForGame(client, timeoutMs = 120000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    assertNoRuntimeExceptions(client.events);
    const result = await client.send("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const loading = document.querySelector("#unity-loading-bar");
        const warning = document.querySelector("#unity-warning");
        const canvas = document.querySelector("#unity-canvas");
        const loadingDisplay = loading ? getComputedStyle(loading).display : "";
        const warningText = warning ? warning.innerText : "";
        return {
          ready: Boolean(canvas) && loadingDisplay === "none" && !warningText.includes("게임을 실행하지 못했어요"),
          failed: warningText.includes("게임을 실행하지 못했어요") || warningText.includes("does not support WebGL"),
          warningText
        };
      })()`,
    });
    const status = result.result?.value;
    if (status?.failed) throw new Error(`WebGL page failed: ${status.warningText}`);
    if (status?.ready) {
      await delay(2500);
      assertNoRuntimeExceptions(client.events);
      return;
    }

    await delay(500);
  }

  throw new Error("Timed out waiting for Unity WebGL.");
}

function logs(client) {
  return client.events
    .filter(e => e.method === "Runtime.consoleAPICalled")
    .map(e => e.params.args.map(a => a.value ?? a.description ?? "").join(" "));
}

async function waitForLog(client, predicate, label, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    assertNoRuntimeExceptions(client.events);
    const found = logs(client).find(predicate);
    if (found) return found;
    await delay(100);
  }

  throw new Error(`Timed out waiting for ${label}`);
}

async function screenshot(client, name) {
  const capture = await client.send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: false,
  });
  const path = resolve(outputDir, `${name}.png`);
  writeFileSync(path, Buffer.from(capture.data, "base64"));
  return path;
}

async function tap(client, x, y) {
  await client.send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: [{ x, y, id: 0 }] });
  await delay(80);
  await client.send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: [] });
  await delay(220);
}

async function drag(client, from, to) {
  await client.send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: [{ x: from[0], y: from[1], id: 0 }] });
  await delay(100);
  for (let i = 1; i <= 16; i++) {
    await client.send("Input.dispatchTouchEvent", {
      type: "touchMove",
      touchPoints: [{
        x: from[0] + (to[0] - from[0]) * i / 16,
        y: from[1] + (to[1] - from[1]) * i / 16,
        id: 0,
      }],
    });
    await delay(30);
  }
  await client.send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: [] });
  await delay(750);
}

async function rotateAndDrag(client, bank, slot, turns = 0) {
  for (let i = 0; i < turns; i++) await tap(client, bank[0], bank[1]);
  await drag(client, bank, slot);
}

async function placeExpression(client, placements) {
  for (const placement of placements) {
    await rotateAndDrag(client, placement.bank, placement.slot, placement.turns ?? 0);
  }
}

async function main() {
  if (!existsSync(chromePath)) throw new Error(`Google Chrome not found: ${chromePath}`);
  if (!existsSync(resolve(buildDir, "index.html"))) throw new Error(`WebGL QA build not found: ${buildDir}`);
  mkdirSync(outputDir, { recursive: true });

  const httpPort = await findFreePort();
  const debugPort = await findFreePort();
  const appUrl = `http://127.0.0.1:${httpPort}/index.html?qaRound=48&qaUnlocked=100`;
  const userDataDir = resolve(repoRoot, `tmp/chrome-one-equals-one-round48-${debugPort}`);
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
      await client.send("Network.enable");
      await client.send("Network.setUserAgentOverride", {
        userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
      });
      await client.send("Emulation.setDeviceMetricsOverride", {
        width: 390,
        height: 844,
        deviceScaleFactor: 3,
        mobile: true,
        screenWidth: 390,
        screenHeight: 844,
      });
      await client.send("Emulation.setTouchEmulationEnabled", { enabled: true, maxTouchPoints: 5 });
      await client.send("Page.navigate", { url: appUrl });
      await waitForGame(client);
      await waitForLog(client, l => l.includes("[Telemetry] round_start ") && l.includes("round=48,"), "Round 48 start");
      await screenshot(client, "round48-empty");

      const banks = [
        [70, 700], [121, 700], [171, 700], [222, 700],
        [272, 700], [322, 700], [196, 760],
      ];
      const slots = {
        topLeft: [144, 220],
        topRight: [242, 220],
        bottomLeft: [110, 318],
        bottomRight: [207, 318],
      };

      client.events.length = 0;
      await placeExpression(client, [
        { bank: banks[0], slot: slots.topLeft },
        { bank: banks[1], slot: slots.topRight, turns: 1 },
        { bank: banks[2], slot: slots.bottomLeft },
        { bank: banks[3], slot: slots.bottomLeft },
        { bank: banks[4], slot: slots.bottomRight },
        { bank: banks[5], slot: slots.bottomRight },
        { bank: banks[6], slot: slots.bottomRight },
      ]);
      await screenshot(client, "round48-wrong-placed");
      await tap(client, 296, 798);
      const failure = await waitForLog(client, l => l.includes("[Telemetry] round_check_failed ") && l.includes("round=48,"), "Round 48 wrong-answer rejection");
      assert.ok(!logs(client).some(l => l.includes("[Telemetry] round_clear ") && l.includes("round=48,")), "Wrong answer should not clear Round 48.");
      assert.ok(failure.includes("1 / 11 111") || failure.includes("reason="), failure);
      await screenshot(client, "round48-wrong-rejected");

      await tap(client, 195, 798);
      await waitForLog(client, l => l.includes("[Telemetry] round_reset ") && l.includes("round=48,"), "Round 48 reset");
      client.events.length = 0;
      await placeExpression(client, [
        { bank: banks[0], slot: slots.topLeft },
        { bank: banks[1], slot: slots.topLeft },
        { bank: banks[2], slot: slots.topLeft },
        { bank: banks[3], slot: slots.topRight, turns: 2 },
        { bank: banks[4], slot: slots.bottomLeft },
        { bank: banks[5], slot: slots.bottomRight },
        { bank: banks[6], slot: slots.bottomRight },
      ]);
      await screenshot(client, "round48-correct-placed");
      await tap(client, 296, 798);
      const clear = await waitForLog(client, l => l.includes("[Telemetry] round_clear ") && l.includes("round=48,"), "Round 48 correct-answer clear");
      assert.ok(clear.includes("expression=111 - 1 11"), clear);
      await screenshot(client, "round48-correct-cleared");

      const results = {
        passed: true,
        wrongAnswerRejected: failure.trim(),
        correctAnswerCleared: clear.trim(),
        buildDir,
        appUrl,
      };
      writeFileSync(resolve(outputDir, "results.json"), JSON.stringify(results, null, 2));
      console.log(`Round 48 WebGL regression passed: ${resolve(outputDir, "results.json")}`);
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
  console.error(error instanceof Error ? error.stack || error.message : String(error));
  process.exit(1);
}
