# Stop & Snow Casual Growth QA

Date: 2026-09-07

## Change Summary

- Preserved the core loop: joystick movement, stop-to-gather ammo, automatic firing, ammo consumption, Warmth damage, and game-over restart.
- Added persistent Snow Coin economy with run-earned coins and owned coins in the HUD.
- Added six persistent upgrades: Ammo Capacity, Gather Speed, Throw Rate, Snowball Damage, Warm Coat, and Coin Magnet.
- Added first-loop growth pacing: edge-spawn opening enemies, timed big snowdrift, timed weapon cache, first mini goal, Rapid Throw reward, and first free upgrade.
- Added weapon variations for ad footage: Big Snowball, Split Snowball, Ice Shot, Snow Burst, plus Rapid Throw.
- Adjusted enemy staging: Walker before 60s, Runner from 60s, Heavy from 120s, mixed pressure after 240s.
- Reworked the result screen around Snow Coin earned, upgrade availability, 2x Coin, Revive, Bonus Chest, and Next Run.
- Added rewarded ad test hooks with success/failure telemetry and no waiting state.
- Blocked game-over interstitial attempts until after both the first 3 runs and first 3 session minutes.
- Expanded telemetry events and common run parameters.
- Changed the runtime camera/HUD from a square playfield assumption to a portrait 9:16 layout.
- Patched the WebGL shell during build to hide Unity's default footer and scale the canvas responsively.
- Adjusted HUD scaling so desktop WebGL side letterboxing uses height-based UI scale and narrow mobile views use width-based UI scale.
- Added App Review 4.3 differentiation pass: visible stop-to-gather ring, packed snow ammo around the player, auto-throw popups, Snow Coin popups, player/runner/ice-shot trail feedback, and clearer heavy enemy scale.
- Added a visible early mission chain so the game is not presented as a score-only template: first snow loop, gather snow, collect big snowdrift, survive runner wave, and defeat a heavy enemy.
- Added a Snow Gear Lab screen after the result screen with all six upgrade tracks, current level, next-level effect, cost/free state, and purchase feedback.
- Added submission support drafts: `docs/gather-and-shot-app-review-notes.md` and `docs/gather-and-shot-store-metadata-draft.md`.
- Replaced the 1024x1024 App Store icon with the polished Stop & Snow concept scene: a human winter player builds snow inside a blue ring while snowman Walker/Runner/Heavy-style enemies close in.
- Preserved the polished source concept at `Assets/_Project/Art/AppStore/Concepts/AppIcon-polished-snow-survival-concept-1024.png` and updated the asset generator so future character regeneration keeps using it for `Assets/_Project/Art/AppStore/AppIcon-1024.png`.
- Reskinned in-game character art for icon/game consistency:
  - `Assets/Resources/GatherAndShot/player.png`: human winter player with navy/teal clothing and reduced black-fill silhouette.
  - `Assets/Resources/GatherAndShot/walker.png`: small teal-accent snowman enemy.
  - `Assets/Resources/GatherAndShot/runner.png`: magenta-accent running snowman enemy.
  - `Assets/Resources/GatherAndShot/heavy.png`: large purple-accent heavy snowman enemy.
- Disabled runtime enemy sprite tinting so the white snowman bodies and role-color accessories remain visible in game.
- Added a second App Review 4.3 differentiation pass after the repeated spam rejection.
- Reframed snow as a map resource instead of a passive ammo counter:
  - Small Snow Patch, Big Snowdrift, and Icy Snowdrift are now snow resources.
  - The player gathers faster when stopping near a snow resource.
  - Snow resources visibly shrink as they are mined and disappear when depleted.
  - Depleted resources award Snow Coin.
  - Big Snowdrift depletion can trigger Big Snowball.
  - Icy Snowdrift depletion can trigger Ice Shot.
- Updated opening run setup so a Small Snow Patch appears near the player immediately.
- Added snow particle pull feedback from the active snow resource toward the player during gathering.
- Reworked enemies from person-like doodles into a snowman family:
  - Walker is a small teal-accent snowman.
  - Runner is a red-scarf snowman with fast trail cues.
  - Heavy is a larger purple-accent snowman.
