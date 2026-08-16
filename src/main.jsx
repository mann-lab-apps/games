import React from "react";
import { createRoot } from "react-dom/client";
import { initGoogleAnalytics, trackAnalyticsEvent } from "./analytics";
import "./styles.css";

const games = [
  {
    id: "10000",
    title: "10000",
    status: "Live",
    description: "60초 안에 1 0 0 0 0을 찾는 숫자 퍼즐",
    route: "/10000",
    embedHref: "/games/10000/",
    available: true,
  },
  {
    id: "2048-crash",
    title: "2048 Crash",
    status: "Prototype",
    description: "고정 특수 블록을 같은 숫자로 깨는 슬라이드 퍼즐",
    route: "/2048-crash",
    embedHref: "/games/2048-crash/",
    available: true,
  },
  {
    id: "drum-duel",
    title: "Drum Duel",
    status: "Candidate",
    description: "4틱 리듬을 듣고 따라 치는 보관 후보 프로토타입",
    route: "/drum-duel",
    embedHref: "/games/drum-duel/",
    available: true,
  },
  {
    id: "dopamine-swap",
    title: "Dopamine Swap",
    status: "Prototype",
    description: "제한 시간 안에 카드 하나를 골라 상대 점수를 넘기는 숫자 스왑 게임",
    route: "/dopamine-swap",
    embedHref: "/games/dopamine-swap/?v=b6544809593f",
    available: true,
  },
  {
    id: "flying-bird",
    title: "Wind Gull",
    status: "Prototype",
    description: "정해진 에너지로 날개짓과 활공을 전환해 더 멀리 나는 비행 게임",
    route: "/wind-gull",
    aliases: ["/flying-bird"],
    embedHref: "/games/flying-bird/index.html?v=26a0600cdc6e",
    available: true,
  },
  {
    id: "sitting",
    title: "Sitting",
    status: "Prototype",
    description: "사람이 지나가지 않을 때 몰래 앉아 체력을 회복하는 눈치 게임",
    route: "/sitting",
    embedHref: "/games/sitting/?v=2db11ad71245",
    available: true,
  },
  {
    id: "next-tile",
    title: "Next Tile",
    status: "Soon",
    description: "짧은 리듬으로 이어지는 다음 실험",
    route: "#",
    available: false,
  },
  {
    id: "one-more",
    title: "One More",
    status: "Draft",
    description: "한 판만 더 하게 만드는 미니게임",
    route: "#",
    available: false,
  },
];

function createFeedbackUrl(game) {
  const title = `[${game.title}] 피드백: `;
  const body = [
    `게임: ${game.title}`,
    "",
    "피드백:",
    "",
    "재현 방법:",
    "",
    "기기/브라우저:",
  ].join("\n");

  const params = new URLSearchParams({
    title,
    body,
  });

  return `https://github.com/mann-lab-apps/games/issues/new?${params.toString()}`;
}

initGoogleAnalytics();

function App() {
  const pathname = window.location.pathname.replace(/\/$/, "") || "/";
  const activeGame = games.find(
    (game) => game.route === pathname || game.aliases?.includes(pathname),
  );
  const isPrivacyRoute = pathname === "/privacy";
  const isPlayRoute = Boolean(activeGame);
  const selectedGame = activeGame ?? games[0];
  const feedbackUrl = createFeedbackUrl(selectedGame);
  const routeClassName = isPlayRoute
    ? " is-play-route"
    : isPrivacyRoute
      ? " is-privacy-route"
      : " is-home-route";

  return (
    <main className={`app-shell${routeClassName}`}>
      <HubPanel activeGame={selectedGame} isPlayRoute={isPlayRoute} />

      {isPrivacyRoute ? (
        <PrivacyStage />
      ) : isPlayRoute ? (
        <GameStage game={selectedGame} feedbackUrl={feedbackUrl} />
      ) : (
        <HomeStage />
      )}
    </main>
  );
}

