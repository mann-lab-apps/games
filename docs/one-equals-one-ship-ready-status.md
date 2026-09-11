# 1 = 1 Ship-Ready Status

Status date: 2026-09-11

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
| Existing WebGL smoke | pass with warning | Existing artifact smoke passes, but the release WebGL build is stale relative to current source and must be rebuilt before completion. |
| WebGL viewport smoke | pass | iPhone SE, standard iPhone, large iPhone, Android 20:9, and desktop screenshots captured. The script now reports local-port sandbox failures clearly; outside-sandbox smoke passed on 2026-09-11. |
| WebGL QA build | stale evidence | Development QA WebGL build exists and supports key-round and round-select screenshot entry, but current source is newer and fresh rebuild is blocked by Unity license. |
| QA key-round/page captures | stale | Required capture verifier exists and fails current `/tmp` captures because they are older than the QA build and some older screenshots have mismatched viewport dimensions. |
| WebGL store-capture build | blocked | Store-capture verifier now fast-fails when no Unity Editor license is active instead of hanging during batchmode startup. Rebuild after license activation. |
| App Store candidate screenshots | stale | Existing candidate PNGs pass visual spot check and size/alpha checks, but verification now fails because the store-capture build is older than project settings. Rebuild store-capture and recapture. |
| iOS AdMob test export | pass with warning | Xcode project export verified; iOS version env vars are not set. |
| Android AdMob test APK | pass | Test APK build verified. |
| iOS export | blocked | Firebase plist, production AdMob ID, and version/build env are missing. |
| Android export | blocked | Firebase json, production AdMob ID, signing env, and version env are missing. |
| 100-round data | pass | `./scripts/verify-one-plus-one-minus-one-rounds.mjs` passes. Round quality report has no token-band review warnings after adding mid/finale equality coverage. |
| Narrow portrait layout plan | pass | Static verifier now covers 320, 390, and 488px expression widths, compact 4-row wrapping, fixed-target fit, and slot aspect bounds. Fresh WebGL screenshots are still required because source changed after the last capture. |
| Round select pagination | partial | Code supports 9 pages and responsive panel; QA captures for pages 1, 5, and 9 pass smoke. Needs real touch/device QA. |
| First-run flow | partial | Code path exists. Needs fresh build/runtime QA. |
| Character readability | partial | Code-side `1`, `=`, and `*` polish exists. Character-policy automation now rejects arms/hands/legs/cheeks/blush regressions. Fresh viewport screenshots and manual readability pass remain. |
| Controls | partial | Drag offset, hover, tap rotate, and drag-out return exist. Needs touch QA. |
| SFX | partial | Generated SFX and cooldown exist. Needs audio QA. |
| Ads policy | partial | Code policy/readiness checks exist. Production IDs are missing, and strict checks now reject copied placeholder IDs as well as Google test IDs. |
| Firebase/Crashlytics | partial | Bridge exists. Platform config files missing. |
| App icon | pass | Source icon and WebGL favicon are 1024x1024 PNGs without alpha; generator now uses RGB24. Small-size visual QA remains manual. |
| Apple privacy manifest | pass | Project includes `Assets/_Project/Store/PrivacyInfo.xcprivacy`; iOS export copies it to `PrivacyInfo.xcprivacy` in the Xcode app bundle and readiness verifies UserDefaults reason `CA92.1`. Final privacy labels still require production Firebase/AdMob review. |
| Store metadata | partial | Draft exists. Screenshots/final privacy labels need final SDK settings and store review. |

## Required To Reach Pass

1. Add Firebase configs:
   - `prototypes/one-plus-one-minus-one/Assets/GoogleService-Info.plist`
   - `prototypes/one-plus-one-minus-one/Assets/google-services.json`
2. Add production AdMob app IDs and interstitial ad unit IDs.
   - Do not use `RELEASE_ENV.example` placeholder values; strict readiness
     rejects copied placeholder IDs.
3. Provide Android signing env vars and Android/iOS version env vars.
   - Use `prototypes/one-plus-one-minus-one/RELEASE_ENV.example` as the checklist.
4. Build fresh iOS and Android artifacts.
5. Complete the device QA tracker.
6. Rebuild the non-development WebGL store-capture target and recapture final
   store screenshots from that fresh artifact.
7. Rebuild QA WebGL, recapture key rounds/pages, and pass
   `./scripts/verify-one-plus-one-minus-one-qa-captures.sh`.
8. Re-run strict readiness checks and resolve all failures.

## Current No-Unity Verification

```sh
./scripts/verify-one-plus-one-minus-one-static.sh
```

Expected current result:

- Passes static round verification.
- Passes existing WebGL artifact smoke with a stale-build warning.
- Passes code readiness with Firebase/AdMob warnings.

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
  no alpha channel.
- Static suite now verifies the stick-character policy and compact expression
  layout plans for iPhone SE/standard/narrow portrait widths.
- Static suite now verifies release-safety for QA round-jump/sample-fill and
  Crashlytics test hooks, so they stay behind development/editor/store-capture
  guards.
- iOS readiness now requires the app-level `PrivacyInfo.xcprivacy` and verifies
  the app-local UserDefaults required-reason entry.
- Fails fresh WebGL build and smoke until the Unity Editor license is active.
- Fails WebGL artifact freshness because current source is newer than the
  release WebGL build.
- Passes generated icon freshness.
- WebGL viewport smoke passes outside the sandbox; inside a restricted sandbox
  it now exits with a clear local-port permission message instead of a Node
  stack trace.
- App Store candidate verification now rejects stale captures whose source or
  store-capture build is newer than the PNGs; latest failure correctly detects
  the icon/source updates made after the prior candidates.
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
