# 1 = 1 Ship-Ready Status

Status date: 2026-09-26

Current completion judgment: `not yet`

Current execution: iOS `1.0.0 (4)` was archived and uploaded to App Store
Connect on 2026-09-25. The archive metadata records `uploadedBuildNumber` `4`
for app `6811571680`, with no upload errors or warnings. TestFlight processing,
installation, review selection and App Review outcome are still external
follow-up items.

Local source after that upload includes additional default-mode quality edits:
the default 30-round fixed-target `= 1` ladder now has its own `--make-one`
pattern report, no duplicate sample strings, and no late standalone `N / N = 1`
violations after Round 10. The report now explicitly marks Rounds 20, 22 and
23 as bounded `node_budget` rows under the current MakeOne pattern budget, so
they are not overclaimed as fully exhausted. The same local source now applies
a mode-aware dominant-pattern review window: the default ladder reviews after
Round 10, Round 24 was redesigned away from the high-ratio `/1` sample
`1 1 - 1 1 + 1 / 1`, and Round 18 was redesigned from `1 - 1 + 1 / 1`
to `1 1 - 11 + 1`. The default ladder now has no high-priority
dominant-pattern rows under the current bounded report. A new
`--make-one --evaluate-sample` report path lets future tuning inspect a
hand-authored candidate's resource cost and dominant shortcut profile before
changing round data. The MakeOne pattern summary now also includes
`resourceReview`, which surfaces repeated slot/stick groups and currently
records 13 unresolved found shared-equality pairs as design-review signals.
The follow-up `--make-one --resource-candidates` report path can now probe
target-preserving replacements for those groups and flag candidates that would
reintroduce late pure `N/N` shortcuts.
After the first resource-candidate tooling pass, Round 23 was changed from
`111 - 11 * 11 + 11 + 1 - 1` to `1 / 11 * 11 - 1 + 1`, reducing the default
ladder from six repeated slot/stick groups to five, from 15 to 14 unresolved
found shared-equality pairs, and removing the bounded high-priority
same-expression review row from the normal MakeOne summary. This is a local
source improvement only until a fresh WebGL/native build is produced.
After the next local source pass, Round 29 changed from `11 + 1 - 1 1` to
`1 1 + 111 - 11 × 11`, reducing the default ladder again to four repeated
slot/stick groups and 13 unresolved found shared-equality pairs. This is also a
local source improvement only until a fresh WebGL/native build is produced.
These 2026-09-26 edits are newer than uploaded iOS `1.0.0 (4)` until another
native build is produced.
The latest WebGL QA rebuild attempt after the final Round 23/Thin Return data
change did not produce fresh runtime evidence: Unity launched from
`scripts/verify-one-plus-one-minus-one-webgl-qa.sh`, then failed licensing
initialization after `74.83s`
(`/tmp/one-plus-one-minus-one-unity-webgl-qa-build.log`). The existing QA WebGL
artifact is still older than `OnePlusOneMinusOneRules.cs`, so it must not be
used as proof for the latest MakeOne round/tooling source.

Earlier execution: local source includes the numerical correctness fix,
round-identity data edits, and the 2026-09-18 universal-shortcut redesign.
`IsRoundSolved` and direct equality checks use exact rational arithmetic in
Unity source and the shared Node mirror, so the known Round 48 bad answer
(`1 / 11 111`) is fixed in source. All non-callback shared-answer pairs were
redesigned without token bans, and the late `N / N = 1` shortcut candidates
52/59/65/100 were also redesigned without banning `/` or forcing sample answers.
Follow-up dominant-pattern tooling now evaluates nearby resource candidates,
excludes replacement candidates that would introduce proven shared equality
witnesses, and redesigned Round 91 from `1 + 1 1 / 1 - 1` to
`1 1 1 - 1 + 1 1`; the round keeps 8 slots/9 sticks but no longer appears in
the high-priority `/1` dominant-pattern list. The same pass also redesigned
Round 65 from `1 / 1 1 - 1 / 11` to `1 - 1 1 / 11 - 1`, keeping 8 slots/9
sticks while dropping it below the high-priority dominant shortcut threshold.
Candidate ranking now includes a review score that penalizes extremely narrow
or target-jumpy replacements. Node identity tests pass 52/52, static
100-round verification passes, the
regular strict round-quality report passes, and `--patterns --strict-patterns`
now reports no late pure self-division violations. Round 100 is no longer the
30/100 identical title callback; it now uses `11 × 11 - 111 + 1`.
The pattern report also records late same-expression/same-number equality echo
review rows for the next quality pass; these are legal alternate answers, not
current strict failures. Cleanup removes copied-equation samples from
53/56/61/69/78 without adding shared equality witnesses; there are no late
sample-level equality echoes left, and 16 alternate-only equality-echo review
rows remain for future play/design review. Full static verification passes
after this redesign with expected stale-WebGL/external-release warnings.

Local iOS `1.0.0 (4)` archive/upload artifacts exist
(`Builds/iOS/Archives/OneEqualsOne-1.0.0-4.xcarchive`, with upload evidence in
`/tmp/one-equals-one-ios-upload-4.log`). Build 4 is the latest App Store Connect
upload confirmed by local evidence. It does not include the 2026-09-26
default-mode pattern edits described above.

