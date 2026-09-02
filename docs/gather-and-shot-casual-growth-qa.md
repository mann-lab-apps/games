# Gather & Shot Casual Growth QA

Date: 2026-09-01

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
- Added a Snow Workshop screen after the result screen with all six upgrade tracks, current level, next-level effect, cost/free state, and purchase feedback.
- Added submission support drafts: `docs/gather-and-shot-app-review-notes.md` and `docs/gather-and-shot-store-metadata-draft.md`.
- Replaced the 1024x1024 App Store icon with an in-game-tone stop-to-gather scene: a human winter player gathers snow inside a blue ring while snowman Walker/Runner/Heavy-style enemies close in, using rough low-fi doodle shapes rather than polished mascot art.
- Preserved the earlier polished/cuter icon concept at `Assets/_Project/Art/AppStore/Concepts/AppIcon-polished-snow-survival-concept-1024.png` for a possible larger-scope sequel, higher-production reskin, or future store experiment.
- Reskinned in-game character art for icon/game consistency:
  - `Assets/Resources/GatherAndShot/player.png`: human winter player with navy/teal clothing and reduced black-fill silhouette.
  - `Assets/Resources/GatherAndShot/walker.png`: small teal-accent snowman enemy.
  - `Assets/Resources/GatherAndShot/runner.png`: magenta-accent running snowman enemy.
  - `Assets/Resources/GatherAndShot/heavy.png`: large purple-accent heavy snowman enemy.
- Disabled runtime enemy sprite tinting so the white snowman bodies and role-color accessories remain visible in game.

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

- Passed: `./scripts/verify-gather-and-shot-mvp.sh`
  - Includes compile coverage for the App Review differentiation mission chain.
- Passed: `./scripts/verify-gather-and-shot-admob-readiness.sh`
- Passed with existing config warning: `./scripts/verify-gather-and-shot-firebase-readiness.sh`
  - Warning: Android Firebase config is still missing at `prototypes/gather-and-shot/Assets/google-services.json`.
- Passed: updated player, Walker, Runner, and Heavy sprites are 128x128 RGBA PNGs with alpha.
- Passed after clearing stale Unity Licensing Client processes: `./scripts/verify-gather-and-shot-webgl.sh`
  - Output: `prototypes/gather-and-shot/Builds/WebGL/gather-and-shot`
  - Log: `/tmp/gather-and-shot-unity-webgl-build.log`
- Passed: local WebGL server at `http://127.0.0.1:8091/`
  - Confirmed `index.html` responds with HTTP 200.
  - Confirmed `.wasm.gz` uses `Content-Type: application/wasm` and `Content-Encoding: gzip`.
  - Confirmed generated `index.html` uses a 540x960 canvas and hides Unity footer controls through `TemplateData/style.css`.
- Passed: `Assets/_Project/Art/AppStore/AppIcon-1024.png` is a 1024x1024 RGB PNG.
- Blocked: `./scripts/verify-gather-and-shot-ios-readiness.sh`
  - Unity iOS export was terminated after Unity Licensing Client handshake errors, including unsupported protocol version `1.18.3`.
  - The failure occurred before the Xcode project icon copy could be verified.

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
- Check first 30 seconds: current mission and mission completion feedback are visible.
- Check result flow: Snow Coin earned remains the primary result, and Upgrade opens the Snow Workshop instead of acting as a hidden one-click purchase.
- Check Snow Workshop: all six upgrade tracks show level, next effect, and cost/free state.
- Check first 3 minutes: Walker, Runner, Heavy, Big Snowball, Split Snowball, Ice Shot, or Snow Burst are visually distinguishable in screenshots.
- Check metadata: use the App Review Notes and Store Metadata drafts to describe specific gameplay changes rather than generic bug fixes.
- Check icon: App Store icon should clearly show stop-to-gather risk, keep enemies readable as snowmen, and keep the player readable as a simple winter person rather than a black furry silhouette.
- Future art direction: prefer a human winter player versus snowman enemies. If in-game enemies are reskinned, make their bodies white snow shapes with colored role accents rather than black humanoid silhouettes.
