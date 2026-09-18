# 1 = 1 4.3(a) Resubmission Plan

## Status

Apple rejected `1 = 1` under Guideline 4.3(a) - Design - Spam. The Resolution
Center clarification has already been sent. This plan tracks the work needed to
prepare a safer resubmission package with updated metadata, review notes,
screenshots/video direction, and a new build candidate if required.

Do not treat the existing uploaded iOS `1.0.0 (2)` binary as current. Local
source and metadata now include changes that are not in that build.

## Rejection Summary

Apple said the app appears to share a similar binary, metadata, and/or concept
with apps submitted by this or other developers, with only minor differences.
The likely issue is not one gameplay bug. It is that the submitted package can
look like another match-based equation puzzle instead of a distinct product.

## Already Completed

- Sent App Review clarification through Resolution Center.
- Added `docs/one-equals-one-app-review-response-4-3a.md`.
- Added `docs/one-equals-one-app-review-4-3a-differentiation.md`.
- Reworked `docs/one-equals-one-store-metadata-draft.md` around a
  character-based equation-builder identity.
- Reworked `prototypes/one-plus-one-minus-one/STORE_READINESS.md` to carry the
  same positioning.
- Updated early tutorial copy in source so the first rounds say "build with
  stick friends" rather than sounding like a one-match repair game.
- Updated WebGL page description metadata to describe a character-based
  equation builder.

## Likely Spam Signals To Reduce

- Generic "stick equation puzzle" wording.
- Any implication that the player moves one stick to repair a prebuilt formula.
- Screenshots that only show a finished equation and not construction from
  empty boxes.
- Review footage that stops before `=`, `111`, `*`, or larger expression
  construction appears.
- A same-build resubmission without visible metadata or first-impression
  changes.

## Product Positioning For Resubmission

Use this wording consistently:

- `1 = 1` is a character-based equation builder.
- The board starts with empty boxes and living stick friends.
- Players compose expressions from scratch.
- Pieces can become `1`, `11`, `111`, `+`, `-`, `/`, `×`, `*`, and `=`.
- Alternate valid answers are accepted.
- Player-built `=` tokens are validated as direct left/right equalities.
- The release has 100 handmade rounds.

Avoid:

- matchstick
- one-match repair
- fix the broken equation
- generic brain training
- template/reskin comparisons except in reviewer-facing clarification.

## Metadata To Enter In App Store Connect

Use `docs/one-equals-one-store-metadata-draft.md` as the source of truth.

Recommended values:

- App name: `1 = 1`
- Subtitle: `Build with stick friends`
- Promotional text: `Build odd little equations from living stick friends. Empty boxes, flexible answers, and 100 handmade rounds.`
- Short description: `Build strange equations with living stick friends.`
- Keywords: `puzzle,logic,equation,characters,numbers,brain,casual,math`

Paste the Review Notes Draft from the same metadata document. It includes the
4.3(a) clarification and reviewer route.

## Review Notes Checklist

The review notes must say:

- This update follows the 4.3(a) clarification.
- The app is not a one-match repair puzzle.
- The player builds expressions from empty boxes.
- Pieces are animated stick characters.
- Multiple token types can be made from the pieces.
- Alternate valid expressions are accepted.
- Direct `=` equality is supported.
- There are 100 handmade rounds.

Reviewer route:

1. Round 1: living `1`.
2. Round 5: constructed `×`.
3. Round 8: `111`.
4. Round 9: `*`.
5. Round 18 or 19: direct equality.
6. Round 30 or 75: larger expression construction.
7. Round 100: finale expression.
8. Round select: 100-round progression.

## Screenshot And Video Package

The previous Round 1-10 footage is useful for control basics but weak for
4.3(a) differentiation. Prepare a new 60-90 second review video.

Shot list:

1. Open on an empty-slot round, before pieces are placed.
2. Show dragging a living `1` stick friend into a box.
3. Show two pieces forming `×` or `+`.
4. Show three pieces forming `111`.
5. Show three pieces forming `*`.
6. Show a player-built `=` equality clearing.
7. Show a later multi-token expression such as Round 30 or 75.
8. Show Round 100 or the round select grid.

Screenshot intent:

- Round 1: character-first identity.
- Round 5: constructed operator token.
- Round 8: packed-number construction.
- Round 9: three-piece `*`.
- Round 30: mid-game expression building.
- Round 75: equality puzzle.
- Round 100: finale density.
- Round select: 100 handmade rounds.

Exclude any screenshot that resembles a generic broken equation repair screen
without visible empty-slot construction or character tokens.

## Build Change Summary

Small source changes made for the safer resubmission candidate:

- Round 1-15 tutorial copy now uses "build" and "stick friends" language.
- WebGL description metadata now says "character-based equation builder".

No rules changed:

- 100 rounds remain.
- Free token recognition remains.
- Alternate answers remain valid.
- Direct equality validation remains.
- No token bans were added.
- No sample-answer enforcement was added.

Because source changed, the next iOS submission should use a new build number.
App Store Connect already has `1.0.0 (2)`, so use build `3` or higher unless
the marketing version changes.

## Verification Commands

Run before packaging:

```sh
bash scripts/verify-one-plus-one-minus-one-store-metadata.sh
bash scripts/verify-one-plus-one-minus-one-static.sh
scripts/check-one-plus-one-minus-one-unity-license.sh
```

If Unity licensing is healthy:

```sh
bash scripts/verify-one-plus-one-minus-one-playmode.sh
bash scripts/verify-one-plus-one-minus-one-webgl-qa.sh
bash scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh
bash scripts/capture-one-plus-one-minus-one-app-store-candidates.sh
bash scripts/verify-one-plus-one-minus-one-app-store-candidates.sh
```

Before a new iOS archive/export:

```sh
bash scripts/verify-one-plus-one-minus-one-ios-readiness.sh release --preflight-only
```

If preflight passes and Unity licensing is healthy, run the iOS release/export
flow requested by the user. Upload and App Store submission remain manual or
separately authorized actions.

## Latest Local Verification

2026-09-18 local checks after the 4.3(a) recovery edits:

- `bash scripts/test-one-plus-one-minus-one-store-metadata.sh`: passed.
- `bash scripts/verify-one-plus-one-minus-one-store-metadata.sh`: passed.
- `git diff --check` for the 4.3(a) recovery files: passed.
- `bash scripts/verify-one-plus-one-minus-one-static.sh`: passed.
- `scripts/check-one-plus-one-minus-one-unity-license.sh`: blocked with
  `Unity licensing client is unavailable`.
- Direct Unity PlayMode, bypassing the wrapper license preflight: passed
  38/38 tests.
  - Result: `/tmp/one-equals-one-playmode-direct.xml`
  - Log: `/tmp/one-equals-one-playmode-direct.log`
- Direct Unity QA WebGL build, bypassing the wrapper license preflight: passed.
  - Build: `prototypes/one-plus-one-minus-one/Builds/WebGL/one-plus-one-minus-one-qa`
  - Log: `/tmp/one-plus-one-minus-one-unity-webgl-qa-direct-build.log`
- Fresh QA WebGL first-ten input regression: passed.
  - Evidence: `/tmp/one-equals-one-webgl-qa-input-20260918`
- QA WebGL Round 48 screen smoke and sample-fill captures: passed.
  - Round screen evidence: `/tmp/one-equals-one-webgl-qa-round48-20260918`
  - Sample evidence: `/tmp/one-equals-one-webgl-qa-round48-sample-20260918`
- Strict iOS release preflight with build `3`, Team ID `ZRA4DHHKQ4`, production
  iOS AdMob IDs, and the local Firebase plist: passed.
- The downloaded provisioning profile was installed locally.
  - UUID: `9ebd16f4-da00-4742-8308-caddccca1f5f`
  - Name: `1 = 1`
  - Bundle ID: `com.mannlab.games.oneplusoneminusone`
  - Installed path:
    `/Users/jaemankim/Library/MobileDevice/Provisioning Profiles/9ebd16f4-da00-4742-8308-caddccca1f5f.mobileprovision`

Open runtime gaps:

- `scripts/check-one-plus-one-minus-one-unity-license.sh` still fails even
  though some direct Unity commands can run. Wrapper PlayMode/WebGL verification
  should not be marked passed until that preflight is healthy.
- Direct ordinary WebGL build and direct iOS Xcode export both hit Unity
  licensing channel/protocol errors during initialization and were stopped.
- A new Round 48 browser touch-regression script exists, but its coordinate
  calibration is not yet reliable enough to use as passing evidence.
- The latest local source has not produced a fresh iOS Xcode export or App
  Store upload candidate yet.

The static suite still reports expected release-readiness warnings for stale
WebGL builds, missing Android Firebase config, missing production release env
values, and incomplete manual device QA. These are not resolved by metadata
changes alone.

The metadata verifier now guards against public full-description language that
looks like generic matchstick or broken-equation repair positioning, while
allowing reviewer-facing clarification documents to discuss the distinction.

## Current Blockers

- Unity licensing may still report `LICENSING_CLIENT_UNAVAILABLE` through the
  helper preflight, and direct Unity can also fail with licensing channel
  handshake errors. Do not loop the same Unity build/export command until Unity
  Hub licensing is repaired or restarted.
- The ordinary WebGL and uploaded iOS binaries are stale relative to local
  source. The QA WebGL build is fresh as of this verification pass.
- A fresh iOS Xcode export for build `3` has not completed.
- Device QA tracker is not signed off.
- iOS Firebase/AdMob inputs are present locally for the next build. Android
  Firebase/signing inputs may still be incomplete.
- New screenshots/video need to be captured from the resubmission candidate,
  not from the stale uploaded iOS build.

## App Store Connect Manual Steps

1. Paste updated subtitle, promotional text, description, keywords, and review
   notes from `docs/one-equals-one-store-metadata-draft.md`.
2. Attach a new 60-90 second review video that shows construction from empty
   slots, `111`, `*`, direct equality, later expressions, and the 100-round
   picker.
3. Replace screenshots if the current set looks like a generic match-based
   equation repair app.
4. If uploading a new build, select build `3` or higher.
5. In the submission notes, mention that this build and metadata clarify the
   distinction discussed in the earlier 4.3(a) Resolution Center reply.

## Resubmission Recommendation

Do not simply resubmit build `1.0.0 (2)` unchanged. The safer package is:

1. Updated metadata.
2. Updated review notes.
3. New review video.
4. New build with the first-impression tutorial wording changes.
5. Fresh screenshots if Unity licensing allows capture.

If Apple explicitly says a text clarification is enough, same-build review may
be possible, but the current recommended path is a new build candidate.
