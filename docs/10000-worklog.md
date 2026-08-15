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

## 2026-07-27 iOS individual account release prep

- Decision: App Store distribution can proceed through an individual Apple Developer Program account, with the developer name shown as the account holder's legal name.
- Decision: Keep the iOS bundle identifier aligned with Android: `com.mannlab.games.game10000`.
- Changed: Added `docs/ios-release-baseline.md` for individual account App Store/TestFlight requirements.
- Changed: Added a Unity iOS Xcode-project build method at `Builds/iOS/Xcode`.
- Changed: Added `scripts/verify-10000-ios-readiness.sh` to check local iOS build prerequisites.
- Verified: `./scripts/verify-10000-mvp.sh` passes after adding the iOS build script.
- Verified: Unity iOS Build Support is installed for Unity `6000.3.20f1`.
- Verified: Unity generated the iOS Xcode project at `prototypes/10000/Builds/iOS/Xcode`.
- Verified: Xcode `16.3` completed an unsigned Release `iphoneos` build and produced `10000.app`.
- Blocked: Local Xcode is `16.3`; App Store uploads require Xcode 26/iOS 26 SDK or newer as of April 28, 2026.
- Next: Enroll the personal Apple Developer Program account.
- Next: Install Xcode 26 or newer before TestFlight/App Store upload.
- Next: Create the App Store Connect app record and reserve `com.mannlab.games.game10000`.

## 2026-07-27 sketch design system baseline

- Decision: Keep `docs/visual-direction.md` as the short visual entry point and add `docs/design-system.md` for concrete design tokens and Unity reuse rules.
- Decision: Promote sketch palette, spacing, button colors, and rough outlines into `shared/unity-packages/com.mannlab.hypercasual-core`.
- Changed: Added shared `SketchPalette`, `SketchMetrics`, `SketchUiFactory`, and `SketchOutlineGraphic` runtime helpers.
- Changed: Updated `10000` to use the shared Mann Lab Games sketch style helpers.
- Changed: Updated the MVP verification script so it compiles shared Unity package runtime code with the game scripts.
- Verified: Unity batchmode import succeeds with the local shared package.
- Next: Add a `SketchTheme` ScriptableObject only after a second game needs theme overrides.
- Next: Replace system fonts only after a readable, licensed handwritten font is selected.

## 2026-07-29 WebGL sharing build

- Decision: Use Unity WebGL as the no-Apple-Developer-account sharing path for iPhone Safari testing.
- Changed: Installed Unity WebGL Build Support for Unity `6000.3.20f1`.
- Changed: Added a Unity WebGL build method at `Builds/WebGL/10000`.
- Changed: Added `scripts/verify-10000-webgl.sh` for repeatable WebGL builds.
- Changed: Added `scripts/serve-10000-webgl.sh` to serve Unity `.gz` and `.wasm` assets with browser-friendly headers.
- Verified: WebGL build completed successfully at `prototypes/10000/Builds/WebGL/10000`.
- Verified: WebGL output size is about `4.7M`.
- Next: Open the local network URL on iPhone Safari and smoke test touch/readability.
- Next: Publish the same WebGL folder to a public Mannlab-hosted URL for external sharing.

## 2026-07-29 WebGL timer and layout fix

- Changed: Moved the board to a top-anchored layout so the first tile row no longer overlaps the timer bar.
- Changed: Replaced `Image.fillAmount` timer rendering with an explicit RectTransform width update for WebGL reliability.
- Changed: Added a compact seconds label in the header to make timer countdown visible even if the bar is subtle.
- Changed: Patched the WebGL shell to hide Unity's default footer and use a full-viewport canvas so the last tile row is not clipped.
- Verified: `./scripts/verify-10000-mvp.sh` passes.
- Verified: WebGL rebuild completed successfully and the local server is serving the updated `.wasm.gz`.
- Verified: The local server is serving the updated responsive WebGL CSS with `Cache-Control: no-store`.

## 2026-07-29 Mannlab Games web publish prep

- Decision: Publish the WebGL version through the Mannlab Games website instead of distributing native build files.
- Changed: Added `web/mannlab-games` as a lightweight Vite site that embeds the `10000` WebGL build.
- Changed: Added `scripts/sync-10000-webgl-to-site.sh` to copy Unity WebGL output into the site.
- Changed: The site copy expands Unity `.gz` build artifacts to plain `.data`, `.framework.js`, and `.wasm` files so static hosting does not depend on custom gzip headers.

## 2026-08-15 Crashlytics integration prep

- Tracking: GitHub issue `#20`.
- Decision: Add Crashlytics before ads so runtime diagnostics are available before monetization changes.
- Changed: Connected `10000` to the shared MannLab Firebase Unity SDK package.
- Changed: Upgraded `FirebaseTelemetry` with Crashlytics custom keys, guarded Firebase reflection calls, log recursion protection, and forced test crash support.
- Changed: Added a development-only hidden Crashlytics test trigger: tap the upper-left corner 7 times within 2.5 seconds.
- Changed: Added iOS and Android development build entry points for Crashlytics smoke testing.
- Changed: Added the iOS Firebase config for `com.mannlab.games.game10000` and generated Firebase editor/desktop config assets.
- Verified: `./scripts/verify-10000-mvp.sh` passes with Firebase SDK references included in the compile check.
- Verified: `./scripts/verify-10000-firebase-readiness.sh` passes for iOS, with Android config intentionally deferred.
- Verified: iOS simulator Xcode build succeeds and the Crashlytics run script reports `Validation succeeded`.
- Verified: Runtime logs show Firebase Crashlytics 12.16.0, Firebase dependencies available, and telemetry events reaching the SDK.
- Verified: Firebase Console detected the iOS app and the dSYM warning cleared after symbol processing.
- Deferred: Ads are intentionally postponed until gameplay metrics justify adding monetization.
- Deferred: Android Firebase config remains out of scope until Android release work resumes.
