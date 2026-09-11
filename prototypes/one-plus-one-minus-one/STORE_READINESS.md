# 1 = 1 Store Readiness

This file tracks the remaining real-device and store-submission checks for the
`1 = 1` release candidate.

Current completion judgment is tracked in
`../../docs/one-equals-one-ship-ready-status.md`.

## Store Metadata Draft

- App name: `1 = 1`
- Internal concept title: `1+1-1*1/1=1`
- Detailed store copy draft: `../../docs/one-equals-one-store-metadata-draft.md`
- Subtitle candidates:
  - Tiny stick equation puzzles
  - Make math with little sticks
  - A small logic puzzle
- Short description:
  - Drag lively little sticks into place and make each equation work.
- Full description draft:
  - `1 = 1` is a small puzzle game about turning simple sticks into numbers and
    operators. Drag, rotate, and combine stick friends to make equations that
    actually work. Start with one stick, then discover plus, minus, divide,
    multiply, equals, 11, 111, and more across 100 compact rounds.
- Keyword candidates:
  - puzzle, math, logic, equation, numbers, sticks, brain, casual, minimal
- Age rating notes:
  - No violence, user-generated content, chat, gambling, or mature content.
  - Contains ads when production AdMob IDs are configured.
- Ads disclosure:
  - Interstitial ads may appear after newly cleared milestone rounds, roughly
    every 10 rounds. Replays and early tutorial rounds should not show ads.
- Data disclosure:
  - Firebase Analytics events: app open, round start, round clear, check failed,
    ad opportunity.
  - Crashlytics may collect crash diagnostics when enabled.
  - AdMob may collect advertising identifiers and ad interaction data according
    to Google Mobile Ads SDK behavior.

## Required Store Assets

- App icon: `Assets/_Project/Art/AppIcon-1024.png`
- WebGL favicon: `Builds/WebGL/one-plus-one-minus-one/app-icon.png`
- Icon verification:
  - `./scripts/verify-one-plus-one-minus-one-icon.sh`
  - Source icon and WebGL favicon must be 1024x1024 PNGs without alpha.
- iOS marketing icon after export:
  - `Builds/iOS/Xcode/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png`
- Screenshot checklist:
  - Round 1 first-play screen
  - Round 5 multiply discovery
  - Round 8 or 9 three-stick discovery
  - Round 30 medium expression
  - Round 75 equation puzzle
  - Round 100 finale expression
  - Round select page with locked/open/current states
- Candidate WebGL screenshot command:
  - `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh`
  - `./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh`
  - `./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh`
  - Output: `Builds/AppStoreScreenshots/Candidates`
  - Candidate verification fails if screenshots are older than the current
    store-capture build or if project source is newer than that build.
- Full visual QA refresh command after Unity license activation:
  - `./scripts/refresh-one-plus-one-minus-one-visual-qa.sh`

## Manual Device QA

These checks require actual devices, simulators, or browser automation with a
rendering engine.

- iPhone SE portrait:
  - Rounds 1-10
  - Round 16 collapsed tutorial area
  - Round select pages 1-9
  - Footer buttons and safe area
- Standard iPhone portrait:
  - Rounds 30, 50, 75, 90, 100
  - Long expression wrapping or scaling
  - `= target` visibility
- Large iPhone portrait:
  - Round select readability
  - Disabled button contrast
  - Character readability inside slots
- Android 20:9 portrait:
  - Drag/drop precision
  - Tap rotate
  - Slot max-3-sticks message
- WebGL desktop browser:
  - Page title and favicon
  - Round select overlay paging
  - Save/progress behavior
- WebGL mobile browser:
  - Canvas fills viewport
  - Touch drag visibility
  - Footer buttons do not overlap browser chrome

## Layout QA Focus

- Long expressions should wrap instead of compressing slots into a broken row.
- Regular portrait layouts should stay at or above the release minimum:
  - width: 104
  - height: 118
