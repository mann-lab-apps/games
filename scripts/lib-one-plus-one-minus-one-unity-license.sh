#!/usr/bin/env bash

check_one_plus_one_minus_one_unity_license() {
  local context="${1:-Unity PlayMode/WebGL verification}"
  local unity_cli="${HOME}/.unity/bin/unity"
  local entitlement="${HOME}/Library/Unity/licenses/UnityEntitlementLicense.xml"
  local license_state

  if [[ ! -x "$unity_cli" ]]; then
    echo "Unity CLI not found: $unity_cli" >&2
    return 2
  fi

  license_state="$("$unity_cli" license --json 2>/dev/null || true)"

  python3 -c '
import json
import os
import sys

entitlement = sys.argv[1]
context = sys.argv[2]

try:
    payload = json.load(sys.stdin)
except json.JSONDecodeError as exc:
    print(f"Unity CLI license JSON is invalid: {exc}", file=sys.stderr)
    raise SystemExit(2)

errors = payload.get("errors") or []
codes = {error.get("code") for error in errors if isinstance(error, dict)}
data = payload.get("data")

if "LICENSING_CLIENT_UNAVAILABLE" in codes:
    print(
        "Unity licensing client is unavailable. Open Unity Hub or repair its "
        f"licensing service before running {context}.",
        file=sys.stderr,
    )
    raise SystemExit(2)

if data:
    print("Unity CLI license check passed with active license data.")
    raise SystemExit(0)

if os.path.getsize(entitlement) if os.path.exists(entitlement) else 0:
    print(
        "Unity CLI license check did not return active entitlements; using "
        "local Unity entitlement license file."
    )
    raise SystemExit(0)

print(
    "No Unity Editor license found. Activate a license in Unity Hub before "
    f"running {context}.",
    file=sys.stderr,
)
raise SystemExit(2)
' "$entitlement" "$context" <<< "$license_state"
}
