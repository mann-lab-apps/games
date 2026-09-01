# Supercent 게임 제작 기준 리서치 및 요구 수준 정의

기준일: 2026-09-01  
작성 목적: 슈퍼센트(Supercent)의 공개 포트폴리오, 스토어 지표, 공개 블로그/퍼블리싱 자료를 바탕으로 신규 캐주얼/하이퍼캐주얼/하이브리드캐주얼 게임의 "출시 가능 수준"과 "프로토타입 통과 기준"을 정의한다.

## 1. 핵심 결론

슈퍼센트형 게임은 "단순한 재미있는 프로토타입"보다 요구 수준이 높다. 공개 자료 기준으로 슈퍼센트는 낮은 CPI 가능성, 첫 세션의 즉시 이해, 반복 가능한 성장 루프, 대중적인 테마, 빠른 크리에이티브 테스트, 데이터 기반 라이브 개선을 동시에 본다.

우리의 기준은 다음으로 잡는다.

- 아이디어 단계: 10초 안에 광고 소재로 설명되는 훅이 있어야 한다.
- 1주 프로토타입: 첫 60초 안에 조작, 보상, 업그레이드, 다음 목표가 모두 보여야 한다.
- 테스트 통과: CPI 1차 목표는 $1 미만, 개선 목표는 $0.50 미만, 강한 후보는 $0.30-$0.50 구간을 노린다.
- 제품 통과: D1 retention은 최소 35% 이상, 강한 후보는 45%-50% 이상을 노린다.
- 출시 전 빌드: 단순 조작 + 즉시 보상 + 메타 성장 + 광고/IAP 구조 + 15개 이상 콘텐츠 단위 + 지표 로깅이 있어야 한다.
- 광고 리스크: 슈퍼센트 대표작 리뷰에서 강제 광고 불만이 반복된다. 수익화는 강하되, 첫 3분 강제 광고 금지와 광고 실패/보상 미지급 방지는 필수 기준으로 둔다.

## 2. 사용한 소스와 신뢰도

