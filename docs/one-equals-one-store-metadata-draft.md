# 1 = 1 Store Metadata Draft

This draft is for store-submission preparation. Re-check it against the final
build, privacy policy, Firebase settings, and AdMob settings before upload.

## Candidate Boundary (2026-09-27)

iOS `1.0.0 (4)` is the last uploaded App Store candidate confirmed in the repo
history. Local source is newer than that upload and now includes the MakeOne
pattern-quality passes plus the first-minute `Found` / `Discoveries` UI wording
pass. App Store Connect review outcome and any future build selection remain
external follow-up items.

The local source is newer than uploaded build 4 and includes the 4.3(a)
positioning changes, exact-rational solve correctness, round-identity edits,
universal shortcut redesign, and discovery UI copy changes. Updated screenshot candidates are kept in
`Builds/AppStoreScreenshots/Candidates-next-build`, separate from the existing
`Candidates`. They are browser-rendered candidates, not native-device captures,
and must be compared against the exact submitted build before submission.
Store capture now uses the native reference scale rather than WebGL's enlarged
phone scale. Safe Area and physical-device differences still need comparison.

The public privacy page loaded on 2026-09-14 but omitted `1 = 1` from its SDK
lists. The local website source now includes this game and distinguishes local
saves from gameplay analytics. Public deployment of that correction is not
verified by this audit.

## App Identity

- App name: `1 = 1`
- Bundle ID: `com.mannlab.games.oneplusoneminusone`
- Category: Games / Puzzle
- Public concept: character-based equation builder
- Internal concept title: `1+1-1*1/1=1`
- Privacy policy URL: `https://games.mannlab.app/privacy`

## Subtitle Candidates

- Build with stick friends
- Living stick logic
- Tiny equation builders

## Promotional Text

Build odd little equations from living stick friends. Empty boxes, flexible answers, and saved discoveries.

## Short Description

Build strange equations with living stick friends.

## Full Description Draft

`1 = 1` is a small logic puzzle about building expressions from living stick
friends.

Each round starts with empty boxes and a handful of animated pieces. Drag,
rotate, and combine them into tokens such as `1`, `11`, `111`, `+`, `-`, `/`,
`×`, `*`, and `=`.

You compose each expression yourself, and the game checks whether the result is
truly correct.
Rounds ask you to build an expression that reaches the displayed `= 1` target,
while alternate valid expressions are still accepted.

Complete 30 compact puzzles, discover alternate answers, and finish with the
odd little equation that started it all. The `Found` button opens your
Discoveries: shape friends, saved expressions, and badges from the answers you
constructed.

When new shapes or expressions are found, short in-game notes such as
`Found: 1` and `Saved expression` keep the discovery loop visible without
showing hints or forcing a single solution.

## Keywords

puzzle,logic,equation,characters,numbers,brain,casual,math

## Screenshot Plan

1. `01-round-1-first-stick`: Round 1 shows the core `1` character and empty-slot construction.
2. `02-round-4-star-start`: Round 4 shows three pieces forming `*`.
3. `03-round-10-title-echo`: Round 10 shows a compact multi-token expression.
4. `04-round-15-tall-fold`: Round 15 shows three pieces combining into `111`.
5. `05-round-20-narrow-path`: Round 20 shows packed neighboring numbers.
6. `06-round-25-wide-loop`: Round 25 shows a longer resource arrangement.
7. `07-round-30-finale`: Round 30 shows the current finale expression.
8. `08-round-select-progression`: Round select progression with 30 focused rounds and locked/open states.

## Review Notes Draft

The app is a local single-player puzzle game. It does not require accounts,
chat, user-generated content, purchases, online leaderboards, or remote gameplay
content.

