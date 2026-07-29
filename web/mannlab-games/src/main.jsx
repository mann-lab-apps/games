import React from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

function App() {
  return (
    <main className="app-shell">
      <section className="game-window" aria-label="10000">
        <div className="window-bar" aria-hidden="true">
          <div className="window-controls">
            <span />
            <span />
            <span />
          </div>
          <strong>10000</strong>
        </div>
        <div className="game-viewport">
          <iframe
            title="10000"
            className="game-frame"
            src="/games/10000/"
            allow="fullscreen; autoplay; gamepad"
          />
        </div>
      </section>
    </main>
  );
}

createRoot(document.getElementById("root")).render(<App />);
