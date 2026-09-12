#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const rulesPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs",
);
const controllerPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneController.cs",
);

const source = fs.readFileSync(rulesPath, "utf8");
const controllerSource = fs.readFileSync(controllerPath, "utf8");

const tokenCosts = new Map([
  ["1", 1],
  ["11", 2],
  ["111", 3],
  ["-", 1],
  ["/", 1],
  ["=", 2],
  ["+", 2],
  ["×", 2],
  ["x", 2],
  ["*", 3],
]);

const operatorTokens = new Set(["+", "-", "/", "=", "×", "x", "*"]);
const postTutorialForbiddenWords = [
  "Star",
  "Cross",
  "Slash",
  "Divide",
  "Times",
  "Multiply",
  "Plus",
  "Minus",
  "Eleven",
  "Triple",
  "Hundred",
  "Ninety",
  "Twenty",
  "Twelve",
  "Ten",
  "One",
];

const releaseRoundCount = 100;
const releasePageSize = 12;
const releasePageCount = 9;
const releaseMaxSlots = 11;
const releaseMaxEquationRows = 3;
const releaseCompactMaxEquationRows = 4;
const releaseMinSlotWidth = 104;
const releaseMinSlotHeight = 118;
const releaseCompactMinSlotWidth = 86;
const releaseCompactMinSlotHeight = 98;
const releaseMinRoundSelectPanelWidth = 260;
const releaseMinRoundSelectPanelHeight = 420;
const releaseMinRoundSelectCellWidth = 88;
const releaseMinRoundSelectCellHeight = 58;
const releaseTallPortraitStageLiftRatio = 0.42;
const releaseTallPortraitStageLiftMax = 320;
const releaseRoundSelectPortraitLiftRatio = 0.45;
const releaseRoundSelectPortraitLiftMax = 460;
const releaseRoundSelectTopMargin = 24;
const narrowPortraitWidth = 488;

const rounds = extractRounds(source);

let failures = 0;
const seenSamples = new Map();

assert(rounds.length === releaseRoundCount, `Expected ${releaseRoundCount} rounds, got ${rounds.length}.`);
assert(
  Math.ceil(rounds.length / releasePageSize) === releasePageCount,
  `Expected ${releasePageCount} round-select pages for ${rounds.length} rounds.`,
);
assert(
  /ReleaseWebGlReferenceResolution\s*=\s*new Vector2\(720f,\s*1280f\)/.test(controllerSource),
  "WebGL reference resolution should keep mobile browser UI readable.",
);
assert(
  controllerSource.includes("useMobilePortraitScale") &&
    controllerSource.includes("ReleaseNativeReferenceResolution"),
  "WebGL scaling should keep desktop viewports on the native reference resolution.",
);
assert(controllerSource.includes("CalculateStartupFlowPlan"), "Startup flow should stay centralized and verifiable.");
assert(controllerSource.includes("CalculateInterstitialDecision"), "Interstitial cadence should stay centralized and verifiable.");
assertStartupFlowPlans();
assertInterstitialDecisionPlans();
assertStageLayouts();
assertRoundSelectLayouts();

