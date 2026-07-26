# 10000

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.20f1
- Platform: Android
- Package name: com.mannlab.games.game10000
- Namespace: MannLab.Games.Game10000

## First Open

Open this directory from Unity Hub.

Main scene:

- `Assets/_Project/Scenes/Game.unity`

The scene contains a `Game10000Controller` object that builds the MVP UI at runtime.

## MVP Status

Implemented:

- 10 x 10 board
- Guaranteed horizontal or vertical `10000`
- Tap any target cell to clear
- Stage progression
- Stage time limits
- Wrong tap time penalty
- Result panel
- Local best score
- Sketch-style board outlines and marker feedback

Not implemented:

- Ads
- Analytics SDKs
- Online leaderboard
- Skins
- Reversed target directions
- Daily challenge

## Verification Notes

Unity batchmode import was attempted, but this machine currently has no activated Unity Editor license.

After activating Unity through Unity Hub, run:

```sh
"/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -quit \
  -projectPath "/Users/gimjaeman/Desktop/coding/mannlab/games/prototypes/10000" \
  -executeMethod MannLab.Games.Game10000.EditorTools.CreateGameScene.Create \
  -logFile /tmp/10000-unity-import.log
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
