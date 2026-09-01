# Thumbwaddle Supercent Loop QA

## Build

- Project: `prototypes/walking`
- Display/URL: `Thumbwaddle`, `/thumbwaddle`
- Internal identifiers remain `walking` and `com.mannlab.games.walking`.

## Implemented Loop

- First 12 steps show a world-space ghost footprint for the recommended next foot.
- First 20 seconds, and later upgrade-assisted play, corrects `short`, `long`, `wide`, and `cross` foot placements toward a nearby valid step.
- Fish rewards spawn in the first few meters so the first collection can happen within the opening seconds.
- Fish is saved as permanent currency and shown with run Fish in the HUD.
- Icebergs now pay Fish when broken; low shards, small icebergs, and normal icebergs differ by durability/reward.
- Rhythm quality drives small `Perfect Waddle` combo rewards and improves collection value/radius.
- Result screen prioritizes `Fish earned`, then distance/steps/ice/rhythm, with `Upgrade`, `2x Fish`, `Chest`, and `Next Run`.
- First free upgrade is forced before the second run if the player skips the `Free Up` button.
- Forced interstitials are gated until after both three runs and three minutes have passed.
- Rewarded slots currently use test hooks/events for `2x Fish` and `bonus_chest`.

## Upgrade Effects

- Bigger Feet: increases assisted placement forgiveness.
- Better Balance: increases cross/wide correction forgiveness.
- Icebreaker Feet: increases iceberg hit damage.
- Fish Magnet: increases auto-collect radius.
- Rhythm Bonus: increases combo Fish rewards and coin value scaling.

## Analytics Hooks

Added events: `tutorial_start`, `tutorial_complete`, `first_reward`, `currency_earned`, `upgrade_purchase`, `rewarded_offer_shown`, `rewarded_offer_completed`, `step_invalid_reason`, `combo_peak`, and `run_end_reason`.

All new events include `game=thumbwaddle`, run number, session time, distance, Fish, and upgrade summary where available.

## Verification Notes

- `scripts/verify-walking-mvp.sh` covers compile, existing walking rules, assisted foot placement, collection radius, iceberg rewards, icebreaker damage, rhythm reward, and upgrade cost scaling.
- Manual 5-minute feel QA still needs a mobile multitouch pass after WebGL/iOS build because desktop mouse cannot validate two-thumb rhythm.
