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

## 2026-07-26 MVP implementation

- Changed: Created the actual Unity project at `prototypes/10000`.
- Changed: Added `Packages/manifest.json` with `com.unity.ugui`.
- Changed: Added `Assets/_Project/Scenes/Game.unity` as the MVP scene.
- Changed: Implemented board generation with guaranteed right/down `10000` placement.
- Changed: Implemented detection for all right/down `10000` sequences, including accidental extra matches.
- Changed: Implemented stage difficulty time limits from the design document.
- Changed: Implemented runtime UI construction for the board, timer, score labels, result panel, restart flow, and local best score.
- Changed: Added sketch-style UI outlines and marker-style correct/wrong feedback.
- Changed: Added an Editor script to recreate the scene and apply Android-oriented project settings after Unity licensing is active.
- Changed: Added an Editor script to build Android App Bundle output after Unity licensing is active.
- Changed: Added `scripts/verify-10000-unity.sh` for post-license Unity import and Android `.aab` verification.
- Changed: Added 10000-specific analytics event candidates to `docs/10000-game-design.md`.
- Verified: `git diff --check` passes.
- Verified: `scripts/verify-10000-mvp.sh` passes.
- Verified: Runtime scripts compile against Unity `6000.3.20f1` assemblies.
- Verified: Editor scripts compile against Unity `6000.3.20f1` assemblies.
- Verified: `BoardGenerator` generates at least one target sequence for 1000 deterministic seeds.
- Verified: Unity CLI lists Unity `6000.3.20f1` with Android Build Support, Android SDK & NDK Tools, and OpenJDK.
- Verified: Android SDK platform `android-36` exists under the installed Unity editor.
- Verified: `scripts/verify-10000-unity.sh` fails early with exit `2` when Unity CLI is not logged in.
- Verified: Unity batchmode was retried and exits `198` with `No valid Unity Editor license found`.
- Blocked: Unity batchmode import, Play Mode, and Android `.aab` build could not complete because Unity Hub is not logged in and no valid Unity Editor license is activated on this machine.
- Next: Activate Unity license through Unity Hub.
- Next: Re-run `scripts/verify-10000-unity.sh`.
- Next: Open `Assets/_Project/Scenes/Game.unity` and test touch/readability in Play Mode.
- Next: Build and smoke test on a real Android device.
