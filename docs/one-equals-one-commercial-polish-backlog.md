# 1 = 1 Commercial Polish Backlog

This document tracks issues found while pushing `1 = 1` from prototype quality
toward a commercial casual puzzle release.

## Current Gate

### Gameplay Quality Loop (2026-09-13 User-Requested Pause)

- A / paused: actual-input investigation completed through Round 95;
  Round 96 opened but not attempted. Finish 96-100 and the ending flow on resume.
  See the gameplay quality audit for expressions, sample exposure and limits.
- A / P1 open: fixed target absent in populated Round 80 and revised Round 87
  captures, despite being present before placement/check. Fresh Round 80 replay
  did not reproduce it. Correct calculation does not prove rendering fixed.
  Next investigation: capture the failing state and establish a reproduction;
  acceptance is persistent target visibility for fixed-target rounds, including
  placement/check/resize and long sessions, while player equalities hide it.
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
  Firebase settings, signing and store submission. B does not block A.
- Some early/mid examples appeared in prior conversation, so the audit is not
  a novice blind playtest. Current attempts avoid opening SampleSolution data.
- Browser observation is Chrome emulation with actual touch events, not a
  physical phone or a human learning-time study. Input trace/screenshots:
  `/tmp/one-equals-one-gameplay` and `/tmp/one-equals-one-gameplay-resumed`.

The entries below are chronological findings, not current pending-state claims.
User-requested pause is not gameplay completion or ship-ready signoff. No commit,
push or merge; release preview remains `http://127.0.0.1:8093/index.html`.

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
| Store | Store copy could fit structurally but still be too long, empty, or out of sync with screenshot capture slugs. | Strengthened store metadata verification with conservative local copy budgets, keyword-shape checks, required release-env notes, and screenshot slug alignment against the App Store candidate capture script. | `./scripts/verify-one-plus-one-minus-one-store-metadata.sh`; included in static suite. |
| Store | iOS export had no app-level privacy manifest under project control. | Added `PrivacyInfo.xcprivacy` with app-local UserDefaults reason `CA92.1`, copied it into the Xcode app bundle, and made iOS readiness require it. | Static suite passes; iOS readiness reaches Unity license/version-env blockers. |
| Analytics | `app_open` had little context for progress/debugging. | Added app open parameters and progress/replay context on round events. | Static readiness checks required telemetry text. |
| Analytics | Readiness only checked that events existed, not that useful debugging context stayed attached. | Added static checks for round name, stick count, slot count, expression, failure count, cadence, ad show decision, app version, and platform parameters. | `./scripts/verify-one-plus-one-minus-one-static.sh` passes. |
| Ads | Production interstitial IDs were source constants, so env-only release setup could never fully pass. | Added build-time `Resources` config generation for iOS/Android interstitial ad units and strict env checks. | Static suite and readiness scripts cover env names and reject Google test interstitial IDs in production env vars. |
| Ads | Release build entry points could rely on test/default AdMob app IDs if env was missing. | Made iOS/Android release build methods require production AdMob app IDs and reject Google test app/ad-unit IDs before building. | iOS/Android AdMob-test builds still pass; strict readiness still fails on missing production config as intended. |
| Ads | Strict readiness could miss copied placeholder AdMob IDs from `RELEASE_ENV.example`. | Added placeholder rejection for production AdMob app/ad-unit env vars in common, iOS, Android readiness and release build scripts. | Placeholder env smoke checks fail as intended; static suite passes. |
| Ads | Strict readiness could accept non-empty but malformed production AdMob IDs. | Added production AdMob app/ad-unit format validation in common, iOS, and Android readiness scripts, plus release env documentation. | Invalid-ID strict smoke fails as intended; static suite passes. |
| Release | Release env checks only verified presence for versions and Android signing. | Added iOS/Android marketing version, build/version-code, Android keystore path format/existence validation, plus env documentation. | Invalid-version/signing smoke fails as intended; static suite passes. |
| Release | External release blockers were scattered across heavier Unity/platform readiness scripts. | Added a no-Unity release-env preflight for Firebase configs, production AdMob IDs, iOS/Android version envs, and Android signing envs; strict ship-ready runs it before platform exports. | Default preflight passes with warnings; strict and malformed-env smoke checks fail as intended. |
| Device QA | Ship-ready could pass automated visual gates without explicitly failing on unfinished manual device QA. | Added a device QA signoff preflight that checks the tracker for blank build-under-test fields, `not run` rows, incomplete WebGL mobile coverage, and remaining real-touch/manual QA notes; strict ship-ready runs it before external config checks. | Default preflight passes with warnings; strict mode fails until manual QA is signed off. |

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
| Analytics | Firebase config files are missing. | external blocker | Add `Assets/GoogleService-Info.plist` and `Assets/google-services.json` for `com.mannlab.games.oneplusoneminusone`. |
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
