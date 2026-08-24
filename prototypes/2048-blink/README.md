# 2048 Blink

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.22f1
- Platforms: WebGL, iOS, Android
- Package name: com.mannlab.games.game2048blink
- Namespace: MannLab.Games.Game2048Blink

## First Open

Open this directory from Unity Hub.

Main scene:

- `Assets/_Project/Scenes/Game.unity`

The scene contains a `Blink2048Controller` object that builds the MVP UI at runtime.

## MVP Status

Implemented:

- 4 x 4 2048-style slide controls
- Standard 2048 merge and game-over rules
- Predictable Gray Cross pattern after every valid move
- Gray masked tiles that hide numbers while preserving occupancy
- Keyboard arrow and swipe input
- Local best tile and best score
- Result panel
- iOS app icon asset
- iOS Xcode build script
- Firebase/Crashlytics telemetry bridge with Unity-log fallback
- Firebase Unity SDK 13.14.0 Analytics/Crashlytics package reference
- Development-only Crashlytics forced crash trigger
- Tile slide and merge animation
- Continuity-first Gray Cross transition after every valid move
- Hidden-tile motion that preserves covered values during movement

Not implemented:

- Ads
- Online leaderboard
- Final store naming/legal clearance

## Verification Notes

Compile and board-rule verification can be run without opening the Unity Editor:

```sh
./scripts/verify-2048-blink-mvp.sh
```

Generate the App Store icon:

```sh
node scripts/generate-2048-blink-app-icon.mjs
```

Unity import verification can be run after Unity Hub sign-in and license activation:

```sh
"/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "/Users/gimjaeman/Desktop/coding/mannlab/games/prototypes/2048-blink" \
  -executeMethod MannLab.Games.Game2048Blink.EditorTools.CreateGameScene.Create \
  -logFile /tmp/2048-blink-unity-import.log
```

iOS release Xcode project verification:

```sh
./scripts/verify-2048-blink-ios-readiness.sh
```

iOS Crashlytics test Xcode project verification:

```sh
./scripts/verify-2048-blink-ios-readiness.sh crashlytics-test
```

Firebase code/config readiness:

```sh
./scripts/verify-2048-blink-firebase-readiness.sh
```

## Firebase Notes

The runtime calls `FirebaseTelemetry` for `app_open`, `run_start`, `restart`, `run_end`, and `crashlytics_test_trigger` breadcrumbs. It also forwards unhandled exceptions and Unity exception logs to Crashlytics when the Firebase Unity SDK is present.

The iOS Firebase app config must be placed at `Assets/GoogleService-Info.plist` for bundle ID `com.mannlab.games.game2048blink`.

The Crashlytics test trigger is compiled only for Unity Editor or development builds. In the `crashlytics-test` iOS build, tap the top-left corner 7 times within 2.5 seconds to request a forced Crashlytics test crash. Reopen the app after the crash so Crashlytics can upload the report.

## AdMob Notes

AdMob iOS App ID:

```text
ca-app-pub-4525914685149405~6400718358
```

Initial ad policy:

- Interstitial only.
- Show after game over.
- Do not show every run; start around once every three game overs.
- Use the Google iOS interstitial test ad unit during development:

```text
ca-app-pub-3940256099942544/4411468910
```

Replace the test ad unit with the production `2048 Blink iOS Game Over Interstitial` ad unit ID before release verification.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
