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

export function classifySolutionPatterns(symbols) {
  if (!Array.isArray(symbols) || symbols.some(symbol => typeof symbol !== 'string')) {
    throw new Error('Expected an array of token strings');
  }

  const ids = new Set();
  const equals = symbols.indexOf('=');
  const sides = equals >= 0
    ? [symbols.slice(0, equals), symbols.slice(equals + 1)]
    : [symbols];

  if (equals >= 0) {
    const left = normalizePatternSide(sides[0]);
    const right = normalizePatternSide(sides[1]);
    if (left.valid && right.valid) {
      ids.add('constructed-equality');
      if (left.key === right.key) ids.add('same-expression-equality');
      if (left.operands.length === 1 && right.operands.length === 1 &&
          left.operands[0] === right.operands[0]) ids.add('same-number-equality');
    }
  }

  for (const side of sides) {
    const parsed = normalizePatternSide(side);
    if (!parsed.valid) continue;

    for (let i = 0; i < parsed.operators.length; i += 1) {
      const op = parsed.operators[i];
      const left = parsed.operands[i];
      const right = parsed.operands[i + 1];
      if (op === '/' && left === right) ids.add('self-division');
      if (op === '-' && left === right) ids.add('self-subtraction');
      if (op === '/' && right === '1') ids.add('divide-by-one');
      if ((op === '*' || op === '×') && (left === '1' || right === '1')) ids.add('multiply-by-one');
    }
  }

  if (equals < 0) {
    const parsed = normalizePatternSide(symbols);
    if (parsed.valid && parsed.operands.length === 2 && parsed.operators.length === 1) {
      if (parsed.operators[0] === '/' && parsed.operands[0] === parsed.operands[1]) {
        ids.add('pure-self-division');
      }

      if (parsed.operators[0] === '-' && parsed.operands[0] === parsed.operands[1]) {
        ids.add('pure-self-subtraction');
      }
    }
  }

  return [...ids].sort();
}

export function analyzeRoundPatterns(rounds, {maxNodes=200000, maxSolutions=1000}={}) {
  return rounds.map(round => {
    const samplePatterns = classifySolutionPatterns(round.symbols);
    const enumeration = enumerateRoundSolutions(round, {maxNodes, maxSolutions});
    const patternCounts = {};
    const pureShortcutSolutions = [];
    for (const solution of enumeration.solutions) {
      const patterns = classifySolutionPatterns(solution);
      for (const pattern of patterns) {
        patternCounts[pattern] = (patternCounts[pattern] ?? 0) + 1;
      }

      if (patterns.includes('pure-self-division') ||
          patterns.includes('pure-self-subtraction') ||
          patterns.includes('same-expression-equality')) {
        pureShortcutSolutions.push({expression: solution.join(' '), patterns});
      }
    }

    return {
      number: round.number,
      name: round.name,
      sample: round.sample,
      target: round.target,
      slots: round.symbols.length,
      sticks: round.stickCount,
      complete: enumeration.complete,
      limitReason: enumeration.limitReason,
      solutionCount: enumeration.solutions.length,
      samplePatterns,
      patternCounts,
      pureShortcutSolutions,
      dominantPatterns: Object.entries(patternCounts)
        .filter(([, count]) => count > 0)
        .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
        .slice(0, 6)
        .map(([pattern, count]) => ({pattern, count})),
    };
  });
}

export const dominantReviewPatterns = [
  'self-division',
  'divide-by-one',
  'self-subtraction',
  'multiply-by-one',
  'same-expression-equality',
];

export function dominantPatternProfileForRound(round, {
  maxNodes = 200000,
  maxSolutions = 1000,
  patterns = dominantReviewPatterns,
} = {}) {
  if (!Array.isArray(patterns) || patterns.some(pattern => typeof pattern !== 'string')) {
    throw new Error('patterns must be an array of strings');
  }

  const enumeration = enumerateRoundSolutions(round, {maxNodes, maxSolutions});
  const patternCounts = Object.fromEntries(patterns.map(pattern => [pattern, 0]));
  for (const solution of enumeration.solutions) {
    const solutionPatterns = classifySolutionPatterns(solution);
    for (const pattern of patterns) {
      if (solutionPatterns.includes(pattern)) patternCounts[pattern] += 1;
    }
  }

  const rankedPatterns = patterns
    .map(pattern => ({
      pattern,
      count: patternCounts[pattern],
      ratio: enumeration.solutions.length > 0 ? patternCounts[pattern] / enumeration.solutions.length : 0,
    }))
    .filter(row => row.count > 0)
    .sort((left, right) => right.ratio - left.ratio ||
      right.count - left.count || left.pattern.localeCompare(right.pattern));
  const dominant = rankedPatterns[0] ?? {pattern:null, count:0, ratio:0};
  return {
    complete: enumeration.complete,
    limitReason: enumeration.limitReason,
    solutionCount: enumeration.solutions.length,
    nodes: enumeration.nodes,
    maxNodes: enumeration.maxNodes,
    maxSolutions: enumeration.maxSolutions,
    patterns,
    rankedPatterns,
    dominantPattern: dominant.pattern,
    dominantCount: dominant.count,
    dominantRatio: dominant.ratio,
  };
}

