#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
qa_build="$repo_root/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-qa"
output_root="${ONE_EQUALS_ONE_QA_ROUND_CAPTURE_DIR:-/tmp/one-equals-one-webgl-qa-rounds}"
if [[ "$#" -gt 0 ]]; then
  rounds=("$@")
else
  rounds=(1 30 50 75 90 100)
fi

if [[ ! -f "$qa_build/index.html" ]]; then
  echo "Missing WebGL QA build: $qa_build" >&2
  echo "Run ./scripts/verify-one-plus-one-minus-one-webgl-qa.sh first." >&2
  exit 2
fi

for round in "${rounds[@]}"; do
  round_dir="$output_root/round-$round"
  echo "Capturing QA Round $round into $round_dir"
  ONE_EQUALS_ONE_WEBGL_BUILD_DIR="$qa_build" \
  ONE_EQUALS_ONE_WEBGL_QUERY="?qaRound=$round&qaUnlocked=100" \
  ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR="$round_dir" \
    "$repo_root/scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs"
done

echo "1 = 1 QA round viewport captures: $output_root"
