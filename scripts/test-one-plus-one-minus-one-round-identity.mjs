import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {spawnSync} from 'node:child_process';
import * as identity from './one-plus-one-minus-one-round-identity.mjs';
const { extractRounds, acceptsRound, analyzeIdentity, findEqualityWitness, identityErrors,
  generateCandidatePool, evaluate, tokenCosts, classifySolutionPatterns, analyzeRoundPatterns,
  findDominantPatternCandidates, findEqualityEchoCandidates } = identity;

const reportSpawnOptions = {
  cwd:new URL('../', import.meta.url),
  encoding:'utf8',
  maxBuffer:8 * 1024 * 1024,
};

const round = (number, sample, target) => ({number, sample, symbols: sample.split(' '),
  stickCount: sample.split(' ').reduce((n, t) => n + ({'1':1,'11':2,'111':3,'+':2,'-':1,'/':1,'×':2,'*':3,'=':2}[t]), 0), target});

const redesigned = [
  [83, '1 1 = 1 × 11', 27, '111 1 / 11'],
  [11, '1 = 1', 2, '1 1 - 1'],
  [33, '1 1 = 1 1', 21, '11 1 - 1'],
  [48, '1 1 - 1', 11, '1 11 - 111'],
  [35, '1 / 1 + 1 1', 43, '1 1 - 11'],
  [43, '1 = 1 × 1', 16, '1 1 + 1 / 1'],
  [57, '111 - 111 = 11 - 11', 44, '11 1 + 11 = 11 + 111'],
  [75, '11 + 1 = 11 / 1 + 1', 55, '111 = 111 + 1 1 - 11'],
  [88, '1 11 = 1 × 111', 82, '111 - 11 * 111 / 111'],
  [38, '11 = 1 1', 18, '1 × 1 1'],
  [39, '11 = 11', 20, '11 + 111'],
  [70, '111 / 1 = 111', 87, '111 / 111 * 111'],
  [90, '1 + 1 = 1 + 1', 19, '1 1 - 11 - 1'],
  [76, '1 1 1 1 111 = 1 111 111', 64, '111 - 111 + 11 / 11 + 1'],
  [95, '1 1 1 11 = 11 111', 22, '1 + 1 1 × 11 / 11 + 1'],
  [96, '1 1 = 1 1 × 1', 25, '111 - 1 - 1 - 1 / 1 - 1'],
  [72, '1 1 1 1 1 = 11 111', 29, '1 1 × 11 - 11 + 11 - 11'],
  [73, '1 1 1 1 1 = 1 1 111', 54, '1 / 1 1 - 1 / 11 + 1'],
  [77, '1 1 1 = 1 11 × 1', 31, '1 / 1 1 - 1 / 11 + 11'],
  [93, '1 1 11 = 1 111', 42, '1 11 + 1 - 1 - 11 / 11'],
  [71, '1 1 1 1 = 1 1 11', 58, '1 - 1 / 1 - 11 / 1 1'],
  [92, '1 × 11 = 11', 15, '11 + 111 / 111 * 111'],
  [26, '1 1 1 = 1 1 1', 13, '1 1 1 / 1 1 1'],
];
for (const [number, shared, owner, alternative] of redesigned) {
  test(`Round ${number} separates a shared answer without banning it globally`, () => {
    const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
    assert.equal(acceptsRound(rounds[number - 1], shared.split(' ')), false, shared);
    assert.equal(acceptsRound(rounds[owner - 1], shared.split(' ')), true, `Preserve owner ${owner}`);
    assert.equal(acceptsRound(rounds[number - 1], alternative.split(' ')), true, alternative);
  });
}

test('same resources and target duplicate across sample equation styles', () => {
  const a = round(5, '1 × 1', 1), b = round(18, '1 = 1', 1);
  assert.equal(acceptsRound(a, b.symbols), true);
  assert.equal(acceptsRound(b, a.symbols), true);
  assert.deepEqual(analyzeIdentity([a,b]).duplicates.map(g=>g.rounds), [[5,18]]);
});
test('different targets share equality but not necessarily all arithmetic answers', () => {
  const a = round(2, '1 + 1', 2), b = round(11, '11 - 1', 10);
  assert.equal(acceptsRound(a, ['1','=','1']), true);
  assert.equal(acceptsRound(b, ['1','=','1']), true);
  assert.equal(acceptsRound(b, a.symbols), false);
  assert.equal(analyzeIdentity([a,b]).duplicates.length, 0);
  assert.equal(analyzeIdentity([a,b]).resourceRepeats.length, 1);
});
test('cross acceptance checks slots, cost, grammar and finite value', () => {
  const a = round(1, '11 / 11', 1);
  for(const tokens of [['1','/','1'], ['1','1','/','11'], ['+','11','11'], ['0','+','1'], ['11','=','1']])
    assert.equal(acceptsRound(a, tokens), false, tokens.join(' '));
  assert.equal(acceptsRound(a, ['1','*','1']), true);
});
test('search discovers equality absent from the sample and reports limits', () => {
  const found = findEqualityWitness(3,4);
  assert.deepEqual(found.symbols, ['1','=','1']);
  assert.equal(found.status, 'found');
  assert.equal(findEqualityWitness(3,3).status, 'exhausted');
  assert.equal(findEqualityWitness(9,14,{maxNodes:1}).status, 'limited');
});
test('parser retains numerical fallback target for equality samples', () => {
  const text = 'return new[]\n{\n Puzzle("A", "11 = 11"),\n Puzzle("B", "1 + 1")\n};\n}';
  const data=extractRounds(text);
  assert.equal(data[0].target,11);
  assert.equal(data[0].stickCount,6);
  assert.equal(data[1].number,2);
});

