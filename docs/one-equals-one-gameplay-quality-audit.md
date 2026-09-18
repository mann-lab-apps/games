# 1 = 1 Gameplay Quality Audit

## Universal Shortcut Pattern Map (2026-09-18)

Added a reusable solution-pattern classifier to the Node round-identity mirror.
`report-one-plus-one-minus-one-round-quality.mjs --patterns` now labels accepted
canonical solutions with broad shortcut families such as `pure-self-division`
(`N / N`), `self-division`, `pure-self-subtraction`, `same-expression-equality`,
`same-number-equality`, `multiply-by-one` and `divide-by-one`. This is a
design-review map, not a human difficulty proof.

The first focused policy is `N / N = 1`: Rounds 1-30 may intentionally teach or
echo the pattern, but later pure `N / N` acceptance should be reviewed unless it
is deliberately combined with another required idea. Initial complete
enumeration found pure self-division answers in:

- Learning/early window: 4, 9, 12, 25, 26, 27, 28, 30.
- Later review candidates: 52, 59, 65, 100.

This supports the player's observation that `N / N` can become a universal key
for target-1 rounds. The redesign should not ban `/` or force a single sample.
Instead, late affected rounds should require a combined pattern or a distinct
resource judgment, then rerun `--patterns --strict-patterns` to confirm late
pure `N / N` no longer passes the stricter design gate.

The stricter policy is intentionally separate from the regular static `--strict`
gate. It is a redesign target, not a release-blocking code failure while the
affected rounds are still being actively reshaped.

Follow-up redesign removed standalone `N / N` keys from all four late rounds
without token bans:

| Round | Previous sample | Current sample | Reason |
| --- | --- | --- | --- |
| 52 | `1 11 / 111` | `1 1 1 - 1` | Replaces pure self-division with adjacent-number construction and subtraction (`111 - 1 = 110`). |
| 59 | `1 * 1 = 1 × 1` | `111 * 11 - 11 11` | Replaces a wide target-1 equality shell with high-value multiplication/subtraction. |
| 65 | `111 / 111 = 1 × 1` | `1 / 1 1 - 1 / 11` | Replaces direct self-division equality with fractional cancellation. |
| 100 | `1 + 1 - 1 × 1 / 1` | `11 × 11 - 111 + 1` | Replaces the title callback duplicate with a finale that uses the `11 × 11 - 111` ten-making trick and closes at target 11. |

After these edits, `--patterns --strict-patterns` reports no late pure
self-division violations. Round 100 is no longer an identical resource/target
callback to Round 30; the regular strict report now has zero identical
resource/target groups.

Verification: `node --test scripts/test-one-plus-one-minus-one-round-identity.mjs`
passes 49/49; `node scripts/verify-one-plus-one-minus-one-rounds.mjs` passes;
`node scripts/report-one-plus-one-minus-one-round-quality.mjs --strict` passes.
`node scripts/report-one-plus-one-minus-one-round-quality.mjs --patterns
--strict-patterns --max-nodes 200000 --max-solutions 1000` passes.
Full static verification also passes after restoring the store-readiness
strict-readiness anchor; it still reports the expected stale-WebGL and external
release/device warnings. Unity licensing remains blocked by
`LICENSING_CLIENT_UNAVAILABLE`, so this is source/static evidence rather than a
fresh player-build runtime pass.

Follow-up map: the same `--patterns` report now also surfaces late
same-expression/same-number equality echo candidates without failing the strict
gate. These legal answers are not token bans or bugs, but they are the next
round-design review list because they can bypass a round's intended resource
judgment. The first equality-echo cleanup changed three sample-first echo
rounds without adding shared equality witnesses:

| Round | Previous sample | Current sample | Reason |
| --- | --- | --- | --- |
| 53 | `111 = 111` | `111 / 1` | Removes a one-solution same-number equality and keeps a compact high-number check. |
| 61 | `1 + 1 + 1 = 1 + 1 + 1` | `11 / 11 + 1 + 1` | Removes a copied expression equality while keeping target 3. |
| 78 | `111 - 11 - 1 = 111 - 11 - 1` | `111 × 111 - 111 1 × 11` | Replaces copied-expression equality with adjacent-number multiplication contrast. |

Second cleanup removed the remaining sample-level equality echoes:

| Round | Previous sample | Current sample | Reason |
| --- | --- | --- | --- |
| 56 | `11 + 11 = 1 1 + 11` | `11 * 11 / 11 + 1 1` | Removes an equivalent `11 + 11` equality while keeping a square/division path to 22. |
| 69 | `111 = 1 11` | `1 11 * 1` | Removes same-number equality while preserving the tucked adjacent-number idea. |

Current sample-level late equality echoes: none. Remaining alternate-only
review rows are 54, 55, 56, 57, 58, 60, 63, 64, 68, 72, 73, 79, 87, 88, 93
and 97.

The report now ranks alternate-only equality echoes by same-expression share.
At the current 200k-node/1000-solution budget, high-priority rows are 97
(`sameExpressionRatio` about 0.62) and 64 (about 0.53). A first hand search for
64/97 replacements lowered the ratio but introduced new shared equality
witnesses in candidate resource groups, so those edits were not applied. A
broader deterministic candidate search over six seeds and up to 2,000,000
samples per seed found conflict-free target-100 candidates for 64 and
target-121 candidates for 97, but none improved the same-expression ratio:
64 stayed around 0.53 or worsened, and 97 stayed around 0.62 or worsened.
Next redesign should avoid trading one shortcut family for broader equality
reuse, and should only edit 64/97 if a candidate improves both the ratio and
the shared-witness map.

Tooling note: use
`node scripts/report-one-plus-one-minus-one-round-quality.mjs --patterns
--summary --strict-patterns --max-nodes 200000 --max-solutions 1000` for the
compact next-action map. It preserves high-priority pattern rows and strict
violations without printing the full per-round solution-pattern JSON.

The compact map now also includes `dominantPatternReview`, which ranks late
rounds where one shortcut family accounts for a large share of the enumerated
accepted answers. The current high-priority arithmetic families are not strict
failures, but they identify the next play-design review targets: Round 91
(`divide-by-one`/`self-division`), 61 (`multiply-by-one`), 73
(`divide-by-one`/`self-division`), 71 (`divide-by-one`, `self-subtraction`,
`self-division`), 98 (`multiply-by-one`), 96 (`divide-by-one`), 65
(`divide-by-one`), 63 (`divide-by-one`) and the existing 97
same-expression-equality concern. This deliberately broadens the map beyond
pure `N / N` without banning tokens or rejecting valid alternate answers.

## Numerical Correctness And Round Identity Pass (2026-09-16)

Fixed the zero-target numerical correctness bug by moving solve/equality
acceptance to exact rational arithmetic in both Unity rules and the shared Node
identity mirror. `Evaluate()` still exposes a double for existing display and
telemetry callers, but `IsRoundSolved` and direct equality now compare reduced
fractions. `1 / 11 111` in Round 48 is rejected as a nonzero value; fractional
cancellation such as `1 / 11 * 11` and equal fraction equations still pass.
Display formatting now shows small nonzero values such as `0.00009` instead of
rounding them to `0`. The controller failure message also reuses the exact
solve reason when a valid expression misses the target, so a future tiny
nonzero miss can say `Makes 1/111111111.` instead of falling back through the
legacy double formatter and saying `Makes 0.`.

Node regressions were changed from characterizing the old tolerance bug to
requiring exact rejection. Round 48's complete canonical solution count changed
from 6 to 4 under the corrected evaluator; the two removed arrays are the
previous tolerance-admitted division answers. Static 100-round verification and
selected solution enumeration pass under the corrected Node mirror.

Twelve non-callback shared-answer pairs were redesigned without token bans:

| Round | Previous sample | Current sample | Reason |
| --- | --- | --- | --- |
| 76 | `111 / 111 + 1 = 1 + 1` | `111 - 111 + 11 / 11 + 1` | Moves out of the 9-slot/16-stick group shared with Round 64 while keeping a target-2 cancellation/division idea. |
| 95 | `1 + 1 + 11 * 1` | `1 + 1 1 × 11 / 11 + 1` | Moves out of the 7-slot/12-stick group shared with Round 22 and keeps a precedence/adjacent-number late-round pattern. |
| 96 | `111 - 1 - 1 - 1` | `111 - 1 - 1 - 1 / 1 - 1` | Moves out of the 7-slot/9-stick group shared with Round 25 using a longer final-band subtraction/division expression. |
| 72 | `111 - 1 = 11 1 - 1` | `1 1 × 11 - 11 + 11 - 11` | Moves out of the 8-slot/12-stick group shared with Round 29 while preserving a target-110 adjacent-number calculation. |
| 73 | `1 + 1 - 1 = 1 × 1` | `1 / 1 1 - 1 / 11 + 1` | Replaces a repeated equality shell with exact fractional cancellation. |
| 77 | `11 + 1 - 1 = 1 1` | `1 / 1 1 - 1 / 11 + 11` | Moves out of the 8-slot/11-stick group shared with Round 31 using a related target-11 fractional cancellation. |
| 93 | `11 1 - 11 + 11` | `1 11 + 1 - 1 - 11 / 11` | Moves out of the 6-slot/10-stick group shared with Round 42 while keeping split triple-one arithmetic. |
| 71 | `1 - 1 = 11 - 1 1` | `1 - 1 / 1 - 11 / 1 1` | Replaces a widely reusable zero equality with a unique-resource negative target. |
| 92 | `1 + 11 × 11` | `11 + 111 / 111 * 111` | Moves out of the 5-slot/9-stick group shared with Round 15 while keeping target 122. |
| 99 | `111 - 11 - 1 × 1` | `111 - 11 - 1 / 1 + 1 - 1` | Moves out of the 7-slot/11-stick group shared with Round 59 while preserving target 99. |
| 26 | `1 + 1 - 1 / 1` | `1 1 1 / 1 1 1` | Moves out of the 7-slot/8-stick group shared with Round 13 while keeping a readable divide-to-one idea. |
| 94 | `111 / 111 + 11 - 1` | `11 - 1 / 111 * 1 11 + 1` | Moves out of the 7-slot/13-stick group shared with Round 65 using exact fractional cancellation. |

Fresh Node identity summary after these edits: only the intentional identical
resource/target callback 30/100 remains; unique resource pairs increased from
60 to 72. Proven equality-sharing pairs dropped from 14 to 2. The remaining
shared pairs are intentional: the tutorial echo 2/5 and title callback 30/100.
All 12 previously open non-callback pairs are now separated by resource and/or
complete canonical-set overlap under the corrected evaluator. These are
token-array counts and witness searches, not human strategy counts.

