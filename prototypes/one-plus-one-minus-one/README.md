# 1 = 1

Unity 2D mobile puzzle release candidate for the `1 + 1 - 1 * 1 / 1 = 1` Goal Mode concept.

Players place living stick tokens into every slot to complete either a true expression against a target value or a direct equality.

## Project

- Unity editor: 6000.3.23f1
- Platform: iOS/Android/WebGL, portrait
- Package name: com.mannlab.games.oneplusoneminusone
- Namespace: MannLab.Games.OnePlusOneMinusOne

## MVP Scope

- Goal Mode only.
- Release round set: 100 validated rounds.
- Release tokens: `1`, `11`, `111`, `+`, `-`, `×`, `x`, `*`, `/`, `=`.
- Token costs are validated from the sample round data.
- Adjacent number tokens concatenate, so `[1][1]` and `[11]` both read as `11`.
- All slots and all sticks must be used exactly.
- The expression parser supports integers, `+`, `-`, `*`, `/`, and normal precedence.
- Parentheses, powers, and handmade `2`/`3` are reserved for later.

## Release Feel Targets

- Sticks should feel like small living characters, but every token must remain
  readable first.
- `1` is the core mascot: chatty, lively, and simple.
- `x`/`×` and `*` should not share the same personality; `*` is a more energetic
  three-stick variant.
- Long expressions wrap within the equation area instead of shrinking slots
  below release minimum size; compact phone widths may use a fourth row to keep
  slot proportions stable.
- Dragged sticks render above the finger while the pointer still determines the
  actual drop location.
- Feedback SFX should be short, quiet, and throttled so fast dragging does not
  become noisy.
- The Rounds header has a persistent Sound checkbox. It mutes only the game's
  effects, independently of round progress; Reset does not change that preference.

## Firebase / Crashlytics

The runtime calls `FirebaseTelemetry` for `app_open`, `round_start`, `round_reset`, `round_clear`, `round_check_failed`, and `crashlytics_test_trigger`. Events include game, round, progress, replay, app version, or platform context where relevant. The bridge logs to Unity even before Firebase config is present, and forwards to Firebase Analytics/Crashlytics when the SDK and app config are available.

Ensure this game's Firebase config files are present before real Crashlytics
testing:

- iOS: `Assets/GoogleService-Info.plist` for `com.mannlab.games.oneplusoneminusone`
- Android: `Assets/google-services.json` for `com.mannlab.games.oneplusoneminusone`

Crashlytics test triggers are development-only. Tap the top-left corner 7 times within 2.5 seconds, or launch with `--mannlab-force-crashlytics-test` / `MANNLAB_FORCE_CRASHLYTICS_TEST=1`, then reopen the app so Crashlytics can upload the report.

## AdMob

AdMob uses the shared game-over interstitial bridge. Development builds and AdMob test builds use Google's test ad units. Production ad unit IDs are loaded from generated release config at build time.

- Current iOS AdMob App ID default: `ca-app-pub-3940256099942544~1458002511`
- Current Android AdMob App ID default: `ca-app-pub-3940256099942544~3347511713`
- Override iOS app ID for export with `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID`.
- Override Android app ID for export with `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID`.
- Release cadence: only offer interstitials after newly cleared milestone rounds, about once every 10 rounds, with the first 5 rounds protected and hard clears skipped.
- AdMob test builds define `MANNLAB_ADMOB_FORCE_TEST_ADS`, so every clear can request a test interstitial for verification.

## Release Checklist

- Keep the public app name as `1 = 1`; use the full equation as the internal finale concept.
- Run `./scripts/check-one-plus-one-minus-one-unity-license.sh` before Unity-gated PlayMode/WebGL/iOS/Android readiness. If it reports `LICENSING_CLIENT_UNAVAILABLE`, repair Unity Hub licensing before rerunning Unity batch commands.
- Ensure `Assets/GoogleService-Info.plist` exists for `com.mannlab.games.oneplusoneminusone` before real Crashlytics verification.
- Add `Assets/google-services.json` for `com.mannlab.games.oneplusoneminusone` before Android Firebase verification.
- Keep `Assets/_Project/Store/PrivacyInfo.xcprivacy` in the iOS export; it declares app-local UserDefaults access for saved progress.
- Override the production iOS AdMob App ID with `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID` before export.
- Override the production Android AdMob App ID with `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID` before export.
- Inject production interstitial ad units at build time with `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID` and `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID`.
- Set `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION` and `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER` for store builds.
- Set `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION` and `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE` for Android store builds.
- Set Android signing env vars before release AAB builds:
  - `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH`
  - `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS`
  - `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME`
  - `MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS`
- Use `RELEASE_ENV.example` as the local/CI secret checklist; do not commit a filled copy.
- Run `VerifyGoalMode.Run`, `./scripts/verify-one-plus-one-minus-one-webgl.sh`, `./scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh`, and `./scripts/verify-one-plus-one-minus-one-ios-readiness.sh admob-test` before release.
- Run `./scripts/verify-one-plus-one-minus-one-android-readiness.sh admob-test` before Android ad QA.
- Run `./scripts/verify-one-plus-one-minus-one-static.sh` for the no-Unity static suite.
- Run `./scripts/verify-one-plus-one-minus-one-playmode.sh` for real controller
  regressions covering rejected drops, pointer ownership/cancellation, progress
  restore, final-round replay ads, UI lifetime, sound preference, equality
  rotation/display, and resize bounds.
