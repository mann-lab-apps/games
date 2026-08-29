#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/2048-crash"
manifest="$project/Packages/manifest.json"
lockfile="$project/Packages/packages-lock.json"
asmdef="$project/Assets/_Project/Scripts/Game2048CrashGame.asmdef"
controller="$project/Assets/_Project/Scripts/Crash2048Controller.cs"
ios_build="$project/Assets/_Project/Editor/BuildIosXcode.cs"
android_build="$project/Assets/_Project/Editor/BuildAndroidAab.cs"
gma_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
gma_linker="$project/Assets/GoogleMobileAds/link.xml"
admob_package="$repo_root/shared/unity-packages/com.mannlab.admob-core/package.json"
admob_bridge="$repo_root/shared/unity-packages/com.mannlab.admob-core/Runtime/MannLabAdMob.cs"
privacy="$repo_root/web/mannlab-games/src/main.jsx"
readme="$project/README.md"
design_doc="$repo_root/docs/2048-crash-game-design.md"
failures=0

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

require_text "$manifest" "\"com.mannlab.admob-core\""
require_text "$manifest" "\"https://package.openupm.com\""
require_text "$lockfile" "\"com.google.ads.mobile\""
require_text "$lockfile" "\"com.google.external-dependency-manager\""
require_text "$asmdef" "\"MannLab.Ads.Core\""
require_text "$admob_package" "\"com.mannlab.admob-core\""
require_text "$admob_package" "\"com.google.ads.mobile\": \"11.4.0\""
require_text "$admob_bridge" "public static class MannLabAdMob"
require_text "$admob_bridge" "AndroidInterstitialTestAdUnitId"
require_text "$admob_bridge" "IosInterstitialTestAdUnitId"
require_text "$admob_bridge" "MANNLAB_ADMOB_FORCE_TEST_ADS"
require_text "$controller" "MannLabAdMob.InitializeGameOverInterstitial"
require_text "$controller" "MannLabAdMob.TryShowGameOverInterstitial"
require_text "$controller" "ca-app-pub-4525914685149405/6947652887"
require_text "$controller" "GameOverInterstitialInterval = 1"
require_text "$ios_build" "GADApplicationIdentifier"
require_text "$ios_build" "MANNLAB_2048_CRASH_ADMOB_IOS_APP_ID"
require_text "$ios_build" "ca-app-pub-4525914685149405~7818820774"
require_text "$ios_build" "MANNLAB_2048_CRASH_IOS_MARKETING_VERSION"
require_text "$ios_build" "DefaultMarketingVersion = \"1.0.1\""
require_text "$ios_build" "BuildAdMobTest"
require_text "$android_build" "MANNLAB_2048_CRASH_ADMOB_ANDROID_APP_ID"
require_text "$android_build" "BuildAdMobTestApk"
require_text "$gma_settings" "adMobIOSAppId: ca-app-pub-4525914685149405~7818820774"
require_text "$gma_settings" "adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713"
require_text "$gma_linker" "GoogleMobileAds.iOS"
require_text "$privacy" "Google AdMob"
require_text "$readme" "AdMob"
require_text "$design_doc" "2048 Crash Game Over Interstitial"

if [[ "$failures" -ne 0 ]]; then
  echo "2048 Crash AdMob readiness check failed." >&2
  exit 1
fi

echo "2048 Crash AdMob readiness verified."
