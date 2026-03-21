import { useMemo, useState } from "react";

const PLAYER_ID_STORAGE_KEY = "player_id";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8001";

const createRandomPlayerId = () => {
  if (window.crypto?.getRandomValues) {
    const randomBuffer = new Uint32Array(1);
    window.crypto.getRandomValues(randomBuffer);
    return randomBuffer[0];
  }

  return Math.floor(Math.random() * Number.MAX_SAFE_INTEGER);
};

const getOrCreatePlayerId = () => {
  const storedPlayerId = Number.parseInt(
    window.localStorage.getItem(PLAYER_ID_STORAGE_KEY) ?? "",
    10,
  );
  if (Number.isInteger(storedPlayerId) && storedPlayerId >= 0) {
    return storedPlayerId;
  }

  const assignedPlayerId = createRandomPlayerId();

  window.localStorage.setItem(PLAYER_ID_STORAGE_KEY, String(assignedPlayerId));
  return assignedPlayerId;
};

export default function App() {
  const [gameCode, setGameCode] = useState("");
  const [joinState, setJoinState] = useState({ status: "idle", message: "" });
  const [playerId, setPlayerId] = useState(getOrCreatePlayerId);

  const isCodeValid = useMemo(() => gameCode.length === 4, [gameCode]);

  const handleCodeChange = (event) => {
    const digitsOnly = event.target.value.replace(/\D/g, "").slice(0, 4);
    setGameCode(digitsOnly);
  };

  const handleJoinClick = async () => {
    if (!isCodeValid) {
      return;
    }

    setJoinState({ status: "loading", message: "Joining game..." });

    try {
      const response = await fetch(`${API_BASE_URL}/join-game`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ game_id: gameCode, player_id: playerId }),
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = typeof data.detail === "string" ? data.detail : "Unable to join game";
        setJoinState({ status: "error", message: errorMessage });
        return;
      }

      if (data.status === "already_joined") {
        setJoinState({ status: "success", message: `Already joined as player ${playerId}.` });
        return;
      }

      setJoinState({
        status: "success",
        message: `Joined game ${gameCode} as player ${playerId}.`,
      });
    } catch {
      setJoinState({ status: "error", message: "Network error while joining game" });
    }
  };

  const handleRegeneratePlayerId = () => {
    const nextPlayerId = createRandomPlayerId();
    window.localStorage.setItem(PLAYER_ID_STORAGE_KEY, String(nextPlayerId));
    setPlayerId(nextPlayerId);
    setJoinState({ status: "idle", message: "" });
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

        <p className="player-id">Player ID: {playerId}</p>

        <button
          type="button"
          className="join-button"
          onClick={handleJoinClick}
          disabled={!isCodeValid || joinState.status === "loading"}
        >
          {joinState.status === "loading" ? "Joining..." : "Join Game"}
        </button>

        <p className={`join-status join-status-${joinState.status}`} aria-live="polite">
          {joinState.message}
        </p>

        <details className="debug-menu">
          <summary>Debug</summary>
          <p className="debug-row">Stored player_id: {playerId}</p>
          <button type="button" className="debug-button" onClick={handleRegeneratePlayerId}>
            Regenerate Player ID
          </button>
        </details>
      </section>
    </main>
  );
}
