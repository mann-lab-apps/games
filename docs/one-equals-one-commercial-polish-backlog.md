# 1 = 1 Commercial Polish Backlog

This document tracks issues found while pushing `1 = 1` from prototype quality
toward a commercial casual puzzle release.

## Current Gate

### Default `= 1` Mode Pattern Gate (2026-09-26)

Current first-run/default content is the 30-round fixed-target `= 1` mode; the
100-round goal set is hidden legacy content. The round-quality report now
supports `--make-one`, giving the default mode its own shortcut-pattern gate.
The first run found late standalone `N / N = 1` review rows after Round 10 at
11, 13, 14, 15, 19, 20, 22, 23, 25 and 26. These were redesigned without token
bans or forced answers, and duplicate sample strings in the default ladder were
removed.

Current default-mode evidence:

- `node scripts/report-one-plus-one-minus-one-round-quality.mjs --make-one --patterns --summary --strict-patterns --max-nodes 200000 --max-solutions 1000`
  exits 0 with no late pure self-division violations.
  The summary now includes an `incompleteReview` block: Rounds 20, 22, 23 and
  29 are still bounded `node_budget` rows at the current search budget, so their
  pattern ratios are prefix evidence rather than exhaustive coverage.
  The same summary now uses a default-mode review window after Round 10 for
  dominant shortcut families. Round 24 was redesigned from
  `1 1 - 1 1 + 1 / 1` to `1 1 11 - 1 111 + 1`, reducing the high-ratio
  divide-by-one pressure. Round 18 was then redesigned from
  `1 - 1 + 1 / 1` to `1 1 - 11 + 1`, removing the remaining high-priority
  default-mode dominant-pattern row under the current bounded report.
  The same summary now exposes a `resourceReview` block so repeated slot/stick
  groups are visible in JSON automation instead of only in the text duplicate
  report.
- `--make-one --dominant-candidates` preserves target `1` automatically, so
  helper output cannot recommend target-changing replacements for the fixed
  `= 1` mode.
- `--make-one --resource-candidates` now searches target-preserving replacements
  for repeated slot/stick groups. It accepts either explicit round numbers or a
  repeated resource key such as `5/8`, includes a small composite target-1
  candidate generator, and marks candidates that would reintroduce late pure
  `N/N`, admit pure `N/N` through their resource budget, depend on bounded
  pattern evidence, visibly present an authored shortcut sample after the
  learning window, or swap one universal key for another dominant shortcut.
  Composite candidates no longer consume the nearby-enumeration candidate
  budget; the report now exposes `compositeCandidateCount` and
  `enumeratedCandidateCount` so capped probes show both search sources. The
  evaluation pass also keeps at least one candidate from each generated source
  when a small `--max-evaluations` budget would otherwise hide nearby resource
  enumeration behind composite target-1 forms, and `candidateSummary.bestBySource`
  keeps the strongest evaluated example from each source visible even when
  `maxResults` is small. Occupied resource budgets now appear as
  `occupied-resource-swap` analysis-only candidates, so future redesign can plan
  swaps across existing rounds without confusing them with direct replacements.
  Candidate summaries also include an `authoredSampleShortcut` risk count, so
  samples like `A - A + 1`, `A + B - A`, `A × B - A × B + 1` or `A / B × B` do
  not remain recommendable merely because their full answer set is not dominated
  by a single shortcut family.
  The
  latest capped probes over the repeated-resource groups produced one later
  manual-quality replacement for Round 29, reducing a repeated resource group.
A small authored-path edit was still applied to Round 26: it now uses
`111 - 11 × 11 + 11` instead of the visible `N/N * N/N` chain. This preserves
the 7-slot/14-stick resource budget and accepted-answer set, so it is not a
full structural duplicate fix, but it moves the intended solution toward the
`11 × 11 - 111` multiplication-offset gimmick without adding bans or forced
answers.
- Two more authored-path edits reduce the old visible self-division/subtraction
  samples without changing resource budgets: Round 25 now uses
  `1 / 1 1 1 × 111` instead of `1 + 11 / 11 - 1`, and Round 30 now closes with
  `1 / 111 111 × 111 111` instead of `111 / 111 + 111 - 111`. The stricter
  pattern classifier now keeps both rows visible as reciprocal-cancellation
  review material, so they are not considered structurally solved.
- Round 29 now uses `1 1 + 111 - 11 × 11` instead of `11 + 1 - 1 1`, moving it
  out of the `6/8` repeated resource group. The current MakeOne summary has 22
  distinct slot/stick resources, four repeated resource groups and 13 unresolved
  found shared-equality pairs.
- Pattern reports now expose `shortcutPolicy.authoredSampleReview`, separating
  visible late-round authored shortcut samples from accepted-answer-set review.
  It covers simple and compound additive cancellation plus reciprocal
  cancellation, as well as the older self-division, divide-by-one,
  multiply-by-one, self-subtraction and equality shortcuts, so future Round
  26-style edits can be found without constraining alternate valid solutions.
- Resource candidate reports also flag `highEqualityEcho` when a candidate's
  accepted-answer evidence is dominated by same-expression equality. This kept
  the tempting Round 30 candidate `11 * 111 - 11 * 111 + 1` out of the
  recommendable list even though it removes visible `N/N`, because it would trade
  one universal-key pattern for another under the bounded review map.
- `node scripts/verify-one-plus-one-minus-one-rounds.mjs` passes.
- `node --test scripts/test-one-plus-one-minus-one-round-identity.mjs` passes
  61/61, including the MakeOne pattern-regression, target-preserving
  candidate-search tests, resource-repeat candidate report and single-sample
  pattern evaluation CLI.
- Unity EditMode passes 63/63 after removing stale 100-round identity
  expectations that no longer match the shared Node redesign list.
- Unity PlayMode passes 41/41 after isolating MakeOne progress keys, explicitly
  selecting the legacy Goal ladder for legacy 100-round regressions and
  increasing footer command button size/readability for small-phone touch
  targets.
- Fresh QA WebGL and ordinary WebGL builds passed before the later Round 18
  data change and resource-review tooling change. The current source is newer
  than that WebGL build, so the static suite is expected to warn about WebGL
  freshness until Unity is responsive enough to rebuild.
- Round 48 browser input regression passes on the fresh QA build using
  `qaMode=goal`, so the hidden 100-round ladder can still be targeted even
  though the default app mode is now the 30-round `= 1` ladder.
- Legacy 100-round strict quality currently reports no identical
  resource/target groups; the only proven shared-equality witness in the current
  report is the intentional early tutorial echo 2/5.

Open review items: the default ladder now has 22 distinct slot/stick resources
across 30 rounds after Round 23 moved out of the old `11/20` repeated-resource
pair and Round 29 moved out of the `6/8` group. Rounds 20/22/23/29 are still
bounded pattern-review rows rather than
exhaustively classified rows, and `resourceReview` currently finds 13
unresolved shared-equality pairs across repeated resource groups. The new
`--evaluate-sample` CLI can inspect hand-authored replacements before data
edits. Round 18's replacement reduces the dominant self-division ratio from
`0.615` to `0.286`; Round 23 now uses `1 / 11 * 11 - 1 + 1`, which completed
under a higher `5000000` node check with a `0.442` dominant shortcut ratio and
`0.103` same-expression ratio. Under the normal bounded MakeOne summary, the
default ladder now has no high-priority same-expression or dominant-pattern
rows. Future tuning should favor similarly distinct visible judgments over
merely extending the same cancellation patterns. The next useful tooling step is
not to auto-apply the current composite candidates, but to generate richer
target-1 candidates whose complete accepted-answer map is not dominated by
`/1`, same-expression equality or pure `N/N`. The latest capped probes for the
remaining repeated-resource groups show useful bounded candidates such as
`1 - 1 / 111 * 111 + 1`, and now confirm nearby enumeration is being sampled
and evaluated alongside composite target-1 forms, but no additional post-tutorial
`recommendableCount > 0` structural data edit yet. Round 26's manual-quality
edit remains useful because the replacement sample is target-correct, complete
under the evaluator, and no longer teaches pure self-division as the authored
path.

### Universal Shortcut Pattern Tightening (2026-09-18)

The round-quality tooling now has a separate `--patterns --strict-patterns`
design gate for broad shortcut families. It is intentionally stricter than the
regular release `--strict` report and currently targets late standalone
`N / N = 1` solutions that can behave like a universal key.

Late pure self-division candidates initially appeared in 52, 59, 65 and 100.
All four were redesigned without token bans or forced samples: 52 now uses
`1 1 1 - 1`, 59 uses `111 * 11 - 11 11`, 65 uses
`1 - 1 1 / 11 - 1`, and 100 uses `11 × 11 - 111 + 1`. Static round
verification, the regular strict quality report and the strict pattern gate pass
after these edits. Round 100 is no longer the identical 30/100 title callback;
it now acts as a finale that revisits the `11 × 11 - 111` ten-making trick and
closes at target 11.

The same pattern report also lists legal late same-expression/same-number
equality echo candidates as review rows. The first cleanup changed 53 from
`111 = 111` to `111 / 1`, 61 from `1 + 1 + 1 = 1 + 1 + 1` to
`11 / 11 + 1 + 1`, and 78 from `111 - 11 - 1 = 111 - 11 - 1` to
`111 × 111 - 111 1 × 11`. These edits remove the most obvious copied-equation
samples without creating new shared equality witnesses. Remaining review rows:
54, 55, 56, 57, 58, 60, 63, 64, 68, 69, 72, 73, 79, 87, 88, 93 and 97. These
answers remain valid; the list exists so playtesting can decide whether
equality echoes are becoming another universal key.

