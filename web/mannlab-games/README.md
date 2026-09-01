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

## Android Beta Waitlist

`/android-beta` 페이지와 허브/게임 화면의 작은 CTA는 Android 베타 대기명단으로 연결됩니다.
신청자 관리를 줄이려면 Google Form, Tally, Airtable Form 같은 외부 폼 URL을
`VITE_ANDROID_BETA_FORM_URL`에 설정하세요. 폼 URL이 비어 있으면
`VITE_ANDROID_BETA_CONTACT_EMAIL`로 이메일 신청 링크가 열립니다.

```sh
VITE_ANDROID_BETA_FORM_URL=https://forms.gle/example npm run build
```

To refresh it from the Unity WebGL build:

```sh
../../scripts/sync-10000-webgl-to-site.sh
```
