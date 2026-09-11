#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/prototypes/one-plus-one-minus-one"

scan_paths=(
  "$project/Assets/_Project/Scripts"
  "$project/Assets/_Project/Editor"
)

if rg -n "CreateChattyHands|Create.*Arms|Create.*Hands|Create.*Legs|\\bBlush\\b|\\bCheek\\b|\\bArm\\b|\\bLeg\\b|홍조|팔다리" "${scan_paths[@]}"; then
  echo "Character policy violation: 1 = 1 stick friends must not add arms, hands, legs, cheeks, or blush." >&2
  exit 1
fi

controller="$project/Assets/_Project/Scripts/OnePlusOneMinusOneController.cs"
rg -Fq "FaceStyle.One" "$controller"
rg -Fq "FaceStyle.Plus" "$controller"
rg -Fq "FaceStyle.Divide" "$controller"
rg -Fq "FaceStyle.Multiply" "$controller"
rg -Fq "FaceStyle.Star" "$controller"
rg -Fq "FaceStyle.Equals" "$controller"
rg -Fq "CornerRadiusRatioForStyle" "$controller"
rg -Fq "HatchColorForSymbol" "$controller"

echo "1 = 1 character policy verification passed."
