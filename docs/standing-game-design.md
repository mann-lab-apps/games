# Standing

Standing is a one-touch hyper-casual stealth-rest prototype.

## Core Loop

- The player character stands behind a desk chair, shown from the back.
- Standing drains stamina over time.
- Holding the screen makes the character sit and recover stamina.
- Each passer keeps an individual readable walking speed.
- A wide warm carpet runner sits on the customer walking lane behind the desk and marks the area where a customer can discover the employee.
- Passers enter from either side one at a time, with varied clothing tint so phone use matters more than shirt color.
- If a visitor passes while the character is sitting, the character is caught and the run ends.
- If stamina reaches zero, the character collapses and the run ends.
- The score is survival time.

## MVP Controls

- Press and hold anywhere on the play area: sit.
- Release: stand.
- `Again`: restart after game over.

## UI

- Stamina bar
- Survival time
- Best survival time
- Current posture
- Current risk state: `Clear`, `Footsteps`, `Passing`
- Passers should be identified by their pose/props, without colored aura backplates.

## States

- `Standing`
- `Sitting`
- `Caught`
- `Exhausted`
- `GameOver`

## Art Direction

The first pass uses the MannLab sketch-style UI palette with simple readable silhouettes:

- rear-view player character
- desk and monitor
- chair seat
- passing person silhouette in the foreground

## Deferred

- Ads
- Firebase/Crashlytics
- App Store assets
- Platform release setup
