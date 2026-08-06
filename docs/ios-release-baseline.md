# iOS Release Baseline

Use this as the minimum checklist before submitting a Unity game to App Store Connect from an individual Apple Developer Program account.

## Current Store Baseline

As of July 27, 2026:

- Apple Developer Program costs 99 USD per membership year, or local currency where available.
- Apps uploaded to App Store Connect must be built with the iOS and iPadOS 26 SDK or later.
- App Store Connect submission requires a paid Apple Developer Program membership.

References:

- Apple Developer Program enrollment: https://developer.apple.com/kr/help/account/membership/program-enrollment/
- App Store submission requirements: https://developer.apple.com/app-store/submitting/
- App Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- App privacy details: https://developer.apple.com/app-store/app-privacy-details/

## Individual Account Requirements

- Apple Account with two-factor authentication enabled.
- Apple Developer Program individual enrollment completed.
- Legal name accepted for App Store display as the developer name.
- App Store Connect access after membership activation.
- Tax and banking information if the app will have paid downloads, in-app purchases, subscriptions, or Apple-paid proceeds.
- Ad network account and payment details if ad monetization is added later.

## Local Build Requirements

- Xcode 26 or newer selected with `xcode-select`.
- Unity 6000.3.20f1 with iOS Build Support installed.
- iOS bundle identifier reserved in Apple Developer/App Store Connect.
- Stable bundle identifier: `com.mannlab.games.game10000`.
- Versioning: increment `CFBundleShortVersionString` for releases and `CFBundleVersion` for every uploaded build.
- Signing: automatic signing with the individual developer team is the default path.
- For manually downloaded App Store profiles, install `.mobileprovision` files under `~/Library/MobileDevice/Provisioning Profiles/` and use `Product > Archive`, not device `Run`.

## Unity Build Settings

- Platform: iOS.
- Build output: Xcode project, then Xcode archive upload.
- Scripting backend: IL2CPP.
- Architecture: ARM64.
- Orientation: portrait.
- Minimum iOS version: keep at `15.0` unless device coverage or SDK requirements change.
- Development Build disabled for App Store/TestFlight uploads.

## App Store Connect Metadata

- App name.
- Subtitle and promotional text, if used.
- Short marketing description and full description.
- Keywords.
- Support URL.
- Privacy policy URL.
- App category: Games.
- Age rating questionnaire.
- App privacy nutrition label.
- Screenshots: 1 to 10 images per required device size, PNG/JPEG, no alpha channel.
- Review notes and demo account only if the app needs special access.

## First Submission Checklist

1. Enroll in Apple Developer Program as an individual.
2. Install Xcode 26 or newer and select it with `xcode-select`.
3. Add iOS Build Support to Unity 6000.3.20f1.
4. Create the App Store Connect app record.
5. Reserve bundle identifier `com.mannlab.games.game10000`.
6. Run local MVP verification.
7. Build the Unity iOS Xcode project.
8. Archive and upload from Xcode.
9. Complete privacy, age rating, pricing, and availability.
10. Add screenshots and metadata.
11. Submit first through TestFlight for smoke testing.
12. Submit to App Review when TestFlight smoke test passes.

## Ads Later

- Update App Privacy details before submitting the ad-enabled build.
- Add App Tracking Transparency if the SDK tracks users or accesses IDFA.
- Keep test ads enabled until release verification is complete.
- Confirm ad content is appropriate for the app age rating.
- Add SKAdNetwork IDs required by the selected ad network.

## Troubleshooting

See `docs/games-troubleshooting.md` for recurring signing, provisioning, Xcode warning, WebGL screenshot, Firebase, and GitHub Pages deployment issues.
