#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ios_script="$repo_root/scripts/verify-one-plus-one-minus-one-ios-readiness.sh"
android_script="$repo_root/scripts/verify-one-plus-one-minus-one-android-readiness.sh"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

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

common_env=(
  env -i
  PATH="$PATH"
  HOME="$HOME"
  REQUIRE_FIREBASE_CONFIG=1
  REQUIRE_PRODUCTION_ADMOB_IDS=1
  REQUIRE_IOS_VERSION_ENV=1
  REQUIRE_ANDROID_SIGNING_ENV=1
  REQUIRE_ANDROID_VERSION_ENV=1
  ONE_EQUALS_ONE_FIREBASE_IOS_CONFIG="$ios_config"
  ONE_EQUALS_ONE_FIREBASE_ANDROID_CONFIG="$android_config"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-1234567890123456~1234567890"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-1234567890123456~1234567891"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567892"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567893"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_MARKETING_VERSION="1.0.0"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="3"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_MARKETING_VERSION="1.0.0"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="3"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PATH="$keystore"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYSTORE_PASS="secret"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_NAME="release"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_KEYALIAS_PASS="secret"
)

"${common_env[@]}" "$ios_script" release --preflight-only >/tmp/one-equals-one-ios-preflight-valid.out
"${common_env[@]}" "$android_script" release --preflight-only >/tmp/one-equals-one-android-preflight-valid.out

set +e
ios_test_id_output="$(
  "${common_env[@]}" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-3940256099942544~1458002511" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-3940256099942544/4411468910" \
    "$ios_script" release --preflight-only 2>&1
)"
ios_test_id_status="$?"
set -e

if [[ "$ios_test_id_status" -eq 0 ]]; then
  echo "Expected iOS release preflight to reject Google test AdMob IDs." >&2
  echo "$ios_test_id_output" >&2
  exit 1
fi

if ! grep -Fq "Production iOS AdMob App ID is still Google's test app ID." <<< "$ios_test_id_output"; then
  echo "Missing iOS app test-ID preflight failure text." >&2
  echo "$ios_test_id_output" >&2
  exit 1
fi

set +e
ios_stale_build_output="$(
  "${common_env[@]}" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER="2" \
    "$ios_script" release --preflight-only 2>&1
)"
ios_stale_build_status="$?"
set -e

if [[ "$ios_stale_build_status" -eq 0 ]]; then
  echo "Expected iOS release preflight to reject stale build number 2." >&2
  echo "$ios_stale_build_output" >&2
  exit 1
fi

if ! grep -Fq "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_IOS_BUILD_NUMBER should be at least 3" <<< "$ios_stale_build_output"; then
  echo "Missing iOS stale-build-number preflight failure text." >&2
  echo "$ios_stale_build_output" >&2
  exit 1
fi

set +e
android_test_id_output="$(
  "${common_env[@]}" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-3940256099942544~3347511713" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-3940256099942544/1033173712" \
    "$android_script" release --preflight-only 2>&1
)"
android_test_id_status="$?"
set -e

if [[ "$android_test_id_status" -eq 0 ]]; then
  echo "Expected Android release preflight to reject Google test AdMob IDs." >&2
  echo "$android_test_id_output" >&2
  exit 1
fi

if ! grep -Fq "Production Android AdMob App ID is still Google's test app ID." <<< "$android_test_id_output"; then
  echo "Missing Android app test-ID preflight failure text." >&2
  echo "$android_test_id_output" >&2
  exit 1
fi

set +e
android_stale_version_output="$(
  "${common_env[@]}" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE="2" \
    "$android_script" release --preflight-only 2>&1
)"
android_stale_version_status="$?"
set -e

if [[ "$android_stale_version_status" -eq 0 ]]; then
  echo "Expected Android release preflight to reject stale version code 2." >&2
  echo "$android_stale_version_output" >&2
  exit 1
fi

if ! grep -Fq "MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ANDROID_VERSION_CODE should be at least 3" <<< "$android_stale_version_output"; then
  echo "Missing Android stale-version-code preflight failure text." >&2
  echo "$android_stale_version_output" >&2
  exit 1
fi

echo "1 = 1 platform readiness preflight tests passed."
