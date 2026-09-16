// Shared Node mirror of OnePlusOneMinusOneRules; Unity tests verify acceptance parity.
export function acceptsRound(round, symbols) {
  if (symbols.length !== round.symbols.length || symbols.some(t => !tokenCosts.has(t))) return false;
  if (symbols.reduce((n,t) => n + tokenCosts.get(t), 0) !== round.stickCount) return false;
  const result = solveRound(symbols, round.target);
  return result.valid && Number.isFinite(result.value);
}

export function analyzeIdentity(rounds) {
  const grouped = key => {
    const map = new Map();
    for (const round of rounds) {
      const value = key(round);
      if (!map.has(value)) map.set(value, []);
      map.get(value).push(round.number);
    }
    return [...map].filter(([,members])=>members.length>1).map(([key, rounds])=>({key,rounds}));
  };
  const resources = r => `${r.symbols.length}/${r.stickCount}`;
  const duplicates = grouped(r => `${resources(r)}/${r.target}`);
  const resourceRepeats = grouped(resources);
  const crossSamples = rounds.map(source => ({source:source.number, expression:source.sample,
    acceptedBy:rounds.filter(target=>acceptsRound(target,source.symbols)).map(r=>r.number)}));
  return {duplicates, resourceRepeats, crossSamples,
    uniqueResources:new Set(rounds.map(resources)).size};
}

export function identityErrors(rounds) {
  const identity=analyzeIdentity(rounds), errors=[];
  const title='1 + 1 - 1 × 1 / 1';
  for(const group of identity.duplicates){
    const reprise=group.key==='9/11/1' && group.rounds.join(',')==='30,100' &&
      group.rounds.every(n=>rounds.find(r=>r.number===n)?.sample===title);
    if(!reprise)errors.push(`Identical accepted-answer constraints ${group.key}: Rounds ${group.rounds.join(', ')}.`);
  }
  for(const group of identity.resourceRepeats){
    const nearby=[];
    for(let i=0;i<group.rounds.length;i++)for(let j=i+1;j<group.rounds.length;j++){
      const a=group.rounds[i],b=group.rounds[j];
      if(a>15 && b-a<=3)nearby.push(`${a}/${b}`);
    }
    if(nearby.length){
      const search=findEqualityWitness(...group.key.split('/').map(Number));
      if(search.status==='found')errors.push(`Nearby shared equality answer: Rounds ${nearby.join(', ')} (${group.key}).`);
      if(search.status==='limited')errors.push(`Incomplete nearby equality search: Rounds ${nearby.join(', ')} (${group.key}).`);
    }
  }
  return errors;
}

// Bounded, deterministic token search. "found" proves a witness, not a complete
// answer set; only "exhausted" proves absence within this token grammar.
export function findEqualityWitness(slots, sticks, {maxNodes=200000}={}) {
  if (!Number.isInteger(slots) || slots < 1 || slots > 11 ||
      !Number.isInteger(sticks) || sticks < 0 || !Number.isInteger(maxNodes) || maxNodes < 1)
    throw new Error('Expected 1-11 slots, nonnegative integer sticks and a positive node budget');
  let nodes=0, limited=false, found=null;
  const tokens=['1','11','111','=','-','/','+','×','*'];
  const visit=(symbols,cost,hasEquality,previousNumber)=>{
    if(found || limited) return;
    if(++nodes>maxNodes){limited=true;return;}
    const left=slots-symbols.length;
    if(cost+left>sticks || cost+left*3<sticks) return;
    if(left===0){
      if(hasEquality && previousNumber && cost===sticks && evaluateEquality(symbols).valid) found=[...symbols];
      return;
    }
    for(const token of tokens){
      const number=!operatorTokens.has(token);
      if(!number && (!previousNumber || left===1)) continue;
      if(token==='=' && hasEquality) continue;
      if(!hasEquality && left===1) continue;
      symbols.push(token);
      visit(symbols,cost+tokenCosts.get(token),hasEquality||token==='=',number);
      symbols.pop();
      if(found || limited) break;
    }
  };
  visit([],0,false,false);
  return {status:found?'found':limited?'limited':'exhausted',symbols:found,nodes:Math.min(nodes,maxNodes),maxNodes};
}

