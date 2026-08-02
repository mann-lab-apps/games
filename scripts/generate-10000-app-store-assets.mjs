import { execFileSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const outDir = resolve(repoRoot, "artifacts/app-store/10000");
const htmlDir = resolve(outDir, "html");
const chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

mkdirSync(htmlDir, { recursive: true });

const colors = {
  paper: "#faf7ef",
  tile: "#fffdf7",
  ink: "#282724",
  muted: "#66615a",
  warm: "#fff4d8",
  amber: "#eea840",
  green: "#5ed481",
  red: "#e64440",
  blue: "#4f8bff",
  shadow: "rgba(40, 39, 36, 0.18)",
};

function renderBoard({ highlight = false, wrong = false, compact = false } = {}) {
  const rows = [
    "3802749106",
    "7529018643",
    "6183402579",
    "4901000042",
    "2358679015",
    "9014267380",
    "8472936150",
    "5607189423",
    "1296048307",
    "7035821649",
  ];

  const cells = rows
    .flatMap((row, y) =>
      row.split("").map((digit, x) => {
        const isTarget = y === 3 && x >= 3 && x <= 7;
        const isWrong = wrong && y === 6 && x === 2;
        return `<div class="cell${highlight && isTarget ? " target" : ""}${isWrong ? " wrong" : ""}">${digit}</div>`;
      })
    )
    .join("");

  return `<div class="board${compact ? " compact" : ""}">${cells}</div>`;
}

function screenshotPage({ title, subtitle, body, footer, width = 1284, height = 2778, scale = 1 }) {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=${width}, initial-scale=1" />
  <style>
    :root { --s: ${scale}; }
    * { box-sizing: border-box; }
    html, body { width: ${width}px; height: ${height}px; margin: 0; }
    body {
      display: grid;
      grid-template-rows: auto 1fr auto;
      overflow: hidden;
      padding: calc(126px * var(--s)) calc(84px * var(--s)) calc(96px * var(--s));
      color: ${colors.ink};
      background:
        radial-gradient(circle at 20% 10%, rgba(238, 168, 64, 0.16), transparent 23%),
        linear-gradient(${colors.paper}, ${colors.paper});
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
    .brand {
      display: inline-grid;
      grid-template-columns: calc(74px * var(--s)) auto;
      gap: calc(22px * var(--s));
      align-items: center;
      width: max-content;
      padding: calc(18px * var(--s)) calc(24px * var(--s));
      border: calc(4px * var(--s)) solid ${colors.ink};
      border-radius: calc(18px * var(--s));
      background: ${colors.tile};
      box-shadow: 0 calc(18px * var(--s)) 0 rgba(40, 39, 36, 0.05);
      font-weight: 850;
    }
    .mark {
      display: grid;
      place-items: center;
      width: calc(74px * var(--s));
      height: calc(74px * var(--s));
      border: calc(4px * var(--s)) solid ${colors.ink};
      border-radius: 50%;
      background: #f4b24f;
      font-size: calc(34px * var(--s));
      font-weight: 900;
    }
    .brand small {
      display: block;
      margin-top: calc(4px * var(--s));
      color: ${colors.muted};
      font-size: calc(22px * var(--s));
      font-weight: 750;
    }
    .hero {
      align-self: center;
      display: grid;
      gap: calc(44px * var(--s));
    }
    .copy {
      display: grid;
      gap: calc(22px * var(--s));
      max-width: calc(1040px * var(--s));
    }
    h1 {
      margin: 0;
      font-size: calc(104px * var(--s));
      line-height: 1.02;
      letter-spacing: 0;
    }
    .subtitle {
      max-width: calc(900px * var(--s));
      color: ${colors.muted};
      font-size: calc(42px * var(--s));
      line-height: 1.24;
      font-weight: 700;
    }
    .phone {
      width: min(100%, calc(1116px * var(--s)));
      padding: calc(38px * var(--s));
      border: calc(5px * var(--s)) solid ${colors.ink};
      border-radius: calc(28px * var(--s));
      background: #fdf9ed;
      box-shadow: 0 calc(38px * var(--s)) calc(80px * var(--s)) ${colors.shadow};
    }
    .game-header {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: calc(24px * var(--s));
      align-items: center;
      margin-bottom: calc(28px * var(--s));
      font-weight: 900;
    }
    .score {
      display: grid;
      gap: calc(8px * var(--s));
      color: ${colors.muted};
      font-size: calc(28px * var(--s));
      font-weight: 800;
      text-transform: uppercase;
    }
    .score strong {
      color: ${colors.ink};
      font-size: calc(48px * var(--s));
      line-height: 1;
    }
    .timer {
      width: calc(320px * var(--s));
      height: calc(38px * var(--s));
      padding: calc(5px * var(--s));
      border: calc(4px * var(--s)) solid ${colors.ink};
      border-radius: 999px;
      background: ${colors.tile};
    }
    .timer span {
      display: block;
      width: 74%;
      height: 100%;
      border-radius: 999px;
      background: ${colors.amber};
    }
    .board {
      display: grid;
      grid-template-columns: repeat(10, 1fr);
      gap: calc(10px * var(--s));
    }
    .cell {
      aspect-ratio: 1;
      display: grid;
      place-items: center;
      border: calc(4px * var(--s)) solid ${colors.ink};
      border-radius: calc(14px * var(--s));
      background: ${colors.tile};
      color: ${colors.ink};
      font-size: calc(55px * var(--s));
      font-weight: 900;
      line-height: 1;
    }
    .cell.target {
      background:
        linear-gradient(rgba(94, 212, 129, 0.56), rgba(94, 212, 129, 0.56)),
        ${colors.tile};
      box-shadow: inset 0 calc(-8px * var(--s)) 0 rgba(40, 39, 36, 0.08);
    }
    .cell.wrong {
      background:
        linear-gradient(rgba(230, 68, 64, 0.36), rgba(230, 68, 64, 0.36)),
        ${colors.tile};
    }
    .panel {
      display: grid;
      gap: calc(28px * var(--s));
      padding: calc(56px * var(--s));
      border: calc(5px * var(--s)) solid ${colors.ink};
      border-radius: calc(24px * var(--s));
      background: ${colors.tile};
    }
    .panel h2 {
      margin: 0;
      font-size: calc(86px * var(--s));
      line-height: 1;
      letter-spacing: 0;
    }
    .panel p {
      margin: 0;
      color: ${colors.muted};
      font-size: calc(40px * var(--s));
      line-height: 1.24;
      font-weight: 750;
    }
    .button {
      display: inline-flex;
      width: max-content;
      padding: calc(24px * var(--s)) calc(34px * var(--s));
      border: calc(4px * var(--s)) solid ${colors.ink};
      border-radius: 999px;
      background: ${colors.warm};
      font-size: calc(34px * var(--s));
      font-weight: 900;
    }
    .footer {
      color: ${colors.muted};
      font-size: calc(28px * var(--s));
      font-weight: 800;
      text-transform: uppercase;
    }
  </style>
</head>
<body>
  <header class="brand">
    <span class="mark">M</span>
    <span>Mannlab 10000<small>Mannlab Games</small></span>
  </header>
  <main class="hero">
    <section class="copy">
      <h1>${title}</h1>
      <div class="subtitle">${subtitle}</div>
    </section>
    <section class="phone">
      ${body}
    </section>
  </main>
  <footer class="footer">${footer}</footer>
</body>
</html>`;
}

function iconPage() {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    * { box-sizing: border-box; }
    html, body { width: 1024px; height: 1024px; margin: 0; overflow: hidden; }
    body {
      display: grid;
      place-items: center;
      background:
        radial-gradient(circle at 18% 22%, rgba(244, 178, 79, 0.32), transparent 26%),
        linear-gradient(${colors.paper}, ${colors.paper});
      color: ${colors.ink};
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
    .card {
      width: 920px;
      height: 920px;
      display: grid;
      align-content: center;
      gap: 62px;
      padding: 68px;
      background: ${colors.tile};
      box-shadow:
        inset 0 -44px 0 rgba(238, 168, 64, 0.32),
        0 0 0 18px ${colors.ink};
    }
    .target {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 20px;
    }
    .tile {
      aspect-ratio: 1;
      display: grid;
      place-items: center;
      border: 12px solid ${colors.ink};
      border-radius: 30px;
      background: ${colors.warm};
      font-size: 112px;
      line-height: 1;
      font-weight: 950;
    }
    .label {
      display: grid;
      grid-template-columns: 104px 1fr;
      gap: 28px;
      align-items: center;
      font-size: 76px;
      font-weight: 950;
      letter-spacing: 0;
    }
    .mark {
      display: grid;
      place-items: center;
      width: 104px;
      height: 104px;
      border: 12px solid ${colors.ink};
      border-radius: 50%;
      background: #f4b24f;
      font-size: 56px;
      font-weight: 950;
    }
  </style>
</head>
<body>
  <div class="card">
    <div class="target">
      <span class="tile">1</span>
      <span class="tile">0</span>
      <span class="tile">0</span>
      <span class="tile">0</span>
      <span class="tile">0</span>
    </div>
    <div class="label"><span class="mark">M</span><span>10000</span></div>
  </div>
</body>
</html>`;
}

const pages = [
  {
    name: "screenshot-01-find-hidden-10000",
    width: 1284,
    height: 2778,
    html: screenshotPage({
      title: "Find the hidden 10000",
      subtitle: "Scan the board, spot the 1-0-0-0-0 pattern, and tap fast.",
      body: `<div class="game-header"><div class="score">Stages<strong>04</strong></div><div class="timer"><span></span></div></div>${renderBoard()}`,
      footer: "Fast number puzzle",
    }),
  },
  {
    name: "screenshot-02-clear-stages",
    width: 1284,
    height: 2778,
    html: screenshotPage({
      title: "Clear stages in 60 seconds",
      subtitle: "Each board has a guaranteed answer. Every correct tap moves you forward.",
      body: `<div class="game-header"><div class="score">Time<strong>42s</strong></div><div class="timer"><span></span></div></div>${renderBoard({ highlight: true })}`,
      footer: "Simple tap controls",
    }),
  },
  {
    name: "screenshot-03-beat-best-score",
    width: 1284,
    height: 2778,
    html: screenshotPage({
      title: "Beat your best score",
      subtitle: "Quick runs make it easy to try one more board.",
      body: `<div class="panel"><h2>8 stages</h2><p>New best score. Ready for one more 60-second run?</p><span class="button">Play again</span></div>`,
      footer: "Local best score",
    }),
  },
  {
    name: "ipad-12-9-screenshot-01-find-hidden-10000",
    width: 2048,
    height: 2732,
    html: screenshotPage({
      width: 2048,
      height: 2732,
      scale: 1.34,
      title: "Find the hidden 10000",
      subtitle: "Scan the board, spot the 1-0-0-0-0 pattern, and tap fast.",
      body: `<div class="game-header"><div class="score">Stages<strong>04</strong></div><div class="timer"><span></span></div></div>${renderBoard()}`,
      footer: "Fast number puzzle",
    }),
  },
  {
    name: "ipad-12-9-screenshot-02-clear-stages",
    width: 2048,
    height: 2732,
    html: screenshotPage({
      width: 2048,
      height: 2732,
      scale: 1.34,
      title: "Clear stages in 60 seconds",
      subtitle: "Each board has a guaranteed answer. Every correct tap moves you forward.",
      body: `<div class="game-header"><div class="score">Time<strong>42s</strong></div><div class="timer"><span></span></div></div>${renderBoard({ highlight: true })}`,
      footer: "Simple tap controls",
    }),
  },
  {
    name: "ipad-12-9-screenshot-03-beat-best-score",
    width: 2048,
    height: 2732,
    html: screenshotPage({
      width: 2048,
      height: 2732,
      scale: 1.34,
      title: "Beat your best score",
      subtitle: "Quick runs make it easy to try one more board.",
      body: `<div class="panel"><h2>8 stages</h2><p>New best score. Ready for one more 60-second run?</p><span class="button">Play again</span></div>`,
      footer: "Local best score",
    }),
  },
  {
    name: "app-icon-1024",
    width: 1024,
    height: 1024,
    html: iconPage(),
  },
];

for (const page of pages) {
  const htmlPath = resolve(htmlDir, `${page.name}.html`);
  const pngPath = resolve(outDir, `${page.name}.png`);
  writeFileSync(htmlPath, page.html);
  execFileSync(chromePath, [
    "--headless=new",
    "--disable-gpu",
    "--no-sandbox",
    `--window-size=${page.width},${page.height}`,
    `--screenshot=${pngPath}`,
    `file://${htmlPath}`,
  ], { stdio: "inherit" });
}

console.log(`Generated App Store assets in ${outDir}`);