rounds.forEach((round, index) => {
  const roundNumber = index + 1;
  const symbols = splitSymbols(round.sample);
  const usedSticks = symbols.reduce((total, symbol) => {
    assert(tokenCosts.has(symbol), `Round ${roundNumber} uses unknown token '${symbol}'.`);
    return total + tokenCosts.get(symbol);
  }, 0);

  assert(symbols.length <= releaseMaxSlots, `Round ${roundNumber} has ${symbols.length} slots.`);
  assert(usedSticks === round.stickCount, `Round ${roundNumber} stick count mismatch.`);

  const solved = solveRound(symbols, round.target);
  assert(solved.valid, `Round ${roundNumber} sample does not solve: ${solved.reason}`);

  const previous = seenSamples.get(round.sample);
  if (previous !== undefined && !(previous === 30 && roundNumber === 100)) {
    assert(false, `Round ${roundNumber} repeats Round ${previous}: ${round.sample}`);
  } else if (previous === undefined) {
    seenSamples.set(round.sample, roundNumber);
  }

  if (index < 15) {
    assert(round.tutorial.trim().length > 0, `Round ${roundNumber} should keep tutorial copy.`);
  } else {
    assert(round.tutorial.trim().length === 0, `Round ${roundNumber} should not show tutorial copy.`);
    for (const word of postTutorialForbiddenWords) {
      assert(
        !round.name.toLowerCase().includes(word.toLowerCase()),
        `Round ${roundNumber} title is too direct: ${round.name}`,
      );
    }
  }

  const fixedTarget = !symbols.includes("=");
  assertEquationLayouts(roundNumber, symbols.length, fixedTarget);
});

if (failures > 0) {
  console.error(`1 = 1 static round verification failed with ${failures} issue(s).`);
  process.exit(1);
}

console.log(`1 = 1 static round verification passed: ${rounds.length} rounds.`);

function extractRounds(text) {
  const roundBlock = text.match(/return new\[\]\s*\{([\s\S]*?)\n\s*\};\n\s*\}/);
  if (!roundBlock) {
    throw new Error("Could not find GoalModeRounds initializer.");
  }

  const result = [];
  const entryPattern = /(Puzzle|Round)\("((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"(?:,\s*"((?:[^"\\]|\\.)*)")?\)/g;
  let match;
  while ((match = entryPattern.exec(roundBlock[1])) !== null) {
    const kind = match[1];
    const name = unescapeCsharpString(match[2]);
    const tutorial = kind === "Round" ? unescapeCsharpString(match[3]) : "";
    const sample = kind === "Round" ? unescapeCsharpString(match[4]) : unescapeCsharpString(match[3]);
    const symbols = splitSymbols(sample);
    result.push({
      name,
      tutorial,
      sample,
      stickCount: symbols.reduce((total, symbol) => total + (tokenCosts.get(symbol) ?? 0), 0),
      target: sampleTarget(symbols),
    });
  }

  return result;
}

