#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

mkdir -p \
  "$tmp_dir/scripts" \
  "$tmp_dir/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one" \
  "$tmp_dir/prototypes/one-plus-one-minus-one/Assets/_Project" \
  "$tmp_dir/prototypes/one-plus-one-minus-one/Assets/Plugins" \
  "$tmp_dir/prototypes/one-plus-one-minus-one/ProjectSettings" \
  "$tmp_dir/prototypes/one-plus-one-minus-one/Packages"

cp "$repo_root/scripts/verify-one-plus-one-minus-one-ship-ready.sh" "$tmp_dir/scripts/verify-one-plus-one-minus-one-ship-ready.sh"

index="$tmp_dir/prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one/index.html"
printf '<!doctype html><title>fixture</title>\n' > "$index"
touch -t 202001010000 "$index"
printf 'newer source\n' > "$tmp_dir/prototypes/one-plus-one-minus-one/Assets/_Project/NewerSource.txt"
touch -t 202601010000 "$tmp_dir/prototypes/one-plus-one-minus-one/Assets/_Project/NewerSource.txt"

for script_name in \
  verify-one-plus-one-minus-one-static.sh \
  check-one-plus-one-minus-one-unity-license.sh \
  verify-one-plus-one-minus-one-playmode.sh \
  verify-one-plus-one-minus-one-webgl.sh \
  verify-one-plus-one-minus-one-webgl-shells.sh \
  verify-one-plus-one-minus-one-icon.sh \
  smoke-one-plus-one-minus-one-webgl-viewports.mjs \
  verify-one-plus-one-minus-one-qa-captures.sh \
  verify-one-plus-one-minus-one-app-store-candidates.sh \
  verify-one-plus-one-minus-one-device-qa-signoff.sh \
  verify-one-plus-one-minus-one-release-env.sh \
  verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh \
  verify-one-plus-one-minus-one-ios-readiness.sh \
  verify-one-plus-one-minus-one-android-readiness.sh
do
  cat > "$tmp_dir/scripts/$script_name" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

: "${ONE_EQUALS_ONE_TEST_LOG:?}"
printf '%s %s\n' "$(basename "$0")" "$*" >> "$ONE_EQUALS_ONE_TEST_LOG"

if [[ "$(basename "$0")" == "check-one-plus-one-minus-one-unity-license.sh" ]]; then
  if [[ "${ONE_EQUALS_ONE_FAKE_UNITY_LICENSE_AVAILABLE:-0}" == "1" ]]; then
    echo "fake Unity licensing available"
    exit 0
  else
    echo "fake Unity licensing unavailable" >&2
    exit 2
  fi
fi
EOF
  chmod +x "$tmp_dir/scripts/$script_name"
done

log="$tmp_dir/invocations.log"
output="$tmp_dir/ship-ready.out"

set +e
ONE_EQUALS_ONE_TEST_LOG="$log" "$tmp_dir/scripts/verify-one-plus-one-minus-one-ship-ready.sh" > "$output" 2>&1
status=$?
set -e

if [[ "$status" -ne 2 ]]; then
  echo "Expected ship-ready fixture to fail with status 2, got $status" >&2
  cat "$output" >&2
  exit 1
fi

require_output() {
  local pattern="$1"
  if ! grep -Fq -- "$pattern" "$output"; then
    echo "Missing expected ship-ready output: $pattern" >&2
    cat "$output" >&2
    exit 1
  fi
}

reject_output() {
  local pattern="$1"
  if grep -Fq -- "$pattern" "$output"; then
    echo "Unexpected ship-ready output: $pattern" >&2
    cat "$output" >&2
    exit 1
  fi
}

require_log() {
  local pattern="$1"
  if ! grep -Fq -- "$pattern" "$log"; then
    echo "Missing expected ship-ready invocation: $pattern" >&2
    cat "$log" >&2
    exit 1
  fi
}

reject_log() {
  local pattern="$1"
  if grep -Eq "$pattern" "$log"; then
    echo "Unexpected ship-ready invocation matched: $pattern" >&2
    cat "$log" >&2
    exit 1
  fi
}

