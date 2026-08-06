# Mann Lab Games Troubleshooting

Last updated: 2026-08-06

만랩 게임즈에서 Unity/WebGL/iOS/App Store 배포 중 실제로 만난 문제와 결론을 모아둔다.

## iOS Provisioning Profiles

### `.mobileprovision` 파일 위치

Apple Developer에서 내려받은 프로비저닝 프로필은 Unity 프로젝트 안에 넣지 않는다. Xcode가 읽는 사용자 폴더에 UUID 파일명으로 설치한다.

```sh
security cms -D -i ~/Downloads/2048_Crash.mobileprovision > /tmp/profile.plist
plutil -extract UUID raw -o - /tmp/profile.plist
plutil -extract Name raw -o - /tmp/profile.plist
plutil -extract Entitlements.application-identifier raw -o - /tmp/profile.plist

mkdir -p "$HOME/Library/MobileDevice/Provisioning Profiles"
cp ~/Downloads/2048_Crash.mobileprovision \
  "$HOME/Library/MobileDevice/Provisioning Profiles/<UUID>.mobileprovision"
```

2048 Crash에서 확인한 값:

- Name: `2048 Crash`
- UUID: `ed014dd0-5e3f-4ab8-87ae-35bbd2039d8b`
- App identifier: `ZRA4DHHKQ4.com.mannlab.games.game2048crash`
- Installed path: `~/Library/MobileDevice/Provisioning Profiles/ed014dd0-5e3f-4ab8-87ae-35bbd2039d8b.mobileprovision`

설치 후 Downloads의 원본 `.mobileprovision`은 삭제해도 된다.

### Distribution Profile vs Development Profile

`get-task-allow = 0`이면 App Store/TestFlight 배포용 프로필이다.

이 프로필로 할 수 있는 것:

- `Any iOS Device (arm64)` 선택
- `Product > Archive`
- App Store Connect/TestFlight 업로드

이 프로필로 할 수 없는 것:

- Xcode에서 실제 iPhone을 선택하고 바로 `Run`

실기기 `Run`을 하려면 별도 Development profile이 필요하다.

1. 테스트 iPhone의 UDID를 Apple Developer에 등록한다.
2. iOS App Development certificate/profile을 만든다.
3. Xcode 자동 서명을 쓰거나 개발용 profile을 직접 선택한다.

`Communication with Apple failed: Your team has no devices...` 또는 `No profiles for ... were found`가 뜰 때는 먼저 지금 하려는 작업이 `Run`인지 `Archive`인지 확인한다. App Store/TestFlight만 목표라면 기기 등록 없이 Archive로 진행하면 된다.

### 2048 Crash Release Signing

2048 Crash는 App Store profile name을 `2048 Crash`로 둔다.

Unity iOS export script가 생성한 Xcode project의 `Unity-iPhone` 타깃은 아래처럼 설정되어야 한다.

- `CODE_SIGN_STYLE = Manual`
- `CODE_SIGN_IDENTITY = Apple Distribution`
- `PROVISIONING_PROFILE_SPECIFIER = "2048 Crash"`
- `DEVELOPMENT_TEAM = ZRA4DHHKQ4`
- `PRODUCT_BUNDLE_IDENTIFIER = com.mannlab.games.game2048crash`

다른 팀이나 프로필 이름을 쓰면 환경 변수로 override한다.

```sh
MANNLAB_APPLE_TEAM_ID=<TEAM_ID> \
MANNLAB_2048_CRASH_IOS_PROFILE_SPECIFIER="<PROFILE_NAME>" \
./scripts/verify-2048-crash-ios-readiness.sh
```

## Xcode Warnings

아래 경고들은 2048 Crash archive 준비 중 확인한 경고이며, Archive가 성공한다면 보통 치명적이지 않다.

- `umbrella header for module 'UnityFramework' does not include header ...`
- `Run script build phase 'Crashlytics Run Script' will be run during every build ...`
- Unity generated code의 `Unused variable`
- Unity/iOS generated code의 `deprecated` API 경고

빨간 signing error, linker error, missing plist, missing framework, archive failure가 아니면 우선 Archive 결과를 기준으로 판단한다.

Crashlytics run script 경고는 output file 목록이 없어서 매번 실행된다는 뜻이다. 빌드 실패 원인은 아니며, 나중에 빌드 시간을 줄이고 싶을 때 정리한다.

## App Store Screenshots

App Store 스크린샷은 실제 플레이 화면과 달라 보이는 정적 목업을 쓰지 않는다. 2048 Crash는 WebGL 빌드를 실제로 띄운 뒤 Chrome DevTools protocol로 캡처한다.

권장 명령:

```sh
node scripts/capture-2048-crash-webgl-app-store-assets.mjs
```

호환 명령:

```sh
node scripts/generate-2048-crash-app-store-assets.mjs
```

`generate-2048-crash-app-store-assets.mjs`는 현재 WebGL 캡처 스크립트로 위임한다.

출력 위치:

- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPhone-6.9`
- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPhone-6.5`
- `prototypes/2048-crash/Assets/_Project/Art/AppStore/Upload/iPad-13`

기대 파일:

- `01-start-board.png`
- `02-after-first-slides.png`
- `03-building-the-crash.png`
- `04-late-board.png`

검증:

```sh
./scripts/verify-2048-crash-app-store-readiness.sh
```

Headless Chrome의 단순 `--screenshot`은 WebGL이 비활성화되어 `Your browser does not support WebGL` 화면을 찍을 수 있다. 이 경우 결과물을 쓰지 말고 DevTools protocol 기반 캡처 스크립트를 사용한다.

## Firebase And Crashlytics

Crashlytics는 Google Play 또는 App Store에 먼저 배포되어야만 켜지는 기능은 아니다. Firebase project와 앱 설정, SDK import, 플랫폼 config 파일이 필요하다.

2048 Crash iOS:

- Firebase project: `crash-6508f`
- Bundle ID: `com.mannlab.games.game2048crash`
- Config file: `prototypes/2048-crash/Assets/GoogleService-Info.plist`
- SDK: Firebase Unity SDK Analytics/Crashlytics

Crashlytics 테스트 크래시는 release build가 아니라 development build에서만 숨은 트리거로 실행한다. 2048 Crash에서는 development build에서 좌상단을 2.5초 안에 7번 탭하면 테스트 크래시가 발생한다. 크래시 후 앱을 다시 열어야 보고서가 업로드된다.

## GitHub Pages Deploy

`Branch "feat/..." is not allowed to deploy to github-pages due to environment protection rules`는 코드 빌드 실패가 아니라 GitHub environment 보호 규칙이다.

해결 방향:

- 배포 허용 브랜치에서 workflow를 실행한다.
- 또는 GitHub repository settings에서 `github-pages` environment protection rule을 수정한다.
- feature branch에서 WebGL 산출물 검증이 통과했다면, 이 메시지만으로 게임 빌드가 깨졌다고 보지 않는다.

## WebGL Runtime

`wasm streaming compile failed` 후 `falling back to ArrayBuffer instantiation`은 서버 MIME/압축/네트워크 상태에 따라 발생할 수 있다. fallback 후 게임이 실행되면 치명적인 실패는 아니다.

`Failed to load resource: net::ERR_HTTP2_PING_FAILED`는 브라우저와 서버 사이 연결이 끊긴 네트워크 오류일 수 있다. 재현이 계속되면 배포 서버의 캐시, 압축 파일, MIME, CDN 연결을 확인한다.
