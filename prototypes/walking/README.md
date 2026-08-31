# Thumbwaddle

Unity mobile MVP for a thumb-driven penguin waddle distance run controlled by left-foot and right-foot touches. The player-facing title and public URL are `Thumbwaddle` and `/thumbwaddle`; the Unity project path, namespace, package id, and some build scripts keep the original `walking` identifier for build compatibility.

## Project

- Unity editor: 6000.3.23f1
- Platform: Android
- Package name: com.mannlab.games.walking
- Namespace: MannLab.Games.Walking

## First Open

Open this directory from Unity Hub and run `Assets/_Project/Scenes/Game.unity`.

## Prototype Scope

- Rear third-person portrait distance run with Ready, Playing, and Result states.
- The player sees a small rounded sketch-style penguin and left/right feet from behind so touch results are readable.
- Foot positions, support feet, candidate steps, and return gestures drive both movement and the visible character.
- Set `debugFootMarkers` on `WalkingController` to show extra development-only candidate markers.
- Screen-left input controls the left foot, screen-right input controls the right foot.
- When a side does not need return, touching anywhere on that side can land the foot when stride, side clearance, and obstacle checks pass.
- The visible foot stamps first, then the paper-doll body and camera follow smoothly so cause and effect are readable.
- After a foot lands, that same side must return near the body side of the screen before it can place another step; dragging the same thumb down can complete return, but return motion is ignored as movement.
- On mobile/WebGL, mouse fallback is suppressed briefly after real touches so synthetic mouse input cannot steal one side of the controls.
- The first-run rhythm is intentionally simple: stamp high, pull low, disappear, repeat.
- The HUD and lower touch zones show step, blocked, pull-back, and returning states.
- Distance accumulates from body-center movement during a short timed run.
- The current MVP space is a broad paper field with sparse soft iceberg obstacles, not a maze.
- Obstacles block invalid landings and body overlap, but they do not end the run; the goal is still maximum distance before time runs out.

## Testing Notes

- Laptop testing is useful for smoke checks around camera, scoring, and state transitions, but not final thumb feel.
- Real control feel must be judged on a mobile multitouch device.
- The public web route is `/thumbwaddle`; `/walking` remains a legacy alias for old links.

## Tuning Candidates

- Step length: `WalkingRules.MinStepDistance` and `WalkingRules.MaxStepDistance`.
- Return zone height: `WalkingRules.ReturnGestureMaxScreenY`.
- Camera rotation blend: `WalkingController.TryLandFoot`.
- Field size and obstacle density: `WalkingController.openFieldLength`, `WalkingController.openFieldHalfWidth`, and `WalkingController.openFieldObstacleCount`.
- Invalid input feedback: `invalidPulse`, status badges, and invalid color.
- Mode shape: consider stronger obstacle penalties only after the thumb rhythm feels clear.
