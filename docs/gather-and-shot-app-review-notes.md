# Stop & Snow App Review Notes

## Review Notes Draft

Stop & Snow has been substantially redesigned after the previous 4.3(a) rejection. The app is now positioned and implemented as a stop-to-gather snowfield survival game rather than a generic arcade survival template.

New gameplay preview video:
[새 링크 입력]

Previous gameplay preview video:
https://drive.google.com/file/d/1oVT2ZDbGm-QbwXpHPiQEv80qAfqSQjdn/view?usp=sharing

## Core Differentiation

The central rule is the Build Snowball System. The player can move with one joystick to stay safe, but snowballs are built only while the player stops. Holding still grows the in-world snowball through Small, Packed, and Giant stages. Larger built snowballs hit harder, knock enemies back more, and can create area/piercing clear moments.

Snow is also a map resource. Stopping near a snow patch or snowdrift gathers faster, and the snow resource visibly shrinks until it is depleted.

This creates a specific risk decision:

- Move to stay safe.
- Stop near fresh snow to build larger snowballs.
- Choose when to leave before snowman enemies close in.
- Let automatic throwing use the current built snowball once enemies enter range.
- Spend Snow Coin in the Snow Gear Lab to change the next run.

## New 4.3(a) Differentiation Changes

- Rebuilt the in-game art direction around the polished Stop & Snow concept icon:
  - Player now has separate idle, move, gather/build, auto-throw, and hit sprites.
  - During gathering, a hand-built snowball visibly grows in front of the player instead of relying only on a HUD ammo number.
  - Enemies are a white snowman family with role-color accessories rather than generic humanoid silhouettes.
- Added the Build Snowball System:
  - Small snowball after a short stop.
  - Packed snowball after a longer stop, with stronger impact.
  - Giant snowball after a risky long stop, with big-snowball style area/piercing pressure.
  - Auto throw now prioritizes the currently built snowball while preserving one-joystick automatic combat.
- Tuned the opening so the first seconds explicitly ask the player to build a Giant snowball, and early auto throw waits for Packed snow when the player is not in immediate danger.
- Added map-based snow resources: Small Snow Patch, Big Snowdrift, and Icy Snowdrift.
- Added snow resource depletion: repeated gathering reduces the visible snowdrift until it disappears.
- Added stronger stop-to-gather feedback: charging ring, snow cloud, snow trail particles moving from the resource to the player, and compact snowball stack around the character.
- Added snow resource rewards: depleted snowdrifts grant Snow Coin and can unlock Big Snowball or Ice Shot moments.
- Reworked enemies into a snowman enemy family:
  - Walker: small teal-accent snowman.
  - Runner: red-scarf fast snowman with movement trail.
  - Heavy: large purple-accent snowman with higher health and heavier hit response.
- Reframed the upgrade screen as Snow Gear Lab with gear-slot upgrades:
  - Snow Pouch
  - Wool Gloves
  - Throw Mitts
  - Packed Core
  - Warm Coat
  - Magnet Charm
- Added simple gear icons and purchasable-slot pulse feedback in Snow Gear Lab so the upgrade screen reads as equipment progression rather than a plain text menu.
- Updated early missions to read as snowfield challenges rather than score-only goals.
- Added named zone progression to the mission chain: Frost Yard, Snow Patch Field, Red Scarf Lane, Heavy Snowbank, and Blizzard Gate.
- Added dedicated zone banners plus zone-specific tint/accent patterns and arrival spawns.
- Updated app icon and in-game sprites so screenshots show a human winter player gathering snow while snowmen approach.

## First 60 Seconds For Review

- 0-10 seconds: move, see Frost Yard banner, stop near a fresh snow patch, see the hand-built snowball grow toward Packed/Giant, first automatic throw, first enemy defeat, and Snow Coin feedback.
- 10-30 seconds: Small/Packed snowball decisions, ammo pressure, snow patch depletion, mission progress, and Snow Coin rewards.
- 20-45 seconds: Big Snowdrift appears and provides faster gather timing.
- 35-60 seconds: mission completion, Rapid Throw reward, and first free gear upgrade availability.

The first run begins in Frost Yard, then mission copy moves the player toward Snow Patch Field and Red Scarf Lane so the early game reads as a snowfield route rather than a single endless score screen.

## Progression And Rewards

Snow Coin is earned from enemy defeats, snow resource depletion, pickups, wave survival, and mission completion. The result screen emphasizes Snow Coin earned and links directly to Snow Gear Lab. The first gear upgrade is free so reviewers can verify the progression loop immediately.

Weapon variations available during early play include Big Snowball, Split Snowball, Ice Shot, Snow Burst, and Rapid Throw. The Big Snowdrift and Icy Snowdrift create visible before/after gameplay moments: small snowball ammo gathering becomes a larger attack that can clear pressure.

## Advertising Behavior

- Forced game-over interstitials are blocked during the first 3 runs and first 3 session minutes.
- Rewarded ad entry points are optional reward hooks only: 2x Snow Coin, Revive, and Bonus Chest.
- Rewarded hooks include failure handling and do not leave the app waiting indefinitely.

## Analytics And Stability

Existing Firebase and AdMob initialization hooks are preserved. Added gameplay events include first_action, first_reward, first_upgrade, currency_earned, upgrade_purchase, weapon_unlocked, wave_start, zone_enter, zone_objective_complete, enemy_defeated, runner_first_seen, heavy_first_seen, ammo_empty, gather_start, gather_complete, snowball_build_start, snowball_build_stage, snowball_build_complete, snowball_auto_thrown, giant_snowball_used, rewarded_offer_shown, rewarded_offer_completed, and run_end_reason.

## Reviewer Checklist

- Watch the linked gameplay preview video to see the first-run loop without relying on text instructions.
- Start a run and move with the joystick.
- Release input near a snow patch to see resource-based stop-to-gather.
- Continue holding still to see Small, Packed, and Giant snowball stages.
- Observe the snow patch shrinking/depleting as ammo is gathered.
- Let a snowman enter range to see automatic throwing use the built snowball.
- Defeat enemies and deplete snowdrifts to receive Snow Coin popups.
- End the run and open Snow Gear Lab from the result screen.
- Claim the free first gear upgrade or buy another upgrade with Snow Coin.
