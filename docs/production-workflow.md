# Mann Lab Games 제작 워크플로우

만랩 하이퍼 캐주얼 게임의 기본 작업 순서다.

목표는 모든 게임을 같은 흐름으로 빠르게 만들고, 측정하고, 배포하고, 개선하는 것이다.

```txt
아이디어 -> 기획 -> 프로토타입 구현 -> 분석 도구 -> 수익화/광고 -> 배포 -> 운영
```

## 1. 아이디어

가장 작은 플레이 약속을 적는다.

필수 산출물:

- 한 줄 설명
- 핵심 루프
- 타겟 플랫폼
- 기록 또는 성공 기준
- 다시 플레이하게 될 이유

이 단계에서는 광고, 전체 아트, 라이브 운영까지 해결하지 않는다.

## 2. 기획

아이디어를 구현 가능한 MVP로 바꾼다.

필수 산출물:

- `docs/` 안의 게임 기획서
- MVP 범위
- MVP에서 제외할 범위
- 난이도 곡선
- 입력 규칙
- 실패 규칙
- 결과 화면 규칙
- 비주얼 방향

만랩 공통 손그림 스타일을 쓰는 게임은 `docs/visual-direction.md`를 참조한다.

## 3. 프로토타입 구현

Unity 프로젝트를 `prototypes/` 아래에 만든다.

```sh
./scripts/new-unity-game.sh prototypes <game-slug>
```

필수 산출물:

- 플레이 가능한 핵심 루프
- 로컬 최고 기록
- 다시하기 흐름
- 기본 피드백과 사운드
- 실제 Android 기기 스모크 테스트

프로토타입 코드는 빠르게 가도 된다. 다만 지우거나, 옮기거나, `releases/`로 승격하기 쉬운 상태를 유지한다.

## 4. 분석 도구

핵심 루프가 플레이 가능해진 뒤 분석 도구를 붙인다.

기본 후보:

- Firebase Analytics / GA4
- Firebase Crashlytics
- 유료 유입을 시작할 경우 attribution SDK 후보

Unity Android 프로토타입에서 Firebase를 붙이는 최소 순서:

1. Firebase Console에서 프로젝트를 만든다.
2. Unity 앱을 추가하고 Android package ID를 등록한다.
   - `10000`: `com.mannlab.games.game10000`
   - `2048 Crash`: `com.mannlab.games.game2048crash`
3. `google-services.json`을 내려받아 Unity 프로젝트의 `Assets/` 아래에 넣는다.
4. Firebase Unity SDK에서 아래 패키지를 import한다.
   - `FirebaseAnalytics.unitypackage`
   - `FirebaseCrashlytics.unitypackage`
5. Unity Editor에서 Android Resolver를 실행해 Android 의존성을 반영한다.
6. Development Build가 아닌 릴리즈 APK/AAB로 실기기 실행을 확인한다.

최소 이벤트 후보:

- `run_start`
- `stage_clear`
- `wrong_tap`
- `run_end`
- `restart`
- `app_open`

`10000` MVP 현재 구현 이벤트:

- `app_open`: 앱 첫 실행 시
- `run_start`: 새 런 시작 시
- `wrong_tap`: 오답 탭 시
- `stage_clear`: 정답으로 스테이지를 넘길 때
- `run_end`: 전체 제한 시간이 끝났을 때

`2048 Crash` MVP 현재 구현 이벤트:

- `app_open`: 앱 첫 실행 시
- `run_start`: 새 런 시작 시
- `special_crash`: 특수 블록을 깨뜨릴 때
- `run_end`: 움직일 수 없어 런이 끝날 때
- `restart`: 결과 화면에서 다시 시작할 때

현재 구현은 Firebase SDK가 없어도 컴파일되며 Unity 로그에 이벤트를 남긴다. SDK와 `google-services.json`이 추가되면 같은 호출이 Firebase Analytics/Crashlytics로 전달된다.

각 이벤트마다 적어야 할 것:

- 이벤트 이름
- 발생 조건
- 파라미터
- 이 이벤트로 알고 싶은 것

분석은 다음 질문에 답해야 한다.

- 플레이어가 게임을 이해하는가?
- 어디에서 실패하는가?
- 한 런은 얼마나 오래 가는가?
- 한 판이 다음 판으로 이어지는가?
- 어느 스테이지 구간에서 이탈이 생기는가?

## 5. 수익화와 광고

최소 1개의 플레이 가능한 프로토타입과 기본 지표가 생긴 뒤 수익화 도구를 붙인다.

기본 후보:

- Google AdMob: 인앱 광고
- Google Ads: 유저 획득 캠페인
- Rewarded ad: 게임 루프에 맞을 때만 부활/보상용으로 사용
- Interstitial ad: 플레이 중이 아니라 런 종료 뒤 후보로 검토

광고를 붙이기 전 확인할 것:

- 개인정보처리방침 필요 여부
- 동의 플로우 필요 여부
- Google Play 데이터 보안 답변
- 출시 전까지 테스트 광고 사용

게임이 다시 하고 싶은 상태가 되기 전에는 광고를 붙이지 않는다.

## 6. 배포 준비

게임을 `prototypes/`에서 `releases/`로 옮기는 기준:

- 핵심 루프가 반복 플레이할 만큼 살아 있다.
- Android 실기기 빌드가 된다.
- 분석 이벤트가 정의되어 있다.
- 패키지명이 안정화되어 있다.
- 비주얼 방향이 일관된다.
- 스토어 등록에 필요한 기본 항목을 알고 있다.

필수 산출물:

- 릴리즈 체크리스트
- 빌드 노트
- 패키지명
- 버전 관리 계획
- 앱 서명 키 계획
- 개인정보처리방침 상태
- 데이터 보안 초안

스토어 업로드 전에는 항상 대상 플랫폼 기준 문서를 확인한다.

- Android: `docs/android-release-baseline.md`
- iOS: `docs/ios-release-baseline.md`
- 공통 트러블슈팅: `docs/games-troubleshooting.md`

### 오늘 배포용 최소 체크리스트

오늘처럼 광고/분석 SDK 없이 먼저 내부 테스트 배포만 목표로 할 때는 아래 순서로 진행한다.

1. 게임 루프 잠금
   - 오늘 포함할 룰만 확정한다.
   - 광고, 분석, 온라인 랭킹, 스킨은 다음 릴리즈 후보로 남긴다.
2. 로컬 검증
   - `./scripts/verify-10000-mvp.sh`
   - `./scripts/verify-10000-unity.sh`
3. Android App Bundle 생성
   - 산출물: `prototypes/10000/Builds/Android/10000.aab`
4. 버전 확인
   - 앱 버전과 Android bundle version code를 확인한다.
   - 같은 트랙에 다시 올릴 때는 version code를 반드시 증가시킨다.
5. 앱 서명 준비
   - Google Play App Signing 사용을 기본으로 한다.
   - Unity/keystore 업로드 키 전략은 첫 Play Console 업로드 흐름에서 확정한다.
6. Play Console 앱 생성
   - 앱 이름, 기본 언어, 앱/게임 여부, 무료/유료 여부를 입력한다.
   - 패키지명은 `com.mannlab.games.game10000`을 유지한다.
7. 스토어 기본 정보 입력
   - 앱 이름
   - 짧은 설명
   - 전체 설명
   - 앱 아이콘
   - 스크린샷
   - 카테고리
   - 연락처
8. 앱 콘텐츠 설문
   - 데이터 보안
   - 콘텐츠 등급
   - 타겟 연령
   - 광고 포함 여부는 오늘 빌드에서는 `아니오`로 답한다.
   - 개인정보처리방침 URL 필요 여부를 확인한다.
9. 내부 테스트 트랙 업로드
   - Internal testing 트랙에 `.aab`를 올린다.
   - 테스터 목록을 만든다.
   - 릴리즈 노트를 짧게 적는다.
10. 실기기 스모크 테스트
    - 설치 가능 여부
    - 첫 실행
    - 터치 입력
    - 60초 런 종료
    - 다시하기
    - 화면 비율/가독성

참고:

- Google Play internal testing: https://support.google.com/googleplay/android-developer/answer/9845334
- Google Play target API requirements: https://developer.android.com/google/play/requirements/target-sdk
- Google Play Data safety: https://support.google.com/googleplay/android-developer/answer/10787469
- Play App Signing: https://support.google.com/googleplay/android-developer/answer/9842756

## 7. 배포 주기

프로토타입 단계:

- 핵심 루프가 의미 있게 바뀔 때마다 빌드한다.
- 실제 기기에서 자주 테스트한다.
- 터치감과 가독성 문제를 늦게 발견하지 않는다.

릴리즈 후보 단계:

- 빌드마다 짧은 변경 기록을 남긴다.
- 게임플레이 또는 SDK 변경이 있으면 내부 테스트 빌드를 올린다.
- version code는 항상 증가시킨다.
- 무엇을 바꿨고 무엇을 관찰할지 기록한다.

권장 리듬:

- 프로토타입: 필요할 때 로컬/기기 빌드
- 릴리즈 후보: 의미 있는 게임플레이 또는 SDK 변경마다 내부 테스트 빌드
- 라이브: 하나의 측정 목표를 가진 작은 업데이트

## 8. 운영

출시 후에는 분석 지표와 스토어 피드백으로 다음 작업을 정한다.

운영 질문:

- 첫 플레이에서 게임을 이해하는가?
- 중간 도달 스테이지는 어디인가?
- 다시하기 비율은 어떤가?
- 광고가 재플레이를 해치는가?
- 어떤 기기나 OS에서 크래시가 나는가?

업데이트마다 하나의 주요 목적을 둔다.

- 온보딩 개선
- 리텐션 개선
- 수익화 개선
- 안정성 개선
- 난이도 곡선 테스트

## 작업 로그 규칙

각 게임은 가벼운 작업 로그를 유지한다.

위치는 `docs/` 또는 게임 프로젝트 내부 중 하나로 둔다.

기본 형식:

```txt
YYYY-MM-DD
- Decision:
- Changed:
- Verified:
- Next:
```

작업 로그는 모든 커밋을 반복해서 적는 곳이 아니다. 왜 방향이 정해졌고, 무엇이 검증됐고, 다음 판단이 무엇인지 남기는 곳이다.