Latest Unity runtime evidence is healthy for the source state before the later
Round 18 data change and resource-review tooling pass: after aligning the
native round-identity regression list with the shared Node identity suite and
making the PlayMode fixture explicitly select the legacy Goal ladder when
testing legacy 100-round behavior, EditMode passed 63/63 on 2026-09-26
(`/tmp/one-equals-one-editmode.xml`). PlayMode now passes 41/41
(`/tmp/one-equals-one-playmode.xml`) after adding focused runtime coverage for
the default `= 1` collection flow: Round 1 records the discovered `1` friend,
first-shape/first-clear badges and the solved expression, and packed/neighbor
number answers de-dupe in the collection UI. The Node identity suite now passes
60/60, including the MakeOne `incompleteReview` summary contract,
target-preserving MakeOne dominant-candidate search, single-sample evaluation
CLI coverage, MakeOne `resourceReview` summary coverage, resource-repeat
candidate report coverage, and static round verification. The footer command
buttons were widened and made taller/readable enough to pass the small-phone
touch/text regression.
Fresh QA and ordinary WebGL builds were rebuilt and verified after the
2026-09-26 default-mode pattern edits and QA input-probe work
(`/tmp/one-plus-one-minus-one-unity-webgl-qa-build.log`,
`/tmp/one-plus-one-minus-one-unity-webgl-build.log`). The default `= 1`
first-ten browser input regression now uses development-only rendered-coordinate
probe logs and clears Rounds 1-10, saving progress to Round 11
(`/tmp/one-equals-one-input-smoke/input/results.json`). The same run opens the
Badges/collection overlay and verifies `friends=8`, `badges=5`, `active=true`
and `answerRound=10` so the player sees the recent solved expression rather
than an empty current-round answer list, with screenshot evidence at
`/tmp/one-equals-one-input-smoke/input/collection-after-first-ten.png`. The
static suite now passes its non-Unity checks but is expected to warn that the
WebGL build is older than the latest `OnePlusOneMinusOneRules.cs` source until
Unity is responsive enough for a fresh rebuild; remaining warnings are external
release inputs and manual device QA signoff. The Round 48 browser input
regression also passes on the fresh QA build using `qaMode=goal`
(`/tmp/one-equals-one-round48-webgl/results.json`). The WebGL build scripts,
PlayMode wrapper, and native readiness scripts now distinguish empty license data from an
unavailable licensing service through the shared
`scripts/lib-one-plus-one-minus-one-unity-license.sh` helper: they may use the
local Unity entitlement file for the former, but fast-fail on
`LICENSING_CLIENT_UNAVAILABLE`. The ship-ready gate now runs a single Unity
licensing preflight before Unity-gated PlayMode/WebGL/native readiness checks,
then marks full export/build checks skipped when the licensing client is
unavailable instead of repeating the same Unity failure. It still runs strict
iOS/Android `--preflight-only` checks so platform env/source blockers are
visible even while Unity is down. A sandboxed ship-ready run still fails, as
expected, on Unity licensing, stale WebGL artifacts, strict release env, strict
device QA, strict Firebase/AdMob config and platform preflight blockers. Because
freshness fails,
the final gate now skips viewport smoke, QA key-round/page captures and App
Store candidate screenshot checks instead of producing old-build evidence for
the local source. The standalone WebGL viewport smoke passes when rerun outside
the filesystem/network sandbox and generated fresh captures for iPhone SE,
standard/large iPhone, Android 20:9 and desktop. This remains older-build smoke
evidence, not runtime verification for the latest numerical fix.

A later PlayMode recheck attempt during this local pass did not produce a
result: Unity fell back to the local entitlement file, emitted `attempt to write
a readonly database`, then remained silent until the command was interrupted.
This is recorded as an incomplete Unity-environment run, not as a gameplay
regression or a replacement for the earlier 41/41 PlayMode evidence above.
After the Round 18 MakeOne data change, a fresh QA WebGL rebuild was also
attempted. It reached the same Unity environment state (`attempt to write a
readonly database`) and stayed silent until interrupted, so the current source
still has a WebGL freshness warning. Rebuild QA and ordinary WebGL once the
Unity environment is responsive again.

Latest source-only hardening also changes tiny nonzero result display to avoid
rounding exact rational failures to `0` in player feedback. The controller's
visible wrong-result message now reuses the exact solve reason, so tiny nonzero
misses do not re-enter the legacy `Makes 0.` double-formatting path. The
corresponding EditMode/PlayMode regressions are added but await Unity execution
with the rest of the runtime gate. Release-safety static checks now also guard
the iOS and Android native build entrypoints: release exports must stay
non-development, require production AdMob IDs where applicable, and keep
forced-test-ad paths confined to explicit test builds.

Latest follow-up after commit `9fa9799e`: the top-level ship-ready gate now has
a fixture test for this blocked mode and prints a final blocker summary. The
2026-09-16 rerun still exits 2, but the ending list now explicitly separates
failed checks (Unity licensing preflight, stale WebGL artifact freshness, strict
device QA, strict release env, strict Firebase/AdMob code readiness, strict
iOS/Android release preflight) from skipped Unity/freshness-dependent checks
(PlayMode, fresh WebGL, viewport smoke, QA captures, App Store candidates and
full native readiness).

Unity licensing recovery check:

```sh
scripts/check-one-plus-one-minus-one-unity-license.sh
$HOME/.unity/bin/unity license --json
```

Current blocking output reports `LICENSING_CLIENT_UNAVAILABLE` with no `data`.
Unity Hub can be focused with `open -a "Unity Hub"`, but the latest check still
returns the same CLI error. Before rerunning PlayMode/WebGL, repair Unity Hub's
licensing service from the Hub UI, sign in again if needed, or fully restart
Unity Hub so the CLI returns active license data or a non-unavailable empty-data
state that can use the local entitlement file. Do not repeat Unity batchmode
tests while this command still reports `LICENSING_CLIENT_UNAVAILABLE`; the
scripts will fast-fail by design.

Latest browser URL: http://127.0.0.1:8093/ is still the older batch 4 ordinary
build unless rebuilt. Uploaded iOS `1.0.0 (2)` does not include these local
source changes.

Latest evidence: the 2026-09-16 sections atop the gameplay audit and commercial
polish backlog. Older sections below retain historical pending/blocked results.

Stage: post-distribution development checkpoint; new native candidate and
production release validation pending.

## Current Round Identity Batch 4 (2026-09-15, Runtime Pending)

Follow-up analysis: Node tests now pass 32/32. Bounded canonical solution
enumeration completes 98 rounds; 61/78 remain limited. It exposed a zero-target
tolerance risk (`1 / 11111` accepted as zero), recorded for native reproduction
and correctness review. Existing native/sample parity is not exhaustive proof
for the new inventory. No additional runtime/data edits or player builds.
The same approved-path request again failed in the approval service before
starting; three requests across two affected goal turns. Runtime gate remains.

Source-only candidate updates 11/33/48/83. Cumulative changed indices 49;
proven shared-equality pairs 14; exact duplicate remains only 30/100. EditMode
47/47, Node 26/26, 100 samples and 10,000 native/Node pairs pass. Static passes
with the expected old-WebGL freshness and external readiness warnings.

PlayMode was not started: two ordinary approval requests failed because the
approval service's selected model was at capacity. No player build or input QA
has validated these four edits. WebGL hashes still match batch 3; the preview
at `http://127.0.0.1:8093/` is NOT the latest source candidate. iOS 1.0.0 (2) is
unchanged. No commit, push or deployment occurred. The identity goal is unfinished;
see the current execution block above.