Guideline 4.3(a) clarification: this updated build and metadata position
`1 = 1` as a character-based equation builder, not a repackaged one-match
repair puzzle. Traditional match-based equation games generally present a
prebuilt incorrect equation and ask the player to move one piece to fix it.
This game starts from empty boxes and a limited set of animated stick
characters. The player constructs the expression from scratch by dragging,
rotating, and combining pieces into tokens such as `1`, `11`, `111`, `+`, `-`,
`/`, `×`, `*`, and `=`.

The solver does not force a single preset answer. It accepts mathematically
valid alternate expressions, and each round is built around the displayed
`= 1` target instead of repairing a preset broken equation. The release set
contains 30 focused rounds, including rounds focused on packed numbers,
operator combinations, and compact fixed-target expression construction. The
top-right `Found` button opens a `Discoveries` screen that records shape
friends, built expressions, and badges, reinforcing that the player is
constructing expressions rather than matching one preset move sequence. The round set was also
reviewed for over-reusable shortcut patterns: late standalone `N / N = 1`
solutions and copied-expression equality samples were removed without adding
token bans or sample-answer enforcement.

Interstitial ads may appear only after newly cleared milestone rounds when
production AdMob IDs are configured. Early tutorial rounds, replayed rounds, and
hard clears after multiple failed checks should not show ads.

The iOS candidate includes Firebase Analytics for gameplay event analytics and
Firebase Crashlytics for crash diagnostics.

No account or login is required. Drag stick characters into boxes and tap them
to rotate. Use all available pieces to reach the target or create a valid
equality, then tap Check. Alternative valid solutions are accepted. Reset
affects the current puzzle only. First launch starts Round 1; returning players
with cleared rounds see round selection.

Suggested review path: Round 1 introduces the living `1` piece and an empty
slot; after the first accepted answer, the app surfaces the `Found: 1` and
`Saved expression` discovery flow. Round 5 shows packed-number division, Round
8 shows a combined expression using all pieces, Round 10 shows a compact
multi-token expression, the top-right `Found` button shows saved shape and
expression discoveries, and later rounds such as Round 22 or Round 30 show
larger expressions that are composed from empty slots rather than repaired from
a preset equation.

The supplied review video shows Rounds 1-10 and entry into Round 11. It is a
replay with prior progress, not a fresh-install or first-ad demonstration. Add
the video as an App Review attachment separately; attachment status is unknown.

## Privacy Disclosure Draft

The game stores progress locally on device. Build 2 includes Firebase and
AdMob; do not answer "Data Not Collected" merely because gameplay is local.
Review collection, purpose, linkage and tracking answers against the exact
submitted SDK configuration and consent behavior. Console receipt, production
consent behavior and App Store Connect answers are not verified by this audit.

Official references last checked for this draft on 2026-09-14:

- [Apple App Privacy Details](https://developer.apple.com/app-store/app-privacy-details/): include relevant third-party SDK practices.
- [Google Mobile Ads disclosure guidance](https://developers.google.com/admob/ios/privacy/data-disclosure): review advertising-related data and the submitted SDK configuration.
- [Firebase Apple-platform disclosure guidance](https://firebase.google.com/docs/ios/app-store-data-collection): evaluate the Firebase products actually included.

## Age Rating Notes

No age rating is assigned by this audit. Complete the current store questionnaire
against the game and third-party advertising; simple visuals do not establish an
all-ages rating.

- No violence
- No chat or user-generated content
- No gambling
- No paid loot or randomized paid rewards
- No mature themes
- Contains third-party ads when production AdMob IDs are configured

## Final Checks Before Upload

- [ ] Fresh WebGL/iOS/Android build created from current source.
- [ ] Device QA tracker signed off.
- [ ] Firebase config added and verified.
- [ ] Production AdMob IDs added and strict readiness passes.
- [ ] App icon regenerated from current `GenerateAppIcon.cs`.
- [ ] Small icon sizes visually checked.
- [ ] Screenshots captured from a fresh non-development store-capture build.
- [ ] `./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh` passes
      after the final screenshot capture.
- [ ] Privacy policy text matches the submitted SDK behavior.
