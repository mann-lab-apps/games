import assert from "node:assert/strict";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const delay = ms => new Promise(resolveDelay => setTimeout(resolveDelay, ms));

export function assertNoRuntimeExceptions(events) {
  for (const event of events) {
    if (event.method === "Runtime.exceptionThrown") {
      throw new Error(`Browser runtime exception: ${JSON.stringify(event.params.exceptionDetails)}`);
    }
    if (event.method !== "Runtime.consoleAPICalled") continue;
    const message = event.params.args.map(arg => arg.value ?? arg.description ?? "").join(" ");
    if (/\b\w*Exception:|\bRuntimeError:/.test(message)) {
      throw new Error(`Unity runtime exception: ${message}`);
    }
  }
}

// Coordinates come from inspected 390x844 captures, not QA sample-fill hooks.
export async function playtestFirstTenRounds(client, appUrl, outputDir, { waitUntilReady, validateScreenshot }) {
  assert.equal(new URL(appUrl).search, "", "Input QA must start without QA round/sample overrides");
  mkdirSync(outputDir, { recursive: true });
  const logs = () => client.events
    .filter(e => e.method === "Runtime.consoleAPICalled")
    .map(e => e.params.args.map(a => a.value ?? a.description ?? "").join(" "));
  async function waitForLog(predicate, label) {
    const deadline = Date.now() + 15000;
    while (Date.now() < deadline) {
      assertNoRuntimeExceptions(client.events);
      const found = logs().find(predicate);
      if (found) return found;
      await delay(100);
    }
    throw new Error(`Input playtest timed out: ${label}`);
  }
  async function screenshot(name) {
    const capture = await client.send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
    const bytes = Buffer.from(capture.data, "base64");
    writeFileSync(resolve(outputDir, `${name}.png`), bytes);
    validateScreenshot(bytes, `iphone-input-${name}`);
  }
  async function tap(x, y) {
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x, y, id: 0}]});
    await delay(80);
    await client.send("Input.dispatchTouchEvent", {type: "touchEnd", touchPoints: []});
    await delay(250);
  }
  async function drag(from, to) {
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x: from[0], y: from[1], id: 0}]});
    await delay(100);
    for (let i = 1; i <= 16; i++) {
      await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{
        x: from[0] + (to[0] - from[0]) * i / 16,
        y: from[1] + (to[1] - from[1]) * i / 16, id: 0
      }]});
      await delay(30);
    }
    await client.send("Input.dispatchTouchEvent", {type: "touchEnd", touchPoints: []});
    await delay(300);
  }

  async function cancelFirstRoundPickup() {
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x: 194, y: 670, id: 0}]});
    await delay(100);
    await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{x: 167, y: 430, id: 0}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchCancel", touchPoints: []});
    await delay(250);
    await screenshot("touch-cancel-preserves-bank");

    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x: 194, y: 670, id: 0}]});
    await delay(100);
    await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{x: 167, y: 430, id: 0}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [
      {x: 167, y: 430, id: 0}, {x: 194, y: 670, id: 1}
    ]});
    await delay(150);
    // Exercise Unity's focus handler without pretending this is native app suspension.
    await client.send("Runtime.evaluate", {expression: "window.dispatchEvent(new Event('blur'))"});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchEnd", touchPoints: [{x: 194, y: 670, id: 1}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchEnd", touchPoints: []});
    await client.send("Runtime.evaluate", {expression: "window.dispatchEvent(new Event('focus'))"});
    await delay(250);
    await screenshot("secondary-release-after-focus-cancel");
    // The ordinary Round 1 drag/clear below must still produce exactly one vertical stick.
  }

  const rounds = [
    { expression: "1", slots: [167], placements: [[0, 0]] },
    { expression: "1 + 1", slots: [70, 157, 243], placements: [[0, 0], [1, 0], [1, 2], [2, 0]] },
    { expression: "1 - 1", slots: [70, 157, 243], placements: [[0, 0], [1, 2], [2, 0]] },
    { expression: "1 / 1", slots: [70, 157, 243], placements: [[0, 0], [1, 1], [2, 0]] },
    { expression: "1 × 1", slots: [70, 157, 243], placements: [[0, 0], [1, 1], [1, 3], [2, 0]] },
    { expression: "11", slots: [156], placements: [[0, 0], [0, 0]] },
    { expression: "1 1", slots: [96, 215], placements: [[0, 0], [1, 0]] },
    { expression: "111", slots: [156], placements: [[0, 0], [0, 0], [0, 0]] },
    { expression: "1 * 1", slots: [70, 157, 243], placements: [[0, 0], [1, 0], [1, 1], [1, 3], [2, 0]] },
    { expression: "11 + 1", slots: [70, 157, 243], placements: [[0, 0], [0, 0], [1, 0], [1, 2], [2, 0]] }
  ];
  const bankXs = {
    1: [194], 2: [158, 231], 3: [122, 194, 267],
    4: [86, 158, 231, 303], 5: [75, 135, 194, 254, 314]
  };
  const report = [];
  try {
    for (const [index, round] of rounds.entries()) {
      const n = index + 1;
      await waitForLog(l => l.includes("[Telemetry] round_start ") && l.includes(`round=${n},`), `Round ${n} start`);
      await delay(350);
      client.events.length = 0;
      if (n === 1) await cancelFirstRoundPickup();
      for (const [bankIndex, [slot, turns]] of round.placements.entries()) {
        const x = bankXs[round.placements.length][bankIndex];
        for (let t = 0; t < turns; t++) await tap(x, 670);
        await drag([x, 670], [round.slots[slot], 430]);
      }
      await screenshot(`round-${n}-placed`);
      await tap(294, 798);
      const clear = await waitForLog(l => l.includes("[Telemetry] round_clear ") && l.includes(`round=${n},`), `Round ${n} clear`);
      assert.ok(clear.includes(`expression=${round.expression}\n`) || clear.endsWith(`expression=${round.expression}`), clear);
      assert.ok(clear.includes("is_replay=false"), clear);
      const ad = await waitForLog(l => l.includes("ad_interstitial_opportunity") && l.includes(`round=${n},`), `Round ${n} ad policy`);
      assert.ok(ad.includes("is_replay=false"), ad);
      assert.ok(ad.includes(n === 10 ? "reason=round_milestone" : n <= 5 ? "reason=early_round" : "reason=cadence"), ad);
      assert.ok(ad.includes(n === 10 ? "eligible=true" : "eligible=false"), ad);
      assert.ok(ad.includes("will_show=false"), "WebGL has no configured native ad: " + ad);
      await waitForLog(l => l.includes("[Telemetry] round_start ") && l.includes(`round=${n + 1},`), `Round ${n + 1} transition`);
      report.push({ round: n, clear: clear.trim(), ad: ad.trim() });
      console.log(`Input playtest Round ${n}: PASS (${round.expression})`);
    }
    client.events.length = 0;
    await client.send("Page.reload");
    await waitUntilReady(client);
    await waitForLog(l => l.includes("[Telemetry] app_open ") && l.includes("highest_unlocked_round=11,"), "saved progress after reload");
    await waitForLog(l => l.includes("[Telemetry] round_start ") && l.includes("round=11,"), "Round 11 restored");
    await screenshot("restored-round-select");
    writeFileSync(resolve(outputDir, "results.json"), JSON.stringify({
      passed: true, rounds: report, savedProgress: 11, physicalDevice: false,
      inputEdges: ["touch cancellation", "secondary release after synthetic focus loss"]
    }, null, 2));
  } catch (error) {
    let screenshotError;
    try { await screenshot("failure"); }
    catch (captureError) { screenshotError = String(captureError); }
    writeFileSync(resolve(outputDir, "results.json"), JSON.stringify({
      passed: false, rounds: report, error: String(error), screenshotError, logs: logs()
    }, null, 2));
    throw error;
  }
}
