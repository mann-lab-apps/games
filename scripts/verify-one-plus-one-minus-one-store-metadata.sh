#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
metadata="$repo_root/docs/one-equals-one-store-metadata-draft.md"
privacy_policy="$repo_root/docs/privacy-policy.md"
readiness="$repo_root/prototypes/one-plus-one-minus-one/STORE_READINESS.md"
capture_script="$repo_root/scripts/capture-one-plus-one-minus-one-app-store-candidates.sh"
failures=0

require_text() {
  local path="$1"
  local pattern="$2"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    failures=1
    return
  fi

  if ! grep -Fq -- "$pattern" "$path"; then
    echo "Missing expected store-readiness text in ${path#$repo_root/}: $pattern" >&2
    failures=1
  fi
}

reject_text() {
  local path="$1"
  local pattern="$2"
  if [[ -f "$path" ]] && grep -Eiq -- "$pattern" "$path"; then
    echo "Unexpected unresolved store metadata marker in ${path#$repo_root/}: $pattern" >&2
    failures=1
  fi
}

field_value() {
  local path="$1"
  local label="$2"
  sed -n "s/^- ${label}: \`\\(.*\\)\`$/\\1/p" "$path" | head -1
}

first_paragraph_after_heading() {
  local path="$1"
  local heading="$2"
  awk -v heading="$heading" '
    $0 == heading { in_block = 1; next }
    in_block && /^## / { exit }
    in_block && NF { print; exit }
  ' "$path"
}

block_after_heading() {
  local path="$1"
  local heading="$2"
  awk -v heading="$heading" '
    $0 == heading { in_block = 1; next }
    in_block && /^## / { exit }
    in_block { print }
  ' "$path"
}

byte_len() {
  printf "%s" "$1" | LC_ALL=C wc -c | awk '{print $1}'
}

require_value_present() {
  local label="$1"
  local value="$2"
  if [[ -z "$value" ]]; then
    echo "Missing store metadata value: $label" >&2
    failures=1
  fi
}

require_max_bytes() {
  local label="$1"
  local value="$2"
  local max="$3"
  local length
  length="$(byte_len "$value")"
  if (( length > max )); then
    echo "Store metadata value is too long: $label is $length bytes, max $max" >&2
    failures=1
  fi
}

require_min_bytes() {
  local label="$1"
  local value="$2"
  local min="$3"
  local length
  length="$(byte_len "$value")"
  if (( length < min )); then
    echo "Store metadata value is too short: $label is $length bytes, min $min" >&2
    failures=1
  fi
}

require_keyword_budget() {
  local value="$1"
  local length
  length="$(byte_len "$value")"
  if (( length > 100 )); then
    echo "Store keyword list is too long for the conservative submission budget: $length bytes, max 100" >&2
    failures=1
  fi

  if [[ "$value" == *" "* ]]; then
    echo "Store keyword list should be comma-separated without spaces." >&2
    failures=1
  fi

  if [[ "$value" == *,*,*,*,* ]]; then
    return
  fi

  echo "Store keyword list should include at least five comma-separated keywords." >&2
  failures=1
}

require_capture_slug() {
  local slug="$1"
  require_text "$metadata" "$slug"
  require_text "$capture_script" "capture_shot \"$slug\""
}

require_text "$metadata" "## App Identity"
require_text "$metadata" "App name: \`1 = 1\`"
require_text "$metadata" "Bundle ID: \`com.mannlab.games.oneplusoneminusone\`"
require_text "$metadata" "Privacy policy URL: \`https://games.mannlab.app/privacy\`"
require_text "$metadata" "## Subtitle Candidates"
require_text "$metadata" "## Short Description"
require_text "$metadata" "## Full Description Draft"
require_text "$metadata" "Complete 100 compact puzzles"
require_text "$metadata" "## Screenshot Plan"
require_text "$metadata" "Round 100"
require_text "$metadata" "Round select"
require_text "$metadata" "## Review Notes Draft"
require_text "$metadata" "Interstitial ads may appear only after newly cleared milestone rounds"
require_text "$metadata" "## Privacy Disclosure Draft"
require_text "$metadata" "Firebase"
require_text "$metadata" "AdMob"
require_text "$metadata" "## Age Rating Notes"
require_text "$metadata" "Contains third-party ads"
require_text "$metadata" "## Final Checks Before Upload"
require_text "$metadata" "strict readiness passes"
require_text "$metadata" "fresh non-development store-capture build"

require_text "$privacy_policy" "1 = 1 may use Firebase Analytics"
require_text "$privacy_policy" "Google AdMob for advertising"
require_text "$readiness" "Assets/_Project/Store/PrivacyInfo.xcprivacy"
require_text "$readiness" "Production interstitial ad unit IDs must not be placeholders"
require_text "$readiness" "./scripts/verify-one-plus-one-minus-one-release-env.sh"
require_text "$readiness" "REQUIRE_ONE_EQUALS_ONE_RELEASE_ENV=1"

reject_text "$metadata" "TODO|TBD|lorem|placeholder"

app_name="$(field_value "$metadata" "App name")"
bundle_id="$(field_value "$metadata" "Bundle ID")"
privacy_url="$(field_value "$metadata" "Privacy policy URL")"
short_description="$(first_paragraph_after_heading "$metadata" "## Short Description")"
keywords="$(first_paragraph_after_heading "$metadata" "## Keywords")"
full_description="$(block_after_heading "$metadata" "## Full Description Draft")"
review_notes="$(block_after_heading "$metadata" "## Review Notes Draft")"
privacy_disclosure="$(block_after_heading "$metadata" "## Privacy Disclosure Draft")"

require_value_present "App name" "$app_name"
require_value_present "Bundle ID" "$bundle_id"
require_value_present "Privacy policy URL" "$privacy_url"
require_value_present "Short description" "$short_description"
require_value_present "Keywords" "$keywords"
require_value_present "Full description" "$full_description"
require_value_present "Review notes" "$review_notes"
require_value_present "Privacy disclosure" "$privacy_disclosure"

require_max_bytes "App name" "$app_name" 30
require_max_bytes "Short description" "$short_description" 80
require_min_bytes "Full description" "$full_description" 200
require_max_bytes "Full description" "$full_description" 4000
require_keyword_budget "$keywords"

if [[ "$bundle_id" != "com.mannlab.games.oneplusoneminusone" ]]; then
  echo "Unexpected store bundle ID: $bundle_id" >&2
  failures=1
fi

if [[ "$privacy_url" != "https://games.mannlab.app/privacy" ]]; then
  echo "Unexpected privacy policy URL: $privacy_url" >&2
  failures=1
fi

while IFS= read -r subtitle; do
  subtitle="${subtitle#- }"
  if [[ -n "$subtitle" ]]; then
    require_max_bytes "Subtitle candidate: $subtitle" "$subtitle" 30
  fi
done < <(block_after_heading "$metadata" "## Subtitle Candidates" | grep -E '^- ' || true)

for slug in \
  "01-round-1-first-stick" \
  "02-round-5-cross-multiply" \
  "03-round-8-triple-one" \
  "04-round-9-star-multiply" \
  "05-round-30-medium-expression" \
  "06-round-75-equality-puzzle" \
  "07-round-100-finale" \
  "08-round-select-progression"
do
  require_capture_slug "$slug"
done

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 store metadata verification failed." >&2
  exit 1
fi

echo "1 = 1 store metadata verification passed."
