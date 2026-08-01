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
  const activeGame = games.find((game) => game.route === pathname);
  const isPlayRoute = Boolean(activeGame);
  const selectedGame = activeGame ?? games[0];
  const feedbackUrl = createFeedbackUrl(selectedGame);

  return (
    <main className={`app-shell${isPlayRoute ? " is-play-route" : " is-home-route"}`}>
      <HubPanel activeGame={selectedGame} isPlayRoute={isPlayRoute} />

      {isPlayRoute ? (
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

createRoot(document.getElementById("root")).render(<App />);
