#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/2048-crash"
doc="$repo_root/docs/2048-crash-app-store-prep.md"
privacy_source="$repo_root/web/mannlab-games/src/main.jsx"
icon="$project/Assets/_Project/Art/AppStore/AppIcon-1024.png"
plist="$project/Assets/GoogleService-Info.plist"
upload_dir="$project/Assets/_Project/Art/AppStore/Upload"
failures=0

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    failures=1
  fi
}

require_dir() {
  local path="$1"
  if [[ ! -d "$path" ]]; then
    echo "Missing directory: $path" >&2
    failures=1
  fi
}

require_text() {
  local path="$1"
  local pattern="$2"
  if ! grep -Fq "$pattern" "$path"; then
    echo "Missing expected text in $path: $pattern" >&2
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

require_file "$doc"
require_file "$privacy_source"
require_file "$icon"
require_file "$plist"
require_file "$project/Assets/_Project/Editor/BuildIosXcode.cs"
require_file "$project/Assets/_Project/Scripts/FirebaseTelemetry.cs"
require_dir "$project/Assets/Firebase"
require_dir "$project/Assets/Plugins/iOS/Firebase"

require_text "$doc" "Bundle ID: \`com.mannlab.games.game2048crash\`"
require_text "$doc" "Firebase Analytics and Firebase Crashlytics"
require_text "$doc" "Google AdMob"
require_text "$doc" "2048 Crash - App Review Information"
require_text "$doc" "Screen recording link: [ADD REVIEW-ACCESSIBLE LINK]"
require_text "$doc" "Devices and operating systems tested"
require_text "$doc" "The app functions consistently across all regions."
require_text "$doc" "The app does not operate in a highly regulated industry"
require_text "$privacy_source" "2048 Crash"
require_text "$privacy_source" "Firebase Analytics"
require_text "$privacy_source" "Firebase Crashlytics"
require_text "$privacy_source" "Google AdMob"
require_text "$plist" "<string>com.mannlab.games.game2048crash</string>"
require_text "$plist" "<string>crash-6508f</string>"

require_png_size "$icon" 1024 1024

require_file "$upload_dir.meta"
for screenshot in \
  "01-start-board.png" \
  "02-after-first-slides.png" \
  "03-building-the-crash.png" \
  "04-late-board.png"; do
  require_file "$upload_dir/iPhone-6.9.meta"
  require_file "$upload_dir/iPhone-6.5.meta"
  require_file "$upload_dir/iPad-13.meta"
  require_png_size "$upload_dir/iPhone-6.9/$screenshot" 1320 2868
  require_file "$upload_dir/iPhone-6.9/$screenshot.meta"
  require_png_size "$upload_dir/iPhone-6.5/$screenshot" 1284 2778
  require_file "$upload_dir/iPhone-6.5/$screenshot.meta"
  require_png_size "$upload_dir/iPad-13/$screenshot" 2064 2752
  require_file "$upload_dir/iPad-13/$screenshot.meta"
done

if [[ "$failures" -ne 0 ]]; then
  echo "2048 Crash App Store readiness check failed." >&2
  exit 1
fi

echo "2048 Crash App Store readiness assets verified."
