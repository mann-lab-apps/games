# Walking

Unity mobile MVP for a first-person maze walk controlled by left-foot and right-foot touches.

## Project

- Unity editor: 6000.3.23f1
- Platform: Android
- Package name: com.mannlab.games.walking
- Namespace: MannLab.Games.Walking

## First Open

Open this directory from Unity Hub and run `Assets/_Project/Scenes/Game.unity`.

## Prototype Scope

- First-person portrait maze run with Ready, Playing, and Result states.
- The player sees the maze corridor and walls from eye height; feet are not shown in normal play.
- Foot positions, support feet, candidate steps, and return gestures exist only as internal simulation.
- Set `debugFootMarkers` on `WalkingController` to show development-only foot and candidate markers.
- Screen-left input controls the left foot, screen-right input controls the right foot.
- Releasing a touch lands the foot when stride, side clearance, and wall checks pass.
- After a foot lands, that same side must start one return touch near the body side of the screen before it can place another step; return touches are ignored as movement.
- The first-run rhythm is intentionally simple: step high, pull low, repeat.
- The HUD and lower touch zones show step, blocked, pull-back, and returning states.
- Distance accumulates from body-center movement until the body radius touches a wall.

## Testing Notes

- Laptop testing is only useful for smoke checks around camera, maze, scoring, and state transitions.
- Real control feel must be judged on a mobile multitouch device.
- Until Unity WebGL builds are available, `/walking` in the local web app may show a placeholder instead of the playable build.

## Tuning Candidates

- Step length: `WalkingRules.MinStepDistance` and `WalkingRules.MaxStepDistance`.
- Return zone height: `WalkingRules.ReturnGestureMaxScreenY`.
- Camera rotation blend: `WalkingController.TryLandFoot`.
- Maze width: `WalkingRules.TileSize` and maze opening rules.
- Invalid input feedback: `invalidPulse`, status badges, and invalid color.
