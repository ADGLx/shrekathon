import { useEffect, useMemo, useState } from "react";

const PLAYER_ID_STORAGE_KEY = "player_id";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8001";
const LOBBY_POLL_INTERVAL_MS = 1500;

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
  const [screen, setScreen] = useState("join");
  const [joinState, setJoinState] = useState({ status: "idle", message: "" });
  const [lobbyState, setLobbyState] = useState({ status: "idle", message: "" });
  const [lobbyData, setLobbyData] = useState(null);
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

      setJoinState({
        status: "success",
        message:
          data.status === "already_joined"
            ? `Already joined as player ${playerId}.`
            : `Joined game ${gameCode} as player ${playerId}.`,
      });
      setLobbyState({ status: "loading", message: "Loading lobby..." });
      setLobbyData(null);
      setScreen("lobby");
    } catch {
      setJoinState({ status: "error", message: "Network error while joining game" });
    }
  };

  useEffect(() => {
    if (screen !== "lobby" || gameCode.length !== 4) {
      return undefined;
    }

    let cancelled = false;

    const pollLobby = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/get-game-public`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ game_id: gameCode }),
        });

        const data = await response.json();
        if (!response.ok) {
          const errorMessage = typeof data.detail === "string" ? data.detail : "Unable to load lobby";
          if (!cancelled) {
            setLobbyState({ status: "error", message: errorMessage });
          }
          return;
        }

        if (cancelled) {
          return;
        }

        setLobbyData(data);
        setLobbyState({
          status: data.all_connected ? "success" : "loading",
          message: data.all_connected ? "All players connected. Ready to start!" : "Waiting for players...",
        });
      } catch {
        if (!cancelled) {
          setLobbyState({ status: "error", message: "Network error while loading lobby" });
        }
      }
    };

    pollLobby();
    const intervalId = window.setInterval(pollLobby, LOBBY_POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [screen, gameCode]);

  const handleLeaveLobby = () => {
    setScreen("join");
    setLobbyData(null);
    setLobbyState({ status: "idle", message: "" });
  };

  const handleRegeneratePlayerId = () => {
    const nextPlayerId = createRandomPlayerId();
    window.localStorage.setItem(PLAYER_ID_STORAGE_KEY, String(nextPlayerId));
    setPlayerId(nextPlayerId);
    setJoinState({ status: "idle", message: "" });
    if (screen === "lobby") {
      setScreen("join");
      setLobbyData(null);
      setLobbyState({ status: "idle", message: "" });
    }
  };

  const connectedPlayers = lobbyData?.connected_players ?? [];

  if (screen === "lobby") {
    return (
      <main className="page">
        <section className="menu-card" aria-labelledby="lobby-title">
          <p className="tagline">Shrekathon</p>
          <h1 id="lobby-title" className="title">
            Lobby
          </h1>

          <p className="lobby-meta">Game ID: {gameCode}</p>
          <p className="lobby-meta">Player ID: {playerId}</p>
          <p className="lobby-meta">
            Connected: {lobbyData?.connected_count ?? 0} / {lobbyData?.amount_of_players ?? "-"}
          </p>

          <p className={`join-status join-status-${lobbyState.status}`} aria-live="polite">
            {lobbyState.message}
          </p>

          <div className="connected-list" aria-label="Connected players">
            {connectedPlayers.length > 0 ? (
              connectedPlayers.map((connectedPlayerId) => (
                <span key={connectedPlayerId} className="player-chip">
                  #{connectedPlayerId}
                </span>
              ))
            ) : (
              <span className="connected-empty">No players connected yet.</span>
            )}
          </div>

          <button type="button" className="join-button leave-button" onClick={handleLeaveLobby}>
            Leave Lobby
          </button>

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
