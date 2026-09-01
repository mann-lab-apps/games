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
2. For new ad-enabled Unity iOS games, read `docs/unity-ios-admob-crashlytics-template.md` and start from `prototypes/_unity-ios-admob-template/`.
3. Use `docs/classic-casual-twist-strategy.md` when choosing small classic-casual variants.
4. Create new concepts in `prototypes/`.
5. Promote promising projects into `releases/`.
6. Keep common code in `shared/unity-packages/`.
7. Treat every game directory as its own Unity project.
8. Use the shared hand-drawn sketch visual direction in `docs/visual-direction.md`.

```sh
./scripts/new-unity-game.sh prototypes stack-jump
```

Then open the generated directory from Unity Hub and let Unity import the project.

## Current Project Status

See `docs/project-inventory.md` for the full cleanup inventory and legacy policy.

| Project | Status | Notes |
| --- | --- | --- |
| `10000` | active/live | Fast number-search puzzle, see `docs/10000-game-design.md` and `docs/10000-worklog.md`. |
| `gather-and-shot` | priority candidate | Sketch survival prototype and current top improvement candidate, see `docs/gather-and-shot-game-design.md`. |
| `standing` | candidate | Strong marketability hook, see `docs/standing-game-design.md`. |
| `yacht-rush` | candidate | Dice/contract prototype with good implementation depth. |
| `2048-crash` | release experiment | Stable 2048 variant with release-readiness docs, see `docs/2048-crash-game-design.md`. |
| `walking` / `Thumbwaddle` | MVP, rename cleanup | `walking` remains the Unity/internal project; public name is `Thumbwaddle`. Do not delete as scratch. |
| `2048-blink` | prototype | Memory-heavy 2048 variant, see `docs/2048-blink-game-design.md`. |
| `best-ramyeon` | prototype | Web-first timing prototype. |
| `flying-bird` / `Wind Gull` | prototype | Energy-limited flap/glide distance prototype, see `docs/flying-bird-game-design.md`. |
| `rainwalker` | prototype | Rain defense mini-game prototype. |
| `dopamine-swap` | low-priority prototype | Retheme needed before further production, see `docs/dopamine-swap-game-design.md`. |
| `drum-duel` | archive | Rhythm echo prototype retained as source/archive; hidden from the public catalog. |
| `_unity-ios-admob-template` | template | Starter layer for ad-enabled Unity iOS games. |

## Baseline

- Engine: Unity 6 LTS line. Current local baseline: Unity 6000.3.20f1.
- Platform: Android first, iOS supported when Apple release prerequisites are ready.
- Store target: Google Play target API requirements should be checked before every release.
- iOS target: App Store submission requirements should be checked before every release.
