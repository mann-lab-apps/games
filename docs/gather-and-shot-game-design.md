# Gather & Shot

Gather & Shot is a mobile portrait snowball survival prototype.

## Core Loop

- Move with a virtual touch joystick.
- Collect snowballs, snowdrifts, and rare big snowdrifts to build ammo.
- When an enemy enters range, the player automatically throws one snowball at the nearest enemy.
- Each throw spends one snowball.
- Collecting snow briefly locks movement and auto-fire while the player gathers it.
- Defeated enemies add score.
- Contact damage drains Warmth and knocks the player away.
- The run ends when Warmth reaches zero.
- Score and best score are kill counts.

## Pickups

- Snowball: common pickup, +1 ammo.
- Snowdrift: medium pickup, +3 ammo and a longer gathering pause.
- Big snowdrift: rare large pickup, +5 ammo, the longest gathering pause, and often appears near enemies to create a simple risk-reward route choice.

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
- Store metadata

## Firebase And Ads

The runtime initializes the shared Firebase Analytics/Crashlytics bridge and logs `app_open`, `run_start`, `restart`, `gather_start`, `run_end`, and Crashlytics test breadcrumbs. Crashlytics custom keys include score, best score, ammo, Warmth, elapsed seconds, enemy count, pickup count, game-over state, and current gathering state.

Crashlytics 확인용 development build는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. CLI 검증 시에는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 주면 앱 시작 직후 테스트 크래시가 발생한다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일된다.

AdMob is wired through the shared game-over interstitial bridge. Development/debug builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` builds use Google's sample test ads; release builds need real Gather & Shot AdMob app/ad unit IDs before store submission.
