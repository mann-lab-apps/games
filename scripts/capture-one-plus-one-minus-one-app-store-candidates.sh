#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
store_capture_build="$repo_root/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-store-capture"
output_root="${ONE_EQUALS_ONE_APP_STORE_CANDIDATE_DIR:-$repo_root/prototypes/one-plus-one-minus-one/Builds/AppStoreScreenshots/Candidates}"

if [[ ! -f "$store_capture_build/index.html" ]]; then
  echo "Missing WebGL store-capture build: $store_capture_build" >&2
  echo "Run ./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh first." >&2
  exit 2
fi

prepare_output_root() {
  mkdir -p "$output_root"
  find "$output_root" -mindepth 1 -maxdepth 1 -name '??-*' -exec rm -rf {} +
}

capture_shot() {
  local slug="$1"
  local query="$2"
  local shot_dir="$output_root/$slug"
  echo "Capturing App Store candidate $slug into $shot_dir"
  ONE_EQUALS_ONE_WEBGL_BUILD_DIR="$store_capture_build" \
  ONE_EQUALS_ONE_WEBGL_QUERY="$query" \
  ONE_EQUALS_ONE_VIEWPORT_SET="store" \
  ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR="$shot_dir" \
    "$repo_root/scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs"
}

prepare_output_root
capture_shot "01-round-1-first-stick" "?qaRound=1&qaUnlocked=30&qaFillSample=1"
capture_shot "02-round-4-star-start" "?qaRound=4&qaUnlocked=30&qaFillSample=1"
capture_shot "03-round-10-title-echo" "?qaRound=10&qaUnlocked=30&qaFillSample=1"
capture_shot "04-round-15-tall-fold" "?qaRound=15&qaUnlocked=30&qaFillSample=1"
capture_shot "05-round-20-narrow-path" "?qaRound=20&qaUnlocked=30&qaFillSample=1"
capture_shot "06-round-25-wide-loop" "?qaRound=25&qaUnlocked=30&qaFillSample=1"
capture_shot "07-round-30-finale" "?qaRound=30&qaUnlocked=30&qaFillSample=1"
capture_shot "08-round-select-progression" "?qaRounds=1&qaUnlocked=30&qaRoundPage=3"

echo "1 = 1 App Store candidate screenshots: $output_root"
