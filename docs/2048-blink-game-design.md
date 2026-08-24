# 2048 Blink 게임 기획

## 한 줄 설명

`2048`과 같은 슬라이드/합치기 규칙이지만, 매 턴마다 예측 가능한 Gray Cross가 지나가며 그 위의 숫자만 가려지는 기억 퍼즐.

## 핵심 재미

플레이어가 느껴야 하는 감정은 "봤다", "기억했다", "맞게 밀었다"이다.

일반 `2048`의 숫자 성장 재미를 유지하되, 절반의 보드가 매 턴 사라지면서 공간 기억과 리스크 판단이 추가된다.

## 기본 루프

1. 4 x 4 보드가 열린다.
2. 일반 `2048`처럼 숫자 타일 2개가 생성된다.
3. 한 행과 한 열이 Gray Cross로 표시된다.
4. 플레이어가 상하좌우로 보드를 민다.
5. 같은 숫자는 일반 `2048`처럼 합쳐진다.
6. 유효 이동 후 새 타일 1개가 생기고, Gray Cross가 다음 위치로 이동한다.
7. 움직일 수 있는 수가 없으면 런이 끝난다.

## MVP 규칙

- 보드 크기: `4 x 4`
- 타일 스폰: 유효 이동 뒤 `2` 90%, `4` 10%
- 가림 규칙: 매 턴 `GrayCrossPhase = Turn % 4`, `HiddenRow = GrayCrossPhase`, `HiddenColumn = (GrayCrossPhase + 2) % 4`에 해당하는 행/열을 가린다.
- Gray Cross 순환:
  - `Turn 0`: Row 1 + Column 3
  - `Turn 1`: Row 2 + Column 4
  - `Turn 2`: Row 3 + Column 1
  - `Turn 3`: Row 4 + Column 2
  - 이후 반복
- 가려진 빈 칸은 연한 회색 바닥으로 보이고, 가려진 타일은 숫자 없는 짙은 회색 타일로 표시한다.
- 가려진 칸은 숫자만 숨긴다. 타일 점유 여부는 항상 알 수 있다.
- 이동/합치기/게임 오버 판정은 가려짐과 무관하게 일반 `2048` 규칙을 따른다.
- 점수: 합쳐진 타일 값의 합
- 최고 기록: 로컬 최고 타일과 최고 점수 저장
- 조작: 키보드 화살표/WASD, 모바일 스와이프

## 디자인 적용

Mann Lab Games 공통 손그림 스케치 스타일을 따른다.

- 보이는 타일은 `2048 Crash`와 유사한 종이/색상 계열을 사용한다.
- 가려진 칸은 Gray Cross와 숫자 없는 회색 타일로 표시해 "정보가 흐려진다"는 느낌을 준다.
- 상단에는 `Score`, 현재 크로스 단계 `Cross 1/4`, `Best`만 둔다.
- 결과 화면에는 최고 타일과 점수를 보여준다.

## 초기 결정

- 게임명: `2048 Blink`
- Unity 프로젝트 slug: `2048-blink`
- Android/iOS package name: `com.mannlab.games.game2048blink`
- C# namespace: `MannLab.Games.Game2048Blink`
- 점수명: `Score`

## MVP 이후 작업 순서

1. 가려진 칸이 너무 억울하지 않은지 첫 플레이 난이도를 확인한다.
2. Gray Cross 전환 전 짧은 전체 공개 연출이 필요한지 테스트한다.
3. 스토어 아이콘과 첫 스크린샷에서 "절반이 깜박이는 2048"을 명확히 보여준다.
4. Firebase Analytics/Crashlytics를 붙인다.
5. iOS TestFlight 또는 WebGL로 빠르게 반응을 본다.

## Firebase 계측

코드는 Firebase SDK가 없어도 컴파일되는 `FirebaseTelemetry` 어댑터를 사용한다. Firebase Unity SDK와 iOS Firebase config 파일이 추가되면 같은 호출이 Firebase Analytics/Crashlytics로 전달된다.

iOS Firebase 앱 설정은 `Assets/GoogleService-Info.plist`에 둔다. 현재 번들 ID는 `com.mannlab.games.game2048blink`이다.

Firebase Unity SDK 13.14.0의 Analytics/Crashlytics 패키지를 포함한 공용 `com.mannlab.firebase-unity-sdk` 패키지를 사용한다.

Crashlytics 확인용 development build는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. CLI 검증 시에는 `--mannlab-force-crashlytics-test` launch argument 또는 `MANNLAB_FORCE_CRASHLYTICS_TEST=1` 환경변수를 주면 앱 시작 직후 테스트 크래시가 발생한다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일되며, App Store/TestFlight release build에는 포함되지 않는다.

Crashlytics custom keys:

- `game`
- `score`
- `highest_tile`
- `turn`
- `gray_cross`
- `best_tile`
- `best_score`
- `game_over`
- `crashlytics_test`

Analytics events:

- `app_open`
- `run_start`
- `restart`
- `run_end`
- `crashlytics_test_trigger`

## iOS 배포 준비

- 앱 아이콘: `prototypes/2048-blink/Assets/_Project/Art/AppStore/AppIcon-1024.png`
- 아이콘 생성: `node scripts/generate-2048-blink-app-icon.mjs`
- iOS 릴리즈 Xcode 프로젝트 생성: `./scripts/verify-2048-blink-ios-readiness.sh`
- iOS Crashlytics 테스트 Xcode 프로젝트 생성: `./scripts/verify-2048-blink-ios-readiness.sh crashlytics-test`
- Firebase 코드/설정 점검: `./scripts/verify-2048-blink-firebase-readiness.sh`
- 기본 산출물: `prototypes/2048-blink/Builds/iOS/Xcode`
