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
- Verified: Opening the project through Unity CLI starts the editor import path and generates Unity-managed `.meta`, `ProjectSettings`, and `packages-lock.json` files.
- Verified: Unity generated `Library/ScriptAssemblies/Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll` during the editor import attempt.
- Verified: `scripts/verify-10000-mvp.sh` still passes after the editor import attempt.
- Blocked: Unity batchmode import, Play Mode, and Android `.aab` build could not complete because Unity Hub is not logged in and no valid Unity Editor license is activated on this machine.
- Next: Activate Unity license through Unity Hub.
- Next: Re-run `scripts/verify-10000-unity.sh`.
- Next: Open `Assets/_Project/Scenes/Game.unity` and test touch/readability in Play Mode.
- Next: Build and smoke test on a real Android device.

## 2026-07-26 Unity license and Android build verification

- Changed: Updated `scripts/verify-10000-unity.sh` to rely on an activated Unity license instead of the Unity CLI login flag.
- Changed: Unity reserialized `Assets/_Project/Scenes/Game.unity` after the editor import path successfully ran.
- Changed: Unity applied Android-oriented player settings, including Mann Lab company name and Android package identifier.
- Verified: Unity CLI lists an assigned `Unity Personal` license.
- Verified: Unity batchmode scene creation/import completes successfully.
- Verified: `scripts/verify-10000-unity.sh` completes successfully.
- Verified: Android App Bundle was created at `prototypes/10000/Builds/Android/10000.aab`.
- Next: Open the project in Unity Editor GUI and run Play Mode for touch/readability feel.
- Next: Install the generated Android build on a device for a real smoke test.

## 2026-07-26 release-candidate loop adjustment

- Decision: Today's release candidate should focus on deployment only; ads, analytics SDKs, Crashlytics, and Google Ads are deferred to a later iteration.
- Decision: `10000` should use one continuous run timer instead of resetting time on every stage.
- Decision: The title should be supported by a visual opening cue so players understand they are looking for the `1 0 0 0 0` pattern.
- Changed: Updated the game loop so a run starts with `60` seconds and each cleared stage keeps the remaining time.
- Changed: Added an opening `1 / 0 / 0 / 0 / 0` tile motion before each run starts.
- Changed: Removed unused stage-by-stage time difficulty code.
- Changed: Documented the minimal Google Play internal testing deployment checklist in `docs/production-workflow.md`.
- Next: Rebuild Android App Bundle after the timer change.
- Next: Upload the `.aab` to Google Play Console internal testing.
- Next: Smoke test the Play-delivered build on a real Android device.

## 2026-07-27 direct APK sharing

- Decision: Use Mannlab site sharing as the immediate distribution path while Google Play production requirements are pending.
- Changed: Added release signing support to the Android build script.
- Changed: Created a local, Git-ignored Android upload keystore for `10000`.
- Verified: Built a release-signed Android APK at `prototypes/10000/Builds/Android/10000.apk`.
- Verified: `apksigner verify` passes with APK Signature Scheme v2.
- Next: Publish the APK through the Mannlab site download page.
- Next: Replace the temporary tester subscription link with a Google Form or Google Group flow.

## 2026-07-27 Firebase Analytics and Crashlytics prep

- Decision: Add Firebase Analytics and Crashlytics before Google Play production release; defer ads until later.
- Changed: Added a Firebase telemetry adapter that compiles before the Firebase Unity SDK is imported.
- Changed: Instrumented `app_open`, `run_start`, `wrong_tap`, `stage_clear`, and `run_end` events.
- Changed: Added Crashlytics forwarding for Unity exceptions when the Firebase Crashlytics SDK is present.
- Changed: Documented the Firebase Console, `google-services.json`, and Unity SDK import steps.
- Blocked: Firebase Console app registration and `google-services.json` must be completed from the Mann Lab Firebase account.
- Next: Create or select the Firebase project and register Android package `com.mannlab.games.game10000`.
- Next: Import `FirebaseAnalytics.unitypackage` and `FirebaseCrashlytics.unitypackage` into `prototypes/10000`.
- Next: Run an Android release build on a real device and confirm events/crashes appear in Firebase.
