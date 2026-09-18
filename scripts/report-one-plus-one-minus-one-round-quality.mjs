#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import assert from 'node:assert/strict';
import { extractRounds, analyzeIdentity, findEqualityWitness, identityErrors, operatorTokens, generateCandidatePool, enumerateRoundSolutions, analyzeRoundPatterns, findDominantPatternCandidates } from './one-plus-one-minus-one-round-identity.mjs';

const repoRoot = process.cwd();
const rulesPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs",
);
const strict = process.argv.includes("--strict");
const strictPatterns = process.argv.includes("--strict-patterns");
const summaryOnly = process.argv.includes("--summary");

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
const dominantCandidatesIndex = process.argv.indexOf('--dominant-candidates');
if (dominantCandidatesIndex >= 0) {
  const selected = process.argv[dominantCandidatesIndex + 1]?.startsWith('--') === false
    ? process.argv[dominantCandidatesIndex + 1]
    : '';
  const roundNumbers = selected
    ? selected.split(',').map(Number)
    : [];
  await writeJson(findDominantPatternCandidates(rounds, {
    roundNumbers,
    seed:numberOption('--seed'),
    samples:numberOption('--samples'),
    maxNodes:numberOption('--max-nodes'),
    maxSolutions:numberOption('--max-solutions'),
    maxResults:numberOption('--max-results'),
    maxEvaluations:numberOption('--max-evaluations'),
    candidateBudget:numberOption('--candidate-budget'),
    maxExtraSlots:numberOption('--max-extra-slots'),
    maxExtraSticks:numberOption('--max-extra-sticks'),
    allowFewerSlots:process.argv.includes('--allow-fewer-slots'),
    allowFewerSticks:process.argv.includes('--allow-fewer-sticks'),
    minRecommendedSolutions:numberOption('--min-recommended-solutions'),
    minVisibleDiversity:numberOption('--min-visible-diversity'),
    minRecommendedImprovement:numberOption('--min-recommended-improvement'),
  }));
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}
if (process.argv.includes('--patterns')) {
  const patternRows = analyzeRoundPatterns(rounds, {
    maxNodes:numberOption('--max-nodes'),
    maxSolutions:numberOption('--max-solutions'),
  });
  const pureSelfDivisionRows = patternRows.filter(row =>
    row.pureShortcutSolutions.some(solution => solution.patterns.includes('pure-self-division')));
  const equalityEchoRows = patternRows.filter(row =>
    row.pureShortcutSolutions.some(solution => solution.patterns.includes('same-expression-equality')));
  const lateEqualityEchoRows = equalityEchoRows.filter(row => row.number > 50);
  const lateSampleEqualityEchoRows = lateEqualityEchoRows
    .filter(row => row.samplePatterns.includes('same-expression-equality'));
  const lateAlternateEqualityEchoRows = lateEqualityEchoRows
    .filter(row => !row.samplePatterns.includes('same-expression-equality'));
  const rankedLateAlternateEqualityEchoRows = lateAlternateEqualityEchoRows
    .map(row => equalityEchoPriorityRow(row))
    .sort((left, right) => right.sameExpressionRatio - left.sameExpressionRatio ||
      right.sameExpressionCount - left.sameExpressionCount || left.number - right.number);
  const dominantPatternRows = dominantShortcutRows(patternRows);
  const policyViolations = patternPolicyViolations(patternRows);
  const shortcutPolicy = {
    pureSelfDivisionLearningWindow:'Rounds 1-30 may intentionally teach or echo N/N = 1. Later pure N/N acceptance should be reviewed unless it is deliberately combined with another required idea.',
    learningRounds:pureSelfDivisionRows.filter(row => row.number <= 30).map(patternPolicyRow),
    reviewRounds:pureSelfDivisionRows.filter(row => row.number > 30).map(patternPolicyRow),
    violations:policyViolations,
    equalityEchoReview:{
      reviewWindow:'Same-expression and same-number equality answers are legal, but after Round 50 they should be treated as review candidates when they dominate a round or bypass the intended resource judgment.',
      reviewRows:lateEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      sampleEchoRows:lateSampleEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      alternateOnlyRows:lateAlternateEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      highPriorityAlternateRows:rankedLateAlternateEqualityEchoRows.filter(row => row.sameExpressionRatio >= 0.5),
      rankedAlternateRows:rankedLateAlternateEqualityEchoRows,
    },
    dominantPatternReview:{
      reviewWindow:'After Round 50, any single shortcut family dominating the enumerated accepted answers should be reviewed as a possible universal-key pattern. This is a design map, not a token ban.',
      highPriorityRows:highPriorityDominantShortcutRows(dominantPatternRows),
      rankedRows:dominantPatternRows,
    },
    strictPatternMode:'Use --strict-patterns to fail on these design-policy violations during round redesign. This is intentionally separate from --strict so existing static checks can report the map before the affected rounds are redesigned.',
  };
  const shortcutReview = patternRows
    .filter(row => row.pureShortcutSolutions.length > 0)
    .map(row => ({
      number:row.number,
      name:row.name,
      sample:row.sample,
      target:row.target,
      complete:row.complete,
      solutionCount:row.solutionCount,
      pureShortcutSolutions:row.pureShortcutSolutions,
    }));
  const payload = summaryOnly ? {
    method:'Summary of accepted canonical token sequence shortcut patterns. Use --patterns without --summary for full per-round rows.',
    complete:patternRows.every(row => row.complete),
    summary:{
      rounds:patternRows.length,
      incompleteRows:patternRows.filter(row => !row.complete).map(row => ({
        number:row.number,
        name:row.name,
        limitReason:row.limitReason,
        solutionCount:row.solutionCount,
      })),
      shortcutReviewRows:shortcutReview.map(row => row.number),
    },
    shortcutPolicy:compactShortcutPolicy(shortcutPolicy),
  } : {
    method:'Classifies accepted canonical token sequences by reusable arithmetic patterns. This is a design-review map, not proof of human difficulty or fun. Incomplete rows only classify the enumerated prefix.',
    complete:patternRows.every(row => row.complete),
    rows:patternRows,
    shortcutPolicy,
    shortcutReview,
  };
  await writeJson(payload);
  process.exit((strict && (patternRows.some(row => !row.complete) || identityErrors(rounds).length)) ||
    (strictPatterns && policyViolations.length > 0) ? 1 : 0);
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

function patternPolicyRow(row, pattern = 'pure-self-division') {
  const examples = row.pureShortcutSolutions
    .filter(solution => solution.patterns.includes(pattern))
    .slice(0, 5)
    .map(solution => solution.expression);
  const result = {
    number:row.number,
    name:row.name,
    target:row.target,
    slots:row.slots,
    sticks:row.sticks,
    sample:row.sample,
    complete:row.complete,
    examples,
  };
  if (pattern === 'pure-self-division') result.pureSelfDivisionExamples = examples;
  return result;
}

function equalityEchoPriorityRow(row) {
  const sameExpressionCount = row.patternCounts['same-expression-equality'] ?? 0;
  const constructedEqualityCount = row.patternCounts['constructed-equality'] ?? 0;
  return {
    ...patternPolicyRow(row, 'same-expression-equality'),
    solutionCount:row.solutionCount,
    sameExpressionCount,
    constructedEqualityCount,
    sameExpressionRatio:row.solutionCount > 0 ? sameExpressionCount / row.solutionCount : 0,
    priority: sameExpressionCount / Math.max(1, row.solutionCount) >= 0.5 ? 'high' : 'review',
  };
}

function dominantShortcutRows(patternRows) {
  const reviewPatterns = [
    'self-division',
    'divide-by-one',
    'self-subtraction',
    'multiply-by-one',
    'same-expression-equality',
  ];
  const rows = [];
  for (const row of patternRows) {
    if (row.number <= 50 || row.solutionCount <= 0) continue;
    for (const pattern of reviewPatterns) {
      const count = row.patternCounts[pattern] ?? 0;
      if (count <= 0) continue;
      rows.push({
        number:row.number,
        name:row.name,
        target:row.target,
        slots:row.slots,
        sticks:row.sticks,
        sample:row.sample,
        complete:row.complete,
        pattern,
        count,
        solutionCount:row.solutionCount,
        ratio:count / row.solutionCount,
        sampleHasPattern:row.samplePatterns.includes(pattern),
        priority:count / row.solutionCount >= 0.5 ? 'high' : 'review',
      });
    }
  }
  return rows.sort((left, right) => right.ratio - left.ratio ||
    right.count - left.count || left.number - right.number ||
    left.pattern.localeCompare(right.pattern));
}

function highPriorityDominantShortcutRows(rows) {
  return rows.filter(row => row.ratio >= 0.6 && row.solutionCount >= 20);
}

function compactPolicyRow(row) {
  const compact = {
    number:row.number,
    name:row.name,
    target:row.target,
    slots:row.slots,
    sticks:row.sticks,
    complete:row.complete,
  };
  if (row.solutionCount !== undefined) compact.solutionCount = row.solutionCount;
  if (row.sameExpressionCount !== undefined) compact.sameExpressionCount = row.sameExpressionCount;
  if (row.constructedEqualityCount !== undefined) compact.constructedEqualityCount = row.constructedEqualityCount;
  if (row.sameExpressionRatio !== undefined) compact.sameExpressionRatio = row.sameExpressionRatio;
  if (row.pattern !== undefined) compact.pattern = row.pattern;
  if (row.count !== undefined) compact.count = row.count;
  if (row.ratio !== undefined) compact.ratio = row.ratio;
  if (row.sampleHasPattern !== undefined) compact.sampleHasPattern = row.sampleHasPattern;
  if (row.priority !== undefined) compact.priority = row.priority;
  return compact;
}

function compactShortcutPolicy(policy) {
  const equalityEchoReview = policy.equalityEchoReview;
  return {
    pureSelfDivisionLearningWindow:policy.pureSelfDivisionLearningWindow,
    learningRounds:policy.learningRounds.map(row => row.number),
    reviewRounds:policy.reviewRounds.map(row => row.number),
    violations:policy.violations,
    equalityEchoReview:{
      reviewWindow:equalityEchoReview.reviewWindow,
      reviewRows:equalityEchoReview.reviewRows.map(row => row.number),
      sampleEchoRows:equalityEchoReview.sampleEchoRows.map(row => row.number),
      alternateOnlyRows:equalityEchoReview.alternateOnlyRows.map(row => row.number),
      highPriorityAlternateRows:equalityEchoReview.highPriorityAlternateRows.map(compactPolicyRow),
      rankedAlternateRows:equalityEchoReview.rankedAlternateRows.map(compactPolicyRow),
    },
    dominantPatternReview:{
      reviewWindow:policy.dominantPatternReview.reviewWindow,
      highPriorityRows:policy.dominantPatternReview.highPriorityRows.map(compactPolicyRow),
      rankedRows:policy.dominantPatternReview.rankedRows.map(compactPolicyRow),
    },
    strictPatternMode:policy.strictPatternMode,
  };
}

function patternPolicyViolations(patternRows) {
  const violations = [];
  for (const row of patternRows) {
    const pureSelfDivision = row.pureShortcutSolutions
      .filter(solution => solution.patterns.includes('pure-self-division'));
    if (row.number > 30 && pureSelfDivision.length > 0) {
      violations.push({
        severity:row.number >= 50 ? 'blocker' : 'review',
        rule:'late-pure-self-division',
        number:row.number,
        name:row.name,
        sample:row.sample,
        target:row.target,
        slots:row.slots,
        sticks:row.sticks,
        examples:pureSelfDivision.slice(0, 5).map(solution => solution.expression),
        expectation:'After Round 30, N/N should not be a standalone universal key unless this round is explicitly redesigned as an intentional callback or requires another nontrivial pattern.',
      });
    }
  }

  return violations;
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