Verification completed: `node --test scripts/test-one-plus-one-minus-one-round-identity.mjs`
passes 49/49; `node scripts/verify-one-plus-one-minus-one-rounds.mjs` passes;
selected `--solutions 11,33,48,83 --strict` passes and reports Round 48 with 4
solutions. Full static verification passes, with the expected stale WebGL
warning because the player build is older than these source edits. After
clearing a stale Unity licensing child process, latest Unity EditMode passes
61/61 for the corrected numeric code and all round-data edits. PlayMode still
could not reach game tests because Unity batchmode licensing timed out while
waiting for `Unity-LicenseClient-jaemankim-6000.3.23`. The WebGL build scripts
and native readiness scripts now distinguish an installed local entitlement
from an unavailable Unity licensing service through the shared
`scripts/lib-one-plus-one-minus-one-unity-license.sh` helper: empty CLI data can
fall back to `UnityEntitlementLicense.xml`, but
`LICENSING_CLIENT_UNAVAILABLE` fails fast. PlayMode, QA WebGL, ordinary WebGL,
store-capture WebGL, iOS readiness and Android readiness all use the same guard
instead of launching Unity into the known licensing timeout. Current
Unity-gated verification exits with
`Unity licensing client is unavailable. Open Unity Hub or repair its licensing
service before running this script.` No failing game test result was produced.
Use `scripts/check-one-plus-one-minus-one-unity-license.sh` as the first retry
step before rerunning Unity PlayMode/WebGL verification.
Next required evidence after Unity Hub licensing is restored: PlayMode, fresh
WebGL build and browser input check that Round 48 rejects `1 / 11 111`.
The ship-ready gate now performs this license preflight once and skips
Unity-gated PlayMode/WebGL/native readiness checks when it fails, so a blocked
run does not waste time repeating identical Unity launches. Current ship-ready
still fails because runtime builds are stale/blocked and release/device inputs
remain incomplete. Existing WebGL viewport smoke passes outside the sandbox;
iPhone SE and desktop startup captures show no obvious clipping or overlap, but
that older player is not evidence for the latest numeric fix.
Release-safety static coverage was extended after this numeric pass to guard
iOS/Android native build entrypoints as well as controller QA hooks, so explicit
AdMob-test paths remain separated from production release exports.

## Batch 4 Runtime Recovery (2026-09-15)

User-requested retry succeeded through the same reviewed PlayMode script.
Approval-service blockage below is historical and resolved for this run.
PlayMode: 37/37. Fresh QA, ordinary and store-capture WebGL builds all succeed;
hashes/timestamps are recorded in `batch4-builds.json`. No additional gameplay
or round-data changes were made. Build-generated scene IDs/order and settings
whitespace were removed after inspecting the diff; unrelated edits preserved.

Actual screenshot-guided CDP touch passes 12 samples/neighbors at 390x844:
10/11/12, 32/33/34, 47/48/49 and 82/83/84. At 320x568, sample 11 and alternate
arrays in 33/48/83 also clear. Bank/slot counts are checked before placing;
the clear event must contain the intended expression. No sample-fill hook is
used. QA round/unlock flags seed these targeted tests; they are not new-player
discovery or physical-device testing. Placed captures for 11/33/48/83 were
inspected, including small-screen packing of triple-one and target placement.

Ordinary-build first-ten input regression passes all ten rounds, touch cancel
and secondary release after synthetic focus loss. Reload preserves unlocked
Round 11; restored round selection shows 1-10 Done, 11 Now, 12 Locked and
Page 1/9. The restored screenshot was inspected. Five ordinary startup
viewports pass (SE, standard/large iPhone, Android 20:9, desktop); SE/desktop
captures were inspected. This is browser evidence, not real-device or native
ad/Crashlytics signoff. WebGL ad opportunity assertions remain unchanged.

The zero-target correctness risk is now REPRODUCED IN WEBGL, not merely a
Node/source finding: actual touch builds `1 / 11 111` in Round 48; telemetry
emits `round_clear ... result=0, expression=1 / 11 111` and advances to 49.
The harness's PASS here means successful bug reproduction, NOT a correct
mathematical answer or a fixed defect. 1/11111 remains nonzero. Numerical
correction with exact-fraction/cancellation regressions is the next priority;
do not hide the issue by banning division or changing only this sample.

Final static verification passes, with no stale-WebGL warning. Other external
release environment/device warnings remain; existing iOS Firebase config is
not being claimed missing. Node identity tests remain 32/32; prior EditMode
47/47 and 10,000 native/sample comparisons apply to unchanged rules source.
Evidence in the existing identity folder: `batch4-play.xml`, `batch4-builds.json`,
`batch4-input/`, `batch4-small/`, `batch4-firstten/`, `batch4-viewports/`,
`batch4-tolerance/`, `batch4-runtime-static.log` and updated verification status.
Local ordinary URL returns HTTP 200 with the fresh build: http://127.0.0.1:8093/.

Checkpoint: batch 4 runtime gap closed; full round-identity objective remains
unfinished. Twelve non-callback shared pairs, numerical correctness and limited
inventories 61/78 remain open. Next: numerical regression/fix, update affected
answer inventories, then remaining resource redesign in verified batches.
No commit, push, merge, native build, upload or store deployment occurred.

## Execution Block / Resume Contract (2026-09-15)

The normal reviewed PlayMode command was rejected before process creation for
the fourth request across three consecutive affected goal turns. The unchanged
cause is `Automatic approval review failed: Selected model is at capacity`.
No Unity session or batch 4 PlayMode result exists. Do not bypass the rejection
via a direct editor call or another execution route. Goal status is blocked,
not complete; no new game edits were made in this checkpoint.

The previous turn made substantive progress: bounded solution enumeration,
32 passing Node tests, full sets for 98 rounds and the numerical-correctness
finding. Current SHA256 confirms that inventory still matches the rules source.
The independent analysis is recorded; the next required acceptance evidence
is actual execution of the four pending round changes. Further unverified
round or numeric-runtime edits would expand that verification gap rather
than close it. Twelve non-callback shared pairs and the zero-target tolerance
issue remain unresolved. Neither static passes nor the old preview closes them.

External resume condition: restore a working approval review/model, then
authorize the same local Unity test command:

`env ONE_EQUALS_ONE_PLAYMODE_RESULTS=/tmp/one-equals-one-identity-batch4-play.xml ONE_EQUALS_ONE_PLAYMODE_LOG=/tmp/one-equals-one-identity-batch4-play.log bash scripts/verify-one-plus-one-minus-one-playmode.sh`

After it passes: fresh QA build; changed/neighbor inputs for 10/11/12,
32/33/34, 47/48/49, 82/83/84; small-screen alternatives and save regression.
Then native reproduction/correction review for `1 / 11 111` at target zero,
followed by remaining shared-resource redesign in verified batches. Existing
WebGL is still batch 3 and native iOS build 2 is unchanged. No commit, push,
merge, upload or deployment was performed.

## Round Identity Solution Inventory (2026-09-15, Node Analysis Only)

Added `enumerateRoundSolutions` and the report's `--solutions 11,33,48,83`
mode, with bounded nodes/results and explicit completeness/limit reasons.
Canonical registered token arrays are enumerated, with adjacent numeral
concatenation and both arithmetic and player-made equalities. The `x` alias
is represented by `×`; physical poses and human solution strategies are not
counted. Small-board results match an independent unpruned token-product
traversal through the existing acceptance predicate. No game data or runtime
code changed in this follow-up.

RED: missing enumerator fails three tests; missing CLI fails its JSON test.
Final Node suite passes 32/32; 100 samples, native/sample parity and static
verification pass, with old-build/external readiness warnings retained in
`batch4-inventory-static.log`. The scoped diff whitespace check passes;
unrelated 2048-blink generated-file whitespace was left untouched.
A first expected-count assertion also exposed
an actual acceptance behavior: Round 48 accepts `1 / 11 111` and `1 / 111 11`
for target zero, because 1/11111 is nonzero but below the current 0.0001
tolerance. Both the Node mirror and C# `TargetTolerance` use this threshold.
The test now characterizes that behavior instead of suppressing those answers.
This is an open numerical-correctness risk, NOT an approved alternate exact
answer or a player/native reproduction. Do not change the tolerance blindly;
native regressions must cover legitimate fractional cancellation as well.

| Round | Complete canonical accepted arrays | Interpretation |
| --- | ---: | --- |
| 11 | 1 | Sample only; no second distinct answer claimed. |
| 33 | 2 | Two packings of the adjacent triple-one operand. |
| 48 | 6 | Four exact-zero subtraction arrays plus two tolerance-admitted divisions. |
| 83 | 3 | Three registered-token packings of 1111 divided by 11. |

Initial per-round limits of 200,000 nodes / 1,000 answers complete 86 rounds.
Only incomplete rounds were expanded to 2,000,000 / 10,000, then remaining
ones to 10,000,000 / 100,000. Final completeness: 98/100 in this Node grammar.
Rounds 61 and 78 hit the final node limit with 12,377 and 11,524 known arrays;
these are lower bounds, not full counts. Full token sets are not human gameplay
or native numerical equivalence proofs. The 10,000 native/sample comparison
still passes, but it does not cover all these newly enumerated arrays.

All 14 currently shared resource pairs have complete sets. Only 30/100 has an
identical full canonical accepted set (315 arrays each). Substantial partial
reuse remains: 64/76 share 2,710 of 3,145 / 3,231 arrays; 22/95 share 167 of
216 / 185; 25/96 share 14 of 66 / 15. These support continued redesign, not a
claim that the remaining pairs are adequately differentiated. Keep the 12
non-callback pairs open. Round 48's numerical issue is a separate correctness
item, not a reason to ban division or another token.

Reproduce selected inventories with:

`node scripts/report-one-plus-one-minus-one-round-quality.mjs --solutions 11,33,48,83 --strict`

For bounded larger searches, use `--max-nodes` and `--max-solutions`;
`--strict` fails incomplete enumeration. Evidence in the existing identity
folder: `batch4-solution-inventory*.json`, `batch4-solution-summary.json`.
The summary records source SHA256, per-round budgets and shared-set counts.

The same reviewed PlayMode command was retried at this continuation's start
and again rejected before process creation by the approval service's capacity
error. That is three requests across two affected goal turns, not a Unity
test failure. No alternate execution route was used. Batch 4 player/build
verification remains pending; the old batch 3 preview is not new evidence.
Resume with that verification, then reproduce the zero-target tolerance case
in native tests and input before choosing a correctness fix. Further round
data changes remain gated on closing batch 4's player-verification gap.

## Round Identity Batch 4 (2026-09-15, EditMode Passed / Runtime Pending)

Four data changes are applied, not yet validated in a fresh player build:

| Round | Previous sample | Current source sample | Reason |
| --- | --- | --- | --- |
| 11 | 11 - 1 | 1 1 - 1 | Preserve target 10 and four sticks, but require neighboring-number construction; 1 = 1 no longer fits four slots. |
| 33 | 11 - 1 - 1 | 1 11 - 1 | Fewer slots/sticks, split triple-one packing instead of two repeated subtractions; removes the equality shared with 21. |
| 48 | 1 1 - 1 | 111 - 1 11 | Moves the former Round 11-equivalent condition to a packed/split triple-one comparison so the coupled edit does not recreate a duplicate. |
| 83 | 111 + 1 - 1 1 | 11 11 / 11 | Preserve target 101 while reducing six slots/nine sticks to four/seven. Adjacent registered 11 tokens form 1111 before division; no new 1111 token or recognition rule is introduced. |

Initial three regressions fail Node 17/20 and EditMode 43/46, then pass after
the coupled edit. The added 83 regression separately fails Node 20/21 and
EditMode 46/47 before its data change. Final EditMode passes 47/47, all 100
samples and 10,000 native/Node sample pairs match. Each test rejects the former
shared answer, preserves its valid owner and accepts a revised answer. For 11
that last answer is its sample, not a claimed second distinct solution; 33/48/83
also have differently packed alternatives awaiting actual input verification.

Current source: 49 cumulative changed indices, 60 resource pairs, 14 proven
shared-equality pairs (batch 3: 18), and 14 directed sample transfers (unchanged).
Mean slots decreases 5.88 -> 5.86; mean sticks stays 9.00; maximum remains 18.
The only exact duplicate is the explicit 30/100 title callback. These figures
describe source/evaluator results, not a new verified browser build or 100
independent human experiences. Remaining 14 pairs include 2/5 and 30/100;
at least 12 further resource reassignments are still needed for the others.