Follow-up cleanup removed the remaining sample-level equality echoes: 56 now
uses `11 * 11 / 11 + 1 1`, and 69 now uses `1 11 * 1`. The strict pattern
report now has no late `sampleEchoRows`; the remaining alternate-only review
rows are 54, 55, 56, 57, 58, 60, 63, 64, 68, 72, 73, 79, 87, 88, 93 and 97.
The report ranks 97 and 64 as the current high-priority alternate-only echo
rows. Initial replacement candidates for these two created new shared equality
witnesses, so leave them as review targets until a candidate improves both the
ratio and the shared-witness map.
Follow-up six-seed deterministic candidate search found no conflict-free 64/97
replacement that also lowers their same-expression ratio, so do not churn these
two with nearby arithmetic variants unless a stronger candidate appears.

Next-pattern-review command:
`node scripts/report-one-plus-one-minus-one-round-quality.mjs --patterns
--summary --strict-patterns --max-nodes 200000 --max-solutions 1000`.
Use the full `--patterns` report only when examples for a specific round are
needed.

The compact report now broadens the map beyond pure `N / N` through
`dominantPatternReview`. Current high-priority arithmetic-pattern review rows
include 61, 73, 71, 98, 96 and 63, plus the already-known 97 equality echo.
Rounds 91 and 65 were removed from that high-priority list by the follow-up
dominant-pattern pass. These are design review targets, not token bans; improve
them only when a candidate reduces the dominant pattern without creating a
broader shared-answer or equality-echo problem.

A first quick candidate probe for the strongest row, 91, reused the existing
bounded candidate pool and direct pattern-ratio scoring. The old candidate pool
is tuned for shared-equality resource replacement, so it produced no suitable
dominant-pattern replacement for 91/61/73/71/98/96/65/63. Next implementation
work should add a dedicated dominant-pattern candidate generator/evaluator
rather than hand-editing one of these rounds from the equality-focused pool.

That dedicated path now exists. It closed 91 and 65, and the next tool-quality
slice adds `analysisOnly` notes for candidates with too few accepted answers,
already-reviewed slot/stick neighborhoods, low visible-arithmetic diversity, or
less than 0.05 dominant-pattern improvement. This prevents the report from
presenting tiny mathematical ratio changes as production-ready round redesigns.

### Numerical Correctness / Identity Batch 5 (2026-09-16)

P1 numeric correctness is fixed in source and Node mirror: solve/equality
acceptance now uses exact rational arithmetic rather than the old 0.0001
acceptance tolerance. Round 48 no longer accepts `1 / 11 111` as target zero;
the wrong division arrays are gone from its complete canonical solution set.
Fraction cancellation and equal fractional equations are covered by new Unity
and Node regressions. Display formatting now exposes small nonzero results
instead of printing them as `0`.

All 12 non-callback reuse pairs were redesigned in source: 26, 71, 72, 73, 76,
77, 92, 93, 94, 95, 96 and 99. Node identity evidence now shows 72 unique
resource pairs, one intentional identical group (30/100), and only 2 proven
equality-sharing pairs total. The remaining sharing is intentional: 2/5 as the
early tutorial echo and 30/100 as the title callback. The round quality report
now labels those as intentional instead of warning on them as unresolved reuse.
This paragraph is superseded for the 30/100 callback by the 2026-09-18 shortcut
pass above: Round 100 no longer shares the Round 30 resource/target callback.
Do not treat this as full ship-ready completion until Unity/runtime/WebGL
verification is healthy.

Historical 2026-09-15 verification, superseded by the 2026-09-26 current gate
above for PlayMode/WebGL freshness: Node identity tests pass 49/49; static
100-round verification
passes; selected solution enumeration for 11/33/48/83 passes with Round 48 at
4 complete canonical arrays. Full static verification passes with a stale-WebGL
warning because the player build predates these source edits. After clearing a
stale Unity licensing child process, Unity EditMode passes 61/61 on the latest
source. PlayMode still could not reach game tests because Unity batchmode
licensing timed out waiting for the 6000.3.23 licensing channel. QA/ordinary/
store-capture WebGL scripts, the PlayMode wrapper, and iOS/Android readiness
scripts now distinguish empty license data from an unavailable licensing
service through the shared
`scripts/lib-one-plus-one-minus-one-unity-license.sh` helper: they may use the
local entitlement file for the former, but fast-fail on
`LICENSING_CLIENT_UNAVAILABLE`. Current PlayMode, QA WebGL, ordinary WebGL,
store-capture WebGL, and native readiness ad-test verification exit with `Unity
licensing client is unavailable. Open Unity Hub or repair its licensing service
before running this script.` The standalone
`scripts/check-one-plus-one-minus-one-unity-license.sh` diagnostic reports the
same current blocker and should be the first retry command. This is an
environment blocker, not a game assertion failure. Required next evidence:
Unity Hub license activation/repair, successful PlayMode rerun, fresh WebGL
build, and browser input check that Round 48 rejects `1 / 11 111` without
advancing progress.

Follow-up hardening: iOS/Android readiness now use the same license helper, and
the ship-ready gate performs one license preflight before skipping Unity-gated
checks. A latest ship-ready run fails clearly on that preflight, stale WebGL
artifacts, strict release env, strict device QA, and strict Firebase/AdMob
config. When artifact freshness fails, viewport smoke, QA captures and App Store
candidate screenshot gates now skip instead of producing stale-build evidence.
Standalone WebGL viewport smoke passes outside the sandbox and the generated
iPhone SE/standard/large, Android 20:9 and desktop captures are available under
`/tmp/one-equals-one-webgl-viewports/`; this is still older-build evidence, not
runtime verification for the numeric fix.

The top-level ship-ready control flow now has a no-Unity fixture test. It
asserts that `LICENSING_CLIENT_UNAVAILABLE` skips PlayMode/WebGL/native full
builds, stale WebGL artifacts skip viewport/capture evidence, and strict
iOS/Android `--preflight-only` checks still run so platform blockers remain
visible while Unity is down. The aggregate gate also prints a concise final
blocker summary so a long failed run ends with the actionable failed/skipped
check list instead of burying it in the full log. The same fixture also covers
the fresh-artifact/available-license path, asserting PlayMode, fresh WebGL,
viewport/capture checks and full native readiness all run before the aggregate
can pass.

Additional source hardening: tiny nonzero exact-rational results now fall back
to fraction text if the compact decimal formatter would display `0`. This keeps
future zero-target failures from saying `Result is 0.` for nonzero values such
as `1 / 111 111 111`. The controller's visible wrong-result feedback now reuses
the exact solve reason as well, preventing the separate `Makes 0.` fallback path
for tiny nonzero misses. The new EditMode/PlayMode regressions are present in
source but still await Unity execution while licensing is unavailable.
Release-safety verification now also guards iOS/Android native build entrypoints
so release builds remain non-development and production AdMob requirements do
not drift into the explicit test-ad build paths.

### Batch 4 Runtime Gate Closed (2026-09-15)

Same reviewed retry succeeds: PlayMode 37/37; QA/ordinary/store-capture WebGL
rebuilt; 12 changed/neighbor samples and four small-screen inputs pass. First
ten touch/cancel/save/reload checks and five ordinary startup viewports pass.
Static passes without old-build freshness warnings. Previous approval-service
block below is historical, not a current environment blocker.

P1 numerical bug now reproduced through actual WebGL touch: Round 48 clears
`1 / 11 111` as zero and advances. Harness PASS only proves reproduction.
Fix correct numerical comparison with fraction/cancellation regressions before
closing this issue; no division ban or round-specific workaround. After that,
recompute inventories and continue the 12 non-callback shared-pair redesigns.
No further round data changed in this retry. Full objective remains unfinished.
See the latest gameplay audit and `batch4-tolerance/results.json` for evidence.

### Execution Block (2026-09-15)

Fourth same-command PlayMode request failed in approval review before launch,
across three consecutive affected goal turns. Goal is blocked, not complete.
Previous independent Node inventory work remains valid by rules-source hash;
no current batch 4 player evidence exists. Restore approval service/model and
run the recorded normal PlayMode command, then fresh build/input QA before
more data changes. The 12 non-callback shared pairs and numerical tolerance
risk remain open. No rule changes or verification waivers close this blocker.

### Solution Inventory Follow-Up (2026-09-15)

- Node tooling now passes 32/32, including bounded full enumeration, unpruned
  small-board comparison, input immutability and CLI completeness/error cases.
  No additional runtime/data edits were made. Inventory completes 98 rounds;
  61/78 remain limited, explicitly not counted as fully explored.
- P1 numerical correctness candidate: Round 48 accepts `1 / 11 111` at target
  zero under existing 0.0001 tolerance. Observed in the Node evaluator and
  matched threshold in C# source, not yet native/input reproduced. Acceptance
  criteria: distinguish this nonzero rational value from zero without rejecting
  correct fractional expressions, preserve Node/native parity, and recheck
  sample/cross-answer sets after any numerical change. No symbol restrictions.
- P2 answer reuse remains material: 64/76 share 2,710 canonical arrays, 22/95
  share 167, and 25/96 share 14. Both sets for each comparison are complete;
  array counts include numeral packing and operator spelling, not human
  strategies. Keep all 12 non-callback pairs open for evidenced redesign.
- Approval service rejected the same PlayMode request again before starting.
  Three requests across two affected goal turns; no bypass. Fresh batch 4
  player/input checks still precede any further round-data changes.
- Evidence and reproduction CLI: latest gameplay audit and
  `batch4-solution-summary.json` in the existing identity evidence folder.

### Round Identity Batch 4 (2026-09-15, Runtime Verification Pending)

Applied 11/33/48 together, then 83's shorter target-101 construction using
neighboring 11 tokens. Source now has 49 cumulative changed indices, 14 shared
equality pairs, 60 resource pairs, mean slots 5.86 / sticks 9.00. No new rules,
token bans, save migration or ad changes. Exact duplicate exception stays 30/100.
EditMode 47/47, 100 samples and native/Node 10,000-pair parity pass after recorded
red tests. Node 26/26 includes a promoted reproducible candidate search and a
new regression for truncated piped JSON; the report now flushes before exit.