First pending action is the same PlayMode verification after approval recovery,
then fresh QA/WebGL and changed/neighbor/small-screen input. Evidence:
`artifacts/one-equals-one/2026-09-15-round-identity/batch4-verification-status.json`.
The latest audit distinguishes the completed source work from this execution gap.

## Current Round Identity Batch 3 (2026-09-15)

Latest verified batch revises 35/43/57/75/88 and adds rules-level slot-count
validation matching the fixed UI board. Cumulative changed indices: 47;
resource pairs: 60; proven shared-equality pairs: 18 (baseline 158).
The title 30/100 is the only identical constraint group. Identity goal is open.

EditMode 43/43, PlayMode 37/37, Node 17/17, native/Node 10,000-pair parity,
100 data, static and fresh QA/ordinary/capture WebGL pass. Revised samples and
ten neighbors pass actual browser touch; five alternate answers pass at 320x568.
First-ten input/save and five ordinary viewports are freshly checked as well.
Evidence prefix: `artifacts/one-equals-one/2026-09-15-round-identity/batch3-*`.
No native/device/production SDK signoff is inferred. iOS 1.0.0 (2) is unchanged;
no commit, push or upload occurred. Preview HTTP 200: `http://127.0.0.1:8093/`.
Next 11/33/48 coupled redesign is an unapplied candidate, not part of this build.

## Current Round Identity Batch 2 (2026-09-15)

Latest data changes 38/39/52/70/90 after the preceding 40-index candidate.
Cumulative changed indices: 44; resource pairs: 57; proven shared-equality
pairs: 22 (original baseline 158). Only the explicit 30/100 title callback
remains an identical constraint group. The identity goal remains active.

Unity EditMode 35/35, PlayMode 37/37, Node identity tests 12/12, 10,000 native/
Node sample pairs, all 100 samples, static and three fresh WebGL builds pass.
Five revised samples plus eight neighbors pass actual browser touch; all five
alternative answers also pass at 320x568. Existing save/first-ten/last-round
evidence is reused for unchanged behavior, not claimed as a new end-to-end run.
Artifact prefix: `artifacts/one-equals-one/2026-09-15-round-identity/batch2-*`.
Local preview `http://127.0.0.1:8093/` returns HTTP 200. No commit, push, native
build, upload or submission occurred. Native 1.0.0 (2) is unchanged. See the
audit/backlog for the next shared-resource groups and remaining design work.

## Post-Checkpoint Resume (2026-09-15)

Latest local continuation: round-identity redesign is verified, with 40 changed
indices and an aspect-ratio clamp fix. EditMode 31/31, PlayMode 37/37, all 100
samples, 10,000 cross-acceptance pairs, static and three fresh WebGL builds pass.
All changed final expressions have actual browser input evidence; small-screen
34/84/100, first-ten/save and final completion/Reset/reload were checked.
Evidence: `artifacts/one-equals-one/2026-09-15-round-identity/`.
The identity goal remains open: common-equality pairs fall 158 -> 27, but their
remaining design disposition is not resolved by build success. Exact duplicate
groups fall ten -> one (the explicit 30/100 title callback). See the latest
gameplay audit/backlog, which supersede the round-quality conclusions below.
Preview remains `http://127.0.0.1:8093/`, HTTP 200 verified on this run. Native
1.0.0 (2) is unchanged; new store-capture binary is not newly submitted imagery.

Base is `26d8508a`. The prior approval blocker is resolved for this run:
PlayMode 36/36 and EditMode 29/29 passed, followed by QA/ordinary WebGL builds
and actual first-ten/save regression. Initial QA replay verified all three
triple-stick returns/transfers, real hidden-tab cancellation, stale tap rejection
and full-slot recovery. The initial soak was intentionally stopped before 30
minutes after a new controller-event race was reproduced.

New finding: a fresh second-finger press after resume overwrites the view's
shared press version, reviving the first finger's delayed click. The new test
failed 36/37; pointer identity and pre-mutation drag guards now pass 37/37.
Final-binary QA replay verifies old/fresh finger release order, actual tab
interruption and every triple-stick return/transfer. QA, ordinary and capture
WebGL builds succeed. The corrected first-ten/save regression and five inspected
viewport captures pass. Static verification passes with external readiness
warnings; existing local iOS configuration is not absent merely because this
shell did not load its env file. Android production configuration/signing and
physical/production SDK evidence remain separate open gates.
The final same-binary/browser session ran 05:18:56.985-05:49:15.694 UTC
(30m18s), with 66 observations and no captured runtime exceptions. Actual
96-100 input, an alternative equality, final completion, full-slot rejection
and replay ad exclusion pass. Subsequent reload preserves completion and Sound
off. Browser heap/listener trends are not native performance signoff.

Current local stage: gameplay development checkpoint passed, not release-ready.
Evidence: `artifacts/one-equals-one/2026-09-15-post-checkpoint/`.
Latest ordinary preview: `http://127.0.0.1:8093/` (HTTP 200 verified).
Uploaded iOS 1.0.0 (2) remains unchanged. No commit, push, install, upload,
deployment or submission was performed. Next: after separate authorization,
build a native candidate with the accumulated fixes and execute the open device
and production SDK checks. Historical September 14 blocked results below are
superseded by this section, not the current execution state.

## Latest Resume State (2026-09-14)

Goal execution status: `blocked`, not complete. The approval/execution barrier
persisted across three consecutive goal turns. The final audit found neither
`/tmp/one-equals-one-resumed-playmode.xml` nor its `.log`; the last executed
result remains 34/35 at 08:57:37 UTC. No live test handle exists from the rejected
launches. Independent runner fixes and pending drag-path coverage are preserved;
further game edits without execution would only widen the verification gap.
Resume when the authorized Unity execution path is available: run full PlayMode
(now 36 test methods), build fresh QA WebGL, verify tab interruption and all
three `111` removals/transfers, then measure a new 30-minute mixed-input session.
Do not retry the rejected command through a different launch path.

The latest Unity retry was rejected before launch by the same approval-service
capacity error. Independent verification-runner fixes now preserve browser
exception events, bound CDP waits and retain the original test failure when a
screenshot fails. Nine isolated Node regressions pass; the interruption bridge,
100-round data and strict round-quality report pass. These do not close the
pending Unity/fresh-browser/30-minute gates below. No new native or WebGL build,
deployment, upload or commit was performed in this approval-blocked resume.

A subsequent retry was also rejected before execution. Added pending regression
coverage for all three triple-stick indices through controller drag return and
cross-slot transfer, including stick-count conservation and ghost cleanup.
This coverage is not yet executed; latest observed Unity result remains 34/35.

