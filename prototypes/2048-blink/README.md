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
- Tile slide and merge animation
- Continuity-first Gray Cross transition after every valid move
- Hidden-tile motion that preserves covered values during movement

Not implemented:

- Firebase/Crashlytics telemetry
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

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
