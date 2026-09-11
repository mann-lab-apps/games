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
webgl_engine="$(dirname "$(dirname "$unity_root")")/PlaybackEngines/WebGLSupport"
build_log="/tmp/one-plus-one-minus-one-unity-webgl-qa-build.log"
build_output="$project/Builds/WebGL/one-plus-one-minus-one-qa"
build_name="$(basename "$build_output")"
missing=0

"$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$webgl_engine" ]]; then
  echo "Unity WebGL Build Support is not installed: $webgl_engine" >&2
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

if [[ "$missing" -ne 0 ]]; then
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildWebGL.BuildDevelopmentQa \
  -logFile "$build_log"

test -f "$build_output/index.html"
test -f "$build_output/app-icon.png"
test -f "$build_output/Build/$build_name.loader.js"
test -f "$build_output/Build/$build_name.data"
test -f "$build_output/Build/$build_name.framework.js"
test -f "$build_output/Build/$build_name.wasm"

echo "WebGL QA build log: $build_log"
echo "WebGL QA build verified: $build_output"
