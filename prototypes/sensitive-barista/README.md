# Too Picky Coffee

Unity hyper-casual prototype.

## Project

- Unity editor: 6000.3.23f1
- Platform: WebGL / mobile prototype
- Package name: com.mannlab.games.sensitivebarista
- Namespace: MannLab.Games.SensitiveBarista

## First Open

Open this directory from Unity Hub. The playable scene is at `Assets/_Project/Scenes/Game.unity`.

## Concept

Make one drink per order by pouring into a single cup. The customer gives a menu name with a too-picky adjustment, while the scoring system compares ingredient volume against target ratios across a 10-drink run.

## MVP Features

- Runtime-built counter, clean 2D cup, liquid layers, and floating ice pieces.
- Tap units for shot and syrup; hold flow for ice, water, and milk.
- 10-round high-score run with readable round results and cumulative score.
- 100+ generated order variants built from base cafe recipes and sensitive adjustment requests.
- Recipe card with menu-specific ratio hints.
- Generated WebGL icon and browser title for `Too Picky Coffee`.
- EditMode tests for scoring rules.

## Telemetry / Ads

- Firebase Analytics/Crashlytics telemetry is wired through `Assets/_Project/Scripts/FirebaseTelemetry.cs`.
- AdMob interstitial setup is wired through `MannLabAdMob` and is triggered after a completed 10-drink run.
- Firebase iOS config is checked in at `Assets/GoogleService-Info.plist` for bundle ID `com.mannlab.games.toopickycoffee`.
- The App Store provisioning profile is stored at `BuildSettings/iOS/ProvisioningProfiles/Too_Picky_Coffee.mobileprovision`.
- Release iOS builds read the AdMob app ID from `MANNLAB_TOO_PICKY_COFFEE_ADMOB_IOS_APP_ID`.
- `admob-test` iOS builds use Google's sample AdMob app ID and force test ads with `MANNLAB_ADMOB_FORCE_TEST_ADS`.
- Production interstitial ad unit IDs still need to replace the empty runtime constants in `SensitiveBaristaController.cs` after the AdMob app is created.