test('release goal rounds keep integer targets for exact acceptance', () => {
  const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  assert.equal(rounds.length, 100);
  for (const round of rounds) {
    assert.equal(Number.isInteger(round.target), true, `Round ${round.number} has non-integer target ${round.target}`);
  }
});

test('search validates budgets instead of silently overclaiming exhaustive results', () => {
  assert.throws(()=>findEqualityWitness(0,4));
  assert.throws(()=>findEqualityWitness(3,4,{maxNodes:0}));
  assert.throws(()=>findEqualityWitness(3,NaN));
  assert.equal(findEqualityWitness(4,5).status,'exhausted');
  assert.equal(findEqualityWitness(6,7).status,'exhausted');
  assert.equal(findEqualityWitness(6,13).status,'exhausted');
});

test('identity gate rejects nearby shared answers and limits the title exception', () => {
  assert.ok(identityErrors([round(20,'1 + 1',2),round(22,'11 - 1',10)]).some(x=>x.includes('Nearby')));
  assert.equal(identityErrors([round(2,'1 + 1',2),round(5,'1 × 1',1)]).length,0);
  assert.ok(identityErrors([round(30,'1 / 1',1),round(100,'1 / 1',1)]).length);
  const title='1 + 1 - 1 × 1 / 1';
  assert.equal(identityErrors([round(30,title,1),round(100,title,1)]).length,0);
  assert.ok(identityErrors([round(30,title,1),round(74,title,1),round(100,title,1)]).length);
});

test('candidate search is deterministic and does not mutate round data', () => {
  const rounds = [round(1, '1 + 1', 2), round(2, '1 - 1', 0)];
  const original = structuredClone(rounds);
  const options = {seed:15092026, samples:50000};
  const first = generateCandidatePool(rounds, options);
  assert.deepEqual(first, generateCandidatePool(rounds, options));
  assert.deepEqual(rounds, original);
  assert.equal(first.samples, options.samples);
  assert.equal(first.seed, options.seed);
  assert.match(first.method, /NOT exhaustive/);
  assert.ok(first.candidates.length > 0);
});

test('small-board enumeration includes arithmetic and target-independent equality answers', () => {
  const result = identity.enumerateRoundSolutions(round(1, '1 + 1', 2));
  assert.equal(result.status, 'exhausted');
  assert.equal(result.complete, true);
  assert.deepEqual(result.solutions.map(s => s.join(' ')).sort(), ['1 + 1', '1 = 1']);
  assert.equal(identity.enumerateRoundSolutions(round(1, '1 - 1', 99)).solutions.length, 0);
});

test('small-board enumeration retains all registered adjacent-number splits', () => {
  const cases = [
    ['1 1 - 1', 10, ['1 1 - 1']],
    ['1 11 - 1', 110, ['1 11 - 1', '11 1 - 1']],
    ['111 - 1 11', 0, ['1 11 - 111', '11 1 - 111', '111 - 1 11', '111 - 11 1']],
    ['11 11 / 11', 101, ['1 111 / 11', '11 11 / 11', '111 1 / 11']],
  ];
  for (const [sample, target, expected] of cases) {
    const data = round(1, sample, target), before = structuredClone(data);
    const result = identity.enumerateRoundSolutions(data);
    assert.equal(result.complete, true, sample);
    assert.deepEqual(result.solutions.map(s => s.join(' ')).sort(), expected.sort(), sample);
    assert.ok(result.solutions.every(s => acceptsRound(data, s)));
    assert.deepEqual(data, before);
  }
});

test('enumeration matches unpruned token products on small boards', () => {
  const tokens = [...tokenCosts.keys()].filter(t => t !== 'x');
  for (const data of [round(1, '1', 1), round(2, '1 + 1', 2), round(3, '111 - 1 11', 0)]) {
    const expected = [];
    const visit = symbols => {
      if (symbols.length === data.symbols.length) {
        if (acceptsRound(data, symbols)) expected.push(symbols.join(' '));
        return;
      }
      for (const token of tokens) visit([...symbols, token]);
    };
    visit([]);
    const actual = identity.enumerateRoundSolutions(data);
    assert.equal(actual.complete, true);
    assert.deepEqual(actual.solutions.map(s => s.join(' ')).sort(), expected.sort());
  }
});