require_output "FAIL: Unity licensing preflight"
require_output "SKIP: PlayMode input and progress regressions (Unity licensing preflight failed)"
require_output "SKIP: fresh WebGL build and smoke (Unity licensing preflight failed)"
require_output "FAIL: WebGL artifact freshness"
require_output "SKIP: WebGL viewport smoke (WebGL artifact freshness failed)"
require_output "SKIP: QA key-round/page captures (WebGL artifact freshness failed)"
require_output "SKIP: App Store candidate screenshots (WebGL artifact freshness failed)"
require_output "== strict iOS release preflight =="
require_output "== strict Android release preflight =="
require_output "SKIP: strict iOS release readiness (Unity licensing preflight failed)"
require_output "SKIP: strict Android release readiness (Unity licensing preflight failed)"
require_output "1 = 1 ship-ready blockers:"
require_output "- FAIL: Unity licensing preflight (exit 2)"
require_output "- FAIL: WebGL artifact freshness (exit 1)"
require_output "- SKIP: strict iOS release readiness (Unity licensing preflight failed)"

require_log "verify-one-plus-one-minus-one-static.sh"
require_log "check-one-plus-one-minus-one-unity-license.sh"
require_log "verify-one-plus-one-minus-one-webgl-shells.sh"
require_log "verify-one-plus-one-minus-one-icon.sh"
require_log "verify-one-plus-one-minus-one-device-qa-signoff.sh --strict"
require_log "verify-one-plus-one-minus-one-release-env.sh --strict"
require_log "verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh"
require_log "verify-one-plus-one-minus-one-ios-readiness.sh release --preflight-only"
require_log "verify-one-plus-one-minus-one-android-readiness.sh release --preflight-only"

reject_log '^verify-one-plus-one-minus-one-playmode\.sh'
reject_log '^verify-one-plus-one-minus-one-webgl\.sh'
reject_log '^smoke-one-plus-one-minus-one-webgl-viewports\.mjs'
reject_log '^verify-one-plus-one-minus-one-qa-captures\.sh'
reject_log '^verify-one-plus-one-minus-one-app-store-candidates\.sh'
reject_log '^verify-one-plus-one-minus-one-ios-readiness\.sh release$'
reject_log '^verify-one-plus-one-minus-one-android-readiness\.sh release$'

rm -f "$tmp_dir/prototypes/one-plus-one-minus-one/Assets/_Project/NewerSource.txt"
touch -t 202701010000 "$index"
: > "$log"

set +e
ONE_EQUALS_ONE_TEST_LOG="$log" ONE_EQUALS_ONE_FAKE_UNITY_LICENSE_AVAILABLE=1 \
  "$tmp_dir/scripts/verify-one-plus-one-minus-one-ship-ready.sh" > "$output" 2>&1
status=$?
set -e

if [[ "$status" -ne 0 ]]; then
  echo "Expected ship-ready fixture to pass with fresh artifacts and fake license, got $status" >&2
  cat "$output" >&2
  exit 1
fi

require_output "PASS: Unity licensing preflight"
require_output "PASS: PlayMode input and progress regressions"
require_output "PASS: fresh WebGL build and smoke"
require_output "PASS: WebGL artifact freshness"
require_output "PASS: WebGL viewport smoke"
require_output "PASS: QA key-round/page captures"
require_output "PASS: App Store candidate screenshots"
require_output "PASS: strict iOS release preflight"
require_output "PASS: strict Android release preflight"
require_output "PASS: strict iOS release readiness"
require_output "PASS: strict Android release readiness"
require_output "1 = 1 ship-ready gate passed."
reject_output "ship-ready blockers"

require_log "verify-one-plus-one-minus-one-playmode.sh"
require_log "verify-one-plus-one-minus-one-webgl.sh"
require_log "smoke-one-plus-one-minus-one-webgl-viewports.mjs"
require_log "verify-one-plus-one-minus-one-qa-captures.sh"
require_log "verify-one-plus-one-minus-one-app-store-candidates.sh"
require_log "verify-one-plus-one-minus-one-ios-readiness.sh release --preflight-only"
require_log "verify-one-plus-one-minus-one-android-readiness.sh release --preflight-only"
require_log "verify-one-plus-one-minus-one-ios-readiness.sh release"
require_log "verify-one-plus-one-minus-one-android-readiness.sh release"

echo "1 = 1 ship-ready gate tests passed."
