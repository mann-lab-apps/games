# Wind Gull 게임 기획

## 한 줄 설명

정해진 에너지를 가지고 날개짓과 활공을 번갈아 쓰며, 바람과 몸체 각도를 읽어 최대한 멀리 날아가는 비행 거리 게임.

## 핵심 재미

플레이어가 느껴야 하는 감정은 "지금 날개짓을 아낄까?", "저 바람을 타면 더 갈 수 있겠다", "각도를 조금만 더 낮추면 기록이 늘 것 같은데"이다.

이 게임은 정밀 비행 시뮬레이터가 아니다. 새의 비행 원리를 전부 재현하기보다, 에너지 관리와 바람 읽기를 손맛 있는 2버튼/2상태 게임으로 압축한다.

## 기본 루프

1. 새는 출발 고도와 정해진 에너지를 가지고 자동으로 앞으로 난다.
2. 플레이어는 매 순간 날개짓 또는 활공 중 하나를 선택한다.
3. 날개짓은 에너지를 쓰지만 고도와 속도, 자세 안정성을 회복한다.
4. 활공은 에너지를 쓰지 않지만 바람과 몸체 각도의 영향을 더 크게 받는다.
5. 고도가 0이 되거나 속도가 너무 낮아지면 런이 종료된다.
6. 최종 비행 거리가 Score가 되고, 최고 거리를 로컬에 저장한다.
7. 다시 시작하면 바람 배치가 일부 바뀌어 매 런의 판단이 달라진다.

## MVP 규칙

- 입력은 두 가지로 고정한다.
- 화면을 누르고 있으면 날개짓, 손을 떼면 활공으로 처리한다.
- 날개짓 중에는 에너지가 줄고, 새가 위쪽 힘과 약간의 전진 속도를 얻는다.
- 활공 중에는 에너지가 줄지 않고, 몸체 각도와 바람에 따라 속도/고도 변화가 커진다.
- 몸체 각도는 플레이어가 직접 슬라이더로 조절하지 않고, 상태 전환과 현재 속도에 따라 자동으로 변한다.
- Score는 출발점에서부터의 수평 거리다.
- Best Distance는 로컬 저장한다.

첫 버전은 "플랩 버튼을 정확히 누르는 게임"보다 "플랩을 참는 타이밍을 고르는 게임"이어야 한다.

## 비행 모델 초안

MVP 물리는 아래 변수만 화면 안에서 체감되게 만든다.

```txt
거리 = 앞으로 간 정도
고도 = 땅에 닿으면 종료
속도 = 너무 낮으면 떨어짐
에너지 = 날개짓 가능량
피치 = 몸체 각도
바람 = 구간별 상승/하강/순풍/역풍
```

추천 모델:

- 날개짓: 에너지 -1, 상승력 +, 속도 +, 피치가 살짝 위로 회복
- 활공: 에너지 변화 없음, 중력으로 고도 감소, 속도는 피치와 바람에 따라 변화
- 순풍: 전진 속도 증가
- 역풍: 전진 속도 감소, 활공 중 영향이 더 큼
- 상승기류: 고도 감소를 줄이거나 고도 상승
- 하강기류: 고도 감소 증가
- 피치가 너무 높음: 속도 손실, 실속 위험
- 피치가 너무 낮음: 속도 증가, 고도 손실 증가

핵심은 피치를 직접 조작하게 하지 않는 것이다. 사용자의 액션은 여전히 날개짓/활공 두 가지이고, 피치는 그 결과로 생기는 상태값으로 둔다.

## 몸체 각도 설계

몸체 각도는 "지금 새가 어떤 자세로 공기를 타고 있는지"를 보여주는 보조 변수다.

상태 변화:

- 날개짓 직후: 피치가 위로 올라가며 고도를 얻는다.
- 긴 활공: 피치가 점점 아래로 숙여지며 속도를 얻지만 고도를 잃는다.
- 상승기류 활공: 피치를 안정적으로 유지하며 에너지 없이 거리를 벌 수 있다.
- 역풍 활공: 피치가 흔들리고 속도 손실이 커진다.
- 에너지가 낮을 때 날개짓: 상승 효과가 약해지고 피치 회복도 작아진다.

실패 위험:

