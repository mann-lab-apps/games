#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

. "$repo_root/scripts/lib-one-plus-one-minus-one-unity-license.sh"

check_one_plus_one_minus_one_unity_license "Unity PlayMode/WebGL verification"
