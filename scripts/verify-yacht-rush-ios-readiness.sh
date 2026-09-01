#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/yacht-rush"
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
missing=0
profile_name="${MANNLAB_YACHT_RUSH_IOS_PROFILE_SPECIFIER:-Yacht Rush}"
profile_uuid="${MANNLAB_YACHT_RUSH_IOS_PROFILE_UUID:-7ac8efc3-c666-48e9-a209-816db04e5ca7}"

case "$mode" in
  release)
    build_method="MannLab.Games.YachtRush.EditorTools.BuildIosXcode.BuildRelease"
    output_path="$project/Builds/iOS/Xcode"
    ;;
  simulator)
    build_method="MannLab.Games.YachtRush.EditorTools.BuildIosXcode.BuildSimulator"
    output_path="$project/Builds/iOS/SimulatorXcode"
    ;;
  crashlytics-test)
    build_method="MannLab.Games.YachtRush.EditorTools.BuildIosXcode.BuildCrashlyticsTest"
    output_path="$project/Builds/iOS/CrashlyticsTestXcode"
    ;;
  crashlytics-simulator-test)
    build_method="MannLab.Games.YachtRush.EditorTools.BuildIosXcode.BuildCrashlyticsSimulatorTest"
    output_path="$project/Builds/iOS/CrashlyticsSimulatorTestXcode"
    ;;
  admob-test)
    build_method="MannLab.Games.YachtRush.EditorTools.BuildIosXcode.BuildAdMobTest"
    output_path="$project/Builds/iOS/AdMobTestXcode"
    ;;
  *)
    echo "Usage: $0 [release|simulator|crashlytics-test|crashlytics-simulator-test|admob-test]" >&2
    exit 64
    ;;
esac

build_log="/tmp/yacht-rush-unity-ios-${mode}-build.log"
pbxproj="$output_path/Unity-iPhone.xcodeproj/project.pbxproj"
xcodeproj="$output_path/Unity-iPhone.xcodeproj"
app_icon="$project/Assets/_Project/Art/AppStore/AppIcon-1024.png"
admob_settings="$project/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"
firebase_plist="$project/Assets/GoogleService-Info.plist"
installed_profile="${HOME}/Library/MobileDevice/Provisioning Profiles/${profile_uuid}.mobileprovision"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  echo "Install it from Unity Hub > Installs > $unity_version > Add modules > iOS Build Support." >&2
  missing=1
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  missing=1
else
  license_state="$("$unity_cli" license --json 2>/dev/null || true)"
  if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data") or []; sys.exit(0 if len(data) > 0 else 1)' <<< "$license_state" 2>/dev/null; then
    if [[ -s "${HOME}/Library/Unity/licenses/UnityEntitlementLicense.xml" ]]; then
      echo "Unity CLI license check did not return active entitlements; using local Unity entitlement license file." >&2
    else
      echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
      missing=1
    fi
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

if [[ ! -f "$app_icon" ]]; then
  echo "App icon is missing: $app_icon" >&2
  missing=1
fi

if [[ "$mode" != "simulator" && "$mode" != "crashlytics-simulator-test" && ! -f "$installed_profile" ]]; then
  echo "Yacht Rush provisioning profile is not installed: $installed_profile" >&2
  missing=1
fi

if [[ ! -f "$admob_settings" ]]; then
  echo "Google Mobile Ads settings are missing: $admob_settings" >&2
  missing=1
fi

if [[ ! -f "$firebase_plist" ]]; then
  echo "Warning: Firebase iOS app config is missing: $firebase_plist" >&2
  echo "Crashlytics SDK will compile, but reports will not reach Firebase until Yacht Rush's GoogleService-Info.plist is added or MANNLAB_YACHT_RUSH_FIREBASE_IOS_PLIST is set." >&2
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
test -d "$xcodeproj"
test -f "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"
test -f "$output_path/Info.plist"
grep -q "GADApplicationIdentifier" "$output_path/Info.plist"
if [[ "$mode" != "simulator" && "$mode" != "crashlytics-simulator-test" ]]; then
  grep -q "PROVISIONING_PROFILE_SPECIFIER = \"$profile_name\";" "$pbxproj"
  grep -Eq "PROVISIONING_PROFILE = \"?$profile_uuid\"?;" "$pbxproj"
fi

echo "iOS build log: $build_log"
echo "iOS $mode Xcode project verified: $output_path"
workspace="$output_path/Unity-iPhone.xcworkspace"
if [[ -d "$workspace" ]]; then
  echo "Open this workspace for Firebase/AdMob/CocoaPods builds: $workspace"
  echo "Do not archive Unity-iPhone.xcodeproj directly; native SDKs are linked through Pods."
else
  echo "Open this Xcode project: $xcodeproj"
fi