- Run `./scripts/verify-one-plus-one-minus-one-release-env.sh` for a fast
  no-Unity preflight of Firebase configs, production AdMob IDs, release version
  envs, and Android signing envs. Add `REQUIRE_ONE_EQUALS_ONE_RELEASE_ENV=1`
  or pass `--strict` when you want missing or malformed external settings to
  fail immediately.
- Run `./scripts/verify-one-plus-one-minus-one-ios-readiness.sh release --preflight-only`
  and `./scripts/verify-one-plus-one-minus-one-android-readiness.sh release --preflight-only`
  for platform source/env checks that do not launch Unity. Full platform
  readiness still requires Unity licensing and an export/build.
- Run `./scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh` for a fast
  no-Unity preflight of the manual device QA tracker. Pass `--strict` before
  store submission so unfinished touch/audio/privacy rows fail the gate.
- Run `./scripts/verify-one-plus-one-minus-one-icon.sh` to verify the source icon and WebGL favicon are 1024x1024 PNGs without alpha channels and keep enough contrast at small launcher sizes.
- Run `./scripts/verify-one-plus-one-minus-one-character-policy.sh` to reject arms, hands, legs, cheeks, blush, or missing symbol face-style hooks.
- Run `./scripts/verify-one-plus-one-minus-one-release-safety.mjs` to ensure
  QA URL overrides and Crashlytics test hooks stay out of release-active code,
  and that iOS/Android release build entrypoints stay separated from explicit
  AdMob-test build paths.
- Run `./scripts/verify-one-plus-one-minus-one-store-metadata.sh` to keep store copy, privacy URL, screenshot plan, ads/privacy disclosure, and final upload checklist intact.
- Run `./scripts/verify-one-plus-one-minus-one-png-visuals.mjs --all` to inspect
  QA and App Store capture PNGs for dimensions, alpha policy, blank/dark
  content, and mobile content-start position.
- Run `./scripts/verify-one-plus-one-minus-one-rounds.mjs` when Unity is unavailable; it statically checks the 100 legacy goal rounds, 30 default fixed-target rounds, tutorial scope, title hint policy, and narrow portrait layout plan.
- Run `./scripts/report-one-plus-one-minus-one-round-quality.mjs --strict` after
  legacy goal round edits to review token-band coverage, target spread, and
  repeated pattern warnings.
- Run `./scripts/report-one-plus-one-minus-one-round-quality.mjs --make-one --patterns --summary --strict-patterns --max-nodes 200000 --max-solutions 1000`
  after default `= 1` round edits to catch late standalone shortcut patterns
  and to keep bounded, incomplete pattern rows visible. The summary also
  includes `resourceReview`, which lists repeated slot/stick budgets with
  found shared-equality witnesses so same-resource fatigue stays visible
  without banning valid alternate answers.
- Run `./scripts/report-one-plus-one-minus-one-round-quality.mjs --make-one --evaluate-sample '1 - 1 + 1 / 1' --target 1`
  before editing a hand-designed default-mode round to inspect the candidate's
  slot count, stick cost, sample patterns and dominant shortcut profile.
- Run `./scripts/report-one-plus-one-minus-one-round-quality.mjs --make-one --resource-candidates 17 --max-evaluations 8 --max-results 4 --candidate-budget 40 --max-nearby-nodes 500000`
  when a default-mode round shares a slot/stick budget with several other
  rounds. The argument accepts either round numbers such as `17` or repeated
  resource keys such as `5/8` for all rounds with that slot/stick budget. The
  bounded report proposes target-preserving resource moves,
  including a small set of composite target-1 cancellation candidates such as
  `A - A + 1`, `A / B * B - A + 1` and paired multiplication cancellation, and
  flags candidates that would reintroduce late pure `N/N`, shift into another
  dominant shortcut family, visibly teach an authored shortcut sample after the
  learning window, or require more exhaustive evidence. Check
  `candidateSummary` first: `recommendableCount: 0` means the current bounded
  pass found only analysis-only candidates. The `search` block separately
  reports `compositeCandidateCount` and `enumeratedCandidateCount`, so a capped
  probe shows whether it considered both hand-shaped target-1 patterns and
  nearby resource enumeration. Evaluated candidates also include `source`, and
  `candidateSummary.sourceCounts` groups those evaluated candidates by source,
  while `candidateSummary.bestBySource` keeps the strongest evaluated example
  from each source visible even when `maxResults` is small. Occupied resources
  are surfaced as `occupied-resource-swap` analysis-only candidates so manual
  redesign can see promising swaps without treating them as direct replacements.
  When a capped evaluation would otherwise be filled by one source, the report
  uses `search.sourceDiverseEvaluation` to show that it kept at least one
  candidate from each generated source in view. Prefer one round at a time, or set
  `--max-nearby-nodes` explicitly when probing a whole repeated-resource group.
