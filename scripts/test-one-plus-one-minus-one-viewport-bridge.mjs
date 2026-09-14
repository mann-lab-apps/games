import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { runInNewContext } from "node:vm";

const handlers = { window: new Map(), document: new Map() };
const document = {
  hidden: false,
  addEventListener(type, listener) {
    assert.equal(handlers.document.has(type), false, `Duplicate document ${type} listener`);
    handlers.document.set(type, listener);
  },
};
const window = {
  addEventListener(type, listener) {
    assert.equal(handlers.window.has(type), false, `Duplicate window ${type} listener`);
    handlers.window.set(type, listener);
  },
};
const library = {};
runInNewContext(readFileSync(new URL("../prototypes/one-plus-one-minus-one/Assets/Plugins/WebGL/OneEqualsOneViewport.jslib", import.meta.url), "utf8"), {
  Module: {}, document, window,
  LibraryManager: { library }, mergeInto: Object.assign,
});
const version = library.OneEqualsOneInputInterruptionVersion;
assert.equal(typeof version, "function");
assert.equal(version(), 0);
for (let i = 0; i < 100; i++) assert.equal(version(), 0);
handlers.window.get("blur")();
assert.equal(version(), 1);
document.hidden = true;
handlers.document.get("visibilitychange")();
// The game may stop updating while hidden: resuming must not erase the event.
document.hidden = false;
handlers.document.get("visibilitychange")();
assert.equal(version(), 2);
assert.equal(version(), 2);
assert.equal(handlers.window.size, 1);
assert.equal(handlers.document.size, 1);
console.log("1 = 1 WebGL input interruption bridge: PASS");
