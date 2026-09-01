# Project Inventory And Legacy Cleanup

기준일: 2026-09-01

이 문서는 `prototypes/`, 웹 카탈로그, 공개 WebGL 빌드, 검증 스크립트, 문서 참조를 맞춰 본 현재 상태표다. 삭제 가능한 로컬 생성물은 정리했고, Git 추적 소스/문서/공개 route 삭제는 별도 승인 대상으로 남긴다.

## Status Table

| Item | Status | Decision | Evidence / Follow-up |
| --- | --- | --- | --- |
| `10000` | `active` | 유지 | README와 웹 카탈로그에서 `Live`; WebGL/검증 스크립트 존재. |
| `gather-and-shot` | `active` | 최우선 개선 후보로 유지 | 평가 문서 1위 후보; WebGL, iOS/AdMob/Firebase 검증 스크립트 존재. |
| `standing` | `candidate` | 유지 | 광고 훅 최고 후보; `/standing` 유지, `/sitting`은 호환 alias. |
| `yacht-rush` | `candidate` | 유지 | 구현량과 콘텐츠 단위가 좋은 완성형 실험 후보. |
| `2048-crash` | `candidate` | 유지 | 출시 실험 기준 안정 후보; release prep 문서와 Android/iOS 검증 스크립트 존재. |
| `walking` / `Thumbwaddle` | `rename-cleanup` | 삭제 금지 | 공개명은 `Thumbwaddle`; 내부 Unity 프로젝트와 빌드 산출명은 `walking`. `.gitignore`의 scratch 제외 규칙은 제거했다. |
| `2048-blink` | `candidate` | 유지하되 낮은 우선순위 | 출시 인프라는 있으나 첫 경험 부담. |
| `best-ramyeon` | `candidate` | 유지, README 반영 | 웹 카탈로그와 prototype 디렉터리가 있으나 README 누락이었다. |
| `flying-bird` / `Wind Gull` | `candidate` | 유지 | 공개명은 `Wind Gull`; `/flying-bird` alias 유지. |
| `rainwalker` | `candidate` | 유지하되 아이디어 후보 | 아직 미니게임 규칙 단계. |
| `dopamine-swap` | `candidate` | 유지하되 재테마 필요 | 평가 문서에서 낮은 우선순위; 삭제보다는 재테마 판단 대상. |
| `drum-duel` | `archive` | 소스 보존, 웹 목록 숨김 | README/평가 문서 모두 보관 판정. 공개 route와 검증 스크립트 삭제는 별도 승인 필요. |
| `sitting` redirect | `needs-confirmation` | 유지 | `/games/sitting/`은 `/games/standing/` redirect만 수행. 공유 링크 보호 목적일 수 있다. |
| `next-tile`, `one-more` | `remove` | 웹 목록 숨김 | 실제 prototype 디렉터리 없는 placeholder. |
| Unity `Library/Temp/Logs/Builds/UserSettings` | `local-clean` | 정리 완료 | Git 미추적 생성물만 삭제. |
| `tmp/`, `mono_crash.*.json` | `local-clean` | 정리 완료 | 임시 캡처/크래시 산출물로 판단. |
| `web/mannlab-games/dist/` | `needs-confirmation` | 보류 | 배포 산출물일 수 있어 workflow 확인 전 삭제하지 않음. |

## Web Catalog Policy

- `drum-duel`은 `Archived`로 표시하고 `visible: false`로 공개 목록에서 숨긴다.
- `/drum-duel` 직접 route와 `web/mannlab-games/public/games/drum-duel/` 빌드는 삭제하지 않는다. 기존 공유 링크가 있을 수 있고, Git 추적 공개 route 삭제는 별도 승인 대상이다.
- `next-tile`, `one-more`는 prototype 없는 placeholder이므로 `visible: false`로 숨긴다.
- `walking`, `sanchaek`, `sitting`, `flying-bird`, `snow-shooter` alias는 현재 호환 링크로 유지한다.

## Cleanup Performed

다음 Git 미추적 로컬 생성물은 삭제했다.

```txt
prototypes/2048-blink/Builds
prototypes/2048-blink/Library
prototypes/2048-blink/Logs
prototypes/2048-blink/UserSettings
prototypes/_unity-ios-admob-template/Builds
prototypes/gather-and-shot/Builds
prototypes/gather-and-shot/Library
prototypes/gather-and-shot/Logs
prototypes/gather-and-shot/UserSettings
prototypes/walking/Builds
prototypes/walking/Library
prototypes/walking/Logs
prototypes/walking/Temp
prototypes/walking/UserSettings
prototypes/yacht-rush/Builds
prototypes/yacht-rush/Library
prototypes/yacht-rush/Logs
prototypes/yacht-rush/Temp
prototypes/yacht-rush/UserSettings
tmp/
mono_crash.11a8136af0.0.json
```

## Next Approval Candidates

| Candidate | Suggested action | Impact |
| --- | --- | --- |
| `web/mannlab-games/public/games/drum-duel/` | Remove public WebGL build after confirmation | Frees tracked public artifact and breaks direct embedded game URL. |
| `prototypes/drum-duel/` | Move to archive folder or delete after confirmation | Removes source reference for the rhythm experiment. |
| `scripts/verify-drum-duel-*`, `scripts/sync-drum-duel-webgl-to-site.sh` | Remove only if source is deleted | Avoids dead scripts after source removal. |
| `web/mannlab-games/public/games/sitting/` | Remove redirect only if old links are not needed | Breaks `/games/sitting/` compatibility. |
| `web/mannlab-games/dist/` | Delete if deployment workflow always rebuilds it | Frees local build output, but confirm hosting/source workflow first. |