- Reframed Snow Workshop as Snow Gear Lab with gear-slot upgrade names:
  - Snow Pouch, Wool Gloves, Throw Mitts, Packed Core, Warm Coat, Magnet Charm.
- Rewrote early mission copy around mining snow patches, stopping snowmen, big snowdrifts, red-scarf waves, and heavy snowmen.
- Updated App Review Notes to emphasize stop-to-gather map resources and snowdrift depletion.
- Rewrote Store Metadata Draft with more distinctive snowfield survival naming, captions, and preview plan.
- Selected `Stop & Snow` as the App Store/display name to reduce the generic functional feel of `Gather & Shot`.
- Updated Unity product name, WebGL shell title patching, and iOS build display-name configuration to use `Stop & Snow` while preserving the existing bundle identifier and internal project path.
- Rebuilt the player around the polished concept-icon direction with separate state sprites:
  - `player_idle.png`
  - `player_move.png`
  - `player_gather.png`
  - `player_throw.png`
  - `player_hit.png`
- Added in-world build-snowball feedback: while the player stops to gather, a snowball grows in front of the character's hands.
- Enlarged the runtime character scale and slightly tightened the 9:16 camera so the player/enemy silhouettes read more clearly in App Review screenshots and preview footage.
- Added named mission zones to the first five-minute route: Frost Yard, Snow Patch Field, Red Scarf Lane, Heavy Snowbank, and Blizzard Gate.
- Added Firebase context/event coverage for the current snow zone.
- Added the Build Snowball System as the signature 4.3 differentiation mechanic:
  - Small built snowballs appear after a short stop.
  - Packed snowballs require a longer stop and add damage/splash pressure.
  - Giant snowballs require a risky long stop and use big-snowball style area/piercing pressure.
  - Auto throw now prioritizes the current built snowball while preserving one-joystick controls.
- Added first-seen Runner/Heavy feedback and telemetry: `runner_first_seen`, `heavy_first_seen`.
- Added build-snowball telemetry: `snowball_build_start`, `snowball_build_stage`, `snowball_build_complete`, `snowball_auto_thrown`, and `giant_snowball_used`.
- Added zone objective telemetry: `zone_objective_complete`.
- Updated result stats to include Built, Packed, and Giant snowball counts.
- Added App Review finishing pass:
  - Opening objective now calls out building a Giant snowball.
  - Early auto throw holds for Packed snow when the player is not in immediate danger.
  - Packed/Giant build stage entry emits stronger burst and floating-text feedback.
  - Packed/Giant throws emit `PACKED THROW` or `GIANT THROW`.
  - Zone entry now shows a dedicated banner in addition to field tint/accent changes.
  - Snow Gear Lab slots now include simple gear icons and purchasable-slot pulse feedback.
- Applied the icon-style character art to gameplay:
  - Added separate 128x128 alpha player sprites for idle, gather, move down, move up, move side, throw, and hit.
  - Added direction-aware player movement art so vertical and side movement do not reuse a single diagonal run pose.
  - Replaced Walker, Runner, and Heavy with 128x128 alpha snowman enemy sprites based on the App Store icon family.
  - Tuned gathering ring/cloud/build-snowball scale and popup positions so the new gather sprite stays readable.
  - Tuned enemy visual scale so Runner reads smaller/faster, Walker reads basic, and Heavy reads larger without misleading contact range.
- Replaced the temporary AI-illustration-downscale gameplay sprites with purpose-built 128x128 game sprites:
  - Used the App Store icon art as reference only.
  - Rebuilt player states with fixed proportions, simple color blocks, consistent outline weight, and transparent alpha.
  - Rebuilt snowman enemies with the same simplified sprite language.
  - Rebuilt snowball, puff, small snowdrift, and big snowdrift support sprites to match the simplified gameplay style.
  - Added `game-sprite-sheet-clean-preview.png` as a quick art QA sheet for the generated gameplay sprites.

## First 10 Seconds

