# 2048 Crash 게임 기획

## 한 줄 설명

`2048`식 슬라이드 퍼즐에 움직이지 않는 특수 블록을 추가하고, 같은 숫자의 일반 블록을 밀어 넣어 깨는 스테이지형 숫자 퍼즐.

## 핵심 재미

플레이어가 느껴야 하는 감정은 "만들었다", "밀어 넣었다", "깨졌다"이다.

일반 `2048`의 큰 숫자 만들기보다, 현재 특수 블록 숫자를 목표로 삼아 보드를 조작하는 압축된 퍼즐감이 핵심이다.

## 기본 루프

1. 4 x 4 보드가 열린다.
2. 일반 블록 2개와 특수 블록 1개가 생성된다.
3. 플레이어가 상하좌우로 보드를 민다.
4. 일반 블록은 같은 숫자끼리 합쳐진다.
5. 특수 블록은 움직이지 않고, 숫자가 다른 일반 블록은 막는다.
6. 같은 숫자의 일반 블록이 특수 블록에 충돌하면 특수 블록이 깨진다.
7. Score/Stage가 1 증가하고, 같은 보드 상태를 유지한 채 다음 특수 블록은 2배 값으로 빈 칸에 다시 생긴다.
8. 움직일 수 있는 수가 없으면 런이 끝난다.

## MVP 규칙

- 보드 크기: `4 x 4`
- 시작 특수 블록: `2`
- 특수 블록 진행: `2`, `4`, `8`, `16`, ...
- 일반 블록 스폰: 유효 이동 뒤 `2` 90%, `4` 10%
- 점수: 깨뜨린 특수 블록 수를 `Stage`로 표시
- 최고 기록: 로컬 `PlayerPrefs` 저장
- 조작: 키보드 화살표/WASD, 모바일 스와이프

## 충돌 판정

MVP에서는 같은 숫자의 일반 블록이 특수 블록과 충돌하면 특수 블록과 일반 블록이 둘 다 깨진다. 깨진 칸은 같은 턴 안에서 다른 블록이 바로 채우지 못하고, 다음 입력부터 빈 칸으로 작동한다.

숫자가 다른 일반 블록은 특수 블록을 통과하거나 밀어내지 못한다.

## 스테이지 연결

각 Stage는 별도 판이 아니라 하나의 런 안에서 이어진다. 특수 블록을 깨면 기존 일반 블록 배치와 합쳐진 숫자는 유지되고, 충돌한 일반 블록은 사라지며, 새 특수 블록만 빈 칸에 생성된다.

## 모션

- 일반 블록은 입력 방향으로 짧게 미끄러지듯 이동한다.
- 합쳐지는 블록은 도착점에서 살짝 커지며 값을 갱신한다.
- 특수 블록은 흰 종이 배경, 연한 파란 사선 해칭, 잉크색 숫자로 일반 블록과 구분한다.
- 특수 블록은 같은 숫자 블록과 충돌하면 충돌한 일반 블록과 함께 흔들리며 깨지고 사라진다.
- 다음 특수 블록과 새 일반 블록은 빈 칸에서 작게 시작해 커지며 등장한다.

## 디자인 적용

Mann Lab Games 공통 손그림 스케치 스타일을 따른다.

- 일반 타일은 값에 따라 종이, 주황, 붉은색, 파랑, 초록, 보라 계열로 변한다.
- 특수 블록은 Mann Lab Games 공통 `SketchHatchFillGraphic` 해칭 배경을 써서 보드 위 목표물처럼 보이게 한다.
- 상단에는 `Stage`, 현재 목표 `Crash N`, `Best`만 둔다.

## MVP 이후 작업 순서

1. 실제 플레이 난이도 확인
2. `2048` 명칭/상표 리스크 검토 후 공개 표시명 결정
3. Firebase Console 앱 등록, SDK import, 실기기 Crashlytics 확인
4. Android 내부 테스트 빌드 업로드
5. iOS TestFlight 업로드와 App Store 심사 준비

## Android 배포 준비

- 앱 아이콘: `prototypes/2048-crash/Assets/_Project/Art/AppStore/AppIcon-1024.png`
- Android release builder: `MannLab.Games.Game2048Crash.EditorTools.BuildAndroidAab`
- 기본 산출물: `prototypes/2048-crash/Builds/Android/2048-crash.aab`
- 실기기 확인용 APK: `prototypes/2048-crash/Builds/Android/2048-crash.apk`
- 로컬 서명 파일: `prototypes/2048-crash/Signing/` 아래에 두고 git에는 올리지 않는다.

## 초기 결정

- 게임명: `2048 Crash`
- Unity 프로젝트 slug: `2048-crash`
- Android package name: `com.mannlab.games.game2048crash`
- C# namespace: `MannLab.Games.Game2048Crash`
- 점수명: `Stage`

## Firebase 계측

코드는 Firebase SDK가 없어도 컴파일되는 `FirebaseTelemetry` 어댑터를 사용한다. Firebase Unity SDK와 플랫폼별 Firebase config 파일이 추가되면 같은 호출이 Firebase Analytics/Crashlytics로 전달된다.

iOS Firebase 앱 설정은 `Assets/GoogleService-Info.plist`에 둔다. 현재 번들 ID는 `com.mannlab.games.game2048crash`이다.

Firebase Unity SDK 13.14.0의 Analytics/Crashlytics 패키지를 사용한다.

Crashlytics 확인용 개발 빌드는 좌상단을 2.5초 안에 7번 탭하면 강제 테스트 크래시를 발생시킨다. 이 트리거는 Unity Editor 또는 development build에서만 컴파일된다.

이벤트/브레드크럼:

- `app_open`: 앱 첫 실행
- `run_start`: 새 런 시작
- `special_crash`: 특수 블록 파괴
- `run_end`: 게임 오버
- `restart`: 결과 화면에서 재시작

Crashlytics custom keys:

- `game`
- `stage`
- `target_value`
- `special_index`
- `best_stage`
- `game_over`

## App Store 출시 준비

- 출시 준비 문서: `docs/2048-crash-app-store-prep.md`
- 스크린샷 생성: `node scripts/generate-2048-crash-app-store-assets.mjs`
- 제출 준비 검증: `./scripts/verify-2048-crash-app-store-readiness.sh`
- iOS 릴리즈 Xcode 프로젝트 생성: `REQUIRE_APP_STORE_XCODE=1 ./scripts/verify-2048-crash-ios-readiness.sh`
- 개인정보 처리방침 URL: `https://games.mannlab.app/privacy`
