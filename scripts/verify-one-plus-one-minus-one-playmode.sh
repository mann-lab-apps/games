#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
project_version="$(awk '/m_EditorVersion:/ {print $2; exit}' "$project/ProjectSettings/ProjectVersion.txt")"
unity_editor="/Applications/Unity/Hub/Editor/${UNITY_EDITOR_VERSION:-$project_version}/Unity.app/Contents/MacOS/Unity"
results="${ONE_EQUALS_ONE_PLAYMODE_RESULTS:-/tmp/one-equals-one-playmode.xml}"
log="${ONE_EQUALS_ONE_PLAYMODE_LOG:-/tmp/one-equals-one-playmode.log}"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor not found: $unity_editor" >&2
  exit 2
fi

"$unity_editor" -batchmode -projectPath "$project" \
  -runTests -testPlatform PlayMode -testFilter MannLab.Games.OnePlusOneMinusOne.Tests \
  -testResults "$results" -logFile "$log"

python3 - "$results" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
total, passed = int(root.get("total", "0")), int(root.get("passed", "0"))
if total == 0 or passed != total or root.get("result") != "Passed":
    raise SystemExit(f"PlayMode verification failed: {passed}/{total} passed")
print(f"1 = 1 PlayMode verification passed: {passed}/{total}; {sys.argv[1]}")
PY
