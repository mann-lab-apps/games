#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/gather-and-shot"
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
build_log="/tmp/gather-and-shot-unity-ios-${mode}-build.log"
missing=0

case "$mode" in
  release)
    build_method="MannLab.Games.GatherAndShot.EditorTools.BuildIosXcode.BuildRelease"
    output_path="$project/Builds/iOS/Xcode"
    expected_gad_app_id="ca-app-pub-4525914685149405~6036634116"
    ;;
  crashlytics-test)
    build_method="MannLab.Games.GatherAndShot.EditorTools.BuildIosXcode.BuildCrashlyticsTest"
    output_path="$project/Builds/iOS/CrashlyticsTestXcode"
    expected_gad_app_id="ca-app-pub-4525914685149405~6036634116"
    ;;
  admob-test)
    build_method="MannLab.Games.GatherAndShot.EditorTools.BuildIosXcode.BuildAdMobTest"
    output_path="$project/Builds/iOS/AdMobTestXcode"
    expected_gad_app_id="ca-app-pub-3940256099942544~1458002511"
    ;;
  *)
    echo "Usage: $0 [release|crashlytics-test|admob-test]" >&2
    exit 64
    ;;
esac

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  echo "Install it from Unity Hub > Installs > $unity_version > Add modules > iOS Build Support." >&2
  missing=1
fi

if [[ ! -f "$project/Assets/_Project/Art/AppStore/AppIcon-1024.png" ]]; then
  echo "Missing iOS app icon: $project/Assets/_Project/Art/AppStore/AppIcon-1024.png" >&2
  echo "Run ./scripts/generate-gather-and-shot-doodle-assets.py first." >&2
  missing=1
fi

if [[ ! -f "$project/Assets/GoogleService-Info.plist" ]]; then
  echo "Missing Firebase iOS config: $project/Assets/GoogleService-Info.plist" >&2
  missing=1
elif ! /usr/libexec/PlistBuddy -c 'Print :BUNDLE_ID' "$project/Assets/GoogleService-Info.plist" | grep -Fxq "com.mannlab.games.gatherandshot"; then
  echo "Firebase iOS config bundle ID does not match com.mannlab.games.gatherandshot." >&2
  missing=1
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  missing=1
else
  license_state="$("$unity_cli" license --json 2>/dev/null || true)"
  if ! python3 -c 'import json,sys; sys.exit(0 if len(json.load(sys.stdin)["data"]) > 0 else 1)' <<< "$license_state"; then
    echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
    missing=1
  fi
fi

if command -v xcodebuild >/dev/null 2>&1; then
  xcode_version="$(xcodebuild -version | awk '/Xcode/ {print $2}')"
  xcode_major="${xcode_version%%.*}"
else
  xcode_version=""
  xcode_major=""
fi

if [[ "${REQUIRE_APP_STORE_XCODE:-0}" == "1" ]]; then
  if [[ -z "$xcode_major" || "$xcode_major" -lt 26 ]]; then
    echo "Xcode 26 or newer is required for App Store uploads after 2026-04-28. Current: ${xcode_version:-unknown}" >&2
    missing=1
  fi
elif [[ -z "$xcode_major" || "$xcode_major" -lt 26 ]]; then
  echo "Warning: Xcode 26 or newer is required for App Store uploads after 2026-04-28. Current: ${xcode_version:-unknown}" >&2
  echo "Local unsigned iOS build checks can still run with the installed Xcode." >&2
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
test -f "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"
test -f "$output_path/LaunchScreen-iPhone.storyboard"
test -f "$output_path/LaunchScreen-iPad.storyboard"
actual_gad_app_id="$(/usr/libexec/PlistBuddy -c 'Print :GADApplicationIdentifier' "$output_path/Info.plist")"
if [[ "$actual_gad_app_id" != "$expected_gad_app_id" ]]; then
  echo "Unexpected GADApplicationIdentifier in $output_path/Info.plist: $actual_gad_app_id" >&2
  echo "Expected: $expected_gad_app_id" >&2
  exit 1
fi

echo "iOS build log: $build_log"
echo "iOS $mode Xcode project verified: $output_path"
workspace="$output_path/Unity-iPhone.xcworkspace"
if [[ "$mode" == "admob-test" || -d "$workspace" ]]; then
  echo "Open this workspace for AdMob/CocoaPods builds: $workspace"
  echo "Do not archive Unity-iPhone.xcodeproj directly; GoogleMobileAds is linked through Pods."
fi