test('small nonzero divisions are not accepted as zero targets', () => {
  const data = round(48, '111 - 1 11', 0), symbols = ['1','/','11','111'];
  const result = evaluate(symbols);
  assert.ok(result.value > 0 && result.value < 0.0001);
  assert.equal(acceptsRound(data, symbols), false);
  assert.equal(identity.enumerateRoundSolutions(data).solutions.some(s => s.join(' ') === symbols.join(' ')), false);
});

test('exact rational arithmetic preserves fractional cancellation and equality', () => {
  assert.equal(evaluate(['1', '/', '11', '*', '11']).value, 1);
  assert.equal(acceptsRound(round(1, '1 / 11 * 11', 1), ['1', '/', '11', '*', '11']), true);
  assert.equal(acceptsRound(round(1, '1 / 11 = 1 / 11', 1), ['1', '/', '11', '=', '1', '/', '11']), true);
  assert.equal(acceptsRound(round(1, '1 / 111 = 1 / 11', 1), ['1', '/', '111', '=', '1', '/', '11']), false);
});

test('enumeration cannot claim complete results after either work limit', () => {
  const data = round(1, '1 + 1', 2);
  const nodes = identity.enumerateRoundSolutions(data, {maxNodes:1});
  assert.equal(nodes.status, 'limited');
  assert.equal(nodes.complete, false);
  assert.equal(nodes.limitReason, 'node_budget');
  assert.equal(nodes.nodes, 1);
  assert.deepEqual(nodes.solutions, []);
  const count = identity.enumerateRoundSolutions(data, {maxSolutions:1});
  assert.equal(count.status, 'limited');
  assert.equal(count.complete, false);
  assert.equal(count.limitReason, 'solution_limit');
  assert.equal(count.solutions.length, 1);
  for (const options of [{maxNodes:0}, {maxNodes:Infinity}, {maxSolutions:0}, {maxSolutions:1.5}])
    assert.throws(() => identity.enumerateRoundSolutions(data, options));
  for (const invalid of [{...data, target:NaN}, {...data, stickCount:-1}, {...data, symbols:[]}])
    assert.throws(() => identity.enumerateRoundSolutions(invalid));
});

test('candidate search avoids occupied targets and shared equality resources', () => {
  const {candidates} = generateCandidatePool([round(1, '1 + 1', 2), round(2, '1 - 1', 0)], {samples:50000});
  assert.ok(candidates.every(c => !(c.slots === 3 && c.sticks === 4)));
  assert.ok(candidates.every(c => !(c.slots === 3 && c.sticks === 3 && c.target === 0)));
  assert.ok(candidates.some(c => c.slots === 3 && c.sticks === 3 && c.target === 1));
});

test('candidate search builds larger operands from registered tokens only', () => {
  const {candidates} = generateCandidatePool([], {samples:50000});
  assert.ok(candidates.some(c => c.slots === 4 && c.sticks === 7 && c.target === 101));
  for (const candidate of candidates) {
    assert.ok(candidate.symbols.every(t => tokenCosts.has(t)));
    assert.equal(candidate.sticks, candidate.symbols.reduce((n,t) => n + tokenCosts.get(t), 0));
    assert.equal(candidate.slots, candidate.symbols.length);
    assert.ok(candidate.slots <= 11 && candidate.sticks <= 18);
    assert.ok(Number.isInteger(candidate.target) && candidate.target >= -2 && candidate.target <= 122);
    assert.equal(evaluate(candidate.symbols).value, candidate.target);
  }
  assert.equal(new Set(candidates.map(c => `${c.slots}/${c.sticks}/${c.target}`)).size, candidates.length);
});

test('candidate search rejects invalid work budgets and seeds', () => {
  for (const samples of [0, -1, NaN, 1.5, 2000001])
    assert.throws(() => generateCandidatePool([], {samples}), /samples/);
  for (const seed of [-1, NaN, 1.5, 4294967296])
    assert.throws(() => generateCandidatePool([], {seed}), /seed/);
});

test('candidate CLI reports its budget without editing game data', () => {
  const rules = new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url);
  const before = fs.readFileSync(rules, 'utf8');
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], reportSpawnOptions);
  const valid = run(['--candidates', '--samples', '2000', '--seed', '0']);
  assert.equal(valid.status, 0, valid.stderr);
  const report = JSON.parse(valid.stdout);
  assert.equal(report.seed, 0);
  assert.equal(report.samples, 2000);
  assert.match(report.method, /NOT exhaustive/);
  const invalid = run(['--candidates', '--samples', '0']);
  assert.notEqual(invalid.status, 0);
  assert.match(invalid.stderr, /samples/);
  const identityReport = run(['--json']);
  assert.equal(identityReport.status, 0, identityReport.stderr);
  assert.equal(JSON.parse(identityReport.stdout).rounds.length, 100);
  assert.equal(fs.readFileSync(rules, 'utf8'), before);
});

