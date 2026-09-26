#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import assert from 'node:assert/strict';
import { acceptsRound, extractMakeOneRounds, extractRounds, analyzeIdentity, findEqualityWitness, identityErrors, operatorTokens, tokenCosts, generateCandidatePool, enumerateRoundSolutions, analyzeRoundPatterns, classifySolutionPatterns, dominantPatternProfileForRound, findDominantPatternCandidates, findEqualityEchoCandidates, findResourceRepeatCandidates, parseResourceKey, sampleTarget, splitSymbols } from './one-plus-one-minus-one-round-identity.mjs';

const repoRoot = process.cwd();
const rulesPath = path.join(
  repoRoot,
  "prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs",
);
const strict = process.argv.includes("--strict");
const strictPatterns = process.argv.includes("--strict-patterns");
const summaryOnly = process.argv.includes("--summary");
const makeOneMode = process.argv.includes("--make-one");

const trackedTokens = ["1", "11", "111", "+", "-", "/", "×", "*", "="];
const goalBands = [
  [1, 15, "tutorial"],
  [16, 30, "early puzzles"],
  [31, 50, "mid puzzles"],
  [51, 80, "equation focus"],
  [81, 100, "finale"],
];
const makeOneBands = [
  [1, 10, "first discoveries"],
  [11, 20, "pattern mixing"],
  [21, 30, "compact finale"],
];
const bands = makeOneMode ? makeOneBands : goalBands;
const pureSelfDivisionLearningMax = makeOneMode ? 10 : 30;
const shortcutReviewMinRound = makeOneMode ? pureSelfDivisionLearningMax : 50;
const modeLabel = makeOneMode ? "default = 1 rounds" : "legacy goal rounds";

