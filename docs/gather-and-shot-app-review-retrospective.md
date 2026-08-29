# Gather & Shot App Review Retrospective

작성일: 2026-08-29

## 요약

Gather & Shot은 처음에는 눈덩이/눈무더기 픽업을 주워 탄약을 모으고, 근처 적에게 자동으로 눈덩이를 던지는 간단한 모바일 생존 슈터로 시작했다. App Store 심사에서 Guideline 4.3(a) Design - Spam으로 거절된 뒤, 핵심 게임성을 "필드 픽업 수집"에서 "멈춰서 눈을 모으는 리스크 장전"으로 크게 변경했다.

최종 재제출 빌드는 iOS `0.1 (2)`이며, 새 심사 노트와 gameplay review video를 포함해서 제출했다.

## 최초 MVP 방향

- 모바일 세로 화면 기준.
- 가상 터치 조이스틱으로 플레이어 이동.
- 필드의 눈덩이/눈무더기를 주워 탄약을 모음.
- 탄약이 있고 적이 사거리 안에 있으면 가장 가까운 적에게 자동 투척.
- 적 처치 수가 점수이며 최고 기록 저장.
- 적 접촉 시 Warmth가 감소하고 넉백.
- Warmth가 0이 되면 게임 오버, Again 버튼으로 재시작.
- MVP 적은 Walker, Runner, Heavy.
- 시간 경과에 따라 스폰 간격 감소, 동시 적 수 증가, 적 속도 증가.
- Standing을 참고한 손그림/스케치풍 비주얼.

## 구현 및 출시 준비 작업

- Unity 프로젝트를 `prototypes/gather-and-shot` 아래에 생성했다.
- `docs/gather-and-shot-game-design.md`에 현재 기획과 규칙을 정리했다.
- `scripts/generate-gather-and-shot-doodle-assets.py`로 Standing풍 procedural PNG 에셋을 재현 가능하게 만들었다.
- `GatherAndShotController.cs` 중심으로 이동, 탄약, 자동 투척, 적 추적, 점수, 최고 기록, Warmth, 게임 오버, 재시작을 구현했다.
- WebGL 빌드 및 `games.mannlab.app/games/gather-and-shot/` 배포 흐름을 만들었다.
- 화면비 대응을 정사각형 플레이 구역 + 남는 영역 검정 letterbox 방식으로 바꿨다.
- 터치 시작 위치 기준의 floating virtual control로 되돌리고, 조작 후 페이드아웃하도록 다듬었다.
- UI가 상단에서 겹치던 문제를 재배치했다.
- 큰 눈무더기 보너스 아이템을 추가했다.
- 눈덩이 드랍율을 낮추고 수집/장전에 리스크를 주는 방향을 논의했다.
- Firebase Analytics/Crashlytics 설정을 추가했다.
- AdMob game-over interstitial 설정을 추가했다.
- `GoogleService-Info.plist`, provisioning profile, app icon, App Store screenshots, App Review metadata를 준비했다.
- iOS export 스크립트와 readiness 검증 스크립트를 추가했다.
- Xcode archive는 `Unity-iPhone.xcworkspace`에서 진행해야 한다는 점을 문서화했다.

## App Store 최초 거절

심사 결과:

- Guideline 4.3(a) - Design - Spam

Apple의 핵심 피드백은 앱이 다른 개발자가 제출한 앱과 유사한 binary, metadata, concept을 공유하며 차이가 작아 보인다는 것이었다. 또한 유사하거나 재포장된 앱 제출은 App Store 검색과 발견성을 해치는 spam으로 간주될 수 있다고 안내했다.

이 피드백은 단순한 문구 문제가 아니라, 템플릿 기반의 반복 앱처럼 보일 수 있는 구조 자체를 지적한 것으로 판단했다. 특히 기존 버전은 "움직이며 아이템을 줍고 자동 공격한다"는 기본 loop가 너무 일반적인 survival shooter 템플릿으로 읽힐 수 있었다.