export function findDominantPatternCandidates(rounds, {
  roundNumbers = [],
  seed = 15092026,
  samples = 600000,
  maxNodes = 200000,
  maxSolutions = 1000,
  maxResults = 8,
  maxEvaluations = 160,
  candidateBudget = 250,
  maxExtraSlots = 2,
  maxExtraSticks = 4,
  allowFewerSlots = false,
  allowFewerSticks = false,
  minRecommendedSolutions = 8,
  minVisibleDiversity = 2,
  minRecommendedImprovement = 0.05,
  minCombinationScore = 2,
} = {}) {
  if (!Array.isArray(roundNumbers)) throw new Error('roundNumbers must be an array');
  if (roundNumbers.some(number => !Number.isInteger(number) || number < 1 || number > rounds.length) ||
      new Set(roundNumbers).size !== roundNumbers.length) {
    throw new Error('roundNumbers must contain unique existing round numbers');
  }

  for (const [name, value] of Object.entries({
    maxResults,
    maxEvaluations,
    candidateBudget,
    maxExtraSlots,
    maxExtraSticks,
    minRecommendedSolutions,
    minVisibleDiversity,
    minCombinationScore,
  })) {
    if (!Number.isInteger(value) || value < 0) throw new Error(`${name} must be a nonnegative integer`);
  }
  if (!Number.isFinite(minRecommendedImprovement) || minRecommendedImprovement < 0) {
    throw new Error('minRecommendedImprovement must be a nonnegative number');
  }

  const selectedNumbers = roundNumbers.length > 0 ? roundNumbers : highDominantPatternRoundNumbers(rounds, {maxNodes, maxSolutions});
  const pool = generateCandidatePool(rounds, {seed, samples});
  const reports = selectedNumbers.map(number => {
    const round = rounds[number - 1];
    const current = dominantPatternProfileForRound(round, {maxNodes, maxSolutions});
    const evaluated = [];
    let skippedByBudget = 0;

    const nearby = generateNearbyDominantCandidates(round, rounds, {
      candidateBudget,
      maxExtraSlots,
      maxExtraSticks,
      allowFewerSlots,
      allowFewerSticks,
    });
    const mergedCandidates = new Map();
    for (const candidate of [...nearby.candidates, ...pool.candidates]) {
      const key = `${candidate.slots}/${candidate.sticks}/${candidate.target}`;
      const previous = mergedCandidates.get(key);
      if (!previous || candidate.score < previous.score) mergedCandidates.set(key, candidate);
    }
    const candidatePool = [...mergedCandidates.values()].sort((left, right) =>
      dominantCandidateDistance(left, round) - dominantCandidateDistance(right, round) ||
      left.score - right.score || left.sample.localeCompare(right.sample));

    for (const candidate of candidatePool) {
      if (maxEvaluations > 0 && evaluated.length >= maxEvaluations) break;
      if (!allowFewerSlots && candidate.slots < round.symbols.length) {
        skippedByBudget += 1;
        continue;
      }

      if (!allowFewerSticks && candidate.sticks < round.stickCount) {
        skippedByBudget += 1;
        continue;
      }

      if (candidate.slots > round.symbols.length + maxExtraSlots ||
          candidate.sticks > round.stickCount + maxExtraSticks) {
        skippedByBudget += 1;
        continue;
      }

      const candidateRound = {
        number,
        name: `${round.name} candidate`,
        sample: candidate.sample,
        symbols: candidate.symbols,
        stickCount: candidate.sticks,
        target: candidate.target,
      };
      const profile = dominantPatternProfileForRound(candidateRound, {maxNodes, maxSolutions});
      const improvement = current.dominantRatio - profile.dominantRatio;
      const targetDistance = Math.abs(candidate.target - round.target);
      const slotDelta = candidate.slots - round.symbols.length;
      const stickDelta = candidate.sticks - round.stickCount;
      const reviewedResourceCount = candidate.existingResources
        .filter(existingNumber => existingNumber !== number).length;
      const samplePatterns = classifySolutionPatterns(candidate.symbols);
      const sourceSamplePatterns = classifySolutionPatterns(round.symbols);
      const visibleDiversityScore = visibleArithmeticDiversityScore(round.symbols, candidate.symbols);
      const combinationTags = visibleCombinationTags(candidate.symbols);
      const combinationScore = combinationTags.length;
      const analysisNotes = [];
      if (profile.solutionCount < minRecommendedSolutions) {
        analysisNotes.push(`only ${profile.solutionCount} accepted answer(s) found`);
      }
      if (reviewedResourceCount > 0) {
        analysisNotes.push(`shares slot/stick budget with Round(s) ${candidate.existingResources
          .filter(existingNumber => existingNumber !== number).join(', ')}`);
      }
      if (visibleDiversityScore < minVisibleDiversity) {
        analysisNotes.push(`visible arithmetic diversity score ${visibleDiversityScore} is below ${minVisibleDiversity}`);
      }
      if (improvement < minRecommendedImprovement) {
        analysisNotes.push(`dominant-pattern improvement ${roundRatio(improvement)} is below ${minRecommendedImprovement}`);
      }
      if (combinationScore < minCombinationScore) {
        analysisNotes.push(`combination score ${combinationScore} is below ${minCombinationScore}`);
      }
      if (current.dominantPattern && samplePatterns.includes(current.dominantPattern)) {
        analysisNotes.push(`sample visibly keeps dominant pattern ${current.dominantPattern}`);
      }
      evaluated.push({
        sample: candidate.sample,
        target: candidate.target,
        slots: candidate.slots,
        sticks: candidate.sticks,
        score: candidate.score,
        equality: candidate.equality,
        existingResources: candidate.existingResources,
        complete: profile.complete,
        solutionCount: profile.solutionCount,
        dominantPattern: profile.dominantPattern,
        dominantRatio: profile.dominantRatio,
        improvement,
        targetDistance,
        slotDelta,
        stickDelta,
        reviewedResourceCount,
        samplePatterns,
        sourceSamplePatterns,
        visibleDiversityScore,
        combinationTags,
        combinationScore,
        analysisOnly: analysisNotes.length > 0,
        analysisNotes,
        reviewScore:dominantCandidateReviewScore({
          dominantRatio:profile.dominantRatio,
          solutionCount:profile.solutionCount,
          targetDistance,
          slotDelta,
          stickDelta,
          baseScore:candidate.score,
          reviewedResourceCount,
          visibleDiversityScore,
          combinationScore,
          minCombinationScore,
        }),
        rankedPatterns: profile.rankedPatterns.slice(0, 5),
      });
    }

    const candidates = evaluated
      .filter(candidate => candidate.complete && candidate.improvement > 0)
      .sort((left, right) => Number(left.analysisOnly) - Number(right.analysisOnly) ||
        left.reviewScore - right.reviewScore ||
        right.improvement - left.improvement || left.dominantRatio - right.dominantRatio ||
        left.score - right.score ||
        left.sample.localeCompare(right.sample))
      .slice(0, maxResults);

    return {
      number,
      name: round.name,
      sample: round.sample,
      target: round.target,
      slots: round.symbols.length,
      sticks: round.stickCount,
      current,
      search:{
        seed,
        samples,
        evaluated:evaluated.length,
        skippedByBudget,
        maxEvaluations,
        maxResults,
        candidateBudget,
        nearbyCandidateCount:nearby.candidates.length,
        nearbyLimited:nearby.limited,
        nearbyNodes:nearby.nodes,
        maxExtraSlots,
        maxExtraSticks,
        allowFewerSlots,
        allowFewerSticks,
        minRecommendedSolutions,
        minVisibleDiversity,
        minRecommendedImprovement,
        minCombinationScore,
      },
      candidates,
    };
  });

  return {
    seed,
    samples,
    method:'Dominant shortcut candidate search is bounded and heuristic. It evaluates sampled replacement expressions against enumerated accepted answers, then ranks complete candidates that reduce the strongest reusable shortcut-family ratio. Narrow candidates, candidates in already-reviewed resource neighborhoods, visibly similar samples, and samples without enough visible combination cues remain visible as analysis-only rows. It never edits round data automatically and does not prove human difficulty or fun.',
    rounds:reports,
  };
}

