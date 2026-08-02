# Drum Duel

Unity rhythm memory prototype.

## Project

- Unity editor: 6000.3.20f1
- Platform: Android
- Package name: com.mannlab.games.drumduel
- Namespace: MannLab.Games.DrumDuel

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

## Local Verification

```sh
../../scripts/verify-drum-duel-mvp.sh
../../scripts/verify-drum-duel-unity.sh
../../scripts/verify-drum-duel-webgl.sh
```

## Concept

`Drum Duel` is a provisional title for a rhythm echo game. The computer plays a short 4-tick rhythm, then the player gets 4 ticks to reproduce the timing with a single hi-hat style input.

See `docs/drum-duel-game-design.md`.

## Prototype Kickoff

- Core promise: Hear a tiny rhythm, answer it on the beat, push one stage further.
- One-minute loop: Computer plays 4 ticks, player answers 4 ticks, score advances or the run ends.
- Input: One tap button or screen tap, rendered as hi-hat hits.
- Fail state: Missed beat, extra hit, or timing outside the accepted window.
- Score or progression: Best score is highest cleared stage.
- First playable target: 8-12 stages with increasing BPM and simple rhythm patterns.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