- 피치가 너무 높고 속도가 낮으면 짧은 실속 상태가 된다.
- 실속 중에는 조작이 둔해지고 고도가 빠르게 떨어진다.
- 실속은 즉시 실패가 아니라 "아, 방금 각도를 욕심냈다"는 피드백이어야 한다.

## 바람 설계

바람은 화면 오른쪽에서 미리 읽을 수 있어야 한다.

바람 타입:

- 순풍: 오른쪽 화살표, 전진 속도 증가
- 역풍: 왼쪽 화살표, 전진 속도 감소
- 상승기류: 위쪽 소용돌이, 활공 효율 증가
- 하강기류: 아래쪽 흔들림, 고도 손실 증가
- 난기류: 짧게 피치가 흔들림

MVP 추천:

- 첫 구현은 순풍, 역풍, 상승기류 3종만 사용한다.
- 하강기류와 난기류는 후반 난도 또는 다음 버전으로 미룬다.
- 바람은 랜덤으로 갑자기 나오기보다 구름/화살표로 1-2초 전에 예고한다.
- 활공 중에는 바람 효과 100%, 날개짓 중에는 바람 효과 40-60%로 적용한다.

## 에너지 설계

에너지는 런 전체를 지배하는 가장 중요한 자원이다.

- 시작 에너지: 100
- 날개짓 소모: 초당 18-24
- 에너지 0일 때: 날개짓 입력은 무시되거나 아주 약한 버둥거림만 발생
- 회복 아이템은 MVP에서 제외한다.
- 상승기류를 잘 타면 에너지를 아끼고 기록을 늘릴 수 있다.

에너지는 "쓰면 좋지만 아끼고 싶다"가 되어야 한다. 날개짓을 누르고만 있으면 초반은 편하지만 후반에 반드시 추락해야 한다.

## 난이도 설계

난이도는 거리 구간에 따라 오른다.

- 0-200m: 순풍과 상승기류 중심, 역풍은 짧게
- 200-500m: 역풍 구간 추가, 상승기류 간격 증가
- 500-900m: 상승기류가 좁아지고 피치 관리 중요
- 900m 이후: 바람 구간이 짧아지고 연속 판단이 필요

처음 10초는 학습 구간이다. 첫 플레이에서 플레이어가 날개짓과 활공의 차이를 몸으로 느끼기 전까지는 강한 역풍을 넣지 않는다.

## 점수 설계

기본 Score:

- 최종 비행 거리(m)

보조 기록 후보:

- 남은 에너지
- 최고 고도
- 가장 긴 무동력 활공 거리
- 상승기류를 탄 횟수

MVP에서는 최종 거리와 Best Distance만 크게 보여준다. 보조 기록은 결과 화면의 작은 통계로만 둔다.

## 실패와 피드백

런 종료:

- 땅에 닿으면 종료
- 속도가 너무 낮고 고도가 낮으면 사실상 착지/추락으로 종료

피드백:

- 날개짓: 선명한 날개 선, 짧은 종이 넘김 같은 소리, 에너지 바 감소
- 활공: 새가 길게 미끄러지는 곡선, 바람 화살표가 더 강하게 반응
- 좋은 활공: 얇은 초록 궤적 또는 거리 숫자 강조
- 실속: 새가 살짝 뒤로 젖혀지고 붉은 흔들림
- 상승기류 성공: 고도가 유지되며 바람 소용돌이 안에서 가볍게 떠오름

## 화면 구성

MVP 화면:

- 상단: Distance, Best, Energy
- 중앙: 새, 지면선, 고도감이 느껴지는 간단한 배경
- 오른쪽 전방: 곧 만날 바람 구간 예고
- 하단: 현재 상태 표시(Flap 또는 Glide), 속도/피치 간단 게이지
- 결과 화면: Distance, Best, Again

비주얼은 `docs/visual-direction.md`의 손그림 스케치 방향을 따른다. 새와 바람은 정밀한 일러스트보다 읽기 쉬운 선과 화살표로 표현한다.

## MVP 범위

첫 구현에 포함:

- 누르고 있으면 날개짓, 떼면 활공
- 에너지 소모
- 거리/고도/속도 계산
- 피치 자동 변화
- 순풍/역풍/상승기류
- 활공 중 바람 영향 증폭
- 실속 상태
- 런 종료
- 로컬 최고 거리 저장
- 다시하기