export function findEqualityEchoCandidates(rounds, {
  roundNumbers = [],
  maxNodes = 200000,
  maxSolutions = 1000,
  maxResults = 8,
  maxEvaluations = 120,
  maxSideExpressions = 240,
  maxResourceDelta = 1,
  minRecommendedSolutions = 8,
  minRecommendedImprovement = 0.05,
  minCombinationScore = 2,
} = {}) {
  if (!Array.isArray(roundNumbers)) throw new Error('roundNumbers must be an array');
  if (roundNumbers.some(number => !Number.isInteger(number) || number < 1 || number > rounds.length) ||
      new Set(roundNumbers).size !== roundNumbers.length) {
    throw new Error('roundNumbers must contain unique existing round numbers');
  }

  for (const [name, value] of Object.entries({
    maxResults,
    maxEvaluations,
    maxSideExpressions,
    maxResourceDelta,
    minRecommendedSolutions,
    minCombinationScore,
  })) {
    if (!Number.isInteger(value) || value < 0) throw new Error(`${name} must be a nonnegative integer`);
  }
  if (!Number.isFinite(minRecommendedImprovement) || minRecommendedImprovement < 0) {
    throw new Error('minRecommendedImprovement must be a nonnegative number');
  }

  const selectedNumbers = roundNumbers.length > 0 ? roundNumbers : highEqualityEchoRoundNumbers(rounds, {maxNodes, maxSolutions});
  const occupied = new Set(rounds.map(existing =>
    `${existing.symbols.length}/${existing.stickCount}/${existing.target}`));
  const reports = selectedNumbers.map(number => {
    const round = rounds[number - 1];
    const current = dominantPatternProfileForRound(round, {
      maxNodes,
      maxSolutions,
      patterns:['same-expression-equality'],
    });
    const resources = nearbyEqualityResources(round, {maxResourceDelta});
    const evaluated = [];
    let skippedByBudget = 0;
    let sideNodes = 0;
    const sideCache = new Map();

    for (const resource of resources) {
      if (maxEvaluations > 0 && evaluated.length >= maxEvaluations) break;
      const sideResources = splitEqualityResource(resource.slots, resource.sticks);
      for (const pairResource of sideResources) {
        if (maxEvaluations > 0 && evaluated.length >= maxEvaluations) break;
        const leftSides = cachedSideExpressions(pairResource.leftSlots, pairResource.leftSticks);
        const rightSides = cachedSideExpressions(pairResource.rightSlots, pairResource.rightSticks);
        const rightByValue = new Map();
        for (const side of rightSides.sides) {
          if (!rightByValue.has(side.exactKey)) rightByValue.set(side.exactKey, []);
          rightByValue.get(side.exactKey).push(side);
        }

        sideNodes += leftSides.nodes + rightSides.nodes;
        for (const left of leftSides.sides) {
          if (maxEvaluations > 0 && evaluated.length >= maxEvaluations) break;
          const matches = rightByValue.get(left.exactKey) ?? [];
          for (const right of matches) {
            if (maxEvaluations > 0 && evaluated.length >= maxEvaluations) break;
            if (left.patternKey === right.patternKey) {
              skippedByBudget += 1;
              continue;
            }

            const symbols = [...left.symbols, '=', ...right.symbols];
            let target;
            try {
              target = sampleTarget(symbols);
            } catch {
              skippedByBudget += 1;
              continue;
            }

            if (!Number.isInteger(target) || target < -2 || target > 122) {
              skippedByBudget += 1;
              continue;
            }

            const identity = `${symbols.length}/${symbolStickCount(symbols)}/${target}`;
            if (occupied.has(identity)) {
              skippedByBudget += 1;
              continue;
            }

            const candidate = equalityCandidateReport({
              number,
              round,
              rounds,
              symbols,
              target,
              current,
              maxNodes,
              maxSolutions,
              minRecommendedSolutions,
              minRecommendedImprovement,
              minCombinationScore,
            });
            evaluated.push(candidate);
          }
        }
      }
    }

    function cachedSideExpressions(slots, sticks) {
      const key = `${slots}/${sticks}`;
      if (!sideCache.has(key)) {
        sideCache.set(key, generateSideExpressions(slots, sticks, {limit:maxSideExpressions}));
      }

      return sideCache.get(key);
    }

    const candidates = evaluated
      .filter(candidate => candidate.complete && candidate.improvement > 0)
      .sort((left, right) => Number(left.analysisOnly) - Number(right.analysisOnly) ||
        left.reviewScore - right.reviewScore ||
        right.improvement - left.improvement ||
        left.sameExpressionRatio - right.sameExpressionRatio ||
        left.sample.localeCompare(right.sample))
      .slice(0, maxResults);

    return {
      number,
      name: round.name,
      sample: round.sample,
      target: round.target,
      slots: round.symbols.length,
      sticks: round.stickCount,
      current,
      search:{
        evaluated:evaluated.length,
        skippedByBudget,
        maxEvaluations,
        maxResults,
        maxSideExpressions,
        maxResourceDelta,
        sideNodes,
        resourceCount:resources.length,
        minRecommendedSolutions,
        minRecommendedImprovement,
        minCombinationScore,
      },
      candidates,
    };
  });

  return {
    method:'Equality-echo candidate search is bounded and heuristic. It builds equality candidates by pairing exact-equal left/right side expressions and keeps non-echo samples visible first. It is designed for same-expression equality review rows, not full puzzle difficulty proof.',
    rounds:reports,
  };
}

