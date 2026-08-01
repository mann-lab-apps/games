# Mannlab Games Web

Static WebGL game shell for Mann Lab Games.

## Local

```sh
npm install
npm run dev
```

The first published game is served from `public/games/10000`.

## Analytics

GA4 측정 ID를 `VITE_GA_MEASUREMENT_ID` 환경변수로 설정하면 Google Analytics가 활성화됩니다.
로컬에서는 `.env.example`을 참고해 `.env.local`을 만들면 됩니다.

```sh
VITE_GA_MEASUREMENT_ID=G-XXXXXXXXXX npm run build
```

To refresh it from the Unity WebGL build:

```sh
../../scripts/sync-10000-webgl-to-site.sh
```
