# Mann Lab Games

Unity-first monorepo for Mann Lab hyper-casual game experiments and mobile release candidates.

## Repo Structure

```txt
games/
  prototypes/  # fast experiments and disposable game concepts
  releases/    # games being prepared for Android release
  shared/      # reusable Unity packages, assets, config, and publishing utilities
  templates/   # starter project shells and conventions
  scripts/     # repo-level automation
  docs/        # release and project standards
```

## Recommended Flow

1. Start from the workflow in `docs/production-workflow.md`.
2. Use `docs/classic-casual-twist-strategy.md` when choosing small classic-casual variants.
3. Create new concepts in `prototypes/`.
4. Promote promising projects into `releases/`.
5. Keep common code in `shared/unity-packages/`.
6. Treat every game directory as its own Unity project.
7. Use the shared hand-drawn sketch visual direction in `docs/visual-direction.md`.

```sh
./scripts/new-unity-game.sh prototypes stack-jump
```

Then open the generated directory from Unity Hub and let Unity import the project.

## Current Prototypes

- `2048-blink`: 2048 memory variant with alternating odd/even curtain cells, see `docs/2048-blink-game-design.md`.
- `2048-crash`: static special-block variant of 2048, see `docs/2048-crash-game-design.md`.
- `10000`: see `docs/10000-game-design.md` and `docs/10000-worklog.md`.
- `dopamine-swap`: candidate card comparison game, see `docs/dopamine-swap-game-design.md`.
- `drum-duel`: candidate/archive rhythm echo prototype, see `docs/drum-duel-game-design.md`.
- `flying-bird`: Wind Gull, energy-limited flap/glide distance prototype, see `docs/flying-bird-game-design.md`.

## Baseline

- Engine: Unity 6 LTS line. Current local baseline: Unity 6000.3.20f1.
- Platform: Android first, iOS supported when Apple release prerequisites are ready.
- Store target: Google Play target API requirements should be checked before every release.
- iOS target: App Store submission requirements should be checked before every release.
