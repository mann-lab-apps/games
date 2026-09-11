#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
metadata="$repo_root/docs/one-equals-one-store-metadata-draft.md"
privacy_policy="$repo_root/docs/privacy-policy.md"
readiness="$repo_root/prototypes/one-plus-one-minus-one/STORE_READINESS.md"
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

reject_text "$metadata" "TODO|TBD|lorem|placeholder"

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 store metadata verification failed." >&2
  exit 1
fi

echo "1 = 1 store metadata verification passed."