function equalityCandidateReport({
  number,
  round,
  rounds,
  symbols,
  target,
  current,
  maxNodes,
  maxSolutions,
  minRecommendedSolutions,
  minRecommendedImprovement,
  minCombinationScore,
}) {
  const candidateRound = {
    number,
    name: `${round.name} equality candidate`,
    sample: symbols.join(' '),
    symbols,
    stickCount: symbolStickCount(symbols),
    target,
  };
  const profile = dominantPatternProfileForRound(candidateRound, {
    maxNodes,
    maxSolutions,
    patterns:['same-expression-equality'],
  });
  const improvement = current.dominantRatio - profile.dominantRatio;
  const reviewedResourceCount = rounds
    .filter(existing => existing.number !== number &&
      existing.symbols.length === symbols.length &&
      existing.stickCount === candidateRound.stickCount)
    .length;
  const combinationTags = visibleCombinationTags(symbols);
  const combinationScore = combinationTags.length;
  const samplePatterns = classifySolutionPatterns(symbols);
  const analysisNotes = [];
  if (profile.solutionCount < minRecommendedSolutions) {
    analysisNotes.push(`only ${profile.solutionCount} accepted answer(s) found`);
  }
  if (reviewedResourceCount > 0) {
    analysisNotes.push(`shares slot/stick budget with ${reviewedResourceCount} existing round(s)`);
  }
  if (improvement < minRecommendedImprovement) {
    analysisNotes.push(`same-expression improvement ${roundRatio(improvement)} is below ${minRecommendedImprovement}`);
  }
  if (combinationScore < minCombinationScore) {
    analysisNotes.push(`combination score ${combinationScore} is below ${minCombinationScore}`);
  }
  if (samplePatterns.includes('same-expression-equality')) {
    analysisNotes.push('sample is still a same-expression equality');
  }

  const sameExpression = profile.rankedPatterns.find(row => row.pattern === 'same-expression-equality');
  const sameExpressionRatio = sameExpression?.ratio ?? 0;
  return {
    sample:candidateRound.sample,
    target,
    slots:symbols.length,
    sticks:candidateRound.stickCount,
    complete:profile.complete,
    solutionCount:profile.solutionCount,
    sameExpressionRatio,
    improvement,
    reviewedResourceCount,
    samplePatterns,
    combinationTags,
    combinationScore,
    analysisOnly:analysisNotes.length > 0,
    analysisNotes,
    reviewScore:Math.round((sameExpressionRatio * 120 +
      Math.max(0, 8 - profile.solutionCount) * 8 +
      Math.max(0, minCombinationScore - combinationScore) * 10 +
      reviewedResourceCount * 12) * 1000) / 1000,
    rankedPatterns:profile.rankedPatterns,
  };
}