The old candidate search excluded composed numbers above 111 and missed 83's
shorter 101 solution. The expanded bounded search allows numeric operands up to
1111, formed only from registered 1/11/111 tokens, integer results -2..122,
2-5 terms, at most one split operand, 11 slots and 18 sticks. Its 600,000
deterministic attempts produce 314 candidate condition/target keys across 24
resource pairs. This is not exhaustive, does not find every coupled reassignment,
and its workload scores do not measure fun or human difficulty.

The search is now exported by the shared identity module and available through:

`node scripts/report-one-plus-one-minus-one-round-quality.mjs --candidates --samples 600000 --seed 15092026`

It suggests candidates without editing the game. Tests cover reproducibility,
input immutability, supported tokens, budgets/seeds, numerical validity, unique
condition keys and rejection of occupied/shared-equality conditions. The CLI
test exposed a real truncation: console output followed by immediate exit lost
the tail of large piped JSON (25/26 tests). Both JSON modes now await stdout's
write callback. All 26 Node tests pass; a complete 288,500-character candidate
report parses correctly. Candidate values match the temporary search's 314
results; tie ordering is now locale independent. Existing static verification
includes these tests automatically. The temporary helper delegates to the
shared implementation instead of keeping a second solver.

Runtime gap: PlayMode did not start. Both normal approval attempts were rejected
by the approval service with `Selected model is at capacity` (the second also
mentions a remote compact task). Script review and `bash -n` confirm a local
Unity test plus /tmp XML/log parsing only; the retry used the same command,
not a bypass. This is an execution-service problem, not a failed game test or
a live process to poll. No QA/ordinary/capture build or browser run follows it.
Hashes confirm all three WebGL outputs still match `batch3-builds.json`; the
preview remains the older verified 47-index / 18-shared-pair candidate. Native
iOS build 2 is also unchanged. Static passes with an expected build-freshness
warning plus the existing external environment/device warnings.

Evidence prefix: `artifacts/one-equals-one/2026-09-15-round-identity/batch4-*`.
`batch4-verification-status.json` separates source passes from missing runtime
evidence. Also see `resource-candidate-pool-v2.json` / `-v3.json` and the full
candidate CLI JSON. No commit, push, native export or deployment occurred.

Resume first: run the same PlayMode command after approval-service recovery,
then fresh QA and actual input for 10/11/12, 32/33/34, 47/48/49, 82/83/84.
Check 11's sample and alternatives in 33/48/83 at 320x568, followed by ordinary
and capture builds, first-ten/save and relevant viewport checks. Do not add more
unvalidated round edits before closing this gap. Next candidate review can use
6/13's `11 * 111 - 11 11` and 8/9's reciprocal cancellation, but neither is
assigned or approved as a better puzzle yet. The goal is active, not complete.

## Round Identity Batch 3 (2026-09-15, Verified Candidate / Goal Open)

Continued from batch 2's 22 proven shared-equality pairs. This coupled batch
preserves 100 indices, free recognition, alternate answers and the ad policy:

| Round | Previous sample | Revised sample | Reason |
| --- | --- | --- | --- |
| 35 | 1 / 1 + 1 1 | 11 - 1 1 | Shorter comparison of packed and neighboring numbers; frees its old condition for 43 without creating another duplicate. |
| 43 | 11 / 1 + 1 | 1 / 1 + 1 1 | Keeps target 12 but removes the equality shared with 16; six slots/seven sticks has no valid equality in the searched grammar. |
| 57 | 111 - 11 = 111 - 11 | 111 + 11 = 11 + 11 1 | Replace identical sides with reordered terms and different number packing, removing the shared answer with 44. |
| 75 | 1 * 1 + 1 = 1 + 1 | 111 = 11 1 + 11 - 11 | One packed side versus a split number and cancelling pair; eight instead of nine slots, distinct resources from 55. |
| 88 | 11 * 11 - 11 1 | 111 - 111 / 111 * 11 | A mixed-precedence challenge: correct result 100 versus left-to-right 0; removes sharing with 82. |

New finding: `IsRoundSolved` checked total sticks but not token-array length.
The controller already supplies the board's fixed slot array, but the raw API
accepted two separate ones in the single-slot Round 6 and one packed 11 in
two-slot Round 7. Both new regression tests fail before the guard (0/2).
The guard now enforces the existing board contract, not a new token restriction.
The native cross-matrix no longer has a separate length pre-filter: all 10,000
pairs go through the actual rules API and match Node acceptance directly.

Pre-change redesign tests: Node 12/17, EditMode 35/41; separate slot-contract
regressions 0/2. Final: Node 17/17, EditMode 43/43, PlayMode 37/37, all 100 data
samples and strict quality gates pass. Exact duplicate exception stays 30/100.
Resource pairs 57 -> 60; shared-equality pairs 22 -> 18; directed sample transfers
18 -> 14. Mean slots stays 5.88; mean sticks 8.94 -> 9.00, below original 9.12;
maximum stays 18. Cumulative changed indices: 47. None of these metrics proves
100 different answer sets or human difficulty/fun.

Fresh QA touch passes 34/35/36, 42/43/44, 56/57/58, 74/75/76 and 87/88/89.
Separate 320x568 input passes alternatives in all five changed rounds, with
filled captures inspected for triple sticks, equality, target 100 and footer
separation. A detector-only failure at small-screen Round 57 is preserved:
eight visible boxes became six connected ink components because neighboring
hand-drawn outlines touched. The temporary input harness now finds separate
slot interiors by fill color, also waiting past the dim startup transition.
The preserved failure image yields eight interiors/sixteen bank sticks, then
all five alternative inputs pass on the unchanged game binary. Assertions and
game rules were not weakened to hide the detector failure.

Fresh QA/ordinary/store-capture WebGL builds pass. Ordinary first-ten input,
focus/cancel edges and saved progress 11 pass. A separate run renders all five
viewport sizes, with SE/desktop boundaries inspected; this is browser QA, not
native device or human pacing signoff. Static passes with the existing external
environment/device warnings. Existing iOS config is not absent just because
this shell did not load release env values. Previous picker/final-state evidence
is retained for unchanged code/data, not reported as a new 100-round playthrough.

Evidence: `artifacts/one-equals-one/2026-09-15-round-identity/batch3-*`, including
failed/passing XML, cross-matrix, changes, build SHA-256, input and viewport images.
Preview returns HTTP 200 at `http://127.0.0.1:8093/`. Only generated scene IDs and
settings whitespace were removed afterward. No commit, native build or upload.

Sixteen resource groups still produce 18 shared-equality pairs. Apart from the
tutorial 2/5 and title 30/100 recurrences, eliminating them requires at least
15 further resource reassignments. No impossibility claim is made. Filtering the
old bounded candidate pool rejects division-by-one chains and long target-9
subtraction variants rather than replacing 33 solely for a better count.
Next unimplemented coupled candidate: 11 `1 1 - 1`, 33 `1 11 - 1`, 48
`111 - 1 11`. In-memory analysis passes identity gates, reduces shared pairs to
15 and keeps mean slots 5.88 / mean sticks 9.02. It preserves 11's target and
stick count, introduces split triple-one packing earlier, and moves 48 out of
11's new condition. Still needs native red tests, data edits, neighboring input
and a review of whether this improves actual judgments. It is NOT in this build.
The identity goal remains active; remaining reuse is not an external blocker.

## Round Identity Batch 2 (2026-09-15, Verified Candidate / Goal Open)

Continued from the verified 40-index candidate instead of treating its remaining
27 shared pairs as an external blocker. Bounded deterministic sampling produced
386 candidate condition/target keys across 28 resource pairs, with integer
targets -2..122, literals at most 111, at most one split number and 11 slots /
18 sticks. It is NOT exhaustive; the score estimates placement/redundant-operation
cost, not human difficulty or fun. `resource-candidate-pool.json` records scope.

Selected batch changes resources, not recognition or the equality rule:

| Round | Previous sample | Revised sample | Reason |
| --- | --- | --- | --- |
| 38 | 1 1 × 11 | 1 1 × 1 | Keep the middle band's cross-multiply practice; four slots/five sticks has no valid equality, unlike 18/38's former resources. |
| 39 | 11 + 11 | 111 + 11 | Three slots/seven sticks removes the reusable 11 = 11; mix two number sizes rather than force a symbol. |
| 52 | 11 / 11 = 1 | 1 11 / 111 | Number packing and division preserve target 1 with one fewer slot/stick; no equality witness exists at four/seven. |
| 70 | 111 / 1 = 111 | 111 * 111 / 111 | Keep the compact five-slot shape but require a different resource allocation; preserves 87's meaningful precedence challenge unchanged. |
| 90 | 11 + 1 × 1 - 1 | 11 - 1 1 - 1 | Return to the negative target introduced at 50 with packing and fewer placements; eliminates 19/90's common equality. |

Five new Node/Unity regressions reject the old shared answer in the revised
round, still accept it in its unchanged owner, and accept a valid alternative
in the revised round. Before edits: Node 7/12, Unity 31/36. After edits: Node
12/12 and Unity 35/35. The old Round 70 equality fixture is replaced by the
stronger resource/owner/alternative case; Round 87's equality and precedence
coverage remains. Native/Node sample parity passes all 10,000 pairs.

The first candidate removed the middle band's only cross-multiply sample;
the existing band check failed and prompted the revised 38 shown above. The
check was not weakened. Current data/strict quality pass with remaining reuse
explicitly warned. Shared equality pairs: 27 -> 22; resource pairs: 56 -> 57;
mean sticks: 8.95 -> 8.94; mean slots: 5.90 -> 5.88. Exact duplicate exception
remains only 30/100. This is still not 100 independent answer sets.

Fresh QA input passes 37/38/39/40, 51/52/53, 69/70/71 and 89/90/91. Separate
320x568 touch input passes alternatives `1 × 1 1`, `11 + 111`, `11 1 / 111`,
`111 / 111 * 111`, and `1 1 - 11 - 1` in the corresponding five changed rounds.
All five filled small-screen captures were inspected; targets 122/-1/111,
triple sticks and the cross/star silhouettes remain visible. These tests use
known inputs and QA-seeded progress, not unassisted human solving or sample-fill.

PlayMode initially fails 36/37 because its target hide/restore test expects the
old Round 39 target 22. It now derives the target from the selected round and
asserts that value both before and after the same rotation sequence; hiding,
restoring and no slot movement assertions remain. Full PlayMode passes 37/37.
The old failure XML is preserved; it is a stale fixture, not a new UI regression.
Fresh QA/ordinary/capture WebGL builds pass, as does static verification with
the existing external environment/device warnings. The ordinary build renders
in five viewport sizes; the small/desktop boundaries were inspected. SHA-256
fingerprints are in `batch2-builds.json`. The preview responds HTTP 200 at
`http://127.0.0.1:8093/`. Only build-generated scene IDs/settings whitespace were
removed afterward; no gameplay data was reverted.

This batch changes neither saved indices/keys, first-ten samples, picker code,
last-round data nor ads. Their previous final-state/first-ten browser evidence
is retained rather than claimed as a new full-game replay. Current PlayMode
also rechecks all 100 target layouts and picker pages. Native device and human
pacing signoff remain open. Earlier screenshots and native build 2 are not
proof of the five revised expressions. Evidence prefix:
`artifacts/one-equals-one/2026-09-15-round-identity/batch2-*`.

Cumulative difference from the original baseline is now 44 indices. Twenty
resource groups still yield 22 shared equality pairs, recorded in
`batch2-remaining-reuse.json`; preserving tutorial/title recurrence still leaves
a lower bound of 19 further resource reassignments. No impossibility or complete
solution-set claim is made. Next inspect 16/43, 21/33 and 82/88 against the
bounded candidate pool, and reject replacements that only add repetitive
division-by-one or a longer board. The active identity goal is not complete.

