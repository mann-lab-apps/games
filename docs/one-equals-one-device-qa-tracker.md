# 1 = 1 Device QA Tracker

Use this tracker after a fresh Unity build. Do not mark an item done from source
inspection alone.

## Build Under Test

- Build date:
- Git commit or local diff note:
- Platform:
- Version:
- Tester:

## Automated Viewport Smoke

### 2026-09-16 Numerical Fix Runtime Pending

- Local source now includes the exact-rational correctness fix, the Round 48
  bad-answer rejection in source/Node, and the 12 non-callback round-identity
  redesigns. Uploaded iOS `1.0.0 (2)` and the existing WebGL player do not
  include this local candidate.
- Static evidence currently passes: Node identity 46/46, 100-round static
  verification, round quality report, icon/character/release-safety/store
  metadata checks and existing WebGL shell/smoke checks. The existing WebGL
  smoke passes only as old-build evidence and reports stale source warnings.
- Unity runtime evidence is still blocked by `LICENSING_CLIENT_UNAVAILABLE`.
  PlayMode, QA WebGL, ordinary WebGL, store-capture WebGL, and native readiness
  scripts share the same license preflight helper and fast-fail instead of
  launching Unity into the known timeout.
- Ship-ready now runs one Unity licensing preflight, then skips Unity-gated
  checks when that preflight fails. The latest ship-ready run still fails on
  Unity licensing, stale WebGL artifacts/captures, strict release env, strict
  device QA and strict Firebase/AdMob config. This is not a launch-ready build.
- Outside-sandbox viewport smoke passes on the older WebGL build, and generated
  iPhone SE, standard/large iPhone, Android 20:9 and desktop captures under
  `/tmp/one-equals-one-webgl-viewports/` were available for clipping/overlap
  inspection. These captures do not validate the latest numeric fix.
- Next runtime QA after Unity Hub licensing is repaired: PlayMode, fresh QA
  WebGL, actual input confirmation that Round 48 rejects `1 / 11 111`, normal
  Round 48 solution acceptance, representative changed-round inputs, ordinary
  WebGL rebuild and fresh screenshot/candidate capture verification.

### 2026-09-15 Batch 4 Browser Verification Recovered

- Same reviewed PlayMode retry: 37/37; fresh QA/ordinary/capture WebGL builds.
- Screenshot-guided CDP touch clears 10/11/12, 32/33/34, 47/48/49, 82/83/84
  at 390x844. Small 320x568 clears sample 11 and alternatives in 33/48/83.
- Ordinary first-ten, touch cancellation, secondary-release/focus-loss and
  reload-to-Round-11 checks pass; restored round selection screenshot inspected.
- Five ordinary startup sizes pass; SE/desktop screenshots inspected. Targeted
  changed-round screenshots also inspected. These do not replace device rows.
- Zero-target bug reproduced in QA via actual input `1 / 11 111`: Round 48
  incorrectly clears. A passing reproduction harness is NOT defect resolution.
- Evidence folders: `batch4-input`, `batch4-small`, `batch4-firstten`,
  `batch4-viewports`, `batch4-tolerance` in the existing identity artifact folder.
- Prior approval blockage below resolved. No native/device/SDK signoff implied.

### 2026-09-15 Round Identity Batch 4: Not Run In Player

- Follow-up Node tooling: 32/32; 98 complete canonical solution inventories,
  61/78 still limited. These are not device or player tests. Add native/input
  reproduction of target-zero `1 / 11 111` acceptance to pending correctness QA.
- The next same-command PlayMode request also failed before starting in the
  approval service. There are still no fresh batch 4 player results.
- Source edits 11/33/48/83 pass EditMode 47/47 and all sample/parity checks.
  Node tooling passes 26/26. These are not runtime/input passes.
- PlayMode launch was denied before starting by approval-service capacity errors.
  Fresh QA/ordinary/capture builds and touch checks have not run for these edits.
- Current WebGL outputs match batch 3 hashes. Its earlier screenshots/input
  results do not validate batch 4. Native iOS build 2 is unchanged as well.
- Pending: 10/11/12, 32/33/34, 47/48/49, 82/83/84 actual input, 320x568 sample
  11 and alternate answers in 33/48/83, followed by relevant save/viewport checks.
  See `batch4-verification-status.json` in the existing identity evidence folder.

### 2026-09-15 Round Identity Batch 3

