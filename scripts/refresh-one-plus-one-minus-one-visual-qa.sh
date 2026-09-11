#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "== Rebuild development QA WebGL =="
"$repo_root/scripts/verify-one-plus-one-minus-one-webgl-qa.sh"

echo "== Capture key QA rounds =="
"$repo_root/scripts/capture-one-plus-one-minus-one-webgl-qa-rounds.sh"

echo "== Capture round-select pages =="
"$repo_root/scripts/capture-one-plus-one-minus-one-round-select-pages.sh"

echo "== Verify QA captures =="
"$repo_root/scripts/verify-one-plus-one-minus-one-qa-captures.sh"

echo "== Rebuild non-development store-capture WebGL =="
"$repo_root/scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh"

echo "== Capture App Store candidates =="
"$repo_root/scripts/capture-one-plus-one-minus-one-app-store-candidates.sh"

echo "== Verify App Store candidates =="
"$repo_root/scripts/verify-one-plus-one-minus-one-app-store-candidates.sh"

echo "1 = 1 visual QA capture refresh completed."
