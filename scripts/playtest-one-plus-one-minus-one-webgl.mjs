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

export async function playtestFirstTenRounds(client, appUrl, outputDir, { waitUntilReady, validateScreenshot }) {
  const appQuery = new URL(appUrl).searchParams;
  assert.equal(appQuery.has("qaRound"), false, "Input QA must not use QA round jumps");
  assert.equal(appQuery.has("qaMode"), false, "Input QA must start in the default mode");
  assert.equal(appQuery.has("qaFillSample"), false, "Input QA must not use sample fill");
  assert.equal(appQuery.get("qaInputProbe"), "1", "Input QA needs coordinate probe logs, not sample-fill hooks");
  mkdirSync(outputDir, { recursive: true });
  const dprResult = await client.send("Runtime.evaluate", {
    returnByValue: true,
    expression: "window.devicePixelRatio || 1",
  });
  const devicePixelRatio = Number(dprResult.result?.value) || 1;
  const logs = () => client.events
    .filter(e => e.method === "Runtime.consoleAPICalled")
    .map(e => e.params.args.map(a => a.value ?? a.description ?? "").join(" "));
  const cssPoint = point => ({
    ...(point ?? {}),
    x: Math.round((point?.x ?? 0) / devicePixelRatio),
    y: Math.round((point?.y ?? 0) / devicePixelRatio),
  });
  const cssLayout = layout => ({
    ...layout,
    slots: layout.slots.map(cssPoint),
    banks: layout.banks.map(cssPoint),
    check: cssPoint(layout.check),
    collection: cssPoint(layout.collection),
  });
  function latestLayout(roundNumber) {
    for (let i = logs().length - 1; i >= 0; i--) {
      const message = logs()[i];
      const marker = "[QA_INPUT_LAYOUT] ";
      const markerIndex = message.indexOf(marker);
      if (markerIndex < 0) continue;
      const parsed = JSON.parse(message.slice(markerIndex + marker.length));
      if (!roundNumber || parsed.round === roundNumber) return cssLayout(parsed);
    }
    return null;
  }
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
  function hasTelemetryRound(message, roundNumber) {
    return new RegExp(`(?:^|[ ,])round=${roundNumber},`).test(message);
  }
  async function waitForLayout(roundNumber, label = `Round ${roundNumber} layout`) {
    const deadline = Date.now() + 15000;
    while (Date.now() < deadline) {
      assertNoRuntimeExceptions(client.events);
      const layout = latestLayout(roundNumber);
      if (layout?.slots?.length > 0 && layout?.banks && layout?.check) return layout;
      await delay(100);
    }
    throw new Error(`Input playtest timed out: ${label}`);
  }
  function latestCollectionState() {
    for (let i = logs().length - 1; i >= 0; i--) {
      const message = logs()[i];
      const marker = "[QA_COLLECTION_STATE] ";
      const markerIndex = message.indexOf(marker);
      if (markerIndex < 0) continue;
      return JSON.parse(message.slice(markerIndex + marker.length));
    }
    return null;
  }
  async function waitForCollectionState(predicate, label) {
    const deadline = Date.now() + 15000;
    while (Date.now() < deadline) {
      assertNoRuntimeExceptions(client.events);
      const state = latestCollectionState();
      if (state && predicate(state)) return state;
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
    const layout = await waitForLayout(1);
    const bank = layout.banks[0];
    const slot = layout.slots[0];
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x: bank.x, y: bank.y, id: 0}]});
    await delay(100);
    await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{x: slot.x, y: slot.y, id: 0}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchCancel", touchPoints: []});
    await delay(250);
    await screenshot("touch-cancel-preserves-bank");

    const restored = await waitForLayout(1);
    const restoredBank = restored.banks[0];
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [{x: restoredBank.x, y: restoredBank.y, id: 0}]});
    await delay(100);
    await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{x: slot.x, y: slot.y, id: 0}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchStart", touchPoints: [
      {x: slot.x, y: slot.y, id: 0}, {x: restoredBank.x, y: restoredBank.y, id: 1}
    ]});
    await delay(150);
    // Exercise Unity's focus handler without pretending this is native app suspension.
    await client.send("Runtime.evaluate", {expression: "window.dispatchEvent(new Event('blur'))"});
    await delay(150);
    // CDP releases removed points when the active set changes; touchEnd ends all points.
    await client.send("Input.dispatchTouchEvent", {type: "touchMove", touchPoints: [{x: 194, y: 670, id: 1}]});
    await delay(150);
    await client.send("Input.dispatchTouchEvent", {type: "touchEnd", touchPoints: []});
    await client.send("Runtime.evaluate", {expression: "window.dispatchEvent(new Event('focus'))"});
    await delay(250);
    await screenshot("secondary-release-after-focus-cancel");
    // The ordinary Round 1 drag/clear below must still produce exactly one vertical stick.
  }

  const tokenTurns = new Map([
    ["1", [0]], ["11", [0, 0]], ["111", [0, 0, 0]],
    ["-", [2]], ["/", [1]], ["+", [0, 2]], ["×", [1, 3]], ["*", [0, 1, 3]], ["=", [2, 2]],
  ]);
  const rounds = [
    { expression: "1", symbols: ["1"] },
    { expression: "1 / 1", symbols: ["1", "/", "1"] },
    { expression: "1 + 1 - 1", symbols: ["1", "+", "1", "-", "1"] },
    { expression: "1 * 1", symbols: ["1", "*", "1"] },
    { expression: "11 / 11", symbols: ["11", "/", "11"] },
    { expression: "11 × 1 - 11 + 1", symbols: ["11", "×", "1", "-", "11", "+", "1"] },
    { expression: "111 - 111 + 1", symbols: ["111", "-", "111", "+", "1"] },
    { expression: "11 / 11 × 1", symbols: ["11", "/", "11", "×", "1"] },
    { expression: "1 1 - 1 1 + 1", symbols: ["1", "1", "-", "1", "1", "+", "1"] },
    { expression: "1 + 1 - 1 × 1 / 1", symbols: ["1", "+", "1", "-", "1", "×", "1", "/", "1"] }
  ];
  const report = [];
  try {
    for (const [index, round] of rounds.entries()) {
      const n = index + 1;
      await waitForLog(l => l.includes("[Telemetry] round_start ") && hasTelemetryRound(l, n), `Round ${n} start`);
      await delay(350);
      for (const [slotIndex, symbol] of round.symbols.entries()) {
        const turnsList = tokenTurns.get(symbol);
        assert.ok(turnsList, `No input recipe for ${symbol}`);
        for (const turns of turnsList) {
          let layout = await waitForLayout(n);
          assert.ok(layout.banks.length > 0, `Round ${n} has no bank stick left before ${symbol}`);
          for (let t = 0; t < turns; t++) {
            await tap(layout.banks[0].x, layout.banks[0].y);
            layout = await waitForLayout(n);
          }
          await drag([layout.banks[0].x, layout.banks[0].y], [layout.slots[slotIndex].x, layout.slots[slotIndex].y]);
        }
      }
      await screenshot(`round-${n}-placed`);
      const layout = await waitForLayout(n);
      await tap(layout.check.x, layout.check.y);
      const clear = await waitForLog(l => l.includes("[Telemetry] round_clear ") && hasTelemetryRound(l, n), `Round ${n} clear`);
      assert.ok(clear.includes(`expression=${round.expression}\n`) || clear.endsWith(`expression=${round.expression}`), clear);
      assert.ok(clear.includes("is_replay=false"), clear);
      const ad = await waitForLog(l => l.includes("ad_interstitial_opportunity") && hasTelemetryRound(l, n), `Round ${n} ad policy`);
      assert.ok(ad.includes("is_replay=false"), ad);
      assert.ok(ad.includes(n === 10 ? "reason=round_milestone" : n <= 5 ? "reason=early_round" : "reason=cadence"), ad);
      assert.ok(ad.includes(n === 10 ? "eligible=true" : "eligible=false"), ad);
      assert.ok(ad.includes("will_show=false"), "WebGL has no configured native ad: " + ad);
      await waitForLog(l => l.includes("[Telemetry] round_start ") && hasTelemetryRound(l, n + 1), `Round ${n + 1} transition`);
      report.push({ round: n, clear: clear.trim(), ad: ad.trim() });
      console.log(`Input playtest Round ${n}: PASS (${round.expression})`);
    }
    const collectionLayout = await waitForLayout(11, "Round 11 collection layout");
    await delay(1200);
    await tap(collectionLayout.collection.x, collectionLayout.collection.y);
    const collection = await waitForCollectionState(
      state => state.mode === "make_one" && state.active === true && state.friends >= 8 && state.badges >= 5 &&
        state.answerRound === 10 && state.answers === 1 && state.firstAnswer === "1 + 1 - 1 × 1 / 1",
      "collection overlay after first-ten clear"
    );
    await screenshot("collection-after-first-ten");
    client.events.length = 0;
    await client.send("Page.reload");
    await waitUntilReady(client);
    await waitForLog(l => l.includes("[Telemetry] app_open ") && l.includes("highest_unlocked_round=11,"), "saved progress after reload");
    await waitForLog(l => l.includes("[Telemetry] round_start ") && hasTelemetryRound(l, 11), "Round 11 restored");
    await screenshot("restored-round-select");
    writeFileSync(resolve(outputDir, "results.json"), JSON.stringify({
      passed: true, rounds: report, savedProgress: 11, physicalDevice: false,
      collection,
      inputEdges: []
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
