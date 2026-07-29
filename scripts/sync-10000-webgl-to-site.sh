#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_dir="$repo_root/prototypes/10000/Builds/WebGL/10000"
target_dir="$repo_root/web/mannlab-games/public/games/10000"

if [[ ! -f "$source_dir/index.html" ]]; then
  echo "WebGL build not found: $source_dir/index.html" >&2
  echo "Run ./scripts/verify-10000-webgl.sh first." >&2
  exit 2
fi

mkdir -p "$target_dir"
rsync -a --delete "$source_dir/" "$target_dir/"

gzip -dc "$target_dir/Build/10000.data.gz" > "$target_dir/Build/10000.data"
gzip -dc "$target_dir/Build/10000.framework.js.gz" > "$target_dir/Build/10000.framework.js"
gzip -dc "$target_dir/Build/10000.wasm.gz" > "$target_dir/Build/10000.wasm"

perl -pi -e 's/10000\.data\.gz/10000.data/g; s/10000\.framework\.js\.gz/10000.framework.js/g; s/10000\.wasm\.gz/10000.wasm/g' "$target_dir/index.html"

rm "$target_dir/Build/10000.data.gz" \
  "$target_dir/Build/10000.framework.js.gz" \
  "$target_dir/Build/10000.wasm.gz"

echo "Synced WebGL site assets: $target_dir"
