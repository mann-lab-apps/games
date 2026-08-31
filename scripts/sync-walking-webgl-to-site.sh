#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_dir="$repo_root/prototypes/walking/Builds/WebGL/walking"
target_dir="$repo_root/web/mannlab-games/public/games/thumbwaddle"

if [[ ! -f "$source_dir/index.html" ]]; then
  cat >&2 <<MSG
Thumbwaddle WebGL build was not found at:
  $source_dir

Build it from Unity with:
  MannLab.Games.Walking.EditorTools.BuildWebGL.Build
MSG
  exit 1
fi

mkdir -p "$target_dir"
rsync -a --delete "$source_dir/" "$target_dir/"

find "$target_dir" -name '*.gz' -print0 | while IFS= read -r -d '' compressed; do
  output="${compressed%.gz}"
  gzip -dc "$compressed" > "$output"
  rm "$compressed"
done

if [[ -f "$target_dir/index.html" ]]; then
  perl -0pi -e 's/\.data\.gz/.data/g; s/\.framework\.js\.gz/.framework.js/g; s/\.wasm\.gz/.wasm/g; s#(var loaderUrl = buildUrl \+ "/walking\.loader\.js")#$1 + "?v=thumbwaddle-20260831c"#g; s#(dataUrl: buildUrl \+ "/walking\.data")#$1 + "?v=thumbwaddle-20260831c"#g; s#(frameworkUrl: buildUrl \+ "/walking\.framework\.js")#$1 + "?v=thumbwaddle-20260831c"#g; s#(codeUrl: buildUrl \+ "/walking\.wasm")#$1 + "?v=thumbwaddle-20260831c"#g; s/canvas\.style\.width = "960px";/canvas.style.width = "100vw";/g; s/canvas\.style\.height = "600px";/canvas.style.height = "100vh";/g' "$target_dir/index.html"
fi

echo "Thumbwaddle WebGL copied to $target_dir."