// Completeness concerns canonical token sequences, not physical stick poses or
// spelling aliases. Hitting either limit conservatively leaves it unproven.
export function enumerateRoundSolutions(round, {maxNodes=200000, maxSolutions=1000}={}) {
  const slots = round.symbols.length, sticks = round.stickCount;
  if (!Number.isInteger(slots) || slots < 1 || slots > 11 ||
      !Number.isInteger(sticks) || sticks < 0 || !Number.isFinite(round.target) ||
      !Number.isInteger(maxNodes) || maxNodes < 1 || maxNodes > 10000000 ||
      !Number.isInteger(maxSolutions) || maxSolutions < 1 || maxSolutions > 100000)
    throw new Error('Expected 1-11 slots, nonnegative integer sticks, finite target, maxNodes 1-10000000 and maxSolutions 1-100000');
  let nodes = 0, limitReason = null;
  const solutions = [], tokens = ['1','11','111','=','-','/','+','×','*'];
  const visit = (symbols, cost, hasEquality, previousNumber) => {
    if (limitReason) return;
    if (nodes === maxNodes) { limitReason = 'node_budget'; return; }
    nodes++;
    const left = slots - symbols.length;
    if (cost + left > sticks || cost + left * 3 < sticks) return;
    if (left === 0) {
      if (previousNumber && acceptsRound(round, symbols)) {
        solutions.push([...symbols]);
        if (solutions.length === maxSolutions) limitReason = 'solution_limit';
      }
      return;
    }
    for (const token of tokens) {
      const number = !operatorTokens.has(token);
      if (!number && (!previousNumber || left === 1)) continue;
      if (token === '=' && hasEquality) continue;
      symbols.push(token);
      visit(symbols, cost + tokenCosts.get(token), hasEquality || token === '=', number);
      symbols.pop();
      if (limitReason) break;
    }
  };
  visit([], 0, false, false);
  return {status:limitReason ? 'limited' : 'exhausted', complete:limitReason === null,
    limitReason, solutions, nodes, maxNodes, maxSolutions,
    domain:'Canonical registered token sequences (x alias normalized to ×), at most one equality, adjacent numerals allowed; Node evaluator, not physical layouts or native input verification.'};
}

export function generateCandidatePool(rounds, {seed=15092026, samples=600000}={}) {
  if (!Number.isInteger(samples) || samples < 1 || samples > 2000000)
    throw new Error('samples must be an integer from 1 to 2000000');
  if (!Number.isInteger(seed) || seed < 0 || seed > 0xffffffff)
    throw new Error('seed must be an unsigned 32-bit integer');
  let state = seed;
  const random = n => {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
    return Math.floor(state / 4294967296 * n);
  };
  const numbers = ['1', '11', '111', '1111'];
  const operators = ['+', '-', '/', '×', '*'];
  const searches = new Map(), pool = new Map();
  sampling: for (let i = 0; i < samples; i++) {
    const terms = 2 + random(4), symbols = [], literals = [], operations = [];
    let splitCount = 0;
    for (let term = 0; term < terms; term++) {
      const value = numbers[random(numbers.length)];
      literals.push(Number(value));
      if (value.length > 3) {
        if (splitCount > 0) continue sampling;
        const left = 1 + random(3);
        symbols.push(value.slice(0, left), value.slice(left));
        splitCount++;
      } else if (value.length > 1 && random(5) === 0 && splitCount === 0) {
        symbols.push('1', value.slice(1));
        splitCount++;
      } else symbols.push(value);
      if (term + 1 < terms) {
        const op = operators[random(operators.length)];
        symbols.push(op);
        operations.push(op);
      }
    }
    if (symbols.some(s => !tokenCosts.has(s))) throw new Error('Unsupported candidate token');
    const slots = symbols.length, sticks = symbols.reduce((sum,s) => sum + tokenCosts.get(s), 0);
    if (slots > 11 || sticks > 18) continue;
    const result = evaluate(symbols), target = result.value;
    if (!result.valid || !Number.isInteger(target) || target < -2 || target > 122) continue;
    const key = `${slots}/${sticks}`, identity = `${key}/${target}`;
    if (!searches.has(key)) searches.set(key, findEqualityWitness(slots, sticks));
    const equality = searches.get(key);
    const existing = rounds.filter(r => r.symbols.length === slots && r.stickCount === sticks);
    if (existing.some(r => r.target === target) || existing.length && equality.status !== 'exhausted') continue;
    let trivial = 0;
    for (let j = 0; j < operations.length; j++) {
      if (['/', '×', '*'].includes(operations[j]) && literals[j + 1] === 1) trivial++;
      if (['×', '*'].includes(operations[j]) && literals[j] === 1) trivial++;
    }
    const repeated = operations.reduce((n,op,j) => n + Number(j > 0 && op === operations[j - 1]), 0);
    const score = 3 * slots + sticks + 5 * trivial + 3 * repeated + 2 * splitCount;
    const candidate = {symbols, sample:symbols.join(' '), slots, sticks, target,
      equality:equality.status, existingResources:existing.map(r => r.number), trivial, repeated, score};
    if (!pool.has(identity) || score < pool.get(identity).score) pool.set(identity, candidate);
  }
  const candidates = [...pool.values()].sort((a,b) => a.score - b.score ||
    (a.sample < b.sample ? -1 : a.sample > b.sample ? 1 : 0));
  const groups = analyzeIdentity(rounds).resourceRepeats.filter(g =>
    findEqualityWitness(...g.key.split('/').map(Number)).status === 'found');
  const replacements = groups.flatMap(g => g.rounds.map(number => {
    const round = rounds.find(r => r.number === number);
    const ranked = candidates.map(c => ({...c, replacementScore:c.score +
      8 * Math.max(0, c.slots - round.symbols.length) + 4 * Math.max(0, c.sticks - round.stickCount) +
      (c.target === round.target ? 0 : 6)})).sort((a,b) => a.replacementScore - b.replacementScore);
    return {round:number, sample:round.sample, target:round.target, slots:round.symbols.length,
      sticks:round.stickCount, candidates:ranked.slice(0,12)};
  }));
  return {seed, samples,
    method:'Bounded deterministic sampling, NOT exhaustive. Integer target -2..122, numeric operands at most 1111 composed only of registered 1/11/111 tokens, 2-5 terms, at most one split operand, at most 11 slots/18 sticks. Scores are workload heuristics, not human fun/difficulty. Candidates are suggestions, never automatic round edits.',
    candidates, replacements};
}

