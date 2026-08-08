#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_dir="$repo_root/prototypes/flying-bird/Builds/WebGL/flying-bird"
target_dir="$repo_root/web/mannlab-games/public/games/flying-bird"

if [[ ! -f "$source_dir/index.html" ]]; then
  echo "WebGL build not found: $source_dir/index.html" >&2
  echo "Run ./scripts/verify-flying-bird-webgl.sh first." >&2
  exit 2
fi

mkdir -p "$target_dir"
rsync -a --delete "$source_dir/" "$target_dir/"

for asset in data framework.js wasm; do
  gz_asset="$target_dir/Build/flying-bird.$asset.gz"
  if [[ -f "$gz_asset" ]]; then
    gzip -dc "$gz_asset" > "$target_dir/Build/flying-bird.$asset"
  fi
done

perl -pi -e 's/flying-bird\.data\.gz/flying-bird.data/g; s/flying-bird\.framework\.js\.gz/flying-bird.framework.js/g; s/flying-bird\.wasm\.gz/flying-bird.wasm/g' "$target_dir/index.html"
perl -pi -e 's/canvas\.style\.width = "960px";/canvas.style.width = "100%";/g; s/canvas\.style\.height = "600px";/canvas.style.height = "100%";/g' "$target_dir/index.html"

asset_version="$(shasum -a 256 \
  "$target_dir/Build/flying-bird.data" \
  "$target_dir/Build/flying-bird.framework.js" \
  "$target_dir/Build/flying-bird.loader.js" \
  "$target_dir/Build/flying-bird.wasm" | shasum -a 256 | awk '{ print substr($1, 1, 12) }')"
ASSET_VERSION="$asset_version" REPO_ROOT="$repo_root" TARGET_DIR="$target_dir" node <<'NODE'
const fs = require("fs");
const path = require("path");

const targetDir = process.env.TARGET_DIR;
const repoRoot = process.env.REPO_ROOT;
const assetVersion = process.env.ASSET_VERSION;
const indexPath = path.join(targetDir, "index.html");
const stylePath = path.join(targetDir, "TemplateData", "style.css");
const appSourcePath = path.join(repoRoot, "web", "mannlab-games", "src", "main.jsx");

let html = fs.readFileSync(indexPath, "utf8");
html = html.replace(
  'var buildUrl = "Build";\n      var loaderUrl = buildUrl + "/flying-bird.loader.js";',
  `var buildUrl = "Build";
      var assetVersion = "${assetVersion}";
      var versionSuffix = "?v=" + assetVersion;
      var loaderUrl = buildUrl + "/flying-bird.loader.js" + versionSuffix;`
);
html = html
  .replace('dataUrl: buildUrl + "/flying-bird.data"', 'dataUrl: buildUrl + "/flying-bird.data" + versionSuffix')
  .replace('frameworkUrl: buildUrl + "/flying-bird.framework.js"', 'frameworkUrl: buildUrl + "/flying-bird.framework.js" + versionSuffix')
  .replace('codeUrl: buildUrl + "/flying-bird.wasm"', 'codeUrl: buildUrl + "/flying-bird.wasm" + versionSuffix');
html = html.replace(
  "showBanner: unityShowBanner,",
  `showBanner: unityShowBanner,
        errorHandler: function(message) {
          showRuntimeError(message);
          return true;
        },`
);
html = html.replace(
  "\n      var config = {",
  `
      function showRuntimeError(message) {
        document.querySelector("#unity-loading-bar").style.display = "none";
        var warning = document.querySelector("#unity-warning");
        warning.className = "runtime-error";
        warning.innerHTML = \`<strong>게임을 실행하지 못했어요</strong><span>\${message}</span><small>WebGL이 꺼져 있거나 현재 브라우저 창에서 그래픽 가속을 사용할 수 없을 때 발생합니다. 새 창에서 다시 열거나 브라우저의 그래픽 가속/WebGL 설정을 확인해 주세요.</small><div><button type="button" id="retry-game">다시 시도</button><a href="." target="_blank" rel="noreferrer">새 창에서 열기</a><a href="https://github.com/mann-lab-apps/games/issues/new?title=%5B%EB%82%A0%EA%B0%9C%EC%9E%94%EA%B3%A0%5D%20%ED%94%BC%EB%93%9C%EB%B0%B1%3A%20WebGL%20%EC%8B%A4%ED%96%89%20%EC%98%A4%EB%A5%98" target="_blank" rel="noreferrer">피드백</a></div>\`;
        warning.style.display = "grid";
        document.querySelector("#retry-game").onclick = function () { window.location.reload(); };
      }

      var config = {`
);
html = html.replace("alert(message);", "showRuntimeError(message);");
fs.writeFileSync(indexPath, html);

let appSource = fs.readFileSync(appSourcePath, "utf8");
appSource = appSource.replace(
  /embedHref: "\/games\/flying-bird\/index\.html(?:\?v=[a-f0-9]+)?"/,
  `embedHref: "/games/flying-bird/index.html?v=${assetVersion}"`
);
fs.writeFileSync(appSourcePath, appSource);

const runtimeErrorCss = `

#unity-warning.runtime-error {
  inset: 0;
  left: 0;
  top: 0;
  transform: none;
  place-content: center;
  justify-items: center;
  gap: 12px;
  padding: 32px;
  background: #f8f5eb;
  color: #27241f;
  font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  text-align: center;
}

#unity-warning.runtime-error strong {
  font-size: 24px;
}

#unity-warning.runtime-error span {
  max-width: 520px;
  font-size: 16px;
  font-weight: 800;
}

#unity-warning.runtime-error small {
  max-width: 520px;
  color: #665f52;
  font-size: 13px;
  line-height: 1.5;
}

#unity-warning.runtime-error div {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
}

#unity-warning.runtime-error a,
#unity-warning.runtime-error button {
  min-height: 36px;
  padding: 8px 12px;
  border: 2px solid #27241f;
  border-radius: 999px;
  background: #fdf9ed;
  color: #27241f;
  font: inherit;
  font-size: 13px;
  font-weight: 800;
  text-decoration: none;
  cursor: pointer;
}
`;
fs.appendFileSync(stylePath, runtimeErrorCss);
NODE

rm -f "$target_dir/Build/flying-bird.data.gz" \
  "$target_dir/Build/flying-bird.framework.js.gz" \
  "$target_dir/Build/flying-bird.wasm.gz"

echo "Synced WebGL site assets: $target_dir"
