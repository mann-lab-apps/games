#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
store_capture_build="$project/Builds/WebGL/one-plus-one-minus-one-store-capture"
candidate_dir="${ONE_EQUALS_ONE_APP_STORE_CANDIDATE_DIR:-$repo_root/prototypes/one-plus-one-minus-one/Builds/AppStoreScreenshots/Candidates}"
failures=0

check_store_capture_build() {
  if [[ ! -f "$store_capture_build/index.html" ]]; then
    echo "Missing WebGL store-capture build: $store_capture_build" >&2
    failures=1
    return
  fi

  if grep -Fq "Development Build" "$store_capture_build/index.html"; then
    echo "Store-capture WebGL shell must not be a Development Build: $store_capture_build/index.html" >&2
    failures=1
  fi

  local newer_source
  newer_source="$(
    find \
      "$project/Assets/_Project" \
      "$project/ProjectSettings" \
      "$project/Packages" \
      -type f \
      ! -path "$project/Assets/_Project/Scenes/Game.unity" \
      ! -path "$project/ProjectSettings/ProjectSettings.asset" \
      -newer "$store_capture_build/index.html" \
      | head -1 || true
  )"
  if [[ -n "$newer_source" ]]; then
    echo "Store-capture build is stale; newer source exists: ${newer_source#$repo_root/}" >&2
    failures=1
  fi
}

require_png() {
  local path="$1"
  local expected_width="$2"
  local expected_height="$3"
  if [[ ! -f "$path" ]]; then
    echo "Missing App Store candidate screenshot: $path" >&2
    failures=1
    return
  fi

  if [[ -f "$store_capture_build/index.html" && "$store_capture_build/index.html" -nt "$path" ]]; then
    echo "App Store candidate screenshot is older than the store-capture build; recapture it: $path" >&2
    failures=1
  fi

  local width
  local height
  width="$(sips -g pixelWidth "$path" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
  height="$(sips -g pixelHeight "$path" 2>/dev/null | awk '/pixelHeight/ {print $2}')"
  if [[ "$width" != "$expected_width" || "$height" != "$expected_height" ]]; then
    echo "Unexpected screenshot size for $path: ${width:-?}x${height:-?}, expected ${expected_width}x${expected_height}" >&2
    failures=1
  fi

  local alpha
  alpha="$(sips -g hasAlpha "$path" 2>/dev/null | awk '/hasAlpha/ {print $2}')"
  if [[ "$alpha" == "yes" ]]; then
    echo "PNG must not have an alpha channel for App Store upload: $path" >&2
    failures=1
  fi
}

shots=(
  "01-round-1-first-stick"
  "02-round-5-cross-multiply"
  "03-round-8-triple-one"
  "04-round-9-star-multiply"
  "05-round-30-medium-expression"
  "06-round-75-equality-puzzle"
  "07-round-100-finale"
  "08-round-select-progression"
)

check_store_capture_build

if [[ -d "$candidate_dir" ]]; then
  while IFS= read -r candidate; do
    candidate_name="$(basename "$candidate")"
    expected=0
    for shot in "${shots[@]}"; do
      if [[ "$candidate_name" == "$shot" ]]; then
        expected=1
        break
      fi
    done

    if [[ "$expected" -eq 0 ]]; then
      echo "Unexpected stale App Store candidate directory: $candidate" >&2
      failures=1
    fi
  done < <(find "$candidate_dir" -mindepth 1 -maxdepth 1 -type d -name '??-*' | sort)
fi

for shot in "${shots[@]}"; do
  require_png "$candidate_dir/$shot/iphone-6-5.png" 1284 2778
  require_png "$candidate_dir/$shot/iphone-6-9.png" 1320 2868
  require_png "$candidate_dir/$shot/ipad-13.png" 2064 2752
done

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 App Store candidate screenshot verification failed." >&2
  exit 1
fi

"$repo_root/scripts/verify-one-plus-one-minus-one-png-visuals.mjs" --app-store

echo "1 = 1 App Store candidate screenshots verified: $candidate_dir"
