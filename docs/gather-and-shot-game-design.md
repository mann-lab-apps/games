# Gather & Shot

Gather & Shot is a mobile portrait snowball survival prototype.

## Core Loop

- Move with a virtual touch joystick.
- Collect snowballs, snowdrifts, and rare big snowdrifts to build ammo.
- When an enemy enters range, the player automatically throws one snowball at the nearest enemy.
- Each throw spends one snowball.
- Defeated enemies add score.
- Contact damage drains Warmth and knocks the player away.
- The run ends when Warmth reaches zero.
- Score and best score are kill counts.

## Pickups

- Snowball: common pickup, +1 ammo.
- Snowdrift: medium pickup, +3 ammo.
- Big snowdrift: rare large pickup, +5 ammo, often appears near enemies to create a simple risk-reward route choice.

## Enemies

- Walker: baseline chaser.
- Runner: small, fast pressure enemy.
- Heavy: large, slow enemy that needs multiple hits.

## Difficulty

- Enemy spawn gaps shrink over time.
- Maximum live enemies rises over time.
- Enemy speed rises over time.
- Player speed also rises slightly, making late runs faster while preserving escape skill.

## Visual Direction

The prototype follows the Standing sketch direction: warm paper, readable silhouettes, dark ink outlines, loose hand-drawn wobble, soft blue snow shadows, and restrained HUD panels.

## Deferred

- Weapon levels
- Upgrade choices
- Additional enemy families
- Ads, Firebase, Crashlytics, and store metadata
