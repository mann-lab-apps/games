#!/usr/bin/env bash
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
build_output="$project/Builds/WebGL/one-plus-one-minus-one"
failures=0

run_check() {
  local name="$1"
  shift
  echo "== $name =="
  "$@"
  local status=$?
  if [[ "$status" -eq 0 ]]; then
    echo "PASS: $name"
    return
  fi

  echo "FAIL: $name (exit $status)" >&2
  failures=1
}

check_fresh_webgl_artifact() {
  if [[ ! -f "$build_output/index.html" ]]; then
    echo "Missing WebGL index.html: $build_output/index.html" >&2
    return 1
  fi

  local newer_source
  newer_source="$(
    find \
      "$project/Assets/_Project" \
      "$project/ProjectSettings" \
      "$project/Packages" \
      -type f \
      -newer "$build_output/index.html" \
      | head -1 || true
  )"
  if [[ -n "$newer_source" ]]; then
    echo "WebGL build is stale; newer source exists: ${newer_source#$repo_root/}" >&2
    return 1
  fi
}

run_check "static suite" "$repo_root/scripts/verify-one-plus-one-minus-one-static.sh"
run_check "fresh WebGL build and smoke" "$repo_root/scripts/verify-one-plus-one-minus-one-webgl.sh"
run_check "WebGL artifact freshness" check_fresh_webgl_artifact
run_check "store-ready app icon" "$repo_root/scripts/verify-one-plus-one-minus-one-icon.sh"
run_check "WebGL viewport smoke" "$repo_root/scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs"
run_check "App Store candidate screenshots" "$repo_root/scripts/verify-one-plus-one-minus-one-app-store-candidates.sh"
run_check "strict Firebase/AdMob code readiness" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh"
run_check "strict iOS release readiness" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  REQUIRE_IOS_VERSION_ENV=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-ios-readiness.sh" release
run_check "strict Android release readiness" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  REQUIRE_ANDROID_SIGNING_ENV=1 \
  REQUIRE_ANDROID_VERSION_ENV=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-android-readiness.sh" release

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 ship-ready gate failed. Resolve the failed checks above before calling the build commercially complete." >&2
  exit 2
fi

echo "1 = 1 ship-ready gate passed."