P1 verification gap: PlayMode launch was rejected twice before process creation
because the approval service's model is at capacity. Same-script review/retry
was used, not an indirect execution route. No fresh WebGL or input evidence for
these four edits exists. The running preview still has batch 3's 47-index / 18
pair candidate, proven by unchanged binary hashes. Do not call batch 4 verified.

Resume: same PlayMode command, QA build, 12 changed/neighbor inputs, small-screen
11/33/48/83, ordinary/capture builds and save/viewport regression. Further data
edits wait until this runtime gap is closed. Meanwhile candidate suggestions
are reproducible with `report-one-plus-one-minus-one-round-quality.mjs
--candidates --samples 600000 --seed 15092026`; they never edit rounds directly.
The remaining 12 non-callback shared pairs remain open, not intentional-repeat
exceptions. Evidence and exact pending commands are in the latest audit.

### Round Identity Batch 3 (2026-09-15)

Verified changes: 35/43/57/75/88, with 35/43 moved together to avoid a new
duplicate. Shared equality pairs 22 -> 18; sample transfers 18 -> 14; resource
pairs 57 -> 60; cumulative changed indices 47. Mean slots 5.88, mean sticks
9.00, maximum 18. Exact duplicate exception remains only the title's 30/100.

The actual rules API lacked slot-length validation even though the controller
fixed the board length. Two equal-cost/wrong-length regressions fail 0/2, then
pass with the guard; the 10,000-pair native matrix now calls the rules API with
no external length filter. Final EditMode 43/43, PlayMode 37/37, Node 17/17,
100 data, parity and static gates pass. All three WebGL variants are fresh.

Actual input: five revised samples plus ten neighbors at 390x844, and five
alternate answers at 320x568. A small-screen connected-outline detector failure
is preserved and fixed in the temporary harness via separate fill interiors;
same game binary passes the repeated inputs. First-ten/save and five ordinary
viewports also pass. Evidence prefix: `batch3-*` in the existing identity folder.
The gameplay audit details caveats and images. Native build 2 is unchanged.

Open P2: 16 resource groups still yield 18 shared pairs, not an approved-repeat
list. Keeping only 2/5 and 30/100 recurrence leaves at least 15 resource moves.
Next investigate the coupled 11/33/48 candidate recorded in the audit; it avoids
making target-9 subtraction longer or padding with division by one. Its
18 -> 15 projection is analysis only, not applied or play-verified. Continue
with failing native/Node tests and neighboring input before accepting it.

### Round Identity Batch 2 (2026-09-15)

The next safe action was candidate exploration, not waiting for a recurrence
preference. Five more resource changes (38/39/52/70/90) reduce shared equality
pairs 27 -> 22 and directed sample transfers 23 -> 18. Cumulative changed
indices: 44. Mean slots/sticks decline to 5.88/8.94; maximum stays 18 sticks.
The 30/100 callback remains the only exact duplicate group. This does not prove
the remaining 22 shared pairs are resolved or approved as repetition.

New native regressions reproduce all five old shared answers (31/36 red), then
pass 35/35 after resource changes and replacement of the obsolete Round 70
equality fixture. Its unchanged owner 87 still accepts that equality and keeps
its precedence lesson. Node 12/12 and native/Node 10,000-pair parity pass.
PlayMode exposed a hardcoded old target in the display-restoration test (36/37);
derive the expected target from the selected round, assert it before and after
the same hide/show interaction, and the full suite passes 37/37.

Fresh QA touch passes all five changed samples plus eight neighbors. At 320x568
all five alternative answers pass actual touch as well, with inspected filled
captures. No sample-fill hook is used by these tests. Current evidence:
`artifacts/one-equals-one/2026-09-15-round-identity/batch2-*`.
Fresh QA/ordinary/capture builds and final static gate pass. Ordinary startup
renders at five sizes; small/desktop boundaries were inspected. The
previous section's 27 pairs/40 indices are historical, not the current state.

Open P2 next: prioritize 16/43, 21/33 and 82/88 shared-resource pairs against
the candidate pool. Do not choose division-by-one chains or longer layouts
merely to remove a count. Preserve each band's symbols and meaningful operator
precedence. No symbol bans, new rules, save migration or release is authorized.

### Round Identity Redesign (2026-09-15, Verified Candidate / Reuse Open)

Contract: retain 100 indices, unrestricted token recognition, alternate answers,
save keys and ad policy. Remove unexplained identical resource/target constraints,
review cross-round equality witnesses and verify changed rounds through actual
input. Do not claim all answer sets are different from sample-only comparisons.
Baseline: 38 resource pairs, ten identical resource/target groups. The old quality
report classified equality samples as a separate mode and did not fail duplicates.
Shared Node evaluation now mirrors the actual numerical fallback/equality rule;
bounded equality search distinguishes found, exhausted and limited results.

Acceptance: sample cross-matrix and non-sample witnesses; unexplained identical
constraints fail strict checks; Unity parity and all samples pass; changed inputs,
layout, save and end-flow are rechecked. New six-slot samples exposed a real
height-only aspect clamp (ratio 1.088 at width 390); a Unity regression is added.
No additional rule or native deployment is authorized by this work.

Implemented: 40 changed indices, 56 resource pairs (was 38), one exact duplicate
group (explicit 30/100 title callback, was ten), 27 common-equality pairs (was
158). The only near pair left is tutorial 2/5. Distant sharing is NOT signed off
as intentional review merely because the blocking gates pass. A new explicit
warning keeps it visible. Each group is recorded in `remaining-reuse-review.json`
under `artifacts/one-equals-one/2026-09-15-round-identity/`.

EditMode 31/31, PlayMode 37/37, 10,000 Unity/Node sample pairs, 100 data samples,
static and all three WebGL builds pass. Actual input evidence matches every
changed final sample, plus neighbors, first-ten/save and small-screen 34/84/100.
The separate end-state fixture preserves Done/Sound off across reload and Reset.
See the gameplay audit for binary versions, sample-fill boundaries and counts.

Open P2: 25 resource groups still share equality witnesses. Target-only changes
cannot fix them under the actual rule. Removing all sharing except tutorial
2/5 and title 30/100 requires at least 24 more resource reassignments. This is
not proof of impossibility: select further candidates only with a judgment/
placement-cost rationale, not to turn the metric green. User preference for
strict reuse removal versus deliberate recurrence is still unanswered. Do not
call the overall identity goal complete or quietly whitelist these groups.

### Post-Checkpoint Resume (2026-09-15)

Base `26d8508a`; authorized Unity execution now works. The pending 36 PlayMode
tests and 29 EditMode tests pass. Fresh QA input verifies all three `111`
returns/transfers, real hidden-tab drag cancellation, pending-tap rejection,
fresh rotation and full-slot rejection. Ordinary first-ten/save replay passes.
These results precede the additional pointer fix below.

| Priority | Reproduction / impact | Acceptance | Evidence / status |
| --- | --- | --- | --- |
| P1 | Finger 1 presses, pause/resume occurs, finger 2 presses the same view, then finger 1's delayed click arrives. The shared press version now matches and rotates unexpectedly. | Reject the older pointer's click/drag without poisoning the fresh pointer's valid tap. | New controller-event test fails with CenterSlash instead of CenterVertical (36/37). Track press pointer ID and check ownership before changing drag state. Full 37/37 pass. Fixed-binary browser touch trace/screenshots confirm only the fresh finger rotates; native suspension remains unverified. |
| P2 QA | Browser replay sends a nonempty touchEnd when trying to release only one of two fingers, contrary to the CDP contract. | Use an active-set touchMove to remove the older finger and an empty touchEnd for the final release. | Corrected first-ten/save browser regression passes; isolated two-finger trace verifies both release IDs and visible before/after poses. |

Evidence: `artifacts/one-equals-one/2026-09-15-post-checkpoint/` test XML and
`/tmp/one-equals-one-sept15-live/` initial browser trace/captures. The first
session ended intentionally after the new issue was reproduced, with the last
sample at 340.909 seconds. It is not a completed 30-minute final-binary soak.

Final verification: QA/ordinary/capture builds, corrected first-ten/save input,
five inspected viewport captures, static checks and 100-round data pass. The
final measured session lasts 1818.709 seconds, with 66 observations and no
captured runtime exception. All three return/transfer paths, full-slot rejection,
96-100 actual placement, valid alternative equality, hard-clear/replay ad
exclusion and final completion pass. Reload retains page 9 Done and Sound off.
Evidence and measured browser-only limits are in the current gameplay audit.
No further reproduced local P0/P1 in this investigated scope. Native suspension,
production ad return/console evidence, listening and human pacing remain open;
the next useful step is an authorized native candidate, not another unchanged
browser run. No native release approval or deployment is implied.

### Browser Verification Reliability (2026-09-14 Resume)

Unity execution was rejected before startup again by the approval service
(`Selected model is at capacity`). No alternate Unity launch or approval bypass
was attempted. Independent Node checks exposed verification defects:

| Priority | Reproduction / impact | Acceptance | Evidence / status |
| --- | --- | --- | --- |
| P1 QA | CDP client discarded `Runtime.exceptionThrown`, although the smoke failure gate inspects it. Browser exceptions could be missed. | Preserve exception events and fail the existing gate. | Injected-event regression failed before the extraction/fix; now passes. |
| P1 QA | Missing CDP replies or a disconnected browser leave commands awaiting forever. | Bound connection/command waits, reject pending commands on close/error, ignore late replies. | Fake-WebSocket tests cover timeout, response IDs, disconnect, explicit close and send failure. No fresh Chrome replay claimed. |
| P2 QA | Capturing an input-test failure throws, hiding the original error and preventing `results.json`. | Preserve original failure; record screenshot failure separately. | Red test returned `Capture unavailable` instead of `original failure`; fixed test preserves both in the report. |

