#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="$repo_root/scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh"
tmp_dir="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_dir"
}
trap cleanup EXIT

write_tracker() {
  local status="$1"
  local path="$2"
  cat > "$path" <<EOF
# 1 = 1 Device QA Tracker

## Build Under Test

- Build date: 2026-09-16
- Git commit or local diff note: local-test
- Platform: WebGL QA
- Version: 1.0.0-test
- Tester: automated fixture

## Screen Matrix

| Device / Viewport | Round 1-10 | Status |
| --- | --- | --- |
| iPhone SE portrait | pass | $status |

## Touch And Feel

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Bank drag | Works. | pass | fixture |

## Character Readability

| Token | Expected Read | Personality Target | Status | Notes |
| --- | --- | --- | --- | --- |
| \`1\` | Immediate \`1\` | chatty core mascot | pass | fixture |

## Audio

| Cue | Expected | Status | Notes |
| --- | --- | --- | --- |
| Button | Quiet click. | pass | fixture |

## Ads And Analytics

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Round 1-5 | No interstitial opportunity shown to player. | pass | fixture |

## Store Privacy

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Apple privacy manifest | Present. | automated source check pass | fixture |

## Sign-Off

- Release blocker count: none
- Manual QA risks:
- External blockers: none
- Ready for store screenshots: yes
- Ready for TestFlight/internal test: yes
EOF
}

valid_tracker="$tmp_dir/valid.md"
write_tracker "pass" "$valid_tracker"
ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$valid_tracker" "$script" --strict >/tmp/one-equals-one-device-qa-valid.out

invalid_tracker="$tmp_dir/invalid.md"
write_tracker "maybe" "$invalid_tracker"

set +e
invalid_output="$(ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$invalid_tracker" "$script" --strict 2>&1)"
invalid_status="$?"
set -e

if [[ "$invalid_status" -eq 0 ]]; then
  echo "Expected unknown device QA status to fail strict verification." >&2
  echo "$invalid_output" >&2
  exit 1
fi

if ! grep -Fq "unknown Status value(s): maybe" <<< "$invalid_output"; then
  echo "Missing unknown-status failure text." >&2
  echo "$invalid_output" >&2
  exit 1
fi

missing_notes_tracker="$tmp_dir/missing-notes.md"
write_tracker "pass" "$missing_notes_tracker"
perl -0pi -e 's/\| Bank drag \| Works\. \| pass \| fixture \|/\| Bank drag \| Works. \| pass \|  \|/' "$missing_notes_tracker"

set +e
missing_notes_output="$(ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$missing_notes_tracker" "$script" --strict 2>&1)"
missing_notes_status="$?"
set -e

if [[ "$missing_notes_status" -eq 0 ]]; then
  echo "Expected terminal device QA status without notes to fail strict verification." >&2
  echo "$missing_notes_output" >&2
  exit 1
fi

if ! grep -Fq "terminal Status row(s) need Notes evidence" <<< "$missing_notes_output"; then
  echo "Missing terminal-status notes failure text." >&2
  echo "$missing_notes_output" >&2
  exit 1
fi

inconsistent_signoff_tracker="$tmp_dir/inconsistent-signoff.md"
write_tracker "pass" "$inconsistent_signoff_tracker"
perl -0pi -e 's/- External blockers: none/- External blockers: production AdMob IDs missing/' "$inconsistent_signoff_tracker"

set +e
inconsistent_signoff_output="$(ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$inconsistent_signoff_tracker" "$script" --strict 2>&1)"
inconsistent_signoff_status="$?"
set -e

if [[ "$inconsistent_signoff_status" -eq 0 ]]; then
  echo "Expected TestFlight-ready signoff with external blockers to fail strict verification." >&2
  echo "$inconsistent_signoff_output" >&2
  exit 1
fi

if ! grep -Fq "cannot be ready for TestFlight/internal test while External blockers remain" <<< "$inconsistent_signoff_output"; then
  echo "Missing inconsistent-signoff failure text." >&2
  echo "$inconsistent_signoff_output" >&2
  exit 1
fi

echo "1 = 1 device QA signoff tests passed."
