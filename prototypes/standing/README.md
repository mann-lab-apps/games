# Standing

One-touch stealth-rest hyper-casual prototype.

## Project

- Unity editor: 6000.3.22f1
- Primary prototype platform: mobile portrait
- Package name: com.mannlab.games.standing
- Namespace: MannLab.Games.Standing

## Core Loop

- Stand behind a desk chair while stamina drains.
- Hold the screen to sit and recover stamina.
- Read the walking passer and stand before a customer reaches the front of the scene.
- Sitting while a visitor passes ends the run as `Caught`.
- Letting stamina hit zero ends the run as `Exhausted`.
- Survive as long as possible.

## Build

Generate the scene from Unity:

```sh
/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/standing \
  -executeMethod MannLab.Games.Standing.EditorTools.CreateGameScene.Create
```

Run the lightweight local verification:

```sh
./scripts/verify-standing-mvp.sh
```

Build the iOS Xcode project:

```sh
./scripts/verify-standing-ios-readiness.sh
```

## Deferred

Ads, Firebase, Crashlytics, screenshots, and store metadata are intentionally deferred until the game loop proves worth continuing.