- Expected: three Walker enemies approach from playfield edges immediately.
- Expected: first input moves the player instantly.
- Expected: releasing input starts a visible gather ring and snow cloud.
- Expected: enemies entering range trigger automatic snowball throws without manual aim.
- Expected: first defeated enemy grants Snow Coin and logs `first_reward`.

## First 60 Seconds

- 0-10s: movement, stop-gather, first auto-fire, first kill, first Snow Coin.
- 10-20s: initial ammo can empty, logging `ammo_empty` once per run.
- 20-35s: big snowdrift appears and can trigger Big Snowball.
- 35-50s: first mini goal completes by 5 kills, 30s survival, or big snowdrift collection; reward is Snow Coin plus Rapid Throw.
- 50-60s: free upgrade is surfaced and auto-applies around 52s if still unclaimed.

## First 5 Minutes

- Run 1: first reward and first free upgrade are available.
- Run 2: upgraded gather speed/ammo capacity changes run feel.
- Run 3: result screen includes rewarded 2x Coin, Revive, and Bonus Chest hooks.
- Run 4: big snowdrift and weapon cache can produce Big/Split/Ice/Burst ad moments.
- Run 5: Runner/Heavy/mixed waves expose enemy differentiation and future boss pressure.

## Verification

- Passed on 2026-09-03: `./scripts/verify-gather-and-shot-mvp.sh`
  - Includes compile coverage for the App Review differentiation mission chain.
  - Includes checks for Small Snow Patch, Big Snowdrift, Icy Snowdrift, resource capacities, faster resource gathering, and depletion rewards.
  - Includes checks for the player state sprite files and Stop & Snow zone chain.
  - Includes checks for Small/Packed/Giant build-snowball stages, build timing order, upgrade scaling, and Giant damage bonus.
- Passed on 2026-09-03: `./scripts/verify-gather-and-shot-admob-readiness.sh`
- Passed with existing config warning on 2026-09-03: `./scripts/verify-gather-and-shot-firebase-readiness.sh`
  - Warning: Android Firebase config is still missing at `prototypes/gather-and-shot/Assets/google-services.json`.
- Passed on 2026-09-03: updated player, Walker, Runner, and Heavy sprites are 128x128 RGBA PNGs with alpha.
- Passed on 2026-09-07: updated player state sprites are 128x128 PNGs with alpha:
  - `player_idle.png`, `player_gather.png`, `player_move_down.png`, `player_move_up.png`, `player_move_side.png`, `player_move.png`, `player_throw.png`, `player_hit.png`.
- Passed on 2026-09-07: updated Walker, Runner, and Heavy sprites are 128x128 PNGs with alpha and use TextureImporter alpha settings.
- Passed on 2026-09-07: simplified gameplay sprite sheet preview generated at `Assets/_Project/Art/AppStore/Concepts/game-sprite-sheet-clean-preview.png`.
- Passed on 2026-09-07: `player.png` fallback was aligned to the new clean idle sprite so old fallback paths do not show the temporary illustration-downscale art.
- Passed on 2026-09-07: `./scripts/verify-gather-and-shot-mvp.sh`
  - Output: `Stop & Snow MVP compile verification passed.`
- Passed on 2026-09-07: `./scripts/verify-gather-and-shot-webgl.sh`
  - Output: `prototypes/gather-and-shot/Builds/WebGL/gather-and-shot`
- Passed on 2026-09-07: local WebGL server at `http://127.0.0.1:8091/index.html`
  - Confirmed `index.html` responds with HTTP 200.
- Passed on 2026-09-03: `./scripts/verify-gather-and-shot-webgl.sh`
  - Output: `prototypes/gather-and-shot/Builds/WebGL/gather-and-shot`
  - Log: `/tmp/gather-and-shot-unity-webgl-build.log`
- Previously passed: local WebGL server at `http://127.0.0.1:8091/`
  - Confirmed `index.html` responds with HTTP 200.
  - Confirmed `.wasm.gz` uses `Content-Type: application/wasm` and `Content-Encoding: gzip`.
  - Confirmed generated `index.html` uses a 540x960 canvas and hides Unity footer controls through `TemplateData/style.css`.