The long-session follow-up found and changed five areas: dead face registry
retention, numeric equality failure feedback, the nearby Round 57/60 duplicate,
interrupted drag/tap handling, and `111` side-stick removal recognition.
Evidence: `artifacts/one-equals-one/2026-09-14-long-session/` and the current
gameplay audit/backlog. User changes and unrelated projects are preserved.

The first three changes passed 33 PlayMode / 29 EditMode tests, all 100 samples,
three WebGL builds, small-screen actual-input replay and first-ten/save regression.
The final two changes have red tests and a subsequent 34/35 PlayMode run.
The remaining fixture lacked pointer-down; it is corrected, but its final rerun
and final-binary browser checks are blocked by approval-service capacity errors.
Node bridge, round data and scoped whitespace checks pass. This is not a fully
green candidate; do not reuse the earlier build results as final-source proof.

The original browser was observed for about 24m44s before interruption, then
became unresponsive during the unattended gap. A fresh measured 30-minute soak
is still required. Resume first with full PlayMode, then QA build and actual
tab-switch/pending-tap/`111` removal replay. Start the soak on that fixed binary;
refresh release and capture artifacts afterward. Generated scene-ID/whitespace
churn was removed, with no game data or user change reverted.

No commits, pushes, uploads, installations, website deployment or store submission
were performed. Uploaded iOS 1.0.0 (2) still lacks these local fixes. Existing
native/production/manual QA requirements remain open. The prior 8093 preview
serves an earlier local binary, not the final interruption/removal fixes.

## Latest Continuous Quality Checkpoint (2026-09-14)

Development follow-up on the existing dirty `main` (`d2abe1b4`), not a native
release approval. New fixes in this pass:

- Restore the preceding feedback when a drag is cancelled by focus/pause/disable
  or resize, instead of leaving an obsolete drop instruction.
- Reject late rotation clicks from a second press started during an active drag.
- Keep picker Previous/Next/Close in a fixed position across the short last page;
  absent rounds stay hidden. Both full and small phone layouts are checked.

Evidence: `artifacts/one-equals-one/2026-09-14-continuous-quality/`, including
before-fix failures, final 31/31 PlayMode results, source patch and browser input
traces. Bounded browser soak covers 26 manipulation/reset cycles, held-drag
resize, all nine pages and Round 96 clear/97 entry; no runtime exceptions were
captured and sampled fixed-target/recognition labels stay visible. Editor
60-cycle input/layout test settles without repeated font atlas generation.
Neither result substitutes for a native allocation profile or long-session
physical-device test.

Ordinary WebGL first-ten regression includes new cancellation/late-pointer
cases and passes with saved progress 11. The subsequent pager-only change passes
the full PlayMode suite, final QA 1-9 page navigation, same-position 8/9 reversal,
Close restoration and final ordinary-build five-size smoke. QA/release/capture
WebGL builds are current. Basic static suite passes; unloaded environment and
device signoff warnings remain. Existing iOS credentials were not removed or
replaced, and this is not a strict production-readiness pass.

All 24 `Candidates-next-build` screenshots were regenerated and pass image size,
alpha, freshness and content checks; inspected contact sheets include the new
stable last-page picker. Older submission `Candidates` remain untouched. These
are browser/native-scale candidates for a future build, not actual iOS build 2
screens or device signoff.

No iOS device was connected. Remaining external work: authorize a new iOS
candidate number above 2 and test real rendering/input/audio and SDK callbacks;
publish the previously amended privacy text; verify actual Apple processing,
review and privacy declarations; supply Android production config/signing if
shipping Android. No install/upload/deploy/commit/push/merge in this loop.

Preview remains `http://127.0.0.1:8093/index.html` (HTTP and built game checked).
Next resume: physical-device pass on an authorized new candidate, starting with
long-session target rendering and interruption recovery. Human puzzle pacing
and sound judgments remain distinct from automation. The prior checkpoint
below describes the earlier fixes, which remain in this local source too.

## Prior Checkpoint (2026-09-14)

The requested post-distribution investigation is wrapped up locally. Video frame
audit and actual-input 96-100/ending/persistence/replay checks are documented in
the gameplay audit. New fixes on top of `d2abe1b4` are NOT in uploaded build 2:

- Prevent fixed targets such as `= 111` wrapping onto two lines.
- Refresh material binding as well as glyph vertices after font atlas changes.
  Build-2-code QA reproduced missing target at Round 100; final browser sequence
  keeps it visible through placement/clear/idle/resize. Native retest is pending.
- Use native reference scale for store-capture builds. 24 new screenshots are
  kept separately in `Builds/AppStoreScreenshots/Candidates-next-build`.
- Correct local website privacy text to include this game's SDK use. The public
  page was checked and still lacks the correction; no deployment was performed.

Validation: PlayMode 28/28, all 100 samples, QA/store/release WebGL builds, first
10 rounds of ordinary-release touch input, saved progress, five viewport smokes,
24 candidate image checks, metadata/release safety and basic static suite pass.
Strict aggregate under the iOS environment fails on missing Android Firebase
JSON and production Android ad IDs. This remains a failure, not ship-ready PASS.
No connected iPhone was found. No current native runtime or production SDK
callback signoff, audio listening or indefinite-runtime validation is claimed.

Evidence: `artifacts/one-equals-one/2026-09-14-post-distribution/`.
Preview: `http://127.0.0.1:8093/index.html` (current ordinary WebGL).
No commit/push/native upload/store submission/public website deployment in this
pass. Next: authorized new iOS build number above 2, physical-device regression,
privacy deployment and owner review of actual App Store status/declarations.

The following checkpoints are historical build-specific evidence; this section
supersedes their pending-work and preview-freshness statements.

## iOS Release Build Checkpoint (2026-09-13)

### App Store Connect Upload

After commit `fa18d8d7`, the user requested Archive/distribution. The signed
`1.0.0 (1)` Archive matches the committed game sources and was uploaded using
`Builds/iOS/UploadOptions.plist` (`destination=upload`, manual signing, unchanged
version/build). Xcode reports `Upload succeeded` and `EXPORT SUCCEEDED` at
2026-09-13 20:01 KST. Delivery logs identify App Store Connect app `6811571680`.
Upload evidence: `/tmp/one-equals-one-ios-upload-1.log`.