test('sample evaluation CLI reports accepted fixed-target pattern evidence', () => {
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], reportSpawnOptions);
  const result = run([
    '--make-one',
    '--evaluate-sample',
    '1 1 - 11 + 1',
    '--target',
    '1',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ]);
  assert.equal(result.status, 0, result.stderr);
  const report = JSON.parse(result.stdout);
  assert.equal(report.accepted, true);
  assert.equal(report.target, 1);
  assert.equal(report.sampleTarget, 1);
  assert.equal(report.slots, 6);
  assert.equal(report.sticks, 8);
  assert.deepEqual(report.samplePatterns, ['additive-cancellation', 'self-subtraction']);
  assert.equal(report.profile.complete, true);
  assert.equal(report.profile.dominantPattern, 'additive-cancellation');
  assert.equal(report.profile.dominantRatio < 0.3, true);
});

test('dominant shortcut candidate search is deterministic and reports bounded evidence', () => {
  const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  const before = structuredClone(rounds);
  const options = {roundNumbers:[91], seed:424242, samples:2000, maxEvaluations:4, maxResults:2, candidateBudget:8};
  const first = findDominantPatternCandidates(rounds, options);
  assert.deepEqual(first, findDominantPatternCandidates(rounds, options));
  assert.deepEqual(rounds, before);
  assert.equal(first.seed, options.seed);
  assert.equal(first.samples, options.samples);
  assert.match(first.method, /bounded and heuristic/);
  assert.match(first.method, /analysis-only/);
  assert.match(first.method, /visibly similar/);
  assert.match(first.method, /combination cues/);
  assert.equal(first.rounds.length, 1);
  assert.equal(first.rounds[0].number, 91);
  assert.equal(first.rounds[0].search.evaluated <= options.maxEvaluations, true);
  assert.equal(first.rounds[0].search.minRecommendedSolutions, 8);
  assert.equal(first.rounds[0].search.minVisibleDiversity, 2);
  assert.equal(first.rounds[0].search.minRecommendedImprovement, 0.05);
  assert.equal(first.rounds[0].search.minCombinationScore, 2);
  assert.equal(first.rounds[0].search.preserveTarget, false);
  assert.equal(first.rounds[0].current.solutionCount > 0, true);
  assert.ok(first.rounds[0].current.rankedPatterns.every(row =>
    typeof row.pattern === 'string' && Number.isFinite(row.ratio)));
  assert.ok(first.rounds[0].candidates.every(candidate =>
    candidate.complete && candidate.improvement > 0 &&
    candidate.slots >= first.rounds[0].slots &&
    candidate.sticks >= first.rounds[0].sticks &&
    Number.isInteger(candidate.visibleDiversityScore) &&
    Array.isArray(candidate.samplePatterns) &&
    Array.isArray(candidate.sourceSamplePatterns) &&
    Array.isArray(candidate.combinationTags) &&
    Number.isInteger(candidate.combinationScore) &&
    typeof candidate.analysisOnly === 'boolean' &&
    Array.isArray(candidate.analysisNotes)));
});

test('dominant shortcut candidate CLI handles explicit and inferred targets', () => {
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], reportSpawnOptions);
  const explicit = run(['--dominant-candidates', '91', '--samples', '2000', '--max-evaluations', '2', '--max-results', '1', '--candidate-budget', '4']);
  assert.equal(explicit.status, 0, explicit.stderr);
  const explicitReport = JSON.parse(explicit.stdout);
  assert.equal(explicitReport.rounds[0].number, 91);
  assert.equal(explicitReport.rounds[0].search.evaluated <= 4, true);
  assert.equal(explicitReport.rounds[0].search.minRecommendedSolutions, 8);
  assert.equal(explicitReport.rounds[0].search.minVisibleDiversity, 2);
  assert.equal(explicitReport.rounds[0].search.minRecommendedImprovement, 0.05);
  assert.equal(explicitReport.rounds[0].search.minCombinationScore, 2);
  const inferred = run(['--dominant-candidates', '--samples', '2000', '--max-evaluations', '1', '--max-results', '1', '--candidate-budget', '1', '--max-extra-slots', '0', '--max-extra-sticks', '0']);
  assert.equal(inferred.status, 0, inferred.stderr);
  const inferredReport = JSON.parse(inferred.stdout);
  assert.ok(inferredReport.rounds.length > 0);
  assert.ok(inferredReport.rounds.some(row => row.number === 61));
  assert.notEqual(run(['--dominant-candidates', '0']).status, 0);
});

