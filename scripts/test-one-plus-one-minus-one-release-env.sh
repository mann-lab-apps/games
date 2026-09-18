#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="$repo_root/scripts/verify-one-plus-one-minus-one-release-env.sh"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

require_failure_text() {
  local name="$1"
  local output="$2"
  local pattern="$3"
  if ! grep -Fq "$pattern" <<< "$output"; then
    echo "Missing expected release-env failure text for '$name': $pattern" >&2
    echo "$output" >&2
    exit 1
  fi
}

capture_failure() {
  local name="$1"
  shift

  set +e
  output="$("$@" 2>&1)"
  status="$?"
  set -e

  if [[ "$status" -eq 0 ]]; then
    echo "Expected release-env case '$name' to fail." >&2
    echo "$output" >&2
    exit 1
  fi

  printf '%s' "$output"
}

missing_output="$(capture_failure "missing strict values" env -i PATH="$PATH" HOME="$HOME" "$script" --strict)"
require_failure_text "missing strict values" "$missing_output" "Production iOS AdMob App ID is not set"
require_failure_text "missing strict values" "$missing_output" "Production Android AdMob App ID is not set"
require_failure_text "missing strict values" "$missing_output" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH is not set"

placeholder_output="$(
  capture_failure "placeholder strict values" env -i PATH="$PATH" HOME="$HOME" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-xxxxxxxxxxxxxxxx~0000000000" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="Replace-android-app-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-xxxxxxxxxxxxxxxx/0000000000" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="Replace-android-unit-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="/Absolute/Path/release.keystore" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="Replace-pass" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="Replace-alias" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="Replace-pass" \
    "$script" --strict
)"
require_failure_text "placeholder strict values" "$placeholder_output" "Production iOS AdMob App ID is a placeholder"
require_failure_text "placeholder strict values" "$placeholder_output" "Production Android AdMob App ID is a placeholder"
require_failure_text "placeholder strict values" "$placeholder_output" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS is a placeholder"

malformed_output="$(
  capture_failure "malformed strict values" env -i PATH="$PATH" HOME="$HOME" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="bad-ios-app-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="bad-android-app-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="bad-ios-unit-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="bad-android-unit-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="one" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="-1" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="relative.keystore" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret" \
    "$script" --strict
)"
require_failure_text "malformed strict values" "$malformed_output" "Production iOS AdMob App ID has invalid format"
require_failure_text "malformed strict values" "$malformed_output" "iOS build number has invalid format"
require_failure_text "malformed strict values" "$malformed_output" "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH should be an absolute path"

test_id_output="$(
  capture_failure "google test ad ids" env -i PATH="$PATH" HOME="$HOME" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-3940256099942544~1458002511" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-3940256099942544~3347511713" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-3940256099942544/4411468910" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-3940256099942544/1033173712" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="/tmp/one-equals-one-release.keystore" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret" \
    "$script" --strict
)"
require_failure_text "google test ad ids" "$test_id_output" "Production iOS AdMob App ID uses Google's test ID"
require_failure_text "google test ad ids" "$test_id_output" "Production Android AdMob App ID uses Google's test ID"
require_failure_text "google test ad ids" "$test_id_output" "Production iOS interstitial ad unit ID uses Google's test ID"
require_failure_text "google test ad ids" "$test_id_output" "Production Android interstitial ad unit ID uses Google's test ID"

stale_ios_build_output="$(
  capture_failure "stale iOS build number" env -i PATH="$PATH" HOME="$HOME" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-1234567890123456~1234567890" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-1234567890123456~1234567891" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567892" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567893" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="2" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="/tmp/one-equals-one-release.keystore" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret" \
    "$script" --strict
)"
require_failure_text "stale iOS build number" "$stale_ios_build_output" "iOS build number should be at least 3"
require_failure_text "stale iOS build number" "$stale_ios_build_output" "next App Store candidate"

stale_android_version_output="$(
  capture_failure "stale Android version code" env -i PATH="$PATH" HOME="$HOME" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-1234567890123456~1234567890" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-1234567890123456~1234567891" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567892" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567893" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="3" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="2" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="/tmp/one-equals-one-release.keystore" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret" \
    "$script" --strict
)"
require_failure_text "stale Android version code" "$stale_android_version_output" "Android version code should be at least 3"
require_failure_text "stale Android version code" "$stale_android_version_output" "next Play Store candidate"
if grep -Fq "Android version code should be at least 3 for the next App Store candidate" <<< "$stale_android_version_output"; then
  echo "Android stale version code should not be described as an App Store candidate." >&2
  echo "$stale_android_version_output" >&2
  exit 1
fi

admob_placeholder_output="$(
  capture_failure "admob readiness mixed-case placeholders" env -i PATH="$PATH" HOME="$HOME" \
    REQUIRE_PRODUCTION_ADMOB_IDS=1 \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="Replace-ios-app-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-xxxxxxxxxxxxxxxx~0000000000" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="Replace-ios-unit-id" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-xxxxxxxxxxxxxxxx/0000000000" \
    "$repo_root/scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh"
)"
require_failure_text "admob readiness mixed-case placeholders" "$admob_placeholder_output" "Production AdMob env uses a placeholder: iOS app ID"
require_failure_text "admob readiness mixed-case placeholders" "$admob_placeholder_output" "Production AdMob env uses a placeholder: Android app ID"
require_failure_text "admob readiness mixed-case placeholders" "$admob_placeholder_output" "Production AdMob env uses a placeholder: iOS interstitial"
require_failure_text "admob readiness mixed-case placeholders" "$admob_placeholder_output" "Production AdMob env uses a placeholder: Android interstitial"

ios_config="$tmp_dir/GoogleService-Info.plist"
cat > "$ios_config" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>BUNDLE_ID</key>
  <string>com.mannlab.games.oneplusoneminusone</string>
</dict>
</plist>
EOF

android_config="$tmp_dir/google-services.json"
cat > "$android_config" <<'EOF'
{
  "client": [
    {
      "client_info": {
        "android_client_info": {
          "package_name": "com.mannlab.games.oneplusoneminusone"
        }
      }
    }
  ]
}
EOF

keystore="$tmp_dir/release.keystore"
printf 'fixture-keystore\n' > "$keystore"

env -i PATH="$PATH" HOME="$HOME" \
  ONE_EQUALS_ONE_FIREBASE_IOS_CONFIG="$ios_config" \
  ONE_EQUALS_ONE_FIREBASE_ANDROID_CONFIG="$android_config" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-1234567890123456~1234567890" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-1234567890123456~1234567891" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567892" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567893" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="3" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="3" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="$keystore" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release" \
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret" \
  "$script" --strict >/tmp/one-equals-one-release-env-valid.out

echo "1 = 1 release environment tests passed."