Build `1.0.0 (2)` then added the deferred font-mesh refresh described in the
gameplay audit. Fresh strict release export, signed Archive, deep/strict
codesign verification and local IPA export all passed. Upload also passed at
2026-09-13 20:21 KST (`Upload succeeded`, `EXPORT SUCCEEDED`). **Build 2 is the
latest uploaded candidate.** It includes the font-refresh follow-up on top of
`fa18d8d7`. The user subsequently requested wrap-up and push/merge; this
checkpoint contains those source/test changes and upload evidence. Work was
already on `main`, so no separate feature-branch merge is needed. Local Firebase,
signing settings, generated archives and other projects are excluded.

- Archive: `Builds/iOS/Archives/OneEqualsOne-1.0.0-2.xcarchive`
- IPA: `Builds/iOS/Export/1.0.0-2/11.ipa` (51,351,197 bytes)
- Upload options: `Builds/iOS/UploadOptions.plist`, `destination=upload`
- Logs: `/tmp/one-equals-one-ios-release-archive-2.log`,
  `/tmp/one-equals-one-ios-release-export-2.log`,
  `/tmp/one-equals-one-ios-upload-2.log`
- Evidence copies: `artifacts/one-equals-one/2026-09-13-font-refresh/`

Apple processing started for both uploads; processing completion, TestFlight
tester assignment, installation and device QA are not yet verified. No review
submission or public release was performed. Future uploads need a new build
number. iOS display/bundle identity, production AdMob app ID and Firebase bundle
were checked in Archive 2; privacy manifests and the profile are present.

### Initial Local Archive Evidence

The user supplied the Firebase iOS plist, App Store provisioning profile and
production iOS AdMob app/interstitial IDs. Bundle ID and signing team match this
app; the profile matches the installed Apple Distribution certificate and expires
2027-08-28. Build settings are in Git-ignored
`prototypes/one-plus-one-minus-one/.env.ios.local`: version `1.0.0`, now build `2`,
team `ZRA4DHHKQ4`, profile `1 = 1`. No production IDs were invented or reused
from another game. Game logic and ad frequency were not changed.

- Strict iOS release readiness and current 100-round verification: PASS.
- Fresh release Unity-to-Xcode export: PASS.
- Signed device Archive with Xcode 26.5: PASS (`ARCHIVE SUCCEEDED`).
- Deep/strict codesign verification against the macOS trust store: PASS.
- App Store Connect-format local IPA export: PASS (`EXPORT SUCCEEDED`).
- Archive metadata: `com.mannlab.games.oneplusoneminusone`, display name `1 = 1`,
  version `1.0.0`, build `1`, minimum iOS `15.0`.
- Exported app/IPA contains Firebase config, provisioning profile and the
  production interstitial ID; archive contains app privacy manifest and dSYMs.
  GADApplicationIdentifier matches the supplied production app ID.
- Crashlytics build-phase validation succeeds; this is not console receipt of
  a device crash. Production ad delivery/consent and device play are unverified.

Artifacts (relative to `prototypes/one-plus-one-minus-one`):

- `Builds/iOS/Xcode/Unity-iPhone.xcworkspace`
- `Builds/iOS/Archives/OneEqualsOne-1.0.0-1.xcarchive`
- `Builds/iOS/Export/1.0.0-1/11.ipa` (about 49 MiB)
- `Builds/iOS/ExportOptions.plist` explicitly uses `destination=export`.

Logs: `/tmp/one-plus-one-minus-one-unity-ios-release-build.log`,
`/tmp/one-equals-one-ios-release-archive.log`,
`/tmp/one-equals-one-ios-release-export.log`.
Re-export the Unity project from the repository root with:

```sh
env BASH_ENV="$PWD/prototypes/one-plus-one-minus-one/.env.ios.local" \
  bash scripts/verify-one-plus-one-minus-one-ios-readiness.sh release
```

Build-time PlayerSettings and scene-ID churn were removed; the local environment
reapplies release version/signing values on each export. Firebase assets/meta
remain in the working tree. The initial checkpoint was committed/pushed as
`fa18d8d7`; store uploads are recorded above. The supplied App Store Connect app
ID is `6811571680`; Apple processing completion still needs checking.
This proves local iOS buildability, not complete gameplay or App Store approval.
Older iOS/Firebase-missing entries below are historical and are superseded here;
Android configuration and device QA remain; the rendering fix is browser-verified
but still needs long-session/native confirmation.

The prior WebGL build/capture gates included alternate-equality display/rotation
and persistent Sound preference. New gameplay changes below supersede those
artifacts; the old aggregate is historical evidence, not current signoff.

## Gameplay Audit Checkpoint

See `docs/one-equals-one-gameplay-quality-audit.md` for per-round observations,
attempts, evidence and remaining coverage. Rounds 11-95 were played using touch
events without sample-fill; Round 96 was opened but not attempted. Current
changes: stable bank pickups and cell hit areas, meaningful precedence examples
in 14/15, replacement of duplicate challenges 42/46, calculated-value failure
feedback, matching rotation, larger footer commands and readable status text.
The resumed pass also improved recognition labels and removed ambiguous global
last-token feedback; revised rounds 65/70/87 passed actual-input replays.
The prior full EditMode passes 27 cases; current PlayMode passes 26 after the
font fix. All 100 samples pass. QA WebGL and iOS build 2 include the font fix;
release WebGL still predates it. First-ten
input regression, five viewport smokes and static suite passed before that last
data-only change; their exact coverage is recorded in the audit checkpoint.
Earlier captures lacked a fixed target; targeted post-fix placement/failure/
resize/clear captures now retain it. See the independent follow-up evidence in
the audit. Further A work includes 96-100 and the ending flow. Neither gameplay
nor release completion is claimed. Distribution follow-up changes are included
in this checkpoint; `fa18d8d7` contains the previous checkpoint.

Previous checkpoint verification: release WebGL build/freshness smoke passed
before the font fix; not current release-WebGL signoff. Browser touch tests are
not native device, audio or production SDK signoff. Preview:
`http://127.0.0.1:8093/index.html` (older release). QA sessions are closed.

## Current Playtest Loop

The test counts below document earlier stages; the checkpoint above supersedes
them for the current source.

The 2026-09-13 uncommitted pass is based on `65578f4f` plus the existing local
release-verification edits. It adds controller PlayMode tests and actual browser
touch-input playthroughs, rather than treating sample screenshots as interaction
evidence.

