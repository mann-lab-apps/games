#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="$repo_root/scripts/verify-one-plus-one-minus-one-store-metadata.sh"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

write_metadata() {
  local path="$1"
  local keywords="$2"
  cat > "$path" <<EOF
# 1 = 1 Store Metadata Draft

## App Identity

- App name: \`1 = 1\`
- Bundle ID: \`com.mannlab.games.oneplusoneminusone\`
- Privacy policy URL: \`https://games.mannlab.app/privacy\`

## Subtitle Candidates

- Tiny stick equation puzzles

## Short Description

Drag little stick friends into boxes and make each equation work.

## Full Description Draft

1 = 1 is a small puzzle game about turning simple sticks into numbers and
operators. Drag, rotate, and combine lively stick friends to build expressions
that really work. A single stick can be 1, a lazy line can be minus, crossed
sticks can multiply, and packed sticks can become 11 or 111. Complete 100 compact puzzles,
discover alternate answers, and finish with the odd little equation that started it all.

## Keywords

$keywords

## Screenshot Plan

1. \`01-round-1-first-stick\`: Round 1 first stick.
2. \`02-round-5-cross-multiply\`: Round 5.
3. \`03-round-8-triple-one\`: Round 8.
4. \`04-round-9-star-multiply\`: Round 9.
5. \`05-round-30-medium-expression\`: Round 30.
6. \`06-round-75-equality-puzzle\`: Round 75.
7. \`07-round-100-finale\`: Round 100.
8. \`08-round-select-progression\`: Round select.

## Review Notes Draft

Interstitial ads may appear only after newly cleared milestone rounds. No
account is required.

## Privacy Disclosure Draft

Firebase and AdMob disclosures must match the submitted SDK configuration.

## Age Rating Notes

Contains third-party ads when production AdMob IDs are configured.

## Final Checks Before Upload

- [ ] strict readiness passes
- [ ] fresh non-development store-capture build
EOF
}

write_privacy_policy() {
  cat > "$1" <<'EOF'
1 = 1 may use Firebase Analytics.
Google AdMob for advertising.
EOF
}

write_readiness() {
  cat > "$1" <<'EOF'
Assets/_Project/Store/PrivacyInfo.xcprivacy
Production interstitial ad unit IDs must not be placeholders
./scripts/verify-one-plus-one-minus-one-release-env.sh
REQUIRE_ONE_EQUALS_ONE_RELEASE_ENV=1
EOF
}

write_capture_script() {
  local path="$1"
  local include_last="${2:-1}"
  cat > "$path" <<'EOF'
capture_shot "01-round-1-first-stick"
capture_shot "02-round-5-cross-multiply"
capture_shot "03-round-8-triple-one"
capture_shot "04-round-9-star-multiply"
capture_shot "05-round-30-medium-expression"
capture_shot "06-round-75-equality-puzzle"
capture_shot "07-round-100-finale"
EOF
  if [[ "$include_last" == "1" ]]; then
    cat >> "$path" <<'EOF'
capture_shot "08-round-select-progression"
EOF
  fi
}

run_with_fixtures() {
  local metadata="$1"
  local capture_script="${2:-$tmp_dir/capture.sh}"
  ONE_EQUALS_ONE_STORE_METADATA="$metadata" \
  ONE_EQUALS_ONE_PRIVACY_POLICY="$tmp_dir/privacy.md" \
  ONE_EQUALS_ONE_STORE_READINESS="$tmp_dir/readiness.md" \
  ONE_EQUALS_ONE_APP_STORE_CAPTURE_SCRIPT="$capture_script" \
    "$script"
}

write_privacy_policy "$tmp_dir/privacy.md"
write_readiness "$tmp_dir/readiness.md"
write_capture_script "$tmp_dir/capture.sh"

valid_metadata="$tmp_dir/valid.md"
write_metadata "$valid_metadata" "puzzle,math,logic,equation,numbers,sticks"
run_with_fixtures "$valid_metadata" >/tmp/one-equals-one-store-metadata-valid.out

invalid_metadata="$tmp_dir/invalid.md"
write_metadata "$invalid_metadata" "puzzle, math"

set +e
invalid_output="$(run_with_fixtures "$invalid_metadata" 2>&1)"
invalid_status="$?"
set -e

if [[ "$invalid_status" -eq 0 ]]; then
  echo "Expected invalid store keywords to fail metadata verification." >&2
  echo "$invalid_output" >&2
  exit 1
fi

if ! grep -Fq "Store keyword list should be comma-separated without spaces." <<< "$invalid_output"; then
  echo "Missing keyword-spacing failure text." >&2
  echo "$invalid_output" >&2
  exit 1
fi

if ! grep -Fq "Store keyword list should include at least five comma-separated keywords." <<< "$invalid_output"; then
  echo "Missing keyword-count failure text." >&2
  echo "$invalid_output" >&2
  exit 1
fi

missing_slug_capture="$tmp_dir/missing-slug-capture.sh"
write_capture_script "$missing_slug_capture" 0

set +e
missing_slug_output="$(run_with_fixtures "$valid_metadata" "$missing_slug_capture" 2>&1)"
missing_slug_status="$?"
set -e

if [[ "$missing_slug_status" -eq 0 ]]; then
  echo "Expected missing screenshot capture slug to fail metadata verification." >&2
  echo "$missing_slug_output" >&2
  exit 1
fi

if ! grep -Fq 'capture_shot "08-round-select-progression"' <<< "$missing_slug_output"; then
  echo "Missing screenshot slug failure text." >&2
  echo "$missing_slug_output" >&2
  exit 1
fi

echo "1 = 1 store metadata verifier tests passed."
