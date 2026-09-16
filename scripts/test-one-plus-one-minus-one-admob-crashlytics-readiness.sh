#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="$repo_root/scripts/verify-one-plus-one-minus-one-admob-crashlytics-readiness.sh"
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

valid_env=(
  env -i
  PATH="$PATH"
  HOME="$HOME"
  REQUIRE_FIREBASE_CONFIG=1
  REQUIRE_PRODUCTION_ADMOB_IDS=1
  ONE_EQUALS_ONE_FIREBASE_IOS_CONFIG="$ios_config"
  ONE_EQUALS_ONE_FIREBASE_ANDROID_CONFIG="$android_config"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-1234567890123456~1234567890"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-1234567890123456~1234567891"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567892"
  MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-1234567890123456/1234567893"
)

"${valid_env[@]}" "$script" >/tmp/one-equals-one-admob-crashlytics-valid.out

set +e
test_id_output="$(
  "${valid_env[@]}" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_APP_ID="ca-app-pub-3940256099942544~1458002511" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_APP_ID="ca-app-pub-3940256099942544~3347511713" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_IOS_INTERSTITIAL_ID="ca-app-pub-3940256099942544/4411468910" \
    MANNLAB_ONE_PLUS_ONE_MINUS_ONE_ADMOB_ANDROID_INTERSTITIAL_ID="ca-app-pub-3940256099942544/1033173712" \
    "$script" 2>&1
)"
test_id_status="$?"
set -e

if [[ "$test_id_status" -eq 0 ]]; then
  echo "Expected Google test AdMob IDs to fail strict AdMob/Crashlytics readiness." >&2
  echo "$test_id_output" >&2
  exit 1
fi

if ! grep -Fq "Production AdMob env uses Google's test ID: iOS app ID" <<< "$test_id_output"; then
  echo "Missing iOS app test-ID failure text." >&2
  echo "$test_id_output" >&2
  exit 1
fi

if ! grep -Fq "Production AdMob env uses Google's test ID: Android interstitial" <<< "$test_id_output"; then
  echo "Missing Android interstitial test-ID failure text." >&2
  echo "$test_id_output" >&2
  exit 1
fi

echo "1 = 1 AdMob/Crashlytics readiness tests passed."
