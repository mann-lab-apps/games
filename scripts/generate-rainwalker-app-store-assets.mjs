import { execFileSync } from "node:child_process";
import { copyFileSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(import.meta.dirname, "..");
const outDir = resolve(repoRoot, "artifacts/app-store/rainwalker");
const htmlDir = resolve(outDir, "html");
const unityDir = resolve(repoRoot, "prototypes/rainwalker/Assets/_Project/Art/AppStore");
const uploadDirs = {
  iphone65: resolve(unityDir, "Upload/iPhone-6.5"),
  ipad13: resolve(unityDir, "Upload/iPad-13"),
};
const chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

mkdirSync(htmlDir, { recursive: true });
mkdirSync(unityDir, { recursive: true });
rmSync(resolve(unityDir, "Upload/iPhone-6.9"), { recursive: true, force: true });
for (const uploadDir of Object.values(uploadDirs)) {
  mkdirSync(uploadDir, { recursive: true });
}

const colors = {
  paper: "#f8f5eb",
  tile: "#fdf9ed",
  ink: "#27241f",
  muted: "#665f52",
  amber: "#f4b24f",
  rain: "#6da3bf",
  rainDark: "#377393",
  umbrella: "#7cc1d4",
  skin: "#f5b580",
  wet: "rgba(38, 88, 120, 0.24)",
};

function rainLines(count, seed = 1, heavy = false, rainAngle = 32) {
  let lines = "";
  const sign = rainAngle < 0 ? -1 : 1;
  const angle = Math.min(64, Math.max(20, Math.abs(rainAngle)));
  const slope = Math.tan((angle * Math.PI) / 180);
  for (let i = 0; i < count; i++) {
    const x = ((i * 173 + seed * 61) % 1180) + 40;
    const y = ((i * 269 + seed * 97) % 1860) + 360;
    const len = heavy ? 145 + ((i * 31) % 90) : 90 + ((i * 29) % 70);
    const tilt = sign * (len * slope * (0.78 + ((i * 13 + seed) % 18) / 100));
    const width = heavy ? 7 + (i % 5) : 5 + (i % 3);
    const color = i % 3 === 0 ? colors.rainDark : colors.rain;
    lines += `<line x1="${x}" y1="${y}" x2="${x + tilt}" y2="${y + len}" stroke="${color}" stroke-width="${width}" opacity="${0.36 + (i % 4) * 0.1}" stroke-linecap="round"/>`;
    if (heavy && i % 4 === 0) {
      lines += `<line x1="${x + 24}" y1="${y + 8}" x2="${x + tilt + 24}" y2="${y + len + 8}" stroke="${color}" stroke-width="${Math.max(3, width - 3)}" opacity="0.28" stroke-linecap="round"/>`;
    }
  }
  return lines;
}

function gameScene({ time = "24s", hits = 10, score = 880, angle = -32, rainAngle = angle || 32, heavy = false, wet = 2, result = false }) {
  const wetMarks = Array.from({ length: wet }, (_, i) => {
    const cx = 635 + ((i * 49) % 90) - 35;
    const cy = 1660 + i * 94;
    const r = 42 + (i % 3) * 14;
    return `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${colors.wet}"/>`;
  }).join("");

  return `<svg class="game" viewBox="0 0 1180 1820" xmlns="http://www.w3.org/2000/svg">
    <rect width="1180" height="1820" rx="32" fill="${colors.paper}" stroke="${colors.ink}" stroke-width="8"/>
    <text x="54" y="98" font-size="54" font-weight="800" fill="${colors.ink}">${time}</text>
    <text x="590" y="98" text-anchor="middle" font-size="44" font-weight="800" fill="${colors.ink}">Hits ${hits}</text>
    <text x="1128" y="98" text-anchor="end" font-size="44" font-weight="800" fill="${colors.ink}">Score ${score}</text>
    <g>${rainLines(heavy ? 32 : 18, hits + score, heavy, rainAngle)}</g>
    <g transform="translate(585 910) rotate(${angle})">
      <path d="M-300 12 C-240 -160 -102 -230 36 -222 C168 -214 273 -128 318 32 C245 -10 178 10 120 50 C54 4 -9 2 -72 48 C-145 -4 -223 -12 -300 12Z" fill="${colors.umbrella}" stroke="${colors.ink}" stroke-width="18" stroke-linejoin="round"/>
      <path d="M-250 8 C-175 -56 -100 -54 -48 40 M-48 40 C4 -44 80 -42 128 44 M128 44 C184 -35 252 -25 288 23 M12 -204 L18 340 C54 396 95 391 112 346" fill="none" stroke="${colors.ink}" stroke-width="10" stroke-linecap="round"/>
      <path d="M-212 10 L-10 55 M16 -202 L58 52 M112 48 L260 14" stroke="#4e9bb3" stroke-width="9" opacity="0.64"/>
    </g>
    <g transform="translate(590 1240)">
      <circle cx="0" cy="-168" r="52" fill="${colors.skin}" stroke="${colors.ink}" stroke-width="13"/>
      <path d="M-20 -178 L-6 -181 M18 -181 L33 -175 M-16 -145 L18 -145" stroke="${colors.ink}" stroke-width="7" stroke-linecap="round"/>
      <path d="M-42 -94 L78 -84 L54 260 L-30 266 Z" fill="${colors.amber}" stroke="${colors.ink}" stroke-width="13" stroke-linejoin="round"/>
      ${wetMarks}
      <path d="M-38 -72 L-115 -4 L-88 92 M72 -62 L147 2 L201 -48 M-16 262 L-44 468 L-110 496 M32 262 L84 466 L152 488" fill="none" stroke="${colors.ink}" stroke-width="13" stroke-linecap="round"/>
    </g>
    ${result ? `<g transform="translate(180 590)">
      <rect width="820" height="440" rx="24" fill="${colors.tile}" stroke="${colors.ink}" stroke-width="8"/>
      <text x="410" y="130" text-anchor="middle" font-size="86" font-weight="900" fill="${colors.ink}">Soaked</text>
      <text x="410" y="230" text-anchor="middle" font-size="42" font-weight="800" fill="${colors.muted}">Score ${score} · Rain hits ${hits}</text>
      <rect x="260" y="302" width="300" height="86" rx="43" fill="#fff4d8" stroke="${colors.ink}" stroke-width="7"/>
      <text x="410" y="360" text-anchor="middle" font-size="38" font-weight="900" fill="${colors.ink}">AGAIN</text>
    </g>` : ""}
  </svg>`;
}

function screenshotPage({ title, subtitle, footer, scene, width = 1242, height = 2688, layout = "phone" }) {
  const isPad = layout === "pad";
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    * { box-sizing: border-box; }
    html, body { width: ${width}px; height: ${height}px; margin: 0; overflow: hidden; }
    body {
      display: grid;
      grid-template-rows: auto 1fr auto;
      gap: ${isPad ? 42 : 50}px;
      padding: ${isPad ? "108px 92px 82px" : "96px 70px 78px"};
      background: ${colors.paper};
      color: ${colors.ink};
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
    header { display: flex; align-items: center; justify-content: space-between; font-weight: 900; }
    .brand { display: flex; align-items: center; gap: 18px; font-size: ${isPad ? 34 : 30}px; }
    .mark { width: ${isPad ? 60 : 54}px; height: ${isPad ? 60 : 54}px; display: grid; place-items: center; border: 5px solid ${colors.ink}; border-radius: 50%; background: ${colors.amber}; }
    .copy { align-self: ${isPad ? "center" : "end"}; display: grid; gap: ${isPad ? 22 : 18}px; }
    h1 { margin: 0; max-width: ${isPad ? 760 : 1060}px; font-size: ${isPad ? 94 : 96}px; line-height: 1.02; letter-spacing: 0; }
    p { margin: 0; max-width: ${isPad ? 720 : 930}px; color: ${colors.muted}; font-size: ${isPad ? 36 : 39}px; line-height: 1.28; font-weight: 760; }
    main { display: grid; align-content: center; gap: ${isPad ? 62 : 48}px; ${isPad ? "grid-template-columns: 0.86fr 1.14fr; align-items: center;" : ""} }
    .phone { width: 100%; max-width: ${isPad ? 1100 : 1400}px; justify-self: ${isPad ? "end" : "center"}; padding: ${isPad ? 24 : 28}px; border: 8px solid ${colors.ink}; border-radius: 28px; background: ${colors.tile}; box-shadow: 0 34px 0 rgba(39, 36, 31, 0.08); }
    .game { display: block; width: 100%; height: auto; }
    footer { color: ${colors.muted}; font-size: ${isPad ? 30 : 27}px; font-weight: 850; text-transform: uppercase; }
  </style>
</head>
<body>
  <header><div class="brand"><span class="mark">M</span><span>Rainwalker</span></div><span>Mannlab Games</span></header>
  <main>
    <section class="copy"><h1>${title}</h1><p>${subtitle}</p></section>
    <section class="phone">${scene}</section>
  </main>
  <footer>${footer}</footer>
</body>
</html>`;
}

function iconPage() {
  return `<!doctype html>
<html>
<head><meta charset="utf-8" /><style>html,body{width:1024px;height:1024px;margin:0;overflow:hidden;background:${colors.paper}}svg{display:block;width:1024px;height:1024px}</style></head>
<body>
<svg viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
  <rect width="1024" height="1024" fill="${colors.paper}"/>
  <path d="M92 110 C171 76 265 93 332 49 C397 7 488 84 555 54 C653 10 723 77 797 87 C875 98 946 155 940 250 L940 920 C822 965 697 932 590 973 C495 1010 399 944 305 969 C207 995 111 958 75 877 L75 178 C78 147 83 125 92 110Z" fill="${colors.tile}" stroke="${colors.ink}" stroke-width="22" stroke-linejoin="round"/>
  <g stroke="${colors.rain}" stroke-width="16" stroke-linecap="round" opacity="0.75"><path d="M227 113 L132 414"/><path d="M356 88 L257 378"/><path d="M773 120 L660 444"/><path d="M877 185 L748 489"/><path d="M816 622 L734 848"/><path d="M189 653 L110 884"/></g>
  <g transform="translate(508 504) rotate(-12)">
    <path d="M-300 10 C-242 -178 -113 -250 22 -248 C163 -244 268 -158 318 25 C244 -14 184 8 124 45 C56 -2 -3 -1 -67 42 C-143 -6 -221 -12 -300 10Z" fill="${colors.amber}" stroke="${colors.ink}" stroke-width="24" stroke-linejoin="round"/>
    <path d="M-250 4 C-181 -60 -99 -59 -44 33 M-48 32 C4 -52 81 -49 130 40 M127 39 C181 -44 245 -36 284 16 M12 -224 L20 313 C51 370 91 372 112 334" fill="none" stroke="${colors.ink}" stroke-width="14" stroke-linecap="round"/>
  </g>
  <g stroke="${colors.ink}" stroke-width="16" stroke-linecap="round" fill="none"><path d="M451 561 C424 582 425 630 454 649"/><path d="M456 649 L446 770"/><path d="M446 770 L383 876"/><path d="M446 770 L526 872"/><path d="M450 671 L377 721"/><path d="M455 669 L527 711"/></g>
  <circle cx="460" cy="532" r="45" fill="${colors.skin}" stroke="${colors.ink}" stroke-width="16"/>
  <path d="M432 530 L447 524 M475 522 L490 529" stroke="${colors.ink}" stroke-width="8" stroke-linecap="round"/>
</svg>
</body>
</html>`;
}

const pages = [
  {
    name: "iphone-6-5-screenshot-01-ready-under-the-rain",
    uploadName: "01-ready-under-the-rain",
    uploadDir: uploadDirs.iphone65,
    width: 1242,
    height: 2688,
    html: screenshotPage({
      title: "Walk through the rain",
      subtitle: "A doodle mini game about one umbrella, thirty seconds, and very rude weather.",
      footer: "Drag the umbrella angle",
      scene: gameScene({ time: "30s", hits: 0, score: 1000, angle: 0, wet: 0 }),
    }),
  },
  {
    name: "iphone-6-5-screenshot-02-first-downpour",
    uploadName: "02-first-downpour",
    uploadDir: uploadDirs.iphone65,
    width: 1242,
    height: 2688,
    html: screenshotPage({
      title: "Block what you can",
      subtitle: "Tilt the umbrella and catch the rain before it reaches the doodle walker.",
      footer: "Simple touch control",
      scene: gameScene({ time: "27s", hits: 3, score: 964, angle: -18, heavy: true, wet: 1 }),
    }),
  },
  {
    name: "iphone-6-5-screenshot-03-left-side-gust",
    uploadName: "03-left-side-gust",
    uploadDir: uploadDirs.iphone65,
    width: 1242,
    height: 2688,
    html: screenshotPage({
      title: "The wind keeps changing",
      subtitle: "Rain switches direction often, forcing quick angle changes that are hard to keep up with.",
      footer: "Random diagonal rain",
      scene: gameScene({ time: "24s", hits: 10, score: 880, angle: -58, rainAngle: -58, heavy: true, wet: 3 }),
    }),
  },
  {
    name: "ipad-13-screenshot-01-low-right-rain",
    uploadName: "01-low-right-rain",
    uploadDir: uploadDirs.ipad13,
    width: 2064,
    height: 2752,
    html: screenshotPage({
      title: "Perfect blocks are rare",
      subtitle: "Find the right umbrella angle before the next burst changes direction again.",
      footer: "Every raindrop counts",
      scene: gameScene({ time: "19s", hits: 22, score: 736, angle: 58, rainAngle: 58, heavy: true, wet: 5 }),
      width: 2064,
      height: 2752,
      layout: "pad",
    }),
  },
  {
    name: "ipad-13-screenshot-02-chaotic-middle-run",
    uploadName: "02-chaotic-middle-run",
    uploadDir: uploadDirs.ipad13,
    width: 2064,
    height: 2752,
    html: screenshotPage({
      title: "Survive the downpour",
      subtitle: "The later the run gets, the denser and faster the rain becomes.",
      footer: "Fast 30-second runs",
      scene: gameScene({ time: "11s", hits: 37, score: 556, angle: -36, rainAngle: -36, heavy: true, wet: 8 }),
      width: 2064,
      height: 2752,
      layout: "pad",
    }),
  },
  {
    name: "ipad-13-screenshot-03-soaked-result",
    uploadName: "03-soaked-result",
    uploadDir: uploadDirs.ipad13,
    width: 2064,
    height: 2752,
    html: screenshotPage({
      title: "How soaked were you?",
      subtitle: "Chase a better grade by reducing the number of raindrops that hit the body.",
      footer: "Score by rain hits",
      scene: gameScene({ time: "00s", hits: 49, score: 412, angle: 38, rainAngle: 38, heavy: true, wet: 10, result: true }),
      width: 2064,
      height: 2752,
      layout: "pad",
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

  if (page.name === "app-icon-1024") {
    copyFileSync(pngPath, resolve(unityDir, "AppIcon-1024.png"));
  } else {
    copyFileSync(pngPath, resolve(page.uploadDir, `${page.uploadName}.png`));
  }
}

console.log(`Generated Rainwalker App Store assets in ${outDir}`);
console.log(`Generated Rainwalker Unity upload assets in ${unityDir}`);
