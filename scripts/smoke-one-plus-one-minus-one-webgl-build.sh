#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
build_output="$repo_root/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one"
missing=0
warnings=0

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

require_file "$build_output/index.html"
require_file "$build_output/app-icon.png"
require_file "$build_output/Build/one-plus-one-minus-one.loader.js"
require_file "$build_output/Build/one-plus-one-minus-one.data"
require_file "$build_output/Build/one-plus-one-minus-one.framework.js"
require_file "$build_output/Build/one-plus-one-minus-one.wasm"
require_text "$build_output/index.html" "<title>1 = 1</title>"
require_text "$build_output/index.html" "app-icon.png"
require_text "$build_output/index.html" "buildVersion"

if [[ -f "$build_output/index.html" ]]; then
  newer_source="$(
    find \
      "$repo_root/prototypes/one-plus-one-minus-one/Assets/_Project" \
      "$repo_root/prototypes/one-plus-one-minus-one/ProjectSettings" \
      "$repo_root/prototypes/one-plus-one-minus-one/Packages" \
      -type f \
      -newer "$build_output/index.html" \
      | head -1 || true
  )"
  if [[ -n "$newer_source" ]]; then
    echo "Warning: WebGL build is older than source file: ${newer_source#$repo_root/}" >&2
    warnings=1
  fi
fi

if command -v sips >/dev/null 2>&1 && [[ -f "$build_output/app-icon.png" ]]; then
  width="$(sips -g pixelWidth "$build_output/app-icon.png" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
  height="$(sips -g pixelHeight "$build_output/app-icon.png" 2>/dev/null | awk '/pixelHeight/ {print $2}')"
  if [[ "$width" != "1024" || "$height" != "1024" ]]; then
    echo "Unexpected app icon size: ${width:-?}x${height:-?}" >&2
    missing=1
  fi
fi

if [[ "$missing" -ne 0 ]]; then
  echo "1 = 1 existing WebGL build smoke failed." >&2
  exit 1
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "1 = 1 existing WebGL build smoke passed with freshness warnings: $build_output"
else
  echo "1 = 1 existing WebGL build smoke passed: $build_output"
fi