## Round Identity Redesign (2026-09-15, Verified Candidate / Reuse Open)

Baseline and candidate evidence is under
`artifacts/one-equals-one/2026-09-15-round-identity/`. The actual rule is a
resource-valid expression matching the target OR a valid player-built equality.
An equality in SampleSolution does not select a separate puzzle mode.

| Metric | Baseline | Latest candidate |
| --- | --- | --- |
| Distinct slot/stick pairs | 38 | 56 |
| Identical resource/numeric-target groups | 10 | 1 |
| Directed sample transfers to other rounds | 89 | 23 |
| Unordered pairs with a proven common equality | 158 | 27 |
| Common-equality pairs within three rounds | 15 | 1 (tutorial 2/5) |
| Mean sticks / maximum sticks | 9.12 / 18 | 8.95 / 18 |
| Mean slots | 5.67 | 5.90 |

These are NOT counts of unique puzzles or complete answer-set similarities.
The remaining identical group is exactly Round 30/100: the same title expression
first closes the original short sequence and returns as the final callback.
This is a narrow, checked exception, not permission for additional duplicates.
Tutorial 2/5 still permits `1 = 1`; teaching two gestures does not make their
answer sets independent. No token restriction was added to force that lesson.

Forty indices differ from baseline: 12, 18, 23, 24, 27, 29, 31, 32, 34-38,
41, 42, 45, 47-51, 56, 58, 63, 67, 68, 71, 74, 77, 80-84, 86, 88, 89,
91, 93, 98. Names, indices, save keys and the first ten samples are preserved.
The JSON snapshots give exact before/after expressions and resource conditions.
Examples: 12 separates division from the three-slot star puzzle; 18 introduces
an equality with number packing; 48/67 share four single sticks but no equality
exists in that grammar, and their different targets require different answers.
34 and 84 use equality-free resource conditions instead of merely changing a
target. 56/71 exchange positions to remove nearby reuse; that exchange is a
pacing change, NOT increased identity. 50 introduces a supported negative target.

### Remaining Reuse And Design Boundary

The report deliberately retains an unresolved shared-equality section. Apart
from the explicit title/tutorial cases, distant shared pairs are not signed off
as educational exceptions. In particular 2/5/11 share one witness, while
14/52, 19/90, 22/95 and 65/94 illustrate reuse spanning large distances.
All groups and concrete witnesses are in final-candidate.json. Moving them
apart reduces immediate repetition but does not remove their common answers.

For any two rounds with the same slot/stick resources, a valid equality witness
is accepted independently of both targets. Therefore changing only target,
title or sample CANNOT remove that overlap under the current rules. Further
removal requires changing resource conditions, accepting explicit recurrence,
or separately authorizing a rule change. A token ban or target-bound equality
would change existing valid answers and was not introduced. More resource
variants can increase concatenation, calculation or placement burden; they are
not automatically better puzzles. No impossibility of 100 distinct answer sets
is claimed. Human pacing review and the user's recurrence preference remain open.
The 25 disjoint resource groups contain 27 shared pairs. Eliminating every such
pair requires at least 26 resource reassignments; preserving tutorial 2/5 and
title 30/100 still requires at least 24. This is a lower bound from the groups,
not proof that further redesign is impossible. Per-group dispositions are in
`remaining-reuse-review.json`; they remain open, not silently approved exceptions.

The search stops at the first witness or 200,000 visited nodes per resource
pair, with found/exhausted/limited reported separately. It covers supported
number tokens, concatenation and a single equality; `x` is represented by its
equivalent `\u00d7`. It is not a full solution enumerator. Node sample acceptance is
cross-checked against the real Unity evaluator for all 10,000 sample/round pairs.

### Verification Results

Final candidate EditMode 31/31 and PlayMode 37/37 pass. The new aspect-ratio
test first failed at six slots / logical width 390 (1.088 ratio). Limiting
width together with the height cap fixes all 1-11 slot plans at four widths,
with both target display modes. Round data and strict identity checks pass.
The quality report now explicitly warns about the 27 shared pairs even when
its blocking duplicate/nearby/coverage gates pass.

- QA, ordinary and store-capture WebGL builds succeed. `final-builds.json`
  records hashes/timestamps. No native build, upload, commit or push was done.
- Candidate-1 actual touch covers 75 distinct rounds. The final four data edits
  (34/56/71/84) and their neighbors were replayed on the final binary. Combined
  input records cover 78 distinct mid/late rounds, including all 40 changed
  indices with their final expressions. Initial placement uses known samples,
  not blind solving and not sample-fill hooks.
- Separate final ordinary-build first-ten/save input regression passes. This
  does not constitute a single natural playthrough from Round 1 to Round 100.
- At 320x568, actual placement/rotation/check pass for 34, 84 and 100. Inspected
  filled captures retain six-slot aspect, `= 111`, triple sticks and end text.
  Candidate-1 input also covered 18-stick/11-slot layouts (97/78).
- Five final ordinary-build viewport renders were inspected. All nine picker
  pages were traversed both ways in a separate final-binary state fixture.
- That state fixture intentionally uses QA sample-fill only to establish the
  final board. Actual Check emits the clear and milestone-100 candidate event;
  WebGL does not show a native ad. Reload without QA query opens page 9 with
  Done retained and Sound off. Reset also preserves completion. Evidence is
  `final-state/`; do not conflate this fixture with the actual placement tests.
- Static verification passes with external environment/device signoff warnings.
  Existing iOS settings were not deleted or deemed absent simply because this
  shell did not load release environment variables. Native/audio/human pacing
  evidence remains outside these browser tests. Previously generated store
  submission screenshots need refresh before any newly authorized upload.

Current result: a verified 40-index redesign and a reproducible identity gate,
NOT 100 independent answer sets or complete game release. Next design decision
is how much distant recurrence to retain before another resource-changing batch;
do not remove accepted equalities or add symbol restrictions without approval.

## Post-Checkpoint Investigation (2026-09-15, Local Gates Passed)

Base `26d8508a`; prior blocked suite now passes 36/36, with EditMode 29/29.
A new interleaving (old finger down, pause/resume, new finger down, old click)
reproduces an unwanted rotation: red result 36/37. Tracking accepted pointer ID
and guarding drag-state mutation fixes it; full PlayMode is now 37/37. The test
also sends the old begin/drag/end before releasing the new finger.

The final QA binary has SHA-256 WASM
`ed49b0b0937de6d21f204fc3960e54a755c9b174674702b724bddc2d4fae84ea`
and data `064561c67bf83cc4c889a0611ca4b11a83e35bef5b2ac39feabad2fa8ba55ccb`.
Its observed session begins 05:18:56 UTC, time origin 1789449526207.9.
Trace/screenshots: `artifacts/one-equals-one/2026-09-15-post-checkpoint/final-browser/`;
observations and CDP responses are timestamped in `trace.jsonl`. Observed end
05:49:15.694 UTC: 1818.709 seconds (30m18s), 66 observations, unchanged time
origin and binary hashes, no captured runtime exception or runner error.
Planned idle gaps and observed tab hiding are included; the old stopped session
is excluded. This is sampled observation, not continuous frame inspection.

Final-binary input checks so far:
- Three separate `111` removals and three transfers preserve `11`, free-stick
  counts and destination `1`; further `11 -> 1` return is also visible.
- Actual tab hiding is verified with `document.hidden=true`. Held-drag return
  clears its ghost, ignores late release, and accepts a fresh rotation.
- Two-finger release evidence was initially ambiguous due to an invalid CDP
  command. The protocol requires empty touch points for touchEnd; changing the
  active set with touchMove releases only the removed point. The first-ten
  script is corrected, and its full input/save regression passes again.
- After correcting that sequence, DOM tracing confirms finger 0 then finger 1
  release order. The old-finger screenshot retains the vertical bank stick;
  only the fresh release turns it into slash. This is browser emulated touch,
  not a physical-device suspension test.
- All nine picker pages are reachable; page 9 preserves control positions and
  keeps rounds 97-100 locked when the QA seed unlocks through 96 only.
- Round 96 is entered through the picker. Known expression
  `111 - 1 - 1 / 1` yields `Makes 109.` against 108, at 390 and 320 widths.
  This is deliberate regression input, not blind novice solving.
- After 11 failed checks, changing the slash to minus clears 96 as 108;
  telemetry excludes the hard clear from ad eligibility. Round 97 accepts
  `111 = 111 + 11 - 11 x 1`, a valid alternative to its sample, through actual
  placement. Rounds 98/99 clear as 101/99, and 100 as the title expression.
- Round 100 displays `Goal Mode clear!`; the picker marks 97-100 Done. A second
  check is classified as replay and excluded from ad eligibility. Milestone
  100's first clear is eligible in telemetry but WebGL shows no native ad.
- All placements above are real emulated touch, not sample-fill hooks. QA
  initially seeded progress through 96; this is not a natural 100-round run.
- The ordinary first-ten/save regression passes with the corrected partial
  touch release. Five ordinary-build viewports were rendered and inspected:
  small/standard/large iPhone sizes, Android 20:9 and desktop. No native
  device, human difficulty or speaker/headphone signoff is inferred.
- Final repeat groups comprise eight short manipulation cycles, ten small-screen
  equation cycles and ten muted Round 100 replay cycles. A fourth stick dropped
  into `111` is rejected with `Box fits 3.` and eight of eleven remain in bank.
- After the end marker, navigation without QA query parameters opens page 9
  with 97-100 Done and Sound off. Reset/replay did not erase completion.
  This reload has a new time origin and is not counted in endurance duration.
- Browser JS heap samples range from 6,862,724 to 9,570,192 bytes, ending at
  8,466,848; listeners peak at 173 and return to 48, DOM nodes stabilize at
  112 after startup, and AudioHandlers peak at 125 then fall to nine. No
  forced collection was requested. These are browser metrics, not a native
  memory-leak verdict. See generated `soak-summary.json` for exact bounds.

Local checkpoint: the pending verification gaps and newly reproduced pointer
issue are resolved in the inspected scope. Next meaningful gate is a separately
authorized native candidate containing these fixes, then real-device old/fresh
finger suspension, long-session labels/audio and ad-return verification. Keep
human pacing assessment and production console evidence open; do not invent
additional code changes solely to extend the run.