function HubPanel({ activeGame, isPlayRoute }) {
  return (
    <section className="hub-panel" aria-label="Mannlab Games">
      <a className="brand-link" href="/" aria-label="만랩 게임즈 홈으로 이동">
        <span className="brand-mark">M</span>
        <span>
          <strong>Mannlab Games</strong>
          <small>작게 만든 웹 게임 모음</small>
        </span>
      </a>

      {isPlayRoute ? (
        <nav className="game-list" aria-label="게임 선택">
          {games.map((game) => {
            const isActive = game.id === activeGame.id;

            return (
              <a
                key={game.title}
                className={`game-choice${isActive ? " is-active" : ""}${game.available ? "" : " is-disabled"}`}
                href={game.route}
                aria-current={isActive ? "page" : undefined}
                aria-disabled={game.available ? undefined : "true"}
                onClick={(event) => {
                  if (!game.available) {
                    event.preventDefault();
                    return;
                  }

                  trackAnalyticsEvent("select_game", {
                    game_id: game.id,
                    game_title: game.title,
                  });
                }}
              >
                <span>
                  <strong>{game.title}</strong>
                  <small>{game.description}</small>
                </span>
                <em>{game.status}</em>
              </a>
            );
          })}
        </nav>
      ) : null}

      <div className="mannlab-card">
        <span>Mannlab</span>
        <strong>만랩의 다른 작업 구경하기</strong>
        <a href="https://mannlab.app/">만랩 본진</a>
        <a className="privacy-link" href="/privacy">Privacy Policy</a>
      </div>
    </section>
  );
}

function HomeStage() {
  return (
    <section className="home-stage" aria-label="만랩 게임즈 홈">
      <div className="home-copy">
        <span>Mannlab Games</span>
        <h1>짧게 해보는 작은 게임들</h1>
      </div>

      <div className="home-games" aria-label="게임 목록">
        {games.map((game) => (
          <a
            key={game.id}
            className={`home-game-card${game.available ? "" : " is-disabled"}`}
            href={game.route}
            aria-disabled={game.available ? undefined : "true"}
            onClick={(event) => {
              if (!game.available) {
                event.preventDefault();
                return;
              }

              trackAnalyticsEvent("select_game", {
                game_id: game.id,
                game_title: game.title,
              });
            }}
          >
            <span>{game.status}</span>
            <strong>{game.title}</strong>
            <small>{game.description}</small>
          </a>
        ))}
      </div>
    </section>
  );
}

function GameStage({ game, feedbackUrl }) {
  return (
    <section className="play-area" aria-label={`${game.title} 플레이`}>
      <section className="game-window" aria-label={game.title}>
        <div className="window-bar">
          <div className="window-controls" aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
          <strong>{game.title}</strong>
          <a
            className="feedback-link"
            href={feedbackUrl}
            target="_blank"
            rel="noreferrer"
            aria-label={`${game.title} 피드백 보내기`}
            onClick={() => {
              trackAnalyticsEvent("select_feedback", {
                game_id: game.id,
                game_title: game.title,
              });
            }}
          >
            피드백
          </a>
        </div>
        <div className="game-viewport">
          <iframe
            title={game.title}
            className="game-frame"
            src={game.embedHref}
            allow="fullscreen; autoplay; gamepad"
            onLoad={() => {
              trackAnalyticsEvent("game_embed_loaded", {
                game_id: game.id,
                game_title: game.title,
              });
            }}
          />
        </div>
      </section>
    </section>
  );
}

function PrivacyStage() {
  return (
    <section className="privacy-stage" aria-label="Privacy Policy">
      <article className="privacy-document">
        <span>Mannlab Games</span>
        <h1>Privacy Policy</h1>
        <p className="privacy-updated">Last updated: August 6, 2026</p>

        <section>
          <h2>Overview</h2>
          <p>
            Mannlab Games publishes small games including Mannlab 10000,
            Dopamine Swap, and 2048 Crash. The mobile app versions do not
            require account creation.
          </p>
        </section>

        <section>
          <h2>Data Collection</h2>
          <p>
            Mannlab Games apps may store gameplay state, such as score,
            progress, or best stage, locally on your device where needed. This
            local gameplay state is not sent to Mannlab.
          </p>
          <p>
            2048 Crash and Wind Gull may use Firebase Analytics to understand
            app launches and gameplay interactions, and Firebase Crashlytics to
            diagnose crashes and stability issues. This may include device
            identifiers, product interaction data, crash data, and performance
            diagnostics.
          </p>
          <p>
            The mobile apps do not currently include third-party advertising
            SDKs, ad tracking, account systems, in-app purchases, chat,
            location access, camera access, or microphone access.
          </p>
        </section>

        <section>
          <h2>Website Analytics</h2>
          <p>
            The Mannlab Games website may use Google Analytics to understand
            aggregated visits to web pages. This website analytics setup is
            separate from the mobile app builds.
          </p>
        </section>

        <section>
          <h2>Contact</h2>
          <p>
            For privacy questions, contact Mannlab through the support page at{" "}
            <a href="https://games.mannlab.app/">https://games.mannlab.app/</a>.
          </p>
        </section>
      </article>
    </section>
  );
}

createRoot(document.getElementById("root")).render(<App />);
