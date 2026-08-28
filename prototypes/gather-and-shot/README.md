# Gather & Shot

Mobile portrait snowball survival prototype.

## Project

- Unity editor: 6000.3.23f1
- Primary prototype platform: mobile portrait
- Package name: com.mannlab.games.gatherandshot
- Namespace: MannLab.Games.GatherAndShot

## Core Loop

- Move with a virtual joystick.
- Collect snowballs, snowdrifts, and rare big snowdrifts to build ammo.
- Automatically throw snowballs at the nearest enemy in range.
- Each hit can defeat or damage enemies.
- Defeated enemies add score.
- Enemy contact drains Warmth and knocks the player back.
- The run ends when Warmth reaches zero.

## Pickups

- Snowball: +1 ammo.
- Snowdrift: +3 ammo.
- Big snowdrift: +5 ammo and a larger pickup radius. It is rare and tends to appear near enemies after the opening seconds.

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

## Deferred

Weapon levels, upgrade choices, ads, Firebase, Crashlytics, and store metadata are intentionally deferred until the simple score-chase loop proves worth continuing.
