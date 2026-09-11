#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
manifest="$project/Packages/manifest.json"
lockfile="$project/Packages/packages-lock.json"
asmdef="$project/Assets/_Project/Scripts/OnePlusOneMinusOneGame.asmdef"
controller="$project/Assets/_Project/Scripts/OnePlusOneMinusOneController.cs"
telemetry="$project/Assets/_Project/Scripts/FirebaseTelemetry.cs"
ios_build="$project/Assets/_Project/Editor/BuildIosXcode.cs"
android_build="$project/Assets/_Project/Editor/BuildAndroidAab.cs"
gma_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
gma_linker="$project/Assets/GoogleMobileAds/link.xml"
crashlytics_settings="$project/Assets/Editor Default Resources/CrashlyticsSettings.asset"
firebase_plist="$project/Assets/GoogleService-Info.plist"
firebase_android_json="$project/Assets/google-services.json"
readme="$project/README.md"
failures=0
warnings=0

if command -v node >/dev/null 2>&1; then
  node "$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"
else
  echo "Warning: node is not available; skipping static round verification." >&2
  warnings=1
fi

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    failures=1
  fi
}

require_text() {
  local path="$1"
  local pattern="$2"
  require_file "$path"
  if [[ ! -f "$path" ]]; then
    return
  fi
  if ! grep -Fq -- "$pattern" "$path"; then
    echo "Missing expected text in $path: $pattern" >&2
    failures=1
  fi
}

warn_or_fail_missing_config() {
  local path="$1"
  if [[ -f "$path" ]]; then
    return
  fi

  if [[ "${REQUIRE_FIREBASE_CONFIG:-0}" == "1" ]]; then
    echo "Missing Firebase config: $path" >&2
    failures=1
    return
  fi

  echo "Warning: Firebase config not present yet: $path" >&2
  warnings=1
}

warn_or_fail_missing_production_ad_unit_env() {
  local name="$1"
  local env_name="$2"
  if [[ -n "${!env_name:-}" ]]; then
    return
  fi

  if [[ "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}" == "1" ]]; then
    echo "Missing production AdMob ad unit env: $name ($env_name)" >&2
    failures=1
    return
  fi

  echo "Warning: production AdMob ad unit env is not set: $name ($env_name)" >&2
  warnings=1
}

warn_or_fail_missing_production_admob_env() {
  local name="$1"
  local env_name="$2"
  if [[ -n "${!env_name:-}" ]]; then
    return
  fi

  if [[ "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}" == "1" ]]; then
    echo "Missing production AdMob env: $name ($env_name)" >&2
    failures=1
    return
  fi

  echo "Warning: production AdMob env is not set: $name ($env_name)" >&2
  warnings=1
}

warn_or_fail_test_production_ad_unit_env() {
  local name="$1"
  local env_name="$2"
  local test_value="$3"
  if [[ "${!env_name:-}" != "$test_value" ]]; then
    return
  fi

  if [[ "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}" == "1" ]]; then
    echo "Production AdMob ad unit env uses Google's test ID: $name ($env_name)" >&2
    failures=1
    return
  fi

  echo "Warning: production AdMob ad unit env uses Google's test ID: $name ($env_name)" >&2
  warnings=1
}

warn_or_fail_placeholder_production_admob_env() {
  local name="$1"
  local env_name="$2"
  local value="${!env_name:-}"
  if [[ "$value" != *XXXX* && "$value" != *replace* && "$value" != *REPLACE* ]]; then
    return
  fi

  if [[ "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}" == "1" ]]; then
    echo "Production AdMob env uses a placeholder: $name ($env_name)" >&2
    failures=1
    return
  fi

  echo "Warning: production AdMob env uses a placeholder: $name ($env_name)" >&2
  warnings=1
}

