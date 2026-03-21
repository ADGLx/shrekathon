import { useMemo, useState } from "react";

export default function App() {
  const [gameCode, setGameCode] = useState("");

  const isCodeValid = useMemo(() => gameCode.length === 4, [gameCode]);

  const handleCodeChange = (event) => {
    const digitsOnly = event.target.value.replace(/\D/g, "").slice(0, 4);
    setGameCode(digitsOnly);
  };

  const handleJoinClick = () => {
    if (!isCodeValid) {
      return;
    }

    // UI-only flow for now.
    window.alert(`Joining game ${gameCode}`);
  };

  return (
    <main className="page">
      <section className="menu-card" aria-labelledby="join-game-title">
        <p className="tagline">Shrekathon</p>
        <h1 id="join-game-title" className="title">
          Join a Game
        </h1>

        <label className="code-label" htmlFor="game-code">
          Enter 4-digit code
        </label>
        <input
          id="game-code"
          type="text"
          inputMode="numeric"
          autoComplete="one-time-code"
          pattern="[0-9]*"
          className="code-input"
          value={gameCode}
          onChange={handleCodeChange}
          maxLength={4}
          placeholder="0000"
          aria-describedby="code-help"
        />
        <p id="code-help" className="code-help" aria-live="polite">
          {isCodeValid ? "Code looks good. Ready to join." : "Use exactly 4 numbers."}
        </p>

        <button
          type="button"
          className="join-button"
          onClick={handleJoinClick}
          disabled={!isCodeValid}
        >
          Join Game
        </button>
      </section>
    </main>
  );
}
