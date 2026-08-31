# Thumbwalk

Unity mobile MVP for a first-person maze walk controlled by left-foot and right-foot touches. The player-facing title is `Thumbwalk`; the project path and package keep the original `walking` identifier for build compatibility.

## Project

- Unity editor: 6000.3.23f1
- Platform: Android
- Package name: com.mannlab.games.walking
- Namespace: MannLab.Games.Walking

## First Open

Open this directory from Unity Hub and run `Assets/_Project/Scenes/Game.unity`.

## Prototype Scope

- Rear third-person portrait maze run with Ready, Playing, and Result states.
- The player sees a small paper-doll body and left/right feet from behind so touch results are readable.
- Foot positions, support feet, candidate steps, and return gestures drive both movement and the visible character.
- Set `debugFootMarkers` on `WalkingController` to show extra development-only candidate markers.
- Screen-left input controls the left foot, screen-right input controls the right foot.
- Touching high lands the foot immediately when stride, side clearance, and wall checks pass.
- After a foot lands, that same side must return near the body side of the screen before it can place another step; dragging the same thumb down can complete return, but return motion is ignored as movement.
- The first-run rhythm is intentionally simple: stamp high, pull low, disappear, repeat.
- The HUD and lower touch zones show step, blocked, pull-back, and returning states.
- Distance accumulates from body-center movement until the body radius touches a wall.
- The generated maze starts with a wide straight practice lane before it becomes more maze-like.

## Testing Notes

- Laptop testing is useful for smoke checks around camera, maze, scoring, and state transitions, but not final thumb feel.
- Real control feel must be judged on a mobile multitouch device.
- Until Unity WebGL builds are available, `/walking` in the local web app may show a placeholder instead of the playable build.

## Tuning Candidates

- Step length: `WalkingRules.MinStepDistance` and `WalkingRules.MaxStepDistance`.
- Return zone height: `WalkingRules.ReturnGestureMaxScreenY`.
- Camera rotation blend: `WalkingController.TryLandFoot`.
- Maze width: `WalkingRules.TileSize` and maze opening rules.
- Invalid input feedback: `invalidPulse`, status badges, and invalid color.
- Mode shape: consider a 60-second distance run with collision penalties once the thumb rhythm feels clear.