function nearbyEqualityResources(round, {maxResourceDelta}) {
  const resources = [];
  for (let slots = Math.max(3, round.symbols.length - maxResourceDelta);
       slots <= Math.min(11, round.symbols.length + maxResourceDelta); slots += 1) {
    if (slots % 2 === 0) continue;
    for (let sticks = Math.max(3, round.stickCount - maxResourceDelta * 2);
         sticks <= round.stickCount + maxResourceDelta * 2; sticks += 1) {
      if (sticks < slots || sticks > slots * 3) continue;
      resources.push({slots, sticks});
    }
  }

  return resources.sort((left, right) =>
    18 * Math.abs(left.slots - round.symbols.length) + Math.abs(left.sticks - round.stickCount) -
    (18 * Math.abs(right.slots - round.symbols.length) + Math.abs(right.sticks - round.stickCount)));
}

function splitEqualityResource(slots, sticks) {
  const sideSlots = slots - 1;
  const sideSticks = sticks - tokenCosts.get('=');
  const resources = [];
  for (let leftSlots = 1; leftSlots < sideSlots; leftSlots += 1) {
    const rightSlots = sideSlots - leftSlots;
    for (let leftSticks = leftSlots; leftSticks <= leftSlots * 3; leftSticks += 1) {
      const rightSticks = sideSticks - leftSticks;
      if (rightSticks < rightSlots || rightSticks > rightSlots * 3) continue;
      resources.push({leftSlots, leftSticks, rightSlots, rightSticks});
    }
  }

  return resources.sort((left, right) =>
    Math.abs(left.leftSlots - left.rightSlots) + Math.abs(left.leftSticks - left.rightSticks) -
    (Math.abs(right.leftSlots - right.rightSlots) + Math.abs(right.leftSticks - right.rightSticks)));
}

