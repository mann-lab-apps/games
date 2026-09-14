import assert from "node:assert/strict";
import test from "node:test";
import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { CdpClient } from "./one-plus-one-minus-one-cdp-client.mjs";
import { assertNoRuntimeExceptions, playtestFirstTenRounds } from "./playtest-one-plus-one-minus-one-webgl.mjs";

class FakeSocket extends EventTarget {
  readyState = 0;
  sent = [];
  connect() { this.readyState = 1; this.dispatchEvent(new Event("open")); }
  send(data) { this.sent.push(JSON.parse(data)); }
  receive(message) { this.dispatchEvent(new MessageEvent("message", { data: JSON.stringify(message) })); }
  close() { this.readyState = 3; this.dispatchEvent(new Event("close")); }
}

async function connected() {
  const client = new CdpClient("ws://test.invalid", { WebSocketImpl: FakeSocket, timeoutMs: 50 });
  const opening = client.open();
  client.socket.connect();
  await opening;
  return client;
}

test("runtime exceptions reach the existing smoke failure gate", async () => {
  const client = await connected();
  try {
    client.socket.receive({ method: "Runtime.exceptionThrown", params: { exceptionDetails: { text: "test failure" } } });
    assert.throws(() => assertNoRuntimeExceptions(client.events), /Browser runtime exception/);
  } finally { client.close(); }
});

test("responses are matched by id and protocol errors reject", async () => {
  const client = await connected();
  try {
    const first = client.send("First"), second = client.send("Second");
    const rejected = assert.rejects(second, /unsupported/);
    client.socket.receive({ id: 2, error: { message: "unsupported" } });
    client.socket.receive({ id: 1, result: { value: 7 } });
    assert.deepEqual(await first, { value: 7 });
    await rejected;
    assert.equal(client.pending.size, 0);
  } finally { client.close(); }
});

test("missing response times out and late response cannot settle a new command", async () => {
  const client = await connected();
  try {
    await assert.rejects(client.send("Page.captureScreenshot"), /timed out: Page.captureScreenshot/);
    assert.equal(client.pending.size, 0);
    const next = client.send("Next");
    client.socket.receive({ id: 1, result: { stale: true } });
    assert.equal(client.pending.size, 1);
    client.socket.receive({ id: 2, result: { current: true } });
    assert.deepEqual(await next, { current: true });
  } finally { client.close(); }
});

for (const event of ["close", "error"]) {
  test(`connection ${event} releases pending commands`, async () => {
    const client = await connected();
    try {
      const rejected = assert.rejects(client.send("Waiting"), /CDP connection/);
      client.socket.dispatchEvent(new Event(event));
      await rejected;
      assert.equal(client.pending.size, 0);
    } finally { client.close(); }
  });
}

test("explicit close releases pending commands and rejects new ones", async () => {
  const client = await connected();
  const rejected = assert.rejects(client.send("Waiting"), /CDP connection closed/);
  client.close();
  await rejected;
  assert.equal(client.pending.size, 0);
  await assert.rejects(client.send("TooLate"), /not open/);
});

test("connection opening is bounded", async () => {
  const client = new CdpClient("ws://test.invalid", { WebSocketImpl: FakeSocket, timeoutMs: 20 });
  try { await assert.rejects(client.open(), /connection timed out/); }
  finally { client.close(); }
});

test("send failure releases its pending entry", async () => {
  const client = await connected();
  try {
    client.socket.send = () => { throw new Error("send failed"); };
    await assert.rejects(client.send("Fail"), /send failed/);
    assert.equal(client.pending.size, 0);
  } finally { client.close(); }
});

test("failed evidence capture does not hide the original input test failure", async () => {
  const output = mkdtempSync(join(tmpdir(), "one-equals-one-cdp-test-"));
  const client = {
    events: [{ method: "Runtime.exceptionThrown", params: { exceptionDetails: { text: "original failure" } } }],
    async send() { throw new Error("Capture unavailable"); },
  };
  try {
    await assert.rejects(playtestFirstTenRounds(client, "http://example.invalid/index.html", output, {
      waitUntilReady: async () => {}, validateScreenshot: () => {},
    }), /original failure/);
    const report = JSON.parse(readFileSync(join(output, "results.json"), "utf8"));
    assert.equal(report.passed, false);
    assert.match(report.error, /original failure/);
    assert.match(report.screenshotError, /Capture unavailable/);
  } finally { rmSync(output, { recursive: true, force: true }); }
});
