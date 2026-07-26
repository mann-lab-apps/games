# Mann Lab Games

Unity-first monorepo for Mann Lab hyper-casual game experiments and Android release candidates.

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

1. Create new concepts in `prototypes/`.
2. Promote promising projects into `releases/`.
3. Keep common code in `shared/unity-packages/`.
4. Treat every game directory as its own Unity project.

```sh
./scripts/new-unity-game.sh prototypes stack-jump
```

Then open the generated directory from Unity Hub and let Unity import the project.

## Baseline

- Engine: Unity 6 LTS line, preferably the current LTS installed via Unity Hub.
- Platform: Android first.
- Store target: Google Play target API requirements should be checked before every release.