- Revised 35/43/57/75/88 and ten neighbors pass actual browser touch at 390x844.
  All five alternate answers pass at 320x568, with filled captures inspected.
- Round 57's first small-screen run fails only screenshot coordinate detection:
  touching outlines merge into six components although eight boxes are visible.
  Fill-based interior detection separates all eight, and repeated actual inputs
  pass on the unchanged game binary. Failure evidence is retained.
- EditMode 43/43, PlayMode 37/37, Node 17/17, static and three fresh WebGL builds
  pass. Ordinary first-ten/save and a separate five-viewport run also pass.
- Evidence prefix: `artifacts/one-equals-one/2026-09-15-round-identity/batch3-*`.
  Physical device, audio, operational SDK and human difficulty remain unverified.
  iOS build 2 is unchanged. Next 11/33/48 candidate is not yet applied.

### 2026-09-15 Round Identity Batch 2

- Revised 38/39/52/70/90 and eight neighbors pass actual QA browser touch at
  390x844. All five alternate answers also pass at 320x568; filled captures
  were inspected for targets, triple sticks and cross/star readability.
- EditMode 35/35, PlayMode 37/37, static and QA/ordinary/capture WebGL pass.
  Ordinary startup renders at five sizes; small/desktop edges were inspected.
- Evidence prefix: `artifacts/one-equals-one/2026-09-15-round-identity/batch2-*`.
  This is not physical-device, audio, SDK or human difficulty signoff, nor a
  new natural 100-round playthrough. Saved indices/keys and ad policy are unchanged.

### 2026-09-15 Round Identity Candidate

- Forty changed indices have final-expression CDP touch evidence, with additional
  neighbor coverage. At 320x568, 34/84/100 pass actual rotation/drop/check.
- Final ordinary first-ten/save and five inspected viewports pass. A separate
  QA-filled end-state fixture checks actual Check, all nine picker pages,
  Sound off, query-free reload and Reset preserving completion.
- EditMode 31/31, PlayMode 37/37 and fresh QA/ordinary/capture WebGL pass.
  Evidence: `artifacts/one-equals-one/2026-09-15-round-identity/`.
- These are browser/Editor results, not physical touch, audio, native SDK or
  human pacing signoff. Remaining shared equality answers are a separate open
  design issue, not resolved by these tests. No new native deployment occurred.

### 2026-09-15 Post-Checkpoint Input Follow-Up

- Base `26d8508a` plus pointer-ownership fix, not a new native distribution.
  The pending 36 PlayMode tests pass; a new stale-finger interleaving then
  fails 36/37 before correction and passes 37/37 afterward. EditMode 29/29 pass.
- Fresh QA browser verifies actual hidden-tab drag cancellation, all three
  `111` return/transfer paths and old/fresh two-finger releases. The CDP test
  now uses the proper active-point-set update for a partial touch release.
- Ordinary first-ten/save replay and five inspected viewport captures pass.
  Actual-input 96-100 includes an alternative equality, hard-clear/replay ad
  exclusions and the final completion/picker state. Initial progress was QA
  seeded through 96; this is not an unassisted full-game human playthrough.
- QA, ordinary and capture WebGL outputs were rebuilt. Test XML, browser
  traces/captures and observation results live under
  `artifacts/one-equals-one/2026-09-15-post-checkpoint/`.
- Same final binary/browser observed for 30m18s, 66 samples and no captured
  runtime exceptions. A separate subsequent reload retains completion and
  Sound off. Browser object/heap trends do not establish native memory or FPS.
- Physical touch, native performance, audio listening and production SDK
  callbacks remain unverified. Do not fill physical signoff from these results.

### 2026-09-14 Continuous Input And Font Follow-Up

- Local fixes beyond build 2: cancellation restores feedback; a press started
  during another drag cannot become a late rotation after cancellation; picker
  navigation and Close retain their position on the short final page.
- PlayMode 31/31 passes. New cancellation/secondary-pointer tests failed before
  the fix; the extended picker test reproduced movement on page 9 before its fix.
- 60 mixed-input/resize cycles and 120 settled idle frames retain generated
  target/recognition glyphs; zero atlas rebuild events in that Editor run. Not
  an allocation profile or physical-device performance verdict.
