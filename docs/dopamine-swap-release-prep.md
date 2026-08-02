# Dopamine Swap Release Prep

Last updated: 2026-08-02

## Current Release Target

- App name: `Dopamine Swap`
- Project: `prototypes/dopamine-swap`
- Package / bundle ID: `com.mannlab.games.dopamineswap`
- Version: `0.1`
- Initial channel: web prototype and mobile internal testing
- Public web URL: `https://games.mannlab.app/dopamine-swap/`
- Privacy policy URL: `https://games.mannlab.app/privacy`
- Support URL: `https://games.mannlab.app/`

Current build assumptions:

- No account system.
- No ads.
- No in-app purchases.
- No third-party analytics SDK in the Unity mobile build.
- No networked gameplay or online leaderboard.
- Best score is stored locally on the device only.

If ads, Firebase, attribution, crash reporting, or online leaderboards are added, update this document, the privacy policy, Google Play Data safety, and App Store privacy details before review.

## Store Listing Copy

### Short Description

Swipe fast. Let the timer choose your card.

### Full Description

Dopamine Swap is a quick card-swapping score game built for short, tense runs.

Each round shows one card and one opponent score. Swipe the card to reroll a random number from 1 to 100. When the timer reaches zero, the card on screen is locked in automatically.

If your card is higher than the opponent score, you win the round and add that card to your score. If it is lower or tied, the run ends.

Early rounds show the exact opponent score. Later rounds gradually hide the opponent behind a narrow score range, making each final card feel riskier.

Features:

- One-card swipe rerolls
- Full-random 1-100 card values
- Timer-based automatic comparison
- Fast rounds for quick retries
- Local best score
- No account required

### Korean Description

Dopamine Swap은 짧고 빠르게 즐기는 카드 스왑 점수 게임입니다.

라운드마다 카드 한 장과 상대 점수가 표시됩니다. 카드를 스와이프하면 1부터 100까지의 숫자 중 하나로 랜덤하게 바뀝니다. 타이머가 0초가 되는 순간 화면에 떠 있는 카드가 자동으로 확정됩니다.

내 카드가 상대 점수보다 높으면 라운드를 이기고 점수가 누적됩니다. 낮거나 같으면 런이 종료됩니다.

초반 라운드는 상대 점수를 정확히 보여주고, 이후 라운드부터는 좁은 범위 힌트가 점점 넓어집니다.

### App Store Subtitle

Swipe the timer card

### App Store Promotional Text

Swipe through random cards before the timer hits zero. Win the round if your final card beats the opponent.

### Keywords

card,random,swipe,casual,arcade,score,timer,risk,quick,number

## Review Notes

No login or demo account is required.

How to test:

1. Launch the app.
2. Swipe the visible card up or down to reroll the number.
3. Wait for the timer to reach zero.
4. The visible card is automatically compared against the opponent score.
5. If the card is higher, the next round starts. If it is lower or tied, the run ends.
6. Tap `Again` to restart.

The app does not include ads, in-app purchases, user accounts, user-generated content, chat, location access, camera access, microphone access, or external gameplay services.

## Privacy And Data Safety Draft

### Google Play Data Safety

Suggested answers for the current no-SDK mobile build:

- Does the app collect or share any required user data types? `No`
- Is all user data collected encrypted in transit? `Not applicable`
- Can users request that data be deleted? `Not applicable`
- Does the app share data with third parties? `No`
- Does the app use advertising ID? `No`

Local-only gameplay state:

- The app stores best score locally on the device.
- This local score is not transmitted to Mannlab or third parties.

### App Store Privacy Details

Suggested answers for the current no-SDK mobile build:

- Data collected: `No`
- Data linked to user: `No`
- Data used to track user: `No`
- Third-party advertising: `No`
- Third-party analytics SDK: `No`

## Age Rating Draft

Current content:

- No violence.
- No sexual content.
- No profanity.
- No medical content.
- No user-generated content.
- No unrestricted web access.
- No real-money gambling.
- No purchases or prizes.

The game uses random card numbers and risk/reward scoring, but it does not contain betting, cash-out, casino simulation, or real-money rewards. Answer store age-rating questionnaires based on the final wording shown in the console.

## Category

- Google Play category: `Game / Casual`
- App Store primary category: `Games`
- App Store secondary category candidate: `Casual` or `Card`

## Release Notes

Initial prototype release.

- Swipe one visible card to reroll a random 1-100 value.
- The card visible at zero seconds is compared automatically.
- Score continues until the final card fails to beat the opponent.
- Local best score is saved on device.

## Submission Checklist

Ready:

- Playable web prototype deployed.
- Package ID selected: `com.mannlab.games.dopamineswap`.
- Store listing draft prepared.
- Review notes prepared.
- Privacy and data-safety draft prepared for the no-SDK build.

Before Google Play internal testing:

- Add an Android AAB build script for Dopamine Swap or generalize the `10000` script.
- Confirm target SDK is API 36 or higher for submissions on or after 2026-08-31.
- Create or reuse Android upload key strategy.
- Build a signed `.aab`.
- Test install on at least one real Android device.
- Prepare app icon and phone screenshots.
- Complete content rating and Data safety in Play Console.

Before App Store / TestFlight:

- Confirm Xcode 26 or newer is installed and selected.
- Add an iOS build script for Dopamine Swap or generalize the `10000` script.
- Create App Store Connect record for `com.mannlab.games.dopamineswap`.
- Build iOS archive with a non-development build.
- Prepare iPhone screenshots and optional iPad screenshots.
- Complete App privacy, age rating, pricing, and availability.

## Official Requirements Checked

- Google Play target API level: new apps and updates must target Android 16 / API 36 or higher starting 2026-08-31.
- Google Play Data safety: all published apps, including testing tracks, must complete the form.
- Apple SDK requirement: App Store Connect uploads require Xcode 26 or later with iOS 26 SDK or later.
- Apple App privacy details: privacy practices and third-party partner data collection must be declared in App Store Connect.

References:

- Google Play target API: https://developer.android.com/google/play/requirements/target-sdk
- Google Play Data safety: https://support.google.com/googleplay/android-developer/answer/10787469
- Apple upcoming requirements: https://developer.apple.com/news/upcoming-requirements/
- Apple App privacy details: https://developer.apple.com/app-store/app-privacy-details/
