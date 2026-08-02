# Dopamine Swap

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.20f1
- Platform: Android
- Package name: com.mannlab.games.dopamineswap
- Namespace: MannLab.Games.DopamineSwap

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

The first playable MVP starts directly in portrait play:

- Pick one of three 1-100 cards before the timer expires.
- Win when the selected card is higher than the computer score.
- Rounds 1-3 reveal the exact computer score.
- Round 4 onward reveals a score range, then shows the exact score after selection.
- Winning adds the selected card to Score; losing records the best local Score.

## Verification

```sh
./scripts/verify-dopamine-swap-mvp.sh
./scripts/verify-dopamine-swap-unity.sh
./scripts/verify-dopamine-swap-webgl.sh
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