const parsedSource = fs.readFileSync(rulesPath, "utf8");
const rounds = (makeOneMode ? extractMakeOneRounds(parsedSource) : extractRounds(parsedSource)).map(r => ({...r,
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
const parseRoundOrResourceSelection = (selected, {allowResourceKeys = false} = {}) => {
  if (!selected) return [];
  const numbers = [];
  const parts = selected.split(',').map(value => value.trim());
  if (parts.some(part => part.length === 0)) {
    throw new Error('selection must not contain empty entries');
  }
  for (const part of parts) {
    if (allowResourceKeys && part.includes('/')) {
      const {slots, sticks} = parseResourceKey(part);
      if (!Number.isInteger(slots) || !Number.isInteger(sticks) || slots < 1 || sticks < 1) {
        throw new Error(`resource key must be slots/sticks, got ${part}`);
      }
      const matching = rounds
        .map((round, index) => ({round, number:index + 1}))
        .filter(({round}) => round.symbols.length === slots && round.stickCount === sticks)
        .map(({number}) => number);
      if (matching.length === 0) {
        throw new Error(`resource key ${part} did not match any rounds`);
      }
      numbers.push(...matching);
      continue;
    }

    numbers.push(Number(part));
  }
  return numbers;
};
const solutionsIndex = process.argv.indexOf('--solutions');
if (solutionsIndex >= 0) {
  const selected = parseRoundOrResourceSelection(process.argv[solutionsIndex + 1] ?? '');
  if (selected.some(n => !Number.isInteger(n) || n < 1 || n > rounds.length) ||
      new Set(selected).size !== selected.length)
    throw new Error(`--solutions expects unique round numbers from 1 to ${rounds.length}, comma separated`);
  const reports = selected.map(number => {
    const round = rounds[number - 1];
    return {number, sample:round.sample, target:round.target, slots:round.symbols.length, sticks:round.stickCount,
      ...enumerateRoundSolutions(round, {maxNodes:numberOption('--max-nodes'), maxSolutions:numberOption('--max-solutions')})};
  });
  await writeJson({rounds:reports});
  process.exit(strict && (reports.some(r => !r.complete) || identityErrors(rounds).length) ? 1 : 0);
}
const evaluateSampleIndex = process.argv.indexOf('--evaluate-sample');
if (evaluateSampleIndex >= 0) {
  const sample = process.argv[evaluateSampleIndex + 1];
  if (!sample || sample.startsWith('--')) {
    throw new Error('--evaluate-sample expects a quoted token expression');
  }

  const symbols = splitSymbols(sample);
  const target = numberOption('--target') ?? sampleTarget(symbols);
  const round = {
    number: 0,
    name: 'Evaluated sample',
    sample,
    symbols,
    stickCount: symbols.reduce((total, symbol) => total + (tokenCosts.get(symbol) ?? 0), 0),
    target,
  };
  await writeJson({
    sample,
    symbols,
    target,
    sampleTarget: sampleTarget(symbols),
    slots: symbols.length,
    sticks: round.stickCount,
    samplePatterns: classifySolutionPatterns(symbols),
    accepted: acceptsRound(round, symbols),
    profile: dominantPatternProfileForRound(round, {
      maxNodes: numberOption('--max-nodes'),
      maxSolutions: numberOption('--max-solutions'),
    }),
  });
  process.exit(0);
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
    maxNearbyNodes:numberOption('--max-nearby-nodes'),
    maxExtraSlots:numberOption('--max-extra-slots'),
    maxExtraSticks:numberOption('--max-extra-sticks'),
    allowFewerSlots:process.argv.includes('--allow-fewer-slots'),
    allowFewerSticks:process.argv.includes('--allow-fewer-sticks'),
    minRecommendedSolutions:numberOption('--min-recommended-solutions'),
    minVisibleDiversity:numberOption('--min-visible-diversity'),
    minRecommendedImprovement:numberOption('--min-recommended-improvement'),
    minCombinationScore:numberOption('--min-combination-score'),
    preserveTarget:makeOneMode || process.argv.includes('--preserve-target'),
  }));
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}
const equalityCandidatesIndex = process.argv.indexOf('--equality-candidates');
if (equalityCandidatesIndex >= 0) {
  const selected = process.argv[equalityCandidatesIndex + 1]?.startsWith('--') === false
    ? process.argv[equalityCandidatesIndex + 1]
    : '';
  const roundNumbers = selected
    ? selected.split(',').map(Number)
    : [];
  await writeJson(findEqualityEchoCandidates(rounds, {
    roundNumbers,
    maxNodes:numberOption('--max-nodes'),
    maxSolutions:numberOption('--max-solutions'),
    maxResults:numberOption('--max-results'),
    maxEvaluations:numberOption('--max-evaluations'),
    maxSideExpressions:numberOption('--max-side-expressions'),
    maxResourceDelta:numberOption('--max-resource-delta'),
    minRecommendedSolutions:numberOption('--min-recommended-solutions'),
    minRecommendedImprovement:numberOption('--min-recommended-improvement'),
    minCombinationScore:numberOption('--min-combination-score'),
  }));
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}
const resourceCandidatesIndex = process.argv.indexOf('--resource-candidates');
if (resourceCandidatesIndex >= 0) {
  const selected = process.argv[resourceCandidatesIndex + 1]?.startsWith('--') === false
    ? process.argv[resourceCandidatesIndex + 1]
    : '';
  const parsedRoundNumbers = parseRoundOrResourceSelection(selected, {allowResourceKeys:true});
  const roundNumbers = parsedRoundNumbers.filter((number, index) => parsedRoundNumbers.indexOf(number) === index);
  await writeJson(findResourceRepeatCandidates(rounds, {
    roundNumbers,
    maxNodes:numberOption('--max-nodes'),
    maxSolutions:numberOption('--max-solutions'),
    maxResults:numberOption('--max-results'),
    maxEvaluations:numberOption('--max-evaluations'),
    candidateBudget:numberOption('--candidate-budget'),
    maxNearbyNodes:numberOption('--max-nearby-nodes'),
    maxExtraSlots:numberOption('--max-extra-slots'),
    maxExtraSticks:numberOption('--max-extra-sticks'),
    preserveTarget:true,
    pureSelfDivisionLearningMax,
    minCombinationScore:numberOption('--min-combination-score'),
  }));
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}
if (process.argv.includes('--patterns')) {
  const identity = analyzeIdentity(rounds);
  const patternRows = analyzeRoundPatterns(rounds, {
    maxNodes:numberOption('--max-nodes'),
    maxSolutions:numberOption('--max-solutions'),
  });
  const pureSelfDivisionRows = patternRows.filter(row =>
    row.pureShortcutSolutions.some(solution => solution.patterns.includes('pure-self-division')));
  const equalityEchoRows = patternRows.filter(row =>
    row.pureShortcutSolutions.some(solution => solution.patterns.includes('same-expression-equality')));
  const lateEqualityEchoRows = equalityEchoRows.filter(row => row.number > shortcutReviewMinRound);
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
    pureSelfDivisionLearningWindow:`In ${modeLabel}, Rounds 1-${pureSelfDivisionLearningMax} may intentionally teach or echo N/N = 1. Later pure N/N acceptance should be reviewed unless it is deliberately combined with another required idea.`,
    learningRounds:pureSelfDivisionRows.filter(row => row.number <= pureSelfDivisionLearningMax).map(patternPolicyRow),
    reviewRounds:pureSelfDivisionRows.filter(row => row.number > pureSelfDivisionLearningMax).map(patternPolicyRow),
    violations:policyViolations,
    equalityEchoReview:{
      reviewWindow:`Same-expression and same-number equality answers are legal, but after Round ${shortcutReviewMinRound} in ${modeLabel} they should be treated as review candidates when they dominate a round or bypass the intended resource judgment.`,
      reviewRows:lateEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      sampleEchoRows:lateSampleEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      alternateOnlyRows:lateAlternateEqualityEchoRows.map(row => patternPolicyRow(row, 'same-expression-equality')),
      highPriorityAlternateRows:rankedLateAlternateEqualityEchoRows.filter(row => row.priority === 'high'),
      rankedAlternateRows:rankedLateAlternateEqualityEchoRows,
    },
    dominantPatternReview:{
      reviewWindow:`After Round ${shortcutReviewMinRound}, any single shortcut family dominating the enumerated accepted answers should be reviewed as a possible universal-key pattern in ${modeLabel}. This is a design map, not a token ban.`,
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
  const incompleteReview = incompletePatternReview(patternRows, {
    maxNodes:numberOption('--max-nodes') ?? 200000,
    maxSolutions:numberOption('--max-solutions') ?? 1000,
  });
  const resourceReview = resourceRepeatReview(identity);
  const payload = summaryOnly ? {
    method:'Summary of accepted canonical token sequence shortcut patterns. Use --patterns without --summary for full per-round rows.',
    complete:patternRows.every(row => row.complete),
    summary:{
      rounds:patternRows.length,
      uniqueResources:identity.uniqueResources,
      repeatedResourceGroups:identity.resourceRepeats.length,
      searchBudget:incompleteReview.searchBudget,
      incompleteRows:incompleteReview.rows,
      shortcutReviewRows:shortcutReview.map(row => row.number),
    },
    shortcutPolicy:compactShortcutPolicy(shortcutPolicy),
    resourceReview:compactResourceReview(resourceReview),
    incompleteReview,
  } : {
    method:'Classifies accepted canonical token sequences by reusable arithmetic patterns. This is a design-review map, not proof of human difficulty or fun. Incomplete rows only classify the enumerated prefix.',
    complete:patternRows.every(row => row.complete),
    rows:patternRows,
    shortcutPolicy,
    shortcutReview,
    resourceReview,
    incompleteReview,
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
    ...equalityWitnessForGroup(g)
  }))});
  process.exit(strict && identityErrors(rounds).length ? 1 : 0);
}

