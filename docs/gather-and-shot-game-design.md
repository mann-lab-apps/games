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
- Contact damage drains Warmth and knocks the player away.
- The run ends when Warmth reaches zero.
- Score and best score are kill counts.

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

- Weapon levels
- Upgrade choices
- Additional enemy families
- Store metadata

## Firebase And Ads

The runtime initializes the shared Firebase Analytics/Crashlytics bridge and logs `app_open`, `run_start`, `restart`, `gather_start`, `bonus_pickup`, `run_end`, and Crashlytics test breadcrumbs. Crashlytics custom keys include score, best score, ammo, Warmth, elapsed seconds, enemy count, pickup count, game-over state, and current gathering state.

Crashlytics 확인용 development build는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. CLI 검증 시에는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 주면 앱 시작 직후 테스트 크래시가 발생한다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일된다.

AdMob is wired through the shared game-over interstitial bridge. Development/debug builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` builds use Google's sample test ads. Production iOS uses AdMob app ID `ca-app-pub-4525914685149405~6036634116` and game-over interstitial `ca-app-pub-4525914685149405/2541126713`; Android production IDs are still deferred.
