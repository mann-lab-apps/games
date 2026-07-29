import React from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

const games = [
  {
    title: "10000",
    status: "Live",
    description: "60초 안에 1 0 0 0 0을 찾는 숫자 퍼즐",
    href: "/games/10000/",
    active: true,
  },
  {
    title: "Next Tile",
    status: "Soon",
    description: "짧은 리듬으로 이어지는 다음 실험",
    href: "#",
    active: false,
  },
  {
    title: "One More",
    status: "Draft",
    description: "한 판만 더 하게 만드는 미니게임",
    href: "#",
    active: false,
  },
];

function App() {
  const activeGame = games.find((game) => game.active) ?? games[0];

  return (
    <main className="app-shell">
      <section className="hub-panel" aria-label="Mannlab Games">
        <a className="brand-link" href="https://mannlab.app/">
          <span className="brand-mark">M</span>
          <span>
            <strong>Mannlab Games</strong>
            <small>만랩으로 들어오는 작은 게임들</small>
          </span>
        </a>

        <nav className="game-list" aria-label="게임 선택">
          {games.map((game) => (
            <a
              key={game.title}
              className={`game-choice${game.active ? " is-active" : ""}${game.active ? "" : " is-disabled"}`}
              href={game.href}
              aria-current={game.active ? "page" : undefined}
              aria-disabled={game.active ? undefined : "true"}
              onClick={(event) => {
                if (!game.active) {
                  event.preventDefault();
                }
              }}
            >
              <span>
                <strong>{game.title}</strong>
                <small>{game.description}</small>
              </span>
              <em>{game.status}</em>
            </a>
          ))}
        </nav>

        <div className="mannlab-card">
          <span>Mannlab</span>
          <strong>게임하다가 진짜 문제도 같이 풀러 오기</strong>
          <a href="https://mannlab.app/">만랩 열기</a>
        </div>
      </section>

      <section className="play-area" aria-label={`${activeGame.title} 플레이`}>
        <section className="game-window" aria-label={activeGame.title}>
          <div className="window-bar" aria-hidden="true">
            <div className="window-controls">
              <span />
              <span />
              <span />
            </div>
            <strong>{activeGame.title}</strong>
          </div>
          <div className="game-viewport">
            <iframe
              title={activeGame.title}
              className="game-frame"
              src={activeGame.href}
              allow="fullscreen; autoplay; gamepad"
            />
          </div>
        </section>
      </section>

      <section className="promo-rail" aria-label="배너">
        <a className="banner-card primary-banner" href="https://mannlab.app/">
          <span>From Mannlab</span>
          <strong>작은 실험을 실제 제품으로</strong>
        </a>
        <a className="banner-card" href="https://in-c.mannlab.app/">
          <span>in C</span>
          <strong>생각과 실행을 정리하는 앱</strong>
        </a>
        <div className="ad-slot" aria-label="광고 슬롯">
          <span>Ad</span>
          <strong>300 x 250</strong>
        </div>
      </section>
    </main>
  );
}

createRoot(document.getElementById("root")).render(<App />);
