#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const rulesPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs",
);
const strict = process.argv.includes("--strict");

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
const trackedTokens = ["1", "11", "111", "+", "-", "/", "×", "*", "="];
const bands = [
  [1, 15, "tutorial"],
  [16, 30, "early puzzles"],
  [31, 50, "mid puzzles"],
  [51, 80, "equation focus"],
  [81, 100, "finale"],
];

const rounds = extractRounds(fs.readFileSync(rulesPath, "utf8"));

console.log(`# 1 = 1 Round Quality Report`);
console.log("");
console.log(`Rounds: ${rounds.length}`);
console.log("");
printBandSummary();
printIntroductions();
printTargetSummary();
printPatternRuns();
const warningCount = printWarnings();
if (strict && warningCount > 0) {
  process.exit(1);
}

function printBandSummary() {
  console.log("## Token Bands");
  for (const [start, end, label] of bands) {
    const counts = Object.fromEntries(trackedTokens.map((token) => [token, 0]));
    for (let roundNumber = start; roundNumber <= end; roundNumber += 1) {
      for (const symbol of rounds[roundNumber - 1].symbols) {
        if (Object.hasOwn(counts, symbol)) counts[symbol] += 1;
      }
    }

    const summary = trackedTokens.map((token) => `${token}:${counts[token]}`).join(" ");
    console.log(`- ${start}-${end} (${label}): ${summary}`);
  }
  console.log("");
}

function printIntroductions() {
  console.log("## First Token Appearances");
  for (const token of trackedTokens) {
    const index = rounds.findIndex((round) => round.symbols.includes(token));
    console.log(`- ${token}: Round ${index + 1} ${index >= 0 ? rounds[index].name : "missing"}`);
  }
  console.log("");
}

function printTargetSummary() {
  const targetOwners = new Map();
  for (const round of rounds) {
    const key = String(round.target);
    if (!targetOwners.has(key)) targetOwners.set(key, []);
    targetOwners.get(key).push(round.number);
  }

  const mostCommon = [...targetOwners.entries()]
    .sort((left, right) => right[1].length - left[1].length)
    .slice(0, 8)
    .map(([target, owners]) => `${target}:${owners.length}`)
    .join(" ");

  console.log("## Target Spread");
  console.log(`- distinct target values: ${targetOwners.size}`);
  console.log(`- most common targets: ${mostCommon}`);
  console.log("");
}

function printPatternRuns() {
  console.log("## Longest Adjacent Pattern Runs");
  let longest = [];
  let current = [];
  for (const round of rounds) {
    if (current.length === 0 || current[0].signature === round.signature) {
      current.push(round);
    } else {
      if (current.length > longest.length) longest = current;
      current = [round];
    }
  }

  if (current.length > longest.length) longest = current;
  console.log(
    `- longest run: ${longest.length} round(s), ${longest[0]?.signature ?? "none"}, ` +
      `Rounds ${longest[0]?.number ?? "?"}-${longest.at(-1)?.number ?? "?"}`,
  );
  console.log("");
}

function printWarnings() {
  console.log("## Review Warnings");
  const warnings = [];
  for (const [start, end, label] of bands) {
    const bandRounds = rounds.slice(start - 1, end);
    const equalityCount = bandRounds.filter((round) => round.symbols.includes("=")).length;
    const starCount = bandRounds.filter((round) => round.symbols.includes("*")).length;
    const crossCount = bandRounds.filter((round) => round.symbols.includes("×") || round.symbols.includes("x")).length;

    if (label !== "tutorial" && equalityCount === 0) {
      warnings.push(`${start}-${end} (${label}) has no direct equality rounds.`);
    }

    if (label !== "tutorial" && starCount === 0) {
      warnings.push(`${start}-${end} (${label}) has no star-multiply rounds.`);
    }

    if (label !== "tutorial" && crossCount === 0) {
      warnings.push(`${start}-${end} (${label}) has no cross-multiply rounds.`);
    }
  }

  if (warnings.length === 0) {
    console.log("- none");
  } else {
    for (const warning of warnings) console.log(`- ${warning}`);
  }

  return warnings.length;
}

function extractRounds(text) {
  const roundBlock = text.match(/return new\[\]\s*\{([\s\S]*?)\n\s*\};\n\s*\}/);
  if (!roundBlock) throw new Error("Could not find GoalModeRounds initializer.");

  const result = [];
  const entryPattern = /(Puzzle|Round)\("((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"(?:,\s*"((?:[^"\\]|\\.)*)")?\)/g;
  let match;
  while ((match = entryPattern.exec(roundBlock[1])) !== null) {
    const kind = match[1];
    const name = unescapeCsharpString(match[2]);
    const tutorial = kind === "Round" ? unescapeCsharpString(match[3]) : "";
    const sample = kind === "Round" ? unescapeCsharpString(match[4]) : unescapeCsharpString(match[3]);
    const symbols = sample.split(/\s+/).filter(Boolean);
    const target = sampleTarget(symbols);
    result.push({
      number: result.length + 1,
      name,
      tutorial,
      sample,
      symbols,
      target,
      signature: symbols.map((symbol) => (operatorTokens.has(symbol) ? symbol : "N")).join(" "),
    });
  }

  return result;
}

function unescapeCsharpString(value) {
  return value.replace(/\\"/g, '"').replace(/\\\\/g, "\\");
}

function sampleTarget(symbols) {
  const equalityIndex = symbols.indexOf("=");
  if (equalityIndex > 0 && equalityIndex < symbols.length - 1) {
    const left = evaluate(symbols.slice(0, equalityIndex));
    if (!left.valid) throw new Error(`Invalid left side '${symbols.join(" ")}': ${left.reason}`);
    return left.value;
  }

  const value = evaluate(symbols);
  if (!value.valid) throw new Error(`Invalid sample '${symbols.join(" ")}': ${value.reason}`);
  return value.value;
}

function evaluate(symbols) {
  const numbers = [];
  const operators = [];
  let numberBuffer = "";

  for (const symbol of symbols) {
    if (!tokenCosts.has(symbol)) return { valid: false, reason: `unknown token ${symbol}` };
    if (!operatorTokens.has(symbol)) {
      numberBuffer += symbol;
      continue;
    }

    if (numberBuffer.length === 0) return { valid: false, reason: "operator first" };
    numbers.push(Number.parseFloat(numberBuffer));
    numberBuffer = "";
    operators.push(symbol);
  }

  if (numberBuffer.length === 0) return { valid: false, reason: "operator last" };
  numbers.push(Number.parseFloat(numberBuffer));

  for (let i = 0; i < operators.length; ) {
    const op = operators[i];
    if (op !== "*" && op !== "x" && op !== "×" && op !== "/") {
      i += 1;
      continue;
    }

    const left = numbers[i];
    const right = numbers[i + 1];
    if (op === "/" && Math.abs(right) < 0.0001) return { valid: false, reason: "divide by zero" };
    numbers[i] = op === "/" ? left / right : left * right;
    numbers.splice(i + 1, 1);
    operators.splice(i, 1);
  }

  let value = numbers[0];
  for (let i = 0; i < operators.length; i += 1) {
    if (operators[i] === "+") value += numbers[i + 1];
    else if (operators[i] === "-") value -= numbers[i + 1];
    else return { valid: false, reason: `unknown operator ${operators[i]}` };
  }

  return { valid: true, value };
}