- Bounded browser QA includes 26 placement/rotation/return/Reset cycles,
  touch cancellation, synthetic focus loss with late second-pointer release,
  held-drag resize, nine picker pages and actual-input 96 -> 97. Final picker
  8 <-> 9 navigation and Close replay pass at 390x844 and 320x568.
- Ordinary WebGL first-ten input regression now includes both cancellation
  cases; clear results, first-clear ad eligibility and saved progress 11 pass.
  Actual production ad delivery, native backgrounding and device touch remain
  unverified. `devicectl` still lists no devices; nothing was installed/uploaded.
- Evidence: `artifacts/one-equals-one/2026-09-14-continuous-quality/`.
  Do not use these browser/Editor results to fill the physical signoff rows.

### 2026-09-14 Post-Distribution Evidence

- Supplied 87.989-second iOS screen recording sampled by timestamp: replay from
  Round 1 through Round 10, entry into 11, prior progress already present. Build
  and physical model not visible; this is not first-install/first-ad evidence.
- Video 64/68s shows wrapped `= 111`; local target-line fix passes all 100 rounds
  at four logical safe widths. No physical capture of the fix yet.
- Final 28 PlayMode cases pass, including material invalidation and settled
  idle-font tests. Browser 96-100 input, completion/reload/reset/replay/Sound
  persistence and first-ten ordinary-release input regression pass.
- Current QA/store/release WebGL builds and five viewport smokes pass. 24
  next-build store candidates pass image checks, using native reference scale.
  They remain browser captures, not replacements for device comparison.
- Evidence root: `artifacts/one-equals-one/2026-09-14-post-distribution/`.
- `devicectl list devices`: no devices. Native target rendering over long
  sessions, consent/ad/crash callbacks, audio and real-touch feel remain open.
- Current code is newer than uploaded iOS `1.0.0 (2)`. No new native distribution,
  public privacy-site deployment or manual signoff occurred in this pass.

### 2026-09-13 Controller And Browser Input Evidence

- Base commit: `65578f4f` plus local `1 = 1` input/progress/layout fixes.
- `./scripts/verify-one-plus-one-minus-one-playmode.sh`: 16 passing tests;
  includes runtime layout checks across all 100 rounds and all nine picker pages,
  44px picker button height, 12px labels and full generated text height.
- Sound checkbox persistence, reset behavior and local AudioSource mute are
  covered by controller tests; this is not speaker/headphone listening signoff.
- Conditional Privacy action visibility/layout is tested. UMP consent form
  reopening and changed consent on a configured native build remain unverified.
- Unity EditMode: 15 passing tests, including arithmetic/equality boundaries.
- Isolated Chrome mobile emulation at 390x844 / DPR 3: Rounds 1-10 solved using
  touch input, no `qaFillSample`; first-clear events, ad cadence, and reload of
  saved progress confirmed. Report: `/tmp/one-equals-one-input-smoke/input/results.json`.
- Alternate Round 5 `1 = 1` was also touch-built without sample fill. Round 39
  used sample fill followed by actual touch rotation `+` to `=` and back;
  equality hides only the fixed target and leaves slot geometry unchanged.
  Round 100 sample-filled clear/reload confirms persisted `Done` on page 9.
  These assisted late-round checks are not manual full solutions.
- These checks do not sign off the manual device/touch/audio/SDK rows below.
  Final layout/platform-guard/equality/Sound build and input refresh passed on 2026-09-13.
  Release, QA and store-capture builds, key rounds/pages and store candidates
  passed. Logs: `/tmp/one-equals-one-ship-ready-final.log` and
  `/tmp/one-equals-one-visual-refresh-final.log`.
- Native build-only evidence on 2026-09-13: fresh Android AdMob test APK passes;
  iOS test export passes but unsigned Xcode build fails in the Crashlytics script
  on missing `GoogleService-Info.plist`. No attached iOS or Android devices were
  reported by devicectl/ADB. Manual build-under-test fields below remain blank.

Run this after every fresh WebGL build:

```sh
./scripts/smoke-one-plus-one-minus-one-webgl-viewports.mjs
```

Expected output is one screenshot per viewport under
`/tmp/one-equals-one-webgl-viewports` unless `ONE_EQUALS_ONE_VIEWPORT_SMOKE_DIR`
is set.

For key-round screenshots, build the development QA WebGL target and capture the
round set:

