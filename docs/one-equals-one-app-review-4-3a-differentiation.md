# 1 = 1 App Review 4.3(a) Differentiation Plan

## Rejection Summary

Apple rejected `1 = 1` under Guideline 4.3(a) - Design - Spam. The review
message said the app appears to share a similar binary, metadata, and/or
concept with apps submitted by this or other developers, with only minor
differences.

This is not a simple technical bug rejection. It means the submitted package
did not make the app's distinct product identity clear enough.

## Likely Review Signals

The submitted app can be read as similar to existing matchstick equation puzzle
apps because the surface signals overlap:

- stick pieces
- equation-based puzzle goals
- drag or rotate interaction
- compact puzzle rounds
- App Store copy that used phrases like "stick equation puzzles" and "make each
  equation work"
- Unity mobile game binary with Firebase, AdMob, local progression, and compact
  casual-game structure

Those signals can make the app look like a lightly differentiated matchstick
math puzzle even though the actual rules are more specific.

## Actual Product Difference

`1 = 1` should be positioned as a character-based equation builder, not a
one-match repair puzzle.

Distinct mechanics:

- The board starts from empty slots rather than a fixed broken equation.
- Players compose the full expression themselves.
- Each piece is a living stick character, not a plain matchstick.
- The same physical pieces can become different tokens depending on placement
  and rotation.
- Supported token constructions include `1`, `11`, `111`, `+`, `-`, `/`, `×`,
  `*`, and `=`.
- All pieces must be allocated, creating a resource-constrained expression
  construction puzzle.
- The solver accepts mathematically valid alternate answers.
- Player-built `=` tokens trigger direct left/right equality validation.
- The 100-round set was hand-authored and later analyzed for unwanted shared
  answer structures.
- The current source uses exact rational correctness checks, not only a loose
  floating-point target comparison.

## Metadata Changes Made

`docs/one-equals-one-store-metadata-draft.md` now avoids leading with generic
"matchstick" or "fix the equation" language. The draft emphasizes:

- living stick friends
- empty boxes
- constructing expressions from scratch
- alternate valid answers
- direct equality validation
- 100 handmade rounds

Updated store-facing candidates:

- Subtitle candidates:
  - Build with stick friends
  - Living stick logic
  - Tiny equation builders
- Promotional text:
  - Build odd little equations from living stick friends. Empty boxes, flexible
    answers, and 100 handmade rounds.
- Short description:
  - Build strange equations with living stick friends.

## Review Notes Changes Made

The review notes now explicitly explain:

- This is not a one-match repair puzzle.
- Traditional match-based equation games usually show a prebuilt incorrect
  equation.
- `1 = 1` starts with empty boxes and a limited set of animated pieces.
- Players build tokens and expressions from scratch.
- Alternate valid expressions are accepted.
- Direct equalities are validated when the player builds `=`.
- Recommended review path: Rounds 1, 5, 8, 9, 18/19, 30 or 100.

## Screenshot And Video Direction

The next App Review attachment should not only show Rounds 1-10. That footage
looks like a tutorial and may underplay the app's differences.

Recommended 60-90 second review video:

1. Round 1: show the living `1` character and empty boxes.
2. Round 5: show pieces being arranged into `×`.
3. Round 8: show three pieces becoming `111`.
4. Round 9: show the three-piece `*`.
5. Round 18 or 19: show the player creating `=`.
6. Round 30 or 75: show a larger expression built from empty slots.
7. Round 100: briefly show late-game density or the title callback.
8. Round select: show the 100-round progression.

Screenshot plan should similarly prioritize expression construction and
character tokens over a simple "fix the formula" appearance.

## Build Change Recommendation

Metadata and review materials should be updated first. A new build is likely
safer than a text-only appeal if Apple asks for resubmission.

Small build changes worth considering:

- Update the earliest tutorial wording to say "Build with stick friends" rather
  than language that implies repairing a fixed equation.
- Add a minimal Help or Info view that explains expression construction,
  alternate valid answers, and direct equality.
- Ensure the first screenshots and first 30 seconds show empty-slot building,
  not only final formulas.

Avoid:

- adding token bans
- forcing sample answers
- changing the core 100-round structure
- adding arms, hands, legs, blush, or off-character decorations
- making aggressive claims about competing apps

## Resubmission Checklist

- [ ] Send App Review response from
      `docs/one-equals-one-app-review-response-4-3a.md`.
- [ ] Update App Store metadata from
      `docs/one-equals-one-store-metadata-draft.md`.
- [ ] Update review notes with the 4.3(a) clarification.
- [ ] Prepare a new gameplay video showing construction, alternate answers, and
      direct equality.
- [ ] Refresh screenshots if current screenshots look like generic matchstick
      equation repair.
- [ ] Decide whether a small build change is needed before resubmission.
- [ ] If a new build is submitted, increment the build number and re-run the
      store metadata/static/readiness checks.
- [ ] Do not call the app release-ready until Unity runtime, device QA, Firebase,
      AdMob, and store settings are verified for the submitted build.

## Current Judgment

Do not immediately resubmit the same binary with unchanged metadata unless
Apple explicitly says a written clarification is enough. The safer route is:

1. Reply with the clarification.
2. Prepare updated metadata and review notes.
3. If Apple asks for a new submission, send a build/metadata package that makes
   the app's character-based equation-builder identity obvious within the first
   screenshots and review video.
