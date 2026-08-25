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
- Shared AdMob interstitial bridge
- Google Mobile Ads Unity SDK dependency through the shared AdMob package
- Google Mobile Ads iOS App ID project settings
- Game-over interstitial hook with a once-every-three-game-overs default
- Tile slide and merge animation
- Continuity-first Gray Cross transition after every valid move
- Hidden-tile motion that preserves covered values during movement

Not implemented:

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

For TestFlight/App Store archive, open the generated CocoaPods workspace, not the
standalone Xcode project:

```sh
open prototypes/2048-blink/Builds/iOS/Xcode/Unity-iPhone.xcworkspace
```

`Unity-iPhone.xcodeproj` does not load the Pods project by itself, so AdMob builds
can fail with `Framework 'GoogleMobileAds' not found` if that file is opened
directly.

iOS Crashlytics test Xcode project verification:

```sh
./scripts/verify-2048-blink-ios-readiness.sh crashlytics-test
```

iOS AdMob test Xcode project verification:

```sh
./scripts/verify-2048-blink-ios-readiness.sh admob-test
```

The AdMob test build is a release-style device build that forces Google's iOS
interstitial test ad unit and shows after every game over. Use it only to verify
ad display in TestFlight; do not submit that build to App Review.

The generated iOS build number defaults to `11`. Override it when uploading
multiple TestFlight builds:

```sh
MANNLAB_2048_BLINK_IOS_BUILD_NUMBER=12 ./scripts/verify-2048-blink-ios-readiness.sh release
```

When Xcode Organizer keeps selecting an older archive, create and verify the
archive directly from the workspace:

```sh
xcodebuild archive \
  -workspace /Users/gimjaeman/Desktop/coding/mannlab/games/prototypes/2048-blink/Builds/iOS/Xcode/Unity-iPhone.xcworkspace \
  -scheme Unity-iPhone \
  -configuration Release \
  -destination generic/platform=iOS \
  -archivePath /Users/gimjaeman/Library/Developer/Xcode/Archives/2026-08-25/2048Blink-Release-11-direct.xcarchive
```

Verify the archive plist before uploading:

```sh
/usr/libexec/PlistBuddy \
  -c 'Print :CFBundleShortVersionString' \
  -c 'Print :CFBundleVersion' \
  -c 'Print :GADApplicationIdentifier' \
  /Users/gimjaeman/Library/Developer/Xcode/Archives/2026-08-25/2048Blink-Release-11-direct.xcarchive/Products/Applications/2048Blink.app/Info.plist
```

The release archive should report `0.1`, build `11`, and AdMob app ID
`ca-app-pub-4525914685149405~6400718358`. The release metadata should include
the production interstitial unit only:

```sh
LC_ALL=C grep -a -o -E \
  'Ad test build|Test Ad|ads: loaded|ads: load failed|ca-app-pub-4525914685149405/8208624041' \
  /Users/gimjaeman/Library/Developer/Xcode/Archives/2026-08-25/2048Blink-Release-11-direct.xcarchive/Products/Applications/2048Blink.app/Data/Managed/Metadata/global-metadata.dat \
  | sort -u
```

Expected release output:

```text
ca-app-pub-4525914685149405/8208624041
```

Firebase code/config readiness:

```sh
./scripts/verify-2048-blink-firebase-readiness.sh
```

AdMob code/config readiness:

```sh
./scripts/verify-2048-blink-admob-readiness.sh
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
- Development and debug builds automatically use the Google iOS interstitial test ad unit:
- The `admob-test` iOS build also forces the Google test ad unit and lowers the game-over interval to 1 for verification.

```text
ca-app-pub-3940256099942544/4411468910
```

Release builds use the production `2048 Blink iOS Game Over Interstitial` ad unit ID.

Production iOS interstitial ad unit:

```text
ca-app-pub-4525914685149405/8208624041
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
