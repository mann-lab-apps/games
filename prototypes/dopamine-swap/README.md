# Dopamine Swap

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.20f1
- Platform: Android
- Package name: com.mannlab.games.dopamineswap
- Namespace: MannLab.Games.DopamineSwap

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

The playable prototype starts directly in portrait play:

- Swipe the single visible card to reroll a full-random 1-100 score.
- The card visible when the timer reaches zero is compared automatically.
- Win when the visible card is higher than the computer score.
- Rounds 1-10 reveal the exact computer score; later rounds reveal a gradually widening score range.
- Winning adds the visible card to Score; losing records the best local Score.

## Verification

```sh
./scripts/verify-dopamine-swap-mvp.sh
./scripts/verify-dopamine-swap-unity.sh
./scripts/verify-dopamine-swap-webgl.sh
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