test('equality echo candidate search is bounded and reports non-echo samples', () => {
  const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  const before = structuredClone(rounds);
  const report = findEqualityEchoCandidates(rounds, {
    roundNumbers:[97],
    maxEvaluations:4,
    maxResults:2,
    maxSideExpressions:16,
    maxResourceDelta:0,
  });
  assert.deepEqual(rounds, before);
  assert.match(report.method, /Equality-echo candidate search/);
  assert.equal(report.rounds[0].number, 97);
  assert.equal(report.rounds[0].search.evaluated <= 4, true);
  assert.equal(report.rounds[0].search.maxSideExpressions, 16);
  assert.ok(report.rounds[0].candidates.every(candidate =>
    candidate.complete &&
    candidate.improvement > 0 &&
    Array.isArray(candidate.combinationTags) &&
    !candidate.samplePatterns.includes('same-expression-equality') &&
    typeof candidate.analysisOnly === 'boolean'));
});

test('equality echo candidate CLI handles explicit targets', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--equality-candidates', '97',
    '--max-evaluations', '2',
    '--max-results', '1',
    '--max-side-expressions', '80',
    '--max-resource-delta', '0',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.equal(report.rounds[0].number, 97);
  assert.equal(report.rounds[0].search.evaluated <= 2, true);
  assert.notEqual(spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--equality-candidates', '105',
  ], reportSpawnOptions).status, 0);
});

test('solution CLI reports completeness and validates selected rounds', () => {
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], reportSpawnOptions);
  const result = run(['--solutions', '11,33,48,83']);
  assert.equal(result.status, 0, result.stderr);
  const report = JSON.parse(result.stdout);
  assert.deepEqual(report.rounds.map(r => [r.number, r.complete, r.solutions.length]),
    [[11,true,1], [33,true,2], [48,true,4], [83,true,3]]);
  const limited = run(['--solutions', '48', '--max-nodes', '1', '--strict']);
  assert.notEqual(limited.status, 0);
  assert.equal(JSON.parse(limited.stdout).rounds[0].limitReason, 'node_budget');
  for (const value of ['0', '121', '11,', '11,11', 'garbage'])
    assert.notEqual(run(['--solutions', value]).status, 0, value);
});

test('solution pattern classifier identifies universal shortcut families', () => {
  assert.deepEqual(classifySolutionPatterns(['11', '/', '11']), ['pure-self-division', 'self-division']);
  assert.deepEqual(classifySolutionPatterns(['111', '-', '111']),
    ['additive-cancellation', 'pure-self-subtraction', 'self-subtraction']);
  assert.deepEqual(classifySolutionPatterns(['111', '-', '11', '=', '111', '-', '11']),
    ['constructed-equality', 'same-expression-equality']);
  assert.deepEqual(classifySolutionPatterns(['11', '=', '11']),
    ['constructed-equality', 'same-expression-equality', 'same-number-equality']);
  assert.deepEqual(classifySolutionPatterns(['1', '/', '1', '+', '11']),
    ['divide-by-one', 'self-division']);
  assert.deepEqual(classifySolutionPatterns(['11', '*', '1']),
    ['multiply-by-one']);
  assert.deepEqual(classifySolutionPatterns(['1', '+', '11', '-', '11']),
    ['additive-cancellation', 'self-subtraction']);
  assert.deepEqual(classifySolutionPatterns(['111', '+', '1', '-', '111']),
    ['additive-cancellation']);
  assert.deepEqual(classifySolutionPatterns(['1', '/', '111', '×', '111']),
    ['reciprocal-cancellation']);
  assert.deepEqual(classifySolutionPatterns(['111', '×', '11', '/', '11']),
    ['reciprocal-cancellation', 'self-division']);
});

test('round pattern map exposes shortcut solutions without changing acceptance', () => {
  const rows = analyzeRoundPatterns([
    round(1, '1 / 1', 1),
    round(2, '1 + 1', 2),
    round(3, '1 + 1 = 1 + 1', 2),
  ]);
  assert.equal(rows.length, 3);
  assert.equal(rows[0].complete, true);
  assert.ok(rows[0].pureShortcutSolutions.some(solution => solution.expression === '1 / 1'));
  assert.ok(rows[1].patternCounts['same-number-equality'] >= 1);
  assert.ok(rows[2].samplePatterns.includes('same-expression-equality'));
});

test('late N over N redesign removes standalone self-division keys from revised rounds', () => {
  const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  for (const [number, sample] of [
    [52, '1 1 1 - 1'],
    [59, '111 * 11 - 11 11'],
    [65, '1 - 1 1 / 11 - 1'],
    [100, '11 × 11 - 111 + 1'],
  ]) {
    const round = rounds[number - 1];
    assert.equal(round.sample, sample);
    const enumeration = identity.enumerateRoundSolutions(round, {maxNodes:200000, maxSolutions:1000});
    assert.equal(enumeration.complete, true, `Round ${number} enumeration`);
    assert.equal(enumeration.solutions.some(solution =>
      classifySolutionPatterns(solution).includes('pure-self-division')), false, `Round ${number}`);
  }
});

