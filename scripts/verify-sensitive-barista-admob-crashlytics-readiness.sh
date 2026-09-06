#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/sensitive-barista"
manifest="$project/Packages/manifest.json"
lockfile="$project/Packages/packages-lock.json"
asmdef="$project/Assets/_Project/Scripts/SensitiveBaristaGame.asmdef"
controller="$project/Assets/_Project/Scripts/SensitiveBaristaController.cs"
telemetry="$project/Assets/_Project/Scripts/FirebaseTelemetry.cs"
ios_build="$project/Assets/_Project/Editor/BuildIosXcode.cs"
gma_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
gma_linker="$project/Assets/GoogleMobileAds/link.xml"
crashlytics_settings="$project/Assets/Editor Default Resources/CrashlyticsSettings.asset"
firebase_plist="$project/Assets/GoogleService-Info.plist"
provisioning_profile="$project/BuildSettings/iOS/ProvisioningProfiles/Too_Picky_Coffee.mobileprovision"
readme="$project/README.md"
admob_bridge="$repo_root/shared/unity-packages/com.mannlab.admob-core/Runtime/MannLabAdMob.cs"
profile_plist="$(mktemp)"
failures=0
warnings=0

cleanup() {
  rm -f "$profile_plist"
}
trap cleanup EXIT

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
require_text "$lockfile" "\"com.google.ads.mobile\""
require_text "$lockfile" "\"com.google.external-dependency-manager\""
require_text "$lockfile" "\"com.mannlab.firebase-unity-sdk\""
require_text "$asmdef" "\"MannLab.Ads.Core\""
require_text "$telemetry" "namespace MannLab.Games.SensitiveBarista"
require_text "$telemetry" "public static void ForceCrashForTesting"
require_text "$telemetry" "Crashlytics forced test crash requested."
require_text "$controller" "FirebaseTelemetry.Initialize"
require_text "$controller" "FirebaseTelemetry.SetContext(\"game\", \"too-picky-coffee\")"
require_text "$controller" "FirebaseTelemetry.LogEvent(\"app_open\")"
require_text "$controller" "FirebaseTelemetry.ForceCrashForTesting"
require_text "$controller" "MannLabAdMob.InitializeGameOverInterstitial"
require_text "$controller" "MannLabAdMob.TryShowGameOverInterstitial"
require_text "$controller" "ProductionIosInterstitialAdUnitId"
require_text "$controller" "CrashlyticsTestTapCount"
require_text "$controller" "--mannlab-force-crashlytics-test"
require_text "$ios_build" "GADApplicationIdentifier"
require_text "$ios_build" "MANNLAB_TOO_PICKY_COFFEE_ADMOB_IOS_APP_ID"
require_text "$ios_build" "aa78feba-c3b8-44d7-975d-8b1eae7b3c05"
require_text "$ios_build" "BuildCrashlyticsTest"
require_text "$ios_build" "BuildCrashlyticsSimulatorTest"
require_text "$ios_build" "BuildAdMobTest"
require_text "$ios_build" "ca-app-pub-3940256099942544~1458002511"
require_text "$gma_settings" "adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713"
require_text "$gma_settings" "adMobIOSAppId: ca-app-pub-4525914685149405~6759852565"
require_text "$gma_linker" "GoogleMobileAds.iOS"
require_text "$gma_linker" "GoogleMobileAds.Ump.iOS"
require_file "$crashlytics_settings"
require_text "$admob_bridge" "public static class MannLabAdMob"
require_text "$admob_bridge" "MobileAds.Initialize"
require_text "$admob_bridge" "ConsentInformation.Update"
require_text "$readme" "Firebase Analytics/Crashlytics telemetry"
require_text "$readme" "AdMob interstitial setup"
require_file "$provisioning_profile"

warn_or_fail_missing_config "$firebase_plist"
if [[ -f "$firebase_plist" ]]; then
  require_text "$firebase_plist" "<string>com.mannlab.games.toopickycoffee</string>"
fi

if [[ -f "$provisioning_profile" ]]; then
  if ! strings "$provisioning_profile" > "$profile_plist"; then
    echo "Could not inspect provisioning profile: $provisioning_profile" >&2
    failures=1
  else
    require_text "$profile_plist" "<string>Too Picky Coffee</string>"
    require_text "$profile_plist" "<string>aa78feba-c3b8-44d7-975d-8b1eae7b3c05</string>"
    require_text "$profile_plist" "<string>ZRA4DHHKQ4.com.mannlab.games.toopickycoffee</string>"
  fi
fi

if [[ "$failures" -ne 0 ]]; then
  echo "Too Picky Coffee AdMob/Crashlytics readiness check failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "Too Picky Coffee AdMob/Crashlytics code readiness verified; Firebase config and production AdMob IDs are still needed."
else
  echo "Too Picky Coffee AdMob/Crashlytics readiness verified."
fi
