#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
helper="$repo_root/scripts/lib-one-plus-one-minus-one-unity-license.sh"
tmp_home="$(mktemp -d)"

cleanup() {
  rm -rf "$tmp_home"
}
trap cleanup EXIT

mkdir -p "$tmp_home/.unity/bin" "$tmp_home/Library/Unity/licenses"

cat > "$tmp_home/.unity/bin/unity" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

case "${ONE_EQUALS_ONE_FAKE_UNITY_LICENSE_STATE:-active}" in
  active)
    printf '{"success":true,"data":[{"serial":"fake"}],"errors":[]}\n'
    ;;
  unavailable)
    printf '{"success":false,"data":null,"errors":[{"code":"LICENSING_CLIENT_UNAVAILABLE","message":"fake unavailable"}]}\n'
    ;;
  empty)
    printf '{"success":false,"data":[],"errors":[]}\n'
    ;;
  invalid)
    printf '{not json}\n'
    ;;
  *)
    echo "Unknown fake state: ${ONE_EQUALS_ONE_FAKE_UNITY_LICENSE_STATE}" >&2
    exit 64
    ;;
esac
EOF
chmod +x "$tmp_home/.unity/bin/unity"

run_check() {
  local state="$1"
  local expected="$2"
  shift 2

  set +e
  output="$(
    HOME="$tmp_home" ONE_EQUALS_ONE_FAKE_UNITY_LICENSE_STATE="$state" \
      bash -c '. "$1"; check_one_plus_one_minus_one_unity_license "test verification"' bash "$helper" 2>&1
  )"
  status="$?"
  set -e

  if [[ "$status" -ne "$expected" ]]; then
    echo "Unexpected status for fake license state '$state': got $status, want $expected" >&2
    echo "$output" >&2
    exit 1
  fi

  for pattern in "$@"; do
    if ! grep -Fq "$pattern" <<< "$output"; then
      echo "Missing expected output for fake license state '$state': $pattern" >&2
      echo "$output" >&2
      exit 1
    fi
  done
}

rm -f "$tmp_home/Library/Unity/licenses/UnityEntitlementLicense.xml"
run_check active 0 "active license data"
run_check unavailable 2 "Unity licensing client is unavailable"
run_check empty 2 "No Unity Editor license found"
run_check invalid 2 "Unity CLI license JSON is invalid"

printf '<license />\n' > "$tmp_home/Library/Unity/licenses/UnityEntitlementLicense.xml"
run_check empty 0 "local Unity entitlement license file"

echo "1 = 1 Unity license helper tests passed."
