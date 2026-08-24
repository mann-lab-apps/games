#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/2048-blink"
unity_editor="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"
unity_cli="${HOME}/.unity/bin/unity"
ios_engine="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/iOSSupport"
mode="${1:-release}"
profile_name="${MANNLAB_2048_BLINK_IOS_PROFILE_SPECIFIER:-2048 Blink}"
profile_uuid="${MANNLAB_2048_BLINK_IOS_PROFILE_UUID:-b8ffa290-1a3e-444d-8190-1474514857cf}"
missing=0

case "$mode" in
  release)
    build_method="MannLab.Games.Game2048Blink.EditorTools.BuildIosXcode.BuildRelease"
    output_path="$project/Builds/iOS/Xcode"
    ;;
  crashlytics-test)
    build_method="MannLab.Games.Game2048Blink.EditorTools.BuildIosXcode.BuildCrashlyticsTest"
    output_path="$project/Builds/iOS/CrashlyticsTestXcode"
    ;;
  crashlytics-simulator-test)
    build_method="MannLab.Games.Game2048Blink.EditorTools.BuildIosXcode.BuildCrashlyticsSimulatorTest"
    output_path="$project/Builds/iOS/CrashlyticsSimulatorTestXcode"
    ;;
  *)
    echo "Usage: $0 [release|crashlytics-test|crashlytics-simulator-test]" >&2
    exit 64
    ;;
esac

build_log="/tmp/2048-blink-unity-ios-${mode}-build.log"
pbxproj="$output_path/Unity-iPhone.xcodeproj/project.pbxproj"
workspace="$output_path/Unity-iPhone.xcworkspace"
podfile="$output_path/Podfile"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  echo "Install it from Unity Hub > Installs > 6000.3.22f1 > Add modules > iOS Build Support." >&2
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

if [[ ! -f "$project/Assets/_Project/Art/AppStore/AppIcon-1024.png" ]]; then
  echo "App icon is missing. Run: node scripts/generate-2048-blink-app-icon.mjs" >&2
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

if [[ "$mode" != "crashlytics-simulator-test" ]]; then
  if ! grep -q "PROVISIONING_PROFILE_SPECIFIER = \"$profile_name\";" "$pbxproj"; then
    echo "Expected provisioning profile specifier not found in Xcode project: $profile_name" >&2
    exit 3
  fi

  if ! grep -Eq "PROVISIONING_PROFILE = \"?$profile_uuid\"?;" "$pbxproj"; then
    echo "Expected provisioning profile UUID not found in Xcode project: $profile_uuid" >&2
    exit 3
  fi
fi

echo "iOS build log: $build_log"
echo "iOS $mode Xcode project verified: $output_path"
echo "Open this workspace for AdMob/CocoaPods builds: $workspace"
echo "Do not archive Unity-iPhone.xcodeproj directly; GoogleMobileAds is linked through Pods."
