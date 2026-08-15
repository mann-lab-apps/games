# 2048 Crash App Store Prep

Last updated: 2026-08-06

## Current Release Target

- App name: `2048 Crash`
- Project: `prototypes/2048-crash`
- Bundle ID: `com.mannlab.games.game2048crash`
- Version: `0.1`
- Build number: `1`
- Initial channel: iOS TestFlight, then App Store
- Public web URL: `https://games.mannlab.app/2048-crash/`
- Privacy policy URL: `https://games.mannlab.app/privacy`
- Support URL: `https://games.mannlab.app/`
- Firebase project: `crash-6508f`

Current build assumptions:

- No account system.
- No ads.
- No in-app purchases.
- No user-generated content, chat, camera, microphone, or location permission.
- Best stage and gameplay progress are stored locally on device.
- Firebase Analytics and Firebase Crashlytics are included for app usage events and crash diagnostics.

## Store Listing Copy

### Subtitle

Slide tiles into the crash block

### Promotional Text

Classic sliding-number moves meet a fixed crash target. Build the matching tile, break the special block, and keep the board alive.

### Full Description

2048 Crash is a focused sliding-number puzzle about making the exact tile you need.

Swipe the board like a classic merge puzzle. Regular tiles slide and merge, but one special crash block stays fixed in place. When a regular tile with the same number collides with it, both blocks shatter, your stage increases, and a new special block appears on the same continuing board.

Every stage raises the target value, so each run becomes a compact puzzle of board control, timing, and planning.

Features:

- Simple swipe controls
- Fixed special crash blocks
- Connected stages on one continuing board
- Local best stage
- Short, replayable puzzle runs
- No account required

### Korean Description

2048 Crash는 원하는 숫자를 만들어 특수 블록을 깨는 슬라이드 숫자 퍼즐입니다.

보드는 익숙한 병합 퍼즐처럼 스와이프해서 움직입니다. 일반 블록은 밀리고 합쳐지지만, 색과 패턴이 다른 특수 블록은 제자리에 고정되어 있습니다. 같은 숫자의 일반 블록을 특수 블록에 충돌시키면 두 블록이 함께 깨지고, Stage가 올라가며 같은 보드 위에 다음 특수 블록이 등장합니다.

스테이지가 올라갈수록 목표 숫자가 커지기 때문에, 매 판은 보드를 살려가며 정확한 충돌을 만들어내는 짧은 퍼즐이 됩니다.

## Keywords

2048,crash,number,puzzle,merge,slide,tile,stage,logic,casual

## App Review Information

Paste the following into the App Review Information `Notes` field in App Store Connect. Replace the bracketed device and recording fields with the actual physical-device smoke-test details for the submitted build.

```txt
2048 Crash - App Review Information

1. Screen recording
Screen recording link: [ADD REVIEW-ACCESSIBLE LINK]

The recording was captured on a physical device running the latest available operating system at test time. It begins with launching 2048 Crash from the device home screen and shows the normal gameplay flow: the starting board, swiping regular tiles, merging tiles, creating a tile that matches the fixed crash block, swiping the matching tile into the crash block, advancing Stage, and tapping Again after game over/restart.

The app has no account registration, login, account deletion, paid content, in-app purchases, subscriptions, user-generated content, reporting/blocking flows, camera access, microphone access, location access, contacts access, or App Tracking Transparency prompt.

2. Devices and operating systems tested
- [ADD DEVICE MODEL], [ADD OS VERSION]
- [ADD DEVICE MODEL], [ADD OS VERSION]

3. App functions and target audience
2048 Crash is a simple sliding-number puzzle game for casual puzzle players. The player swipes a 4 x 4 board to slide and merge numbered tiles. A special crash block stays fixed in place; when a regular tile with the same number collides with it, both blocks break, Stage increases, and a new crash block appears. The app solves the need for a short, focused puzzle experience by turning the familiar merge-tile mechanic into a clear target-based challenge with quick replay sessions and local best-stage tracking.

4. Setup and access instructions
No login or demo account is required. No sample files are required.

How to test:
1. Launch the app.
2. Swipe in any direction to move regular tiles.
3. Merge regular tiles until one matches the fixed special block value.
4. Swipe the matching regular tile into the special block.
5. The regular tile and special block break together, Stage increases, and the next special block appears.
6. Continue until there are no valid moves, then tap Again to restart.

5. External services, tools, or platforms
The app uses Firebase Analytics for gameplay event analytics and Firebase Crashlytics for crash diagnostics. Gameplay itself runs locally on the device. Best stage and gameplay progress are stored locally on device. The app does not use authentication services, payment processors, ad networks, AI services, external gameplay services, online leaderboards, or remote content providers.

6. Regional differences
The app functions consistently across all regions. There are no region-specific features, content, pricing, services, or restrictions in the submitted build.

7. Regulated industry or protected third-party material
The app does not operate in a highly regulated industry and does not include protected third-party material requiring additional authorization. It is an original casual puzzle game implementation.

Additional notes
The submitted release build does not include the development-only Crashlytics forced crash trigger. This trigger is compiled only for Unity Editor or development builds and is not present in App Store/TestFlight release builds.
```

### Screen Recording Checklist

Use a physical iPhone or iPad with the latest available OS before resubmission.

