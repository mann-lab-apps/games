#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
failures=0

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    failures=1
  fi
}

require_text() {
  local path="$1"
  local pattern="$2"
  require_file "$path"
  if [[ -f "$path" ]] && ! grep -Fq -- "$pattern" "$path"; then
    echo "Missing expected text in $path: $pattern" >&2
    failures=1
  fi
}

verify_shell() {
  local label="$1"
  local output_dir="$2"
  local build_name="$3"
  local expected_development_state="$4"

  require_file "$output_dir/index.html"
  require_file "$output_dir/app-icon.png"
  require_file "$output_dir/Build/$build_name.loader.js"
  require_file "$output_dir/Build/$build_name.data"
  require_file "$output_dir/Build/$build_name.framework.js"
  require_file "$output_dir/Build/$build_name.wasm"
  require_text "$output_dir/index.html" "<title>1 = 1</title>"
  require_text "$output_dir/index.html" "app-icon.png"
  require_text "$output_dir/index.html" "buildVersion"
  require_text "$output_dir/index.html" "name=\"description\""
  require_text "$output_dir/index.html" "name=\"application-name\" content=\"1 = 1\""
  require_text "$output_dir/index.html" "name=\"apple-mobile-web-app-title\" content=\"1 = 1\""
  require_text "$output_dir/index.html" "name=\"theme-color\" content=\"#fffffc\""
  require_text "$output_dir/index.html" "rel=\"apple-touch-icon\" href=\"app-icon.png\""

  if [[ -f "$output_dir/index.html" ]]; then
    if [[ "$expected_development_state" == "development" ]]; then
      require_text "$output_dir/index.html" "TemplateData/profiler.js"
      require_text "$output_dir/index.html" "unityProfiler.createButton"
    elif grep -Eq "Development Build|TemplateData/profiler\\.js|unityProfiler\\.createButton" "$output_dir/index.html"; then
      echo "$label WebGL shell should not contain development/profiler markers." >&2
      failures=1
    fi
  fi
}

verify_shell \
  "Release" \
  "$project/Builds/WebGL/one-plus-one-minus-one" \
  "one-plus-one-minus-one" \
  "release"

verify_shell \
  "QA" \
  "$project/Builds/WebGL/one-plus-one-minus-one-qa" \
  "one-plus-one-minus-one-qa" \
  "development"

verify_shell \
  "Store-capture" \
  "$project/Builds/WebGL/one-plus-one-minus-one-store-capture" \
  "one-plus-one-minus-one-store-capture" \
  "release"

"$repo_root/scripts/verify-one-plus-one-minus-one-icon.sh"

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 WebGL shell verification failed." >&2
  exit 1
fi

echo "1 = 1 WebGL shells verified: release, QA, store-capture."
