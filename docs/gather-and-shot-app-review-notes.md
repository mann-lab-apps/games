# Gather & Shot App Review Notes

## Review Notes Draft

Gather & Shot build 1.0.1 (5) has been updated as a distinct stop-to-gather survival game.

Gameplay preview video:
https://drive.google.com/file/d/1oVT2ZDbGm-QbwXpHPiQEv80qAfqSQjdn/view?usp=sharing

The core rule is unique to this app: the player moves with one joystick to stay safe, but snow ammo only gathers while the player stops. When enemies enter range, the character automatically throws snowballs without manual aiming. This creates a repeated risk decision: keep moving to avoid enemies, or stop briefly to build ammo.

Key gameplay features available during review:

- Stop-to-gather ammo loop with a visible charging ring, snow cloud, and a small representative snowball stack around the player.
- The latest build reduces duplicate ammo UI: the charging feedback is shown primarily around the character, while the top-right Snow counter remains a compact exact ammo count.
- Automatic snowball throwing when enemies enter range.
- Snow Coin currency earned from enemy defeats, pickups, wave survival, and mission completion.
- Persistent upgrade progression through the Snow Workshop.
- Six upgrade tracks: Ammo Capacity, Gather Speed, Throw Rate, Snowball Damage, Warm Coat, and Coin Magnet.
- First upgrade is free and is presented through the Snow Workshop so players can see the progression system.
- Mission chain during early play: first snow loop, gather snow, collect big snowdrift, survive runner wave, and defeat a heavy enemy.
- Weapon variations for visible gameplay changes: Big Snowball, Split Snowball, Ice Shot, Snow Burst, and Rapid Throw.
- Enemy staging: snowman Walker enemies first, snowman Runner enemies after the first minute, snowman Heavy enemies after two minutes, and mixed pressure later.
- Updated art direction: the player is a human winter character, while enemies are readable snowmen with different color accents for each role.

The first 60 seconds are designed to show the full loop:

- 0-10 seconds: move, stop, gather snow, first automatic throw, first enemy defeat, first Snow Coin reward.
- 10-30 seconds: ammo pressure, mission progress, and coin popups.
- 20-45 seconds: big snowdrift pickup and weapon cache opportunities.
- 35-60 seconds: first mission completion, Rapid Throw reward, and free upgrade availability.

Advertising behavior:

- Forced game-over interstitials are blocked during the first 3 runs and first 3 session minutes.
- Rewarded ad entry points are optional reward hooks only: 2x Snow Coin, Revive, and Bonus Chest.
- Rewarded hooks include failure handling and do not leave the app waiting indefinitely.

Analytics and stability:

- Existing Firebase and AdMob initialization hooks are preserved.
- Added gameplay events include first_action, first_reward, first_upgrade, currency_earned, upgrade_purchase, weapon_unlocked, wave_start, enemy_defeated, ammo_empty, gather_start, gather_complete, rewarded_offer_shown, rewarded_offer_completed, and run_end_reason.

Reviewer checklist:

- If possible, watch the linked gameplay preview video first to see the first-run loop without relying on text instructions.
- Start a run and move with the joystick.
- Release input to see the stop-to-gather charging ring, snow cloud, and compact snow ammo stack.
- Let an enemy enter range to see automatic throwing.
- Defeat enemies to receive Snow Coin popups.
- End the run and open Snow Workshop from the result screen.
- Claim the free first upgrade or buy another upgrade with Snow Coin.