- Compact phone layouts may use the compact minimum:
  - width: 86 on very narrow phones, 96 on standard compact phones
  - height: 98 on very narrow phones, 108 on standard compact phones
- Long expressions should use no more than 3 regular rows or 4 compact rows.
- Slot aspect should stay stable, roughly 1.1-1.24 height/width.
- The fixed `= target` label should stay attached to the final expression row.
- Placed sticks should scale from the slot size, not only from the round slot
  count.
- Dragged sticks should render above the finger, with the pointer still used as
  the actual drop location.
- Tapping a placed stick should cycle its pose; dragging it outside should return
  it to the bank for outside rotation.
- A slot under the drag pointer should lightly highlight without obscuring the
  token label or character.
- SFX should be short and quiet:
  - button click
  - pick up
  - drop
  - rotate
  - fail
  - success
  - finale
- Repeated quick dragging should not stack painfully loud SFX.

Priority manual checks:

- Round 30: 9-slot fixed-target expression
- Round 50: mid-game expression spacing
- Round 75: equality row wrapping
- Round 90: late-game dense expression
- Round 100: final fixed-target expression

## Commercial-Quality Gate

Current code-side polish pass covers:

- Long-expression slot wrapping with minimum slot dimensions.
- Compact expression layout checks for iPhone SE, standard iPhone, and narrow
  portrait widths.
- Pointer-hover slot highlight during dragging.
- Finger-offset drag ghost so sticks remain visible while touched.
- Tap-to-rotate for placed sticks, plus drag-out return to the bank.
- Gentle generated SFX for button, pick up, drop, rotate, fail, success, and
  finale moments.
- Separate `*` personality from `x`/`×` with distinct face behavior.
- Character policy verification rejects arms, hands, legs, cheeks, and blush so
  the stick itself stays the character.
- Release-safety verification keeps QA round-jump/sample-fill and Crashlytics
  test hooks out of release-active code.
- iOS export includes `Assets/_Project/Store/PrivacyInfo.xcprivacy` as an app
  bundle resource for app-local PlayerPrefs/UserDefaults progress storage.
- Static 100-round verification can run without Unity through
  `scripts/verify-one-plus-one-minus-one-rounds.mjs`.
- Existing WebGL artifact smoke can run without rebuilding through
  `scripts/smoke-one-plus-one-minus-one-webgl-build.sh`.
- The WebGL smoke warns when the build artifact is older than project source
  files, so a passing smoke result may still require a fresh build.
- WebGL viewport smoke can capture iPhone SE, standard iPhone, large iPhone,
  Android 20:9, and desktop screenshots through
  `scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs`.
- A development-only WebGL QA build can open target rounds through `qaRound`
  query parameters, then capture Round 1, 30, 50, 75, 90, and 100 with
  `scripts/capture-one-plus-one-minus-one-webgl-qa-rounds.sh`.

Current external blockers before calling this commercially ready:

- `Assets/GoogleService-Info.plist` must be added for real iOS Firebase and
  Crashlytics verification.
- `Assets/google-services.json` must be added for real Android Firebase and
  Crashlytics verification.
- Production AdMob app/ad unit IDs must be provided through
  `RELEASE_ENV.example` env vars during store builds. Strict readiness rejects
  missing values, Google test IDs, and copied placeholder IDs.
- Android release signing env vars are required before Play Store AAB builds.
- iOS and Android fresh release exports still need to run after production
  config values are present.
- `RELEASE_ENV.example` lists the local/CI env vars required by strict release
  readiness.
- Real device/simulator QA is still required for touch feel, SFX loudness, safe
  area, round-select paging, and small-screen character readability.

## First-Run Progress QA

- Fresh install or cleared PlayerPrefs:
  - App opens directly to Round 1.
  - Round select does not auto-open.
- After clearing Round 1 and relaunching:
  - Round select opens first.
  - Round 1 is marked cleared.
  - Round 2 is selectable.
