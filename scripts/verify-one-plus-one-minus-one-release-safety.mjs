#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const controllerPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneController.cs",
);
const buildWebGlPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Editor/BuildWebGL.cs",
);

const controller = fs.readFileSync(controllerPath, "utf8");
const buildWebGl = fs.readFileSync(buildWebGlPath, "utf8");
const lines = controller.split(/\r?\n/);
const activeStack = [];
const violations = [];

const releaseForbiddenPatterns = [
  /--mannlab-qa-/,
  /"qaRound"/,
  /"qaUnlocked"/,
  /"qaRounds"/,
  /"qaRoundPage"/,
  /"qaFillSample"/,
  /CrashlyticsTestArgument/,
  /CrashlyticsTestEnvironmentVariable/,
  /CrashlyticsTestTap/,
  /ShouldForceCrashlyticsTestOnLaunch/,
  /ForceCrashlyticsTestAfterStartup/,
  /HandleCrashlyticsTestTrigger/,
  /TriggerCrashlyticsTest/,
  /crashlytics_test_trigger/,
];

for (let index = 0; index < lines.length; index += 1) {
  const line = lines[index];
  const trimmed = line.trim();
  if (trimmed.startsWith("#if ")) {
    activeStack.push({ parentActive: isActive(), conditionActive: evaluateReleaseCondition(trimmed.slice(4)) });
    continue;
  }

  if (trimmed.startsWith("#else")) {
    const frame = activeStack.at(-1);
    if (!frame) {
      violations.push(`${index + 1}: unexpected #else`);
      continue;
    }

    frame.conditionActive = !frame.conditionActive;
    continue;
  }

  if (trimmed.startsWith("#endif")) {
    if (!activeStack.pop()) {
      violations.push(`${index + 1}: unexpected #endif`);
    }

    continue;
  }

  if (!isActive()) {
    continue;
  }

  for (const pattern of releaseForbiddenPatterns) {
    if (pattern.test(line)) {
      violations.push(`${index + 1}: release-active QA/test hook matched ${pattern}: ${trimmed}`);
    }
  }
}

if (activeStack.length > 0) {
  violations.push("unterminated preprocessor block");
}

if (!buildWebGl.includes("BuildInternal(OutputPath, false);")) {
  violations.push("Release WebGL build should use non-development BuildInternal(OutputPath, false).");
}

if (!buildWebGl.includes("BuildInternal(QaOutputPath, true);")) {
  violations.push("QA WebGL build should stay explicit and development-only.");
}

if (!buildWebGl.includes("BuildInternal(StoreCaptureOutputPath, false, true);")) {
  violations.push("Store-capture WebGL build should be non-development with the store-capture define only.");
}

if (!controller.includes("#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE")) {
  violations.push("QA and test hooks should remain behind the development/editor/store-capture preprocessor guard.");
}

if (!controller.includes("#if MANNLAB_ADMOB_FORCE_TEST_ADS")) {
  violations.push("Forced AdMob test cadence should remain build-define-gated.");
}

if (violations.length > 0) {
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }

  console.error(`1 = 1 release safety verification failed with ${violations.length} issue(s).`);
  process.exit(1);
}

console.log("1 = 1 release safety verification passed.");

function isActive() {
  return activeStack.every((frame) => frame.parentActive && frame.conditionActive);
}

function evaluateReleaseCondition(expression) {
  const orTerms = expression.split("||");
  if (orTerms.length > 1) {
    return orTerms.some((term) => evaluateAndCondition(term));
  }

  return evaluateAndCondition(expression);
}

function evaluateAndCondition(expression) {
  return expression.split("&&").every((term) => evaluateTerm(term));
}

function evaluateTerm(term) {
  const value = term.trim().replace(/^\(+|\)+$/g, "");
  if (value.startsWith("!")) {
    return !evaluateTerm(value.slice(1));
  }

  return false;
}
