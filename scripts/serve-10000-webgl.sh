#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
build_output="$repo_root/prototypes/10000/Builds/WebGL/10000"
host="${HOST:-0.0.0.0}"
port="${PORT:-8080}"

if [[ ! -f "$build_output/index.html" ]]; then
  echo "WebGL build not found: $build_output/index.html" >&2
  echo "Run ./scripts/verify-10000-webgl.sh first." >&2
  exit 2
fi

cd "$build_output"

python3 - "$host" "$port" <<'PY'
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
import sys

host = sys.argv[1]
port = int(sys.argv[2])

class UnityWebGLHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(".wasm") or path.endswith(".wasm.gz"):
            return "application/wasm"
        if path.endswith(".js") or path.endswith(".js.gz"):
            return "application/javascript"
        if path.endswith(".data") or path.endswith(".data.gz"):
            return "application/octet-stream"
        return super().guess_type(path)

    def end_headers(self):
        path = self.path.split("?", 1)[0]
        if path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

print(f"Serving Unity WebGL build on http://{host}:{port}")
ThreadingHTTPServer((host, port), UnityWebGLHandler).serve_forever()
PY
