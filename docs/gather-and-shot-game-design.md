# Gather & Shot

Gather & Shot is a mobile portrait snowball survival prototype built around a stop-to-reload risk loop.

## Core Loop

- Move with a virtual touch joystick.
- Release the touch and stand still to gather snow into ammo.
- Gathering locks movement and auto-fire, so every reload is a timing risk.
- When an enemy enters range, the player automatically throws one snowball at the nearest enemy.
- Each throw spends one snowball.
- Rare snowballs, snowdrifts, and big snowdrifts remain as emergency bonus refills, not the main ammo source.
- Defeated enemies add score.
- Defeated enemies also grant Snow Coin for persistent upgrades.
- Contact damage drains Warmth and knocks the player away.
- The run ends when Warmth reaches zero.
- Score and best score are kill counts.

## Growth Loop Upgrade Pass

This pass reframes the prototype as a growth survival casual game while preserving the stop-to-reload risk loop.

- Snow Coin is awarded from enemy defeats, survival/wave completion, bonus pickups, mini goals, and rewarded test hooks.
- The HUD shows run-earned Snow Coin and owned Snow Coin separately.
- The result screen prioritizes `Snow Coin earned`, then kills, best kills, survival time, ammo gathered, pickup count, and weapon defeat split.
- Persistent upgrades:
  - Ammo Capacity: increases max ammo and starting ammo.
  - Gather Speed: shortens stationary gather cycles.
  - Throw Rate: reduces auto-fire cooldown.
  - Snowball Damage: increases projectile damage and can make Ice Shot the upgraded default later.
  - Warm Coat: increases max Warmth and reduces contact damage.
  - Coin Magnet: increases pickup collection radius.
- The first free upgrade is forced through the early loop: a mini goal unlocks it, it auto-applies around 52 seconds if the run is still active, and it remains free/highlighted on the first result screen otherwise.

## First 60 Seconds

- 0-10s: three Walker enemies enter from the edges; movement, stop-gather, first auto-fire, and first Snow Coin reward are visible.
- 10-20s: opening ammo can empty after the first throws, making the stop-to-gather risk clear.
- 20-35s: a timed big snowdrift appears and activates Big Snowball when collected.
- 35-50s: the first mini goal completes through 5 kills, 30 seconds survived, or big snowdrift collection; this grants Snow Coin and Rapid Throw.
- 50-60s: the first free upgrade is surfaced or auto-applied.

## First Five Minutes

- Run 1: basic movement, first kill, first Snow Coin, first free upgrade.
- Run 2: upgrade effects are felt through faster gathering or higher capacity; Runner wave begins after 60 seconds.
- Run 3: rewarded test hooks are natural on the result screen through 2x Coin, Revive, and Bonus Chest.
- Run 4: big snowdrift and weapon cache pickups can create Big/Split/Ice/Burst weapon footage.
- Run 5: Heavy/mixed waves foreshadow boss-like pressure and future region/challenge expansion.

## Gathering And Bonuses

- Stationary gather: after releasing movement and staying still briefly, the player gathers +1 snow at a time.
- Touching again cancels gathering immediately and returns to movement.
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

The prototype follows the Standing sketch direction: warm paper, readable silhouettes, dark ink outlines, loose hand-drawn wobble, soft blue snow shadows, and restrained HUD panels. Gathering uses a small snow-cloud doodle and progress mark around the player so the reload risk reads without a tutorial screen.

## Deferred

- Selectable upgrade shop UI
- Production rewarded ad SDK calls
- Additional enemy families
- New areas, bosses, and long-form mission chains
- Store metadata

## Firebase And Ads

The runtime initializes the shared Firebase Analytics/Crashlytics bridge and logs `app_open`, `run_start`, `restart`, `first_action`, `first_reward`, `first_upgrade`, `currency_earned`, `upgrade_purchase`, `weapon_unlocked`, `wave_start`, `enemy_defeated`, `ammo_empty`, `gather_start`, `gather_complete`, `bonus_pickup`, `rewarded_offer_shown`, `rewarded_offer_completed`, `run_end`, `run_end_reason`, and Crashlytics test breadcrumbs. Crashlytics custom keys include score, best score, ammo, max ammo, Warmth, elapsed seconds, run number, coins, upgrade levels, current weapon, enemy count, pickup count, game-over state, and current gathering state.

Crashlytics 확인용 development build는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. CLI 검증 시에는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 주면 앱 시작 직후 테스트 크래시가 발생한다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일된다.

AdMob is wired through the shared game-over interstitial bridge. Development/debug builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` builds use Google's sample test ads. Production iOS uses AdMob app ID `ca-app-pub-4525914685149405~6036634116` and game-over interstitial `ca-app-pub-4525914685149405/2541126713`; Android production IDs are still deferred.
