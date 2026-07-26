# Android Release Baseline

Use this as the minimum checklist before promoting a Unity game from `prototypes/` to `releases/`.

## Current Store Baseline

As of July 26, 2026:

- Unity recommends LTS releases for projects close to production lock.
- Unity 6.3 LTS is supported until December 2027.
- Starting August 31, 2026, new Google Play apps and updates must target Android 16, API level 36, or higher.

References:

- Unity LTS support: https://unity.com/releases/unity-6/support
- Google Play target API requirement: https://developer.android.com/google/play/requirements/target-sdk

## Build Settings

- Platform: Android
- Build output for store: Android App Bundle, `.aab`
- Scripting backend: IL2CPP
- Target architecture: ARM64
- Minimum API: choose based on device coverage after prototype testing
- Target API: highest installed, and compliant with current Google Play requirement
- Versioning: increment both app version and bundle version code for every uploaded build

## Release Readiness

- App signing key strategy documented
- Package name reserved, for example `com.mannlab.games.stackjump`
- Privacy policy URL prepared
- Data safety answers drafted
- Ads, analytics, attribution, and consent SDK behavior reviewed
- Play Console internal testing track build uploaded
- Crash-free smoke test completed on at least one real Android device

