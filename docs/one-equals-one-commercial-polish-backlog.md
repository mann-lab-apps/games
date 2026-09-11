# 1 = 1 Commercial Polish Backlog

This document tracks issues found while pushing `1 = 1` from prototype quality
toward a commercial casual puzzle release.

## Current Gate

`1 = 1` should not be called commercially complete until a fresh Unity build and
device QA pass confirm the current source state.

Current checklist status is tracked in
`docs/one-equals-one-ship-ready-status.md`.

## Done

| Area | Issue | Slice | Verification |
| --- | --- | --- | --- |
| Layout | Dense expressions compressed slot proportions. | Added multi-row equation layout with release minimum slot dimensions. | Unity verifier and static round verifier layout checks. |
| Layout | Compact phones were under-tested, so long expressions could still assume a wider logical row than the safe area. | Equation layout now uses the current safe width, can fold compact layouts to 4 rows, shrinks fixed-target text width on narrow screens, and caps 4-row slot width to keep aspect stable. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs` now checks 320, 390, and 488px widths plus slot aspect bounds. |
| Controls | Dragged sticks could be hidden by the finger. | Added pointer-offset drag ghost. | Needs fresh build and touch QA. |
| Controls | Drop intent was hard to read. | Added hover tint on the slot under the pointer. | Needs fresh build and visual QA. |
| Controls | Placed stick rotation was indirect. | Added tap-to-rotate for placed sticks and drag-out return. | Needs fresh build and touch QA. |
| Feedback | Success/fail state felt quiet. | Added slot bumps, equation shake, randomized short result copy, and gentle generated SFX. | Static policy checks pass; needs audio QA. |
| Characters | `x`/`×` and `*` were too similar. | Split `*` into a dedicated Star face style. | Needs fresh build and visual QA. |
| Characters | Character direction could regress into arms, hands, legs, cheeks, or blush after polish passes. | Added a character-policy verifier that rejects forbidden body-part/blush code and requires the symbol-based face style hooks to remain. | `./scripts/verify-one-plus-one-minus-one-character-policy.sh`; included in static suite. |
| Round select | Fixed 520x760 round-select panel could overflow or feel detached on narrow screens. | Added safe-area-based panel, narrow-screen fallbacks, and 3-column cell resizing. | Needs fresh build and mobile visual QA. |
| Round select | Round-select sizing had no static guard for 100-round mobile pages. | Added C#/JS layout-plan verification for common portrait safe sizes. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Round select | Underlying game screen could show through the overlay. | Made the round-select blocker fully opaque. | Needs fresh build and visual QA. |
| Round select | Page 9 and disabled pager states were not easy to verify repeatedly. | Added dev-only `qaRoundPage` startup override and round-select page capture script; strengthened disabled command-button visuals. | Page 9 iPhone SE capture spot-check passes. |
| Icon | `=` character becomes thin at small icon sizes. | Increased generated icon stick/equal proportions and reduced stick roundness. | Needs small-size visual QA. |
| Icon | Generated PNG kept an alpha channel even though the icon is visually opaque. | Switched icon generation to `TextureFormat.RGB24`, stripped alpha from current icon/WebGL favicon, and added an icon verifier. | `./scripts/verify-one-plus-one-minus-one-icon.sh` passes and is part of static/ship-ready checks. |
| Automation | Unity license blocked round data verification. | Added Node-based static 100-round verifier. | `node scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Rounds | Direct equality rounds disappeared from the mid and finale bands. | Replaced two target-only puzzles with neutral-title equality puzzles: Round 44 `Even Still`, Round 98 `Final Balance`. | 100-round verifier passes; quality report has no review warnings. |
| Automation | Round quality review depended on manually eyeballing 100 entries. | Added a round quality report for token bands, token introductions, target spread, adjacent pattern runs, and review warnings; mirrored token-band coverage checks in the Unity verifier. | `./scripts/report-one-plus-one-minus-one-round-quality.mjs --strict`; included in static suite. |
| Automation | Existing WebGL artifact smoke was coupled to Unity rebuild. | Added build artifact smoke script. | `bash scripts/smoke-one-plus-one-minus-one-webgl-build.sh`. |
| Automation | No-Unity checks were scattered across multiple commands. | Added static verification suite. | `./scripts/verify-one-plus-one-minus-one-static.sh`. |
| Automation | Existing WebGL smoke could pass an outdated build silently. | Added source-newer-than-build freshness warning. | Static suite now reports stale builds. |
| Automation | QA round-jump/sample-fill and test-crash hooks could accidentally leak into commercial release builds. | Added a release-safety verifier that evaluates release-active preprocessor lines and rejects QA/test hooks outside development/editor/store-capture guards. | `./scripts/verify-one-plus-one-minus-one-release-safety.mjs`; included in static suite. |
| Automation | Visual QA was only a manual checklist. | Added Chrome/CDP WebGL viewport smoke screenshots for mobile and desktop sizes. | `./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs`. |
| Automation | Viewport smoke could save a screenshot even if the page was visually blank. | Added PNG pixel inspection to fail nearly blank or overly dark captures. | `./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs` and static suite pass. |
| Automation | Viewport smoke failed with a raw Node stack trace when local port binding was sandbox-blocked. | Added a clear EPERM/listen error message that explains the required outside-sandbox execution. | Sandbox run now fails cleanly; outside-sandbox viewport smoke passes. |
| Automation | Key late-round visual QA had no direct entry point. | Added a development-only WebGL QA build and `qaRound`/`qaUnlocked` startup overrides, plus a capture wrapper for Round 1, 30, 50, 75, 90, and 100. | QA WebGL build passes; Rounds 30, 50, 75, 90, and 100 screenshots captured and spot-checked. |
| Automation | QA round and round-select screenshots could go stale without detection. | Added a QA capture verifier for required key rounds, pages, viewport dimensions, and QA-build freshness. | Current `/tmp` captures fail correctly until QA build/captures are regenerated. |
| Store | App Store screenshot capture was a manual loose checklist. | Added store-sized WebGL candidate capture for 6.5-inch iPhone, 6.9-inch iPhone, and 13-inch iPad; QA fill can place sample solutions for stronger shots. | Candidate screenshots generated and size/alpha verifier passes. |
| Store | Candidate screenshots could be stale or come from a mismatched capture build. | Candidate verifier now requires a non-development store-capture build and fails screenshots older than that build. | Current run fails correctly until Unity license is restored, store-capture is rebuilt, and candidates are recaptured. |
| Build | Store-capture WebGL verification could hang when Unity licensing failed during batchmode startup. | Added the same Unity CLI license precheck used by other build scripts. | `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh` now exits quickly with a license error. |
| Automation | Commercial completion required humans to combine many scripts. | Added one final ship-ready gate that aggregates fresh build, freshness, icon, viewport, Firebase, AdMob, iOS, and Android checks. | `./scripts/verify-one-plus-one-minus-one-ship-ready.sh`. |
| Android | README listed Android, but no Android build/readiness path existed. | Added Android AAB/APK/AdMob-test build methods and readiness script. | AdMob-test APK passes; release AAB still needs signing env and production config. |
| iOS | AdMob test export was not proven end to end. | Ran and fixed iOS AdMob-test readiness so Xcode placeholder bundle IDs resolve through pbxproj. | `./scripts/verify-one-plus-one-minus-one-ios-readiness.sh admob-test`. |
| Android | AdMob test APK path was not proven end to end. | Ran Android AdMob-test build successfully. | `./scripts/verify-one-plus-one-minus-one-android-readiness.sh admob-test`. |
| Store | Store copy and privacy disclosure were only embedded in readiness notes. | Added dedicated store metadata draft and linked privacy policy. | Needs final build and SDK settings before submission. |
| Store | Store metadata could drift away from required submission fields. | Added a store metadata verifier for identity, privacy URL, screenshots, ads/privacy disclosure, age-rating notes, final checks, and unresolved placeholders. | `./scripts/verify-one-plus-one-minus-one-store-metadata.sh`; included in static suite. |
| Store | iOS export had no app-level privacy manifest under project control. | Added `PrivacyInfo.xcprivacy` with app-local UserDefaults reason `CA92.1`, copied it into the Xcode app bundle, and made iOS readiness require it. | Static suite passes; iOS readiness reaches Unity license/version-env blockers. |
| Analytics | `app_open` had little context for progress/debugging. | Added app open parameters and progress/replay context on round events. | Static readiness checks required telemetry text. |
| Analytics | Readiness only checked that events existed, not that useful debugging context stayed attached. | Added static checks for round name, stick count, slot count, expression, failure count, cadence, ad show decision, app version, and platform parameters. | `./scripts/verify-one-plus-one-minus-one-static.sh` passes. |
| Ads | Production interstitial IDs were source constants, so env-only release setup could never fully pass. | Added build-time `Resources` config generation for iOS/Android interstitial ad units and strict env checks. | Static suite and readiness scripts cover env names and reject Google test interstitial IDs in production env vars. |
| Ads | Release build entry points could rely on test/default AdMob app IDs if env was missing. | Made iOS/Android release build methods require production AdMob app IDs and reject Google test app/ad-unit IDs before building. | iOS/Android AdMob-test builds still pass; strict readiness still fails on missing production config as intended. |
| Ads | Strict readiness could miss copied placeholder AdMob IDs from `RELEASE_ENV.example`. | Added placeholder rejection for production AdMob app/ad-unit env vars in common, iOS, Android readiness and release build scripts. | Placeholder env smoke checks fail as intended; static suite passes. |

