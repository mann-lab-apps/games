# 1 = 1 Gameplay Quality Audit

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
