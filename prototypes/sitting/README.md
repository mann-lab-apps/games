# Sitting

One-touch stealth-rest hyper-casual prototype.

## Project

- Unity editor: 6000.3.20f1
- Primary prototype platform: mobile portrait
- Package name: com.mannlab.games.sitting
- Namespace: MannLab.Games.Sitting

## Core Loop

- Stand behind a desk chair while stamina drains.
- Hold the screen to sit and recover stamina.
- Stand before a visitor passes through the front of the scene.
- Sitting while a visitor passes ends the run as `Caught`.
- Letting stamina hit zero ends the run as `Exhausted`.
- Survive as long as possible.

## Build

Generate the scene from Unity:

```sh
/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/sitting \
  -executeMethod MannLab.Games.Sitting.EditorTools.CreateGameScene.Create
```

Run the lightweight local verification:

```sh
./scripts/verify-sitting-mvp.sh
```

## Deferred

Ads, Firebase, Crashlytics, app icons, screenshots, and store metadata are intentionally deferred until the game loop proves worth continuing.
