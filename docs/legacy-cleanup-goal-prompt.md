# 프로젝트 레거시 일괄 정리 목표모드 프롬프트

기준일: 2026-09-01
대상 repo: `games`
주요 후보: `drum-duel`, `walking`/`Thumbwaddle` 명명 잔재, 웹 alias 빌드, Unity 생성물, 임시 산출물

## 목표모드에 넣을 프롬프트

```txt
이 repo의 레거시 프로젝트, 중복 공개 빌드, 로컬 생성물, 오래된 alias를 일괄 정리하라.

작업 목표는 "저장소를 현재 운영/출시 후보 중심으로 가볍고 명확하게 만드는 것"이다. 단, 아직 서비스 경로, 문서, 검증 스크립트, 릴리스 준비 흐름에서 참조되는 항목을 근거 없이 삭제하지 말라.

현재 알려진 레거시/정리 후보:

1. `drum-duel`
   - `README.md`에서 `candidate/archive`로 표시되어 있다.
   - `docs/supercent-fit-current-vs-potential-evaluation.md`에서 최하위/보관 판정이다.
   - `prototypes/drum-duel/`, `docs/drum-duel-game-design.md`, `scripts/verify-drum-duel-*`, `scripts/sync-drum-duel-webgl-to-site.sh`, `web/mannlab-games/public/games/drum-duel/`, `web/mannlab-games/src/main.jsx`의 게임 목록에 남아 있다.
   - 기본 방침: 보관 처리 또는 제거 대상으로 분류하되, 공개 웹에서 숨길지, 소스까지 삭제할지 근거를 제시하라.

2. `walking` / `Thumbwaddle`
   - 공개명은 `Thumbwaddle`, 내부 Unity 프로젝트명은 `walking`으로 남아 있다.
   - `.gitignore`에는 `prototypes/walking/`, `scripts/sync-walking-webgl-to-site.sh`, `scripts/verify-walking-mvp.sh`, `web/mannlab-games/public/games/walking/`이 로컬 스크래치로 표시되어 있다.
   - 그러나 실제로 `prototypes/walking/` 파일이 Git에 추적되고, 웹 앱에서도 `/thumbwaddle`의 alias로 `/walking`, `/sanchaek`이 살아 있다.
   - 기본 방침: 삭제하지 말고 명명/alias/ignore 정책 정리 대상으로 분류하라. Thumbwaddle가 유지 대상이면 `walking`을 제거 대상으로 오판하지 말라.

3. `sitting`
   - `web/mannlab-games/public/games/sitting/index.html`은 `/games/standing/`으로 redirect만 한다.
   - `web/mannlab-games/src/main.jsx`에서 `standing`의 alias로 `/sitting`이 있다.
   - 기본 방침: 호환용 redirect인지, 제거 가능한 alias인지 판정하라.

4. 웹 카탈로그 placeholder
   - `next-tile`, `one-more`는 웹 게임 목록에 `Soon`/`Draft`로만 있고 실제 prototype 디렉터리는 없다.
   - 기본 방침: 현재 운영 화면에 필요한 placeholder인지, 제거할 빈 슬롯인지 판정하라.

5. README와 실제 prototype 목록 불일치
   - `README.md`의 Current Prototypes에는 일부만 적혀 있다.
   - 실제 `prototypes/`에는 `best-ramyeon`, `gather-and-shot`, `rainwalker`, `standing`, `walking`, `yacht-rush`, `_unity-ios-admob-template` 등이 더 있다.
   - 기본 방침: README를 현재 구조에 맞게 업데이트하거나, 별도 상태 표를 만들어 active/prototype/archive/template/local로 구분하라.

6. Unity/웹 생성물 및 임시 산출물
   - `prototypes/*/Library`, `prototypes/*/Temp`, `prototypes/*/Logs`, `prototypes/*/Builds`, `prototypes/*/UserSettings`는 원칙적으로 Git 추적 대상이 아니다.
   - 현재 큰 로컬 생성물 후보: `prototypes/walking`, `prototypes/yacht-rush`, `prototypes/gather-and-shot`, `prototypes/2048-blink` 아래 Unity 생성물.
   - `tmp/`, `web/mannlab-games/dist/`, `mono_crash.*.json`도 임시/빌드 산출물 후보이다.
   - 기본 방침: Git 추적 여부를 먼저 확인하고, 추적되지 않는 로컬 생성물만 삭제 후보로 분류하라.

작업 절차:

1. `git status --short`로 사용자 변경사항을 먼저 확인하라.
   - 기존 변경사항은 사용자가 만든 것으로 간주하고 되돌리지 말라.
   - 삭제나 rename 전에 추적 상태를 확인하라.

2. 전체 inventory를 작성하라.
   - `prototypes/`의 1-depth 디렉터리 목록
   - `web/mannlab-games/src/main.jsx`의 게임 목록과 alias
   - `web/mannlab-games/public/games/`의 공개 빌드/redirect 목록
   - `scripts/verify-*`, `scripts/sync-*-webgl-to-site.sh`
   - `docs/*game-design.md`, 평가 문서, README 참조
   - `.gitignore`의 prototype 관련 규칙

3. 각 항목을 아래 상태 중 하나로 분류하라.
   - `active`: 유지 및 문서화 필요
   - `candidate`: 현재 개발/평가 후보
   - `archive`: 소스 보존, 웹/스크립트 노출 축소
   - `remove`: Git에서 제거 가능한 레거시
   - `local-clean`: Git 미추적 생성물 삭제 가능
   - `rename-cleanup`: 이름/alias/문서 정리 필요
   - `needs-confirmation`: 삭제하면 운영 경로가 깨질 수 있어 확인 필요

4. 삭제 전 승인 게이트를 둬라.
   - `local-clean` 항목은 Git 미추적이면 바로 정리 가능하다.
   - Git 추적 파일이나 공개 route를 삭제하는 경우에는 먼저 삭제 목록과 영향 범위를 요약하고 사용자 확인을 받아라.
   - `drum-duel`은 최소한 웹 카탈로그 숨김, 공개 빌드 제거, 문서 archive 이동/표시 중 어느 수준으로 정리할지 제안하라.

5. 정리 실행 시 우선순위:
   - 1순위: Git 미추적 Unity 생성물과 `tmp/`, crash dump 같은 로컬 산출물 정리
   - 2순위: README와 문서의 상태 표 업데이트
   - 3순위: 웹 카탈로그에서 archive/remove 대상 숨김 또는 상태를 `Archived`로 변경
   - 4순위: 불필요한 redirect alias 정리
   - 5순위: Git 추적 소스/문서/스크립트 삭제는 확인 후 실행

6. 검증:
   - `git status --short`로 최종 변경사항을 요약하라.
   - 웹 변경이 있으면 `web/mannlab-games`의 사용 가능한 검증 스크립트를 확인하고 실행하라.
   - 문서만 바꾼 경우에는 링크/경로가 깨지지 않았는지 `rg`로 확인하라.
   - 삭제한 항목이 남은 코드/문서에서 참조되는지 `rg`로 다시 검색하라.

완료 조건:

- 레거시/보관/활성/로컬청소 후보가 표로 정리되어 있다.
- Git 미추적 생성물과 안전한 로컬 산출물은 정리되어 있다.
- `drum-duel`은 보관 또는 제거 방향이 명확해져 있고, 웹 노출 정책이 문서/코드에 반영되어 있다.
- `walking`/`Thumbwaddle`은 보존 대상과 이름 정리 대상이 명확히 구분되어 있다.
- README와 웹 카탈로그가 실제 프로젝트 상태와 크게 어긋나지 않는다.
- 최종 `git status --short`와 실행한 검증 결과가 보고되어 있다.
```

## 권장 실행 방침

첫 실행에서는 `drum-duel` 소스 삭제까지 바로 가지 말고, 다음 수준으로 정리하는 것을 권장한다.

| 항목 | 권장 처리 | 이유 |
| --- | --- | --- |
| `drum-duel` 웹 카탈로그 | `Archived` 또는 숨김 | 문서상 보관 판정이지만 소스는 작고 참조가 명확하다. |
| `prototypes/drum-duel/` | 보존 | 240KB 수준이라 저장 비용이 작고, 리듬 실험 참고 가치가 있다. |
| `scripts/verify-drum-duel-*` | 보존 또는 archive 표시 | 소스를 보존하면 검증 스크립트도 같이 남기는 편이 안전하다. |
| `web/mannlab-games/public/games/drum-duel/` | 확인 후 제거 후보 | 공개 빌드는 운영 노출과 직접 연결된다. |
| `walking`/`Thumbwaddle` | 삭제 금지, 명명 정리 | 현재 MVP 및 alias로 쓰인다. |
| `sitting` redirect | 확인 후 유지/제거 | 이전 공유 링크 보호 목적일 수 있다. |
| Unity `Library/Temp/Logs/Builds/UserSettings` | Git 미추적이면 삭제 | 재생성 가능한 로컬 산출물이다. |
| `tmp/`, `mono_crash.*.json` | Git 미추적이면 삭제 | 임시 캡처/크래시 산출물로 보인다. |
| `web/mannlab-games/dist/` | 배포 소스 정책 확인 후 정리 | 정적 배포 산출물일 수 있으므로 workflow 확인 필요. |

## 첫 목표모드에서 기대하는 결과물

- `docs/project-inventory.md` 또는 README의 상태 표
- 웹 카탈로그의 archive/remove 정책 반영
- 로컬 생성물 삭제 후 용량 감소
- 남은 레거시 후보 목록과 다음 삭제 승인안
