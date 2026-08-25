#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/standing"
unity_editor="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"
ios_engine="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/iOSSupport"
build_log="/tmp/standing-unity-ios-build.log"
output_path="$project/Builds/iOS/Xcode"
pbxproj="$output_path/Unity-iPhone.xcodeproj/project.pbxproj"
profile_name="Standing!"
profile_uuid="a8eb35e9-069d-4df9-aaf3-098cb9d724c7"
profile_app_id="ZRA4DHHKQ4.com.mannlab.games.standing"
local_profile="$project/Signing/Standing.mobileprovision"
installed_profile="${HOME}/Library/MobileDevice/Provisioning Profiles/${profile_uuid}.mobileprovision"
missing=0

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$ios_engine" ]]; then
  echo "Unity iOS Build Support is not installed: $ios_engine" >&2
  echo "Install it from Unity Hub > Installs > 6000.3.22f1 > Add modules > iOS Build Support." >&2
  missing=1
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

if [[ ! -f "$local_profile" ]]; then
  echo "Standing provisioning profile is missing: $local_profile" >&2
  exit 2
fi

profile_plist="$(mktemp /tmp/standing-profile.XXXXXX.plist)"
openssl smime -inform der -verify -noverify -in "$local_profile" -out "$profile_plist" >/dev/null 2>&1
actual_profile_uuid="$(plutil -extract UUID raw -o - "$profile_plist")"
actual_profile_name="$(plutil -extract Name raw -o - "$profile_plist")"
actual_app_id="$(plutil -extract Entitlements.application-identifier raw -o - "$profile_plist")"
rm -f "$profile_plist"

if [[ "$actual_profile_uuid" != "$profile_uuid" ]]; then
  echo "Unexpected Standing provisioning profile UUID: $actual_profile_uuid" >&2
  exit 2
fi

if [[ "$actual_profile_name" != "$profile_name" ]]; then
  echo "Unexpected Standing provisioning profile name: $actual_profile_name" >&2
  exit 2
fi

if [[ "$actual_app_id" != "$profile_app_id" ]]; then
  echo "Standing provisioning profile App ID mismatch: $actual_app_id" >&2
  exit 2
fi

mkdir -p "$(dirname "$installed_profile")"
cp "$local_profile" "$installed_profile"

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod MannLab.Games.Standing.EditorTools.BuildIosXcode.Build \
  -logFile "$build_log"

test -d "$output_path"
test -f "$pbxproj"
test -f "$output_path/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-AppStore-1024.png"

if ! grep -q "PROVISIONING_PROFILE_SPECIFIER = \"$profile_name\";" "$pbxproj"; then
  echo "Expected provisioning profile specifier not found in Xcode project: $profile_name" >&2
  exit 3
fi

if ! grep -Eq "PROVISIONING_PROFILE = \"?$profile_uuid\"?;" "$pbxproj"; then
  echo "Expected provisioning profile UUID not found in Xcode project: $profile_uuid" >&2
  exit 3
fi

echo "iOS build log: $build_log"
echo "iOS Xcode project verified: $output_path"
