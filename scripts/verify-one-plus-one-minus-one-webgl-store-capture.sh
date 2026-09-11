#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
build_output="$project/Builds/WebGL/one-plus-one-minus-one-store-capture"
project_unity_version="$(awk '/m_EditorVersion:/ {print $2; exit}' "$project/ProjectSettings/ProjectVersion.txt")"
unity_version="${UNITY_EDITOR_VERSION:-$project_unity_version}"
unity_editor="/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity"
if [[ ! -x "$unity_editor" ]]; then
  latest_unity_app="$(find /Applications/Unity/Hub/Editor -maxdepth 2 -path '*/Unity.app' -type d 2>/dev/null | sort | tail -1 || true)"
  if [[ -n "$latest_unity_app" ]]; then
    unity_editor="$latest_unity_app/Contents/MacOS/Unity"
  fi
fi

unity_cli="${HOME}/.unity/bin/unity"
build_log="/tmp/one-plus-one-minus-one-unity-webgl-store-capture-build.log"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  exit 2
fi

if [[ ! -x "$unity_cli" ]]; then
  echo "Unity CLI not found: $unity_cli" >&2
  exit 2
fi

license_state="$("$unity_cli" license --json 2>/dev/null || true)"
if ! python3 -c 'import json,sys; data=json.load(sys.stdin).get("data", []); sys.exit(0 if data else 1)' <<< "$license_state"; then
  echo "No Unity Editor license found. Activate a license in Unity Hub before running this script." >&2
  exit 2
fi

"$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildWebGL.BuildStoreCapture \
  -logFile "$build_log"

test -f "$build_output/index.html"
test -f "$build_output/Build/one-plus-one-minus-one-store-capture.loader.js"
test -f "$build_output/Build/one-plus-one-minus-one-store-capture.data"
test -f "$build_output/Build/one-plus-one-minus-one-store-capture.framework.js"
test -f "$build_output/Build/one-plus-one-minus-one-store-capture.wasm"

if grep -Fq "Development Build" "$build_output/index.html"; then
  echo "Store capture WebGL shell should not be a Development Build." >&2
  exit 1
fi

echo "WebGL store-capture build log: $build_log"
echo "WebGL store-capture build verified: $build_output"
