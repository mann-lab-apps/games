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
missing=0

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    missing=1
  fi
}

require_text() {
  local path="$1"
  local pattern="$2"
  require_file "$path"
  if [[ -f "$path" ]] && ! grep -Fq -- "$pattern" "$path"; then
    echo "Missing expected text in $path: $pattern" >&2
    missing=1
  fi
}

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

require_file "$build_output/index.html"
require_file "$build_output/app-icon.png"
require_file "$build_output/Build/one-plus-one-minus-one-store-capture.loader.js"
require_file "$build_output/Build/one-plus-one-minus-one-store-capture.data"
require_file "$build_output/Build/one-plus-one-minus-one-store-capture.framework.js"
require_file "$build_output/Build/one-plus-one-minus-one-store-capture.wasm"
require_text "$build_output/index.html" "<title>1 = 1</title>"
require_text "$build_output/index.html" "app-icon.png"
require_text "$build_output/index.html" "buildVersion"
require_text "$build_output/index.html" "name=\"description\""
require_text "$build_output/index.html" "name=\"application-name\" content=\"1 = 1\""
require_text "$build_output/index.html" "name=\"apple-mobile-web-app-title\" content=\"1 = 1\""
require_text "$build_output/index.html" "name=\"theme-color\" content=\"#fffffc\""
require_text "$build_output/index.html" "rel=\"apple-touch-icon\" href=\"app-icon.png\""

if grep -Fq "Development Build" "$build_output/index.html"; then
  echo "Store capture WebGL shell should not be a Development Build." >&2
  missing=1
fi

"$repo_root/scripts/verify-one-plus-one-minus-one-icon.sh"

if [[ "$missing" -ne 0 ]]; then
  echo "1 = 1 WebGL store-capture build verification failed." >&2
  exit 1
fi

echo "WebGL store-capture build log: $build_log"
echo "WebGL store-capture build verified: $build_output"