require_text "$manifest" "\"com.mannlab.firebase-unity-sdk\""
require_text "$manifest" "\"com.mannlab.admob-core\""
require_text "$manifest" "\"https://package.openupm.com\""
require_text "$lockfile" "\"com.mannlab.firebase-unity-sdk\""
require_text "$asmdef" "\"MannLab.Ads.Core\""
require_text "$telemetry" "public static void SetContext"
require_text "$telemetry" "public static void ForceCrashForTesting"
require_text "$telemetry" "Crashlytics forced test crash requested."
require_text "$controller" "FirebaseTelemetry.SetContext(\"game\", GameIdentifier)"
require_text "$controller" "FirebaseTelemetry.LogEvent(\"app_open\", AppOpenParameters())"
require_text "$controller" "\"highest_unlocked_round\""
require_text "$controller" "\"is_replay\""
require_text "$controller" "\"round_name\""
require_text "$controller" "\"stick_count\""
require_text "$controller" "\"slots\""
require_text "$controller" "\"app_version\""
require_text "$controller" "\"platform\""
require_text "$controller" "FirebaseTelemetry.LogEvent(\"round_clear\""
require_text "$controller" "\"expression\""
require_text "$controller" "\"failure_count\""
require_text "$controller" "FirebaseTelemetry.ForceCrashForTesting"
require_text "$controller" "MannLabAdMob.InitializeGameOverInterstitial"
require_text "$controller" "MannLabAdMob.TryShowGameOverInterstitial"
require_text "$controller" "ReleaseAdMobConfigResourceName"
require_text "$controller" "GetConfiguredAdUnitId"
require_text "$controller" "ReleaseRoundClearInterstitialInterval = 10"
require_text "$controller" "ReleaseInterstitialGraceRoundCount = 5"
require_text "$controller" "ReleaseInterstitialMaxFailuresBeforeSkip = 3"
require_text "$controller" "FirebaseTelemetry.LogEvent(\"ad_interstitial_opportunity\""
require_text "$controller" "\"cadence\""
require_text "$controller" "\"will_show\""
require_text "$ios_build" "GADApplicationIdentifier"
require_text "$ios_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID"
require_text "$ios_build" "must not use Google's test app ID for iOS release builds"
require_text "$ios_build" "must not use Google's test interstitial ad unit ID for iOS release builds"
require_text "$ios_build" "not a placeholder"
require_text "$ios_build" "BuildCrashlyticsTest"
require_text "$ios_build" "BuildAdMobTest"
require_text "$android_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID"
require_text "$android_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID"
require_text "$android_build" "must not use Google's test app ID for Android release builds"
require_text "$android_build" "must not use Google's test interstitial ad unit ID for Android release builds"
require_text "$android_build" "not a placeholder"
require_text "$android_build" "BuildAdMobTestApk"
require_text "$gma_settings" "adMobIOSAppId: ca-app-pub-3940256099942544~1458002511"
require_text "$gma_settings" "adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713"
require_text "$gma_linker" "GoogleMobileAds.Ump"
require_text "$crashlytics_settings" "CrashlyticsSettings"
require_text "$readme" "Firebase / Crashlytics"
require_text "$readme" "AdMob"
require_text "$readme" "Release Checklist"

warn_or_fail_missing_config "$firebase_plist"
warn_or_fail_missing_config "$firebase_android_json"
warn_or_fail_missing_production_admob_env "iOS app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID"
warn_or_fail_missing_production_admob_env "Android app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID"
warn_or_fail_missing_production_ad_unit_env "iOS interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID"
warn_or_fail_missing_production_ad_unit_env "Android interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID"
warn_or_fail_test_production_ad_unit_env "iOS app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID" "ca-app-pub-3940256099942544~1458002511"
warn_or_fail_test_production_ad_unit_env "Android app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID" "ca-app-pub-3940256099942544~3347511713"
warn_or_fail_test_production_ad_unit_env "iOS interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID" "ca-app-pub-3940256099942544/4411468910"
warn_or_fail_test_production_ad_unit_env "Android interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID" "ca-app-pub-3940256099942544/1033173712"
warn_or_fail_placeholder_production_admob_env "iOS app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID"
warn_or_fail_placeholder_production_admob_env "Android app ID" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID"
warn_or_fail_placeholder_production_admob_env "iOS interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID"
warn_or_fail_placeholder_production_admob_env "Android interstitial" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID"

if [[ -f "$firebase_plist" ]]; then
  require_text "$firebase_plist" "<string>com.mannlab.games.oneplusoneminusone</string>"
fi

if [[ -f "$firebase_android_json" ]]; then
  require_text "$firebase_android_json" "\"package_name\": \"com.mannlab.games.oneplusoneminusone\""
fi

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 AdMob/Crashlytics readiness check failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "1 = 1 AdMob/Crashlytics code readiness verified; release config values are still needed."
else
  echo "1 = 1 AdMob/Crashlytics readiness verified."
fi
