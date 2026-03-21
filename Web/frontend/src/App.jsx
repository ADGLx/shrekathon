import { useEffect, useMemo, useState } from "react";

const PLAYER_NAME_STORAGE_KEY = "player_name";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8001";
const LOBBY_POLL_INTERVAL_MS = 1500;
const USERNAME_MAX_LENGTH = 8;
const USERNAME_CHARS = "abcdefghijklmnopqrstuvwxyz";

const createRandomPlayerName = () => {
  const nameLength = 6;

  if (window.crypto?.getRandomValues) {
    const randomBuffer = new Uint32Array(nameLength);
    window.crypto.getRandomValues(randomBuffer);

    return Array.from(randomBuffer, (value) => USERNAME_CHARS[value % USERNAME_CHARS.length]).join("");
  }

  return Array.from({ length: nameLength }, () => {
    const index = Math.floor(Math.random() * USERNAME_CHARS.length);
    return USERNAME_CHARS[index];
  }).join("");
};

const sanitizePlayerName = (value) => value.replace(/[^A-Za-z]/g, "").slice(0, USERNAME_MAX_LENGTH);

const getOrCreatePlayerName = () => {
  const storedPlayerName = sanitizePlayerName(window.localStorage.getItem(PLAYER_NAME_STORAGE_KEY) ?? "");
  if (storedPlayerName.length > 0) {
    if (storedPlayerName !== window.localStorage.getItem(PLAYER_NAME_STORAGE_KEY)) {
      window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, storedPlayerName);
    }
    return storedPlayerName;
  }

  const assignedPlayerName = createRandomPlayerName();

  window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, assignedPlayerName);
  return assignedPlayerName;
};

export default function App() {
  const [gameCode, setGameCode] = useState("");
  const [screen, setScreen] = useState("join");
  const [joinState, setJoinState] = useState({ status: "idle", message: "" });
  const [lobbyState, setLobbyState] = useState({ status: "idle", message: "" });
  const [lobbyData, setLobbyData] = useState(null);
  const [playerName, setPlayerName] = useState(getOrCreatePlayerName);
  const [nameInput, setNameInput] = useState(playerName);
  const [nameState, setNameState] = useState({ status: "idle", message: "" });

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
        body: JSON.stringify({ game_id: gameCode, player_name: playerName }),
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
            ? `Already joined as ${playerName}.`
            : `Joined game ${gameCode} as ${playerName}.`,
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
    const nextPlayerName = createRandomPlayerName();
    window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, nextPlayerName);
    setPlayerName(nextPlayerName);
    setNameInput(nextPlayerName);
    setNameState({ status: "success", message: "Random name generated." });
    setJoinState({ status: "idle", message: "" });
    if (screen === "lobby") {
      setScreen("join");
      setLobbyData(null);
      setLobbyState({ status: "idle", message: "" });
      setJoinState({ status: "idle", message: "Name changed. Join lobby again." });
    }
  };

  const handleNameInputChange = (event) => {
    setNameInput(sanitizePlayerName(event.target.value));
    setNameState({ status: "idle", message: "" });
  };

  const handleSaveName = () => {
    const nextPlayerName = sanitizePlayerName(nameInput);
    if (nextPlayerName.length === 0) {
      setNameState({ status: "error", message: "Use 1-8 letters only." });
      return;
    }

    window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, nextPlayerName);
    setPlayerName(nextPlayerName);
    setNameInput(nextPlayerName);
    setNameState({ status: "success", message: "Name saved." });

    if (screen === "lobby") {
      setScreen("join");
      setLobbyData(null);
      setLobbyState({ status: "idle", message: "" });
      setJoinState({ status: "idle", message: "Name changed. Join lobby again." });
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
          <p className="lobby-meta">Username: {playerName}</p>
          <p className="lobby-meta">
            Connected: {lobbyData?.connected_count ?? 0} / {lobbyData?.amount_of_players ?? "-"}
          </p>

          <p className={`join-status join-status-${lobbyState.status}`} aria-live="polite">
            {lobbyState.message}
          </p>

          <div className="connected-list" aria-label="Connected players">
            {connectedPlayers.length > 0 ? (
              connectedPlayers.map((connectedPlayerName) => (
                <span key={connectedPlayerName} className="player-chip">
                  {connectedPlayerName}
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
            <summary>Change Name</summary>
            <p className="debug-row">Current: {playerName}</p>
            <input
              type="text"
              className="debug-input"
              value={nameInput}
              onChange={handleNameInputChange}
              maxLength={USERNAME_MAX_LENGTH}
              placeholder="letters only"
            />
            <div className="debug-actions">
              <button type="button" className="debug-button" onClick={handleSaveName}>
                Save Name
              </button>
              <button type="button" className="debug-button" onClick={handleRegeneratePlayerId}>
                Random Name
              </button>
            </div>
            <p className={`join-status join-status-${nameState.status}`}>{nameState.message}</p>
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

        <p className="player-id">Username: {playerName}</p>

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
          <summary>Change Name</summary>
          <p className="debug-row">Current: {playerName}</p>
          <input
            type="text"
            className="debug-input"
            value={nameInput}
            onChange={handleNameInputChange}
            maxLength={USERNAME_MAX_LENGTH}
            placeholder="letters only"
          />
          <div className="debug-actions">
            <button type="button" className="debug-button" onClick={handleSaveName}>
              Save Name
            </button>
            <button type="button" className="debug-button" onClick={handleRegeneratePlayerId}>
              Random Name
            </button>
          </div>
          <p className={`join-status join-status-${nameState.status}`}>{nameState.message}</p>
        </details>
      </section>
    </main>
  );
}