function generateSideExpressions(slots, sticks, {limit}) {
  const tokens = ['1','11','111','-','/','+','×','*'];
  const sides = [];
  let nodes = 0;
  let limited = false;
  const seen = new Set();

  const visit = (symbols, cost, previousNumber) => {
    if (limited) return;
    nodes += 1;
    const left = slots - symbols.length;
    if (cost + left > sticks || cost + left * 3 < sticks) return;
    if (left === 0) {
      if (!previousNumber || cost !== sticks) return;
      const evaluation = evaluateExact(symbols);
      if (!evaluation.valid) return;
      const normalized = normalizePatternSide(symbols);
      if (!normalized.valid) return;
      const exactKey = `${evaluation.exact.n}/${evaluation.exact.d}`;
      const sample = symbols.join(' ');
      if (seen.has(sample)) return;
      seen.add(sample);
      sides.push({symbols:[...symbols], sample, exactKey, patternKey:normalized.key});
      if (limit > 0 && sides.length >= limit) limited = true;
      return;
    }

    for (const token of tokens) {
      const number = !operatorTokens.has(token);
      if (!number && (!previousNumber || left === 1)) continue;
      symbols.push(token);
      visit(symbols, cost + tokenCosts.get(token), number);
      symbols.pop();
      if (limited) break;
    }
  };

  visit([], 0, false);
  return {sides, nodes, limited};
}

function dominantCandidateReviewScore({
  dominantRatio,
  solutionCount,
  targetDistance,
  slotDelta,
  stickDelta,
  baseScore,
  reviewedResourceCount,
  visibleDiversityScore,
  combinationScore,
  minCombinationScore,
}) {
  const scarcityPenalty = Math.max(0, 8 - solutionCount) * 8;
  const targetPenalty = Math.min(40, targetDistance) * 1.5;
  const resourcePenalty = Math.abs(slotDelta) * 5 + Math.abs(stickDelta) * 3;
  const reviewedResourcePenalty = reviewedResourceCount * 12;
  const visibleSimilarityPenalty = Math.max(0, 4 - visibleDiversityScore) * 8;
  const combinationPenalty = Math.max(0, minCombinationScore - combinationScore) * 10;
  return Math.round((dominantRatio * 120 + scarcityPenalty + targetPenalty +
    resourcePenalty + reviewedResourcePenalty + visibleSimilarityPenalty +
    combinationPenalty + baseScore * 0.1) * 1000) / 1000;
}

function visibleArithmeticDiversityScore(leftSymbols, rightSymbols) {
  const leftOperators = operatorProfile(leftSymbols);
  const rightOperators = operatorProfile(rightSymbols);
  const operators = new Set([...Object.keys(leftOperators.counts), ...Object.keys(rightOperators.counts)]);
  let operatorDistance = 0;
  for (const operator of operators) {
    operatorDistance += Math.abs((leftOperators.counts[operator] ?? 0) - (rightOperators.counts[operator] ?? 0));
  }

  const leftPatterns = new Set(classifySolutionPatterns(leftSymbols));
  const rightPatterns = new Set(classifySolutionPatterns(rightSymbols));
  const patterns = new Set([...leftPatterns, ...rightPatterns]);
  let patternDistance = 0;
  for (const pattern of patterns) {
    if (leftPatterns.has(pattern) !== rightPatterns.has(pattern)) patternDistance += 1;
  }

  const sequenceDistance = leftOperators.sequence === rightOperators.sequence ? 0 : 1;
  return operatorDistance + patternDistance * 2 + sequenceDistance;
}