export const tokenCosts = new Map([
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

export const operatorTokens = new Set(["+", "-", "/", "=", "×", "x", "*"]);
export function extractRounds(text) {
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
      number: result.length + 1,
      symbols,
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

export function splitSymbols(sample) {
  return sample.split(/\s+/).filter(Boolean);
}

export function sampleTarget(symbols) {
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

export function solveRound(symbols, target) {
  const equality = evaluateEquality(symbols);
  if (equality.handled) {
    return equality;
  }

  const value = evaluateExact(symbols);
  if (!value.valid) {
    return value;
  }

  const valid = exactMatchesTarget(value.exact, target);
  return valid ? { valid: true, value: value.value } : { valid: false, reason: `expected ${target}, got ${value.value}` };
}

export function evaluateEquality(symbols) {
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

  const left = evaluateExact(symbols.slice(0, equalsIndex));
  const right = evaluateExact(symbols.slice(equalsIndex + 1));
  if (!left.valid) {
    return { handled: true, valid: false, reason: `left side: ${left.reason}` };
  }

  if (!right.valid) {
    return { handled: true, valid: false, reason: `right side: ${right.reason}` };
  }

  const valid = rationalEquals(left.exact, right.exact);
  return valid
    ? { handled: true, valid: true, value: left.value }
    : { handled: true, valid: false, reason: `${left.value} is not ${right.value}` };
}

export function evaluate(symbols) {
  const exact = evaluateExact(symbols);
  if (!exact.valid) return exact;
  return { valid: true, value: exact.value };
}

export function evaluateExact(symbols) {
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

    numbers.push(rationalFromIntegerText(numberBuffer));
    numberBuffer = "";
    operators.push(symbol);
  }

  if (numberBuffer.length === 0) {
    return { valid: false, reason: "operator last" };
  }

  numbers.push(rationalFromIntegerText(numberBuffer));

  for (let i = 0; i < operators.length; ) {
    const op = operators[i];
    if (op !== "*" && op !== "x" && op !== "×" && op !== "/") {
      i += 1;
      continue;
    }

    const left = numbers[i];
    const right = numbers[i + 1];
    if (op === "/" && right.n === 0n) {
      return { valid: false, reason: "cannot divide by zero" };
    }

    numbers[i] = op === "/" ? rationalDiv(left, right) : rationalMul(left, right);
    numbers.splice(i + 1, 1);
    operators.splice(i, 1);
  }

  let value = numbers[0];
  for (let i = 0; i < operators.length; i += 1) {
    const op = operators[i];
    const right = numbers[i + 1];
    if (op === "+") {
      value = rationalAdd(value, right);
    } else if (op === "-") {
      value = rationalSub(value, right);
    } else {
      return { valid: false, reason: `unknown operator ${op}` };
    }
  }

  return { valid: true, value: rationalToNumber(value), exact: value };
}

function rationalFromIntegerText(text) {
  return normalizeRational(BigInt(text), 1n);
}

function normalizeRational(n, d) {
  if (d === 0n) throw new Error('zero rational denominator');
  if (d < 0n) {
    n = -n;
    d = -d;
  }
  const divisor = gcd(absBigInt(n), d);
  return { n: n / divisor, d: d / divisor };
}

function rationalAdd(left, right) {
  return normalizeRational(left.n * right.d + right.n * left.d, left.d * right.d);
}

function rationalSub(left, right) {
  return normalizeRational(left.n * right.d - right.n * left.d, left.d * right.d);
}

function rationalMul(left, right) {
  return normalizeRational(left.n * right.n, left.d * right.d);
}

function rationalDiv(left, right) {
  return normalizeRational(left.n * right.d, left.d * right.n);
}

function rationalEquals(left, right) {
  return left.n === right.n && left.d === right.d;
}

function exactMatchesTarget(value, target) {
  if (!Number.isFinite(target)) return false;
  if (Number.isInteger(target)) return value.n === BigInt(target) * value.d;
  return rationalToNumber(value) === target;
}

function rationalToNumber(value) {
  return Number(value.n) / Number(value.d);
}

function gcd(a, b) {
  while (b !== 0n) {
    const next = a % b;
    a = b;
    b = next;
  }
  return a || 1n;
}

function absBigInt(value) {
  return value < 0n ? -value : value;
}
