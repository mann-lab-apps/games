# 1 = 1 Ship-Ready Status

Status date: 2026-09-13

Current completion judgment: `not yet`

Stage: development checkpoint; native/production release validation pending.

Goal state: user-requested pause after Round 95; Round 96 opened, not attempted.
The intermittent fixed-target rendering observation received a font-refresh
fix during the iOS distribution follow-up; targeted browser checks pass and
native/long-session confirmation remains pending.
The previous release-only loop was
blocked on external inputs, but that did not prove the remaining gameplay work
was complete. The resumed scope separates actionable gameplay work (A) from
native/production validation (B). Resume actual-input play through 96-100 and
confirm rendering on native devices; production validation does not block A.
The earlier missing-config iOS failure is superseded by the native release
checkpoint below; gameplay investigation remains paused by the user.

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

Resume by adding the real Firebase configs and rerunning the failed unsigned
iOS Xcode build, then device ad/consent/persistence QA on fresh test artifacts. Do not
repeat unchanged successful WebGL checks as a substitute for that evidence.

1. Add Firebase configs:
   - `prototypes/one-plus-one-minus-one/Assets/GoogleService-Info.plist`
   - `prototypes/one-plus-one-minus-one/Assets/google-services.json`
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
- The release-env preflight passes with warnings in normal mode and fails in
  strict mode until Firebase files, production AdMob IDs, version envs, and
  Android signing envs are provided. Its output now includes the required
  external input checklist and the strict rerun command.
- The device QA signoff preflight passes with warnings in normal mode and fails
  in strict mode until the tracker has a named build under test and all manual
  device/touch/audio/privacy rows are signed off.

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
- Ship-ready now also runs strict device QA signoff, so automated screenshot
  coverage cannot be mistaken for real-device completion.
- Historical ship-ready run on 2026-09-12 passed every local build, freshness,
  viewport, WebGL metadata, icon, QA capture, App Store screenshot, and PNG
  visual-content gate before failing on strict device QA signoff and missing
  production Firebase/AdMob/platform release settings.
- Strict AdMob readiness now rejects malformed production app/ad unit ID
  values, not only missing, placeholder, or Google test values.
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