console.log(`# 1 = 1 Round Quality Report`);
console.log("");
console.log(`Mode: ${modeLabel}`);
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
    priority: row.solutionCount >= 20 && sameExpressionCount / Math.max(1, row.solutionCount) >= 0.5 ? 'high' : 'review',
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
    if (row.number <= shortcutReviewMinRound || row.solutionCount <= 0) continue;
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
        priority:count / row.solutionCount >= 0.6 && row.solutionCount >= 20 ? 'high' : 'review',
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
  const compactRankLimit = 12;
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
      rankedAlternateRows:equalityEchoReview.rankedAlternateRows.slice(0, compactRankLimit).map(compactPolicyRow),
      rankedAlternateRowsTruncated:equalityEchoReview.rankedAlternateRows.length > compactRankLimit,
    },
    dominantPatternReview:{
      reviewWindow:policy.dominantPatternReview.reviewWindow,
      highPriorityRows:policy.dominantPatternReview.highPriorityRows.map(compactPolicyRow),
      rankedRows:policy.dominantPatternReview.rankedRows.slice(0, compactRankLimit).map(compactPolicyRow),
      rankedRowsTruncated:policy.dominantPatternReview.rankedRows.length > compactRankLimit,
    },
    strictPatternMode:policy.strictPatternMode,
  };
}