`node --test scripts/test-one-plus-one-minus-one-cdp-client.mjs` passes 9 tests.
The viewport bridge test and 100-round data/strict quality report also pass.
The CDP tests now run in static verification. These are isolated runner tests,
not Unity, fresh WebGL, native device or 30-minute soak evidence. Next executable
game gate remains full PlayMode followed by a new QA binary and real tab-switch
and triple-stick-removal replay. Do not mark the active gameplay goal complete.

Continuation: auto-review rejected the unchanged Unity retry before launch,
citing the prior approval-capacity failure. No fallback execution was attempted.
Coverage review found the triple-stick regression only invoked the internal
removal function. Added a separate controller-drag test covering all three
indices, both outside return and transfer to another slot, remaining `11`,
destination `1`, total/bank counts and ghost/source cleanup. This new Unity test
is NOT RUN; the previous 34/35 execution is unchanged. Do not confuse this
controller-event fixture with an actual browser/touch replay.

### Long-Session Investigation (2026-09-14, Verification Blocked)

This follow-up continues beyond the preceding checkpoint. Evidence root:
`artifacts/one-equals-one/2026-09-14-long-session/`.

| Priority | Reproduction / impact | Acceptance | Evidence / status |
| --- | --- | --- | --- |
| P2 | Forty bank rotations plus eight resets retain 245 animation-registry entries for only five live faces. Dead entries add iteration work and dilute blink selection during same-round play. | Unregister descendants before detaching/destroying visuals; repeated refresh preserves existing visible faces and does not retain dead roots. | Red PlayMode case reproduces 245/5; fixed case remains 5/5. All 33 PlayMode tests pass, including repeated sample refresh across character combinations. This is a managed registry finding, not a measured native byte-leak claim. |
| P2 | Round 59 `11 / 1 = 11 / 11` logs calculated sides but displays only `Not balanced.`. | Show `11 is not 1.` without advancing, unlocking, or revealing a solution. | Before browser capture and failing controller assertion; preserve the evaluator's existing numeric comparison. Editor and fresh browser replay at 390/320 widths pass. |
| P2 | Round 60 accepts the exact Round 57 arrangement, three rounds later, with the same 14 sticks / seven slots / equality constraints. | Change the reviewed repetition's resources, retain 100 indices and alternate-answer rules, validate sample plus an alternative. | Identical actual-input clears captured. Round 60 now `Small Chorus`, five slots / 12 sticks, sample `111 * 1 = 111`; alternative `111 = 1 * 111` passes. EditMode 29/29, 100 data checks and fresh small-screen alternative input clear pass. |

Fresh browser replay confirmed the numeric comparison at 390x844 and 320x568,
and revised Round 60 cleared through actual touch as `111 = 1 * 111` on the
small screen. The first three changes passed 33 PlayMode / 29 EditMode tests,
three WebGL builds and the ordinary first-ten input/save regression.

Further investigation found two more issues, implemented but not yet fully
revalidated in a fresh WebGL build:

| Priority | Reproduction / impact | Acceptance | Evidence / status |
| --- | --- | --- | --- |
| P1 | While a placed stick is dragged, switching to another actual browser tab (`document.hidden=true`) leaves the drag alive after returning. A release then changes placement. A pending non-drag tap also rotates after the native pause callback. | Interruption invalidates both active drags and presses begun before interruption; new presses after resume still work. | Before screenshots `baseline/r96-real-tab-held`, `r96-after-real-tab-return`, `r96-real-tab-release`; pending-tap red test expects vertical, gets slash. Added a browser interruption version and per-press generation guard. JS bridge test passes; latest PlayMode pending-tap case passes; fresh browser verification pending. |
| P1 | Removing either outside stick from `111` leaves center/side poses that are not recognized as `11`. | Removing any of the three sticks leaves a recognized, spaced `11`, without changing count. | Browser release screenshot shows the failed recognition; red test gets empty symbol. Normalize remaining vertical pairs; three-index removal test passes in the latest PlayMode run. Fresh input replay pending. |

Last full run: 35 tests, 34 passed. The remaining pickup-cell test sent a click
without pointer-down and failed after press-generation validation was added.
It now sends pointer-down first; the new pause test also checks a fresh resumed
press. Rerun was blocked by the automatic approval service reporting model
capacity errors, not by a new Unity compile failure. Do not label this suite PASS.
JS interruption bridge, 100-round data and scoped whitespace checks pass.

The baseline session was observed 06:55:01-07:19:45 UTC (about 24m44s) before
user interruption. On resumption at 08:56 UTC both old CDP sessions were
unresponsive. The unattended gap is not a completed 30-minute soak. Their
owned interactive processes were stopped; child-process inspection was blocked
by the same approval-service error. Native build 2 remains unchanged. Resume:
rerun PlayMode, rebuild QA, replay both new P1 cases, then start a new measured
30-minute session on that final binary. Refresh release/capture outputs afterward.

### Continuous Quality Follow-Up (2026-09-14)

| Priority | Reproduction / impact | Acceptance | Evidence / status |
| --- | --- | --- | --- |
| P2 | Browser touch cancellation preserves sticks but leaves `Drop into a box.` after the drag is gone. | Cancellation restores the preceding feedback and color, without changing the board or bank. | `cancel-after.png` under `/tmp/one-equals-one-continuous-input`; new PlayMode case fails before fix, passes after. |
| P1 | Secondary press on the same bank stick during a drag resets the handler's drag flag; after focus cancellation its late click rotates a stick. | Reject click eligibility for presses begun during an active drag; keep normal taps and owning drag working. | Event-handler regression fails with `CenterSlash` instead of `CenterVertical`, then passes. Native timing is not yet verified. |
| P2 | Page 9 shrinks its grid and moves Previous/Next/Close upward; repeated navigation at the prior location misses. | Pager and Close retain their bounds across all nine pages without showing nonexistent round cards. | Browser `picker-page-8/9.png` plus failing PlayMode pager-distance assertion (298.286 logical units); grid flexible space added. Final 31/31 tests and 390/320 browser page reversal/Close pass. |

Current investigation separates A (local input/font/feedback regressions and
bounded browser soak) from B (physical device, native SDK callbacks, website
publication and App Store state). No iOS devices found in this follow-up. No
installation, upload, deployment or commit is authorized by this goal.

The scoped A issues above pass their acceptance checks. Added cancellation
coverage to the ordinary first-ten browser test (PASS, progress 11 restored).
26 browser manipulation/Reset cycles and a 60-cycle Editor input/resize test
retain visible/generated labels; the browser continuation clears 96 into 97.
No further reproduced P0/P1 in these inspected states. Bounded soak is not
native long-session/performance approval, and no human fun rating is inferred.
Evidence: `artifacts/one-equals-one/2026-09-14-continuous-quality/`.

### Post-Distribution Development Checkpoint (2026-09-14)

- A / completed for this loop: supplied video frame audit, actual-input 96-100,
  ending/picker/reload/reset/replay, target-line fix, font-material refresh,
  native-scale store candidates, and local public-privacy wording correction.
  See the gameplay audit for timestamps, expressions and exact evidence.
- P1 target wrapping: video 64/68s and failing one-line test; now 100 rounds x
  four logical widths pass without widening slots.
- P1 font recurrence: build-2-code QA lost target after Round 100 placement;
  material binding now refreshes with vertices. Final 96-100 browser replay,
  idle/resize and 28 PlayMode cases pass. Native/long-duration confirmation pending.
- P2 store capture scale: use native reference scale in store-capture only.
  Existing candidates preserved; 24 verified new images in `Candidates-next-build`.
- Submission discrepancy: public privacy URL omits `1 = 1` SDK usage. Local
  website patch/build/browser check complete; deployment not authorized/performed.
- B / still open: new numbered iOS build and device verification, production
  consent/ad/crash-console evidence, privacy deployment/store declarations,
  Apple processing/review status, Android config/signing and subjective audio QA.
- Current validation: 28 PlayMode, 100 samples, three WebGL builds, release
  first-ten input regression, five viewports, store candidate checks, basic
  static suite PASS. Strict aggregate fails on Android config/IDs; retained as
  failure, not downgraded to a passing release gate. No connected iPhone found.
- No game rule, round data, character direction or ad cadence changes. No commit,
  push, native upload or public website deployment. Local preview now refreshed
  at `http://127.0.0.1:8093/index.html`. This is not commercial-release signoff.

The checkpoints below describe earlier states and are superseded by this one.

### Gameplay Quality Loop (2026-09-13 User-Requested Pause)

- A / paused: actual-input investigation completed through Round 95;
  Round 96 opened but not attempted. Finish 96-100 and the ending flow on resume.
  See the gameplay quality audit for expressions, sample exposure and limits.
- A / P1 follow-up implemented during iOS distribution: reproduced a missing
  target during Round 87 placement and a missing recognition label in a separate
  diagnostic build. Defer shared-font text mesh invalidation until the next
  LateUpdate after an atlas rebuild. Final QA captures retain both labels through
  Round 87 placement/clear and Round 80 failure/correction/clear, including
  320x568 resize. PlayMode 26/26 passes, including equality target visibility.
  Evidence: `artifacts/one-equals-one/2026-09-13-font-refresh/`. This mitigates
  the observed browser case; long-session/native confirmation remains pending.
- Completed in the resumed pass: recognition-label readability, removal of
  ambiguous global last-token feedback, and round 65/70/87 quality corrections.
  All three revised rounds pass actual-input replay with saved unlocks preserved.
  Final EditMode 27/27, PlayMode 24/24, 100-round data, WebGL build/freshness pass.
- Completed in this pass: stable bank positions and cell hit areas, consistent
  rotation, calculated-value failure feedback, revised rounds 14/15/42/46,
  readable small-screen status text and enlarged footer commands. Remaining
  repeated-constraint candidates (including 69/70) are not automatically defects.
- A / existing evidence: first-ten input regression, sample/math/layout checks
  and browser captures from the prior pass; these do not establish 100-round fun.
