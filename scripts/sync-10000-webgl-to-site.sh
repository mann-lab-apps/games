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
perl -pi -e 's/canvas\.style\.width = "960px";/canvas.style.width = "100%";/g; s/canvas\.style\.height = "600px";/canvas.style.height = "100%";/g' "$target_dir/index.html"
asset_version="$(shasum -a 256 "$target_dir/Build/10000.wasm" | awk '{ print substr($1, 1, 12) }')"
perl -0pi -e "s/var buildUrl = \"Build\";\\n      var loaderUrl = buildUrl \\+ \"\\/10000\\.loader\\.js\";/var buildUrl = \"Build\";\\n      var assetVersion = \"$asset_version\";\\n      var versionSuffix = \"?v=\" + assetVersion;\\n      var loaderUrl = buildUrl + \"\\/10000.loader.js\" + versionSuffix;/" "$target_dir/index.html"
perl -pi -e 's/(dataUrl: buildUrl \+ "\/10000\.data")/$1 + versionSuffix/; s/(frameworkUrl: buildUrl \+ "\/10000\.framework\.js")/$1 + versionSuffix/; s/(codeUrl: buildUrl \+ "\/10000\.wasm")/$1 + versionSuffix/' "$target_dir/index.html"

rm "$target_dir/Build/10000.data.gz" \
  "$target_dir/Build/10000.framework.js.gz" \
  "$target_dir/Build/10000.wasm.gz"

echo "Synced WebGL site assets: $target_dir"
