#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
qa_build="$project/Builds/WebGL/one-plus-one-minus-one-qa"
round_capture_root="${ONE_EQUALS_ONE_QA_ROUND_CAPTURE_DIR:-/tmp/one-equals-one-webgl-qa-rounds}"
round_select_root="${ONE_EQUALS_ONE_ROUND_SELECT_CAPTURE_DIR:-/tmp/one-equals-one-round-select-pages}"
failures=0

devices=(
  "iphone-se:640:1136"
  "iphone-standard:1170:2532"
  "iphone-large:1290:2796"
  "android-20x9:1133:2516"
  "desktop:1440:1024"
)
rounds=(1 30 50 75 90 100)
pages=(1 5 9)

if [[ ! -f "$qa_build/index.html" ]]; then
  echo "Missing WebGL QA build: $qa_build" >&2
  exit 2
fi

newer_source="$(
  find \
    "$project/Assets/_Project" \
    "$project/ProjectSettings" \
    "$project/Packages" \
    -type f \
    -newer "$qa_build/index.html" \
    | head -1 || true
)"
if [[ -n "$newer_source" ]]; then
  echo "WebGL QA build is stale; newer source exists: ${newer_source#$repo_root/}" >&2
  failures=1
fi

require_png() {
  local path="$1"
  local expected_width="$2"
  local expected_height="$3"
  if [[ ! -f "$path" ]]; then
    echo "Missing QA capture: $path" >&2
    failures=1
    return
  fi

  if [[ "$qa_build/index.html" -nt "$path" ]]; then
    echo "QA capture is older than the QA build; recapture it: $path" >&2
    failures=1
  fi

  local width
  local height
  width="$(sips -g pixelWidth "$path" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
  height="$(sips -g pixelHeight "$path" 2>/dev/null | awk '/pixelHeight/ {print $2}')"
  if [[ "$width" != "$expected_width" || "$height" != "$expected_height" ]]; then
    echo "Unexpected QA capture size for $path: ${width:-?}x${height:-?}, expected ${expected_width}x${expected_height}" >&2
    failures=1
  fi
}

for round in "${rounds[@]}"; do
  for device in "${devices[@]}"; do
    IFS=":" read -r name width height <<< "$device"
    require_png "$round_capture_root/round-$round/$name.png" "$width" "$height"
  done
done

for page in "${pages[@]}"; do
  for device in "${devices[@]}"; do
    IFS=":" read -r name width height <<< "$device"
    require_png "$round_select_root/page-$page/$name.png" "$width" "$height"
  done
done

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 QA capture verification failed." >&2
  exit 1
fi

echo "1 = 1 QA captures verified:"
echo "- rounds: $round_capture_root"
echo "- round select: $round_select_root"