- Mid-progress relaunch:
  - Round select opens first.
  - Cleared rounds and next unlocked round are selectable.
  - Locked rounds cannot be clicked.
- Reset button:
  - Resets only the current round.
  - Does not clear overall progress.

## AdMob QA

- Normal build:
  - No ads during Rounds 1-5.
  - Interstitial opportunity only after newly cleared 10-round milestones.
  - Replayed rounds do not create ad opportunities.
  - Rounds cleared after many failed checks skip ads.
- AdMob test build:
  - Uses Google test app ID and test ad units.
  - Test interstitial can be requested on every clear.
  - Closing an ad continues to the next round normally.
- Release readiness:
  - Production AdMob App ID must not be Google's test app ID.
- Production interstitial ad unit IDs must not be blank.
- Production interstitial ad unit IDs must not be placeholders copied from
  `RELEASE_ENV.example`.

## Firebase / Crashlytics QA

- Keep `Assets/_Project/Store/PrivacyInfo.xcprivacy` in the project.
- Confirm exported iOS app bundle includes `PrivacyInfo.xcprivacy`.
- Confirm the manifest declares `NSPrivacyAccessedAPICategoryUserDefaults` with
  reason `CA92.1` for app-local saved progress.
- Add `Assets/GoogleService-Info.plist`.
- Confirm plist `BUNDLE_ID` is `com.mannlab.games.oneplusoneminusone`.
- Confirm events:
  - `app_open`
  - `round_start`
  - `round_clear`
  - `round_check_failed`
  - `ad_interstitial_opportunity`
  - `crashlytics_test_trigger`
- Confirm event parameters include round/progress context such as
  `highest_unlocked_round` and `is_replay` where relevant.
- Trigger a development crash by tapping the top-left corner 7 times, or by
  launching with `--mannlab-force-crashlytics-test`.
- Relaunch the app and verify the crash report arrives in Firebase.

## Automated Checks

Run from the repository root.

```sh
./scripts/verify-one-plus-one-minus-one-webgl.sh
./scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh
./scripts/verify-one-plus-one-minus-one-ios-readiness.sh admob-test
./scripts/verify-one-plus-one-minus-one-android-readiness.sh admob-test
./scripts/verify-one-plus-one-minus-one-icon.sh
./scripts/verify-one-plus-one-minus-one-character-policy.sh
./scripts/verify-one-plus-one-minus-one-release-safety.mjs
./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs
./scripts/verify-one-plus-one-minus-one-webgl-qa.sh
./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh
./scripts/capture-one-plus-one-minus-one-webgl-qa-rounds.sh
./scripts/capture-one-plus-one-minus-one-round-select-pages.sh
./scripts/verify-one-plus-one-minus-one-qa-captures.sh
./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh
./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh
./scripts/refresh-one-plus-one-minus-one-visual-qa.sh
```

Final commercial-completion gate:

```sh
./scripts/verify-one-plus-one-minus-one-ship-ready.sh
```

This gate should fail until all fresh builds, Firebase/AdMob production settings,
Android signing, icon freshness, and viewport smoke checks are satisfied.

Strict release readiness should fail until real Firebase and AdMob values are
present:

```sh
REQUIRE_FIREBASE_CONFIG=1 \
REQUIRE_PRODUCTION_ADMOB_IDS=1 \
REQUIRE_IOS_VERSION_ENV=1 \
./scripts/verify-one-plus-one-minus-one-ios-readiness.sh release
```

Android strict release readiness should fail until real AdMob and signing values
are present:

```sh
REQUIRE_FIREBASE_CONFIG=1 \
REQUIRE_PRODUCTION_ADMOB_IDS=1 \
REQUIRE_ANDROID_SIGNING_ENV=1 \
REQUIRE_ANDROID_VERSION_ENV=1 \
./scripts/verify-one-plus-one-minus-one-android-readiness.sh release
```