```sh
./scripts/verify-one-plus-one-minus-one-webgl-qa.sh
./scripts/capture-one-plus-one-minus-one-webgl-qa-rounds.sh
./scripts/capture-one-plus-one-minus-one-round-select-pages.sh
./scripts/verify-one-plus-one-minus-one-qa-captures.sh
```

When both QA captures and App Store candidates are stale, run the combined
refresh command after activating the Unity license:

```sh
./scripts/refresh-one-plus-one-minus-one-visual-qa.sh
```

The QA build accepts development-only `qaRound`, `qaUnlocked`, `qaRounds`, and
`qaRoundPage` URL parameters; release builds ignore them.
The QA capture verifier rejects missing captures, stale captures older than the
QA build, and screenshots with unexpected viewport dimensions.
Static round verification also checks expression layout plans at 320, 390, and
488px widths, including compact 4-row wrapping and slot aspect stability.
It also checks tall portrait stage placement so 19.5:9-20:9 phone browsers do
not leave excessive empty space above the game.

For App Store candidate screenshots, use the non-development store-capture
target instead of the development QA build:

```sh
./scripts/verify-one-plus-one-minus-one-webgl-store-capture.sh
./scripts/capture-one-plus-one-minus-one-app-store-candidates.sh
./scripts/verify-one-plus-one-minus-one-app-store-candidates.sh
```

The candidate verifier rejects stale screenshots when project source is newer
than the store-capture build, when PNGs are older than that build, or when the
PNG visual-content check fails.

Earlier automated evidence (September 11-12):

- Release viewport smoke: `/tmp/one-equals-one-webgl-viewports`
- QA key-round captures: `/tmp/one-equals-one-webgl-qa-rounds`
- Round-select page captures: `/tmp/one-equals-one-round-select-pages`
- App Store candidate captures:
  `prototypes/one-plus-one-minus-one/Builds/AppStoreScreenshots/Candidates`
- Confirmed after fresh WebGL builds on 2026-09-11 and 2026-09-12.
- Fresh QA WebGL, key-round captures for Rounds 1, 5, 8, 9, 16, 30, 50,
  75, 90, and 100, round-select page captures, store-capture build, and App
  Store candidate captures passed on 2026-09-12.
- Outside-sandbox viewport smoke ran again on 2026-09-12 after a fresh release
  WebGL rebuild. iPhone SE, standard iPhone, large iPhone, Android 20:9, and
  desktop passed the stricter content-top smoke. Android 20:9 and tall iPhones
  still have generous first-screen whitespace, so final real-device QA should
  judge whether the opening screen feels too sparse.
- Visual spot check on 2026-09-12: Round-select pages 1 and 9 fit standard
  iPhone portrait after the panel lift; page 9 now compacts around its two
  visible rows, and the disabled next-page button is visibly muted.
- Visual spot check on 2026-09-12: tutorial key rounds 1, 5, 8, 9; post-
  tutorial Round 16; and late Rounds 30, 50, 75, 90, and 100 automated
  captures passed across iPhone SE, standard iPhone, large iPhone, Android
  20:9, and desktop. Full real touch/device QA remains open.
- Visual spot check on 2026-09-12: App Store candidate screenshots were
  recaptured from a non-development store-capture build and verified for
  required sizes, no alpha, and freshness.
- PNG visual verification on 2026-09-12 passed for QA key-round captures,
  round-select page captures, and App Store candidate screenshots. The verifier
  checks PNG dimensions, App Store alpha policy, dark/light content, and mobile
  content-start position.
- Icon visual verification on 2026-09-12 passed for the source icon and WebGL
  favicon at 180, 120, 64, and 32 px, including left/center/right content
  contrast for the `1 = 1` composition.
- Full visual QA refresh on 2026-09-12 rebuilt release, QA, and store-capture
  WebGL targets, recaptured QA rounds/pages and App Store candidates, and
  cleared the previous stale-capture warnings after WebGL metadata changed.
- App Store candidate capture on 2026-09-12 now has an 8-shot set: Round 1,
  Round 5, Round 8, Round 9, Round 30, Round 75, Round 100, and round select.
  Verification rejects unexpected old numbered candidate folders.
- QA round/page capture verification on 2026-09-12 rejects unexpected managed
  `round-*` and `page-*` folders so stale visual evidence cannot remain mixed
  with the current QA set.
- Release-env preflight on 2026-09-12 passes with warnings in development mode
  and intentionally fails in strict mode when Firebase configs, production
  AdMob IDs, version envs, or Android signing envs are absent or malformed.