1. Start recording before tapping the app icon.
2. Launch `2048 Crash`.
3. Show the initial board and header values.
4. Swipe several times to show tile movement and merging.
5. Crash a regular tile into the fixed special block with the same value.
6. Show the Stage increment and next special block.
7. Reach or force a game over if practical, then tap `Again`.
8. Upload the video to a review-accessible link and paste it into the Notes field above.

## App Privacy Draft

Use this as the App Store Connect privacy answer draft for the current Firebase Analytics/Crashlytics build. Re-check Firebase Console settings before submission.

### Data Collected

- `Identifiers / Device ID`
  - Purpose: `Analytics`, `App Functionality`
  - Linked to user: `Yes`, unless Firebase settings are changed to prevent linkage.
  - Used for tracking: `No`
- `Usage Data / Product Interaction`
  - Purpose: `Analytics`
  - Linked to user: `Yes`, unless Firebase settings are changed to prevent linkage.
  - Used for tracking: `No`
- `Diagnostics / Crash Data`
  - Purpose: `App Functionality`
  - Linked to user: `Yes`, unless Firebase settings are changed to prevent linkage.
  - Used for tracking: `No`
- `Diagnostics / Performance Data`
  - Purpose: `App Functionality`, `Analytics`
  - Linked to user: `Yes`, unless Firebase settings are changed to prevent linkage.
  - Used for tracking: `No`

### Data Not Collected

- Contact info
- Precise or coarse location
- Contacts
- Photos, videos, audio, or files
- Purchases
- Financial information
- Health or fitness information
- User-generated gameplay content

### Tracking

Suggested answer for the current build: `No`.

There are no ads, no IDFA use, and no cross-app advertising measurement in the current implementation. If ad SDKs, attribution SDKs, remarketing, or cross-app identifiers are added later, revisit App Tracking Transparency and the privacy label before submission.

## Age Rating Draft

Current content:

- No violence.
- No sexual content.
- No profanity.
- No medical content.
- No user-generated content.
- No unrestricted web access.
- No gambling, betting, purchases, prizes, or real-money rewards.

Expected age rating path: likely `4+`, assuming the final App Store Connect questionnaire matches the current content.

## Category

- App Store primary category: `Games`
- App Store secondary category candidate: `Puzzle`

## Assets

Generated by:

```sh
node scripts/generate-2048-crash-app-store-assets.mjs
```

The command above delegates to the real WebGL capture script:

```sh
node scripts/capture-2048-crash-webgl-app-store-assets.mjs
```

Expected upload folders:

- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPhone-6.9`
- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPhone-6.5`
- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPad-13`

Current screenshot sets are captured from the live WebGL build through Chrome DevTools, so they should match the playable web version's UI rather than a separate marketing mockup.

Troubleshooting notes for screenshot capture are kept in `docs/games-troubleshooting.md`.

## Submission Checklist

Ready locally:

- iOS bundle ID set to `com.mannlab.games.game2048crash`.
- Firebase iOS plist added.
- Firebase Analytics and Crashlytics SDKs imported.
- iOS release Xcode build script added.
- App icon added and verified as 1024 x 1024 PNG without alpha.
- Crashlytics development test build and hidden trigger added.
- Store listing copy drafted.
- App privacy draft prepared.
- App Store screenshot generator added.

Before TestFlight upload:

- Confirm Apple Developer Program membership and App Store Connect access.
- Confirm bundle ID exists in Apple Developer and App Store Connect.
- Confirm signing team ID. The local default is `ZRA4DHHKQ4`; override with `MANNLAB_APPLE_TEAM_ID` if needed.
- Confirm the App Store provisioning profile `2048 Crash` is installed under `~/Library/MobileDevice/Provisioning Profiles/`.
- Select Xcode 26 or newer with the iOS 26 SDK or newer.
- Run `./scripts/verify-2048-crash-app-store-readiness.sh`.
- Run `REQUIRE_APP_STORE_XCODE=1 ./scripts/verify-2048-crash-ios-readiness.sh`.
- Archive and upload from Xcode.
- Install the TestFlight build on a real iPhone.
- Confirm first open, gameplay, restart, and local best stage.
- Confirm Crashlytics receives a development test crash before submitting the release build.

Before App Review:

- Publish or redeploy the updated `https://games.mannlab.app/privacy` page.
- Complete App Privacy answers in App Store Connect.
- Complete age rating, pricing, and availability.
- Upload screenshots and metadata.
- Confirm release build is not a development build.

## Naming Risk Note

`2048` is already associated with official apps and trademark listings for games. `2048 Crash` is descriptive and differentiated by the crash-block mechanic, but this should be treated as a trademark clearance item before public launch. If the risk feels uncomfortable, use a more distinctive public title and keep `2048-crash` only as the internal project slug.

References:

- Apple app privacy details: https://developer.apple.com/app-store/app-privacy-details/
- Apple screenshot specifications: https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/
- Apple upcoming requirements: https://developer.apple.com/news/upcoming-requirements/
- Firebase App Store data disclosure guide: https://firebase.google.com/docs/ios/app-store-data-collection
- Ubisoft 2048 brand page: https://www.ubisoft.com/en-us/company/about-us/our-brands/2048
- Mann Lab Games troubleshooting: `docs/games-troubleshooting.md`
