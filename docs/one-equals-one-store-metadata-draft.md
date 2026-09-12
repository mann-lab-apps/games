# 1 = 1 Store Metadata Draft

This draft is for store-submission preparation. Re-check it against the final
build, privacy policy, Firebase settings, and AdMob settings before upload.

## App Identity

- App name: `1 = 1`
- Bundle ID: `com.mannlab.games.oneplusoneminusone`
- Category: Games / Puzzle
- Public concept: tiny stick equation puzzles
- Internal concept title: `1+1-1*1/1=1`
- Privacy policy URL: `https://games.mannlab.app/privacy`

## Subtitle Candidates

- Tiny stick equation puzzles
- Make math with little sticks
- A tiny logic puzzle

## Short Description

Drag little stick friends into boxes and make each equation work.

## Full Description Draft

`1 = 1` is a small puzzle game about turning simple sticks into numbers and
operators.

Drag, rotate, and combine lively stick friends to build expressions that really
work. A single stick can be `1`, a lazy line can be `-`, crossed sticks can
multiply, and packed sticks can become `11` or `111`.

The rules stay simple, but the solutions get stranger as the rounds grow.
Complete 100 compact puzzles, discover alternate answers, and finish with the
odd little equation that started it all.

## Keywords

puzzle,math,logic,equation,numbers,sticks,brain,casual,minimal

## Screenshot Plan

1. `01-round-1-first-stick`: Round 1 first `1` stick friend.
2. `02-round-5-cross-multiply`: Round 5 multiply discovery.
3. `03-round-8-triple-one`: Round 8 `111` discovery.
4. `04-round-9-star-multiply`: Round 9 star multiply discovery.
5. `05-round-30-medium-expression`: Round 30 medium expression.
6. `06-round-75-equality-puzzle`: Round 75 equality puzzle.
7. `07-round-100-finale`: Round 100 finale expression.
8. `08-round-select-progression`: Round select progression with locked/open states.

## Review Notes Draft

The app is a local single-player puzzle game. It does not require accounts,
chat, user-generated content, purchases, online leaderboards, or remote gameplay
content.

Interstitial ads may appear only after newly cleared milestone rounds when
production AdMob IDs are configured. Early tutorial rounds, replayed rounds, and
hard clears after multiple failed checks should not show ads.

Firebase Analytics may be used for gameplay event analytics, and Firebase
Crashlytics may be used for crash diagnostics when the final build includes
valid Firebase configuration.

## Privacy Disclosure Draft

The game stores progress locally on device. If Firebase and AdMob are enabled in
the submitted build, disclose analytics, crash diagnostics, identifiers,
diagnostics, coarse location, and ad interaction data according to the final SDK
configuration.

## Age Rating Notes

Expected age rating path: likely suitable for all ages, assuming the final store
questionnaire matches the current content.

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
