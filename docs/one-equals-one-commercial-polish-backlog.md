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
| Layout | Tall 20:9 WebGL viewports could leave the first screen visually stranded too low with too much blank space above. | Added a centralized stage layout plan that gently lifts tall-portrait stages while keeping SE/desktop layouts centered, then mirrored the rule in Unity and Node verification. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs`, `./scripts/verify-one-plus-one-minus-one-static.sh`, fresh WebGL build, and fresh viewport smoke pass. |
| Controls | Dragged sticks could be hidden by the finger. | Added pointer-offset drag ghost. | Needs fresh build and touch QA. |
| Controls | Drop intent was hard to read. | Added hover tint on the slot under the pointer. | Needs fresh build and visual QA. |
| Controls | Placed stick rotation was indirect. | Added tap-to-rotate for placed sticks and drag-out return. | Needs fresh build and touch QA. |
| Feedback | Success/fail state felt quiet. | Added slot bumps, equation shake, randomized short result copy, and gentle generated SFX. | Static policy checks pass; needs audio QA. |
| Characters | `x`/`×` and `*` were too similar. | Split `*` into a dedicated Star face style. | Needs fresh build and visual QA. |
| Characters | Character direction could regress into arms, hands, legs, cheeks, or blush after polish passes. | Added a character-policy verifier that rejects forbidden body-part/blush code and requires the symbol-based face style hooks to remain. | `./scripts/verify-one-plus-one-minus-one-character-policy.sh`; included in static suite. |
| Round select | Fixed 520x760 round-select panel could overflow or feel detached on narrow screens. | Added safe-area-based panel, narrow-screen fallbacks, and 3-column cell resizing. | Needs fresh build and mobile visual QA. |
| Round select | Round-select sizing had no static guard for 100-round mobile pages. | Added C#/JS layout-plan verification for common portrait safe sizes. | `./scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Round select | Round-select pages sat too low on tall mobile WebGL captures, making the overlay feel detached from the first viewport. | Added portrait-aware panel lift and static top-position guards for 19.5:9-20:9 screens. | Fresh round-select page 1/5/9 captures pass across iPhone SE, standard iPhone, large iPhone, Android 20:9, and desktop. |
| Round select | The final 100-round page kept a full 4-row grid height even when only four rounds were visible. | Compacted round-select grid height by visible row count, so page 9 pulls pager and Close closer to the remaining buttons. | Fresh page 9 captures pass and visual spot check looks tighter. |
| Round select | Underlying game screen could show through the overlay. | Made the round-select blocker fully opaque. | Needs fresh build and visual QA. |
| Round select | Page 9 and disabled pager states were not easy to verify repeatedly. | Added dev-only `qaRoundPage` startup override and round-select page capture script; strengthened disabled command-button visuals. | Page 9 iPhone SE capture spot-check passes. |
| Icon | `=` character becomes thin at small icon sizes. | Increased generated icon stick/equal proportions and reduced stick roundness. | Needs small-size visual QA. |
| Icon | Generated PNG kept an alpha channel even though the icon is visually opaque. | Switched icon generation to `TextureFormat.RGB24`, stripped alpha from current icon/WebGL favicon, and added an icon verifier. | `./scripts/verify-one-plus-one-minus-one-icon.sh` passes and is part of static/ship-ready checks. |
| Icon | Icon freshness used noisy mtime checks even though the generator can produce identical bytes. | Made `GenerateAppIcon.Run` skip identical PNG writes and changed icon verification to byte-compare source and WebGL icons. | Static suite and ship-ready icon gate pass without icon mtime warnings. |
| Icon | Small launcher-size readability was still a manual check, so the icon could pass file checks while `1 = 1` became too faint or narrow. | Added a PNG-based small-icon visual verifier that downscales to 180, 120, 64, and 32 px and checks content contrast across left, center, and right regions. | `./scripts/verify-one-plus-one-minus-one-icon-visuals.mjs`; included through the icon verifier and static suite. |
| Automation | Unity license blocked round data verification. | Added Node-based static 100-round verifier. | `node scripts/verify-one-plus-one-minus-one-rounds.mjs`. |
| Rounds | Direct equality rounds disappeared from the mid and finale bands. | Replaced two target-only puzzles with neutral-title equality puzzles: Round 44 `Even Still`, Round 98 `Final Balance`. | 100-round verifier passes; quality report has no review warnings. |
| Automation | Round quality review depended on manually eyeballing 100 entries. | Added a round quality report for token bands, token introductions, target spread, adjacent pattern runs, and review warnings; mirrored token-band coverage checks in the Unity verifier. | `./scripts/report-one-plus-one-minus-one-round-quality.mjs --strict`; included in static suite. |
| Automation | Existing WebGL artifact smoke was coupled to Unity rebuild. | Added build artifact smoke script. | `bash scripts/smoke-one-plus-one-minus-one-webgl-build.sh`. |
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

| Area | Risk | Classification | Next Slice |
| --- | --- | --- | --- |
| Analytics | Firebase config files are missing. | external blocker | Add `Assets/GoogleService-Info.plist` and `Assets/google-services.json` for `com.mannlab.games.oneplusoneminusone`. |
| Ads | Production AdMob IDs are blank or still test IDs. | external blocker | Add production app/ad unit IDs, then run strict readiness. |
| Android | Release signing env vars are unset. | external blocker | Add keystore path/password and key alias env vars before Play Store AAB builds. |
| Store | Final privacy labels are not confirmed against a configured production build. | manual QA | Re-check SDK data collection after final Firebase/AdMob configs. |
| Device QA | Touch feel, SFX loudness, safe area, and small-slot character readability are unverified. | manual QA | Run device/simulator QA across target screens. |
| Visual QA | Tall iPhone and Android 20:9 fresh screenshots still feel spacious on the first tutorial screen even though the stranded-lower-half regression is fixed. | manual QA | Judge first-screen density on real devices; consider a future title/header compaction pass only if it feels empty in hand. |

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