Protocol reference checked:
[Chrome DevTools Input protocol](https://github.com/ChromeDevTools/devtools-protocol/blob/master/pdl/domains/Input.pdl).
The initial 5m40s session before the pointer fix is separate, not added to this
final-binary run. Native touch, listening and production SDK checks remain open.

## Approval-Blocked Resume: QA Reliability (2026-09-14)

The requested full PlayMode retry did not start: automatic approval failed with
`Selected model is at capacity`. No new game binary or live browser result was
produced during this resume. Earlier 34/35 results remain the latest Unity run.

Independent runner inspection found dropped browser exception events, unbounded
CDP waits and failure screenshots masking original input-test errors. Added a
small shared CDP client and nine isolated Node regressions. Both dropped-event
and masked-failure cases were observed red before correction; all nine now pass.
The existing interruption bridge test and 100-round data/strict quality report
pass as well. The fixture uses a fake WebSocket, not emulated game input.

Resume with the pending Unity suite, fresh QA build, actual tab interruption and
all-three `111` removals, then the measured uninterrupted 30-minute session.
Approval infrastructure failure is not a game crash and is not release proof.

## Long-Session Follow-Up (2026-09-14, Interrupted / Resumed)

Baseline browser started at 06:55:01 UTC with the previous local QA build.
Source baseline: `d2abe1b4` rules plus the preceding local controller changes;
controller snapshot is in `artifacts/one-equals-one/2026-09-14-long-session/`.
No navigation/reload is used during the measured session. A separate fresh QA
browser verifies the changes made during this investigation.

- Round 57 clears as `111 - 11 = 111 - 11`, 58 as `1 x 1 = 1 / 1`
  (the game uses the multiplication glyph), and 59 as `1 * 1 = 1 x 1`.
  These formulas were already known; this is input/pacing inspection, not a
  blind novice test. No sample-fill hook was used in these browser attempts.
- Before correcting 59, `11 / 1 = 11 / 11` gives `Not balanced.` while logs
  contain `11 is not 1.`. The new controller keeps the evaluator's comparison.
  The first test fixture accidentally overlapped both vertical sticks; it was
  corrected to the actual left/right poses before recording the red assertion.
- Old Round 60 accepts exactly the Round 57 input sequence. Unlike a distant
  callback, this repeats within three rounds. Revised 60 has five slots / 12
  sticks instead of seven / 14. It offers a shorter arrangement with the star
  representation; this is not evidence that human-rated fun has improved.
- Round 61 was checked at 390x844 and 320x568, including pickup/return/reset,
  picker sound toggle and 12 muted manipulation/reset cycles with idle gaps.
  Small-bank touch comfort still needs physical-device assessment.
- A same-round registry test found 245 stored faces versus five live roots
  after 40 rotations / eight resets. Cleanup now occurs before detachment.
  Placed non-shared faces are not registered for chatter in the existing design;
  regression checks preserve their visible mouths separately rather than
  changing that animation design. PlayMode 33/33 and EditMode 29/29 pass.
- Browser metrics include JS heap, DOM/listener/audio counts and a browser rAF
  interval histogram. They are not Unity/native heap or GPU/FPS measurements.
  Transient listener/audio growth decreased again without forced collection.
  Observed duration before interruption was about 24m44s, not 30 minutes.
  The resumed browser did not respond after the unattended gap; no endurance
  pass is claimed. Raw available metric responses are in `browser-metrics.json`.
  The long rAF gaps include deliberate tab hiding and are not classified as
  foreground rendering regressions. Neither browser JS heap growth nor its
  subsequent collection establishes Unity/native allocation behavior.
- New-code browser replays (`fixed-input/`) show `11 is not 1.` at both sizes
  and clear revised Round 60 as `111 = 1 * 111` at 320x568. The subsequent
  ordinary first-ten input/save regression passed, before the interruption fix.
- Round 96 kept its target and labels through resizing, but synthetic blur
  alone did not reliably cancel a placed drag. Actual tab activation confirmed
  `document.hidden=true` while hidden; on return the drag ghost was still alive.
  Releasing outside returned a stick. Therefore earlier bank-only cancellation
  smoke coverage must not be treated as a real tab-switch cancellation pass.
- That outside return also exposed `111 -> 11` normalization: removing a side
  stick left center/right poses and an unrecognized label. The red test fails
  on index zero; the fixed test covers removal of all three indices.
- Browser blur/visibility changes now advance a retained version, even when
  the game does not update while hidden. Controller polling plus event-entry
  checks cancel stale drags; per-press versions reject taps begun before pause.
  The bridge's Node test checks persistence through hide/show and listener
  non-duplication. This does not replace an actual WebGL replay.
- Latest PlayMode run after these fixes: 34/35. The pickup-cell test omitted
  pointer-down; its input sequence is corrected and a new resumed-press assertion
  added. The final rerun was rejected by automatic approval infrastructure
  (model capacity). Final QA/release/capture builds and a new 30-minute measured
  session remain pending. Old browser runners were stopped after losing CDP
  responses; child-process inspection also requires restored approval access.

## Continuous Quality Follow-Up (2026-09-14)

Base remains `d2abe1b4` plus the preceding local changes. Investigated new input
boundaries instead of replaying all 100 solutions. Evidence root:
`artifacts/one-equals-one/2026-09-14-continuous-quality/`.

- Actual emulated `touchCancel` preserved all sticks but left the drag prompt
  visible. Cancellation now restores the preceding feedback and its color.
  Focus, pause and disable callbacks are covered by the new PlayMode test.
- A second pointer-down on the same stick could clear the handler's drag flag;
  after focus cancellation, its delayed click rotated the bank stick. The event
  regression failed before the fix. Presses begun during a drag now lose click
  eligibility; beginning a drag marks it as a drag immediately. No rotation
  rules, stick costs or drag thresholds changed.
- Added both cancellation paths to the ordinary WebGL first-ten input script.
  It passes through Round 10 and restores progress 11. The focus event is
  synthetic browser focus loss, not native background/resume. Normal Round 1
  placement must still clear as `1` after these interruptions.
- Bounded QA browser session 06:30:13-06:38:11 UTC: 26 placement/rotation/return/
  Reset cycles (52 captured states, active repetition about 160 seconds),
  390x844 to 320x568 resize during a held drag, all nine picker pages, then
  actual-input Round 96 `111 - 1 - 1 - 1` and transition to 97. Sample fill was
  not used, but the solution was already known. No captured runtime exceptions;
  inspected beginning/middle/end snapshots retain `= 111`, recognition labels,
  counts and geometry; the final Round 96 target `= 108` remains visible.
  This is sampled visual evidence, not continuous frame-by-frame proof.
- The picker exploration uncovered another issue: page 9 moved Previous/Next/
  Close upward because it contains only four rounds. A click at the previous
  button location missed. New bounds assertions reproduced 298.286 logical
  units of pager movement. Flexible grid space now anchors the commands while
  nonexistent round buttons remain inactive. Existing compact-grid calculations
  are preserved. Final QA page 1-9 traversal and same-location 8/9 reversal,
  plus Close restoration, pass at 390x844 and 320x568 (`picker/`).
- Final PlayMode suite passes 31/31, including a 60-cycle mixed-input/resize
  test across Rounds 8/30/50/75/90/100. It checks visible targets separately
  from intentionally hidden equality targets, recognition glyph generation,
  placement conservation, and 120 settled idle frames. No font atlas rebuilds
  occurred during this measured Editor run. This does not establish native
  allocation/frame-time behavior or indefinite font stability.
- Current quality report still flags no consecutive structural run above two;
  separated constraint repetitions remain listed for human pacing assessment.
  Prior adjacent-repeat fixes are retained. No new round replacement is
  justified by this investigation; no claims about novice learning speed.

Physical-device enumeration still returns no devices. Native audio/touch/Safe
Area, production consent/ad/crash callbacks and Apple processing/review remain
external verification. The previous local privacy correction is still pending
authorized publication; this pass does not redeploy or certify store answers.
No native installation, upload, commit, push or merge is performed. Uploaded
`1.0.0 (2)` does not contain any of these local follow-up fixes.

Final artifacts: QA/release/store-capture builds pass; final release five-size
smoke and basic static suite pass. Static production/device warnings are not a
strict ship-ready approval; the default invocation does not load the existing
iOS local environment. Next-build candidates were recaptured (24 images), pass
dimensions/alpha/freshness/content checks, and were inspected in three contact
sheets and a full-size final-picker capture. Existing `Candidates` for the prior
submission were not changed. Final game/source evidence is in `local-source.patch`;
preview remains `http://127.0.0.1:8093/index.html`.

Tap cancellation before reaching drag threshold also leaves the bank unchanged;
a subsequent ordinary tap rotates exactly once. This browser boundary needed no
additional code change. New locally reproduced acceptance items are resolved;
this is a development checkpoint with physical/production and human evaluation
still open, not game perfection or release approval.

## Method And Limits

2026-09-13: agent-directed Chrome touch events at 390x844. Each expression below
was chosen from the visible board before opening that round's sample source.
Earlier conversation already contained some examples; this is not novice or
human difficulty research. Input time is automation time, not learning time.
Evidence: `/tmp/one-equals-one-gameplay/trace.jsonl` and round PNGs. No sample-fill
was used in this investigation. Round 34's sample was incidentally revealed by
a duplicate-data verifier after proposing a change to Round 15.

## Observed Rounds

| Round | Touch-built expression | Observation |
| --- | --- | --- |
| 11 | `11 - 1` | Packed number/subtraction; the tutorial hint directly suggests the operation. |
| 12 | `11 / 11` | Five-stick budget distinguishes this from a four-stick `1 = 1`. |
| 13 | `1 - 1 + 1 - 1` | First observed jump from three to seven slots; moving bank targets amplified eight placements. |
| 14 | `1 + 1 × 1` | Precedence lesson did not distinguish left-to-right calculation. Replaced and replayed as `1 + 1 × 11`. |
| 15 | `1 + 1 / 1` | Same teaching issue. Replaced with `11 + 11 / 11`; actual-input replay cleared as 12. |
| 16 | `1 + 1 + 1` | Explanation collapsed, neutral title; repeated plus placements but short expression. |
| 17 | `11 - 11` | Lower-input interval after Round 16, reuses packed number subtraction. |
| 18 | `1 = 1` | Target omitted, player-built equality clears correctly. |
| 19 | `1 + 1 = 1 + 1` | Applies equality to expressions; ten placements with no new token. |
| 20 | `11 = 11` | Short packed equality; milestone ad candidate emitted, no ad available, progression continues. |
| 21 | `1 + 1 - 1` | Returns to target mode; no explanatory text, six placements. |
| 22 | `11 + 1 = 11 + 1` | Structurally similar to 19, with two packed numbers and twelve placements. Candidate repetition, not yet changed. |
| 23 | `111 - 11` | Triple-one/eleven subtraction; target 100 wraps under equals but stays within its reserved area. |
| 24 | `111 - 11 / 1` | Same target as 23, adding division by one creates more manipulation without changing the calculation. Repetition candidate. |
| 25 | `1 + 1 - 1 × 1` | Longer combined expression, preparing the title motif. |
| 26 | `1 + 1 - 1 / 1` | Same target/shape as 25 with division replacing multiplication; repetition candidate. |
| 27 | `11 / 11 * 1` | Nine-stick budget led to three-stick multiplication. Alternative ordering to the sample, which was inspected only after attempting. |
| 28 | `111 / 111` | Compact triple-one division; three packed strokes remain readable at the observed viewport. |
| 29 | `1 + 1 - 1 = 1` | Mixed operations on one side of equality; correct transition to 30. |
| 30 | `1 + 1 - 1 × 1 / 1` | Deliberately tried a final minus first (value 0); generic `Not yet.` hid the useful result. One placed-stick rotation corrected it; failure count remained 1 and clear advanced normally. |
| 31 | `1 + 1 + 1 + 1` | Ten placements for four; arithmetic is simple, but opens the next band with a readable seven-slot board. |
| 32 | `11 + 1 + 1` | Packed number plus two singles; repeated addition with a different budget. |
| 33 | `11 - 1 - 1` | Lower-budget subtraction companion to 32. |
| 34 | `11 / 11 + 1` | Alternative order, but sample had already been exposed by a duplicate check. |
| 35 | `111 / 111 + 1` | Same calculation as 34 with triple-one groups; intentionally tried subtraction first to verify new failure feedback. |
| 36 | `111 / 111 + 11 + 1` | Fourteen sticks switches bank to seven columns; combines prior expressions, but adds considerable placement work. |
| 37 | `111 - 11 - 1` | Shorter subtraction after dense 36. Margin tap/drag deliberately failed before normal center-input solve. |
| 38 | `11 × 11` | Target 121, only three slots; large result does not imply long interaction. |
| 39 | `11 = 11` | Deliberate alternate equality instead of target 22. Accepted and fixed target hidden, as required. Target magnitude alone cannot establish puzzle difficulty. |
| 40 | `111 - 1` | Five-stick subtraction; new bank cell margin successfully accepted both tap and drag. |
| 41 | `11 × 1 + 1` | Eight sticks/five slots/target 12. |
| 42 | `11 × 1 + 1` | Exact same gestures as 41 succeeded; identical player constraints confirmed. Replacement `11 + 11 + 11` also cleared using touch input. |
| 43 | `11 + 1 / 1` | Agent first misread target 12 as 10 and tried `11 - 1 × 1`; `Makes 10.` explained the mismatch. Reset/retry cleared and preserved failure count 1; not evidence of novice difficulty. |
| 44 | `111 + 1 = 111 + 1` | Fourteen-stick equality, triple-one groups on both sides; clear advances normally. |
| 45 | `11 * 1` | Six-stick star multiplication. |
| 46 | `11 * 1` | Same gestures as 45 clear again; second confirmed identical adjacent puzzle. Replacement `11 * 11` also cleared using touch input. |
| 47 | `11 * 1 + 1` | Star multiplication combined with addition; nine-stick constraint. |
| 48 | `11 - 11 / 11` | Chose a division-based alternative rather than relying on the previous star pattern. |
| 49 | `111 - 11 + 1` | Reuses the hundred construction with a one-unit adjustment. |
| 50 | `11 + 11 - 1` | Deliberate `11 + 11 / 1` failed three times; resize to 320x568 and back preserved board/failures. Correction cleared with `reason=hard_clear`, eligible=false at the ad milestone. |
| 51 | `1 / 1 = 1` | Cleared using actual touch input; six sticks, five slots. |
| 52 | `1 * 1 = 1` | Cleared using actual touch input; eight sticks, five slots. |
| 53 | `111 = 111` | Resumed using QA start/unlock only; eight sticks, three slots, no sample fill. |
| 54 | `1 + 1 - 1 = 1 × 1` | Nine slots in three rows at 390x844; alternate expression accepted. |
| 55 | `11 + 1 - 1 = 11 × 1` | Same approach as 54 with two packed numbers; resize to 320x568 preserves placement and clear succeeds. Repetition candidate, not automatically replaced. |
| 56 | `11 - 1 = 11 - 1` | Symmetric subtraction alternative; seven-slot layout and clear transition work. |
| 57 | `111 - 11 = 111 - 11` | Same construction with larger packed numbers; 14 placements. |
| 58 | `1 × 1 = 1 / 1` | Different operators balance without a symmetric token sequence. |
| 59 | `11 × 1 = 11 / 1` | Intentional minus first produced `Not balanced.`; three in-slot taps corrected it without reset. Failure count stayed 1. |
| 60 | `111 - 11 = 111 - 11` | Exact Round 57 gestures cleared again. Milestone opportunity eligible=true, will_show=false; progression continued. Repetition candidate. |
| 61 | `1 + 1 + 1 = 1 + 1 + 1` | Eleven slots, sixteen placements; three-row layout and progression work. |
| 62 | `11 + 1 - 1 = 11 * 1` | Three-stroke star variation of 55; accepted and advanced. |
| 63 | `1 + 1 - 1 = 1 × 1` | Exact Round 54 input succeeds; repeated constraints confirmed. |
| 64 | `111 - 111 = 11 - 11 / 1` | Asymmetric zero equality; new recognition labels inspected at 390x844 and 320x568, resized clear passes. |
| 65 | `1 + 1 - 1 = 1 × 1` | Exact 54/63 input succeeds again, separated from 63 by one round. Repetition cluster confirmed. |
| 66 | `1 + 1 - 1 = 1 * 1` | Star variant of the earlier solution with one additional stick. |
| 67 | `1 1 = 11` | Adjacent single-number slots concatenate correctly; shorter six-placement interval. |
| 68 | `1 * 1 = 1` | Same resource constraints and solve as 52; recognized and advanced. |
| 69 | `111 = 11 1` | Adjacent packed/single-number slots concatenate to 111. |
| 70 | `111 = 11 1` | Exact same gestures as 69 pass; consecutive duplicate confirmed. Milestone fallback advances without an ad. |
| 71 | `11 1 + 1 = 111 + 1` | Number concatenation combined with addition; eight-slot, fourteen-stick input succeeds. |
| 72 | `11 1 - 1 = 111 - 1` | Subtraction companion to 71, two fewer sticks. |
| 73 | `1 + 1 - 1 = 1 × 1` | Same visible budget and exact input as 54/63/original 65. |
| 74 | `1 + 1 - 1 = 1 / 1` | One-stick operator replacement of the prior solution, preserving value 1. |
| 75 | `11 + 1 - 1 = 11 × 1` | Same constraint/input as 55; recorded rather than automatically replacing another callback. |
| 76 | `111 - 111 = 11 - 11 / 1` | Same visible budget and actual solution as 64. |
| 77 | `11 × 1 = 11 / 1` | Same successful solution as corrected 59. |
| 78 | `11 + 1 + 1 = 11 + 1 + 1` | Eighteen sticks/eleven slots; all placements accepted, but dense bank spacing needs physical-finger comfort evaluation. |
| 79 | `1 + 1 - 1 = 1 + 1 - 1` | New feedback build: per-slot labels remain, no naked global last-token value. |
| 80 | `111 - 11 - 11` | Intentionally tried `111 - 11 / 11`: `Makes 110.` retained. Resize preserves state; one rotation clears as 89, failure_count=1. |
| 81 | `111 - 11 - 1 - 1` | Intentional final slash gives 99; target 98 remains visible after failure. Rotation corrects without reset, failure_count=1. |
| 82 | `111 + 11 - 1` | Target 121 constructed with addition/subtraction rather than square multiplication. |
| 83 | `111 - 11 + 1` | Target 101; matches Round 49 constraints/solution as a late callback. |
| 84 | `111 / 111 + 11` | Packed-number unit division plus eleven reaches 12 with eleven sticks. |
| 85 | `11 * 11 - 111` | Target 10 combines square multiplication with triple-one subtraction; three-stick star satisfies the budget. |
| 86 | `111 - 11 × 1` | Target 100 with nine sticks. |
| 87 | `111 - 11 * 1`; revised `1 + 11 * 11` | Original repeats 86 with extra manipulation. Revised target 122 passes actual-touch replay; unlock 96 preserved and replay ad excluded. Populated capture lacks fixed target; rendering follow-up remains open. |
| 88 | `11 * 11 - 11` | Target 110 uses square multiplication and eleven subtraction. |
| 89 | `111 - 1 / 1` | Seven-stick division route to the same target 110. |
| 90 | `11 + 1 - 1 × 1` | Small-screen populated board retains target 11; clear succeeds after resize, milestone fallback continues. |
| 91 | `11 + 1 - 1 / 1` | Unit-operator variation of 90 in the final title-motif sequence. |
| 92 | `11 × 11 + 1` | Target 122; same arithmetic family as revised 87 with a different operator cost/order. Not claimed as wholly new arithmetic. |
| 93 | `111 / 11 × 11` | Non-integer intermediate returns to target 111; left-to-right equal-precedence chain accepted. |
| 94 | `111 / 111 + 11 - 1` | Target 11; actual-touch clear succeeds. |
| 95 | `11 / 11 + 11 + 1` | Target 13; actual-touch clear succeeds. |
| 96 | Not attempted | Opened before user-requested stop. Resume here after investigating the intermittent fixed-target rendering observation. |

Failed Check attempts are explicitly noted in the rows above. Their count does not establish ease
for a new player. Round 14's replay also exercised bank-to-slot placement and
slot-to-bank return before solving; remaining pickups stayed at their original
positions and size in the new build.

## Evidence-Backed Changes

- Stable bank positions: removal/return/reset PlayMode case failed before the
  fix (`/tmp/one-equals-one-bank-red.xml`); full PlayMode 17/17 passed afterward
  (`/tmp/one-equals-one-gameplay-playmode.xml`). Browser before/after/return PNGs
  confirm positions, not physical-device comfort.
- First-ten actual-touch regression with fixed bank coordinates also passes:
  `/tmp/one-equals-one-stable-bank-input/input/results.json`, including saved
  Round 11 progress after reload and WebGL milestone ad fallback.
- Precedence teaching: two EditMode cases failed against the original lessons
  (`/tmp/one-equals-one-precedence-red.xml`). Full EditMode 17/17 passed after
  sample replacement (`/tmp/one-equals-one-gameplay-editmode.xml`). All 100 sample,
  resource, layout-plan and existing round-quality validations pass.
- Quality report now groups identical player-visible slot/stick/target budgets,
  treating equality samples' values as non-constraints. This reveals adjacent
  candidates 41-42, 45-46 and 69-70 that sample-only checks missed. Distinct
  budgets still do not prove distinct solution spaces because player equality
  remains a valid alternative in target rounds.

## New Actionable Findings

- Round 30 wrong-result feedback: screenshot `r30-failure-feedback.png` shows
  only `Not yet.` although telemetry correctly knows result 0. Show the current
  calculated value, not a suggested solution. Preserve board and failure count.
- Rotation location inconsistency: the existing bank cycle is vertical/slash/
  horizontal/backslash, while a single placed stick uses vertical/horizontal/
  slash/backslash. Match the physical order in both locations; preserve free
  recognition and additional packed-number positions for multiple sticks.

Both findings reproduced in `/tmp/one-equals-one-feedback-rotation-red.xml`.
The controller now uses the valid evaluation result for `Makes n.` and uses
the same vertical/slash/horizontal/backslash order inside and outside. Full
PlayMode 19/19 passes, including equality spacing/target hiding after rotation.
Fresh browser comparison confirms both bank and slot first taps produce slash
(`rotation-bank-slash.png`, `rotation-slot-slash.png`); intentional wrong Round 35
shows `Makes 1.` against target 2 (`r35-new-feedback.png`). EditMode 19/19 passes, including alternate
equalities for the two revised precedence lessons.

P1 pickup hit area: bank roots had a layout cell but no raycast graphic, so only
the thin animated body accepted input. At Round 37, a touch 17 CSS pixels beside
the first stick ignored both tap and drag (`bank-hit-margin-before.png`,
`bank-hit-margin-drag-before.png`). A real GraphicRaycaster regression reproduced
the missing handler (`/tmp/one-equals-one-bank-hit-red.xml`). Added a transparent
raycast image to the existing cell, preserving appearance and cell spacing.
Full PlayMode 20/20 passes; fresh browser margin replay confirms a 19 CSS pixel
offset tap rotates and a drag places the stick (`bank-hit-margin-after.png`,
`bank-hit-margin-drag-after.png`). Dense bank cells still
need small-phone comfort review; this change does not assert native 44pt sizing.

## Remaining Work

Round 42's repeated constraints were reproduced in EditMode
(`/tmp/one-equals-one-repeat-red.xml`). Replaced its sample with `11 + 11 + 11`
(five slots, ten sticks, target 33), retaining its index/title and progress.
All 100 sample/resource/layout-plan checks pass and this budget does not duplicate
another round. Full EditMode passes; browser replay passes. This removes an identical adjacent puzzle,
not a claim that alternate solution spaces are disjoint.

Round 46 also reproduced as identical constraints
(`/tmp/one-equals-one-repeat-45-red.xml`). Replaced with `11 * 11` (three slots,
seven sticks, target 121): the same numerical product seen with six-stick `×`
in Round 38 now has a different material budget. All 100 samples validate;
full EditMode 21/21 passes and fresh QA WebGL build succeeds. Actual-input replay
of revised 42/46 passes, with replay ads suppressed. Recognition/answer policy is unchanged.

A: investigate 53-75/76-100 with
actual input; inspect the repeated-constraint candidates; small-screen controls,
failure/recovery, final clear, character and audio-state checks. Run affected
regressions and a fresh release build once the next candidate is ready.

B: human fun/difficulty feedback, actual phone touch/audio, production SDK
callbacks, signing and store submission remain separate and unverified.
This audit is paused at the user's request, not gameplay completion or ship-ready signoff.

New P1: `r50-small-filled.png` revealed footer commands approximately 24 CSS
pixels high, despite earlier picker-only 44px guarantees. Acceptance: footer
commands at least 44px high with readable 14px labels at 320x568; preserve board,
bank and footer bounds across all rounds. This is independent of native setup.
The red test measured 24px (`/tmp/one-equals-one-footer-red.xml`). Enlarged footer
buttons/labels now pass the full 21-case PlayMode suite, including all-round
layout bounds. The final release browser verification is recorded below.

## User-Requested Checkpoint (2026-09-13)

No further round investigation was started after the stop request. Finished only
the pending small-screen status typography: round/tutorial/counter text uses
28 logical pixels, result feedback 32, with matching reserved layout height.
The red test measured the original round label at 8.89 CSS pixels on 320x568
(`/tmp/one-equals-one-status-text-red.xml`). Full PlayMode now passes 22/22,
including minimum 12px status and 14px result text, 44px footer buttons and
all-round layout bounds. Full EditMode remains 21/21; all 100 samples and static
verification pass. Native/config warnings are unchanged.

Final release WebGL build and existing-build smoke pass without freshness
warnings. Five viewport smokes pass (320x568, 390x844, 430x932, 412x915,
1440x1024); captures are in `/tmp/one-equals-one-checkpoint-viewports`.
Small-phone, standard-phone, Android-ratio and desktop captures were visually
inspected for the changed status text/footer: readable, no overlap or clipping.
This final capture set covers the entry screen, not a new 100-round visual audit.
The local release preview is `http://127.0.0.1:8093/index.html`.
Final release actual-touch regression also passes all first ten rounds, their
ad-opportunity policies and persisted unlock/reload to Round 11. Evidence:
`/tmp/one-equals-one-checkpoint-input/input/results.json`. This uses the final
footer/status layout and no QA sample/round overrides. Test browser/server
sessions are closed; only the user-facing release preview remains running.

Resume with Round 53, then investigate the remaining 53-100 range and the
unreviewed 69/70 repeated-constraint candidate. Browser touch automation is not
human difficulty, listening or physical-device signoff. No commit, push or merge
was requested for this checkpoint.

## Resumed Round 53 Audit

The user resumed the loop after the checkpoint. HEAD remains `65578f4f`; prior
local changes are preserved. Release freshness smoke passes. QA WebGL rebuilt
to include the final status typography before the new investigation.
New evidence is `/tmp/one-equals-one-gameplay-resumed/trace.jsonl` and PNGs.
The QA query selects/unlocks 53 only; placements and rotations use touch events.
No new sample source was opened. A naked `1` below a completed equality initially
looked like an incorrect result, but code inspection confirms it is the most
recent recognized token, not an evaluation. Record as ambiguous feedback, not
a calculation bug. Human playtesting is still needed to judge its impact.

P1 recognition-label readability: placed-board captures (54/55/59) show tiny
recognition labels above otherwise readable silhouettes. Contract: keep slot
geometry, recognition and character appearance; labels at least 10 CSS pixels
at 320x568, fit their area, and do not intersect the stick area. The regression
initially skipped inactive empty-slot labels; corrected it to include inactive
labels and assert the exact slot count. It then reproduced 5.78px text before
the fix (`/tmp/one-equals-one-recognition-red.xml`). Set fixed 24-unit label font
with 34-unit header space, preserving the existing 42-unit stick inset.
Full PlayMode 23/23 passes (`/tmp/one-equals-one-resumed-playmode.xml`).
Fresh QA rebuild passes. `r64-placed-new-labels.png` and
`r64-small-new-labels.png` confirm readable `111`, `11`, slash and equals labels
on a populated nine-slot board; no observed glyph clipping or body overlap.
The padding remains compact, with unchanged character dimensions.

P2 repeated challenges: actual identical inputs confirmed 63/65 and 69/70.
Both pairs reproduced in focused EditMode tests before edits (files
`/tmp/one-equals-one-repeat-65-red.xml` and `...-repeat-70-red.xml`). Replaced 65
with `111 / 111 = 1 × 1` (seven slots, thirteen sticks) and 70 with
`111 / 1 = 111` (five slots, ten sticks), keeping names, indices, save keys and
alternative-answer rules. This changes player budgets instead of sample order;
it does not claim disjoint solution spaces or measured human difficulty.
All 100 samples/costs/layout plans and EditMode 23/23 pass. Browser replay of both
replacements passes (`r65-revised-placed.png`, `r70-revised-placed.png` plus trace),
preserving unlock 76 and suppressing replay ads. Other observed repetitions remain review candidates, not blanket
changes to the entire equality band.

Round 53-75 findings are not a claim of commercial puzzle variety. Several
budgets admit symmetric or identity-based solutions. Since free equality and
alternative answers are intentional rules, these paths are not rejected.
Human variety/fatigue evaluation remains distinct from these automated clears.

P2 feedback clarity: the unqualified last token below the board was mistaken
for a calculated value during the audit. With readable per-slot labels now in
place, remove that duplicate global token on successful placement/rotation.
Keep incomplete-shape guidance, check failures and calculated wrong-result text.
Red test reproduced the old global `1` (`/tmp/one-equals-one-token-feedback-red.xml`).
This changes presentation only, not live evaluation or token recognition.
Full PlayMode 24/24 passes, including retained failure feedback. QA rebuild
passes; browser switches to this build at Round 79 for populated-board review.
`r79-placed-new-feedback.png` confirms the cleaned global feedback; Round 80
confirms useful `Makes 110.` failure text still appears.

Rendering observation requiring follow-up: `r80-failure-new-feedback.png` lacks
the fixed target while the before-check frame and subsequent resized frame show
it. No runtime exception was logged. Round 81 wrong-result replay kept its target.
A fresh Round 80 replay with identical gestures kept target 89 at both 600ms and
later (`r80-repro-600ms.png`, `r80-repro-later.png`). Cause and persistence of the
original missing-target frame are not established; do not claim it fixed or
infer a calculation bug. Recheck after long sessions / on physical WebGL devices.

Current candidate verification: release WebGL build succeeds; first-ten actual
touch regression (including ads/progress reload) passes at
`/tmp/one-equals-one-resumed-release-input/input/results.json`; five viewport
smokes pass at `/tmp/one-equals-one-resumed-release-viewports`. Static suite and
freshness smoke pass with existing external setup/device warnings only.
Release preview restarted at `http://127.0.0.1:8093/index.html`.

P2 Round 87: after actual unit-operator repetition with 86, retain five slots
and ten sticks but change the sample to `1 + 11 * 11` (122). This recalls square
multiplication and gives a meaningful precedence distinction (left-to-right 132).
The focused precedence regression failed on the old sample
(`/tmp/one-equals-one-round87-red.xml`). All 100 samples/costs pass after editing.
The original sample was inspected only after the actual attempt; its operator
operands were reversed from the player expression. No new recognition restriction.

## User-Requested Checkpoint After Round 95 (2026-09-13)

Stopped new investigation at the user's request. Actual-touch coverage now
includes 53-95, in addition to the previous 11-52 pass. Round 96 was opened only;
96-100 and the final-clear/restart flow have not been investigated in this pass.
Only the already-modified Round 87 replay was finished during wrap-up: expression
`1 + 11 * 11`, result 122, highest unlock 96 preserved, replay ad ineligible.
Evidence: `r87-revised-placed.png`, `r87-revised-cleared.png`, and `trace.jsonl`
under `/tmp/one-equals-one-gameplay-resumed`.

The revised 87 populated capture also lacks the fixed target, which was visible
in `r87-revised-start.png`. This is a second observation, not just the earlier
Round 80 frame. Correct evaluation does not resolve the rendering issue. Cause
and exact reproduction conditions remain unknown. Keep it as an actionable
P1 investigation, not an external-configuration blocker or a fixed issue.

Final source verification: EditMode 27/27 and PlayMode 24/24 passed, with XML at
`/tmp/one-equals-one-resumed-editmode.xml` and
`/tmp/one-equals-one-resumed-playmode.xml`. All 100 sample/cost checks pass.
QA and release WebGL builds include the Round 87 change; final existing-build
freshness smoke passes. The first-ten touch regression and five viewport smokes
above ran before the last Round-87-only data edit; they are not claimed as a
rerun against the final artifact. Earlier static-suite results likewise precede
that final data edit. Browser checks do not establish physical-device quality.

Closed the isolated QA browser/server and removed only this pass's generated
scene-ID/ProjectSettings whitespace churn. Existing user changes remain intact.
Release preview remains at `http://127.0.0.1:8093/index.html`.
No commit, push or merge. This is a user-requested pause, not goal completion.

Next: reproduce and fix intermittent missing fixed-target rendering, then resume
actual input at Round 96 through 100 and inspect the ending/replay/restart flow.
Human puzzle-variety/fatigue, touch/listening, native devices, production SDKs,
credentials and submission checks remain separate unverified work.

## iOS Distribution Follow-Up: Font Refresh (2026-09-13)

The user requested iOS Archive/distribution plus feasible additional work. Kept
this follow-up limited to the known rendering observation; did not resume 96-100.

Fresh browser input on the old QA build reproduced the missing target after
placing `1` and `+` in Round 87 (`r87-plus.png`). A diagnostic build later showed
the target but omitted the recognition `+` label (`diagnostic-plus.png`). Correct
text state, active/cull state and subsequent reappearance suggest shared dynamic
font atlas/rebuild timing, but do not prove one specific Unity engine cause.

The controller now queues a refresh when its font texture is rebuilt, then
invalidates matching active Text generators/meshes in the next LateUpdate. It
does not scan/rebuild labels unconditionally each frame and unsubscribes on
destruction. Temporary diagnostic code is removed. No math, recognition or ad
policy changes were made.

Verification against the final fix:

- PlayMode 26/26 passes. Added placement glyph coverage and synthetic stale-mesh
  recovery coverage. The original focused Editor placement test passed before
  the fix; the failing browser capture, not that test, is reproduction evidence.
- All 100 sample/cost checks and a fresh QA WebGL build pass.
- Round 87 actual-input `1 + 11 * 11`: target 122 and recognition labels remain
  visible after placement and the round clears. No sample-fill hook used.
- Round 80 actual-input `111 - 11 / 11`: target 89 and `Makes 110.` remain visible
  after failure at 390x844 and 320x568. Rotate `/` to `-`, clear as 89 and reach
  81. Replay ad remains ineligible; highest unlock 96 is preserved.
- Native release export and signed Archive build `1.0.0 (2)` include this fix.
  This is not native runtime/rendering, production SDK or physical-touch signoff.

Screenshots, input trace and full PlayMode XML/log were copied to ignored local
`artifacts/one-equals-one/2026-09-13-font-refresh/`. The older temporary resumed
trace was no longer available during this follow-up; its historical claims were
not revalidated. The new evidence is independent. The QA browser/server is
closed; the release WebGL preview at 8093 still predates this font change.

Next: verify build 2 on a physical iPhone after Apple processing, including long
sessions, target/recognition labels, ads/consent and crash reporting. Resume
96-100/ending investigation only when the broader gameplay work is resumed.

## Post-Distribution Loop (2026-09-14)

Base: `d2abe1b4`. No commit, push, native upload or public-site deployment is
authorized in this pass. Evidence root:
`artifacts/one-equals-one/2026-09-14-post-distribution/`.

Video: supplied `ScreenRecording_09-14-2026 14-06-47_1.mp4`, duration 87.989s,
one video/audio track. Inspected timestamped frame samples at four-second
intervals and denser samples around interactions/end; not continuous playback
or subjective audio listening. Device/build identity is not visible.

- 0s: picker already shows cleared rounds and Round 22 available. This is not
  first-install evidence, even though Round 1 is selected afterward.
- 6-11s: Round 1 placement, removal and replacement; input intent is not visible,
  so repeated gestures alone do not prove a touch bug.
- 21/23s: Round 2 accepts adjacent numeral composition as 1111 and reports
  `Makes 1111.`; 25-31s show removal/rotation and eventual `+` composition.
- 64/68s: Round 8 target wraps `=` and `111` onto separate lines. Reproduced
  one-line contract failure in Editor (Round 6, logical safe width 390).
- 85/86/87s: Round 10 populated, success feedback, then Round 11. Sampled frames
  show no ad interruption, but these replay rounds cannot validate first-clear
  ad eligibility or production ad delivery.

Actual browser touch input, no sample-fill/source opened for these attempts:

| Round | Player expression | Result / observation |
| --- | --- | --- |
| 96 | `111 - 1 - 1 - 1` | 108; clear, next unlock persisted. |
| 97 | `11 + 11 = 11 + 11 * 1` | Equal sides 22; nine boxes and 18 sticks accepted. |
| 98 | `111 - 11 + 1 × 1` | 101; clear. |
| 99 | `111 - 11 - 1 × 1` | 99; clear. Similar to 98, but not a three-round duplicate run; no speculative replacement. |
| 100 | `1 + 1 - 1 × 1 / 1` | 1; title concept was already known from the brief. Finale feedback displayed. |

After Round 100: page 9 displays `100 Done`; Sound off survives reload, which
opens the picker. Re-enter/reset/re-solve preserves completion. First finale
log offers a milestone opportunity but `will_show=false` (no native ad in
WebGL); replay logs `eligible=false`, `reason=replay_round`. This is not a test
of native ad callbacks. Screens/input logs are under `input/`.

P1 recurrence: the original build-2-code QA session lost target `= 1` after
Round 100 placements, persisting through clear and a later frame. Resize
restored it. See `r100-filled.png`, `r100-completed-later.png`, and
`r100-completed-small.png`. Thus the prior vertex-only font repair was not
sufficient. Target values/evaluation were correct; rendering was not.

Changes under verification:

- Single-line target measurement inside existing bounds; no slot aspect change.
  Red test: `/tmp/one-equals-one-target-line-red.xml`; all 100 targets now pass
  at logical safe widths 1080/720/390/320.
- Refresh font material binding together with glyph vertices after atlas events.
  Red callback test: `/tmp/one-equals-one-font-material-red.xml`. This exposes
  missing material invalidation, not a direct proof of the engine's root cause.
- Idle atlas-settling test and full PlayMode 28/28 pass. This does not replace a
  native frame-time/allocation profile or long-session test.
- Store-capture build uses native reference scale; future candidates remain
  separate from files used for uploaded build 2.
- Public privacy page omitted this app's SDK usage; local website text amended,
  not deployed. Official disclosure guidance linked in store metadata.

Final verification and boundary:

- Final QA actual-input replay of 96-100 retains fixed targets and recognition
  labels through placement/clear. Round 100 also retains its target after idle,
  320x568 resize and return to 390x844. Round 8 displays `= 111` on one line.
  Evidence: `input/final-*.png` and `input/trace.jsonl`. Target repair includes
  both the single-line change and material refresh; no isolated engine-root-cause
  or indefinite-runtime claim is made.
- Fresh QA, store-capture and ordinary release WebGL builds pass. The ordinary
  build passes the first-ten touch-input regression, including saved-progress
  reload and milestone eligibility, plus five viewport smokes. Results under
  `release-input/input/results.json` and `release-viewports/`.
- 24 next-build screenshots (8 scenes x 3 sizes) pass dimensions/alpha/freshness
  and image-content checks. Inspected device contact sheets and full-size 8/75/
  100 candidates. Source: `Builds/AppStoreScreenshots/Candidates-next-build`;
  review sheets: `candidate-sheets/`. These use sample placement for capture,
  not actual-input evidence. Older `Candidates` files remain untouched.
- Basic static suite passes with setup/device warnings. Strict aggregate with
  the existing iOS environment fails on missing Android Firebase JSON and
  production Android ad IDs. Criteria were not weakened; strict log retained.
- Public privacy omission reproduced in browser; modified local Vite preview
  renders the `1 = 1` disclosures without horizontal overflow. Website build
  passes. Public deployment and store answers remain unchanged/unverified.
- `xcrun devicectl list devices --timeout 10`: no devices. No fresh native
  runtime, audio listening, consent/ads/crash-console test was possible.

Development scope in this loop is wrapped up, not a release approval. No new
round rules/data, monetization frequency, character parts, commit, push, native
upload or website deployment. Latest ordinary preview:
`http://127.0.0.1:8093/index.html`.

Next authorized action: prepare a new numbered iOS candidate (greater than 2),
verify target rendering and real consent/ad/crash flows on device, compare the
next-build screenshots, and deploy the privacy-page correction. Build 2 on
App Store Connect does not contain these local fixes. Owner review of store
privacy answers, age rating and actual processing/review state is still needed.

## 2026-09-18 Dominant Shortcut Follow-Up

Dominant-pattern review is now backed by a candidate search command instead of
manual one-off guesses:

```sh
node scripts/report-one-plus-one-minus-one-round-quality.mjs \
  --dominant-candidates 91 --samples 20000 --max-evaluations 40 \
  --max-results 8 --candidate-budget 1200 --allow-fewer-slots --allow-fewer-sticks
```

The first promising Round 91 candidate lowered the `/1` dominance but reused
the 8-slot/11-stick resource group and introduced an unresolved shared equality
witness with Round 31. That sample was rejected and the tool now filters nearby
resource candidates whose existing resource group has a proven equality witness.

Accepted Round 91 redesign:

- Before: `1 + 1 1 / 1 - 1`, target 11, 8 slots, 9 sticks.
- After: `1 1 1 - 1 + 1 1`, target 121, 8 slots, 9 sticks.
- Effect: Round 91 no longer appears in
  `dominantPatternReview.highPriorityRows`.
- The 8/9 resource group with Rounds 65/81/91 remains equality-exhausted, so
  this does not add the shared-equality problem seen in the rejected candidate.

The candidate ranking was then tightened with a `reviewScore` that penalizes
nearby candidates with very few accepted answers, large target jumps, and
resource drift. This keeps the report from over-promoting narrow one-off
answers that merely lower a shortcut ratio on paper.

Accepted Round 65 redesign:

- Before: `1 / 1 1 - 1 / 11`, target 0, 8 slots, 9 sticks.
- After: `1 - 1 1 / 11 - 1`, target -1, 8 slots, 9 sticks.
- Effect: Round 65 drops below the high-priority dominant-pattern threshold
  while preserving a fractional-cancellation idea and the existing slot/stick
  budget.
- The 8/9 resource group with Rounds 65/81/91 remains equality-exhausted after
  both 91 and 65 changes.

Verification:

- `node --test scripts/test-one-plus-one-minus-one-round-identity.mjs` passes
  52/52.
- `bash scripts/verify-one-plus-one-minus-one-static.sh` passes with the same
  expected stale-WebGL, production-config, and manual-device-QA warnings.

Next round-quality slice: the high-priority dominant-pattern list now starts
with Round 61 (`multiply-by-one` ratio about 0.716), followed by 73, 71, 98,
96, 97 and 63. Round 91 and Round 65 are closed for this specific dominant
shortcut issue, but the redesign remains static/Node evidence until a fresh
Unity/WebGL runtime build is available.

Immediate probes for Rounds 61, 73 and 71 did not produce an applied edit:

- Round 61's top candidates either moved into the already-used 8/9 resource
  group, reduced the stick budget substantially, or jumped to sparse high
  targets such as 91/99/101 with only a few accepted answers.
- Round 73's best-scored candidate had only two accepted answers; the broader
  candidates barely improved the shortcut ratio or reused the 71/65 resource
  neighborhoods.
- Round 71 has the same shape: the ratio can be lowered numerically, but the
  available candidates are either very narrow or effectively shift the round
  into another reviewed resource neighborhood.

This suggests the next productive step is not another hand edit from the same
candidate list. Improve the candidate generator to require a healthier minimum
solution count, penalize already-reviewed resource neighborhoods more strongly,
and prefer samples whose visible arithmetic introduces a different player
decision instead of just moving the target.

The candidate report now marks such rows as `analysisOnly` instead of presenting
them as clean recommendations. A candidate becomes analysis-only when it has
fewer than the minimum recommended accepted-answer count or shares a slot/stick
budget with an already-reviewed resource neighborhood. The rows remain visible
so unusual but mathematically interesting ideas are not hidden, but regular
sorting now prefers broader, less entangled candidates first.

The same report now also records `visibleDiversityScore` and
`minRecommendedImprovement`. Candidates whose visible sample arithmetic barely
changes the round, or whose dominant-pattern improvement is below 0.05, remain
visible but are marked analysis-only. Re-running Round 73 after this change
classifies all eight sampled candidates as analysis-only: the two broadest
candidates improve the dominant pattern by only about 0.004 and 0.020, while the
stronger ratio changes are too narrow or reuse the 71/65/81/91 neighborhoods.

Additional focused probes:

- Round 63: all returned candidates are analysis-only. The strong-ratio
  candidates have only one or two accepted answers; the broader candidate
  reuses the reviewed 65/81/91 slot/stick neighborhood.
- Round 98: all returned candidates are analysis-only. The candidates that
  remove `multiply-by-one` dominance are narrow or reuse 71, 59/84, or
  65/81/91 resource neighborhoods.
- Round 96: only one candidate appeared in the bounded pass, and it has two
  accepted answers plus the reviewed Round 71 slot/stick neighborhood.
- Round 97: the 9-slot/18-stick equality-echo candidate search is too expensive
  for the current loop; a 60-evaluation/900-candidate run exceeded two minutes
  and was interrupted. Future 97 work should use a narrower equality-specific
  generator instead of the general dominant-candidate pass.

Current conclusion: the remaining high-priority rows are not clean one-line
data edits. Further progress likely requires a smarter generator that searches
for multi-pattern combinations directly, rather than nearby resource variants
that merely lower one ratio.

The candidate report now emits `combinationTags` and `combinationScore` for the
visible sample itself. Tags include cues such as `packed-number`,
`triple-number`, `operator-mix`, `nontrivial-division`,
`nontrivial-multiply`, nontrivial self-cancellation and non-echo equality. This
lets review distinguish "looks like a combined idea" from "actually playable
and distinct enough." For example, Round 63 candidates can have strong
combination tags, but they remain analysis-only when they have only one or two
accepted answers or reuse the 65/81/91 resource neighborhood.

Round 97 follow-up: added a bounded equality-specific candidate search that
pairs exact-equal left/right side expressions and filters out visible
same-expression samples before scoring candidates. This avoids the earlier
multi-minute general search path. A focused pass for Round 97 completed cleanly
with `maxEvaluations=120`, `maxSideExpressions=240`, and `maxResourceDelta=1`,
but found no replacement candidates in that budget. Treat Round 97 as requiring
either a wider search with explicit time budgeting or a hand-designed late
equality puzzle rather than a quick data tweak.
