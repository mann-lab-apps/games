#!/usr/bin/env bash
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
build_output="$project/Builds/WebGL/one-plus-one-minus-one"
failures=0
unity_license_available=0
webgl_artifact_fresh=0
failed_checks=()
skipped_checks=()

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
  failed_checks+=("$name (exit $status)")
  return "$status"
}

skip_check() {
  local name="$1"
  local reason="$2"
  echo "== $name =="
  echo "SKIP: $name ($reason)"
  failures=1
  skipped_checks+=("$name ($reason)")
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
      "$project/Assets/Plugins" \
      "$project/ProjectSettings" \
      "$project/Packages" \
      -type f \
      ! -path "$project/Assets/_Project/Scenes/Game.unity" \
      ! -path "$project/ProjectSettings/ProjectSettings.asset" \
      -newer "$build_output/index.html" \
      | head -1 || true
  )"
  if [[ -n "$newer_source" ]]; then
    echo "WebGL build is stale; newer source exists: ${newer_source#$repo_root/}" >&2
    return 1
  fi
}

run_check "static suite" "$repo_root/scripts/verify-one-plus-one-minus-one-static.sh"

echo "== Unity licensing preflight =="
"$repo_root/scripts/check-one-plus-one-minus-one-unity-license.sh"
unity_license_status=$?
if [[ "$unity_license_status" -eq 0 ]]; then
  echo "PASS: Unity licensing preflight"
  unity_license_available=1
else
  echo "FAIL: Unity licensing preflight (exit $unity_license_status)" >&2
  failures=1
  failed_checks+=("Unity licensing preflight (exit $unity_license_status)")
fi

if [[ "$unity_license_available" -eq 1 ]]; then
  run_check "PlayMode input and progress regressions" "$repo_root/scripts/verify-one-plus-one-minus-one-playmode.sh"
  run_check "fresh WebGL build and smoke" "$repo_root/scripts/verify-one-plus-one-minus-one-webgl.sh"
else
  skip_check "PlayMode input and progress regressions" "Unity licensing preflight failed"
  skip_check "fresh WebGL build and smoke" "Unity licensing preflight failed"
fi
if run_check "WebGL artifact freshness" check_fresh_webgl_artifact; then
  webgl_artifact_fresh=1
fi
run_check "WebGL shell metadata" "$repo_root/scripts/verify-one-plus-one-minus-one-webgl-shells.sh"
run_check "store-ready app icon" "$repo_root/scripts/verify-one-plus-one-minus-one-icon.sh"
if [[ "$webgl_artifact_fresh" -eq 1 ]]; then
  run_check "WebGL viewport smoke" "$repo_root/scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs"
  run_check "QA key-round/page captures" "$repo_root/scripts/verify-one-plus-one-minus-one-qa-captures.sh"
  run_check "App Store candidate screenshots" "$repo_root/scripts/verify-one-plus-one-minus-one-app-store-candidates.sh"
else
  skip_check "WebGL viewport smoke" "WebGL artifact freshness failed"
  skip_check "QA key-round/page captures" "WebGL artifact freshness failed"
  skip_check "App Store candidate screenshots" "WebGL artifact freshness failed"
fi
run_check "strict device QA signoff" "$repo_root/scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh" --strict
run_check "strict release environment preflight" "$repo_root/scripts/verify-one-plus-one-minus-one-release-env.sh" --strict
run_check "strict Firebase/AdMob code readiness" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh"
run_check "strict iOS release preflight" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  REQUIRE_IOS_VERSION_ENV=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-ios-readiness.sh" release --preflight-only
run_check "strict Android release preflight" env \
  REQUIRE_FIREBASE_CONFIG=1 \
  REQUIRE_PRODUCTION_ADMOB_IDS=1 \
  REQUIRE_ANDROID_SIGNING_ENV=1 \
  REQUIRE_ANDROID_VERSION_ENV=1 \
  "$repo_root/scripts/verify-one-plus-one-minus-one-android-readiness.sh" release --preflight-only
if [[ "$unity_license_available" -eq 1 ]]; then
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
else
  skip_check "strict iOS release readiness" "Unity licensing preflight failed"
  skip_check "strict Android release readiness" "Unity licensing preflight failed"
fi

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 ship-ready blockers:" >&2
  for failed_check in "${failed_checks[@]}"; do
    echo "- FAIL: $failed_check" >&2
  done
  for skipped_check in "${skipped_checks[@]}"; do
    echo "- SKIP: $skipped_check" >&2
  done
  echo "1 = 1 ship-ready gate failed. Resolve the failed checks above before calling the build commercially complete." >&2
  exit 2
fi

echo "1 = 1 ship-ready gate passed."
