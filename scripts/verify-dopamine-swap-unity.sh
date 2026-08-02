#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/dopamine-swap"
unity_editor="/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity"
unity_cli="${HOME}/.unity/bin/unity"
import_log="/tmp/dopamine-swap-unity-import.log"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
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
  -executeMethod MannLab.Games.DopamineSwap.EditorTools.CreateGameScene.Create \
  -logFile "$import_log"

test -f "$project/Assets/_Project/Scenes/Game.unity"

echo "Unity import log: $import_log"
echo "Dopamine Swap scene verified: $project/Assets/_Project/Scenes/Game.unity"
