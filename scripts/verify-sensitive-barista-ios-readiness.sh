#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/sensitive-barista"
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
profile_name="${MANNLAB_TOO_PICKY_COFFEE_IOS_PROFILE_SPECIFIER:-Too Picky Coffee}"
profile_uuid="${MANNLAB_TOO_PICKY_COFFEE_IOS_PROFILE_UUID:-aa78feba-c3b8-44d7-975d-8b1eae7b3c05}"
build_log="/tmp/sensitive-barista-unity-ios-${mode}-build.log"
missing=0

case "$mode" in
  release)
    build_method="MannLab.Games.SensitiveBarista.EditorTools.BuildIosXcode.BuildRelease"
    output_path="$project/Builds/iOS/Xcode"
    expected_gad_app_id="ca-app-pub-4525914685149405~6759852565"
    ;;
  crashlytics-test)
    build_method="MannLab.Games.SensitiveBarista.EditorTools.BuildIosXcode.BuildCrashlyticsTest"
    output_path="$project/Builds/iOS/CrashlyticsTestXcode"
    expected_gad_app_id="ca-app-pub-4525914685149405~6759852565"
    ;;
  crashlytics-simulator-test)
    build_method="MannLab.Games.SensitiveBarista.EditorTools.BuildIosXcode.BuildCrashlyticsSimulatorTest"
    output_path="$project/Builds/iOS/CrashlyticsSimulatorTestXcode"
    expected_gad_app_id=""
    ;;
  admob-test)
    build_method="MannLab.Games.SensitiveBarista.EditorTools.BuildIosXcode.BuildAdMobTest"
    output_path="$project/Builds/iOS/AdMobTestXcode"
    expected_gad_app_id="ca-app-pub-3940256099942544~1458002511"
    ;;
  *)
    echo "Usage: $0 [release|crashlytics-test|crashlytics-simulator-test|admob-test]" >&2
    exit 64
    ;;
esac

pbxproj="$output_path/Unity-iPhone.xcodeproj/project.pbxproj"
workspace="$output_path/Unity-iPhone.xcworkspace"
podfile="$output_path/Podfile"

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
  if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data") or []; sys.exit(0 if len(data) > 0 else 1)' <<< "$license_state" 2>/dev/null; then
    if [[ ! -s "${HOME}/Library/Unity/licenses/UnityEntitlementLicense.xml" ]]; then
      echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
      missing=1
    fi
  fi
fi

if ! command -v xcodebuild >/dev/null 2>&1; then
  echo "xcodebuild not found." >&2
  missing=1
fi

if [[ ! -f "$project/Assets/GoogleService-Info.plist" ]]; then
  echo "Firebase iOS plist is missing: $project/Assets/GoogleService-Info.plist" >&2
  missing=1
elif ! /usr/libexec/PlistBuddy -c 'Print :BUNDLE_ID' "$project/Assets/GoogleService-Info.plist" | grep -Fxq "com.mannlab.games.toopickycoffee"; then
  echo "Firebase iOS config bundle ID does not match com.mannlab.games.toopickycoffee." >&2
  missing=1
fi

if [[ ! -f "$project/Assets/_Project/Art/TooPickyCoffeeIcon.png" ]]; then
  echo "Source app icon is missing: $project/Assets/_Project/Art/TooPickyCoffeeIcon.png" >&2
  missing=1
fi

if [[ "$mode" != "crashlytics-simulator-test" && -n "$profile_uuid" && ! -f "${HOME}/Library/MobileDevice/Provisioning Profiles/${profile_uuid}.mobileprovision" ]]; then
  echo "Provisioning profile is not installed: ${profile_uuid}.mobileprovision" >&2
  missing=1
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
test -f "$pbxproj"
test -d "$workspace"
test -f "$podfile"
test -f "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"
test -f "$output_path/LaunchScreen-iPhone.storyboard"
test -f "$output_path/LaunchScreen-iPad.storyboard"

if [[ "$mode" != "crashlytics-simulator-test" ]]; then
  grep -q "PROVISIONING_PROFILE_SPECIFIER = \"$profile_name\";" "$pbxproj"
  grep -Eq "PROVISIONING_PROFILE = \"?$profile_uuid\"?;" "$pbxproj"
fi

if [[ -n "$expected_gad_app_id" ]]; then
  actual_gad_app_id="$(/usr/libexec/PlistBuddy -c 'Print :GADApplicationIdentifier' "$output_path/Info.plist")"
  if [[ "$actual_gad_app_id" != "$expected_gad_app_id" ]]; then
    echo "Unexpected GADApplicationIdentifier in $output_path/Info.plist: $actual_gad_app_id" >&2
    echo "Expected: $expected_gad_app_id" >&2
    exit 1
  fi
fi

echo "iOS build log: $build_log"
echo "iOS $mode Xcode project verified: $output_path"
echo "Open this workspace for AdMob/CocoaPods builds: $workspace"
echo "Do not archive Unity-iPhone.xcodeproj directly; GoogleMobileAds is linked through Pods."