## Active Risks

| Area | Risk | Classification | Next Slice |
| --- | --- | --- | --- |
| Analytics | Firebase config files are missing. | external blocker | Add `Assets/GoogleService-Info.plist` and `Assets/google-services.json` for `com.mannlab.games.oneplusoneminusone`. |
| Ads | Production AdMob IDs are blank or still test IDs. | external blocker | Add production app/ad unit IDs, then run strict readiness. |
| Android | Release signing env vars are unset. | external blocker | Add keystore path/password and key alias env vars before Play Store AAB builds. |
| Store | Final privacy labels are not confirmed against a configured production build. | manual QA | Re-check SDK data collection after final Firebase/AdMob configs. |
| Store | App Store candidate screenshots are stale relative to the current store-capture build/source state. | build blocker | Restore Unity license, run `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh`, then rerun `./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh` and the candidate verifier. |
| Device QA | Touch feel, SFX loudness, safe area, and small-slot character readability are unverified. | manual QA | Run device/simulator QA across target screens. |
| Visual QA | Browser viewport smoke is automated, but key-round QA captures are stale after the latest source/layout/icon changes. | build blocker | Restore Unity license, rebuild QA WebGL, recapture key rounds/pages, then run `./scripts/verify-one-plus-one-minus-one-qa-captures.sh`. |

## Next High-Value Loops

1. Fresh build loop:
   - Run Unity `VerifyGoalMode.Run`.
   - Build WebGL.
   - Run existing WebGL smoke.
   - Visually inspect Round 1, 5, 8, 9, 30, 50, 75, 90, and 100.

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
