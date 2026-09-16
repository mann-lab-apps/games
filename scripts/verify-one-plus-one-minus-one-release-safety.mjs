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
const buildIosPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Editor/BuildIosXcode.cs",
);
const buildAndroidPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Editor/BuildAndroidAab.cs",
);

const controller = fs.readFileSync(controllerPath, "utf8");
const buildWebGl = fs.readFileSync(buildWebGlPath, "utf8");
const buildIos = fs.readFileSync(buildIosPath, "utf8");
const buildAndroid = fs.readFileSync(buildAndroidPath, "utf8");
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
    const conditionActive = evaluateReleaseCondition(trimmed.slice(4));
    activeStack.push({ parentActive: isActive(), conditionActive, branchMatched: conditionActive });
    continue;
  }

  if (trimmed.startsWith("#elif ")) {
    const frame = activeStack.at(-1);
    if (!frame) {
      violations.push(`${index + 1}: unexpected #elif`);
      continue;
    }

    const conditionActive = !frame.branchMatched && evaluateReleaseCondition(trimmed.slice(6));
    frame.conditionActive = conditionActive;
    frame.branchMatched = frame.branchMatched || conditionActive;
    continue;
  }

  if (trimmed.startsWith("#else")) {
    const frame = activeStack.at(-1);
    if (!frame) {
      violations.push(`${index + 1}: unexpected #else`);
      continue;
    }

    frame.conditionActive = !frame.branchMatched;
    frame.branchMatched = true;
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

if (!buildIos.includes("BuildIos(ReleaseOutputPath, false, iOSSdkVersion.DeviceSDK, false, true);")) {
  violations.push("iOS release build should be non-development device SDK and require production AdMob IDs.");
}

if (!buildIos.includes("BuildIos(CrashlyticsTestOutputPath, true, iOSSdkVersion.DeviceSDK);")) {
  violations.push("iOS Crashlytics test build should stay explicit and development-only.");
}

if (!buildIos.includes("BuildIos(AdMobTestOutputPath, false, iOSSdkVersion.DeviceSDK, true);")) {
  violations.push("iOS AdMob test build should stay explicit and force test ads only.");
}

if (!buildIos.includes("options = developmentBuild ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None")) {
  violations.push("iOS release builds should use BuildOptions.None when not development builds.");
}

if (!buildAndroid.includes("BuildAndroid(AabOutputPath, true, false);")) {
  violations.push("Android AAB release build should be an app bundle without forced test ads.");
}

if (!buildAndroid.includes("BuildAndroid(AdMobTestApkOutputPath, false, true);")) {
  violations.push("Android AdMob test APK should stay explicit and force test ads only.");
}

if (!buildAndroid.includes("var requireProductionAds = buildAppBundle && !forceAdMobTestAds;")) {
  violations.push("Android production AdMob IDs should be required for non-test AAB builds.");
}

if (!buildAndroid.includes("options = BuildOptions.None")) {
  violations.push("Android builds should not enable development build options by default.");
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