function resourceRepeatReview(identity) {
  const rows = identity.resourceRepeats.map(group => {
    const resource = parseResourceKey(group.key);
    const witness = equalityWitnessForGroup(group);
    const intentionalReason = intentionalSharedEqualityReason(group);
    const pairCount = group.rounds.length * (group.rounds.length - 1) / 2;
    return {
      key:group.key,
      rounds:group.rounds,
      slots:resource.slots,
      sticks:resource.sticks,
      pairCount,
      witnessStatus:witness.status,
      witnessSymbols:witness.symbols,
      nodes:witness.nodes,
      maxNodes:witness.maxNodes,
      intentionalReason,
      priority:witness.status === 'found' && !intentionalReason ? 'review' : 'info',
    };
  });
  const unresolvedFoundRows = rows.filter(row => row.witnessStatus === 'found' && !row.intentionalReason);
  const intentionalFoundRows = rows.filter(row => row.witnessStatus === 'found' && row.intentionalReason);
  return {
    method:'Repeated slot/stick resources can allow the same constructed equality across rounds. This is a design-review map, not a ban on repeated resources.',
    uniqueResources:identity.uniqueResources,
    repeatedResourceGroups:identity.resourceRepeats.length,
    unresolvedFoundSharedEqualityPairs:unresolvedFoundRows.reduce((total, row) => total + row.pairCount, 0),
    intentionalFoundSharedEqualityPairs:intentionalFoundRows.reduce((total, row) => total + row.pairCount, 0),
    rows,
  };
}

function compactResourceReview(review) {
  const compactRankLimit = 12;
  const reviewRows = review.rows
    .filter(row => row.priority === 'review')
    .sort((left, right) => right.pairCount - left.pairCount || left.slots - right.slots ||
      left.sticks - right.sticks || left.rounds[0] - right.rounds[0]);
  return {
    method:review.method,
    uniqueResources:review.uniqueResources,
    repeatedResourceGroups:review.repeatedResourceGroups,
    unresolvedFoundSharedEqualityPairs:review.unresolvedFoundSharedEqualityPairs,
    intentionalFoundSharedEqualityPairs:review.intentionalFoundSharedEqualityPairs,
    reviewRows:reviewRows.slice(0, compactRankLimit).map(compactResourceRow),
    reviewRowsTruncated:reviewRows.length > compactRankLimit,
  };
}

function compactResourceRow(row) {
  return {
    key:row.key,
    rounds:row.rounds,
    slots:row.slots,
    sticks:row.sticks,
    pairCount:row.pairCount,
    witnessStatus:row.witnessStatus,
    witnessSymbols:row.witnessSymbols,
    intentionalReason:row.intentionalReason,
    priority:row.priority,
  };
}

