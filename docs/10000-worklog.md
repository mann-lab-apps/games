# 10000 Work Log

## 2026-07-26

- Decision: Use `10000` as the first Mann Lab Games prototype.
- Decision: Use Unity as the Android-first engine.
- Decision: Keep all Mann Lab hyper-casual games in the `games` monorepo.
- Decision: Use a shared hand-drawn sketch visual direction for Mann Lab Games, inspired by whiteboard tools like Excalidraw.
- Decision: `10000` starts as a `10 x 10` board game where the player finds a horizontal or vertical `10000`.
- Decision: MVP input is tapping any cell in the correct `10000` sequence.
- Decision: MVP difficulty increases by reducing the stage time limit.
- Decision: Wrong taps subtract time instead of ending the run immediately.
- Changed: Created the `games` Git repository and connected it to `https://github.com/mann-lab-apps/games.git`.
- Changed: Added Unity Android starter repository structure.
- Changed: Installed Unity Hub, Unity CLI, Unity Editor `6000.3.20f1`, and Android modules.
- Changed: Added `docs/10000-game-design.md`.
- Changed: Added `docs/visual-direction.md`.
- Changed: Updated `scripts/new-unity-game.sh` so numeric game slugs such as `10000` generate valid Android package names and C# namespaces.
- Verified: Unity Editor reports version `6000.3.20f1`.
- Verified: Unity CLI lists Android Build Support, Android SDK & NDK Tools, and OpenJDK.
- Verified: `scripts/new-unity-game.sh prototypes 10000` generates `com.mannlab.games.game10000` and `MannLab.Games.Game10000`.
- Next: Create the actual `prototypes/10000` Unity project.
- Next: Implement the MVP loop.
- Next: Add analytics event planning after the core loop is playable.

