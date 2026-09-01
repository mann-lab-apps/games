#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/walking"
manifest="$project/Packages/manifest.json"
lockfile="$project/Packages/packages-lock.json"
asmdef="$project/Assets/_Project/Scripts/WalkingGame.asmdef"
controller="$project/Assets/_Project/Scripts/WalkingController.cs"
telemetry="$project/Assets/_Project/Scripts/FirebaseTelemetry.cs"
ios_build="$project/Assets/_Project/Editor/BuildIosXcode.cs"
gma_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
gma_linker="$project/Assets/GoogleMobileAds/link.xml"
crashlytics_settings="$project/Assets/Editor Default Resources/CrashlyticsSettings.asset"
admob_package="$repo_root/shared/unity-packages/com.mannlab.admob-core/package.json"
admob_bridge="$repo_root/shared/unity-packages/com.mannlab.admob-core/Runtime/MannLabAdMob.cs"
firebase_package="$repo_root/shared/unity-packages/com.mannlab.firebase-unity-sdk/package.json"
privacy="$repo_root/web/mannlab-games/src/main.jsx"
readme="$project/README.md"
ios_plist="$project/Assets/GoogleService-Info.plist"
android_json="$project/Assets/google-services.json"
failures=0
warnings=0

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

require_text "$manifest" "\"com.mannlab.firebase-unity-sdk\""
require_text "$manifest" "\"com.mannlab.admob-core\""
require_text "$manifest" "\"https://package.openupm.com\""
require_text "$lockfile" "\"com.mannlab.firebase-unity-sdk\""
require_text "$lockfile" "\"com.mannlab.admob-core\""
require_text "$lockfile" "\"com.google.ads.mobile\""
require_text "$lockfile" "\"com.google.external-dependency-manager\""
require_text "$asmdef" "\"MannLab.Ads.Core\""
require_text "$firebase_package" "\"com.mannlab.firebase-unity-sdk\""
require_text "$admob_package" "\"com.mannlab.admob-core\""
require_text "$admob_package" "\"com.google.ads.mobile\": \"11.4.0\""
require_text "$admob_bridge" "public static class MannLabAdMob"
require_text "$admob_bridge" "AndroidInterstitialTestAdUnitId"
require_text "$admob_bridge" "IosInterstitialTestAdUnitId"
require_text "$telemetry" "public static void SetContext"
require_text "$telemetry" "public static void ForceCrashForTesting"
require_text "$telemetry" "Crashlytics forced test crash requested."
require_text "$controller" "InitializeTelemetryAndAds"
require_text "$controller" "FirebaseTelemetry.SetContext(\"game\", \"thumbwaddle\")"
require_text "$controller" "FirebaseTelemetry.LogEvent(\"app_open\")"
require_text "$controller" "FirebaseTelemetry.ForceCrashForTesting"
require_text "$controller" "CrashlyticsTestTapCount"
require_text "$controller" "--mannlab-force-crashlytics-test"
require_text "$controller" "MANNLAB_FORCE_CRASHLYTICS_TEST"
require_text "$controller" "MannLabAdMob.InitializeGameOverInterstitial"
require_text "$controller" "MannLabAdMob.TryShowGameOverInterstitial"
require_text "$controller" "ProductionIosInterstitialAdUnitId"
require_text "$controller" "GameOverInterstitialInterval = 3"
require_text "$ios_build" "BuildCrashlyticsTest"
require_text "$ios_build" "BuildAdMobTest"
require_text "$ios_build" "MANNLAB_ADMOB_FORCE_TEST_ADS"
require_text "$ios_build" "GADApplicationIdentifier"
require_text "$ios_build" "values.Remove(\"GADApplicationIdentifier\")"
require_text "$ios_build" "MANNLAB_THUMBWADDLE_ADMOB_IOS_APP_ID"
require_text "$gma_settings" "adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713"
require_text "$gma_settings" "adMobIOSAppId: ca-app-pub-3940256099942544~1458002511"
require_text "$gma_linker" "GoogleMobileAds.iOS"
require_text "$gma_linker" "GoogleMobileAds.Android"
require_text "$crashlytics_settings" "CrashlyticsSettings"
require_text "$privacy" "Thumbwaddle"
require_text "$privacy" "Firebase Analytics"
require_text "$privacy" "Google AdMob"
require_text "$readme" "Firebase/Crashlytics"
require_text "$readme" "AdMob"

warn_or_fail_missing_config "$ios_plist"
warn_or_fail_missing_config "$android_json"

if [[ -f "$ios_plist" ]]; then
  require_text "$ios_plist" "<string>com.mannlab.games.walking</string>"
fi

if [[ -f "$android_json" ]]; then
  require_text "$android_json" "\"package_name\": \"com.mannlab.games.walking\""
fi

if [[ "$failures" -ne 0 ]]; then
  echo "Thumbwaddle AdMob/Crashlytics readiness check failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "Thumbwaddle AdMob/Crashlytics code readiness verified; Firebase config files are still needed."
else
  echo "Thumbwaddle AdMob/Crashlytics readiness verified."
fi