| 구분 | 소스 | 확인일 | 사용 범위 |
| --- | --- | --- | --- |
| 공식 | [Supercent Games](https://supercent.io/game) | 2026-09-01 | 공식 포트폴리오, Featured/All Games 목록 |
| 공식 | [Supercent About](https://supercent.io/about) | 2026-09-01 | 회사 성과: 누적 다운로드, 매출, MAU, 퍼블리셔 순위 |
| 공식 | [Supercent Blog - Recipe Part 1](https://medium.com/supercent-blog/breaking-the-mold-supercents-recipe-for-success-in-a-competitive-market-b52b93b7f5b0) | 2026-09-01 | 시장 변화, 신규 히트 난이도, Burger Please 사례 |
| 공식 | [Supercent Blog - Recipe Part 2](https://medium.com/supercent-blog/breaking-the-mold-supercents-recipe-for-success-in-a-competitive-market-part-2-d6f04df6da4c) | 2026-09-01 | D1 50%, CPI <$1, Snake Clash CPI 개선 사례, 메타/코어루프 |
| 공식/파트너 | [Supercent Blog - AppMagic case](https://medium.com/supercent-blog/how-supercents-new-games-made-it-to-the-top-in-the-stagnating-hypercasual-market-7c9f8f8ccd73) | 2026-09-01 | UA, 광고 소재, Burger Please/Outlets Rush 분석 |
| 보조 | [PocketGamer.biz AppMagic case](https://www.pocketgamer.biz/case-study-how-supercents-games-made-it-to-the-top-in-a-stagnating-hypercasual-market/) | 2026-09-01 | AppMagic 기반 시장/UA 해석 |
| 보조 | [Chrome-Stats Supercent publisher page](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu) | 2026-09-01 | Android 추정 다운로드/평점/리뷰 수 |
| 공식 스토어 | [Google Play - Supercent developer](https://play.google.com/store/apps/dev?id=6384832178452405684) | 2026-09-01 | Google Play 개발사 노출 목록/평점 |
| 벤치마크 | [GameAnalytics 2026 Mobile & PC Gaming Benchmarks](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks) | 2026-09-01 | D1/D7/D30, 세션 길이, 플레이타임 벤치마크 |

주의: Chrome-Stats, AppBrain, AppRank, Sensor Tower류 수치는 공개 추정치다. 공식 스토어의 `100M+`, `50M+`, `10M+` 같은 다운로드 배지는 공식 스토어 노출 수치지만 세부 다운로드 수는 범위형이다.

## 3. 슈퍼센트 공개 성과

공식 About 페이지 기준 수치다.

| 항목 | 수치 | 출처/확인일 |
| --- | --- | --- |
| 누적 다운로드 | 1.5B+ | [Supercent About](https://supercent.io/about), 2026-09-01 |
| 누적 매출 | $380M+ | [Supercent About](https://supercent.io/about), 2026-09-01 |
| 글로벌 게임 퍼블리셔 순위 | #7 | [Supercent About](https://supercent.io/about), 2026-09-01 |
| 글로벌 앱 퍼블리셔 순위 | #16 | [Supercent About](https://supercent.io/about), 2026-09-01 |
| MAU | 100M+ | [Supercent About](https://supercent.io/about), 2026-09-01 |
| Android 공개 추정 합산 audience | 1,467,073,159 | [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01, 보조 추정 |
| Android 공개 추정 평점 평균 | 4.46 / 11,359,463 ratings | [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01, 보조 추정 |

## 4. 대표작 11개 리서치 요약

| 게임 | 장르/서브장르 | 공식/스토어 수치 | 보조 추정 수치 | 핵심 10초 훅 | 슈퍼센트다운 특징 |
| --- | --- | --- | --- | --- | --- |
| Pizza Ready! | 음식점 idle tycoon / simulation | Google Play `100M+`, 4.4, 2.82M reviews, 업데이트 2026-08-11. 출처: [Google Play](https://play.google.com/store/apps/details?hl=en-US&id=io.supercent.pizzaidle), 2026-09-01 | 392,647,647 downloads, 4.41, 2,815,796 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 작은 피자가게를 직접 굴려 글로벌 프랜차이즈로 키운다. | 손님 응대, 조리, 청소, 업그레이드, 고용이 한 화면에서 연결된다. |
| Burger Please! | 음식점 idle arcade / tycoon | Google Play `100M+`, 4.4, 626K reviews. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.burgeridle), 2026-09-01 | 104,670,090 downloads, 4.38, 625,921 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 햄버거 매장을 직접 뛰어다니며 돈 벌고 자동화한다. | 대중적인 음식 테마와 조이스틱 이동/스택 조작의 조합. |
| Snake Clash! | snake battle / arcade simulation | Google Play `100M+`, 4.5, 1.44M reviews. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.linkedcubic), 2026-09-01. App Store 776K ratings, 4.6. 출처: [App Store](https://apps.apple.com/us/app/snake-clash/id6449243946), 2026-09-01 | 191,485,007 downloads, 4.51, 1,443,883 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 작게 시작해 먹고 커져서 더 큰 상대를 잡아먹는다. | 즉시 이해되는 먹이사슬, 크기 성장, 위험/보상 판단. |
| Outlets Rush | retail idle tycoon / simulation | Google Play `50M+`, 4.4, 312K reviews, 업데이트 2026-08-03. 출처: [Google Play](https://play.google.com/store/apps/details?id=com.corestudios.storemanagementidle), 2026-09-01 | 78,622,426 downloads, 4.36, 312,553 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 작은 매장을 거대 쇼핑 아울렛으로 확장한다. | 상품 진열, 피팅룸 청소, 계산, 고용 등 익숙한 오프라인 노동을 루프로 만든다. |
| Prison Life: Idle Game | prison management / idle tycoon | Google Play `50M+`, 4.1, 126K reviews, 업데이트 2026-08-27. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.prison), 2026-09-01 | 55,831,272 downloads, 4.11, 126,066 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 감옥을 운영하고 죄수 흐름을 관리해 시설을 키운다. | 강한 테마 차별성, 방/시설 업그레이드, 직원 배치. |
| Suzy's Food Restaurant Game | cooking dash + idle tycoon | Google Play `10M+`, 4.3, 268K reviews, 업데이트 2026-08-28. 출처: [Google Play](https://play.google.com/store/apps/details?id=com.corestudiso.suzyrest), 2026-09-01 | 27,769,956 downloads, 4.26, 267,765 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 주문을 빠르게 처리하고 셰프/직원을 키워 레스토랑 제국을 만든다. | 시간 압박과 idle 자동화가 함께 있다. |
| Super Big Slime: Black Hole 3D | devour / black hole action | Google Play `10M+`, 4.3, 211K reviews, 업데이트 2026-08-07. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.bigslimemanyslime), 2026-09-01 | 30,650,597 downloads, 4.33, 211,236 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 작은 슬라임으로 시작해 도시 전체를 삼킨다. | 스케일 성장의 시각적 쾌감, 타임어택, 마지막 보스전. |
| Coffee Break - Cafe Simulation | cafe idle tycoon | Google Play `10M+`, 4.5, 160K reviews, 업데이트 2026-07-29. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.coffeebreak), 2026-09-01 | 24,826,008 downloads, 4.51, 159,848 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 커피를 만들고 손님을 응대하며 카페를 확장한다. | 친숙한 카페 판타지, 드라이브스루/지점 확장. |
| Dinosaur Universe | idle RPG / creature collection | Google Play `10M+`, 4.4, 166K reviews, 업데이트 2026-08-12. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.ageofdinosaurs), 2026-09-01. App Store 42K ratings, 4.7. 출처: [App Store](https://apps.apple.com/us/app/dinosaur-universe/id6448496802), 2026-09-01 | 21,380,468 downloads, 4.43, 166,313 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 공룡 알을 부화시키고 랩터 부대를 키워 보스를 잡는다. | 수집, 진화, 전투, idle 보상의 하이브리드화. |
| WaterPark Boys | water park idle tycoon | Google Play `10M+`, 4.5, 94.3K reviews, 업데이트 2026-07-28. 출처: [Google Play](https://play.google.com/store/apps/details?id=com.Albus.WaterParkBoys), 2026-09-01 | 20,197,179 downloads, 4.52, 94,148 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 워터파크를 청소/운영/확장해 여름 테마파크 제국을 만든다. | 청소, 구조, 고객 흐름, 시설 확장이 즉시 보인다. |
| XP Hero | action idle RPG / survival RPG | Google Play `10M+`, 4.4, 174K reviews, 업데이트 2026-08-24. 출처: [Google Play](https://play.google.com/store/apps/details?id=io.supercent.weaponrpg), 2026-09-01. App Store 21K ratings, 4.7. 출처: [App Store](https://apps.apple.com/us/app/xp-hero/id6740618570), 2026-09-01 | 11,707,266 downloads, 4.36, 175,527 ratings. 출처: [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 | 몬스터 웨이브를 뚫고 무기/스킬을 강화해 보스를 잡는다. | 전투-보상-성장-보스의 RPG 깊이를 캐주얼 조작으로 압축. |

## 5. 게임별 1분 루프와 메타 구조

| 게임 | 첫 1분 플레이 루프 | 메타 성장 구조 | 광고/IAP 구조 추정 |
| --- | --- | --- | --- |
| Pizza Ready! | 이동 -> 피자 제조/수거 -> 손님에게 전달 -> 돈 획득 -> 스테이션 업그레이드 | 매장 확장, 직원 고용, 설비 업그레이드, 지점/프랜차이즈 성장 | 공식 스토어상 광고+IAP. 리뷰상 강제 광고 불만 존재. No-ads/부스터/재화 패키지형으로 추정 |
| Burger Please! | 재료/버거 수거 -> 카운터 전달 -> 계산 -> 신규 설비 개방 | 음식 종류 확장, 직원 자동화, 매장 확장 | 광고+IAP. 광고 제거/재화/부스터형으로 추정 |
| Snake Clash! | 먹이 수집 -> 몸집 성장 -> 작은 상대 공격 -> 큰 상대 회피 -> 보스/랭킹 목표 | 스킨, 파워업, 시즌/랭킹, 능력 성장 | 광고+IAP. App Store IAP 예: $0.99-$9.99 재화/VIP/스킨 |
| Outlets Rush | 상품 수거 -> 진열 -> 고객 응대/계산 -> 돈 획득 -> 매장 확장 | 신규 매장, 상품군, 직원, 청소/계산 자동화 | 광고+IAP. App Store IAP 예: no ads, VIP weekly, gems |
| Prison Life | 죄수 수용 -> 니즈 처리 -> 시설 업그레이드 -> 직원 배치 | 방/시설/감옥 단계 확장, guard/staff 성장 | 광고+IAP. 리뷰상 no-ads 결제 후에도 광고 접점 불만 존재 |
| Suzy's Food | 주문 확인 -> 조리/서빙 -> 돈/레벨 보상 -> 직원 훈련 | 레스토랑, 도시, 셰프/웨이터, 음식/장비 강화 | 광고+IAP. 리뷰상 광고와 콘텐츠 부족 불만 존재 |
| Super Big Slime | 작은 오브젝트 먹기 -> 크기 증가 -> 큰 오브젝트 먹기 -> 제한시간 목표 -> 보스 | 크기/흡입력/스테이지/미션/스킨 성장 | 광고+IAP. 리뷰상 중간 광고/보상 실패 리스크 존재 |
| Coffee Break | 커피 제조 -> 테이블/테이크아웃 응대 -> 청소 -> 돈 획득 -> 직원 고용 | 테이블, 드라이브스루, 지점, 직원 자동화 | 광고+IAP. 리뷰상 30초-1분 단위 광고 불만 존재 |
| Dinosaur Universe | 적 접근/전투 -> 보상 -> 알 부화/동료 획득 -> 보스 도전 | 공룡 수집, 진화, 스킬, 챕터, 일일 보상 | 광고+IAP. App Store IAP 예: egg collector, gems, level pass |
| WaterPark Boys | 입장/튜브/청소/구조 처리 -> 돈 획득 -> 시설 업그레이드 | 슬라이드/풀/스태프/스킨/다음 파크 | 광고+IAP. 리뷰상 레벨 부족/광고 빈도 불만 존재 |
| XP Hero | 자동/간단 전투 -> 몬스터 처치 -> XP/장비 보상 -> 스킬/무기 강화 -> 보스 | 무기, 스킬, 진화 재료, 출석, 보스 레이드, 패스 | 광고+IAP. App Store IAP 예: no-ads, subscription, passes, lucky spin |

## 6. 슈퍼센트 게임들의 공통 성공 패턴

### 6.1 테마

- 음식점, 카페, 쇼핑몰, 워터파크, 감옥, 공룡, 뱀처럼 대중이 즉시 이해하는 소재를 쓴다.
- 테마는 CPI에 큰 영향을 준다는 점을 슈퍼센트가 직접 강조했다. 출처: [Supercent Blog - AppMagic case](https://medium.com/supercent-blog/how-supercents-new-games-made-it-to-the-top-in-the-stagnating-hypercasual-market-7c9f8f8ccd73), 2026-09-01.
- 현실 노동/관리 판타지를 한 손 조작으로 압축한다. 예: 만들기, 나르기, 진열하기, 청소하기, 계산하기, 고용하기.

### 6.2 조작

- 대부분 한 손 조이스틱 이동, 탭, 자동 전투, 가까이 가면 수집/처리되는 방식이다.
- 첫 5초 안에 "어디로 가야 하는지"가 보이고, 첫 15초 안에 돈/XP/성장 숫자가 증가한다.
- 실패보다 진행감이 우선이다. 어려움은 액션 게임에서도 "성장하면 뚫린다"로 해석된다.

### 6.3 코어 루프

반복 구조는 대체로 다음이다.

1. 이동하거나 접근한다.
2. 자원을 얻거나 적/손님/오브젝트를 처리한다.
3. 즉시 돈, XP, 크기, 전투력, 공간 확장 중 하나가 증가한다.
4. 업그레이드 지점이 5-20초 안에 열린다.
5. 자동화 또는 더 큰 대상이 다음 목표가 된다.

### 6.4 메타 레이어

슈퍼센트 블로그는 낮은 CPI만으로 충분하지 않고, 더 깊은 gameplay와 meta-layer가 필요하다고 설명한다. Core objective를 정의하고, 여러 성장 루트를 제공하며, level design과 economy balancing으로 진행감을 조절해야 한다. 출처: [Supercent Blog - Recipe Part 2](https://medium.com/supercent-blog/breaking-the-mold-supercents-recipe-for-success-in-a-competitive-market-part-2-d6f04df6da4c), 2026-09-01.

필수 메타 레이어:

- 스테이션/방/시설/캐릭터 업그레이드
- 직원/동료/펫/자동화
- 다음 지역/지점/챕터
- 일일 보상 또는 미션
- 광고 보상으로 당장 얻는 숏컷

### 6.5 UA/광고 소재

- 슈퍼센트는 경쟁작 크리에이티브를 매주 분석하고, video ads와 playable ads를 섞어 사용한다고 공개했다. 출처: [Supercent Blog - AppMagic case](https://medium.com/supercent-blog/how-supercents-new-games-made-it-to-the-top-in-the-stagnating-hypercasual-market-7c9f8f8ccd73), 2026-09-01.
- 광고 소재는 컬러/UI보다 "초반 몇 초의 UX와 감정"을 더 본다고 설명되어 있다. 출처: [PocketGamer.biz AppMagic case](https://www.pocketgamer.biz/case-study-how-supercents-games-made-it-to-the-top-in-a-stagnating-hypercasual-market/), 2026-09-01.
- 따라서 우리 프로토타입도 15-30초 영상 소재로 바로 잘라낼 수 있어야 한다.

### 6.6 수익화

- 조사 대상 11개 모두 Google Play 또는 App Store에서 광고 및 IAP를 포함한다.
- 광고는 보상형, 강제 인터스티셜, no-ads, VIP/패스/재화/부스터 조합으로 추정된다.
- 단, 대표작 리뷰에서 광고 과다, 광고 실패, no-ads 결제 후 기대 불일치가 반복된다. 우리 기준에서는 광고 수익화보다 평점 방어를 더 강하게 둔다.

## 7. KPI 기준표

| 단계 | 지표 | Must | Should | Nice-to-have | 근거/확인일 |
| --- | --- | --- | --- | --- | --- |
| 아이디어 | 10초 광고 이해도 | 5명 중 4명이 목표/쾌감을 설명 | 5명 중 5명 설명 | 3개 이상 광고 앵글 도출 | Supercent가 marketability와 크리에이티브 분석을 강조. [AppMagic case](https://medium.com/supercent-blog/how-supercents-new-games-made-it-to-the-top-in-the-stagnating-hypercasual-market-7c9f8f8ccd73), 2026-09-01 |
| 아이디어 | CPI 가설 | $1 미만 가능성 명확 | $0.50 미만 가능성 | $0.30-$0.50 도전 가능 | Supercent는 80%의 경우 CPI <$1를 찾는다고 공개. Snake Clash는 $0.7->$0.3 개선 사례. [Recipe Part 2](https://medium.com/supercent-blog/breaking-the-mold-supercents-recipe-for-success-in-a-competitive-market-part-2-d6f04df6da4c), 2026-09-01 |
| 1주 프로토타입 | 첫 세션 길이 | 3분 이상 자연 플레이 | 5분 이상 | 8분 이상 | GameAnalytics 2025 모바일 median session 3.1-3.5분, P75 약 5.2분, P90 8분+. [GameAnalytics](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks), 2026-09-01 |
| 1주 프로토타입 | D1 retention | 35% 이상 | 45% 이상 | 50% 이상 | Supercent는 D1 50%를 주요 KPI 예시로 언급. GameAnalytics P90 D1 약 40%. [Recipe Part 2](https://medium.com/supercent-blog/breaking-the-mold-supercents-recipe-for-success-in-a-competitive-market-part-2-d6f04df6da4c), [GameAnalytics](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks), 2026-09-01 |
| 소프트런칭 | D7 retention | 8% 이상 | 12% 이상 | 20% 이상 | GameAnalytics P75 D7 6-7%, P90 11-12%, P99 25%+. [GameAnalytics](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks), 2026-09-01 |
| 소프트런칭 | D30 retention | 1.5% 이상 | 4% 이상 | 8% 이상 | GameAnalytics P75 D30 1.6-1.8%, P90 지역별 2.5%-5%대, P99 7%-16%대. [GameAnalytics](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks), 2026-09-01 |
| 프로토타입 | 튜토리얼 이탈률 | 첫 60초 이탈 30% 이하 | 20% 이하 | 15% 이하 | GameAnalytics는 첫 5분/첫 세션 중요성을 강조. [GameAnalytics](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks), 2026-09-01 |
| 프로토타입 | 첫 보상 시간 | 15초 이내 | 8초 이내 | 5초 이내 | 슈퍼센트 대표작 공통 루프 관찰 기반. [Supercent Games](https://supercent.io/game), 2026-09-01 |
| 소프트런칭 | 광고 노출 허용선 | 첫 3분 강제 광고 0회, 이후 90초보다 잦은 강제 광고 금지 | 보상형 중심, 강제 광고는 스테이지/휴식점에만 | 광고 없는 첫 5분 | 대표작 리뷰에서 광고 과다 불만 반복. 각 Google Play 리뷰, 2026-09-01 |
| 소프트런칭 | 광고 보상 안정성 | 보상 실패율 0.5% 이하 | 0.2% 이하 | 0.1% 이하 | Super Big Slime, Suzy's, XP Hero 등 리뷰상 광고 실패/재시작 불만 확인. Google Play, 2026-09-01 |
| 출시 후보 | 평점 리스크 | 테스트 리뷰/설문 평균 4.0 이상 | 4.3 이상 | 4.5 이상 | 슈퍼센트 Android 추정 평균 4.46. [Chrome-Stats](https://chrome-stats.com/a/U3VwZXJjZW50LCBJbmMu), 2026-09-01 |
| 출시 후보 | 콘텐츠 볼륨 | 30분 이상 신규 목표 | 2시간 이상 | 7일 이상 daily/mission | WaterPark Boys/Suzy's 리뷰에서 레벨 부족 불만 확인. Google Play, 2026-09-01 |

## 8. 신규 게임 아이디어 단계 통과 기준

### Must

- 한 문장 훅이 있다: "작은 X를 키워 거대한 Y를 만든다" 또는 "약한 X가 먹고/모아/업그레이드해서 강한 Y를 이긴다".
- 10초 영상 콘티가 가능하다: 시작 상태, 조작, 보상, 성장, 더 큰 목표가 모두 들어간다.
- 대중 테마 또는 강한 밈/쇼츠형 소재다.
- 조작은 한 손으로 설명 없이 가능하다.
- 첫 1분 안에 최소 3번의 보상이 발생한다.
- 최소 3개 이상의 성장 축이 있다: 공간, 능력, 자동화, 수집, 장비, 스킨 중 3개.
- CPI <$1 가설을 문장으로 설명할 수 있다.

### Should

- 경쟁작 5개와 광고 소재 10개를 분석했다.
- 같은 코어 메커닉에 다른 테마 3개를 대입해 비교했다.
- 15초, 30초, playable ad 소재 기획이 각각 나온다.
- 스토어 아이콘에서 게임 목표가 읽힌다.
- 첫 스크린샷에서 "무엇을 하는 게임인지"가 보인다.

### Nice-to-have

- TikTok/Reels 쇼츠처럼 보기만 해도 만족스러운 전후 변화가 있다.
- 실패 장면도 광고 소재가 된다.
- 지역별로 현지화 가능한 테마다.
- 캐릭터/스킨/컬렉션 확장성이 있다.

## 9. 1주 프로토타입 통과 기준

### Must

- iOS/Android 또는 WebGL에서 실제 플레이 가능한 빌드가 있다.
- 첫 60초 루프가 완성되어 있다: 조작 -> 보상 -> 업그레이드 -> 다음 목표.
- 첫 5분 안에 새로운 시설/능력/지역/적 중 최소 3개가 열린다.
- 플레이어가 멈추지 않도록 다음 목표 UI가 항상 보인다.
- 카메라, 조작, 충돌, 수집, 보상 숫자가 끊기지 않는다.
- 15초 광고 영상으로 자를 수 있는 장면이 최소 3개 있다.
- 기본 이벤트 로깅이 있다: install/open, tutorial_start/complete, first_reward, first_upgrade, ad_impression, ad_reward, session_end.

### Should

- 20분 이상 콘텐츠가 있다.
- 광고 보상 버튼과 no-ads/booster 같은 수익화 자리만이라도 더미로 들어가 있다.
- 난이도/가격/보상 밸런스가 JSON 또는 ScriptableObject 등으로 분리되어 있다.
- 첫 플레이 5명 테스트에서 4명 이상이 "다음에 무엇을 해야 하는지" 묻지 않는다.

### Nice-to-have

- 2개 이상 테마 스킨 또는 스테이지 변주가 있다.
- playable ad용 미니 빌드로 분리 가능하다.
- CPI 테스트용 영상 5종을 바로 캡처할 수 있다.

## 10. 소프트런칭 전 빌드 퀄리티 기준

### Must

- 첫 30분 콘텐츠가 끊기지 않는다.
- 첫 7일 유지 장치가 있다: 출석, 일일 미션, 반복 보상, 챕터 목표 중 2개 이상.
- 광고 SDK, IAP, 개인정보/동의, 크래시 리포팅, 분석 이벤트가 통합되어 있다.
- 첫 3분 강제 광고가 없다.
- 보상형 광고 실패 시 보상 미지급/무한 로딩/앱 재시작이 없다.
- 오프라인 플레이 가능성을 내세우는 경우, 실제 오프라인에서도 핵심 루프가 동작한다.
- 스토어 스크린샷 5장 이상이 실제 게임 장면으로 구성된다.
- 15-30초 UA 영상 5개, playable ad 콘셉트 1개 이상을 준비한다.

### Should

- 첫 2시간 콘텐츠와 가격곡선이 있다.
- 최소 3개 지역/스테이지/매장/챕터가 있다.
- 광고 빈도 A/B 테스트 설정이 있다.
- 앱 크기, 발열, 프레임, 로딩 시간 QA가 끝났다.
- 리뷰 리스크 문구를 사전에 점검했다: "too many ads", "not enough levels", "ad reward failed", "paid no ads but still ads".

### Nice-to-have

- 지역별 CPI 테스트용 로컬라이징 소재가 있다.
- 장기 메타: 패스, 시즌, 수집 앨범, 이벤트가 설계되어 있다.
- 라이브 운영 없이도 14일 이상 반복 목표가 있다.

## 11. 게임 품질 루브릭

각 항목은 1-5점으로 채점한다. 소프트런칭 후보는 평균 4.0 이상, `Core Fun`, `Marketability`, `First Session`은 각각 4점 이상이어야 한다.

| 항목 | 1점 | 3점 | 5점 |
| --- | --- | --- | --- |
| Core Fun | 조작은 되지만 왜 하는지 약하다 | 반복 보상이 있으나 금방 단조롭다 | 5초마다 작은 선택/보상/쾌감이 있다 |
| Marketability | 영상으로 봐도 목표가 불명확하다 | 설명하면 이해된다 | 10초 무자막 영상만으로 설치 욕구가 생긴다 |
| First Session | 튜토리얼 없이는 막힌다 | 1분 내 기본 루프는 이해된다 | 첫 5분 동안 보상/성장/새 목표가 자연스럽게 이어진다 |
| Controls/Feel | 이동/터치/충돌이 답답하다 | 기능적으로 무난하다 | 손맛, 속도, 피드백, 카메라가 즉각적이다 |
| Visual Clarity | 오브젝트 역할이 헷갈린다 | 중요한 것은 구분된다 | 한 화면에서 위험, 보상, 목표, 업그레이드가 즉시 읽힌다 |
| Progression | 숫자만 오른다 | 업그레이드와 새 기능이 있다 | 공간/능력/자동화/수집이 서로 맞물린다 |
| Monetization | 광고가 플레이를 끊는다 | 보상형과 강제 광고가 섞여 있다 | 보상형 광고가 욕구를 만들고 강제 광고는 흐름을 해치지 않는다 |
| Content Scalability | 새 콘텐츠 추가가 어렵다 | 스테이지/스킨 추가 가능 | 테마만 바꿔도 지점/적/상품/직원/퀘스트 확장이 쉽다 |
| LiveOps Readiness | 이벤트/원격 설정이 없다 | 기본 로그와 밸런스 조정 가능 | 광고, 경제, 미션, 콘텐츠가 원격 조정 가능하다 |

## 12. 스토어/ASO 공통 패턴

### Must

- 제목/부제에서 장르와 판타지가 바로 보인다: `Pizza Ready!`, `Burger Please!`, `Snake Clash!`, `Outlets Rush`.
- 스토어 설명 첫 줄에 "무엇을 키우는지"가 나온다.
- 스크린샷은 실제 플레이 장면 중심이어야 한다.
- 첫 스크린샷은 조작보다 결과를 보여준다: 매장 확장, 거대 성장, 보스, 많은 손님, 큰 보상.

### Should

- 키워드는 `idle`, `tycoon`, `simulation`, `management`, `offline`, `single player`, `stylized`, `light-hearted` 중심으로 구성한다.
- Feature copy는 동사로 시작한다: build, cook, serve, grow, hire, upgrade, expand, survive, evolve.
- 4장 이내에 성장 전후 비교가 보인다.

### Nice-to-have

- 플레이어가 광고에서 본 장면을 첫 3분 안에 실제로 경험한다.
- 리뷰에서 칭찬받은 단어를 ASO에 반영한다: addictive, fun, satisfying, easy.

## 13. AI 게임 제작자/에이전트에게 요구할 완료 기준

### Must

- 게임은 첫 화면부터 바로 플레이 가능해야 한다. 랜딩 페이지, 설명 페이지, 빈 로비로 시작하지 않는다.
- 첫 5분 안에 조작, 보상, 업그레이드, 자동화 또는 성장 목표가 모두 등장해야 한다.
- 코어 루프는 1문장으로 설명 가능해야 한다.
- 게임 화면만 녹화해도 15초 광고 소재가 되어야 한다.
- 플레이어가 10초 이상 할 일을 못 찾는 상태가 없어야 한다.
- 최소 20분 이상 반복 가능한 콘텐츠와 3개 이상의 성장 축을 구현해야 한다.
- 광고/IAP는 실제 SDK가 아니어도 더미 UI와 이벤트 흐름까지 들어가야 한다.
- 이벤트 로깅 이름과 발생 시점이 문서화되어야 한다.
- 모바일 세로 화면에서 모든 UI가 겹치지 않아야 한다.
- 빌드는 실제 실행 검증을 통과해야 한다.

### Should

- 가격/보상/레벨 데이터는 코드 하드코딩이 아니라 데이터 테이블로 조정 가능해야 한다.
- 첫 세션이 3-5분 이상 이어지도록 5초, 30초, 2분, 5분 목표가 단계적으로 배치되어야 한다.
- 보상형 광고 버튼은 플레이어가 "보고 싶어서 누르는" 위치에 있어야 한다.
- 강제 광고는 첫 3분 이후, 자연스러운 휴식 지점에만 배치되어야 한다.
- 스토어 스크린샷 5장과 광고 영상 콘티 3개를 함께 제공해야 한다.

### Nice-to-have

- playable ad 전용 축약 모드를 제공한다.
- 테마 스왑이 가능한 구조로 만든다.
- 일일 미션, 출석, 패스, 시즌 이벤트 중 1개 이상이 구현되어 있다.

## 14. AI 게임 제작 프롬프트에 넣을 품질 기준 문장

아래 문장을 게임 제작 프롬프트의 "완료 기준" 또는 "품질 기준" 섹션에 붙여 넣는다.

```text
이 게임은 Supercent 스타일의 글로벌 캐주얼/하이브리드캐주얼 출시 후보 수준을 목표로 한다. 단순히 작동하는 프로토타입이 아니라, 10초 광고 영상만으로 목표와 재미가 이해되고, 첫 60초 안에 조작-보상-업그레이드-다음 목표가 모두 드러나야 한다.

첫 5분 플레이 안에는 최소 3번의 명확한 성장 순간이 있어야 한다. 플레이어는 이동하거나 탭하는 즉시 돈, XP, 크기, 전투력, 공간 확장, 자동화 중 하나의 보상을 받아야 하며, 다음 목표는 항상 화면에서 읽혀야 한다.

코어 루프는 한 손 조작으로 플레이 가능해야 한다. 조작은 설명 없이 이해되어야 하고, 카메라/충돌/수집/보상 피드백은 즉각적이어야 한다. 10초 이상 플레이어가 할 일을 찾지 못하는 순간이 있으면 실패로 본다.

게임은 대중적인 테마와 강한 시각적 전후 변화를 가져야 한다. 작은 가게가 큰 제국이 되거나, 약한 캐릭터가 먹고 성장해 강한 적을 이기는 식의 스케일업 쾌감이 필요하다.

프로토타입에는 최소 20분 이상 반복 가능한 콘텐츠, 3개 이상의 성장 축, 더미 광고/IAP 흐름, 기본 분석 이벤트가 포함되어야 한다. 이벤트는 tutorial_start, tutorial_complete, first_reward, first_upgrade, ad_impression, ad_reward, session_end를 최소로 한다.

수익화는 플레이 흐름을 해치지 않아야 한다. 첫 3분 동안 강제 광고는 금지한다. 보상형 광고는 플레이어가 원해서 누르는 보상 지점에 배치하고, 광고 실패 시 보상 미지급/무한 로딩/앱 재시작이 발생하지 않도록 처리한다.

출시 후보로 판단하려면 내부 테스트 기준으로 첫 세션 3분 이상, D1 retention 35% 이상 가능성, D7 retention 8% 이상 가능성, CPI $1 미만 가능성을 보여야 한다. 강한 후보는 D1 45%-50%, D7 12% 이상, CPI $0.50 미만을 목표로 한다.

최종 제출물에는 실행 가능한 빌드, 핵심 루프 설명, 15초 광고 콘티 3개, 스토어 스크린샷 콘셉트 5개, 밸런스 데이터, 테스트 체크리스트, 알려진 리스크와 다음 개선안을 포함한다.
```