첫 구현에서 제외:

- 직접 각도 조절 입력
- 복잡한 날개 모양/양력 계수 시뮬레이션
- 회복 아이템
- 스테이지 선택
- 온라인 랭킹
- 스킨
- 광고
- 튜토리얼 화면

## 열어둘 질문

- 입력은 길게 누르기/떼기가 가장 좋을까, 아니면 두 버튼이 더 명확할까?
- 피치 게이지를 숫자로 보여줄까, 새의 기울기만으로 전달할까?
- 실속은 어느 정도 벌칙이어야 억울하지 않을까?
- 상승기류를 맵에 고정할까, 런마다 랜덤으로 둘까?
- 거리 기록 외에 "최장 활공" 같은 보조 기록이 다시 플레이를 늘릴까?

## 초기 결정

- 초기 가제: `플라잉버드`
- 중간 후보: `날개잔고`, `도요새`
- 현재 배포명: `Wind Gull`
- Unity 프로젝트 slug: `flying-bird`
- Android package name: `com.mannlab.games.flyingbird`
- C# namespace: `MannLab.Games.FlyingBird`
- 핵심 규칙: 정해진 에너지로 날개짓과 활공을 전환하며 최대 거리를 노린다.
- Score: 최종 비행 거리

## App Review Information

Paste the following into the App Review Information `Notes` field in App Store Connect. Replace the bracketed recording field if a new review video is captured.

```txt
Wind Gull - App Review Information

1. Screen recording
Screen recording link: [ADD REVIEW-ACCESSIBLE LINK]

The recording was captured on a physical device running the latest available operating system at test time. It begins with launching Wind Gull from the device home screen and shows the normal gameplay flow: starting a run, pressing/holding the screen to flap, releasing to glide, using energy while flapping, reading wind zones, gaining distance, ending a run when the bird reaches the ground or stalls, and tapping Again to restart.

The app has no account registration, login, account deletion, paid content, in-app purchases, subscriptions, user-generated content, reporting/blocking flows, camera access, microphone access, location access, contacts access, or App Tracking Transparency prompt.

2. Devices and operating systems tested
- [ADD DEVICE MODEL], [ADD OS VERSION]

3. App functions and target audience
Wind Gull is a casual one-touch flight distance game for players who enjoy short arcade runs and light physics-based timing. The player controls a gull by holding the screen to flap and releasing to glide. Flapping spends limited energy but helps recover altitude, speed, and pitch. Gliding saves energy but is more affected by wind, altitude, speed, and pitch. The goal is to read wind conditions, manage energy, avoid stalling or hitting the ground, and fly as far as possible. The app provides a compact flight challenge with quick retries and local best-distance tracking.

4. Setup and access instructions
No login or demo account is required. No sample files are required.

How to test:
1. Launch the app.
2. Press and hold the screen to flap and gain altitude.
3. Release the screen to glide and conserve energy.
4. Watch the distance, best distance, and energy indicators.
5. Continue switching between flap and glide while reading wind zones.
6. When the bird reaches the ground or loses too much speed, the run ends.
7. Tap Again to restart.

5. External services, tools, or platforms
The app uses Firebase Analytics for gameplay event analytics and Firebase Crashlytics for crash diagnostics. Gameplay itself runs locally on the device. Best distance and gameplay progress are stored locally on device. The app does not use authentication services, payment processors, ad networks, AI services, external gameplay services, online leaderboards, or remote content providers.

6. Regional differences
The app functions consistently across all regions. There are no region-specific features, content, pricing, services, or restrictions in the submitted build.

7. Regulated industry or protected third-party material
The app does not operate in a highly regulated industry and does not include protected third-party material requiring additional authorization. It is an original casual flight game implementation.
```

### Screen Recording Checklist

Use a physical iPhone or iPad with the latest available OS before resubmission.

1. Start recording before tapping the app icon.
2. Launch `Wind Gull`.
3. Show the distance, best distance, and energy indicators.
4. Hold the screen to flap.
5. Release to glide.
6. Show wind zones affecting the run if visible.
7. Show the run ending and tap `Again` to restart.
8. Upload the video to a review-accessible link and paste it into the Notes field above.