function operatorProfile(symbols) {
  const sequence = symbols.filter(symbol => operatorTokens.has(symbol)).join(' ');
  const counts = {};
  for (const symbol of symbols) {
    if (operatorTokens.has(symbol)) counts[symbol] = (counts[symbol] ?? 0) + 1;
  }

  return {sequence, counts};
}

function visibleCombinationTags(symbols) {
  const tags = new Set();
  const sides = splitEqualitySides(symbols);
  for (const side of sides) {
    const parsed = normalizePatternSide(side);
    if (!parsed.valid) continue;
    if (parsed.operands.some(operand => operand.length >= 2)) tags.add('packed-number');
    if (parsed.operands.some(operand => operand.length >= 3)) tags.add('triple-number');
    const distinctOperators = new Set(parsed.operators);
    if (distinctOperators.size >= 2) tags.add('operator-mix');
    for (let i = 0; i < parsed.operators.length; i += 1) {
      const operator = parsed.operators[i];
      const left = parsed.operands[i];
      const right = parsed.operands[i + 1];
      if (operator === '/' && left === right && left !== '1') tags.add('nontrivial-self-division');
      if (operator === '-' && left === right && left !== '1') tags.add('nontrivial-self-subtraction');
      if (operator === '/' && right !== '1' && left !== right) tags.add('nontrivial-division');
      if ((operator === '×' || operator === '*') && left !== '1' && right !== '1') tags.add('nontrivial-multiply');
    }
  }

  const equalityIndex = symbols.indexOf('=');
  if (equalityIndex >= 0) {
    const left = normalizePatternSide(symbols.slice(0, equalityIndex));
    const right = normalizePatternSide(symbols.slice(equalityIndex + 1));
    if (left.valid && right.valid && left.key !== right.key) tags.add('non-echo-equality');
  }

  return [...tags].sort();
}

function splitEqualitySides(symbols) {
  const equalityIndex = symbols.indexOf('=');
  return equalityIndex >= 0
    ? [symbols.slice(0, equalityIndex), symbols.slice(equalityIndex + 1)]
    : [symbols];
}

function symbolStickCount(symbols) {
  return symbols.reduce((sum, symbol) => sum + tokenCosts.get(symbol), 0);
}

function roundRatio(value) {
  return Math.round(value * 1000) / 1000;
}