- Unity PlayMode: 16 tests pass, including all 100 rounds instantiated at a
  compact portrait logical size. Tests cover target/slot overlap, bank and footer
  bounds, resize, rejected drops, second pointers, cancellation, final completion
  after controller restart, replay ad suppression, unavailable ads, and audio setup.
  All nine picker pages also meet 44px button height / 12px text at the 320x568
  simulated display, including full label-height checks.
  A conditional Privacy action is wired to the shared UMP privacy form; its
  visibility/layout is tested, while the native form still needs device QA.
  In-slot `+` to `=` rotation now spaces horizontal sticks consistently with
  bank insertion, hides the fixed target, and restores it on rotation back.
  Sound preference is preserved across round reset and controller restart.
  Browser touch also confirms the unchecked Sound state after page reload,
  with no header overlap at 320x568.
- Unity EditMode: 15 tests pass, including all round samples, alternate player
  equality, invalid equality, adjacent numbers, and left-to-right precedence.
- Browser input: Rounds 1-10 were solved with emulated touch rotation/dragging at
  390x844, DPR 3, with no sample-fill hook. Clear/ad logs and persisted Round 11
  startup were checked; restored round-select screenshot was visually inspected.
  Final picker label-height, native-platform guards and Sound UI are included
  in this run.
- Fixes include preserving failed drops, single-pointer ownership, safe drag
  cancellation, explicit CanvasRenderer dependencies, destroyed-animation guards,
  persistent final-round completion, consistent replay context, ready-only ads,
  an audio listener fallback, CSS-size-based WebGL scaling, responsive slot
  reflow, grouped last-row target alignment, and proportional bank fitting.
- Final QA, store-capture and release WebGL rebuilds passed on 2026-09-13.
  Key-round/page captures and the eight-shot store candidate set were refreshed.
  The final ship-ready aggregate passed all nine local gates; its five strict
  device/production gates failed on the external inputs listed below.
  Logs: `/tmp/one-equals-one-ship-ready-final.log` and
  `/tmp/one-equals-one-visual-refresh-final.log`.
- Strict release-env and device-signoff preflights were rerun: both correctly fail
  on missing external production settings and unfinished device QA. No native
  device touch, speaker listening, live ad callback or Firebase-console result is
  claimed by the browser or controller tests.
- Fresh native test artifacts were attempted on 2026-09-13: Android AdMob test
  APK builds successfully; iOS AdMob test Xcode export succeeds. A subsequent
  unsigned `xcodebuild` fails in the Crashlytics Run Script because the exported
  `GoogleService-Info.plist` is absent (exit 65), not a successful app build.
  Logs: `/tmp/one-equals-one-android-test-final.log`,
  `/tmp/one-equals-one-ios-test-final.log`, and
  `/tmp/one-equals-one-xcode-unsigned-final.log`.
  `xcrun devicectl list devices` found no iOS devices. After starting ADB,
  `adb devices -l` returned an empty attached-device list.

Reproduce controller tests with `./scripts/verify-one-plus-one-minus-one-playmode.sh`.
The README documents `ONE_EQUALS_ONE_INPUT_PLAYTEST=1` for the browser playthrough;
its screenshots and event report are written under `/tmp/one-equals-one-input-smoke`.
Additional emulated-touch picker/page and rejected-drop evidence is under
`/tmp/one-equals-one-input-qa`. Local release URL: `http://127.0.0.1:8093/index.html`.
No commit, push or merge was performed. Only this pass's generated scene/settings,
icon-import whitespace and Android Crashlytics build-ID churn were removed.
No build/test jobs remain running; the local release preview server remains up.

The project has stronger release infrastructure, fresh WebGL verification, and
automated viewport smoke coverage, but it must not be called commercially
complete until production Firebase/AdMob settings and store-platform readiness
pass.

## Checklist

| Area | Status | Evidence / Blocker |
| --- | --- | --- |
| Unity batch verify | pass | `BuildWebGL.Build` invokes `VerifyGoalMode.Run`; latest WebGL build succeeded. |
| WebGL build | pass | Fresh release WebGL build succeeded. |
| Existing WebGL smoke | pass | Fresh release artifact smoke passed on 2026-09-13, including title, icon, metadata and cache-busting. |
| WebGL viewport smoke | pass | SE, standard/large iPhone, Android 20:9 and desktop viewport smoke passed on 2026-09-13. Browser checks now reject Unity/JS runtime exceptions as well as invalid pixels. Physical-device density/readability remains manual. |
| WebGL QA build | pass | Development QA WebGL rebuilt successfully on 2026-09-13. |
| QA key-round/page captures | pass | Ten key rounds across five viewports, pages 1/5/9, freshness and PNG checks passed on 2026-09-13. |
| WebGL store-capture build | pass | Non-development store-capture WebGL rebuilt successfully on 2026-09-13. |
| WebGL shell metadata | pass | Existing release, QA, and store-capture WebGL shells are verified together for product title, app icon, mobile web app metadata, theme color, apple-touch-icon, build URL cache-busting, QA profiler markers, and release/store-capture non-development state. |
| App Store candidate screenshots | pass | Eight shots at three store sizes were recaptured and passed size, alpha, freshness and content checks on 2026-09-13. These are candidates, not store approval. |
| iOS AdMob test export | export pass / Xcode build blocked | Fresh export on 2026-09-13 passes. Unsigned Xcode build fails in Crashlytics Run Script on missing GoogleService-Info.plist; no app/device pass claimed. |
| Android AdMob test APK | build pass / device pending | Fresh test APK built on 2026-09-13; no Android device attached for installation or ad callbacks. |
| iOS export | blocked | Firebase plist, production AdMob ID, and version/build env are missing. |
| Android export | blocked | Firebase json, production AdMob ID, signing env, and version env are missing. |
| Release env preflight | pass with warning | Lightweight no-Unity preflight classifies missing Firebase files, production AdMob IDs, version envs, and Android signing envs; strict mode fails until external settings are present. |
| 100-round data | pass | `./scripts/verify-one-plus-one-minus-one-rounds.mjs` passes. Round quality report has no token-band review warnings after adding mid/finale equality coverage. |
| Narrow portrait layout plan | pass | Static verifier now covers 320, 390, and 488px expression widths, compact 4-row wrapping, fixed-target fit, slot aspect bounds, and tall-portrait stage lift for common phone viewports. Fresh viewport smoke passed after this source change. |
| Round select pagination | browser pass / native pending | Nine pages navigated with emulated touch; all labels and button sizes checked in PlayMode. Fresh page 1/5/9 captures pass. |
| First-run flow | browser pass / native pending | Fresh profile starts Round 1; touch-clearing 1-10 and reloading opens Rounds with 11 available and 12 locked. |
| Character readability | partial | Fresh key-round and store captures inspected; character policy passes. No character redesign. Final small-device judgment remains open. |
| Controls | browser/controller pass / native pending | Sixteen PlayMode tests and first-ten touch playthrough pass; full-slot rejection preserves placements. Real finger feel still needs device QA. |
| SFX | partial | Listener/non-silent signal and persisted Sound mute tested. Browser toggle/reload confirmed. Speaker/headphone listening has not been signed off. |
| Device QA signoff | blocked | Lightweight signoff preflight detects blank build-under-test fields, `not run` rows, WebGL mobile browser coverage gap, remaining real-touch/manual QA notes, manual-risk signoff text, and TestFlight/internal-test not-ready state. |
| Ads policy | partial | Cadence, replay/final replay, failure exclusion, ready-only show and unavailable-ad continuation tested locally. Conditional UMP Privacy action wired for native platforms. Live ad/form callbacks and production IDs remain open. |
| Firebase/Crashlytics | partial | Bridge exists. Platform config files missing. |
| App icon | pass | Source icon and WebGL favicon are 1024x1024 PNGs without alpha; generator now uses RGB24 and avoids rewriting identical PNGs. Small-size PNG visual checks pass at 180, 120, 64, and 32 px for left/center/right `1 = 1` contrast. |
| Apple privacy manifest | pass | Project includes `Assets/_Project/Store/PrivacyInfo.xcprivacy`; iOS export copies it to `PrivacyInfo.xcprivacy` in the Xcode app bundle and readiness verifies UserDefaults reason `CA92.1`. Final privacy labels still require production Firebase/AdMob review. |
| Store metadata | partial | Draft exists. Screenshots/final privacy labels need final SDK settings and store review. |

