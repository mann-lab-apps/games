# Stop & Snow

Stop & Snow is a mobile portrait snowfield survival prototype built around a stop-to-gather resource loop.

## Core Loop

- Move with a virtual touch joystick.
- Release the touch and stand still to build a visible snowball.
- Holding still grows the built snowball through Small, Packed, and Giant stages.
- Standing near Small Snow Patches, Big Snowdrifts, or Icy Snowdrifts gathers faster.
- Snow resources visibly shrink as they are mined and disappear when depleted.
- Gathering locks movement, but a completed built snowball can still auto-throw, so every longer build is a timing risk.
- When an enemy enters range, the player automatically throws the current built snowball or one stored snowball at the nearest enemy.
- Each throw spends one snowball.
- Rare snowballs and weapon caches remain as emergency bonuses, not the main ammo source.
- Defeated enemies add score.
- Defeated enemies also grant Snow Coin for persistent upgrades.
- Contact damage drains Warmth and knocks the player away.
- The run ends when Warmth reaches zero.
- Score and best score are kill counts.

## Growth Loop Upgrade Pass

This pass reframes the prototype as a growth survival casual game while preserving the stop-to-reload risk loop.

- Snow Coin is awarded from enemy defeats, snow resource depletion, survival/wave completion, bonus pickups, mini goals, and rewarded test hooks.
- The HUD shows run-earned Snow Coin and owned Snow Coin separately.
- The result screen prioritizes `Snow Coin earned`, then kills, best kills, survival time, ammo gathered, pickup count, and weapon defeat split.
- Persistent upgrades:
  - Snow Pouch: increases max ammo and starting ammo.
  - Wool Gloves: shortens stationary gather cycles.
  - Throw Mitts: reduces auto-fire cooldown.
  - Packed Core: increases projectile damage and can make Ice Shot the upgraded default later.
  - Warm Coat: increases max Warmth and reduces contact damage.
  - Coin Magnet: increases pickup collection radius.
- The first free upgrade is forced through the early loop: a mini goal unlocks it, it auto-applies around 52 seconds if the run is still active, and it remains free/highlighted on the first result screen otherwise.

## First 60 Seconds

- 0-10s: Frost Yard banner appears, three Walker enemies enter from the edges, and the objective pushes the player to stop and build a Giant snowball.
- 10-20s: opening ammo can empty after the first throws, making the stop-to-gather risk clear.
- 20-35s: a timed Big Snowdrift appears and activates Big Snowball when depleted.
- 35-50s: the first mini goal completes through 5 kills, 30 seconds survived, or big snowdrift collection; this grants Snow Coin and Rapid Throw.
- 50-60s: the first free upgrade is surfaced or auto-applied.

## First Five Minutes

- Run 1: basic movement, first kill, first Snow Coin, first free upgrade.
- Run 2: upgrade effects are felt through faster gathering or higher capacity; Runner wave begins after 60 seconds.
- Run 3: rewarded test hooks are natural on the result screen through 2x Coin, Revive, and Bonus Chest.
- Run 4: big snowdrift and weapon cache pickups can create Big/Split/Ice/Burst weapon footage.
- Run 5: Heavy/mixed waves foreshadow boss-like pressure and future region/challenge expansion.

The first mission route uses named snowfield zones so progression is visible even in short App Review sessions:

- Frost Yard: learn movement, stop-to-gather, and auto throw.
- Snow Patch Field: gather enough fresh snow and mine larger resources.
- Red Scarf Lane: introduce faster runner snowmen.
- Heavy Snowbank: introduce large multi-hit snowmen.
- Blizzard Gate: mixed pressure and future boss/challenge space.

Zone entry uses a dedicated banner, zone-specific tint/accent lines, and a small arrival spawn/resource beat so the route is readable without a loading screen.

## Gathering And Bonuses

- Stationary gather: after releasing movement and staying still briefly, the player gathers +1 snow at a time.
- Touching again cancels gathering immediately and returns to movement.
- Build Snowball System:
  - Small: quick basic snowball.
  - Packed: longer stop, higher damage and small splash.
  - Giant: risky long stop, big-snowball scale with area/piercing pressure.
- Snowball bonus: rare emergency refill, +2 ammo.
- Snowdrift bonus: rarer refill, +4 ammo.
- Big snowdrift bonus: rare large refill, +6 ammo, often placed near enemy pressure.
- The intended decision is whether to keep moving safely or stop long enough to reload before the enemy wave closes in.

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

The prototype now follows the polished Stop & Snow icon direction: a human winter player with teal gear, oversized mittens, visible snowball-building poses, and a white snowman enemy family with role-color accessories. The player has separate idle, move, gather/build, auto-throw, and hit sprites. Gathering uses a blue ring, snow cloud, particle pull from active resources, packed ammo snowballs, and a hand-built snowball that grows from Small to Packed to Giant in front of the character so the stop-to-build risk reads without a tutorial screen.

Snow Gear Lab uses six equipment slots with simple gear icons, level/cost/effect text, and purchasable-slot pulse feedback.

## Deferred

- Production rewarded ad SDK calls
- Additional enemy families
- Bosses and long-form mission chains beyond Blizzard Gate

## Firebase And Ads

The runtime initializes the shared Firebase Analytics/Crashlytics bridge and logs `app_open`, `run_start`, `restart`, `first_action`, `first_reward`, `first_upgrade`, `currency_earned`, `upgrade_purchase`, `weapon_unlocked`, `wave_start`, `zone_enter`, `zone_objective_complete`, `runner_first_seen`, `heavy_first_seen`, `snowball_build_start`, `snowball_build_stage`, `snowball_build_complete`, `snowball_auto_thrown`, `giant_snowball_used`, `enemy_defeated`, `ammo_empty`, `gather_start`, `gather_complete`, `bonus_pickup`, `rewarded_offer_shown`, `rewarded_offer_completed`, `run_end`, `run_end_reason`, and Crashlytics test breadcrumbs. Crashlytics custom keys include score, best score, ammo, max ammo, Warmth, elapsed seconds, run number, coins, upgrade levels, current weapon, current snow zone, snowball size, build time, enemy count, pickup count, game-over state, and current gathering state.

Crashlytics 확인용 development build는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. CLI 검증 시에는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 주면 앱 시작 직후 테스트 크래시가 발생한다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일된다.

AdMob is wired through the shared game-over interstitial bridge. Development/debug builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` builds use Google's sample test ads. Production iOS uses AdMob app ID `ca-app-pub-4525914685149405~6036634116` and game-over interstitial `ca-app-pub-4525914685149405/2541126713`; Android production IDs are still deferred.
