import React from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

function App() {
  return (
    <main className="app-shell">
      <iframe
        title="10000"
        className="game-frame"
        src="/games/10000/"
        allow="fullscreen; autoplay; gamepad"
      />
    </main>
  );
}

createRoot(document.getElementById("root")).render(<App />);
