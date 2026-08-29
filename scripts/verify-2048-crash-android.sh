#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/2048-crash"
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
build_format="${1:-aab}"
build_log="/tmp/2048-crash-unity-android-${build_format}-build.log"
signing_env="$project/Signing/local-signing.env"

case "$build_format" in
  aab)
    build_method="MannLab.Games.Game2048Crash.EditorTools.BuildAndroidAab.BuildAab"
    artifact="$project/Builds/Android/2048-crash.aab"
    ;;
  apk)
    build_method="MannLab.Games.Game2048Crash.EditorTools.BuildAndroidAab.BuildApk"
    artifact="$project/Builds/Android/2048-crash.apk"
    ;;
  admob-test)
    build_method="MannLab.Games.Game2048Crash.EditorTools.BuildAndroidAab.BuildAdMobTestApk"
    artifact="$project/Builds/Android/2048-crash-admob-test.apk"
    ;;
  *)
    echo "Usage: $0 [aab|apk|admob-test]" >&2
    exit 64
    ;;
esac

if [[ "$build_format" != "admob-test" && -f "$signing_env" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$signing_env"
  set +a
fi

required_env=(
  MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PATH
  MANNLAB_2048_CRASH_ANDROID_KEYSTORE_PASS
  MANNLAB_2048_CRASH_ANDROID_KEYALIAS_NAME
  MANNLAB_2048_CRASH_ANDROID_KEYALIAS_PASS
)

if [[ "$build_format" != "admob-test" ]]; then
  for env_name in "${required_env[@]}"; do
    if [[ -z "${!env_name:-}" ]]; then
      echo "Missing required signing env: $env_name" >&2
      echo "Create $signing_env or export the signing variables before running this script." >&2
      exit 65
    fi
  done
fi

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  exit 1
fi

android_engine="$(dirname "$(dirname "$unity_root")")/PlaybackEngines/AndroidPlayer"
if [[ ! -d "$android_engine" ]]; then
  echo "Unity Android Build Support is missing: $android_engine" >&2
  exit 1
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  exit 1
fi

license_state="$("$unity_cli" license --json 2>/dev/null || true)"
if ! python3 -c 'import json,sys; sys.exit(0 if len(json.load(sys.stdin)["data"]) > 0 else 1)' <<< "$license_state"; then
  echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod "$build_method" \
  -logFile "$build_log"

test -f "$artifact"

echo "Android build log: $build_log"
echo "Android $build_format verified: $artifact"