- Round 26 currently uses `111 - 11 × 11 + 11` to surface the multiplication-offset
  idea instead of presenting a visible `N/N * N/N` route, while preserving the
  same resource budget and free alternate-answer validation.
- Round 25 and Round 30 use same-budget authored paths,
  `1 / 1 1 1 × 111` and `1 / 111 111 × 111 111`. The stricter pattern map
  still treats both as reciprocal-cancellation review rows, so they remain
  visible follow-up design material rather than closed structural fixes.
- Round 29 uses `1 1 + 111 - 11 × 11` to move out of the old `6/8` repeated
  resource group while reusing the `11 × 11 - 111` offset idea in reverse.
- Pattern reports expose `shortcutPolicy.authoredSampleReview` so late-round
  samples that visibly teach self-division, divide-by-one, multiply-by-one,
  self-subtraction, simple or compound additive cancellation, reciprocal
  cancellation, or same-expression equality can be reviewed separately from the
  full accepted-answer set.
- Resource candidate reports flag `highEqualityEcho` when a target-preserving
  replacement is dominated by same-expression equality evidence. This keeps
  replacements that merely swap a visible `N/N` shortcut for another universal
  equality shortcut out of the recommendable set.
- Run `./scripts/smoke-one-plus-one-minus-one-webgl-build.sh` to verify an existing WebGL artifact without rebuilding it.
- Run `./scripts/verify-one-plus-one-minus-one-webgl-shells.sh` to verify existing release, QA, and store-capture WebGL shells share the correct app title, icon, mobile metadata, cache-busted build URLs, and development-marker policy.
- Run `./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs` after a fresh WebGL build to capture iPhone SE, standard iPhone, large iPhone, Android 20:9, and desktop viewport smoke screenshots.
- For an isolated fresh-player touch-input run (no sample fill), run:

  ```sh
  ONE_EQUALS_ONE_INPUT_PLAYTEST=1 \
  ONE_EQUALS_ONE_WEBGL_BUILD_DIR=prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-qa \
  ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR=/tmp/one-equals-one-input-smoke \
    node scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs
  ```

  This run uses the development-only `qaInputProbe` layout dump to derive
  rendered slot, bank, and Check coordinates, then clears the current default
  `= 1` Rounds 1-10 with emulated touch input. It requires a fresh QA build for
  telemetry assertions and does not replace physical-device touch QA.
- Run `./scripts/verify-one-plus-one-minus-one-webgl-qa.sh`, then `./scripts/capture-one-plus-one-minus-one-webgl-qa-rounds.sh`, to capture development-only QA round screenshots for Rounds 1, 5, 8, 9, 16, 30, 50, 75, 90, and 100.
- Run `./scripts/capture-one-plus-one-minus-one-round-select-pages.sh` to capture round-select pages 1, 5, and 9, or pass page numbers explicitly.
- Run `./scripts/verify-one-plus-one-minus-one-qa-captures.sh` after QA captures to verify required key-round/page screenshots, viewport dimensions, and QA-build freshness.
- Run `./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh`, then `./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh`, to generate watermark-free App Store candidate screenshots from a non-development WebGL capture build.
- Run `./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh` after capture; it checks required sizes, alpha, store-capture freshness, and rejects stale screenshots.
- Run `./scripts/refresh-one-plus-one-minus-one-visual-qa.sh` after Unity license activation when both QA captures and App Store candidates need to be regenerated.
- Run `./scripts/verify-one-plus-one-minus-one-ship-ready.sh` as the final commercial-completion gate; it should fail until fresh builds, WebGL shell metadata, QA captures, App Store candidates, strict release-env/platform preflights, real Firebase/AdMob values, signing, icon checks, and viewport smoke all pass.
- Use `STORE_READINESS.md` for metadata, device QA, screenshot, Firebase, and AdMob checks.
- Use `../../docs/one-equals-one-commercial-polish-backlog.md` for the continuous commercial-quality polish loop.
- Use `../../docs/one-equals-one-device-qa-tracker.md` for fresh-build device and viewport sign-off.
- Use `../../docs/one-equals-one-store-metadata-draft.md` for store copy, screenshot planning, privacy disclosure, and age rating notes.
- Use `../../docs/one-equals-one-ship-ready-status.md` for the current pass/not-yet completion judgment.
- Do a device pass on Round 1-10, 16, 30, 50, 75, 90, 100, and all 9 round-select pages.

iOS build methods:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildIosXcode.BuildCrashlyticsTest

/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildIosXcode.BuildAdMobTest
```

Android build methods:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildAndroidAab.BuildAdMobTestApk

/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildAndroidAab.BuildAab
```

WebGL QA build method:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildWebGL.BuildDevelopmentQa
```

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

To generate the playable scene from the command line:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/one-plus-one-minus-one \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.CreateGameScene.Create
```

Then open `Assets/_Project/Scenes/Game.unity`.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
