# Gather & Shot

Mobile portrait snowball survival prototype built around a stop-to-reload risk loop.

## Project

- Unity editor: 6000.3.23f1
- Primary prototype platform: mobile portrait
- Package name: com.mannlab.games.gatherandshot
- Namespace: MannLab.Games.GatherAndShot

## Core Loop

- Move with a virtual joystick.
- Release touch and stand still to gather snow into ammo.
- Gathering snow stops movement and auto-fire until the cycle completes or movement resumes.
- Rare snowballs, snowdrifts, and big snowdrifts act as emergency bonus refills.
- Automatically throw snowballs at the nearest enemy in range.
- Each hit can defeat or damage enemies.
- Defeated enemies add score.
- Enemy contact drains Warmth and knocks the player back.
- The run ends when Warmth reaches zero.

## Gathering And Bonus Refills

- Stationary gather: +1 ammo per completed stillness cycle.
- Touching again cancels gathering immediately.
- Snowball bonus: +2 ammo.
- Snowdrift bonus: +4 ammo.
- Big snowdrift bonus: +6 ammo. It is rare and tends to appear near enemy pressure after the opening seconds.

## Build

Generate doodle assets:

```sh
python3 scripts/generate-gather-and-shot-doodle-assets.py
```

Generate the scene from Unity:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/gather-and-shot \
  -executeMethod MannLab.Games.GatherAndShot.EditorTools.CreateGameScene.Create
```

Run the lightweight local verification:

```sh
./scripts/verify-gather-and-shot-mvp.sh
```

Firebase/Crashlytics readiness:

```sh
./scripts/verify-gather-and-shot-firebase-readiness.sh
```

AdMob readiness:

```sh
./scripts/verify-gather-and-shot-admob-readiness.sh
```

iOS Xcode export readiness:

```sh
./scripts/generate-gather-and-shot-doodle-assets.py
./scripts/verify-gather-and-shot-ios-readiness.sh
./scripts/verify-gather-and-shot-ios-readiness.sh crashlytics-test
./scripts/verify-gather-and-shot-ios-readiness.sh admob-test
```

Capture App Store screenshots from the WebGL build:

```sh
node scripts/capture-gather-and-shot-webgl-app-store-assets.mjs
```

## Firebase Notes

The runtime calls `FirebaseTelemetry` for `app_open`, `run_start`, `restart`, `gather_start`, `bonus_pickup`, `run_end`, and `crashlytics_test_trigger` breadcrumbs. It also forwards unhandled exceptions and Unity exception logs to Crashlytics when the Firebase Unity SDK is present.

Firebase app config must be added per platform before real Crashlytics testing:

- iOS: `Assets/GoogleService-Info.plist`
- Android: `Assets/google-services.json`

The Crashlytics test trigger is compiled only for Unity Editor or development builds. Tap the top-left corner 7 times within 2.5 seconds, or launch with `--mannlab-force-crashlytics-test` / `MANNLAB_FORCE_CRASHLYTICS_TEST=1`, then reopen the app so Crashlytics can upload the report.

## AdMob Notes

AdMob uses the shared game-over interstitial bridge. Debug/development builds use Google's test interstitial IDs through the bridge, and `MANNLAB_ADMOB_FORCE_TEST_ADS` forces every game over to request a test interstitial.

The Google Mobile Ads settings asset uses the production iOS app ID and Google's sample Android app ID until Android release setup is ready. Debug/development builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` still force Google's test interstitial IDs.

- Android AdMob App ID: `ca-app-pub-3940256099942544~3347511713`
- iOS AdMob App ID: `ca-app-pub-4525914685149405~6036634116`
- Production Android interstitial: set `ProductionAndroidInterstitialAdUnitId` in `GatherAndShotController`
- Production iOS interstitial: `ca-app-pub-4525914685149405/2541126713`
- iOS release export App ID override: set `MANNLAB_GATHER_AND_SHOT_ADMOB_IOS_APP_ID`

## iOS Notes

The generated app icon is `Assets/_Project/Art/AppStore/AppIcon-1024.png`. The iOS export script copies it into the Xcode AppIcon asset catalog as the marketing icon and uses it for Unity's iOS application icons.

Default iOS versioning is `0.1 (1)`. Override with `MANNLAB_GATHER_AND_SHOT_IOS_MARKETING_VERSION` and `MANNLAB_GATHER_AND_SHOT_IOS_BUILD_NUMBER` before exporting a store build. The default App Store provisioning profile specifier is `Gather And Shot`; override with `MANNLAB_GATHER_AND_SHOT_IOS_APP_STORE_PROFILE_SPECIFIER` if Apple Developer uses a different profile name. AdMob/CocoaPods exports should be archived from `Builds/iOS/Xcode/Unity-iPhone.xcworkspace`.

## Deferred

Weapon levels, upgrade choices, and store metadata are intentionally deferred until the simple score-chase loop proves worth continuing.
