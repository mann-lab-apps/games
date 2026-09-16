#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import assert from 'node:assert/strict';
import { extractRounds, analyzeIdentity, findEqualityWitness, identityErrors, operatorTokens, generateCandidatePool, enumerateRoundSolutions } from './one-plus-one-minus-one-round-identity.mjs';

const repoRoot = process.cwd();
const rulesPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs",
);
const strict = process.argv.includes("--strict");

const trackedTokens = ["1", "11", "111", "+", "-", "/", "×", "*", "="];
const bands = [
  [1, 15, "tutorial"],
  [16, 30, "early puzzles"],
  [31, 50, "mid puzzles"],
  [51, 80, "equation focus"],
  [81, 100, "finale"],
];

const rounds = extractRounds(fs.readFileSync(rulesPath, "utf8")).map(r => ({...r,
  signature:r.symbols.map(s=>operatorTokens.has(s)?s:'N').join(' ')}));
const unityIndex = process.argv.indexOf('--unity-report');
if (unityIndex >= 0) {
  const native = JSON.parse(fs.readFileSync(process.argv[unityIndex + 1], 'utf8'));
  assert.deepEqual(native.rows, analyzeIdentity(rounds).crossSamples,
    'Unity/Node cross acceptance differs or the Unity report is stale');
  console.error(`Unity/Node acceptance parity: ${rounds.length * rounds.length} sample pairs passed.`);
}
const numberOption = name => {
  const index = process.argv.indexOf(name);
  return index < 0 ? undefined : Number(process.argv[index + 1]);
};
const solutionsIndex = process.argv.indexOf('--solutions');
if (solutionsIndex >= 0) {
  const selected = (process.argv[solutionsIndex + 1] ?? '').split(',').map(Number);
  if (selected.some(n => !Number.isInteger(n) || n < 1 || n > rounds.length) ||
      new Set(selected).size !== selected.length)
    throw new Error('--solutions expects unique round numbers from 1 to 100, comma separated');
  const reports = selected.map(number => {
    const round = rounds[number - 1];
    return {number, sample:round.sample, target:round.target, slots:round.symbols.length, sticks:round.stickCount,
      ...enumerateRoundSolutions(round, {maxNodes:numberOption('--max-nodes'), maxSolutions:numberOption('--max-solutions')})};
  });
  await writeJson({rounds:reports});
  process.exit(strict && (reports.some(r => !r.complete) || identityErrors(rounds).length) ? 1 : 0);
}
if (process.argv.includes('--candidates')) {
  await writeJson(generateCandidatePool(rounds, {
    seed:numberOption('--seed'), samples:numberOption('--samples')
  }));
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}
if (process.argv.includes('--json')) {
  const identity = analyzeIdentity(rounds);
  await writeJson({rounds, ...identity, equalitySearch:identity.resourceRepeats.map(g=>({
    ...g,
    intentionalReason:intentionalSharedEqualityReason(g),
    ...findEqualityWitness(...g.key.split('/').map(Number))
  }))});
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}

console.log(`# 1 = 1 Round Quality Report`);
console.log("");
console.log(`Rounds: ${rounds.length}`);
console.log("");
printBandSummary();
printIntroductions();
printTargetSummary();
printPatternRuns();
printConstraintRepeats();
const warningCount = printWarnings();
if (strict && warningCount > 0) {
  process.exit(1);
}

function writeJson(value) {
  return new Promise((resolve, reject) => {
    process.stdout.write(`${JSON.stringify(value,null,2)}\n`, error => error ? reject(error) : resolve());
  });
}

function printConstraintRepeats() {
  const identity = analyzeIdentity(rounds);
  console.log("## Actual Accepted-Answer Constraints");
  console.log(`- resource pairs: ${identity.uniqueResources}; identical resource/target groups: ${identity.duplicates.length}`);
  for (const group of identity.duplicates) console.log(`- identical ${group.key}: Rounds ${group.rounds.join(", ")}`);
  console.log("## Shared Equality Witnesses (Reuse Review)");
  let unresolvedSharedPairs = 0;
  let intentionalSharedPairs = 0;
  for (const group of identity.resourceRepeats) {
    const [slots, sticks] = group.key.split("/").map(Number);
    const witness = findEqualityWitness(slots, sticks);
    const pairCount = group.rounds.length * (group.rounds.length - 1) / 2;
    const intentional = intentionalSharedEqualityReason(group);
    if (witness.status === "found") {
      if (intentional) {
        intentionalSharedPairs += pairCount;
      } else {
        unresolvedSharedPairs += pairCount;
      }
    }

    const suffix = intentional ? `; intentional: ${intentional}` : "";
    console.log(`- ${group.key}, Rounds ${group.rounds.join(", ")}: ${witness.status}; ${witness.symbols?.join(" ") ?? "no witness"} (${witness.nodes}/${witness.maxNodes} nodes)${suffix}`);
  }
  if (intentionalSharedPairs > 0) {
    console.log(`Intentional proven shared-equality pairs: ${intentionalSharedPairs}.`);
  }

  if (unresolvedSharedPairs > 0) {
    console.warn(`Warning: ${unresolvedSharedPairs} unresolved round pairs share a proven equality answer. Passing duplicate gates does not resolve this reuse.`);
  }
  console.log("Found witnesses prove shared answers, not identical full answer sets. Limited searches do not prove absence.");
  console.log("");
}

function intentionalSharedEqualityReason(group) {
  const roundsKey = group.rounds.join(",");
  if (group.key === "3/4" && roundsKey === "2,5") {
    return "early tutorial echo";
  }

  if (group.key === "9/11" && roundsKey === "30,100") {
    return "title callback";
  }

  return "";
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
  console.log("## Blocking Duplicate / Coverage Errors");
  const warnings = [];
  warnings.push(...identityErrors(rounds));
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
    console.log("- none; see reuse review above for intentional callbacks and bounded-search limits");
  } else {
    for (const warning of warnings) console.log(`- ${warning}`);
  }

  return warnings.length;
}
