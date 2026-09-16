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

| Device / Viewport | Round 1-10 | Round 16 | Round 30 | Round 50 | Round 75 | Round 90 | Round 100 | Round Select 1-9 | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| iPhone SE portrait | pass | pass | pass | pass | pass | pass | pass | pass | $status |
| Standard iPhone portrait | pass | pass | pass | pass | pass | pass | pass | pass | pass |
| Large iPhone portrait | pass | pass | pass | pass | pass | pass | pass | pass | pass |
| Android 20:9 portrait | pass | pass | pass | pass | pass | pass | pass | pass | pass |
| WebGL desktop browser | pass | pass | pass | pass | pass | pass | pass | pass | pass |
| WebGL mobile browser | pass | pass | pass | pass | pass | pass | pass | pass | pass |

## Touch And Feel

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Bank drag | Stick picks up immediately and remains visible above finger. | pass | fixture |
| Slot hover | Target slot highlights while pointer is over it. | pass | fixture |
| Drop | Stick snaps into the intended slot without surprising rotation. | pass | fixture |
| Drag-out return | Placed stick dragged outside returns to the bank. | pass | fixture |
| Placed tap rotate | Tapping a placed stick cycles pose clearly. | pass | fixture |
| Max 3 sticks | Fourth stick attempt gives a clear, gentle failure. | pass | fixture |
| Check disabled | Disabled Check button is visually distinct. | pass | fixture |
| Failure recovery | After a failed check, the next action is obvious. | pass | fixture |

## Character Readability

| Token | Expected Read | Personality Target | Status | Notes |
| --- | --- | --- | --- | --- |
| \`1\` | Immediate \`1\` | chatty core mascot | pass | fixture |
| \`-\` | Immediate minus | lazy, relaxed | pass | fixture |
| \`/\` | Immediate divide | tilted, playful | pass | fixture |
| \`+\` | Immediate plus | bright single friend | pass | fixture |
| \`×\` | Immediate multiply | crossed duo | pass | fixture |
| \`*\` | Immediate star multiply | energetic variant | pass | fixture |
| \`=\` | Immediate equals | teammates | pass | fixture |
| \`11\` | Immediate eleven | two friends | pass | fixture |
| \`111\` | Immediate triple one | three friends | pass | fixture |

## Audio

| Cue | Expected | Status | Notes |
| --- | --- | --- | --- |
| Button | Quiet click. | pass | fixture |
| Pick up | Small lift cue. | pass | fixture |
| Drop | Satisfying but gentle. | pass | fixture |
| Rotate | Light chirp. | pass | fixture |
| Fail | Soft negative cue. | pass | fixture |
| Success | Pleasant clear cue. | pass | fixture |
| Finale | Slightly special cue. | pass | fixture |

## Ads And Analytics

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Round 1-5 | No interstitial opportunity shown to player. | pass | fixture |
| Round 10 clear | Interstitial opportunity eligible for new clear. | pass | fixture |
| Replay clear | No interstitial opportunity shown to player. | pass | fixture |
| Hard clear | Interstitial skipped after 3+ failed checks. | pass | fixture |
| Ad close | Next round continues normally. | pass | fixture |
| Analytics | \`app_open\`, \`round_start\`, \`round_clear\`, \`round_check_failed\`, \`ad_interstitial_opportunity\`. | pass | fixture |
| Crashlytics | Development test crash uploads after relaunch. | pass | fixture |

## Store Privacy

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Apple privacy manifest | \`PrivacyInfo.xcprivacy\` is present in the exported app bundle and declares app-local UserDefaults reason \`CA92.1\`. | automated source check pass | fixture |
| App privacy labels | Firebase Analytics, Crashlytics, and AdMob disclosures match the final production SDK configuration. | pass | fixture |

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
perl -0pi -e 's/\| Bank drag \| Stick picks up immediately and remains visible above finger\. \| pass \| fixture \|/\| Bank drag \| Stick picks up immediately and remains visible above finger. \| pass \|  \|/' "$missing_notes_tracker"

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

missing_row_tracker="$tmp_dir/missing-row.md"
write_tracker "pass" "$missing_row_tracker"
perl -0pi -e 's/\| WebGL mobile browser \| pass \| pass \| pass \| pass \| pass \| pass \| pass \| pass \| pass \|\n//' "$missing_row_tracker"

set +e
missing_row_output="$(ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$missing_row_tracker" "$script" --strict 2>&1)"
missing_row_status="$?"
set -e

if [[ "$missing_row_status" -eq 0 ]]; then
  echo "Expected device QA tracker missing a required row to fail strict verification." >&2
  echo "$missing_row_output" >&2
  exit 1
fi

if ! grep -Fq "Missing expected QA tracker text: | WebGL mobile browser |" <<< "$missing_row_output"; then
  echo "Missing required-row failure text." >&2
  echo "$missing_row_output" >&2
  exit 1
fi

ambiguous_drop_tracker="$tmp_dir/ambiguous-drop.md"
write_tracker "pass" "$ambiguous_drop_tracker"
perl -0pi -e 's/\| Drop \| Stick snaps into the intended slot without surprising rotation\. \| pass \| fixture \|\n//' "$ambiguous_drop_tracker"

set +e
ambiguous_drop_output="$(ONE_EQUALS_ONE_DEVICE_QA_TRACKER="$ambiguous_drop_tracker" "$script" --strict 2>&1)"
ambiguous_drop_status="$?"
set -e

if [[ "$ambiguous_drop_status" -eq 0 ]]; then
  echo "Expected device QA tracker missing touch Drop row to fail even when audio Drop remains." >&2
  echo "$ambiguous_drop_output" >&2
  exit 1
fi

if ! grep -Fq "Missing expected QA tracker text: | Drop | Stick snaps" <<< "$ambiguous_drop_output"; then
  echo "Missing precise touch-drop row failure text." >&2
  echo "$ambiguous_drop_output" >&2
  exit 1
fi

echo "1 = 1 device QA signoff tests passed."