- Device QA signoff preflight on 2026-09-12 passes with warnings in development
  mode and intentionally fails in strict mode while build-under-test fields,
  WebGL mobile browser coverage, touch/character/audio/ad/privacy rows, or
  real-device QA notes remain incomplete.

## Screen Matrix

| Device / Viewport | Round 1-10 | Round 16 | Round 30 | Round 50 | Round 75 | Round 90 | Round 100 | Round Select 1-9 | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| iPhone SE portrait | key automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | real touch QA still needed |
| Standard iPhone portrait | key automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | real touch QA still needed |
| Large iPhone portrait | key automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | real touch QA still needed |
| Android 20:9 portrait | key automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | real touch QA and first-screen density judgment still needed |
| WebGL desktop browser | key automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | automated pass | browser smoke pass; manual playthrough still useful |
| WebGL mobile browser |  |  |  |  |  |  |  |  | not run |

## Touch And Feel

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Bank drag | Stick picks up immediately and remains visible above finger. | not run |  |
| Slot hover | Target slot highlights while pointer is over it. | not run |  |
| Drop | Stick snaps into the intended slot without surprising rotation. | not run |  |
| Drag-out return | Placed stick dragged outside returns to the bank. | not run |  |
| Placed tap rotate | Tapping a placed stick cycles pose clearly. | not run |  |
| Max 3 sticks | Fourth stick attempt gives a clear, gentle failure. | not run |  |
| Check disabled | Disabled Check button is visually distinct. | not run |  |
| Failure recovery | After a failed check, the next action is obvious. | not run |  |

## Character Readability

| Token | Expected Read | Personality Target | Status | Notes |
| --- | --- | --- | --- | --- |
| `1` | Immediate `1` | chatty core mascot | not run |  |
| `-` | Immediate minus | lazy, relaxed | not run |  |
| `/` | Immediate divide | tilted, playful | not run |  |
| `+` | Immediate plus | bright single friend | not run |  |
| `×` | Immediate multiply | crossed duo | not run |  |
| `*` | Immediate star multiply | energetic three-stick variant | not run |  |
| `=` | Immediate equals | two teammate friends | not run |  |
| `11` | Immediate eleven | two `1` friends | not run |  |
| `111` | Immediate triple one | three `1` friends | not run |  |

## Audio

| Cue | Expected | Status | Notes |
| --- | --- | --- | --- |
| Button | Quiet click, not harsh. | not run |  |
| Pick up | Small lift cue. | not run |  |
| Drop | Satisfying but gentle. | not run |  |
| Rotate | Light chirp, not annoying when repeated. | not run |  |
| Fail | Soft negative cue, not punishing. | not run |  |
| Success | Pleasant clear cue. | not run |  |
| Finale | Slightly special, not too long. | not run |  |

## Ads And Analytics

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Round 1-5 | No interstitial opportunity shown to player. | not run |  |
| Round 10 clear | Interstitial opportunity eligible for new clear. | not run |  |
| Replay clear | No interstitial opportunity shown to player. | not run |  |
| Hard clear | Interstitial skipped after 3+ failed checks. | not run |  |
| Ad close | Next round continues normally. | not run |  |
| Analytics | `app_open`, `round_start`, `round_clear`, `round_check_failed`, `ad_interstitial_opportunity`. | not run |  |
| Crashlytics | Development test crash uploads after relaunch. | not run |  |

## Store Privacy

| Check | Expected | Status | Notes |
| --- | --- | --- | --- |
| Apple privacy manifest | `PrivacyInfo.xcprivacy` is present in the exported app bundle and declares app-local UserDefaults reason `CA92.1`. | automated source check pass | Re-confirm in exported Xcode project after Unity license is active. |
| App privacy labels | Firebase Analytics, Crashlytics, and AdMob disclosures match the final production SDK configuration. | not run | Requires production Firebase/AdMob config. |

## Sign-Off

- Release blocker count: external production settings remain
- Manual QA risks: touch feel, SFX loudness, real-device safe area, first-screen density
- External blockers: Firebase iOS/Android configs, production AdMob IDs, Android signing env, release version envs
- Ready for store screenshots: yes for current art/round data; recheck after production SDK config
- Ready for TestFlight/internal test: no, production Firebase/AdMob and iOS release envs still missing
