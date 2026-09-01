# Yacht Rush

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.23f1
- Platform: WebGL / mobile prototype
- Package name: com.mannlab.games.yachtrush
- Namespace: MannLab.Games.YachtRush

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.

## Services

- Firebase Analytics/Crashlytics runtime bridge is wired through `FirebaseTelemetry`.
- Add Yacht Rush's Firebase iOS config at `Assets/GoogleService-Info.plist`, or set `MANNLAB_YACHT_RUSH_FIREBASE_IOS_PLIST` before an iOS build.
- AdMob interstitials are initialized through `MannLabAdMob` at every game over. Test builds use Google test ads; release iOS builds use `ca-app-pub-4525914685149405~8143053169` and `ca-app-pub-4525914685149405/8278784535`.
- Set `MANNLAB_YACHT_RUSH_ADMOB_IOS_APP_ID` for a production iOS AdMob app ID before release builds.

## iOS Build Checks

```bash
scripts/verify-yacht-rush-ios-readiness.sh release
scripts/verify-yacht-rush-ios-readiness.sh crashlytics-test
scripts/verify-yacht-rush-ios-readiness.sh admob-test
```