test('pattern CLI reports shortcut review rows', () => {
  const run = spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', '--patterns', '--max-nodes', '200000', '--max-solutions', '1000'], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.match(report.method, /design-review map/);
  assert.equal(report.rows.length, 100);
  assert.ok(report.shortcutPolicy.learningRounds.some(row => row.number === 4));
  assert.ok(report.shortcutPolicy.reviewRounds.every(row => row.number > 30));
  assert.deepEqual(report.shortcutPolicy.reviewRounds.map(row => row.number), []);
  assert.deepEqual(report.shortcutPolicy.violations, []);
  assert.match(report.shortcutPolicy.equalityEchoReview.reviewWindow, /after Round 50/);
  assert.ok(report.shortcutPolicy.equalityEchoReview.reviewRows.every(row => row.number > 50));
  assert.deepEqual(report.shortcutPolicy.equalityEchoReview.sampleEchoRows, []);
  assert.deepEqual(
    report.shortcutPolicy.equalityEchoReview.alternateOnlyRows.map(row => row.number),
    report.shortcutPolicy.equalityEchoReview.reviewRows.map(row => row.number),
  );
  assert.deepEqual(report.shortcutPolicy.equalityEchoReview.highPriorityAlternateRows.map(row => row.number), [97, 64]);
  assert.ok(report.shortcutPolicy.equalityEchoReview.reviewRows.some(row => row.number === 54));
  assert.equal(report.shortcutPolicy.equalityEchoReview.reviewRows.some(row => row.number === 53), false);
  assert.equal(report.shortcutPolicy.equalityEchoReview.reviewRows.some(row => row.number === 61), false);
  assert.equal(report.shortcutPolicy.equalityEchoReview.reviewRows.some(row => row.number === 78), false);
  assert.ok(report.shortcutReview.some(row =>
    row.pureShortcutSolutions.some(solution => solution.patterns.includes('pure-self-division'))));
});

test('pattern CLI summary omits bulky per-round rows while keeping policy targets', () => {
  const run = spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', '--patterns', '--summary', '--max-nodes', '200000', '--max-solutions', '1000'], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.match(report.method, /Summary/);
  assert.equal(report.rows, undefined);
  assert.equal(report.summary.rounds, 100);
  assert.equal(report.summary.searchBudget.maxNodes, 200000);
  assert.equal(report.summary.searchBudget.maxSolutions, 1000);
  assert.equal(Array.isArray(report.incompleteReview.rows), true);
  assert.match(report.incompleteReview.strictBehavior, /--strict fails/);
  assert.deepEqual(report.shortcutPolicy.violations, []);
  assert.deepEqual(report.shortcutPolicy.equalityEchoReview.highPriorityAlternateRows.map(row => row.number), [97, 64]);
  assert.ok(report.shortcutPolicy.dominantPatternReview.highPriorityRows.some(row =>
    row.number === 61 && row.pattern === 'multiply-by-one'));
  assert.equal(report.shortcutPolicy.dominantPatternReview.highPriorityRows.some(row =>
    row.number === 91), false);
  assert.equal(report.shortcutPolicy.dominantPatternReview.highPriorityRows.some(row =>
    row.number === 65), false);
  assert.ok(report.shortcutPolicy.dominantPatternReview.highPriorityRows.every(row =>
    row.ratio >= 0.6 && row.solutionCount >= 20));
});

test('make-one pattern CLI tracks default mode shortcut regressions separately', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--patterns',
    '--summary',
    '--strict-patterns',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.equal(report.summary.rounds, 30);
  assert.deepEqual(report.shortcutPolicy.reviewRounds, []);
  assert.deepEqual(report.shortcutPolicy.violations, []);
  assert.match(report.shortcutPolicy.equalityEchoReview.reviewWindow, /after Round 10/);
  assert.deepEqual(report.shortcutPolicy.equalityEchoReview.highPriorityAlternateRows, []);
  assert.equal(report.summary.uniqueResources, 22);
  assert.equal(report.summary.repeatedResourceGroups, 4);
  assert.deepEqual(report.incompleteReview.rows.map(row => row.number), [20, 22, 23, 29]);
  assert.ok(report.incompleteReview.rows.every(row =>
    row.limitReason === 'node_budget' &&
    row.recheckCommand.includes('--make-one') &&
    row.recheckCommand.includes('--max-nodes 1000000') &&
    row.interpretation.includes('prefix evidence')));
  assert.deepEqual(report.shortcutPolicy.dominantPatternReview.highPriorityRows, []);
  assert.equal(report.shortcutPolicy.dominantPatternReview.rankedRowsTruncated, true);
  assert.match(report.shortcutPolicy.authoredSampleReview.reviewWindow, /authored samples/);
  assert.ok(report.shortcutPolicy.authoredSampleReview.rows.includes(14));
  assert.equal(report.shortcutPolicy.authoredSampleReview.rows.includes(25), false);
  assert.equal(report.shortcutPolicy.authoredSampleReview.rows.includes(26), false);
  assert.equal(report.shortcutPolicy.authoredSampleReview.rows.includes(30), false);
  assert.equal(report.shortcutPolicy.authoredSampleReview.divisionRows.includes(25), false);
  assert.equal(report.shortcutPolicy.authoredSampleReview.divisionRows.includes(26), false);
  assert.equal(report.shortcutPolicy.authoredSampleReview.divisionRows.includes(30), false);
  assert.ok(report.shortcutPolicy.authoredSampleReview.multiplicationRows.includes(28));
  assert.match(report.resourceReview.method, /Repeated slot\/stick resources/);
  assert.equal(report.resourceReview.unresolvedFoundSharedEqualityPairs, 13);
  assert.equal(report.resourceReview.reviewRows.some(row =>
    row.rounds.join(',') === '18,29' ||
    row.key === '6/8'), false);
});

