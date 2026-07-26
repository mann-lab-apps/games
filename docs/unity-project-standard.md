# Unity Project Standard

This repo keeps each game as a separate Unity project inside one Git repository.

## Project Layout

```txt
prototypes/my-game/
  Assets/
    _Project/
      Art/
      Audio/
      Prefabs/
      Scenes/
      Scripts/
      Settings/
  Packages/
  ProjectSettings/
  README.md
```

Use `_Project` for game-owned files so imported assets and packages stay easy to distinguish.

## Naming

- Directory names use `kebab-case`: `stack-jump`, `color-dash`.
- C# namespaces use PascalCase under `MannLab.Games`: `MannLab.Games.StackJump`.
- Scenes use PascalCase: `Boot`, `Game`, `Result`.

## Unity Settings

For Android-oriented projects:

- Use IL2CPP for release builds.
- Use ARM64 for Google Play release builds.
- Use portrait orientation unless the game concept needs landscape.
- Prefer deterministic 60 FPS prototypes before adding visual polish.
- Keep scenes small: `Boot`, `Game`, and `Result` is enough for most first passes.

## Packages

Start lean. Add SDKs only when the game needs them:

- Input System for richer input handling.
- Addressables only when content size or remote delivery requires it.
- Firebase, ads, attribution, and consent SDKs only after a game graduates from prototype.

## Shared Code

Reusable code should move into `shared/unity-packages/` once two games need it. Avoid moving code into shared while only one game uses it.