## 대응 기준

단순히 아이콘, 설명, 스크린샷만 바꾸는 방식은 Guideline 4.3(a)에 충분하지 않다고 판단했다. 대응 기준을 다음처럼 잡았다.

- 핵심 조작 감각이 달라져야 한다.
- 탄약 획득 방식이 다른 게임과 명확히 구분되어야 한다.
- 플레이어가 반복해서 내리는 의사결정이 바뀌어야 한다.
- 심사자가 10-20초 gameplay video만 봐도 변경점을 이해할 수 있어야 한다.
- metadata, screenshots, review notes가 새 게임성을 같은 방향으로 설명해야 한다.

## 대규모 게임성 변경

최종 방향은 "stop-to-gather survival mechanic"이다.

변경 전:

- 필드에 떨어진 눈덩이/눈무더기를 줍는다.
- 주운 탄약을 가까운 적에게 자동 투척한다.
- 플레이어는 계속 움직이며 픽업과 회피를 동시에 한다.

변경 후:

- 플레이어는 이동 중 적을 피한다.
- 터치를 떼고 가만히 서 있으면 눈을 모아 탄약을 얻는다.
- 눈을 모으는 동안 이동과 자동 공격이 제한된다.
- 다시 터치하면 gathering이 즉시 취소되고 이동으로 복귀한다.
- 필드 눈덩이/눈무더기/큰 눈무더기는 주 수급원이 아니라 희귀 emergency bonus refill로 바뀌었다.

이 변경으로 핵심 판단은 "계속 움직이며 살아남을 것인가, 위험을 감수하고 멈춰서 장전할 것인가"가 되었다.

## 구현된 주요 변경점

- `GatherAndShotRules.cs`
  - `StationaryGatherDelaySeconds`와 `StationaryGatherCycleSeconds` 추가.
  - `MaxAmmo`를 10으로 조정.
  - 일반 pickup spawn 빈도와 최대 live pickup 수를 줄임.
  - Snowball, Snowdrift, Big Snowdrift를 보너스 refill로 조정.

- `GatherAndShotController.cs`
  - 터치 시작 시 gathering 취소.
  - 터치 종료 후 잠깐의 delay 뒤, 플레이어가 정지 상태면 gathering 시작.
  - gathering 중 movement와 auto-fire 제한.
  - gathering progress UI와 상태 표시 추가.
  - rare bonus pickup 수집 이벤트와 telemetry 반영.

- 문서 및 검증
  - `docs/gather-and-shot-game-design.md`를 stop-to-reload 규칙으로 갱신.
  - `prototypes/gather-and-shot/README.md`에 iOS/WebGL/Firebase/AdMob 관련 실행 흐름 정리.
  - `scripts/verify-gather-and-shot-mvp.sh`를 새 규칙 기준으로 업데이트.
  - Unity iOS release export 검증을 `scripts/verify-gather-and-shot-ios-readiness.sh release`로 수행.

## Firebase/Crashlytics 연결 경험

Gather & Shot에서 반복 가능한 Firebase/Crashlytics 연결 절차를 한 번 정리했다. 다음 Unity 게임에도 거의 같은 순서로 적용할 수 있다.

구현 범위:

- shared Unity package `com.mannlab.firebase-unity-sdk`를 Unity package manifest에 연결했다.
- Firebase Analytics와 Crashlytics를 감싸는 `FirebaseTelemetry.cs` bridge를 프로젝트 스크립트에 추가했다.
- 게임 시작 시 `FirebaseTelemetry.SetContext("game", "gather-and-shot")`로 앱/게임 식별 context를 남겼다.
- runtime event로 `app_open`, `run_start`, `restart`, `gather_start`, `bonus_pickup`, `run_end`를 기록했다.
- Crashlytics custom key로 score, best score, ammo, Warmth, elapsed seconds, enemy count, pickup count, game-over state, gathering state를 기록했다.
- development build에서만 동작하는 forced crash path를 넣었다.