test('make-one authored sample review keeps late shortcut evidence visible', () => {
  const rounds = identity.extractMakeOneRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  const round25 = rounds[24];
  const round30 = rounds[29];
  assert.equal(round25.sample, '1 / 1 1 1 × 111');
  assert.equal(round25.symbols.length, 7);
  assert.equal(round25.stickCount, 10);
  assert.deepEqual(classifySolutionPatterns(round25.symbols), ['reciprocal-cancellation']);
  assert.equal(identity.acceptsRound(round25, round25.symbols), true);

  assert.equal(round30.sample, '1 / 111 111 × 111 111');
  assert.equal(round30.symbols.length, 7);
  assert.equal(round30.stickCount, 16);
  assert.deepEqual(classifySolutionPatterns(round30.symbols), ['reciprocal-cancellation']);
  assert.equal(identity.acceptsRound(round30, round30.symbols), true);

  const round29 = rounds[28];
  assert.equal(round29.sample, '1 1 + 111 - 11 × 11');
  assert.equal(round29.symbols.length, 8);
  assert.equal(round29.stickCount, 14);
  assert.equal(classifySolutionPatterns(round29.symbols).length, 0);
  assert.equal(identity.acceptsRound(round29, round29.symbols), true);
});

test('make-one dominant candidate search preserves fixed target one', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--dominant-candidates',
    '18',
    '--samples',
    '500',
    '--max-evaluations',
    '3',
    '--max-results',
    '2',
    '--candidate-budget',
    '12',
    '--allow-fewer-slots',
    '--allow-fewer-sticks',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.ok(report.rounds.every(row => row.target === 1 && row.search.preserveTarget === true));
  assert.ok(report.rounds.every(row => row.search.nearbyCandidateCount > 0));
  assert.ok(report.rounds.every(row => row.candidates.every(candidate => candidate.target === 1)));
});

test('make-one resource candidate search flags shortcut-risk replacements', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--resource-candidates',
    '17',
    '--max-evaluations',
    '8',
    '--max-results',
    '4',
    '--candidate-budget',
    '40',
    '--max-nearby-nodes',
    '500000',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.match(report.method, /Resource-repeat candidate search/);
  assert.equal(report.rounds.length, 1);
  assert.equal(report.rounds[0].number, 17);
  assert.equal(report.rounds[0].search.preserveTarget, true);
  assert.equal(report.rounds[0].search.maxNearbyNodes, 500000);
  assert.equal(report.rounds[0].search.nearbyNodes <= 500000, true);
  assert.ok(report.rounds[0].search.compositeCandidateCount > 0);
  assert.ok(report.rounds[0].search.enumeratedCandidateCount > 0);
  assert.equal(report.rounds[0].candidateSummary.evaluated, 8);
  assert.equal(report.rounds[0].candidateSummary.recommendableCount, 0);
  assert.equal(report.rounds[0].candidateSummary.analysisOnlyCount, 8);
  assert.ok(report.rounds[0].candidateSummary.sourceCounts['composite-target-one'] > 0);
  assert.ok(report.rounds[0].candidateSummary.sourceCounts['nearby-resource-enumeration'] > 0);
  assert.match(report.rounds[0].candidateSummary.bestBySource['composite-target-one'].sample, /\S/);
  assert.match(report.rounds[0].candidateSummary.bestBySource['nearby-resource-enumeration'].sample, /\S/);
  assert.ok(report.rounds[0].candidateSummary.riskCounts.latePureSelfDivision > 0);
  assert.ok(report.rounds[0].candidateSummary.riskCounts.dominantShortcut > 0);
  assert.ok(report.rounds[0].candidateSummary.riskCounts.authoredSampleShortcut > 0);
  assert.ok(report.rounds[0].candidates.length > 0);
  assert.ok(report.rounds[0].candidates.every(candidate => candidate.target === 1));
  assert.ok(report.rounds[0].candidates.some(candidate =>
    candidate.sample === '1 - 1 / 111 * 111 + 1' &&
    candidate.source === 'composite-target-one' &&
    candidate.combinationTags.includes('nontrivial-division') &&
    candidate.combinationTags.includes('nontrivial-multiply')));
  assert.ok(report.rounds[0].candidates.some(candidate =>
    candidate.analysisOnly &&
    candidate.pureDivisionResource === false &&
    candidate.analysisNotes.some(note => note.includes('dominant shortcut'))));

  const groupedRun = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--resource-candidates',
    '7/10',
    '--max-evaluations',
    '4',
    '--max-results',
    '2',
    '--candidate-budget',
    '24',
    '--max-nearby-nodes',
    '300000',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ], reportSpawnOptions);
  assert.equal(groupedRun.status, 0, groupedRun.stderr);
  const groupedReport = JSON.parse(groupedRun.stdout);
  assert.equal(groupedReport.rounds.length, 4);
  for (const row of groupedReport.rounds) {
    assert.equal(row.search.sourceDiverseEvaluation, true);
    assert.ok(row.candidateSummary.sourceCounts['composite-target-one'] > 0);
    assert.ok(row.candidateSummary.sourceCounts['occupied-resource-swap'] > 0);
    assert.ok(row.candidateSummary.sourceCounts['nearby-resource-enumeration'] > 0);
    assert.match(row.candidateSummary.bestBySource['composite-target-one'].sample, /\S/);
    assert.match(row.candidateSummary.bestBySource['occupied-resource-swap'].sample, /\S/);
    assert.match(row.candidateSummary.bestBySource['nearby-resource-enumeration'].sample, /\S/);
    assert.equal(row.candidateSummary.bestBySource['occupied-resource-swap'].analysisOnly, true);
    assert.ok(row.candidateSummary.bestBySource['occupied-resource-swap'].analysisNotes.some(note =>
      note.includes('still shares slot/stick')));
  }

  const equalityEchoRun = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--resource-candidates',
    '30',
    '--max-evaluations',
    '12',
    '--max-results',
    '6',
    '--candidate-budget',
    '50',
    '--max-nearby-nodes',
    '500000',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ], reportSpawnOptions);
  assert.equal(equalityEchoRun.status, 0, equalityEchoRun.stderr);
  const equalityEchoReport = JSON.parse(equalityEchoRun.stdout);
  assert.equal(equalityEchoReport.rounds[0].candidateSummary.recommendableCount, 0);
  assert.ok(equalityEchoReport.rounds[0].candidateSummary.riskCounts.highEqualityEcho > 0);
});