function incompletePatternReview(patternRows, {maxNodes, maxSolutions}) {
  const rows = patternRows.filter(row => !row.complete).map(row => {
    const rowMaxNodes = row.maxNodes ?? maxNodes;
    const rowMaxSolutions = row.maxSolutions ?? maxSolutions;
    const compact = {
      number:row.number,
      name:row.name,
      target:row.target,
      slots:row.slots,
      sticks:row.sticks,
      sample:row.sample,
      limitReason:row.limitReason,
      nodes:row.nodes,
      maxNodes:rowMaxNodes,
      solutionCount:row.solutionCount,
      maxSolutions:rowMaxSolutions,
      observedPatterns:Object.fromEntries(Object.entries(row.patternCounts)
        .filter(([, count]) => count > 0)
        .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))),
    };
    compact.recheckCommand = [
      'node scripts/report-one-plus-one-minus-one-round-quality.mjs',
      '--make-one',
      '--patterns',
      '--summary',
      '--strict-patterns',
      '--max-nodes',
      String(Math.max(rowMaxNodes * 5, 1000000)),
      '--max-solutions',
      String(Math.max(rowMaxSolutions * 5, 5000)),
    ].join(' ');
    compact.interpretation = row.limitReason === 'solution_limit'
      ? 'The row has at least this many accepted answers; pattern ratios are prefix evidence and should not be treated as exhaustive.'
      : 'The search reached its node budget before exhausting the row; pattern ratios are prefix evidence and should not be treated as exhaustive.';
    return compact;
  });

  return {
    searchBudget:{maxNodes, maxSolutions},
    rowCount:rows.length,
    strictBehavior:'--strict fails incomplete pattern enumeration; --strict-patterns only fails design-policy violations.',
    reviewGuidance:rows.length === 0
      ? 'All rows were exhausted under the selected budget.'
      : 'Review incomplete rows before claiming full pattern coverage. Increase budgets for targeted rows or document them as bounded evidence.',
    rows,
  };
}

function patternPolicyViolations(patternRows) {
  const violations = [];
  for (const row of patternRows) {
    const pureSelfDivision = row.pureShortcutSolutions
      .filter(solution => solution.patterns.includes('pure-self-division'));
    if (row.number > pureSelfDivisionLearningMax && pureSelfDivision.length > 0) {
      violations.push({
        severity:!makeOneMode && row.number >= 50 ? 'blocker' : 'review',
        rule:'late-pure-self-division',
        number:row.number,
        name:row.name,
        sample:row.sample,
        target:row.target,
        slots:row.slots,
        sticks:row.sticks,
        examples:pureSelfDivision.slice(0, 5).map(solution => solution.expression),
        expectation:`After Round ${pureSelfDivisionLearningMax}, N/N should not be a standalone universal key unless this round is explicitly redesigned as an intentional callback or requires another nontrivial pattern.`,
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
    const witness = equalityWitnessForGroup(group);
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

function equalityWitnessForGroup(group) {
  const resource = parseResourceKey(group.key);
  return findEqualityWitness(resource.slots, resource.sticks);
}

function intentionalSharedEqualityReason(group) {
  const roundsKey = group.rounds.join(",");
  const resource = parseResourceKey(group.key);
  if (resource.slots === 3 && resource.sticks === 4 && roundsKey === "2,5") {
    return "early tutorial echo";
  }

  if (resource.slots === 9 && resource.sticks === 11 && roundsKey === "30,100") {
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
  if (!makeOneMode) {
    warnings.push(...identityErrors(rounds));
  }
  for (const [start, end, label] of bands) {
    if (makeOneMode) {
      continue;
    }

    const coverageRequired = label !== "tutorial";
    const bandRounds = rounds.slice(start - 1, end);
    const equalityCount = bandRounds.filter((round) => round.symbols.includes("=")).length;
    const starCount = bandRounds.filter((round) => round.symbols.includes("*")).length;
    const crossCount = bandRounds.filter((round) => round.symbols.includes("×") || round.symbols.includes("x")).length;

    if (coverageRequired && equalityCount === 0) {
      warnings.push(`${start}-${end} (${label}) has no direct equality rounds.`);
    }

    if (coverageRequired && starCount === 0) {
      warnings.push(`${start}-${end} (${label}) has no star-multiply rounds.`);
    }

    if (coverageRequired && crossCount === 0) {
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
