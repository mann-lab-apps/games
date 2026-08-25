#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/standing"
icon="$project/Assets/_Project/Art/AppStore/AppIcon-1024.png"
upload_dir="$project/Assets/_Project/Art/AppStore/Upload"
failures=0

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    failures=1
  fi
}

require_png_size() {
  local path="$1"
  local width="$2"
  local height="$3"
  require_file "$path"
  if [[ ! -f "$path" ]]; then
    return
  fi

  local metadata
  metadata="$(sips -g pixelWidth -g pixelHeight -g hasAlpha "$path" 2>/dev/null || true)"
  if ! grep -Fq "pixelWidth: $width" <<< "$metadata"; then
    echo "Unexpected PNG width for $path; expected $width" >&2
    failures=1
  fi
  if ! grep -Fq "pixelHeight: $height" <<< "$metadata"; then
    echo "Unexpected PNG height for $path; expected $height" >&2
    failures=1
  fi
  if ! grep -Fq "hasAlpha: no" <<< "$metadata"; then
    echo "PNG must not have an alpha channel for App Store upload: $path" >&2
    failures=1
  fi
}

require_png_size "$icon" 1024 1024
require_file "$icon.meta"
require_file "$upload_dir.meta"

for screenshot in \
  "01-watch-the-counter.png" \
  "02-sneak-a-sit.png" \
  "03-customer-saw-you.png"; do
  require_file "$upload_dir/iPhone-6.5.meta"
  require_file "$upload_dir/iPad-13.meta"
  require_png_size "$upload_dir/iPhone-6.5/$screenshot" 1242 2688
  require_file "$upload_dir/iPhone-6.5/$screenshot.meta"
  require_png_size "$upload_dir/iPad-13/$screenshot" 2064 2752
  require_file "$upload_dir/iPad-13/$screenshot.meta"
done

if [[ "$failures" -ne 0 ]]; then
  echo "Standing App Store readiness check failed." >&2
  exit 1
fi

echo "Standing App Store screenshot assets verified."
