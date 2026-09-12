#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tracker="$repo_root/docs/one-equals-one-device-qa-tracker.md"
failures=0
warnings=0

case "${1:-}" in
  "")
    ;;
  --strict)
    export REQUIRE_ONE_EQUALS_ONE_DEVICE_QA=1
    ;;
  --help|-h)
    cat <<'USAGE'
Usage: ./scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh [--strict]

Checks the 1 = 1 manual device QA tracker.

Default mode reports incomplete real-device QA as warnings so development
checks can continue. --strict fails while required manual QA rows still say
"not run" or while build-under-test fields are blank.
USAGE
    exit 0
    ;;
  *)
    echo "Usage: $0 [--strict]" >&2
    exit 64
    ;;
esac

require_file() {
  if [[ ! -f "$tracker" ]]; then
    echo "Missing device QA tracker: $tracker" >&2
    failures=1
  fi
}

warn_or_fail() {
  local message="$1"
  if [[ "${REQUIRE_ONE_EQUALS_ONE_DEVICE_QA:-0}" == "1" ]]; then
    echo "$message" >&2
    failures=1
    return
  fi

  echo "Warning: $message" >&2
  warnings=1
}

count_literal() {
  local pattern="$1"
  grep -F "$pattern" "$tracker" | wc -l | awk '{print $1}'
}

require_text() {
  local pattern="$1"
  if ! grep -Fq -- "$pattern" "$tracker"; then
    echo "Missing expected QA tracker text: $pattern" >&2
    failures=1
  fi
}

check_build_under_test() {
  for label in "Build date" "Git commit or local diff note" "Platform" "Version" "Tester"; do
    if grep -Eq "^- ${label}:\\s*$" "$tracker"; then
      warn_or_fail "Device QA tracker Build Under Test field is blank: $label"
    fi
  done
}

check_required_sections() {
  require_text "## Screen Matrix"
  require_text "## Touch And Feel"
  require_text "## Character Readability"
  require_text "## Audio"
  require_text "## Ads And Analytics"
  require_text "## Store Privacy"
  require_text "## Sign-Off"
}

check_incomplete_rows() {
  local not_run_count
  not_run_count="$(count_literal "| not run")"
  if (( not_run_count > 0 )); then
    warn_or_fail "Device QA tracker still has $not_run_count required row(s) marked not run."
  fi

  if grep -Fq "| WebGL mobile browser |  |" "$tracker"; then
    warn_or_fail "Device QA tracker WebGL mobile browser row is still blank."
  fi

  if grep -Fq "real touch QA still needed" "$tracker"; then
    warn_or_fail "Device QA tracker still marks real touch QA as needed."
  fi

  if grep -Fq "manual QA still needed" "$tracker"; then
    warn_or_fail "Device QA tracker still marks manual QA as needed."
  fi

  if grep -Eq "^- Manual QA risks: *[^ ]" "$tracker"; then
    warn_or_fail "Device QA tracker Sign-Off still lists manual QA risks."
  fi

  if grep -Fq "Ready for TestFlight/internal test: no" "$tracker"; then
    warn_or_fail "Device QA tracker says TestFlight/internal test is not ready."
  fi
}

print_next_steps() {
  cat >&2 <<'NEXT_STEPS'

Required manual QA signoff inputs:
- Fill Build Under Test with date, commit/local diff note, platform, version, and tester.
- Run real device or simulator checks for screen layout, touch feel, character readability, audio, ads/analytics, and store privacy.
- Replace every required "not run" status with pass/fail evidence and notes.
- Record any remaining issue as a concrete blocker instead of leaving generic manual QA risk text.

Rerun before store submission:
  ./scripts/verify-one-plus-one-minus-one-device-qa-signoff.sh --strict
NEXT_STEPS
}

require_file
if [[ "$failures" -eq 0 ]]; then
  check_required_sections
  check_build_under_test
  check_incomplete_rows
fi

if [[ "$failures" -ne 0 ]]; then
  echo "1 = 1 device QA signoff verification failed." >&2
  print_next_steps
  exit 2
fi

if [[ "$warnings" -ne 0 ]]; then
  echo "1 = 1 device QA signoff preflight passed with warnings; manual device QA remains open." >&2
  print_next_steps
else
  echo "1 = 1 device QA signoff verified."
fi
