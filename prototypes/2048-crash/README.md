# 2048 Crash

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.20f1
- Platforms: WebGL, iOS, Android
- Package name: com.mannlab.games.game2048crash
- Namespace: MannLab.Games.Game2048Crash

## First Open

Open this directory from Unity Hub.

Main scene:

- `Assets/_Project/Scenes/Game.unity`

The scene contains a `Crash2048Controller` object that builds the MVP UI at runtime.

## MVP Status

Implemented:

- 4 x 4 2048-style slide controls
- Static special block
- Same-value crash rule that destroys both blocks
- Distinct special block paper hatch style from the shared design system
- Connected stages on a single continuing board
- Sliding tile motion
- Special block crash and spawn motion
- Special block stage progression by powers of two
- Keyboard arrow and swipe input
- Local best stage
- Firebase/Crashlytics telemetry bridge with Unity-log fallback
- Firebase Unity SDK 13.14.0 Analytics/Crashlytics packages
- Android app icon asset
- Release-signed Android AAB/APK build script
- Firebase iOS config file for `com.mannlab.games.game2048crash`
- iOS Xcode build script with release and Crashlytics test modes
- Development-only Crashlytics forced crash trigger
- App Store listing/privacy prep draft
- App Store screenshot generator and readiness verifier
- Result panel

Not implemented:

- Ads
- Online leaderboard
- Final store naming/legal clearance

## Verification Notes

Compile and board-rule verification can be run without opening the Unity Editor:

```sh
./scripts/verify-2048-crash-mvp.sh
```

Unity import verification can be run after Unity Hub sign-in and license activation:

```sh
"/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "/Users/gimjaeman/Desktop/coding/mannlab/games/prototypes/2048-crash" \
  -executeMethod MannLab.Games.Game2048Crash.EditorTools.CreateGameScene.Create \
  -logFile /tmp/2048-crash-unity-import.log
```

Android release signing is configured through local environment variables. The local keystore and env file live under `Signing/` and are ignored by git.

Expected local files:

- `Signing/2048-crash-upload.keystore`
- `Signing/local-signing.env`

Required variables:

- `MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PATH`
- `MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PASS`
- `MANNLAB_2048_CRASH_ANDROID_KEYALIAS_NAME`
- `MANNLAB_2048_CRASH_ANDROID_KEYALIAS_PASS`

Build a Play Console upload AAB:

```sh
./scripts/verify-2048-crash-android.sh
```

Build a release-signed APK for device smoke testing:

```sh
./scripts/verify-2048-crash-android.sh apk
```

iOS release Xcode project verification:

```sh
./scripts/verify-2048-crash-ios-readiness.sh
```

iOS Crashlytics test Xcode project verification:

```sh
./scripts/verify-2048-crash-ios-readiness.sh crashlytics-test
```

Generate App Store screenshots:

```sh
node ../../scripts/capture-2048-crash-webgl-app-store-assets.mjs
```

Verify App Store listing assets and privacy prep:

```sh
../../scripts/verify-2048-crash-app-store-readiness.sh
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.

## Firebase Notes

The runtime calls `FirebaseTelemetry` for `app_open`, `run_start`, `special_crash`, `run_end`, and `restart` breadcrumbs. It also forwards unhandled exceptions and Unity exception logs to Crashlytics when the Firebase Unity SDK is present.

The iOS Firebase app config is checked in at `Assets/GoogleService-Info.plist` for bundle ID `com.mannlab.games.game2048crash`.

Firebase Unity SDK 13.14.0 packages are imported for Analytics and Crashlytics. The next verification step is an iOS device build that triggers a test crash and confirms it appears in Firebase Console.

The Crashlytics test trigger is compiled only for Unity Editor or development builds. In the `crashlytics-test` iOS build, tap the top-left corner 7 times within 2.5 seconds to request a forced Crashlytics test crash. Reopen the app after the crash so Crashlytics can upload the report.