- Passed on 2026-09-03: `Assets/_Project/Art/AppStore/AppIcon-1024.png` is a 1024x1024 PNG.
- Not rerun in this pass: `./scripts/verify-gather-and-shot-ios-readiness.sh`
  - Previous iOS archive/distribution pass succeeded for build 1.0.1 (5).

## Remaining QA

- Open in Unity Editor and play 3 runs on a portrait Game view.
- Capture three 10-15s ad scenes:
  - Opening stop-gather and first auto throw.
  - Big Snowball after big snowdrift.
  - Weapon cache into Split/Ice/Burst screen clear.
- Refresh the running local WebGL page after layout changes.
- Add production rewarded ad calls when final rewarded ad unit IDs are ready.

## App Review 4.3 Differentiation QA

- Check first 10 seconds: stop-to-gather ring, snow cloud, packed snow ammo, first auto throw, and Snow Coin popup are visible without audio.
- Check first 15 seconds: first auto throw should be Packed when the player has enough space to keep building.
- Check first 10 seconds: player sprite changes between move, gather/build, and auto-throw states.
- Check first 10 seconds: player should visibly match the App Store icon hero: teal winter outfit, cream face, bold eyebrows, and hand-built snowball.
- Check directional movement: down, up, and side movement should use distinct sprites; left/right side movement should mirror naturally.
- Check gathering clarity: the hand-built snowball should visibly grow during stop-to-gather and should feel separate from the small orbiting ammo count.
- Check art/effect overlap: `+SNOW`, `GIANT`, `PACKED THROW`, and `AUTO THROW` popups should not cover the player's face during gather/throw.
- Check build stages: continuing to hold still should progress from Small to Packed to Giant before throwing.
- Check attack result: Packed/Giant built snowballs should produce visibly stronger impact than a quick Small snowball.
- Check first 10 seconds after latest pass: a snow patch appears near the player and stopping beside it gathers faster than open snow.
- Check first 30 seconds: current mission and mission completion feedback are visible.
- Check snow resource depletion: Small Snow Patch, Big Snowdrift, and Icy Snowdrift shrink as they are mined and disappear with a Snow Coin popup.
- Check Big Snowdrift/Icy Snowdrift rewards: depleted big resources visibly activate Big Snowball or Ice Shot.
- Check result flow: Snow Coin earned remains the primary result, and Upgrade opens the Snow Gear Lab instead of acting as a hidden one-click purchase.
- Check Snow Gear Lab: all six gear slots show level, next effect, and cost/free state.
- Check first 3 minutes: Walker, Runner, Heavy, Big Snowball, Split Snowball, Ice Shot, or Snow Burst are visually distinguishable in screenshots.
- Check snowman family readability: Walker, Runner, and Heavy should read as snowmen with different role accents, not generic humanoid enemies.
- Check enemy scale: Runner should look compact and fast, Walker should look like the basic enemy, and Heavy should look bigger without obscuring contact readability.
- Check zone readability: mission text should show Frost Yard, Snow Patch Field, Red Scarf Lane, Heavy Snowbank, or Blizzard Gate during progression.
- Check zone visuals: zone entry should change the field tint/accent lines and spawn zone-relevant pressure or resources.
- Check zone banner: Frost Yard should appear at run start, and later zones should appear through a dedicated banner.
- Check first enemy intros: Runner first appearance should show `RED SCARF RUNNER`; Heavy first appearance should show `HEAVY SNOWMAN`.
- Check Gear Lab visuals: every upgrade slot should show a gear icon, level, effect, and cost/free state without text overlap.
- Check metadata: use the App Review Notes and Store Metadata drafts to describe specific gameplay changes rather than generic bug fixes.
- Check icon: App Store icon should clearly show stop-to-gather risk, keep enemies readable as snowmen, and keep the player readable as a simple winter person rather than a black furry silhouette.
- Future art direction: prefer a human winter player versus snowman enemies. If in-game enemies are reskinned, make their bodies white snow shapes with colored role accents rather than black humanoid silhouettes.