- B / pending: device feel/listening, production consent/ad/crash callbacks,
  Apple processing/TestFlight setup and store submission. iOS Firebase/AdMob
  configuration, signing, Archive and initial upload now pass; see ship-ready
  status for exact build numbers. Android setup remains separate. B does not block A.
- Some early/mid examples appeared in prior conversation, so the audit is not
  a novice blind playtest. Current attempts avoid opening SampleSolution data.
- Browser observation is Chrome emulation with actual touch events, not a
  physical phone or a human learning-time study. Input trace/screenshots:
  `/tmp/one-equals-one-gameplay` and `/tmp/one-equals-one-gameplay-resumed`.

The entries below are chronological findings, not current pending-state claims.
User-requested pause is not gameplay completion or ship-ready signoff. The earlier
checkpoint was committed/pushed as `fa18d8d7`; the user requested wrap-up and
push/merge of the font-refresh follow-up, included in this checkpoint on `main`.
Preview `http://127.0.0.1:8093/index.html` is the older release WebGL;
the updated QA WebGL was tested separately and its isolated browser was closed.

Initial observations (sample source not opened): Round 11 cleared as `11 - 1`,
12 as `11 / 11`, and 13 as `1 - 1 + 1 - 1`, using visible constraints/hints.
Round 13 jumps from three to seven slots and needs eight placements; bank items
recenter/rescale as each is removed, repeatedly moving the next pickup target.
P2 candidate: retain bank pickup positions during a round, including returns,
without changing stick count or token recognition; verify before/after touch.

P2 teaching candidate: Round 14 announces multiplication precedence, but the
visible 7-stick/5-slot/target-2 challenge admits `1 + 1 × 1` with the same result
under left-to-right evaluation. Acceptance for an introductory precedence
example: conventional precedence and left-to-right evaluation differ, while
all existing alternative-answer rules remain unchanged. Investigate Round 15
before selecting a data correction; do not judge novice difficulty from these
agent-operated attempts.

Round 15 reproduced the same teaching issue with `1 + 1 / 1`. Two focused
EditMode cases failed against the old samples (evidence:
`/tmp/one-equals-one-precedence-red.xml`). Replaced the samples with
`1 + 1 × 11` (eight sticks) and `11 + 11 / 11` (nine sticks), both five slots,
preserving indices, progress keys and alternate-answer rules. The first division
candidate duplicated Round 34; the existing verifier caught it, and the final
candidate passes all 100-round data/quality checks. This exposed Round 34's
sample via verification output, so its later playtest is not sample-blind.
Unity verification/replay pending.
Rounds 16-17 cleared as `1 + 1 + 1` and `11 - 11`; the explanation area is
collapsed and the shorter puzzle after Round 16 provides a lower-input interval.

The stable-bank regression reproduced a 61-unit pickup shift before the fix
(`/tmp/one-equals-one-bank-red.xml`). Remaining sticks now retain a round-local
bank position and scale; returning sticks fill a vacant position. Reset rebuilds
the original arrangement. PlayMode: 17/17 pass including remove/return/reset
position checks; fresh browser comparison pending.

Latest gameplay evidence is maintained in
`docs/one-equals-one-gameplay-quality-audit.md` (11-30 investigated; 31-50 active).
Both modified lessons and the stable bank were replayed successfully. Further
actual play exposed hidden wrong-result information and inconsistent bank/slot
rotation. Both red tests reproduced, fixes are implemented, and current full
EditMode/PlayMode each pass 19 tests. A fresh QA build is available; feedback/
rotation browser comparison is in progress. The release artifact and earlier
final-gate results must not be treated as fresh for these new changes.

`1 = 1` should not be called commercially complete until a fresh Unity build and
device QA pass confirm the current source state.

Current checklist status is tracked in
`docs/one-equals-one-ship-ready-status.md`.

## Done

