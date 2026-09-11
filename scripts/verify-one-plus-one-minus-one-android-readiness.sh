#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
project_settings="$project/ProjectSettings/ProjectSettings.asset"
android_build="$project/Assets/_Project/Editor/BuildAndroidAab.cs"
gma_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
firebase_android_json="$project/Assets/google-services.json"
controller="$project/Assets/_Project/Scripts/OnePlusOneMinusOneController.cs"
app_icon="$project/Assets/_Project/Art/AppIcon-1024.png"
project_unity_version="$(awk '/m_EditorVersion:/ {print $2; exit}' "$project/ProjectSettings/ProjectVersion.txt")"
unity_version="${UNITY_EDITOR_VERSION:-$project_unity_version}"
unity_root="/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents"
if [[ ! -x "$unity_root/MacOS/Unity" ]]; then
  latest_unity_app="$(find /Applications/Unity/Hub/Editor -maxdepth 2 -path '*/Unity.app' -type d 2>/dev/null | sort | tail -1 || true)"
  if [[ -n "$latest_unity_app" ]]; then
    unity_root="$latest_unity_app/Contents"
  fi
fi

unity_editor="$unity_root/MacOS/Unity"
unity_cli="${HOME}/.unity/bin/unity"
android_engine="$(dirname "$(dirname "$unity_root")")/PlaybackEngines/AndroidPlayer"
mode="${1:-release}"
build_log="/tmp/one-plus-one-minus-one-unity-android-${mode}-build.log"
failures=0
warnings=0

case "$mode" in
  release)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildAndroidAab.BuildAab"
    output_path="$project/Builds/Android/one-plus-one-minus-one.aab"
    expected_gad_app_id="${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID:-ca-app-pub-3940256099942544~3347511713}"
    ;;
  apk)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildAndroidAab.BuildApk"
    output_path="$project/Builds/Android/one-plus-one-minus-one.apk"
    expected_gad_app_id="${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID:-ca-app-pub-3940256099942544~3347511713}"
    ;;
  admob-test)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildAndroidAab.BuildAdMobTestApk"
    output_path="$project/Builds/Android/one-plus-one-minus-one-admob-test.apk"
    expected_gad_app_id="ca-app-pub-3940256099942544~3347511713"
    ;;
  *)
    echo "Usage: $0 [release|apk|admob-test]" >&2
    exit 64
    ;;
esac

warn_or_fail() {
  local message="$1"
  local strict="${2:-0}"
  if [[ "$strict" == "1" ]]; then
    echo "$message" >&2
    failures=1
    return
  fi

  echo "Warning: $message" >&2
  warnings=1
}

warn_or_fail_placeholder() {
  local message="$1"
  local value="$2"
  if [[ "$value" != *XXXX* && "$value" != *replace* && "$value" != *REPLACE* ]]; then
    return
  fi

  warn_or_fail "$message" "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
}

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
  if [[ -f "$path" ]] && ! grep -Fq -- "$pattern" "$path"; then
    echo "Missing expected text in $path: $pattern" >&2
    failures=1
  fi
}

"$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"
require_file "$app_icon"
require_text "$android_build" "BuildAndroidAab"
require_file "$android_build.meta"
require_text "$android_build" "BuildAdMobTestApk"
require_text "$android_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID"
require_text "$android_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID"
require_text "$android_build" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH"
require_text "$project_settings" "Android: com.mannlab.games.oneplusoneminusone"
require_text "$project_settings" "AndroidMinSdkVersion: 25"
require_text "$project_settings" "AndroidTargetArchitectures: 2"
if [[ "$mode" == "admob-test" ]]; then
  require_text "$gma_settings" "adMobAndroidAppId: $expected_gad_app_id"
else
  require_text "$gma_settings" "adMobAndroidAppId:"
fi
require_text "$controller" "GetConfiguredAdUnitId"

if [[ "$mode" != "admob-test" ]]; then
  if [[ ! -f "$firebase_android_json" ]]; then
    warn_or_fail "Missing Firebase Android config: $firebase_android_json" "${REQUIRE_FIREBASE_CONFIG:-0}"
  elif ! grep -Fq '"package_name": "com.mannlab.games.oneplusoneminusone"' "$firebase_android_json"; then
    echo "Firebase Android config package name does not match com.mannlab.games.oneplusoneminusone." >&2
    failures=1
  fi

  if [[ "$expected_gad_app_id" == "ca-app-pub-3940256099942544~3347511713" ]]; then
    warn_or_fail "Production Android AdMob App ID is still Google's test app ID." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  fi
  warn_or_fail_placeholder "Production Android AdMob App ID is a placeholder." "$expected_gad_app_id"

  if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID:-}" ]]; then
    warn_or_fail "Production Android interstitial ad unit env is not set." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  elif [[ "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID:-}" == "ca-app-pub-3940256099942544/1033173712" ]]; then
    warn_or_fail "Production Android interstitial ad unit env uses Google's test ID." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  else
    warn_or_fail_placeholder "Production Android interstitial ad unit env is a placeholder." "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID:-}"
  fi

  for env_name in \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS
  do
    if [[ -z "${!env_name:-}" ]]; then
      if [[ "$mode" == "release" ]]; then
        warn_or_fail "$env_name is not set." "1"
      else
        warn_or_fail "$env_name is not set." "${REQUIRE_ANDROID_SIGNING_ENV:-0}"
      fi
    elif [[ "${!env_name:-}" == *replace* || "${!env_name:-}" == *REPLACE* ]]; then
      warn_or_fail "$env_name is a placeholder." "${REQUIRE_ANDROID_SIGNING_ENV:-0}"
    fi
  done

  if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION:-}" ]]; then
    warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION is not set." "${REQUIRE_ANDROID_VERSION_ENV:-0}"
  fi

  if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE:-}" ]]; then
    warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE is not set." "${REQUIRE_ANDROID_VERSION_ENV:-0}"
  fi
fi

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  failures=1
fi

if [[ ! -d "$android_engine" ]]; then
  echo "Unity Android Build Support is not installed: $android_engine" >&2
  failures=1
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  failures=1
else
  license_state="$("$unity_cli" license --json 2>/dev/null || true)"
  if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data", []); sys.exit(0 if data else 1)' <<< "$license_state"; then
    echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
    failures=1
  fi
fi

if [[ "$failures" -ne 0 ]]; then
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod "$build_method" \
  -logFile "$build_log"

require_file "$output_path"

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 Android $mode readiness failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "1 = 1 Android $mode build verified with release warnings."
else
  echo "1 = 1 Android $mode build verified."
fi

echo "Android build log: $build_log"
echo "Android output: $output_path"