필요한 설정 파일:

- iOS: `Assets/GoogleService-Info.plist`
- Android: `Assets/google-services.json`
- Desktop/editor fallback: `Assets/StreamingAssets/google-services-desktop.json`

주의할 점:

- iOS plist의 `BUNDLE_ID`가 Unity bundle identifier와 반드시 같아야 한다.
- Gather & Shot 기준 bundle identifier는 `com.mannlab.games.gatherandshot`이다.
- Firebase config 파일은 코드로 대체할 수 있는 값이 아니라 Firebase Console에서 앱별로 내려받아야 하는 빌드 입력이다.
- Android production을 열기 전에는 Android용 `google-services.json`을 별도로 받아야 한다.

테스트/검증:

- readiness script: `./scripts/verify-gather-and-shot-firebase-readiness.sh`
- iOS release readiness에서도 `GoogleService-Info.plist` 존재와 bundle ID 일치를 검사한다.
- Crashlytics forced test crash는 development build에서 좌상단을 2.5초 안에 7번 탭하면 발생한다.
- CLI/자동 확인용으로는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 사용한다.
- forced crash trigger는 Unity Editor 또는 development build에서만 컴파일되어야 한다. App Store release build에 노출되면 안 된다.

다음 앱에서 반복할 순서:

1. Firebase Console에서 iOS app을 만들고 bundle ID를 확정한다.
2. `GoogleService-Info.plist`를 Unity project의 `Assets/` 아래에 넣는다.
3. Firebase Unity package와 telemetry bridge를 연결한다.
4. app/game context, run lifecycle event, crash custom key를 게임 controller에 붙인다.
5. development build 전용 forced crash trigger를 넣는다.
6. Firebase readiness script를 게임별 이름으로 복제하고 bundle ID, event name, config path를 고친다.
7. 실제 기기/TestFlight에서 forced crash가 Firebase Console에 들어오는지 확인한다.

## AdMob 연결 경험

AdMob도 다음 게임에서 반복될 가능성이 높다. Gather & Shot에서는 "game over interstitial"만 붙였고, 보상형 광고나 배너는 넣지 않았다.

구현 범위:

- shared Unity package `com.mannlab.admob-core`를 만들고 프로젝트 manifest에 연결했다.
- Google Mobile Ads Unity plugin은 OpenUPM registry를 통해 연결했다.
- `GatherAndShotGame.asmdef`에서 `MannLab.Ads.Core`를 참조했다.
- `MannLabAdMob.InitializeGameOverInterstitial`로 game-over interstitial을 초기화했다.
- `MannLabAdMob.TryShowGameOverInterstitial`로 게임 오버 후 광고 표시를 시도했다.
- 광고는 매 게임 오버마다가 아니라 `GameOverInterstitialInterval = 3` 기준으로 간격을 뒀다.
- `link.xml`에 `GoogleMobileAds.iOS`, `GoogleMobileAds.Android`, UMP 관련 namespace preserve를 추가했다.

사용한 Gather & Shot iOS production 값:

- AdMob iOS App ID: `ca-app-pub-4525914685149405~6036634116`
- Game-over interstitial ad unit: `ca-app-pub-4525914685149405/2541126713`

테스트 광고 정책:

- development/debug build는 Google sample test ad unit을 사용해야 한다.
- `MANNLAB_ADMOB_FORCE_TEST_ADS` define을 주면 AdMob test build로 강제한다.
- `BuildIosXcode.BuildAdMobTest`는 Google sample iOS App ID를 사용한다.
- release build는 production iOS App ID와 production interstitial ad unit을 사용한다.
- Android production ID는 아직 deferred로 두고, Android release 전 별도 AdMob app/ad unit을 만들어야 한다.

필수 App Store/정책 작업:

- App Privacy에 AdMob 사용 사실과 data collection 항목을 맞춰야 한다.
- privacy policy/support page에 Gather & Shot이 Google AdMob을 사용할 수 있음을 노출했다.
- 광고가 age rating과 충돌하지 않는지 확인해야 한다.
- ATT/IDFA가 필요한 방식으로 추적을 켜는 경우 App Tracking Transparency 문구와 권한 흐름이 필요하다.
- UMP/consent 흐름은 Google Mobile Ads/UMP SDK 연동 상태를 기준으로 별도 확인해야 한다.

iOS 빌드 주의점:

- AdMob/CocoaPods가 붙은 iOS 빌드는 `Unity-iPhone.xcodeproj`가 아니라 `Unity-iPhone.xcworkspace`를 열어 archive해야 한다.
- Unity export 후 `Podfile`, `Podfile.lock`, `Pods`, `Pods.xcodeproj`, `Unity-iPhone.xcworkspace`가 있어야 한다.
- Gather & Shot Podfile은 `UnityFramework` target에 `Google-Mobile-Ads-SDK`와 `GoogleUserMessagingPlatform`을 연결한다.
- `Info.plist`의 `GADApplicationIdentifier`가 release/test mode에 맞는지 readiness script에서 확인해야 한다.

테스트/검증:

- readiness script: `./scripts/verify-gather-and-shot-admob-readiness.sh`
- iOS release readiness: `./scripts/verify-gather-and-shot-ios-readiness.sh release`
- AdMob test export: `./scripts/verify-gather-and-shot-ios-readiness.sh admob-test`
- release export에서 expected `GADApplicationIdentifier`는 production App ID여야 한다.
- test export에서 expected `GADApplicationIdentifier`는 Google sample App ID여야 한다.

다음 앱에서 반복할 순서:

1. AdMob에서 새 app을 만들고 iOS App ID를 받는다.
2. 광고 단위는 우선 `Game Over Interstitial`처럼 노출 맥락이 명확한 이름으로 만든다.
3. production ad unit ID와 test ad path를 분리한다.
4. shared `com.mannlab.admob-core` package를 manifest에 연결한다.
5. asmdef에 `MannLab.Ads.Core` 참조를 추가한다.
6. game-over flow에 initialize/show call을 붙이고 표시 간격을 둔다.
7. `GoogleMobileAdsSettings.asset`, `link.xml`, iOS build script의 App ID를 업데이트한다.
8. privacy/support text와 App Store App Privacy를 갱신한다.
9. readiness script를 게임별로 복제해서 production/test App ID와 ad unit ID를 검증한다.
10. Xcode archive는 반드시 workspace에서 진행한다.

## WebGL 배포 중 배운 점

`games.mannlab.app` 배포 후 다음 WebGL 경고가 발생했다.

- WebAssembly streaming compilation failed.
- 메시지는 `Content-Encoding` HTTP header와 pre-compressed file 불일치를 예로 들었다.

확인 결과 `.wasm` 파일 자체는 raw wasm으로 정상이며 `WebAssembly.compile`도 성공했다. 다만 GitHub Pages/Fastly가 브라우저 요청에 따라 raw `.wasm`을 동적으로 gzip 응답으로 내려줄 수 있고, Unity/Emscripten의 `instantiateStreaming` 경로가 먼저 실패한 뒤 ArrayBuffer fallback으로 이어지는 구조였다. 실제 게임 파일 문제라기보다 startup warning UX 문제에 가까웠다.

대응:

- `scripts/sync-gather-and-shot-webgl-to-site.sh`에서 public WebGL 빌드의 `instantiateStreaming` 분기를 끄고 ArrayBuffer instantiate를 사용하도록 후처리했다.
- 새 asset version `bdf1ebe15f08`로 GitHub Pages와 Sites 배포를 갱신했다.
- `npm run build`, public wasm compile, live framework patch 반영을 확인했다.

## 재제출 준비