| Area | Issue | Slice | Verification |
| --- | --- | --- | --- |
| Layout | Dense expressions compressed slot proportions. | Added multi-row equation layout with release minimum slot dimensions. | Unity verifier and static round verifier layout checks. |
| Layout | Compact phones were under-tested, so long expressions could still assume a wider logical row than the safe area. | Equation layout now uses the current safe width, can fold compact layouts to 4 rows, shrinks fixed-target text width on narrow screens, and caps 4-row slot width to keep aspect stable. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs` now checks 320, 390, and 488px widths plus slot aspect bounds. |
| Layout | Tall 20:9 WebGL viewports could leave the first screen visually stranded too low with too much blank space above. | Added a centralized stage layout plan that gently lifts tall-portrait stages while keeping SE/desktop layouts centered, then mirrored the rule in Unity and Node verification. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs`, `./scripts/verify-one-plus-one-minus-one-static.sh`, fresh WebGL build, and fresh viewport smoke pass. |
| Controls | Dragged sticks could be hidden by the finger. | Added pointer-offset drag ghost. | Fresh QA/WebGL captures pass; needs real touch QA. |
| Controls | Drop intent was hard to read. | Added hover tint on the slot under the pointer. | Fresh QA/WebGL captures pass; needs real touch QA. |
| Controls | Placed stick rotation was indirect. | Added tap-to-rotate for placed sticks and drag-out return. | Fresh QA/WebGL captures pass; needs real touch QA. |
| Feedback | Success/fail state felt quiet. | Added slot bumps, equation shake, randomized short result copy, and gentle generated SFX. | Static policy checks pass; needs audio QA. |
| Characters | `x`/`×` and `*` were too similar. | Split `*` into a dedicated Star face style. | Fresh Round 5 and Round 9 captures pass; needs final device readability QA. |
| Characters | Character direction could regress into arms, hands, legs, cheeks, or blush after polish passes. | Added a character-policy verifier that rejects forbidden body-part/blush code and requires the symbol-based face style hooks to remain. | `./scripts/verify-one-plus-one-minus-one-character-policy.sh`; included in static suite. |
| Round select | Fixed 520x760 round-select panel could overflow or feel detached on narrow screens. | Added safe-area-based panel, narrow-screen fallbacks, and 3-column cell resizing. | Fresh page 1/5/9 captures pass; needs real touch QA. |
| Round select | Round-select sizing had no static guard for 100-round mobile pages. | Added C#/JS layout-plan verification for common portrait safe sizes. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Round select | Round-select pages sat too low on tall mobile WebGL captures, making the overlay feel detached from the first viewport. | Added portrait-aware panel lift and static top-position guards for 19.5:9-20:9 screens. | Fresh round-select page 1/5/9 captures pass across iPhone SE, standard iPhone, large iPhone, Android 20:9, and desktop. |
| Round select | The final 100-round page kept a full 4-row grid height even when only four rounds were visible. | Compacted round-select grid height by visible row count, so page 9 pulls pager and Close closer to the remaining buttons. | Fresh page 9 captures pass and visual spot check looks tighter. |
| Round select | Underlying game screen could show through the overlay. | Made the round-select blocker fully opaque. | Fresh round-select captures pass. |
| Round select | Page 9 and disabled pager states were not easy to verify repeatedly. | Added dev-only `qaRoundPage` startup override and round-select page capture script; strengthened disabled command-button visuals. | Page 9 iPhone SE capture spot-check passes. |
| Icon | `=` character becomes thin at small icon sizes. | Increased generated icon stick/equal proportions and reduced stick roundness. | Small-size icon visual verification passes at 180, 120, 64, and 32 px. |
| Icon | Generated PNG kept an alpha channel even though the icon is visually opaque. | Switched icon generation to `TextureFormat.RGB24`, stripped alpha from current icon/WebGL favicon, and added an icon verifier. | `./scripts/verify-one-plus-one-minus-one-icon.sh` passes and is part of static/ship-ready checks. |
| Icon | Icon freshness used noisy mtime checks even though the generator can produce identical bytes. | Made `GenerateAppIcon.Run` skip identical PNG writes and changed icon verification to byte-compare source and WebGL icons. | Static suite and ship-ready icon gate pass without icon mtime warnings. |
| Icon | Small launcher-size readability was still a manual check, so the icon could pass file checks while `1 = 1` became too faint or narrow. | Added a PNG-based small-icon visual verifier that downscales to 180, 120, 64, and 32 px and checks content contrast across left, center, and right regions. | `./scripts/verify-one-plus-one-minus-one-icon-visuals.mjs`; included through the icon verifier and static suite. |
| Automation | Unity license blocked round data verification. | Added Node-based static 100-round verifier. | `node scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Rounds | Direct equality rounds disappeared from the mid and finale bands. | Replaced two target-only puzzles with neutral-title equality puzzles: Round 44 `Even Still`, Round 98 `Final Balance`. | 100-round verifier passes; quality report has no review warnings. |
| Automation | Round quality review depended on manually eyeballing 100 entries. | Added a round quality report for token bands, token introductions, target spread, adjacent pattern runs, and review warnings; mirrored token-band coverage checks in the Unity verifier. | `./scripts/report-one-plus-one-minus-one-round-quality.mjs --strict`; included in static suite. |
| Automation | Existing WebGL artifact smoke was coupled to Unity rebuild. | Added build artifact smoke script. | `bash scripts/smoke-one-plus-one-minus-one-webgl-build.sh`. |
| Automation | QA and store-capture WebGL shells could drift from release WebGL metadata without failing their own build scripts. | Added title, icon, cache-busting, mobile web app metadata, theme-color, apple-touch-icon, and icon consistency checks to QA/store-capture build verification. | `./scripts/verify-one-plus-one-minus-one-webgl-qa.sh`, `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh`, and icon verifier. |
| Automation | Final ship-ready could rely on screenshot freshness without directly checking release/QA/store-capture shell metadata as a group. | Added an existing-artifact WebGL shell verifier for release, QA, and store-capture outputs and wired it into the final ship-ready gate. | `./scripts/verify-one-plus-one-minus-one-webgl-shells.sh`; included in `./scripts/verify-one-plus-one-minus-one-ship-ready.sh`. |
| Automation | No-Unity checks were scattered across multiple commands. | Added static verification suite. | `./scripts/verify-one-plus-one-minus-one-static.sh`. |
| Automation | Existing WebGL smoke could pass an outdated build silently. | Added source-newer-than-build freshness warning. | Static suite now reports stale builds. |
| Automation | QA round-jump/sample-fill and test-crash hooks could accidentally leak into commercial release builds. | Added a release-safety verifier that evaluates release-active preprocessor lines and rejects QA/test hooks outside development/editor/store-capture guards. | `./scripts/verify-one-plus-one-minus-one-release-safety.mjs`; included in static suite. |
| Automation | Visual QA was only a manual checklist. | Added Chrome/CDP WebGL viewport smoke screenshots for mobile and desktop sizes. | `./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs`. |
| Automation | Viewport smoke could save a screenshot even if the page was visually blank. | Added PNG pixel inspection to fail nearly blank or overly dark captures. | `./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs` and static suite pass. |
| Automation | Viewport smoke failed with a raw Node stack trace when local port binding was sandbox-blocked. | Added a clear EPERM/listen error message that explains the required outside-sandbox execution. | Sandbox run now fails cleanly; outside-sandbox execution now reaches per-viewport checks. |
| Automation | Viewport smoke could miss layouts that technically render but begin too low on tall mobile screens. | Added a mobile screenshot content-top check that fails when meaningful pixels start too far down the viewport. | Stale artifact failed on iPhone-standard top whitespace as intended; fresh build passes after the tall-stage lift. |
| Automation | Key late-round visual QA had no direct entry point. | Added a development-only WebGL QA build and `qaRound`/`qaUnlocked` startup overrides, plus a capture wrapper for tutorial, post-tutorial, and late-round checks. | QA WebGL build passes; Rounds 1, 5, 8, 9, 16, 30, 50, 75, 90, and 100 screenshots are captured and verified. |
| Automation | QA round and round-select screenshots could go stale without detection. | Added a QA capture verifier for required key rounds, pages, viewport dimensions, and QA-build freshness. | Fresh `/tmp` QA captures pass after the expanded coverage set. |
| Automation | QA round/page capture roots could retain old managed folders after the required visual set changed. | Default QA capture commands now recreate managed `round-*` and `page-*` folders, while verification rejects unexpected stale managed capture directories. | `./scripts/verify-one-plus-one-minus-one-qa-captures.sh`. |
| Automation | QA/App Store screenshot checks could pass a correctly sized but visually blank or badly positioned PNG. | Added a shared PNG visual verifier for QA captures and App Store candidates; it inspects PNG pixels for dimensions, alpha policy, dark/light content, and mobile content-start position. | `./scripts/verify-one-plus-one-minus-one-png-visuals.mjs --qa`, `--app-store`, QA capture verifier, and App Store candidate verifier pass. |
| Store | App Store screenshot capture was a manual loose checklist. | Added store-sized WebGL candidate capture for 6.5-inch iPhone, 6.9-inch iPhone, and 13-inch iPad; QA fill can place sample solutions for stronger shots. | Candidate screenshots generated and size/alpha verifier passes. |
| Store | Candidate screenshots could be stale or come from a mismatched capture build. | Candidate verifier now requires a non-development store-capture build and fails screenshots older than that build. | Fresh store-capture build and App Store candidate verification pass. |
| Store | Renumbered App Store screenshot slugs could leave old candidate folders beside the current upload set. | Candidate capture now recreates managed `??-*` folders and verification rejects unexpected stale candidate directories. | `./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh` and `./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh`. |
| Build | Store-capture WebGL verification could hang when Unity licensing failed during batchmode startup. | Added the same Unity CLI license precheck used by other build scripts. | `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh` now exits quickly with a license error. |
| Automation | Commercial completion required humans to combine many scripts. | Added one final ship-ready gate that aggregates fresh build, freshness, icon, viewport, QA captures, App Store candidates, Firebase, AdMob, iOS, and Android checks. | `./scripts/verify-one-plus-one-minus-one-ship-ready.sh`. |
| Android | README listed Android, but no Android build/readiness path existed. | Added Android AAB/APK/AdMob-test build methods and readiness script. | AdMob-test APK passes; release AAB still needs signing env and production config. |
| iOS | AdMob test export was not proven end to end. | Ran and fixed iOS AdMob-test readiness so Xcode placeholder bundle IDs resolve through pbxproj. | `./scripts/verify-one-plus-one-minus-one-ios-readiness.sh admob-test`. |
| Android | AdMob test APK path was not proven end to end. | Ran Android AdMob-test build successfully. | `./scripts/verify-one-plus-one-minus-one-android-readiness.sh admob-test`. |
| Store | Store copy and privacy disclosure were only embedded in readiness notes. | Added dedicated store metadata draft and linked privacy policy. | Needs final build and SDK settings before submission. |
| Store | Store metadata could drift away from required submission fields. | Added a store metadata verifier for identity, privacy URL, screenshots, ads/privacy disclosure, age-rating notes, final checks, and unresolved placeholders. | `./scripts/verify-one-plus-one-minus-one-store-metadata.sh`; included in static suite. |
| Store | Store copy could fit structurally but still be too long, empty, or out of sync with screenshot capture slugs. | Strengthened store metadata verification with conservative local copy budgets, keyword-shape checks, required release-env notes, and screenshot slug alignment against the App Store candidate capture script. | `./scripts/verify-one-plus-one-minus-one-store-metadata.sh`; fixture coverage in `scripts/test-one-plus-one-minus-one-store-metadata.sh` checks keyword failures and missing screenshot capture slugs; included in static suite. |
| Store | iOS export had no app-level privacy manifest under project control. | Added `PrivacyInfo.xcprivacy` with app-local UserDefaults reason `CA92.1`, copied it into the Xcode app bundle, and made iOS readiness require it. | Static suite passes; iOS readiness reaches Unity license/version-env blockers. |
| Analytics | `app_open` had little context for progress/debugging. | Added app open parameters and progress/replay context on round events. | Static readiness checks required telemetry text. |
| Analytics | Readiness only checked that events existed, not that useful debugging context stayed attached. | Added static checks for round name, stick count, slot count, expression, failure count, cadence, ad show decision, app version, and platform parameters. | `./scripts/verify-one-plus-one-minus-one-static.sh` passes. |
| Ads | Production interstitial IDs were source constants, so env-only release setup could never fully pass. | Added build-time `Resources` config generation for iOS/Android interstitial ad units and strict env checks. | Static suite and readiness scripts cover env names and reject Google test interstitial IDs in production env vars. |
| Ads | The shared AdMob/Crashlytics readiness gate could warn/fail correctly in the live tree but lacked an isolated strict success fixture. | Added Firebase config path overrides and a dedicated readiness fixture test that proves strict success with injected Firebase files and production-shaped IDs, then proves Google test IDs fail. | `scripts/test-one-plus-one-minus-one-admob-crashlytics-readiness.sh`; included in static suite. |
| Ads | Release build entry points could rely on test/default AdMob app IDs if env was missing. | Made iOS/Android release build methods require production AdMob app IDs and reject Google test app/ad-unit IDs before building. | iOS/Android AdMob-test builds still pass; strict readiness still fails on missing production config as intended. |
| Release | iOS/Android readiness mixed Unity availability with release-env validation, making Unity licensing failures obscure whether platform env/source preflight was healthy. | Added `--preflight-only` to iOS and Android readiness scripts, plus Firebase config path overrides and fixture tests for strict release success and Google test AdMob ID failure without launching Unity. | `scripts/test-one-plus-one-minus-one-platform-readiness-preflight.sh`; included in static suite. Full export/build still requires Unity licensing. |
| Release | Ship-ready skipped all iOS/Android readiness when Unity licensing was unavailable, hiding platform-specific env/source blockers behind the Unity blocker. | Ship-ready now always runs strict iOS/Android release `--preflight-only` checks, then only skips the full Unity-gated export/build checks when licensing is unavailable. | `./scripts/verify-one-plus-one-minus-one-ship-ready.sh` reports platform preflight blockers independently from Unity licensing. |
| Ads | Strict readiness could miss copied placeholder AdMob IDs from `RELEASE_ENV.example`. | Added case-insensitive placeholder rejection for production AdMob app/ad-unit env vars in common, iOS, Android readiness and release build scripts. | `scripts/test-one-plus-one-minus-one-release-env.sh` now exercises mixed-case placeholder strict failures; static suite passes. |
| Ads | Strict readiness could accept non-empty but malformed production AdMob IDs. | Added production AdMob app/ad-unit format validation in common, iOS, and Android readiness scripts, plus release env documentation. | `scripts/test-one-plus-one-minus-one-release-env.sh` now exercises malformed strict failures; static suite passes. |
| Release | Release env checks only verified presence for versions and Android signing. | Added iOS/Android marketing version, build/version-code, Android keystore path format/existence validation, plus env documentation. | Release-env failure-mode tests cover invalid versions and relative Android keystore paths; static suite passes. |
| Release | External release blockers were scattered across heavier Unity/platform readiness scripts. | Added a no-Unity release-env preflight for Firebase configs, production AdMob IDs, iOS/Android version envs, and Android signing envs; strict ship-ready runs it before platform exports. | Default preflight passes with warnings; `scripts/test-one-plus-one-minus-one-release-env.sh` covers missing values, placeholders, malformed values, Google test AdMob IDs and a strict success fixture with injected Firebase/keystore paths. |
| Device QA | Ship-ready could pass automated visual gates without explicitly failing on unfinished manual device QA. | Added a device QA signoff preflight that checks the tracker for blank build-under-test fields, `not run` rows, incomplete WebGL mobile coverage, remaining real-touch/manual QA notes, unknown Status values, terminal `pass`/`fail`/`blocked` rows without Notes evidence, and inconsistent TestFlight-ready signoff while external blockers remain; strict ship-ready runs it before external config checks. | `scripts/test-one-plus-one-minus-one-device-qa-signoff.sh` covers passable fixture, unknown-status failure, missing terminal-status Notes evidence and inconsistent external-blocker signoff; default preflight passes with warnings; strict mode fails until manual QA is signed off. |
| Device QA | A manual QA tracker could delete a difficult required row and still satisfy only section/status checks. | Added required-row checks for the core screen matrix, touch, character, audio, ads/analytics and privacy rows, using precise row prefixes where labels are duplicated across sections. | `scripts/test-one-plus-one-minus-one-device-qa-signoff.sh` now includes missing-row fixtures for the WebGL mobile browser row and the touch `Drop` row while an audio `Drop` row remains. |
| Store metadata | App Store promotional text was discussed but not persisted as a verified metadata field. | Added a Promotional Text section to the store metadata draft and verifier length checks against the 170-byte submission field. | `scripts/test-one-plus-one-minus-one-store-metadata.sh` now includes an overlong promotional-text fixture. |
| Release env | iOS build env accepted build numbers lower than the already uploaded `1.0.0 (2)` candidate, which would fail the next App Store upload. | Added a default minimum iOS build number of `3` to release-env and iOS readiness preflight, overrideable via `ONE_EQUALS_ONE_MIN_IOS_BUILD_NUMBER`, and updated `RELEASE_ENV.example`. | Release-env and platform-readiness preflight tests now cover stale iOS build number rejection. |
| Release env | Android version code only required a positive integer, allowing stale `1`/`2` candidate envs. | Added a default minimum Android version code of `3`, overrideable via `ONE_EQUALS_ONE_MIN_ANDROID_VERSION_CODE`, and updated `RELEASE_ENV.example`. | Release-env and platform-readiness preflight tests now cover stale Android version code rejection. |

## Active Risks

### 2026-09-13 Input And Progress Loop

Follow-up P1: the 320x568 browser picker navigated all nine pages, but labels
were about 8 CSS pixels and pager targets only about 21 pixels high. Acceptance:
all picker buttons at least 44 CSS pixels high and labels at least 12 pixels at
that viewport, with no clipping on pages 1-9. The new PlayMode test reproduced
the undersized first round button before changing the portrait panel scale.
Portrait sizing, label size and pager/Close height are now adjusted. A DPR 3
follow-up exposed clipped third title lines; cell/panel height was increased,
and the regression now also checks the full generated text height for all 100
labels. Final build/capture refresh passed on 2026-09-13.

P1 privacy follow-up: the shared UMP bridge supported privacy options but this
game had no entry point. Added a conditional Privacy action beside Close in
Rounds, refreshed from the SDK requirement state. Its visibility and layout
test passes; actual consent-form presentation/revocation remains native QA.
The [official UMP guide](https://developers.google.com/admob/unity/privacy)
(reviewed 2026-09-13) requires an entry point when the SDK reports Required.
The complete controller suite now has 16 passing tests.
The first WebGL pass exposed an unsupported-platform UMP factory exception;
privacy-state queries are now native iOS/Android only (never Editor/WebGL).
Browser smoke now rejects Unity/JavaScript runtime exceptions explicitly. Its
new check reproduced the failing pre-guard QA build before rebuilding.
The fixed build passes runtime smoke and the full first-ten touch playthrough.

P1 equality follow-up: Round 5 accepted a touch-built `1 = 1` but still drew
the fixed `= 1` suffix. In Round 39 the same policy could visually claim
`11 = 11 = 22`. Acceptance: player equality hides the fixed target without moving
or resizing slots; removing equality restores it. A PlayMode test reproduced the
visible suffix before the fix. Arithmetic/acceptance rules are unchanged.
The follow-up now tests actual pose rotation instead of injecting a symbol:
rotating the vertical member of `+` yields two separated horizontal sticks,
and rotating one back restores `+` and the fixed target. Previously this in-slot
path overlapped the horizontal sticks even though bank insertion spaced them.
The same two-stick spacing normalization now runs after in-slot rotation/drop.
PlayMode passes. The fresh browser build also passes touch rotation of a
sample-filled Round 39 from `11 + 11` to `11 = 11`, back, and then clear.
Screenshots: `/tmp/one-equals-one-input-qa/round39-rotation-equality.png`,
`round39-rotation-plus-restored.png`, and `round39-alternate-equality-cleared.png`.
Separately, Round 5 was built from the bank with touch only and cleared as
`1 = 1`, without sample fill; `alternate-equality-fixed-cleared.png` confirms
no duplicate target suffix. The complete first-ten input run passes again.

Final-round browser persistence was also exercised with QA sample fill (not a
manual solution): clear Round 100, reload without QA query parameters, and
confirm `100 Done` on page 9. Evidence:
`/tmp/one-equals-one-input-qa/finale-restored.png`.

P2 sound follow-up: generated effects had no player mute control. Rounds now
contains a Sound checkbox that controls only this game's AudioSource and stores
the choice independently of progress. A new PlayMode regression checks default
on, immediate mute, reset, controller restart, and re-enable; picker bounds also
check its touch height and separation from the heading. Device speaker/headphone
listening remains unverified. The fresh QA browser also retains the unchecked
Sound state after reload and fits the header on 320x568. Screenshots:
`/tmp/one-equals-one-input-qa/sound-off-restored.png` and
`sound-off-small-picker.png`. Final release/capture aggregation passes all nine
local gates. Five strict native/configuration gates remain failed; this is a
development checkpoint, not commercial completion.

Native boundary evidence on 2026-09-13: the current Android AdMob test APK builds,
and iOS test export succeeds. The unsigned Xcode build then fails at the
Crashlytics Run Script's missing `GoogleService-Info.plist` (exit 65). This is
recorded as an external configuration blocker, not hidden by skipping the phase.
The final test build paths/logs are in the ship-ready status report. Device lists
were empty after ADB startup and via `xcrun devicectl list devices`.

| Priority | Reproduction / Impact | Acceptance | Evidence / Status |
| --- | --- | --- | --- |
| P1 | Move a placed stick onto a slot containing three sticks: the source empties and the stick returns to the bank. Bank rejection also overwrites the capacity message. | Failed drops preserve both slots and bank; `Box fits 3.` remains visible. Intentional outside drops return exactly one stick. | Reproduced with Chrome emulated touch on Round 2; controller regression and fresh WebGL replay pass. `/tmp/one-equals-one-input-qa/rejection-after.png` shows both placements and capacity feedback preserved. |
| P1 | A second pointer replaces the active drag; Reset or focus loss leaves a pending drop. | Only the owning pointer moves/ends the drag; reset, overlay and focus loss cancel without changing placement. | PlayMode tests pass for second pointer, reset and focus loss. Device touch signoff remains separate. |
| P1 | Runtime-created custom stick graphics lack an explicit CanvasRenderer dependency in Editor play mode. | Starting/rebuilding the board and raycasting produce no missing-component exceptions. | Reproduced by PlayMode setup; fixed by required-component attributes on the three local Graphic types. |
| P1 | Final-round completion is not persisted separately from unlocking Round 100. | Restart shows Round 100 Done; replay/reset preserve completion and never offer a release-policy replay ad. | Restart/controller integration test passes; production ad-device test remains external. |
| P2 | First-clear ad opportunity reports `is_replay=true` after unlocking the next round. | First-clear and replay events describe the attempt consistently. | Runtime event test and browser Rounds 1-10 clear/ad logs pass after capturing replay state on load. |
| P1 | Switching rounds while a slot bump is running accesses a destroyed RectTransform. | Rapid round replacement leaves no animation exceptions. | PlayMode transition test reproduced; animation lifetime guards added and runtime regression passes. |
| P1 | At the same 390x844 browser size, switching DPR from 1 to 3 shrinks the UI because physical render width selects a different CanvasScaler reference resolution. | CSS display size determines mobile layout; normal/high-DPI phones retain matching slot and control proportions. | Before screenshots `r11.png` and `r11-dpr3.png` reproduce the issue. Canvas-display-size bridge added; DPR 1 visual inspection and DPR 3 input playthrough pass. |
| P1 | Shared ad bridge queues an unavailable milestone ad and shows it when loading finishes, potentially during the next puzzle. | Only request an already-ready ad at the clear boundary; unavailable ads never block or interrupt the next round. Report eligibility separately from show readiness. | Local call-site readiness guard and PlayMode transition/log test added; native SDK callback/device QA still required. |
| P1 | Generated scene contains no AudioListener although the controller plays generated effects. | Runtime has one listener and non-silent generated clips. | Scene/controller inspection and PlayMode warning reproduced absence; listener fallback and signal test added. Subjective/device listening is still pending. |
| P1 | Initial/changed canvas size leaves equation slots using the old width; late rounds can retain too many rows while the surrounding stage changes. | Reflow slots when safe display width changes, preserve placements, and keep bank/footer inside the stage. | Small-phone Round 30 capture exposed missing footer; runtime all-100 resize/bounds test and fresh Round 30/100 captures pass. Short final rows now center slots and target together; bank fitting preserves proportions. |

Browser inspection uses an isolated Chrome profile and emulated touch, not a
physical phone. Rounds 1-10 were solved by touch drag/rotation,
without QA sample fill. The integrated browser runtime tool was unavailable in
this session; the repo's local Chrome/CDP approach was used instead.

| Area | Risk | Classification | Next Slice |
| --- | --- | --- | --- |
| Analytics | iOS Firebase plist is present locally; Android Firebase json is still missing. | external blocker | Keep `Assets/GoogleService-Info.plist` in the next iOS export, add `Assets/google-services.json`, then rerun strict readiness and Crashlytics delivery checks. |
| Ads | Production AdMob IDs are blank or still test IDs. | external blocker | Add production app/ad unit IDs, then run strict readiness. |
| Android | Release signing env vars are unset. | external blocker | Add keystore path/password and key alias env vars before Play Store AAB builds. |
| Store | Final privacy labels are not confirmed against a configured production build. | manual QA | Re-check SDK data collection after final Firebase/AdMob configs. |
| Device QA | Touch feel, SFX loudness, safe area, and small-slot character readability are unverified. | manual QA | Run device/simulator QA across target screens. |
| Visual QA | Tall iPhone and Android 20:9 fresh screenshots still feel spacious on the first tutorial screen even though the stranded-lower-half regression is fixed. | manual QA | Judge first-screen density on real devices; consider a future title/header compaction pass only if it feels empty in hand. |

## Next High-Value Loops

1. Rebaseline only what changed:
   - Read the September 13 evidence before rerunning checks.
   - Local build/input/layout gates passed; native release gates did not.
   - Build and recapture again only after relevant source/environment changes.
   - First resume target: supply the real Firebase plist, re-export and rerun
     the failed unsigned iOS Xcode build, then native consent reopening,
     ad close/failure recovery, and saved progress after process restart.

2. Touch feel loop:
   - Check drag offset on iPhone SE and Android 20:9.
   - Check placed-stick tap rotation versus drag-out return.
   - Tune hover color and SFX volume if either feels noisy or unclear.
   - Confirm the responsive round-select panel stays inside the safe area.

3. Character readability loop:
   - Compare `+`, `/`, `×`, and `*` in small slots.
   - Keep `1`, `-`, and `=` close to the current direction unless readability fails.
   - Do not add arms, hands, legs, blush, or external props.

4. Round quality loop:
   - Play first 10 rounds for first-three-minute clarity.
   - Play every tenth round for pacing.
   - Replace any late round whose target feels arbitrary or whose title gives away the solution.
   - Run `./scripts/report-one-plus-one-minus-one-round-quality.mjs --strict` after round edits and review token-band warnings.

5. Store readiness loop:
   - Add real Firebase and AdMob settings.
   - Confirm app icon at small sizes.
   - Prepare screenshots and privacy disclosures.

## 2026-09-18 Round Identity / Universal-Key Backlog

Completed in this slice:

- Added a bounded `--dominant-candidates` report path that searches nearby
  resource candidates and ranks replacements by reduction of the strongest
  reusable shortcut-family ratio.
- Added regression coverage so the candidate search is deterministic, reports
  its budget, and does not mutate round data.
- Hardened candidate filtering after a rejected Round 91 trial created a proven
  shared-equality reuse with Round 31.
- Added a replacement `reviewScore` so the dominant-candidate report prefers
  broadly playable improvements over narrow candidates with one or two accepted
  answers, large target jumps, or unnecessary resource drift.
- Redesigned Round 91 from `1 + 1 1 / 1 - 1` to
  `1 1 1 - 1 + 1 1`, preserving 8 slots and 9 sticks while removing it from
  the high-priority `/1` dominant-pattern list.
- Redesigned Round 65 from `1 / 1 1 - 1 / 11` to
  `1 - 1 1 / 11 - 1`, preserving 8 slots and 9 sticks while lowering the
  dominant `/1` family below the high-priority threshold.
- Rebalanced the tests so routine Node verification remains fast enough for
  the static suite; large candidate searches stay opt-in via CLI budgets.

Current next candidates from the latest pattern summary:

| Priority | Round | Pattern | Notes |
| --- | ---: | --- | --- |
| P2 | 61 | `multiply-by-one` | Highest remaining high-priority dominant row. Needs candidate search with shared-equality guard. |
| P2 | 73 | `/1`, `N/N` | Complete enumeration; high solution count. Avoid replacing one universal key with another. |
| P2 | 71 | `/1`, `N-N`, `N/N` | Several high rows on the same round; likely needs a resource/target rethink, not a title tweak. |
| P2 | 98/96 | `×1` or `/1` | Incomplete enumeration at current node budget; treat ratios as prefix evidence, not full proof. |
| P2 | 97 | `A=A` echo | Tied to finale-style equality; review for App Review optics before changing. |
| P2 | 63 | `/1` | Smaller complete round. Recent top candidates overlapped with the new Round 65 identity, so rerun after the current edits before applying anything. |

Useful commands:

```sh
node scripts/report-one-plus-one-minus-one-round-quality.mjs \
  --patterns --summary --strict-patterns --max-nodes 200000 --max-solutions 1000