## Required To Reach Pass

Resume by keeping the present iOS Firebase plist in the next export, adding the
missing Android Firebase config, and rerunning the failed unsigned iOS Xcode
build, then device ad/consent/persistence QA on fresh test artifacts. Do not
repeat unchanged successful WebGL checks as a substitute for that evidence.

1. Firebase config state:
   - iOS plist present locally:
     `prototypes/one-plus-one-minus-one/Assets/GoogleService-Info.plist`
   - Android json still needed:
     `prototypes/one-plus-one-minus-one/Assets/google-services.json`
2. Add production AdMob app IDs and interstitial ad unit IDs.
   - Do not use `RELEASE_ENV.example` placeholder values; strict readiness
     rejects copied placeholder IDs.
   - App IDs must match `ca-app-pub-0000000000000000~0000000000`, and
     interstitial ad unit IDs must match
     `ca-app-pub-0000000000000000/0000000000`.
3. Provide Android signing env vars and Android/iOS version env vars.
   - Use `prototypes/one-plus-one-minus-one/RELEASE_ENV.example` as the checklist.
   - Version/build envs must use release-ready formats; Android keystore path
     must be an absolute path to an existing keystore file.
4. Build fresh iOS and Android artifacts.
5. Complete the device QA tracker.
6. Re-run strict readiness checks and resolve all failures after production
   config is available.

After adding the real Firebase plist and re-exporting the iOS test project,
resume the failed build-only check with:

```sh
xcodebuild -workspace prototypes/one-plus-one-minus-one/Builds/iOS/AdMobTestXcode/Unity-iPhone.xcworkspace \
  -scheme Unity-iPhone -configuration Release -sdk iphoneos \
  -destination 'generic/platform=iOS' \
  -derivedDataPath /tmp/one-equals-one-ios-unsigned CODE_SIGNING_ALLOWED=NO build
```

An unsigned build does not replace signing, device installation, or ad/consent QA.

## Current No-Unity Verification

```sh
./scripts/verify-one-plus-one-minus-one-static.sh
./scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh
./scripts/verify-one-plus-one-minus-one-ios-readiness.sh release --preflight-only
./scripts/verify-one-plus-one-minus-one-android-readiness.sh release --preflight-only
./scripts/verify-one-plus-one-minus-one-release-env.sh
./scripts/verify-one-plus-one-minus-one-release-env.sh --strict
./scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh
./scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh --strict
```

Expected current result:

- Passes static round verification.
- Passes existing WebGL artifact smoke.
- Outside-sandbox WebGL viewport smoke passes on the fresh build. The smoke
  test rejects mobile captures whose meaningful content begins too low, which
  guards against the earlier tall-portrait whitespace regression.
- Passes code readiness with Firebase/AdMob warnings.
- The AdMob/Crashlytics readiness verifier now has no-Unity fixture coverage
  for strict success with injected Firebase config files and production-shaped
  IDs, plus strict failure when Google test AdMob IDs are copied into
  production env vars.
- iOS and Android readiness now support `--preflight-only` for source/env
  validation without launching Unity. Fixture tests prove strict release
  preflight can pass with injected Firebase/keystore files and production-shaped
  IDs, and that Google test AdMob IDs fail before export. This does not replace
  native export/build verification after Unity licensing is repaired.
- The release-env preflight passes with warnings in normal mode and fails in
  strict mode until Firebase files, production AdMob IDs, version envs, and
  Android signing envs are provided. Its fixture tests cover missing values,
  placeholders, malformed values, Google test AdMob IDs, and a strict success
  path with injected Firebase config and keystore files. Its output now includes
  the required external input checklist and the strict rerun command.
- The device QA signoff preflight passes with warnings in normal mode and fails
  in strict mode until the tracker has a named build under test and all manual
  device/touch/audio/privacy rows are signed off. Its fixture tests now cover
  unknown Status values, terminal pass/fail/blocked rows without Notes evidence,
  and inconsistent TestFlight-ready signoff while external blockers remain.

## Final Gate

```sh
./scripts/verify-one-plus-one-minus-one-ship-ready.sh
```

Current expected result: fail.

Latest observed result:

- Passes static suite.
- Static suite now includes the round quality report; latest report has no
  review warnings.
- Static and ship-ready gates now verify the source icon and WebGL favicon have
  no alpha channel, matching bytes, and small-size visual contrast at launcher
  sizes.
- Static suite now verifies the stick-character policy, compact expression
  layout plans for iPhone SE/standard/narrow portrait widths, and tall-portrait
  stage lift so 20:9 mobile WebGL screens do not leave the game stranded in the
  lower half of the browser viewport.
