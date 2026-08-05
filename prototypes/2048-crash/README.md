# 2048 Crash

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.20f1
- Platform: Android
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
- Same-value crash rule
- Connected stages on a single continuing board
- Sliding tile motion
- Special block crash and spawn motion
- Special block stage progression by powers of two
- Keyboard arrow and swipe input
- Local best stage
- Result panel

Not implemented:

- Firebase Analytics / Crashlytics SDKs
- Ads
- Online leaderboard
- Store-ready naming/legal clearance

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

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
