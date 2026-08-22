#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/rainwalker"
unity_editor="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"
unity_cli="${HOME}/.unity/bin/unity"
webgl_engine="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/WebGLSupport"
build_log="/tmp/rainwalker-unity-webgl-build.log"
build_output="$project/Builds/WebGL/rainwalker"
missing=0

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  missing=1
fi

if [[ ! -d "$webgl_engine" ]]; then
  echo "Unity WebGL Build Support is not installed: $webgl_engine" >&2
  echo "Install it from Unity Hub > Installs > 6000.3.22f1 > Add modules > Web Build Support." >&2
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

if [[ "$missing" -ne 0 ]]; then
  exit 2
fi

"$unity_editor" \
  -batchmode \
  -quit \
  -projectPath "$project" \
  -executeMethod MannLab.Games.Rainwalker.EditorTools.BuildWebGL.Build \
  -logFile "$build_log"

test -f "$build_output/index.html"
test -d "$build_output/Build"

echo "WebGL build log: $build_log"
echo "WebGL build verified: $build_output"
