# 1 = 1 Store Metadata Draft

This draft is for store-submission preparation. Re-check it against the final
build, privacy policy, Firebase settings, and AdMob settings before upload.

## Candidate Boundary (2026-09-14)

iOS `1.0.0 (2)` was uploaded, but Apple processing/review status is not verified.
The current local target-line/font-material fixes are newer than build 2 and
need a new build number before distribution. Updated screenshot candidates are
kept in `Builds/AppStoreScreenshots/Candidates-next-build`, separate from the
existing `Candidates`. They are browser-rendered candidates, not native-device
captures, and must be compared against the next iOS build before submission.
Store capture now uses the native reference scale rather than WebGL's enlarged
phone scale. Safe Area and physical-device differences still need comparison.

The public privacy page loaded on 2026-09-14 but omitted `1 = 1` from its SDK
lists. The local website source now includes this game and distinguishes local
saves from gameplay analytics. This correction has not been deployed publicly.

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

The iOS candidate includes Firebase Analytics for gameplay event analytics and
Firebase Crashlytics for crash diagnostics.

No account or login is required. Drag sticks into boxes and tap them to rotate.
Use all available sticks to reach the target or create a valid equality, then
tap Check. Alternative valid solutions are accepted. Reset affects the current
puzzle only. First launch starts Round 1; returning players with cleared rounds
see round selection.

The supplied review video shows Rounds 1-10 and entry into Round 11. It is a
replay with prior progress, not a fresh-install or first-ad demonstration. Add
the video as an App Review attachment separately; attachment status is unknown.

## Privacy Disclosure Draft

The game stores progress locally on device. Build 2 includes Firebase and
AdMob; do not answer "Data Not Collected" merely because gameplay is local.
Review collection, purpose, linkage and tracking answers against the exact
submitted SDK configuration and consent behavior. Console receipt, production
consent behavior and App Store Connect answers are not verified by this audit.

Official references checked on 2026-09-14:

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
