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
build_log="/tmp/one-plus-one-minus-one-unity-webgl-build.log"
build_output="$project/Builds/WebGL/one-plus-one-minus-one"
missing=0

if command -v node >/dev/null 2>&1; then
  node "$repo_root/scripts/verify-one-plus-one-minus-one-rounds.mjs"
else
  echo "Warning: node is not available; skipping static round verification." >&2
fi

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
  -executeMethod MannLab.Games.OnePlusOneMinusOne.EditorTools.BuildWebGL.Build \
  -logFile "$build_log"

require_file "$build_output/index.html"
require_file "$build_output/app-icon.png"
require_file "$build_output/Build/one-plus-one-minus-one.loader.js"
require_file "$build_output/Build/one-plus-one-minus-one.data"
require_file "$build_output/Build/one-plus-one-minus-one.framework.js"
require_file "$build_output/Build/one-plus-one-minus-one.wasm"
require_text "$build_output/index.html" "<title>1 = 1</title>"
require_text "$build_output/index.html" "app-icon.png"
require_text "$build_output/index.html" "buildVersion"

if command -v sips >/dev/null 2>&1; then
  width="$(sips -g pixelWidth "$build_output/app-icon.png" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
  height="$(sips -g pixelHeight "$build_output/app-icon.png" 2>/dev/null | awk '/pixelHeight/ {print $2}')"
  if [[ "$width" != "1024" || "$height" != "1024" ]]; then
    echo "Unexpected app icon size: ${width:-?}x${height:-?}" >&2
    missing=1
  fi
fi

if [[ "$missing" -ne 0 ]]; then
  echo "1 = 1 WebGL smoke failed." >&2
  exit 1
fi

echo "WebGL build log: $build_log"
echo "WebGL smoke verified: $build_output"
