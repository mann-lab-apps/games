# Stop & Snow

Mobile portrait snowfield survival prototype built around a stop-to-gather resource loop.

## Project

- Unity editor: 6000.3.23f1
- Primary prototype platform: mobile portrait
- Package name: com.mannlab.games.gatherandshot
- Namespace: MannLab.Games.GatherAndShot

## Core Loop

- Move with a virtual joystick.
- Release touch and stand still to build a visible snowball.
- Holding still grows the built snowball through Small, Packed, and Giant stages.
- Standing near Small Snow Patches, Big Snowdrifts, or Icy Snowdrifts gathers faster.
- Snow resources visibly shrink as they are mined and disappear when depleted.
- Gathering snow stops movement; once a built snowball is ready, auto-throw can still fire when an enemy enters range.
- Rare snowballs and weapon caches still act as emergency bonuses.
- Automatically throw snowballs at the nearest enemy in range.
- Each hit can defeat or damage enemies.
- Defeated enemies add score and Snow Coin.
- Enemy contact drains Warmth and knocks the player back.
- The run ends when Warmth reaches zero, then Snow Coin connects into upgrades and the next run.

## Casual Growth Loop

- First 10 seconds: three Walker enemies enter from screen edges; moving gives immediate escape, stopping starts the gather ring, and auto-fire/first coin reward can happen before the opening pressure closes.
- First 60 seconds: enemy kills, ammo shortage, a timed big snowdrift, a weapon cache, first mini-goal reward, and the first free upgrade are all surfaced.
- Persistent Snow Coin is saved in PlayerPrefs and shown as run coins plus owned coins.
- Six gear upgrades are saved persistently: Snow Pouch, Wool Gloves, Throw Mitts, Packed Core, Warm Coat, and Magnet Charm.
- Weapon growth keeps auto-fire intact: Big Snowball, Split Snowball, Ice Shot, Snow Burst, and Rapid Throw appear through pickups, mini goals, or upgrade progression.
- Wave staging is time-based: Walker focus before 60s, Runner intro from 60s, Heavy intro from 120s, mixed pressure after 240s.
- Result screen prioritizes Snow Coin earned, upgrade availability, rewarded 2x Coin, Revive, Bonus Chest, and Next Run.
- Forced game-over interstitials stay blocked for the first 3 runs and the first 3 minutes.
- First-route zones are visible in mission text: Frost Yard, Snow Patch Field, Red Scarf Lane, Heavy Snowbank, and Blizzard Gate.
- Zone entry shows a dedicated banner plus field tint/accent changes.
- Player art includes separate idle, move, gather/build, auto-throw, and hit poses.
- Snow Gear Lab uses six equipment slots with simple gear icons and purchasable-slot pulse feedback.

## Gathering And Snow Resources

- Stationary gather: +1 ammo per completed stillness cycle.
- Touching again cancels gathering immediately.
- While gathering, the player switches to a build pose and a hand-built snowball grows in front of the character.
- Small/Packed/Giant built snowballs change projectile size, damage, splash, and piercing pressure.
- Small Snow Patch: faster local gather resource that depletes after a few gathers.
- Big Snowdrift: larger resource that can activate Big Snowball when depleted.
- Icy Snowdrift: icy resource that can activate Ice Shot when depleted.
- Snowball bonus: +2 emergency ammo.

## Build

Generate character and App Store icon assets:

```sh
python3 scripts/generate-gather-and-shot-doodle-assets.py
```

Generate the scene from Unity:

```sh
/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath prototypes/gather-and-shot \
  -executeMethod MannLab.Games.GatherAndShot.EditorTools.CreateGameScene.Create
```

Run the lightweight local verification:

```sh
./scripts/verify-gather-and-shot-mvp.sh
```

Firebase/Crashlytics readiness:

```sh
./scripts/verify-gather-and-shot-firebase-readiness.sh
```

AdMob readiness:

```sh
./scripts/verify-gather-and-shot-admob-readiness.sh
```

iOS Xcode export readiness:

```sh
./scripts/generate-gather-and-shot-doodle-assets.py
./scripts/verify-gather-and-shot-ios-readiness.sh
./scripts/verify-gather-and-shot-ios-readiness.sh crashlytics-test
./scripts/verify-gather-and-shot-ios-readiness.sh admob-test
```

Capture App Store screenshots from the WebGL build:

```sh
node scripts/capture-gather-and-shot-webgl-app-store-assets.mjs
```

Serve the local WebGL build:

```sh
./scripts/serve-gather-and-shot-webgl.sh
```

Open `http://127.0.0.1:8091/`. The generated WebGL shell is patched during build to use a portrait 9:16 canvas and hide Unity's default footer controls.

## Firebase Notes

The runtime calls `FirebaseTelemetry` for `app_open`, `run_start`, `restart`, `first_action`, `first_reward`, `first_upgrade`, `currency_earned`, `upgrade_purchase`, `weapon_unlocked`, `wave_start`, `zone_enter`, `zone_objective_complete`, `runner_first_seen`, `heavy_first_seen`, `snowball_build_start`, `snowball_build_stage`, `snowball_build_complete`, `snowball_auto_thrown`, `giant_snowball_used`, `enemy_defeated`, `ammo_empty`, `gather_start`, `gather_complete`, `bonus_pickup`, `rewarded_offer_shown`, `rewarded_offer_completed`, `run_end`, `run_end_reason`, and `crashlytics_test_trigger` breadcrumbs. It also forwards unhandled exceptions and Unity exception logs to Crashlytics when the Firebase Unity SDK is present.

When the Firebase Analytics SDK exposes the string parameter overload, telemetry forwards common run parameters such as game, app name, run number, session time, survival time, kills, ammo, Warmth, coins, upgrade levels, current weapon, current snow zone, snowball size, build time, enemy count, and end reason.

Firebase app config must be added per platform before real Crashlytics testing:

- iOS: `Assets/GoogleService-Info.plist`
- Android: `Assets/google-services.json`

The Crashlytics test trigger is compiled only for Unity Editor or development builds. Tap the top-left corner 7 times within 2.5 seconds, or launch with `--mannlab-force-crashlytics-test` / `MANNLAB_FORCE_CRASHLYTICS_TEST=1`, then reopen the app so Crashlytics can upload the report.

## AdMob Notes

AdMob uses the shared game-over interstitial bridge. Debug/development builds use Google's test interstitial IDs through the bridge, and `MANNLAB_ADMOB_FORCE_TEST_ADS` forces every game over to request a test interstitial.

The Google Mobile Ads settings asset uses the production iOS app ID and Google's sample Android app ID until Android release setup is ready. Debug/development builds and `MANNLAB_ADMOB_FORCE_TEST_ADS` still force Google's test interstitial IDs.

- Android AdMob App ID: `ca-app-pub-3940256099942544~3347511713`
- iOS AdMob App ID: `ca-app-pub-4525914685149405~6036634116`
- Production Android interstitial: set `ProductionAndroidInterstitialAdUnitId` in `GatherAndShotController`
- Production iOS interstitial: `ca-app-pub-4525914685149405/2541126713`
- iOS release export App ID override: set `MANNLAB_GATHER_AND_SHOT_ADMOB_IOS_APP_ID`

## iOS Notes

The generated app icon is `Assets/_Project/Art/AppStore/AppIcon-1024.png`. The iOS export script copies it into the Xcode AppIcon asset catalog as the marketing icon and uses it for Unity's iOS application icons.

Default iOS versioning is `0.1 (1)`. Override with `MANNLAB_GATHER_AND_SHOT_IOS_MARKETING_VERSION` and `MANNLAB_GATHER_AND_SHOT_IOS_BUILD_NUMBER` before exporting a store build. The default App Store provisioning profile specifier is `Gather And Shot`; override with `MANNLAB_GATHER_AND_SHOT_IOS_APP_STORE_PROFILE_SPECIFIER` if Apple Developer uses a different profile name. AdMob/CocoaPods exports should be archived from `Builds/iOS/Xcode/Unity-iPhone.xcworkspace`.

## Deferred

Selectable upgrade shop layout, production rewarded ad SDK calls, deeper bosses/regions, and store metadata remain deferred after this first growth-loop pass.
