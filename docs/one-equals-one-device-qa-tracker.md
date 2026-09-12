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

Latest automated evidence:

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
