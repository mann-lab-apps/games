#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
qa_build="$repo_root/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-qa"
output_root="${ONE_EQUALS_ONE_ROUND_SELECT_CAPTURE_DIR:-/tmp/one-equals-one-round-select-pages}"
if [[ "$#" -gt 0 ]]; then
  pages=("$@")
else
  pages=(1 5 9)
fi

if [[ ! -f "$qa_build/index.html" ]]; then
  echo "Missing WebGL QA build: $qa_build" >&2
  echo "Run ./scripts/verify-one-plus-one-minus-one-webgl-qa.sh first." >&2
  exit 2
fi

if [[ "$#" -eq 0 ]]; then
  mkdir -p "$output_root"
  find "$output_root" -mindepth 1 -maxdepth 1 -name 'page-*' -exec rm -rf {} +
fi

for page in "${pages[@]}"; do
  page_dir="$output_root/page-$page"
  if [[ "$#" -gt 0 ]]; then
    rm -rf "$page_dir"
  fi

  echo "Capturing round-select page $page into $page_dir"
  ONE_EQUALS_ONE_WEBGL_BUILD_DIR="$qa_build" \
  ONE_EQUALS_ONE_WEBGL_QUERY="?qaRounds=1&qaUnlocked=100&qaRoundPage=$page" \
  ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR="$page_dir" \
    "$repo_root/scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs"
done

echo "1 = 1 round-select viewport captures: $output_root"