test('make-one resource candidates reject visible shortcut samples after the learning window', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--resource-candidates',
    '13',
    '--max-evaluations',
    '12',
    '--max-results',
    '6',
    '--candidate-budget',
    '120',
    '--max-nearby-nodes',
    '1500000',
    '--max-nodes',
    '300000',
    '--max-solutions',
    '1500',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.equal(report.rounds.length, 1);
  const row = report.rounds[0];
  assert.equal(row.number, 13);
  assert.equal(row.candidateSummary.recommendableCount, 0);
  assert.ok(row.candidateSummary.riskCounts.authoredSampleShortcut > 0);
  assert.ok(row.candidates.some(candidate =>
    candidate.sample === '1 1 - 11 + 1' &&
    candidate.samplePatterns.includes('self-subtraction') &&
    candidate.analysisOnly &&
    candidate.analysisNotes.some(note => note.includes('authored sample uses shortcut pattern'))));
});

test('make-one resource candidate CLI accepts repeated resource keys', () => {
  const run = spawnSync(process.execPath, [
    'scripts/report-one-plus-one-minus-one-round-quality.mjs',
    '--make-one',
    '--resource-candidates',
    '6/8',
    '--max-evaluations',
    '1',
    '--max-results',
    '1',
    '--candidate-budget',
    '4',
    '--max-nearby-nodes',
    '50000',
    '--max-nodes',
    '200000',
    '--max-solutions',
    '1000',
  ], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.deepEqual(report.rounds.map(row => row.number), [18]);
  assert.ok(report.rounds.every(row => row.slots === 6 && row.sticks === 8));
});

test('late equality echo redesign removes same-expression samples from targeted rounds', () => {
  const rounds = extractRounds(fs.readFileSync(new URL('../prototypes/one-plus-one-minus-one/Assets/_Project/Scripts/OnePlusOneMinusOneRules.cs', import.meta.url), 'utf8'));
  for (const [number, sample] of [
    [53, '111 / 1'],
    [61, '11 / 11 + 1 + 1'],
    [78, '111 × 111 - 111 1 × 11'],
  ]) {
    const round = rounds[number - 1];
    assert.equal(round.sample, sample);
    assert.equal(classifySolutionPatterns(round.symbols).includes('same-expression-equality'), false);
  }
});

test('strict pattern policy passes after late pure N/N review candidates are redesigned', () => {
  const run = spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', '--patterns', '--strict-patterns', '--max-nodes', '200000', '--max-solutions', '1000'], reportSpawnOptions);
  assert.equal(run.status, 0, run.stderr);
  const report = JSON.parse(run.stdout);
  assert.deepEqual(report.shortcutPolicy.violations, []);
});
