# Thumbwaddle

Unity mobile MVP for a thumb-driven penguin waddle distance run controlled by left-foot and right-foot touches. The player-facing title and public URL are `Thumbwaddle` and `/thumbwaddle`; the Unity project path, namespace, Android package id, and some build scripts keep the original `walking` identifier for build compatibility.

## Project

- Unity editor: 6000.3.23f1
- Platform: iOS / Android / WebGL
- iOS bundle id: com.mannlab.games.thumbwaddler
- iOS provisioning profile: Thumbwaddle (`3c745d8d-b794-4204-b5f1-2fd886a0242e`)
- Android package name: com.mannlab.games.walking
- Namespace: MannLab.Games.Walking

## First Open

Open this directory from Unity Hub and run `Assets/_Project/Scenes/Game.unity`.

## Prototype Scope

- Rear third-person portrait distance run with Ready, Playing, and Result states.
- The player sees a small rounded sketch-style penguin and left/right feet from behind so touch results are readable.
- The default penguin, iceberg, ice field, distant polar backdrop, and small snow/ice dressing visuals are generated doodle PNGs under `Assets/Resources/Thumbwaddle`, following the heavier outline and paper-texture language used by `Standing!`.
- Run `scripts/generate-thumbwaddle-doodle-assets.py` after changing the generated art recipe.
- The penguin has idle, left-step, right-step, stumble, and happy/result poses so touch cause, invalid input, and end-of-run feedback read through character motion.
- Foot positions, support feet, candidate steps, and return gestures drive both movement and the visible character.
- Set `debugFootMarkers` on `WalkingController` to show extra development-only candidate markers.
- Screen-left input controls the left foot, screen-right input controls the right foot.
- When a side does not need return, touching anywhere on that side can land the foot when stride, side clearance, and obstacle checks pass.
- The visible foot stamps first, then the paper-doll body and camera follow smoothly so cause and effect are readable.
- After a foot lands, that same side must return near the body side of the screen before it can place another step; dragging the same thumb down can complete return, but return motion is ignored as movement.
- On mobile/WebGL, mouse fallback is suppressed briefly after real touches so synthetic mouse input cannot steal one side of the controls.
- The first-run rhythm is intentionally simple: stamp high, pull low, disappear, repeat.
- The HUD and lower touch zones show step, blocked, pull-back, and returning states.
- Distance accumulates from body-center movement during a short timed run. Results also show steps and broken icebergs as small secondary rewards.
- The current MVP space is a broad paper field with sparse faceted iceberg obstacles, not a maze.
- The score is total waddled distance, not one-axis forward progress, so the field avoids numbered or ruler-like distance ticks.
- Icebergs block invalid landings and body overlap, but they chip down through intact/cracked doodle states after repeated contact and emit small ice chips so the player can eventually push through.

## Testing Notes

- Laptop testing is useful for smoke checks around camera, scoring, and state transitions, but not final thumb feel.
- Real control feel must be judged on a mobile multitouch device.
- The public web route is `/thumbwaddle`; `/walking` remains a legacy alias for old links.
- Firebase/Crashlytics code readiness is wired through `FirebaseTelemetry`.
- Crashlytics needs Firebase config files for the stable app ids: `Assets/GoogleService-Info.plist` for iOS using `com.mannlab.games.thumbwaddler` and `Assets/google-services.json` for Android using `com.mannlab.games.walking`.
- iOS builds can import the Firebase plist just before export by setting `MANNLAB_THUMBWADDLE_FIREBASE_IOS_PLIST=/path/to/GoogleService-Info.plist`.
- `FirebaseTelemetry` runs `FirebaseApp.CheckAndFixDependenciesAsync()`, queues early events until Firebase is ready, enables Firebase Analytics collection, and forwards Unity exceptions to Crashlytics.
- Development builds can force a Crashlytics test crash with the `--mannlab-force-crashlytics-test` argument, the `MANNLAB_FORCE_CRASHLYTICS_TEST=1` environment variable, or seven quick taps in the top-left corner.
- AdMob is wired through `MannLabAdMob`. The iOS production AdMob App ID is `ca-app-pub-4525914685149405~7787773444`, and the game-over interstitial unit is `ca-app-pub-4525914685149405/1797809111`.
- Game-over interstitials are requested every 3 completed runs in release builds; `BuildAdMobTest` forces Google's sample interstitial unit and requests an ad every run for smoke testing.
- Android production AdMob IDs are not configured yet, so Android ads remain disabled until those values are added.
- Run `scripts/verify-thumbwaddle-admob-crashlytics-readiness.sh` to check local Crashlytics/AdMob wiring. Missing Firebase config files are warnings unless `REQUIRE_FIREBASE_CONFIG=1` is set.
- First App Store submission should describe only the current distance/rhythm/iceberg loop. Fish, upgrades, chests, and rewarded ad choices are future growth candidates unless implemented and verified later.

## Tuning Candidates

- Step length: `WalkingRules.MinStepDistance` and `WalkingRules.MaxStepDistance`.
- Return zone height: `WalkingRules.ReturnGestureMaxScreenY`.
- Camera rotation blend: `WalkingController.TryLandFoot`.
- Field size and obstacle density: `WalkingController.openFieldLength`, `WalkingController.openFieldHalfWidth`, and `WalkingController.openFieldObstacleCount`.
- Visual layer strength: `scripts/generate-thumbwaddle-doodle-assets.py` controls the penguin poses, iceberg cracks, backdrop, and field dressing.
- Invalid input feedback: `invalidPulse`, status badges, and invalid color.
- Mode shape: consider stronger obstacle penalties only after the thumb rhythm feels clear.
