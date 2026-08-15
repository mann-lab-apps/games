#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/10000"
manifest="$project/Packages/manifest.json"
lockfile="$project/Packages/packages-lock.json"
controller="$project/Assets/_Project/Scripts/Game10000Controller.cs"
telemetry="$project/Assets/_Project/Scripts/FirebaseTelemetry.cs"
android_build="$project/Assets/_Project/Editor/BuildAndroidAab.cs"
ios_build="$project/Assets/_Project/Editor/BuildIosXcode.cs"
design_doc="$repo_root/docs/10000-game-design.md"
ios_plist="$project/Assets/GoogleService-Info.plist"
android_json="$project/Assets/Plugins/Android/google-services.json"
desktop_json="$project/Assets/StreamingAssets/google-services-desktop.json"
failures=0
warnings=0

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
require_text "$lockfile" "\"com.mannlab.firebase-unity-sdk\""
require_text "$telemetry" "public static void SetContext"
require_text "$telemetry" "public static void ForceCrashForTesting"
require_text "$telemetry" "Crashlytics forced test crash requested."
require_text "$controller" "CrashlyticsTestTapCount"
require_text "$controller" "--mannlab-force-crashlytics-test"
require_text "$controller" "MANNLAB_FORCE_CRASHLYTICS_TEST"
require_text "$controller" "FirebaseTelemetry.SetContext(\"game\", \"10000\")"
require_text "$controller" "FirebaseTelemetry.ForceCrashForTesting"
require_text "$android_build" "BuildCrashlyticsTestApk"
require_text "$android_build" "PlayerSettings.Android.useCustomKeystore = false"
require_text "$ios_build" "BuildCrashlyticsTest"
require_text "$design_doc" "Crashlytics 확인용 development build"
require_text "$design_doc" "com.mannlab.games.game10000"

warn_or_fail_missing_config "$ios_plist"
warn_or_fail_missing_config "$android_json"
warn_or_fail_missing_config "$desktop_json"

if [[ -f "$ios_plist" ]]; then
  require_text "$ios_plist" "<string>com.mannlab.games.game10000</string>"
fi

if [[ -f "$android_json" ]]; then
  require_text "$android_json" "\"package_name\": \"com.mannlab.games.game10000\""
fi

if [[ -f "$desktop_json" ]]; then
  require_text "$desktop_json" "\"package_name\": \"com.mannlab.games.game10000\""
fi

if [[ "$failures" -ne 0 ]]; then
  echo "10000 Firebase readiness check failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "10000 Firebase code readiness verified; Firebase config files are still needed."
else
  echo "10000 Firebase readiness verified."
fi
