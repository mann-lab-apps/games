#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"
firebase_ios="$project/Assets/GoogleService-Info.plist"
firebase_android="$project/Assets/google-services.json"
failures=0
warnings=0

case "${1:-}" in
  "")
    ;;
  --strict)
    export REQUIRE_ONE_EQUALS_ONE_RELEASE_ENV=1
    ;;
  --help|-h)
    cat <<'USAGE'
Usage: ./scripts/verify-one-plus-one-minus-one-release-env.sh [--strict]

Checks 1 = 1 external release configuration without launching Unity.

Default mode reports missing production Firebase/AdMob/version/signing settings
as warnings so normal development verification can continue.

--strict fails on every missing, placeholder, Google-test, or malformed release
setting and should pass before store submission.
USAGE
    exit 0
    ;;
  *)
    echo "Usage: $0 [--strict]" >&2
    exit 64
    ;;
esac

is_required() {
  local env_name="$1"
  [[ "${REQUIRE_ONE_EQUALS_ONE_RELEASE_ENV:-0}" == "1" || "${!env_name:-0}" == "1" ]]
}

warn_or_fail() {
  local message="$1"
  local require_env="$2"
  if is_required "$require_env"; then
    echo "$message" >&2
    failures=1
    return
  fi

  echo "Warning: $message" >&2
  warnings=1
}

has_placeholder_text() {
  local value="$1"
  [[ "$value" == *XXXX* || "$value" == *replace* || "$value" == *REPLACE* || "$value" == *"/absolute/path/"* ]]
}

check_firebase_ios() {
  if [[ ! -f "$firebase_ios" ]]; then
    warn_or_fail "Missing Firebase iOS config: $firebase_ios" REQUIRE_FIREBASE_CONFIG
    return
  fi

  if command -v /usr/libexec/PlistBuddy >/dev/null 2>&1; then
    local bundle_id
    bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :BUNDLE_ID' "$firebase_ios" 2>/dev/null || true)"
    if [[ "$bundle_id" != "com.mannlab.games.oneplusoneminusone" ]]; then
      echo "Firebase iOS config bundle ID does not match com.mannlab.games.oneplusoneminusone." >&2
      failures=1
    fi
  elif ! grep -Fq "com.mannlab.games.oneplusoneminusone" "$firebase_ios"; then
    echo "Firebase iOS config does not mention com.mannlab.games.oneplusoneminusone." >&2
    failures=1
  fi
}

check_firebase_android() {
  if [[ ! -f "$firebase_android" ]]; then
    warn_or_fail "Missing Firebase Android config: $firebase_android" REQUIRE_FIREBASE_CONFIG
    return
  fi

  if ! grep -Fq '"package_name": "com.mannlab.games.oneplusoneminusone"' "$firebase_android"; then
    echo "Firebase Android config package name does not match com.mannlab.games.oneplusoneminusone." >&2
    failures=1
  fi
}

check_admob_env() {
  local label="$1"
  local env_name="$2"
  local test_value="$3"
  local pattern="$4"
  local value="${!env_name:-}"

  if [[ -z "$value" ]]; then
    warn_or_fail "$label is not set ($env_name)." REQUIRE_PRODUCTION_ADMOB_IDS
    return
  fi

  if [[ "$value" == "$test_value" ]]; then
    warn_or_fail "$label uses Google's test ID ($env_name)." REQUIRE_PRODUCTION_ADMOB_IDS
    return
  fi

  if has_placeholder_text "$value"; then
    warn_or_fail "$label is a placeholder ($env_name)." REQUIRE_PRODUCTION_ADMOB_IDS
    return
  fi

  if [[ ! "$value" =~ $pattern ]]; then
    warn_or_fail "$label has invalid format ($env_name)." REQUIRE_PRODUCTION_ADMOB_IDS
  fi
}

check_version_env() {
  local label="$1"
  local env_name="$2"
  local pattern="$3"
  local require_env="$4"
  local value="${!env_name:-}"

  if [[ -z "$value" ]]; then
    warn_or_fail "$label is not set ($env_name)." "$require_env"
    return
  fi

  if [[ ! "$value" =~ $pattern ]]; then
    warn_or_fail "$label has invalid format ($env_name)." "$require_env"
  fi
}

check_android_signing_env() {
  local path_value="${MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH:-}"

  for env_name in \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS
  do
    if [[ -z "${!env_name:-}" ]]; then
      warn_or_fail "$env_name is not set." REQUIRE_ANDROID_SIGNING_ENV
    elif has_placeholder_text "${!env_name:-}"; then
      warn_or_fail "$env_name is a placeholder." REQUIRE_ANDROID_SIGNING_ENV
    fi
  done

  if [[ -n "$path_value" ]] && ! has_placeholder_text "$path_value"; then
    if [[ "$path_value" != /* ]]; then
      warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH should be an absolute path." REQUIRE_ANDROID_SIGNING_ENV
    elif [[ ! -f "$path_value" ]]; then
      warn_or_fail "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH does not exist." REQUIRE_ANDROID_SIGNING_ENV
    fi
  fi
}

print_next_steps() {
  cat >&2 <<'NEXT_STEPS'

Required external release inputs:
- Assets/GoogleService-Info.plist with bundle ID com.mannlab.games.oneplusoneminusone
- Assets/google-services.json with package name com.mannlab.games.oneplusoneminusone
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME
- MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS

Use prototypes/one-plus-one-minus-one/RELEASE_ENV.example as the local/CI
template, then rerun:
  ./scripts/verify-one-plus-one-minus-one-release-env.sh --strict
NEXT_STEPS
}

check_firebase_ios
check_firebase_android

check_admob_env \
  "Production iOS AdMob App ID" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID \
  "ca-app-pub-3940256099942544~1458002511" \
  '^ca-app-pub-[0-9]{16}~[0-9]{10}$'
check_admob_env \
  "Production Android AdMob App ID" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID \
  "ca-app-pub-3940256099942544~3347511713" \
  '^ca-app-pub-[0-9]{16}~[0-9]{10}$'
check_admob_env \
  "Production iOS interstitial ad unit ID" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID \
  "ca-app-pub-3940256099942544/4411468910" \
  '^ca-app-pub-[0-9]{16}/[0-9]{10}$'
check_admob_env \
  "Production Android interstitial ad unit ID" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID \
  "ca-app-pub-3940256099942544/1033173712" \
  '^ca-app-pub-[0-9]{16}/[0-9]{10}$'

check_version_env \
  "iOS marketing version" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION \
  '^[0-9]+([.][0-9]+){1,2}$' \
  REQUIRE_IOS_VERSION_ENV
check_version_env \
  "iOS build number" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER \
  '^[1-9][0-9]*$' \
  REQUIRE_IOS_VERSION_ENV
check_version_env \
  "Android marketing version" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION \
  '^[0-9]+([.][0-9]+){1,2}$' \
  REQUIRE_ANDROID_VERSION_ENV
check_version_env \
  "Android version code" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE \
  '^[1-9][0-9]*$' \
  REQUIRE_ANDROID_VERSION_ENV

check_android_signing_env

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 release environment preflight failed." >&2
  print_next_steps
  exit 2
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "1 = 1 release environment preflight passed with warnings; production files/env vars are still needed." >&2
  print_next_steps
else
  echo "1 = 1 release environment preflight passed."
fi
