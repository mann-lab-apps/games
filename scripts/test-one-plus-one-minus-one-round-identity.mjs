import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {spawnSync} from 'node:child_process';
import * as identity from './one-plus-one-minus-one-round-identity.mjs';
const { extractRounds, acceptsRound, analyzeIdentity, findEqualityWitness, identityErrors,
  generateCandidatePool, evaluate, tokenCosts } = identity;

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
  [52, '11 / 11 = 1', 14, '11 1 / 111'],
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
  [99, '1 1 1 = 1 × 111', 59, '111 - 11 - 1 / 1 + 1 - 1'],
  [26, '1 1 1 = 1 1 1', 13, '1 1 1 / 1 1 1'],
  [94, '1 111 = 1 111 × 1', 65, '11 - 1 / 111 * 1 11 + 1'],
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
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], {
    cwd:new URL('../', import.meta.url), encoding:'utf8'
  });
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

test('solution CLI reports completeness and validates selected rounds', () => {
  const run = args => spawnSync(process.execPath, ['scripts/report-one-plus-one-minus-one-round-quality.mjs', ...args], {
    cwd:new URL('../', import.meta.url), encoding:'utf8'
  });
  const result = run(['--solutions', '11,33,48,83']);
  assert.equal(result.status, 0, result.stderr);
  const report = JSON.parse(result.stdout);
  assert.deepEqual(report.rounds.map(r => [r.number, r.complete, r.solutions.length]),
    [[11,true,1], [33,true,2], [48,true,4], [83,true,3]]);
  const limited = run(['--solutions', '48', '--max-nodes', '1', '--strict']);
  assert.notEqual(limited.status, 0);
  assert.equal(JSON.parse(limited.stdout).rounds[0].limitReason, 'node_budget');
  for (const value of ['0', '101', '11,', '11,11', 'garbage'])
    assert.notEqual(run(['--solutions', value]).status, 0, value);
});
