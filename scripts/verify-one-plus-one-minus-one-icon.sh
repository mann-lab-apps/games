#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
icon="$project/Assets/_Project/Art/AppIcon-1024.png"
webgl_icon="$project/Builds/WebGL/one-plus-one-minus-one/app-icon.png"
webgl_qa_icon="$project/Builds/WebGL/one-plus-one-minus-one-qa/app-icon.png"
webgl_store_capture_icon="$project/Builds/WebGL/one-plus-one-minus-one-store-capture/app-icon.png"
generator="$project/Assets/_Project/Editor/GenerateAppIcon.cs"
failures=0

require_icon() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing icon: $path" >&2
    failures=1
    return
  fi

  local width
  local height
  local alpha
  width="$(sips -g pixelWidth "$path" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
  height="$(sips -g pixelHeight "$path" 2>/dev/null | awk '/pixelHeight/ {print $2}')"
  alpha="$(sips -g hasAlpha "$path" 2>/dev/null | awk '/hasAlpha/ {print $2}')"

  if [[ "$width" != "1024" || "$height" != "1024" ]]; then
    echo "Unexpected icon size for $path: ${width:-?}x${height:-?}" >&2
    failures=1
  fi

  if [[ "$alpha" == "yes" ]]; then
    echo "Icon must not have an alpha channel for store upload: $path" >&2
    failures=1
  fi
}

require_icon "$icon"
for generated_icon in "$webgl_icon" "$webgl_qa_icon" "$webgl_store_capture_icon"; do
  if [[ -f "$generated_icon" ]]; then
    require_icon "$generated_icon"
    if ! cmp -s "$icon" "$generated_icon"; then
      echo "Generated app icon differs from source icon; rebuild after regenerating the icon: $generated_icon" >&2
      failures=1
    fi
  fi
done

if ! grep -Fq "TextureFormat.RGB24" "$generator"; then
  echo "GenerateAppIcon should create an RGB24 texture so the source PNG has no alpha channel." >&2
  failures=1
fi

if ! "$repo_root/scripts/verify-one-plus-one-minus-one-icon-visuals.mjs"; then
  failures=1
fi

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 icon verification failed." >&2
  exit 1
fi

echo "1 = 1 icon verified without alpha and with small-size visual contrast: $icon"
