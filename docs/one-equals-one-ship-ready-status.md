# 1 = 1 Ship-Ready Status

Status date: 2026-09-12

Current completion judgment: `not yet`

The project has stronger release infrastructure, fresh WebGL verification, and
automated viewport smoke coverage, but it must not be called commercially
complete until production Firebase/AdMob settings and store-platform readiness
pass.

## Checklist

| Area | Status | Evidence / Blocker |
| --- | --- | --- |
| Unity batch verify | pass | `BuildWebGL.Build` invokes `VerifyGoalMode.Run`; latest WebGL build succeeded. |
| WebGL build | pass | Fresh release WebGL build succeeded. |
| Existing WebGL smoke | pass | Fresh release WebGL build and artifact smoke passed on 2026-09-12, including title, app icon, cache-busting, mobile web app metadata, theme color, and apple-touch-icon checks. |
| WebGL viewport smoke | pass | Fresh iPhone SE, standard iPhone, large iPhone, Android 20:9, and desktop screenshots passed outside the sandbox on 2026-09-12 after the tall-portrait stage lift. Very tall first screens still have generous whitespace, so final device QA should judge first-screen density. |
| WebGL QA build | pass | Development QA WebGL build succeeded on 2026-09-12 and supports key-round and round-select screenshot entry. |
| QA key-round/page captures | pass | Fresh QA WebGL build, expanded key-round captures, round-select page captures, stale managed-folder checks, freshness checks, and PNG visual verification passed on 2026-09-12. |
| WebGL store-capture build | pass | Fresh non-development store-capture WebGL build passed on 2026-09-12. |
| App Store candidate screenshots | pass | App Store candidate PNGs were recaptured from the fresh store-capture build and verified for the 8-shot Round 1/5/8/9/30/75/100/round-select set, expected folder list, size, alpha, freshness, and PNG visual content on 2026-09-12. |
| iOS AdMob test export | pass with warning | Xcode project export verified; iOS version env vars are not set. |
| Android AdMob test APK | pass | Test APK build verified. |
| iOS export | blocked | Firebase plist, production AdMob ID, and version/build env are missing. |
| Android export | blocked | Firebase json, production AdMob ID, signing env, and version env are missing. |
| Release env preflight | pass with warning | Lightweight no-Unity preflight classifies missing Firebase files, production AdMob IDs, version envs, and Android signing envs; strict mode fails until external settings are present. |
| 100-round data | pass | `./scripts/verify-one-plus-one-minus-one-rounds.mjs` passes. Round quality report has no token-band review warnings after adding mid/finale equality coverage. |
| Narrow portrait layout plan | pass | Static verifier now covers 320, 390, and 488px expression widths, compact 4-row wrapping, fixed-target fit, slot aspect bounds, and tall-portrait stage lift for common phone viewports. Fresh viewport smoke passed after this source change. |
| Round select pagination | partial | Code supports 9 pages, responsive panel lift on tall phones, and compact last-page grid height; QA captures for pages 1, 5, and 9 pass smoke. Needs real touch/device QA. |
| First-run flow | partial | Code path exists. Needs fresh build/runtime QA. |
| Character readability | partial | Code-side `1`, `=`, and `*` polish exists. Character-policy automation now rejects arms/hands/legs/cheeks/blush regressions. Fresh viewport screenshots and manual readability pass remain. |
| Controls | partial | Drag offset, hover, tap rotate, and drag-out return exist. Needs touch QA. |
| SFX | partial | Generated SFX and cooldown exist. Needs audio QA. |
| Device QA signoff | blocked | Lightweight signoff preflight detects blank build-under-test fields, `not run` rows, WebGL mobile browser coverage gap, remaining real-touch/manual QA notes, manual-risk signoff text, and TestFlight/internal-test not-ready state. |
| Ads policy | partial | Code policy/readiness checks exist. Production IDs are missing, and strict checks now reject copied placeholder IDs as well as Google test IDs. |
| Firebase/Crashlytics | partial | Bridge exists. Platform config files missing. |
| App icon | pass | Source icon and WebGL favicon are 1024x1024 PNGs without alpha; generator now uses RGB24 and avoids rewriting identical PNGs. Small-size PNG visual checks pass at 180, 120, 64, and 32 px for left/center/right `1 = 1` contrast. |
| Apple privacy manifest | pass | Project includes `Assets/_Project/Store/PrivacyInfo.xcprivacy`; iOS export copies it to `PrivacyInfo.xcprivacy` in the Xcode app bundle and readiness verifies UserDefaults reason `CA92.1`. Final privacy labels still require production Firebase/AdMob review. |
| Store metadata | partial | Draft exists. Screenshots/final privacy labels need final SDK settings and store review. |

## Required To Reach Pass

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
- Latest ship-ready run on 2026-09-12 passes every local build, freshness,
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
