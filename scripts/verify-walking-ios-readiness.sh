#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/walking"
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
build_log="/tmp/walking-unity-ios-build.log"
output_path="$project/Builds/iOS/Xcode"
pbxproj="$output_path/Unity-iPhone.xcodeproj/project.pbxproj"
profile_name="${MANNLAB_THUMBWADDLE_IOS_PROFILE_SPECIFIER:-Thumbwaddle}"
profile_uuid="${MANNLAB_THUMBWADDLE_IOS_PROFILE_UUID:-3c745d8d-b794-4204-b5f1-2fd886a0242e}"
bundle_id="com.mannlab.games.thumbwaddler"
project_profile="$project/BuildSettings/iOS/ProvisioningProfiles/Thumbwaddle.mobileprovision"
installed_profile="${HOME}/Library/MobileDevice/Provisioning Profiles/${profile_uuid}.mobileprovision"
missing=0

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  echo "Install it from Unity Hub > Installs > $unity_version > Add modules > iOS Build Support." >&2
  missing=1
fi

if [[ -x "$unity_cli" ]]; then
  license_state="$("$unity_cli" license --json 2>/dev/null || true)"
  if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data") or []; sys.exit(0 if len(data) > 0 else 1)' <<< "$license_state" 2>/dev/null; then
    if [[ -s "${HOME}/Library/Unity/licenses/UnityEntitlementLicense.xml" ]]; then
      echo "Unity CLI license check did not return active entitlements; using local Unity entitlement license file." >&2
    else
      echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
      missing=1
    fi
  fi
else
  echo "Unity CLI not found: $unity_cli" >&2
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
  echo "App icon is missing. Run: node scripts/generate-walking-app-icon.mjs" >&2
  missing=1
fi

if [[ ! -f "$project_profile" ]]; then
  echo "Thumbwaddle provisioning profile is missing from project: $project_profile" >&2
  missing=1
elif ! strings "$project_profile" | grep -q "<string>ZRA4DHHKQ4.${bundle_id}</string>"; then
  echo "Thumbwaddle provisioning profile does not match bundle id: $bundle_id" >&2
  missing=1
fi

if [[ ! -f "$installed_profile" ]]; then
  echo "Thumbwaddle provisioning profile is not installed for Xcode: $installed_profile" >&2
  missing=1
fi

if [[ "$missing" -ne 0 ]]; then
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod MannLab.Games.Walking.EditorTools.BuildIosXcode.Build \
  -logFile "$build_log"

test -d "$output_path"
test -f "$pbxproj"
test -f "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"
grep -q "PRODUCT_BUNDLE_IDENTIFIER = $bundle_id;" "$pbxproj"
grep -Eq "PROVISIONING_PROFILE_SPECIFIER = \"?$profile_name\"?;" "$pbxproj"
grep -Eq "PROVISIONING_PROFILE = \"?$profile_uuid\"?;" "$pbxproj"

echo "iOS build log: $build_log"
echo "iOS Xcode project verified: $output_path"
echo "Open this Xcode project for device/archive builds: $output_path/Unity-iPhone.xcodeproj"