function unescapeCsharpString(value) {
  return value.replace(/\\"/g, '"').replace(/\\\\/g, "\\");
}

function splitSymbols(sample) {
  return sample.split(/\s+/).filter(Boolean);
}

function sampleTarget(symbols) {
  const equality = evaluateEquality(symbols);
  if (equality.handled && equality.valid) {
    return equality.value;
  }

  const value = evaluate(symbols);
  if (!value.valid) {
    throw new Error(`Invalid sample '${symbols.join(" ")}': ${value.reason}`);
  }

  return value.value;
}

function solveRound(symbols, target) {
  const equality = evaluateEquality(symbols);
  if (equality.handled) {
    return equality;
  }

  const value = evaluate(symbols);
  if (!value.valid) {
    return value;
  }

  const valid = Math.abs(value.value - target) < 0.0001;
  return valid ? { valid: true, value: value.value } : { valid: false, reason: `expected ${target}, got ${value.value}` };
}

function evaluateEquality(symbols) {
  const equalsIndexes = symbols
    .map((symbol, index) => (symbol === "=" ? index : -1))
    .filter((index) => index >= 0);

  if (equalsIndexes.length === 0) {
    return { handled: false, valid: false, reason: "" };
  }

  if (equalsIndexes.length > 1) {
    return { handled: true, valid: false, reason: "too many equals" };
  }

  const equalsIndex = equalsIndexes[0];
  if (equalsIndex === 0 || equalsIndex === symbols.length - 1) {
    return { handled: true, valid: false, reason: "equals needs both sides" };
  }

  const left = evaluate(symbols.slice(0, equalsIndex));
  const right = evaluate(symbols.slice(equalsIndex + 1));
  if (!left.valid) {
    return { handled: true, valid: false, reason: `left side: ${left.reason}` };
  }

  if (!right.valid) {
    return { handled: true, valid: false, reason: `right side: ${right.reason}` };
  }

  const valid = Math.abs(left.value - right.value) < 0.0001;
  return valid
    ? { handled: true, valid: true, value: left.value }
    : { handled: true, valid: false, reason: `${left.value} is not ${right.value}` };
}

function evaluate(symbols) {
  const numbers = [];
  const operators = [];
  let numberBuffer = "";

  for (const symbol of symbols) {
    if (!symbol) {
      return { valid: false, reason: "empty box" };
    }

    if (!tokenCosts.has(symbol)) {
      return { valid: false, reason: `unknown token ${symbol}` };
    }

    if (!operatorTokens.has(symbol)) {
      numberBuffer += symbol;
      continue;
    }

    if (numberBuffer.length === 0) {
      return { valid: false, reason: "operator first" };
    }

    numbers.push(Number.parseFloat(numberBuffer));
    numberBuffer = "";
    operators.push(symbol);
  }

  if (numberBuffer.length === 0) {
    return { valid: false, reason: "operator last" };
  }

  numbers.push(Number.parseFloat(numberBuffer));

  for (let i = 0; i < operators.length; ) {
    const op = operators[i];
    if (op !== "*" && op !== "x" && op !== "×" && op !== "/") {
      i += 1;
      continue;
    }

    const left = numbers[i];
    const right = numbers[i + 1];
    if (op === "/" && Math.abs(right) < 0.0001) {
      return { valid: false, reason: "cannot divide by zero" };
    }

    numbers[i] = op === "/" ? left / right : left * right;
    numbers.splice(i + 1, 1);
    operators.splice(i, 1);
  }

  let value = numbers[0];
  for (let i = 0; i < operators.length; i += 1) {
    const op = operators[i];
    const right = numbers[i + 1];
    if (op === "+") {
      value += right;
    } else if (op === "-") {
      value -= right;
    } else {
      return { valid: false, reason: `unknown operator ${op}` };
    }
  }

  return { valid: true, value };
}

function calculateLayoutPlan(slotCount, usesFixedTarget, availableWidth) {
  availableWidth = clamp(availableWidth, 300, 1040);
  const compact = availableWidth < 420;
  const narrow = availableWidth < 360;
  const maxRows = compact ? releaseCompactMaxEquationRows : releaseMaxEquationRows;
  const spacing = narrow ? 6 : slotCount >= 8 ? 8 : 14;
  const minSlotWidth = narrow ? releaseCompactMinSlotWidth : compact ? 96 : releaseMinSlotWidth;
  const minSlotHeight = narrow ? releaseCompactMinSlotHeight : compact ? 108 : releaseMinSlotHeight;
  const targetWidth = usesFixedTarget ? (narrow ? 78 : compact ? 88 : 104) : 0;
  const targetGap = usesFixedTarget ? (narrow ? 12 : compact ? 16 : 24) : 0;
  let rows = 1;

  while (rows < maxRows) {
    const slotsPerRow = Math.ceil(slotCount / rows);
    const lastRowCount = slotCount - slotsPerRow * (rows - 1);
    const topRowWidth = rowWidth(slotsPerRow, minSlotWidth, spacing);
    const lastRowWidth = rowWidth(lastRowCount, minSlotWidth, spacing) + targetGap + targetWidth;
    if (Math.max(topRowWidth, lastRowWidth) <= availableWidth) {
      break;
    }

    rows += 1;
  }

  const slotsPerFinalRow = Math.ceil(slotCount / rows);
  let idealSlotWidth = slotCount <= 3 ? 185 : slotCount <= 6 ? 142 : 118;
  if (rows >= 4) {
    idealSlotWidth = Math.min(idealSlotWidth, narrow ? 96 : 104);
  }

  let maxSlotWidth = idealSlotWidth;

  for (let row = 0; row < rows; row += 1) {
    const rowCount = rowSlotCount(slotCount, slotsPerFinalRow, row);
    const reservedTargetWidth = row === rows - 1 ? targetWidth + targetGap : 0;
    const fitWidth = (availableWidth - reservedTargetWidth - spacing * Math.max(0, rowCount - 1)) / Math.max(1, rowCount);
    maxSlotWidth = Math.min(maxSlotWidth, fitWidth);
  }

  const slotWidth = clamp(maxSlotWidth, minSlotWidth, idealSlotWidth);
  const heightRatio = rows >= 4 ? 1.12 : rows >= 3 ? 1.16 : slotCount <= 3 ? 1.13 : 1.22;
  const maxSlotHeight = slotCount <= 3 ? 210 : rows >= 4 ? 128 : rows >= 3 ? 148 : 188;
  const slotHeight = clamp(slotWidth * heightRatio, minSlotHeight, maxSlotHeight);
  const rowGap = rows >= 4 ? 8 : rows >= 3 ? 10 : rows === 2 ? 12 : 18;
  const contentHeight = rows * slotHeight + (rows - 1) * rowGap;
  let slotAreaWidth = 0;

  for (let row = 0; row < rows; row += 1) {
    const rowCount = rowSlotCount(slotCount, slotsPerFinalRow, row);
    slotAreaWidth = Math.max(slotAreaWidth, rowWidth(rowCount, slotWidth, spacing));
  }

  const lastRowCountFinal = rowSlotCount(slotCount, slotsPerFinalRow, rows - 1);
  const lastRowWidthFinal = rowWidth(lastRowCountFinal, slotWidth, spacing);
  const totalWidth = Math.max(slotAreaWidth, lastRowWidthFinal + targetGap + targetWidth);

  return { rows, slotWidth, slotHeight, totalWidth, contentHeight };
}

function assertEquationLayouts(roundNumber, slotCount, usesFixedTarget) {
  const viewports = [
    ["iphone-se", 320, releaseCompactMaxEquationRows, releaseCompactMinSlotWidth, releaseCompactMinSlotHeight, 500],
    ["iphone-standard", 390, releaseCompactMaxEquationRows, 96, 108, 500],
    ["narrow-portrait", narrowPortraitWidth, releaseMaxEquationRows, releaseMinSlotWidth, releaseMinSlotHeight, 470],
  ];

  for (const [name, width, maxRows, minSlotWidth, minSlotHeight, maxHeight] of viewports) {
    const layout = calculateLayoutPlan(slotCount, usesFixedTarget, width);
    assert(layout.rows <= maxRows, `Round ${roundNumber} uses ${layout.rows} rows at ${name}.`);
    assert(layout.slotWidth >= minSlotWidth, `Round ${roundNumber} slot width too small at ${name}: ${layout.slotWidth}.`);
    assert(
      layout.slotHeight >= minSlotHeight,
      `Round ${roundNumber} slot height too small at ${name}: ${layout.slotHeight}.`,
    );
    assert(layout.totalWidth <= width + 0.1, `Round ${roundNumber} layout too wide at ${name}: ${layout.totalWidth}.`);
    assert(layout.contentHeight <= maxHeight, `Round ${roundNumber} layout too tall at ${name}: ${layout.contentHeight}.`);
    const aspect = layout.slotHeight / layout.slotWidth;
    assert(aspect >= 1.1 && aspect <= 1.24, `Round ${roundNumber} slot aspect broken at ${name}: ${aspect}.`);
  }
}

function assertRoundSelectLayouts() {
  const safeSizes = [
    [320, 568, false],
    [390, 844, true],
    [430, 932, true],
    [412, 915, true],
    [640, 1136, true],
    [1170, 2532, true],
    [1290, 2796, true],
    [1133, 2516, true],
    [768, 1024, false],
  ];

  for (const [safeWidth, safeHeight, expectLift] of safeSizes) {
    const layout = calculateRoundSelectLayout(safeWidth, safeHeight);
    assert(
      layout.panelWidth <= safeWidth + 0.1,
      `Round select panel exceeds safe width ${safeWidth}: ${layout.panelWidth}.`,
    );
    assert(
      layout.panelHeight <= safeHeight + 0.1,
      `Round select panel exceeds safe height ${safeHeight}: ${layout.panelHeight}.`,
    );
    assert(
      layout.cellWidth >= releaseMinRoundSelectCellWidth,
      `Round select cell width too small at ${safeWidth}x${safeHeight}: ${layout.cellWidth}.`,
    );
    assert(
      layout.cellHeight >= releaseMinRoundSelectCellHeight,
      `Round select cell height too small at ${safeWidth}x${safeHeight}: ${layout.cellHeight}.`,
    );

    const gridWidth = layout.cellWidth * 3 + layout.spacing * 2;
    assert(
      gridWidth <= layout.panelWidth - layout.innerPadding + 0.1,
      `Round select grid overflows panel at ${safeWidth}x${safeHeight}: ${gridWidth}.`,
    );
    assert(
      layout.gridHeight <= layout.panelHeight - 230 + 0.1,
      `Round select grid leaves too little footer room at ${safeWidth}x${safeHeight}: ${layout.gridHeight}.`,
    );
    assert(
      expectLift ? layout.offsetY > 0 : Math.abs(layout.offsetY) <= 0.1,
      `Unexpected round select lift at ${safeWidth}x${safeHeight}: ${layout.offsetY}.`,
    );

    const panelTopY = (safeHeight - layout.panelHeight) * 0.5 - layout.offsetY;
    if (safeHeight - layout.panelHeight >= releaseRoundSelectTopMargin * 2) {
      assert(
        panelTopY >= releaseRoundSelectTopMargin - 0.1,
        `Round select panel violates top margin at ${safeWidth}x${safeHeight}: ${panelTopY}.`,
      );
    }

    if (expectLift && safeHeight >= 1000) {
      assert(
        panelTopY / safeHeight <= 0.22,
        `Round select panel starts too low at ${safeWidth}x${safeHeight}: ${panelTopY / safeHeight}.`,
      );
    }

    const fullPageGridHeight = calculateRoundSelectVisibleGridHeight(layout.cellHeight, layout.spacing, 12);
    assert(
      Math.abs(fullPageGridHeight - layout.gridHeight) <= 0.1,
      `Round select full-page grid height drifted at ${safeWidth}x${safeHeight}: ${fullPageGridHeight} vs ${layout.gridHeight}.`,
    );

    const lastPageGridHeight = calculateRoundSelectVisibleGridHeight(layout.cellHeight, layout.spacing, 4);
    assert(
      lastPageGridHeight < layout.gridHeight - 0.1,
      `Round select last page should compact at ${safeWidth}x${safeHeight}: ${lastPageGridHeight}.`,
    );
  }
}

function assertStageLayouts() {
  const safeSizes = [
    ["iphone-se", 320, 568, false],
    ["iphone-standard", 390, 844, true],
    ["iphone-large", 430, 932, true],
    ["android-20x9", 412, 915, true],
    ["android-20x9-render-target", 1133, 2516, true, 300],
    ["ipad", 768, 1024, false],
    ["desktop", 1440, 1024, false],
  ];

  for (const [name, safeWidth, safeHeight, expectLift, minLift = 0] of safeSizes) {
    const layout = calculateStageLayout(safeWidth, safeHeight);
    assert(layout.width >= 560, `Stage width too small at ${name}: ${layout.width}.`);
    assert(layout.height >= 820, `Stage height too small at ${name}: ${layout.height}.`);
    assert(layout.width <= Math.max(560, safeWidth) + 0.1, `Stage exceeds safe width at ${name}: ${layout.width}.`);
    assert(layout.height <= Math.max(820, safeHeight) + 0.1, `Stage exceeds safe height at ${name}: ${layout.height}.`);
    assert(
      expectLift ? layout.offsetY > 0 : Math.abs(layout.offsetY) <= 0.1,
      `Unexpected stage lift at ${name}: ${layout.offsetY}.`,
    );
    assert(
      !expectLift || layout.offsetY >= minLift,
      `Stage lift too small at ${name}: ${layout.offsetY}.`,
    );
    assert(
      layout.offsetY <= releaseTallPortraitStageLiftMax + 0.1,
      `Stage lift exceeds cap at ${name}: ${layout.offsetY}.`,
    );
  }
}

function assertStartupFlowPlans() {
  const cases = [
    [-10, 0, 0, false],
    [0, 0, 0, false],
    [1, 1, 1, true],
    [42, 42, 42, true],
    [1000, 99, 99, true],
  ];

  for (const [saved, expectedHighest, expectedStart, expectedRounds] of cases) {
    const plan = calculateStartupFlow(saved);
    assert(
      plan.highestUnlocked === expectedHighest &&
        plan.startRound === expectedStart &&
        plan.showRounds === expectedRounds,
      `Unexpected startup flow for saved progress ${saved}: ${JSON.stringify(plan)}.`,
    );
  }
}

function calculateStartupFlow(savedHighestUnlockedRoundIndex) {
  const highestUnlocked = clamp(savedHighestUnlockedRoundIndex, 0, releaseRoundCount - 1);
  const showRounds = highestUnlocked > 0;
  const startRound = showRounds ? highestUnlocked : 0;
  return { highestUnlocked, startRound, showRounds };
}

function assertInterstitialDecisionPlans() {
  const cases = [
    [0, 0, 0, false, false, "early_round"],
    [4, 4, 0, false, false, "early_round"],
    [5, 5, 0, false, false, "cadence"],
    [9, 9, 0, false, true, "round_milestone"],
    [9, 10, 0, false, false, "replay_round"],
    [19, 19, 3, false, false, "hard_clear"],
    [19, 19, 0, false, true, "round_milestone"],
    [99, 99, 0, false, true, "round_milestone"],
    [0, 99, 5, true, true, "forced_test_ads"],
  ];

  for (const [roundIndex, highestUnlocked, failures, forceTestAds, expectedShouldOffer, expectedReason] of cases) {
    const decision = calculateInterstitialDecision(roundIndex, highestUnlocked, failures, forceTestAds);
    assert(
      decision.shouldOffer === expectedShouldOffer && decision.reason === expectedReason,
      `Unexpected interstitial decision for ${JSON.stringify({ roundIndex, highestUnlocked, failures, forceTestAds })}: ${JSON.stringify(decision)}.`,
    );
  }
}

function calculateInterstitialDecision(roundIndex, highestUnlockedRoundIndex, failureCount, forceTestAds = false) {
  if (forceTestAds) return { shouldOffer: true, reason: "forced_test_ads" };
  const roundNumber = roundIndex + 1;
  if (roundIndex < highestUnlockedRoundIndex) return { shouldOffer: false, reason: "replay_round" };
  if (roundNumber <= 5) return { shouldOffer: false, reason: "early_round" };
  if (failureCount >= 3) return { shouldOffer: false, reason: "hard_clear" };
  if (roundNumber % 10 !== 0) return { shouldOffer: false, reason: "cadence" };
  return { shouldOffer: true, reason: "round_milestone" };
}

function calculateRoundSelectLayout(safeWidth, safeHeight) {
  safeWidth = safeWidth > 1 ? safeWidth : 560;
  safeHeight = safeHeight > 1 ? safeHeight : 840;
  let panelWidth = Math.min(520, Math.max(300, safeWidth - 40));
  let panelHeight = Math.min(760, Math.max(500, safeHeight - 72));

  if (safeWidth < 360) {
    panelWidth = Math.max(releaseMinRoundSelectPanelWidth, safeWidth - 12);
  }

  if (safeHeight < 600) {
    panelHeight = Math.max(releaseMinRoundSelectPanelHeight, safeHeight - 40);
  }

  const innerPadding = panelWidth < 320 ? 8 : panelWidth < 360 ? 20 : panelWidth < 420 ? 36 : 60;
  const innerWidth = panelWidth - innerPadding;
  const spacing = panelWidth < 360 ? 8 : innerWidth < 430 ? 12 : 16;
  const cellMinWidth = panelWidth < 360 ? releaseMinRoundSelectCellWidth : panelWidth < 420 ? releaseMinSlotWidth : 112;
  const cellWidth = clamp((innerWidth - spacing * 2) / 3, cellMinWidth, 140);
  const availableGridHeight = panelHeight - 246;
  const cellMinHeight = panelHeight < 560 ? releaseMinRoundSelectCellHeight : 68;
  const cellHeight = clamp((availableGridHeight - spacing * 3) / 4, cellMinHeight, 86);
  const gridHeight = cellHeight * 4 + spacing * 3;
  const portraitRatio = safeHeight / Math.max(1, safeWidth);
  const spareHeight = Math.max(0, safeHeight - panelHeight);
  let offsetY = 0;
  if (portraitRatio >= 1.7 && spareHeight > 0) {
    const liftByRatio = spareHeight * releaseRoundSelectPortraitLiftRatio;
    const liftWithTopMargin = Math.max(0, spareHeight * 0.5 - releaseRoundSelectTopMargin);
    offsetY = Math.min(liftByRatio, liftWithTopMargin, releaseRoundSelectPortraitLiftMax);
  }

  return { panelWidth, panelHeight, innerPadding, spacing, cellWidth, cellHeight, gridHeight, offsetY };
}

function calculateRoundSelectVisibleGridHeight(cellHeight, spacing, visibleCount) {
  visibleCount = clamp(visibleCount, 1, releasePageSize);
  const rows = Math.ceil(visibleCount / 3);
  return cellHeight * rows + spacing * Math.max(0, rows - 1);
}

function calculateStageLayout(safeWidth, safeHeight) {
  safeWidth = safeWidth > 1 ? safeWidth : 560;
  safeHeight = safeHeight > 1 ? safeHeight : 840;
  let width = Math.min(safeWidth - 36, 1040);
  const portraitHeightLimit = safeHeight > safeWidth ? 1660 : 1120;
  let height = Math.min(safeHeight - 24, portraitHeightLimit);

  if (safeWidth / Math.max(1, safeHeight) > 0.72) {
    height = Math.min(safeHeight - 24, 1360);
    width = Math.min(safeWidth - 56, 1040);
  }

  const stageWidth = Math.max(560, width);
  const stageHeight = Math.max(820, height);
  const tallPortrait = safeHeight / Math.max(1, safeWidth) >= 1.95;
  const spareHeight = Math.max(0, safeHeight - stageHeight);
  const offsetY = tallPortrait
    ? Math.min(spareHeight * releaseTallPortraitStageLiftRatio, releaseTallPortraitStageLiftMax)
    : 0;
  return { width: stageWidth, height: stageHeight, offsetY };
}

function rowSlotCount(slotCount, slotsPerRow, row) {
  return clamp(slotCount - slotsPerRow * row, 0, slotsPerRow);
}

function rowWidth(slotCount, slotWidth, spacing) {
  return slotCount <= 0 ? 0 : slotCount * slotWidth + Math.max(0, slotCount - 1) * spacing;
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function assert(condition, message) {
  if (condition) {
    return;
  }

  failures += 1;
  console.error(`- ${message}`);
}
