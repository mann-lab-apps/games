# Yacht Sailing

Unity hyper-casual game project.

## Project

- Unity editor: 6000.3.23f1
- Platform: WebGL / mobile prototype
- Package name: com.mannlab.games.yachtrush
- Namespace: MannLab.Games.YachtRush

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.

## App Review Differentiation

Yacht Sailing is a crew-resource voyage strategy game, not a standard
Yacht/Yahtzee score sheet. The five dice are thrown from a ceramic bowl, but die
position is not the scoring rule. Each face represents a ship resource used by
the crew council to plan the month.

- Die faces map to voyage resources: 1 Sail, 2 Hull, 3 Food, 4 Crew, 5 Gold,
  and 6 Map.
- Each month, the player rolls five dice to gather crew resources, then clicks a
  ship-deck strategy token and spends the listed resources.
- Resources carry over between months and decrease only when a chosen strategy
  spends them.
- Resources have voyage roles: Sail and Map push Distance, Hull and Food keep
  the voyage alive, Crew unlocks mixed plans, and Gold drives port value and
  score.
- Resource pressure is split into three readable pairs: Hull/Food are survival
  meters, Sail/Gold create distance and score pressure, and Crew/Map unlock
  combo or special strategies.
- Strategies include Tailwind Run, Patch the Hull, Stock the Hold, Rally the
  Crew, Port Bargain, Read the Stars, Safe Passage, Long Voyage, Repair Convoy,
  Trade Route, Full Deck, and Captain's Gambit.
- The six basic commands are repeatable monthly plans, while the six composite
  strategies are limited once-per-voyage plays that create timing decisions.
- The 12 strategies are presented as always-visible ship-deck tokens with
  locked/open/best states and a detail modal for cost, missing resources, and effect.
- Each resource cell shows compact gameplay impact states such as DIST LOW,
  HULL RISK, FOOD RISK, SCORE LOW, COMBO LOCKED, or SPECIAL READY. Tapping it opens the
  resource's gain rule, spend rule, upside, shortage impact, and exchange rule.
- Resource shortages do not create hidden passive damage. They lock matching
  token families; HULL 0 or FOOD 0 ends the voyage.
- Each strategy token uses distinct nautical board-game piece artwork, separated
  from the readable command card text.
- The objective is an open high-score voyage: survive 12 months and sail as far
  as possible while managing Distance, Hull, Food, and Gold. The 120 nm route
  mark is a record benchmark, not an instant finish.
- There is no automatic monthly drain. Strategy tokens are the explicit source
  of every resource spend and every resource gain.
- Initial state is Month 1/12, Distance 0 nm, SAIL 0, HULL 18, FOOD 8, CREW 0,
  GOLD 0, and MAP 0.
- The top HUD shows Month, Distance, and Best Distance. The resource row is the
  single ledger for SAIL/HULL/FOOD/CREW/GOLD/MAP.
- Resources decrease only when a selected deck token costs them.
  Distance and Best Distance never decrease during a run.
- Resources cannot be freely exchanged. Strategy tokens are explicit conversion
  recipes, such as FOOD into extra supplies, HULL into repairs, or
  GOLD/MAP/FOOD into trade-route value.
- The 3D bowl throw remains the tactile interaction, while the core decisions
  come from crew resources and nautical strategy selection rather than filling a
  reusable Yacht score table.

App Review note: Yacht Sailing uses familiar numeric dice as physical crew
resource tokens. Gameplay centers on rolling monthly resources, inspecting and
activating ship-deck strategy tokens, spending explicit resource costs, and
trying to sail farther over a 12-month journey.

## Services

- Firebase Analytics/Crashlytics runtime bridge is wired through `FirebaseTelemetry`.
- Add Yacht Sailing's Firebase iOS config at `Assets/GoogleService-Info.plist`, or set `MANNLAB_YACHT_RUSH_FIREBASE_IOS_PLIST` before an iOS build.
- AdMob interstitials are initialized through `MannLabAdMob` at every game over. Test builds use Google test ads; release iOS builds use `ca-app-pub-4525914685149405~8143053169` and `ca-app-pub-4525914685149405/8278784535`.
- Set `MANNLAB_YACHT_RUSH_ADMOB_IOS_APP_ID` for a production iOS AdMob app ID before release builds.

## iOS Build Checks

```bash
scripts/verify-yacht-rush-ios-readiness.sh release
scripts/verify-yacht-rush-ios-readiness.sh crashlytics-test
scripts/verify-yacht-rush-ios-readiness.sh admob-test
```
