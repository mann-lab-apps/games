# 1 = 1 App Review Response: Guideline 4.3(a)

Use this for the App Review Resolution Center response after the Guideline
4.3(a) spam rejection. Keep the tone cooperative: clarify the app's distinct
mechanic, avoid arguing that the reviewer is wrong, and offer updated metadata
or footage if Apple wants a resubmission.

## Short Response

Hello App Review Team,

Thank you for reviewing the app. We understand the concern under Guideline
4.3(a).

We would like to clarify that `1 = 1` is not a repackaged matchstick puzzle app
or a template reskin. It is an original equation-building puzzle built around
animated stick characters.

Unlike traditional matchstick equation games, the app does not present a fixed
incorrect equation and ask the player to move one matchstick to repair it. Each
round starts with empty boxes and a limited set of living stick pieces. Players
compose the expression themselves by dragging, rotating, and combining the
pieces into tokens such as `1`, `11`, `111`, `+`, `-`, `/`, `×`, `*`, and `=`.

The solver accepts mathematically valid alternate answers instead of forcing a
single preset solution. Players can also build their own `=` token, in which
case the app checks whether the left and right sides are actually equal.

The current release includes 100 handmade rounds, custom stick-character
visuals, local progression, sound controls, and custom exact equation
validation logic. We can update the metadata and review materials to make this
distinction clearer, and we can provide additional gameplay footage if helpful.

Best regards,
MannLab

## Detailed Response

Hello App Review Team,

Thank you for reviewing `1 = 1`. We understand the concern raised under
Guideline 4.3(a), and we appreciate the opportunity to clarify the design.

`1 = 1` is not intended to be a repackaged matchstick equation game or a
template reskin. The app is an original single-player puzzle about composing
valid expressions from animated stick characters.

The core mechanic is different from traditional matchstick repair puzzles:

- The player is not shown a fixed broken equation to repair by moving one
  matchstick.
- Each round begins with empty slots and a limited number of animated stick
  pieces.
- The player builds the expression from scratch by dragging, rotating, and
  combining pieces.
- Pieces can form several different token types, including `1`, `11`, `111`,
  `+`, `-`, `/`, `×`, `*`, and `=`.
- All pieces must be allocated, so the puzzle is about constructing a valid
  expression under a resource constraint.
- The app accepts mathematically valid alternate answers rather than enforcing
  a single preset solution.
- If a player constructs `=`, the app validates the equality directly by
  comparing the left and right sides.

The app currently includes 100 handmade rounds and custom validation logic. The
round set was reviewed to reduce unintended duplicate answer structures, and
the solver uses exact rational arithmetic for equation correctness so small
nonzero values are not accepted as zero. This is specific game logic rather
than a generic template mechanic.

The visual presentation is also built around small stick characters rather than
generic matchsticks. The pieces have custom hand-drawn outlines, facial
expressions, symbol-specific appearances, local progression, round selection,
sound controls, and gameplay feedback.

To make the distinction clearer for review and for users, we are prepared to
update the App Store metadata and review notes to describe the app as a
character-based equation-building puzzle rather than as a generic stick or
matchstick puzzle. We can also provide new gameplay footage showing:

- Round 1 introducing the living `1` piece.
- Round 5 showing a constructed `×` token.
- Round 8 showing `111`.
- Round 9 showing `*`.
- Round 18 or 19 showing direct equality construction.
- Later rounds such as Round 30 or Round 100 showing larger expressions built
  from empty slots.

Please let us know if you would like us to submit the updated metadata and
additional review footage in a new build for review.

Best regards,
MannLab

## Optional Follow-Up If Apple Requests A New Build

Hello App Review Team,

Thank you for the clarification. We will submit an updated build and metadata
that more clearly present `1 = 1` as a character-based equation-building puzzle.
The updated review notes and screenshots will focus on the app's distinctive
mechanic: constructing expressions from empty slots using animated stick
characters, accepting alternate valid expressions, and validating direct
equalities created by the player.

Best regards,
MannLab