node scripts/report-one-plus-one-minus-one-round-quality.mjs \
  --dominant-candidates 61,73,71,98,96,97,63 \
  --samples 120000 --max-evaluations 80 --max-results 5 \
  --candidate-budget 1200 --allow-fewer-slots --allow-fewer-sticks
```

Do not auto-apply candidate output. Check adjacent pacing, repeated targets,
resource/equality reuse, mobile slot load, and review-video optics before
editing another round.

Follow-up probe result: the current candidate generator found no clean immediate
edit for 61, 73 or 71. Top candidates either had only two to six accepted
answers, reduced the stick budget enough to feel like a different difficulty
band, jumped to far targets, or reused reviewed resource neighborhoods such as
8/9 and 10/11. Before another data edit, tighten the candidate search itself:

- require a minimum accepted-answer count for "recommended" candidates;
- down-rank candidates that reuse a resource neighborhood already under review;
- include a visible-arithmetic diversity score so `target changed, same
  universal-key feel` is not treated as a good replacement;
- keep narrow candidates visible as "analysis only" rather than top
  recommendations.

Implemented so far: dominant-candidate rows now include `analysisOnly`,
`analysisNotes`, `reviewedResourceCount`, `visibleDiversityScore`,
`minRecommendedSolutions`, `minVisibleDiversity`, and
`minRecommendedImprovement` in the search metadata. The score penalizes reviewed
resource neighborhoods and visibly similar samples, and sorting prefers
non-analysis-only candidates. This is a tool-quality improvement only; it does
not by itself redesign 61/73/71.

Follow-up search after this tool change:

- 63, 96 and 98 currently produce only analysis-only candidates.
- 73 also produces only analysis-only candidates under the new thresholds.
- 61 and 71 remain without a clean replacement from the current candidate pool.
- 97 is too heavy for the general dominant-candidate search at current budgets;
  interrupt long runs and build an equality-specific probe before trying to edit
  it.

Next practical improvement: add a generator that explicitly searches for
combined-pattern samples, for example a useful adjacent-number construction plus
one nontrivial cancellation, instead of only scoring nearby resource variants.
The current tool is now good at saying "do not apply this candidate"; it is less
good at inventing genuinely different late-round puzzle identities.

Latest tooling increment: candidate rows now include `combinationTags` and
`combinationScore`. This exposes visible sample cues such as packed numbers,
operator mix, nontrivial division/multiplication and non-echo equality. It is
not a replacement for playtesting: Round 63 shows why, because several
candidates look like combined-pattern expressions but still fail as production
edits due to tiny answer sets or reviewed resource reuse.

Round 97 follow-up tooling exists now: `--equality-candidates` searches
non-echo equality samples by pairing exact-equal left and right sides. The
bounded Round 97 pass now terminates instead of hanging the general candidate
search, but it found no candidate with `maxEvaluations=120`,
`maxSideExpressions=240`, and `maxResourceDelta=1`. Next useful work is a
hand-designed Round 97 replacement or a deliberately scheduled wider equality
search, not another unbounded general probe.

Collection and achievement differentiation track:

- Status: implementation and focused runtime verification pass complete for the
  current local source; broader human/device feel remains a separate QA item.
- The capacity-slot tutorial appendix has been discarded before release. The
  current default player path is the 30-round `= 1` fixed-target ladder, while
  the old 100-round Goal ladder remains hidden legacy content for QA/backward
  coverage.
- Current differentiation work focuses on lightweight collection hooks that do
  not constrain free solving: discovered shape friends, badges, and per-round
  solved-expression collection.
- The solved-expression collection intentionally supports the player's idea of
  clearing the same round in multiple ways. It records up to three distinct
  accepted expressions per round and awards a badge for finding three answers.
- Static Node tests and the static suite pass for the collection pivot. On
  2026-09-26, PlayMode passed 40/40 after adding runtime checks that the default
  `= 1` Round 1 clear records friend/badge/expression progress and that
  neighbor-number answers such as `1 1 / 1 1` de-dupe with packed-number answers
  such as `11 / 11` in the collection UI.
- PlayMode now backs up and restores collection/achievement/expression
  PlayerPrefs so test runs do not leak collection state across cases.
- Next playable-design step: verify in browser/native that the collection
  overlay and unlock toast feel celebratory rather than tutorial-heavy, and
  that expression collection encourages alternate solutions without revealing
  answers too early.

Browser input automation follow-up:

- Completed for the current local source on 2026-09-26: the first-ten WebGL
  input script now uses a development-only `qaInputProbe` layout dump instead of
  hard-coded old coordinates. It cleared the default `= 1` Rounds 1-10 with
  emulated touch input and saved progress to Round 11. Evidence:
  `/tmp/one-equals-one-input-smoke/input/results.json`.
- The same browser run now opens the Badges/collection overlay after Round 10
  and verifies the runtime collection state (`friends=8`, `badges=5`,
  `active=true`). Screenshot:
  `/tmp/one-equals-one-input-smoke/input/collection-after-first-ten.png`.
- Collection now shows the most recent solved round's answers when the current
  round has not been solved yet, so opening Badges after Round 10 displays
  `Recent Round 10 Answers` instead of an empty Round 11 list. The WebGL input
  result verifies `answerRound=10`, `answers=1`.
- The top-right entry is now labeled `Album`, and achievement rows use
  `Done:` / `Locked:` prefixes. This better matches the combined
  friends/answers/badges surface without adding solve constraints.
- Remaining limitation: this is browser automation evidence, not native-device
  finger feel, audio, or production SDK signoff.