- iOS release Xcode export를 다시 생성했다.
- 새 build number는 `0.1 (2)`로 올렸다.
- 검증된 workspace:
  - `prototypes/gather-and-shot/Builds/iOS/Xcode/Unity-iPhone.xcworkspace`
- Xcode 확인:
  - Xcode `26.5`
  - Unity `6000.3.23f1`
  - bundle ID `com.mannlab.games.gatherandshot`
  - Team ID `ZRA4DHHKQ4`
  - provisioning profile specifier `Gather And Shot`
  - production AdMob app ID 적용
  - App Store 1024 icon 포함
  - `Unity-iPhone` scheme 확인

## 재제출 심사 노트

재제출 심사 노트는 다음 내용을 중심으로 작성했다.

- 이전 Guideline 4.3(a) 피드백에 대응해서 core gameplay loop와 interaction model을 크게 변경했다.
- 게임은 stop-to-gather survival mechanic 중심으로 바뀌었다.
- 플레이어는 움직여서 적을 피하지만, snow ammunition은 터치를 떼고 정지해야 주로 회복된다.
- gathering 중에는 이동과 공격이 제한되어 risk-reward decision이 생긴다.
- Snow piles는 main collection loop가 아니라 rare emergency bonus resource로 바뀌었다.
- iOS build, screenshots, metadata, review video를 새 mechanic 기준으로 업데이트했다.

Gameplay review video:

https://drive.google.com/file/d/1V0mgkY1fo8jMQ1nA9n33MXNHLyBulmMG/view?usp=sharing

## App Review 운영 메모

- Guideline 4.3(a)는 metadata만의 문제가 아닐 가능성이 높다.
- 실제 gameplay loop와 binary를 바꿨다면 같은 build 재제출보다 새 build로 다시 제출하는 편이 낫다.
- 기존 App Review 대화에는 "new build submitted with significant gameplay changes" 정도로 짧게 회신할 수 있다.
- 메타데이터의 표현도 새 loop와 맞춰야 한다.
  - 피해야 할 표현: collect snow pickups as the main loop.
  - 강조할 표현: release touch and stand still to gather snow, choose when to risk reloading.
- 리뷰 영상에는 반드시 새 차별점이 보여야 한다.
  - 이동하며 회피.
  - 멈춰서 눈 모으기.
  - 모으는 동안 리스크.
  - 탄약 회복 후 자동 투척.
  - 점수 상승.

## 다음 게임에 남기는 교훈

- App Store에 낼 작은 아케이드 게임도 초기부터 "한 문장으로 설명되는 고유 조작/판단"이 있어야 한다.
- 같은 Unity 템플릿, 같은 visual pipeline, 같은 App Store metadata 구조를 반복하면 4.3(a) 리스크가 커진다.
- 아이콘/스크린샷/색감 차이보다 gameplay decision 차이가 중요하다.
- 심사 노트는 변명보다 변경 내역, 플레이 방식, 증거 영상 링크 중심으로 쓰는 편이 낫다.
- WebGL smoke test는 Unity 로딩 성공뿐 아니라 CDN에서 실제로 내려오는 `.wasm`, `.framework.js`, `assetVersion`까지 확인해야 한다.
- iOS 재제출 전에는 build number를 반드시 올리고, workspace 기준 archive 여부를 확인해야 한다.

## 관련 커밋

- `a113090` Add Firebase and AdMob hooks to Gather & Shot
- `4189534` Use touch-origin movement in Gather and Shot
- `d84a767` Add Gather and Shot iOS export readiness
- `5fda5e8` Set Gather and Shot iOS AdMob IDs
- `eda8759` Match Gather and Shot provisioning profile name
- `bd7f741` Add Gather and Shot App Store screenshots
- `394b011` Make Gather and Shot stop-to-reload
- `f806e79` Disable WebGL streaming instantiate for Gather and Shot
- `0776688` Bump Gather and Shot iOS build number