- Static suite now verifies release-safety for QA round-jump/sample-fill and
  Crashlytics test hooks, so they stay behind development/editor/store-capture
  guards.
- iOS readiness now requires the app-level `PrivacyInfo.xcprivacy` and verifies
  the app-local UserDefaults required-reason entry.
- Passes fresh WebGL build and smoke after Unity batchmode access succeeded on
  2026-09-12.
- Fresh WebGL build output now includes browser/product metadata for
  description, application name, mobile web app title, theme color, and
  apple-touch-icon.
- Passes WebGL artifact freshness for the latest release WebGL build.
- Passes generated icon checks without relying on noisy mtime comparisons.
- WebGL viewport smoke passes outside the sandbox; inside a restricted sandbox
  it now exits with a clear local-port permission message instead of a Node
  stack trace. The smoke test also rejects mobile captures whose meaningful
  content starts too far down the viewport.
- App Store candidate verification now rejects stale captures whose source or
  store-capture build is newer than the PNGs; latest fresh candidates pass.
- Ship-ready now also verifies existing release, QA, and store-capture WebGL
  shells as a group, so screenshot freshness cannot hide an outdated browser
  title, icon, mobile web app metadata, theme color, apple-touch-icon, build URL
  cache-busting, missing QA profiler marker, or leaked release/store-capture
  development marker.
- App Store candidate capture now recreates managed `??-*` folders and
  verification rejects unexpected stale candidate directories, so old numbered
  screenshots cannot sit beside the current upload set.
- QA capture verification now covers Rounds 1, 5, 8, 9, 16, 30, 50, 75, 90,
  and 100 plus round-select pages 1, 5, and 9.
- QA round/page capture verification also rejects unexpected stale managed
  `round-*` and `page-*` directories.
- QA capture and App Store candidate verification now share PNG pixel checks so
  correctly sized but blank, too-dark, alpha-bearing, or vertically misplaced
  screenshots are rejected.
- Ship-ready now runs a strict lightweight release-env preflight before the
  heavier platform readiness scripts, so external configuration blockers are
  reported clearly without relying on Unity export failures.
- Ship-ready now also runs strict iOS/Android release preflight-only checks
  before the Unity-gated native export/build checks, so platform env/source
  blockers remain visible while Unity licensing is unavailable.
- Ship-ready now also runs strict device QA signoff, so automated screenshot
  coverage cannot be mistaken for real-device completion.
- Historical ship-ready run on 2026-09-12 passed every local build, freshness,
  viewport, WebGL metadata, icon, QA capture, App Store screenshot, and PNG
  visual-content gate before failing on strict device QA signoff and missing
  production Firebase/AdMob/platform release settings.
- Strict AdMob readiness now rejects malformed production app/ad unit ID
  values, not only missing, placeholder, or Google test values.
- Static suite now includes focused fixture tests for AdMob/Crashlytics
  readiness, platform preflight readiness, release-env readiness, device QA
  signoff, and store metadata verifier failure modes.
- iOS/Android readiness also reject malformed production AdMob IDs before a
  release export is attempted.
- iOS/Android readiness now validate release version/build formats and Android
  keystore path shape before a release export is attempted.
- iOS AdMob test export passes with version-env warnings.
- Android AdMob test APK passes.
- Fails strict Firebase/AdMob code readiness because platform config files and
  production interstitial ID env vars are missing. The check also rejects
  Google test interstitial IDs in production env vars.
- Fails strict iOS release readiness because Firebase plist, production AdMob
  app ID, and iOS version env vars are missing.
- Fails strict Android release readiness because Firebase json, production
  AdMob app/ad unit IDs, signing env vars, and Android version env vars are
  missing.

2026-09-20 to 2026-09-26 local source delta: the capacity-slot tutorial
experiment was discarded before release. The current default player path is the
30-round `= 1` ladder, with the 100-round Goal ladder retained as hidden legacy
content for QA/backward coverage. Current local work adds
collection/achievement UI, unlock toasts, per-round solved-expression
collection, and QA input-probe automation for the current default ladder.
PlayMode, static verification, fresh QA WebGL, fresh ordinary WebGL and default
first-ten browser input regression all pass for this local source. It is still
not equivalent to the uploaded iOS build 4 until a new native archive is built
and uploaded.

2026-09-26 MakeOne pattern quality follow-up: the default 30-round `= 1`
ladder now has stronger pattern-review tooling for repeated slot/stick
resources and universal-key answers. Round 23 was changed to `Thin Return`
with sample `1 / 11 * 11 - 1 + 1`, breaking the previous repeated `11/20`
resource pairing with Round 22 while keeping target `1` and free alternate
answers. The resource candidate CLI now accepts either explicit round numbers
or repeated resource keys such as `5/8`, so repeated-resource groups can be
probed directly. Follow-up tooling keeps composite target-1 candidates from
consuming the nearby-enumeration budget and reports both
`compositeCandidateCount` and `enumeratedCandidateCount`, making repeated-group
probes less likely to falsely look exhausted. It also preserves source-diverse
evaluation under small caps, so `candidateSummary.sourceCounts` reflects both
composite target-1 and nearby-enumeration candidates when both sources exist.
Occupied resources are reported as `occupied-resource-swap` analysis-only
candidates, and `candidateSummary.bestBySource` keeps each source's strongest
evaluated example visible even when the final result list is short.
Round 26's authored MakeOne sample is now `111 - 11 × 11 + 11`, replacing the
previous visible `N/N * N/N` chain while keeping the same 7-slot/14-stick
resource budget and alternate-answer behavior.
Round 25 and Round 30 also now use cleaner authored paths within their existing
resource budgets: `1 / 1 1 1 × 111` and `1 / 111 111 × 111 111`, respectively.
Pattern reports also include `shortcutPolicy.authoredSampleReview`, so visible
late-round shortcut samples can be reviewed separately from valid alternate
answers.
Resource candidate reports now also classify `highEqualityEcho` candidates,
which prevents Round 30-style replacements that merely swap visible `N/N` for a
same-expression equality dominated answer map from looking recommendable.
`node --test
scripts/test-one-plus-one-minus-one-round-identity.mjs` passes 60/60 and
`bash scripts/verify-one-plus-one-minus-one-static.sh` passes. The current
WebGL artifacts are still older than the latest source because a fresh WebGL QA
build is blocked by Unity licensing initialization timing out; do not use the
stale WebGL as runtime evidence for these MakeOne changes.