function generateNearbyDominantCandidates(round, rounds, {
  candidateBudget,
  maxExtraSlots,
  maxExtraSticks,
  allowFewerSlots,
  allowFewerSticks,
}) {
  const minSlots = allowFewerSlots ? Math.max(1, round.symbols.length - maxExtraSlots) : round.symbols.length;
  const maxSlots = Math.min(11, round.symbols.length + maxExtraSlots);
  const minSticks = allowFewerSticks ? Math.max(1, round.stickCount - maxExtraSticks) : round.stickCount;
  const maxSticks = round.stickCount + maxExtraSticks;
  const occupied = new Set(rounds.map(existing =>
    `${existing.symbols.length}/${existing.stickCount}/${existing.target}`));
  const resources = [];
  for (let slots = minSlots; slots <= maxSlots; slots += 1) {
    for (let sticks = minSticks; sticks <= maxSticks; sticks += 1) {
      if (sticks < slots || sticks > slots * 3) continue;
      resources.push({slots, sticks});
    }
  }
  resources.sort((left, right) =>
    18 * Math.abs(left.slots - round.symbols.length) + 9 * Math.abs(left.sticks - round.stickCount) -
    (18 * Math.abs(right.slots - round.symbols.length) + 9 * Math.abs(right.sticks - round.stickCount)));

  const tokens = ['1','11','111','=','-','/','+','×','*'];
  const candidates = new Map();
  const equalityCache = new Map();
  let nodes = 0;
  let limited = false;

  const addCandidate = symbols => {
    let target;
    try {
      target = sampleTarget(symbols);
    } catch {
      return;
    }

    if (!Number.isInteger(target) || target < -2 || target > 122) return;
    const slots = symbols.length;
    const sticks = symbols.reduce((sum, symbol) => sum + tokenCosts.get(symbol), 0);
    const identity = `${slots}/${sticks}/${target}`;
    if (occupied.has(identity)) return;
    const existingResources = rounds
      .filter(existing => existing.symbols.length === slots && existing.stickCount === sticks)
      .map(existing => existing.number);
    const resourceKey = `${slots}/${sticks}`;
    if (!equalityCache.has(resourceKey)) equalityCache.set(resourceKey, findEqualityWitness(slots, sticks));
    const equality = equalityCache.get(resourceKey);
    if (existingResources.length > 0 && equality.status === 'found') return;
    const sample = symbols.join(' ');
    const operations = symbols.filter(symbol => operatorTokens.has(symbol) && symbol !== '=');
    let trivial = 0;
    for (let index = 0; index < symbols.length; index += 1) {
      const op = symbols[index];
      if (!['/', '×', '*'].includes(op)) continue;
      const left = symbols[index - 1];
      const right = symbols[index + 1];
      if (op === '/' && right === '1') trivial += 1;
      if ((op === '×' || op === '*') && (left === '1' || right === '1')) trivial += 1;
    }
    const repeated = operations.reduce((count, op, index) =>
      count + Number(index > 0 && operations[index - 1] === op), 0);
    const score = 3 * slots + sticks + 5 * trivial + 3 * repeated +
      2 * Math.min(30, Math.abs(target - round.target));
    const candidate = {symbols:[...symbols], sample, slots, sticks, target,
      equality:equality.status, existingResources,
      trivial, repeated, score, source:'nearby-resource-enumeration'};
    const previous = candidates.get(identity);
    if (!previous || candidate.score < previous.score) candidates.set(identity, candidate);
  };

  const visit = (slots, sticks, symbols, cost, hasEquality, previousNumber) => {
    if (limited) return;
    if (candidateBudget > 0 && candidates.size >= candidateBudget) {
      limited = true;
      return;
    }

    nodes += 1;
    const left = slots - symbols.length;
    if (cost + left > sticks || cost + left * 3 < sticks) return;
    if (left === 0) {
      if (previousNumber && cost === sticks) addCandidate(symbols);
      return;
    }

    for (const token of tokens) {
      const number = !operatorTokens.has(token);
      if (!number && (!previousNumber || left === 1)) continue;
      if (token === '=' && hasEquality) continue;
      symbols.push(token);
      visit(slots, sticks, symbols, cost + tokenCosts.get(token), hasEquality || token === '=', number);
      symbols.pop();
      if (limited) break;
    }
  };

  for (const resource of resources) {
    visit(resource.slots, resource.sticks, [], 0, false, false);
    if (limited) break;
  }

  return {limited, nodes, candidates:[...candidates.values()]};
}

function dominantCandidateDistance(candidate, round) {
  return 18 * Math.abs(candidate.slots - round.symbols.length) +
    9 * Math.abs(candidate.sticks - round.stickCount) +
    2 * Math.min(30, Math.abs(candidate.target - round.target)) +
    (candidate.target === round.target ? 0 : 7);
}

function highDominantPatternRoundNumbers(rounds, {maxNodes, maxSolutions}) {
  return analyzeRoundPatterns(rounds, {maxNodes, maxSolutions})
    .flatMap(row => {
      if (row.number <= 50 || row.solutionCount < 20) return [];
      return dominantReviewPatterns.map(pattern => {
        const count = row.patternCounts[pattern] ?? 0;
        return {
          number:row.number,
          pattern,
          count,
          solutionCount:row.solutionCount,
          ratio:row.solutionCount > 0 ? count / row.solutionCount : 0,
        };
      });
    })
    .filter(row => row.count > 0 && row.ratio >= 0.6)
    .sort((left, right) => right.ratio - left.ratio ||
      right.count - left.count || left.number - right.number ||
      left.pattern.localeCompare(right.pattern))
    .map(row => row.number)
    .filter((number, index, numbers) => numbers.indexOf(number) === index);
}

function normalizePatternSide(symbols) {
  const operands = [];
  const operators = [];
  let buffer = '';

  for (const symbol of symbols) {
    const token = symbol === 'x' ? '×' : symbol;
    if (!tokenCosts.has(token) || token === '=') return {valid:false, key:'', operands:[], operators:[]};
    if (!operatorTokens.has(token)) {
      buffer += token;
      continue;
    }

    if (buffer.length === 0) return {valid:false, key:'', operands:[], operators:[]};
    operands.push(buffer);
    buffer = '';
    operators.push(token);
  }

  if (buffer.length === 0) return {valid:false, key:'', operands:[], operators:[]};
  operands.push(buffer);
  const key = operands.map((operand, index) =>
    index === 0 ? operand : `${operators[index - 1]}${operand}`).join('');
  return {valid:true, key, operands, operators};
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
