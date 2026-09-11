#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
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
ios_engine="$(dirname "$(dirname "$unity_root")")/PlaybackEngines/iOSSupport"
mode="${1:-release}"
build_log="/tmp/one-plus-one-minus-one-unity-ios-${mode}-build.log"
missing=0
warnings=0

if command -v node >/dev/null 2>&1; then
  node "$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"
else
  echo "Warning: node is not available; skipping static round verification." >&2
  warnings=1
fi

case "$mode" in
  release)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildIosXcode.BuildRelease"
    output_path="$project/Builds/iOS/Xcode"
    expected_gad_app_id="${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID:-ca-app-pub-3940256099942544~1458002511}"
    ;;
  crashlytics-test)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildIosXcode.BuildCrashlyticsTest"
    output_path="$project/Builds/iOS/CrashlyticsTestXcode"
    expected_gad_app_id="${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID:-ca-app-pub-3940256099942544~1458002511}"
    ;;
  admob-test)
    build_method="MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildIosXcode.BuildAdMobTest"
    output_path="$project/Builds/iOS/AdMobTestXcode"
    expected_gad_app_id="ca-app-pub-3940256099942544~1458002511"
    ;;
  *)
    echo "Usage: $0 [release|crashlytics-test|admob-test]" >&2
    exit 64
    ;;
esac

warn_or_fail() {
  local message="$1"
  local strict="${2:-0}"
  if [[ "$strict" == "1" ]]; then
    echo "$message" >&2
    missing=1
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
    missing=1
  fi
}

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  missing=1
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  missing=1
else
  license_state="$("$unity_cli" license --json 2>/dev/null || true)"
  if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data", []); sys.exit(0 if data else 1)' <<< "$license_state"; then
    echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
    missing=1
  fi
fi

if [[ ! -f "$project/Assets/_Project/Art/AppIcon-1024.png" ]]; then
  echo "Missing app icon: $project/Assets/_Project/Art/AppIcon-1024.png" >&2
  missing=1
fi

if [[ ! -f "$project/Assets/_Project/Store/PrivacyInfo.xcprivacy" ]]; then
  echo "Missing Apple privacy manifest: $project/Assets/_Project/Store/PrivacyInfo.xcprivacy" >&2
  missing=1
else
  /usr/libexec/PlistBuddy -c 'Print :NSPrivacyAccessedAPITypes:0:NSPrivacyAccessedAPIType' "$project/Assets/_Project/Store/PrivacyInfo.xcprivacy" | grep -Fxq "NSPrivacyAccessedAPICategoryUserDefaults" || {
    echo "Apple privacy manifest should declare UserDefaults required-reason API usage." >&2
    missing=1
  }
  /usr/libexec/PlistBuddy -c 'Print :NSPrivacyAccessedAPITypes:0:NSPrivacyAccessedAPITypeReasons:0' "$project/Assets/_Project/Store/PrivacyInfo.xcprivacy" | grep -Fxq "CA92.1" || {
    echo "Apple privacy manifest should use UserDefaults reason CA92.1 for app-local progress storage." >&2
    missing=1
  }
fi

if [[ "$mode" != "admob-test" ]]; then
  if [[ ! -f "$project/Assets/GoogleService-Info.plist" ]]; then
    warn_or_fail "Missing Firebase iOS config: $project/Assets/GoogleService-Info.plist" "${REQUIRE_FIREBASE_CONFIG:-0}"
  elif ! /usr/libexec/PlistBuddy -c 'Print :BUNDLE_ID' "$project/Assets/GoogleService-Info.plist" | grep -Fxq "com.mannlab.games.oneplusoneminusone"; then
    echo "Firebase iOS config bundle ID does not match com.mannlab.games.oneplusoneminusone." >&2
    missing=1
  fi

  if [[ "$expected_gad_app_id" == "ca-app-pub-3940256099942544~1458002511" ]]; then
    warn_or_fail "Production iOS AdMob App ID is still Google's test app ID." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  fi
  warn_or_fail_placeholder "Production iOS AdMob App ID is a placeholder." "$expected_gad_app_id"

  if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID:-}" ]]; then
    warn_or_fail "Production iOS interstitial ad unit env is not set." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  elif [[ "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID:-}" == "ca-app-pub-3940256099942544/4411468910" ]]; then
    warn_or_fail "Production iOS interstitial ad unit env uses Google's test ID." "${REQUIRE_PRODUCTION_ADMOB_IDS:-0}"
  else
    warn_or_fail_placeholder "Production iOS interstitial ad unit env is a placeholder." "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID:-}"
  fi
fi

if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION:-}" ]]; then
  warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION is not set." "${REQUIRE_IOS_VERSION_ENV:-0}"
fi

if [[ -z "${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER:-}" ]]; then
  warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER is not set." "${REQUIRE_IOS_VERSION_ENV:-0}"
fi

if [[ "$missing" -ne 0 ]]; then
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod "$build_method" \
  -logFile "$build_log"

test -d "$output_path"
require_file "$output_path/Info.plist"
require_file "$output_path/PrivacyInfo.xcprivacy"
require_file "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"
require_file "$output_path/LaunchScreen-iPhone.storyboard"
require_file "$output_path/LaunchScreen-iPad.storyboard"

actual_bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$output_path/Info.plist")"
if [[ "$actual_bundle_id" == *'$'* || "$actual_bundle_id" == *'{'* ]]; then
  actual_bundle_id="$(awk -F' = ' '/PRODUCT_BUNDLE_IDENTIFIER = com[.]mannlab[.]games[.]oneplusoneminusone/ {gsub(/;$/, "", $2); print $2; exit}' "$output_path/Unity-iPhone.xcodeproj/project.pbxproj")"
fi
if [[ "$actual_bundle_id" != "com.mannlab.games.oneplusoneminusone" ]]; then
  echo "Unexpected bundle ID: $actual_bundle_id" >&2
  missing=1
fi

actual_display_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' "$output_path/Info.plist" 2>/dev/null || /usr/libexec/PlistBuddy -c 'Print :CFBundleName' "$output_path/Info.plist")"
if [[ "$actual_display_name" != "1 = 1" ]]; then
  echo "Unexpected app display name: $actual_display_name" >&2
  missing=1
fi

actual_gad_app_id="$(/usr/libexec/PlistBuddy -c 'Print :GADApplicationIdentifier' "$output_path/Info.plist")"
if [[ "$actual_gad_app_id" != "$expected_gad_app_id" ]]; then
  echo "Unexpected GADApplicationIdentifier: $actual_gad_app_id" >&2
  echo "Expected: $expected_gad_app_id" >&2
  missing=1
fi

if [[ "$missing" -ne 0 ]]; then
  echo "1 = 1 iOS $mode readiness failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "iOS $mode Xcode project verified with release warnings."
else
  echo "iOS $mode Xcode project verified."
fi

echo "iOS build log: $build_log"
echo "iOS Xcode output: $output_path"
workspace="$output_path/Unity-iPhone.xcworkspace"
if [[ -d "$workspace" ]]; then
  echo "Open this workspace for CocoaPods builds: $workspace"
fi
