import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import onionSprite from "./assets/images/My_Onion_2.png";
import bookOpenIcon from "./assets/images/Book_Open.png";
import rightHandIcon from "./assets/images/M_HandR .png";

const PLAYER_NAME_STORAGE_KEY = "player_name";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8001";
const LOBBY_POLL_INTERVAL_MS = 1500;
const USERNAME_MAX_LENGTH = 8;
const USERNAME_CHARS = "abcdefghijklmnopqrstuvwxyz";
const ROUND_BUTTON_FEEDBACK_MS = 180;
const HOLD_THRESHOLD_MS = 2000;
const ONION_SPRITE_FRAME_COUNT = 8;

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

const TopBanner = () => {
  return (
    <header className="top-banner" aria-label="Game title banner">
      <img className="top-banner-icon" src={bookOpenIcon} alt="" aria-hidden="true" />
      <div className="top-banner-copy">
        <h1 className="title title-shrek top-banner-title">Clause and Effect</h1>
        <p className="top-banner-subtitle">(Concordia Shrekathon 2026)</p>
      </div>
    </header>
  );
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
  const [roundInfo, setRoundInfo] = useState(null);
  const [roundRemainingMs, setRoundRemainingMs] = useState(0);
  const [roundButtonPressed, setRoundButtonPressed] = useState(false);
  const [roundPresses, setRoundPresses] = useState([]);
  const [activePressStartOffsetMs, setActivePressStartOffsetMs] = useState(null);
  const [onionFrame, setOnionFrame] = useState(0);
  const [submittedRoundId, setSubmittedRoundId] = useState(null);
  const [roundSubmitState, setRoundSubmitState] = useState({ status: "idle", message: "" });
  const pressFeedbackTimeoutRef = useRef(null);

  const isCodeValid = useMemo(() => gameCode.length === 4, [gameCode]);

  const handleCodeChange = (event) => {
    const digitsOnly = event.target.value.replace(/\D/g, "").slice(0, 4);
    setGameCode(digitsOnly);
  };

  const handleJoinClick = async () => {
    const normalizedPlayerName = sanitizePlayerName(nameInput);

    if (normalizedPlayerName.length === 0) {
      setNameState({ status: "error", message: "Use 1-8 letters only." });
      return;
    }

    if (normalizedPlayerName !== playerName) {
      window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, normalizedPlayerName);
      setPlayerName(normalizedPlayerName);
      setNameInput(normalizedPlayerName);
    }

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
        body: JSON.stringify({ game_id: gameCode, player_name: normalizedPlayerName }),
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
            ? `Already joined as ${normalizedPlayerName}.`
            : `Joined game ${gameCode} as ${normalizedPlayerName}.`,
      });
      setLobbyState({ status: "loading", message: "Loading lobby..." });
      setLobbyData(null);
      setRoundInfo(null);
      setRoundRemainingMs(0);
      setRoundButtonPressed(false);
      setRoundPresses([]);
      setActivePressStartOffsetMs(null);
      setOnionFrame(0);
      setSubmittedRoundId(null);
      setRoundSubmitState({ status: "idle", message: "" });
      setScreen("lobby");
    } catch {
      setJoinState({ status: "error", message: "Network error while joining game" });
    }
  };

  const handleGameEnded = useCallback((message = "Game ended by host. Returned to main menu.") => {
    setScreen("join");
    setLobbyData(null);
    setLobbyState({ status: "idle", message: "" });
    setRoundInfo(null);
    setRoundRemainingMs(0);
    setRoundButtonPressed(false);
    setRoundPresses([]);
    setActivePressStartOffsetMs(null);
    setOnionFrame(0);
    setSubmittedRoundId(null);
    setRoundSubmitState({ status: "idle", message: "" });
    setJoinState({ status: "error", message });
  }, []);

  useEffect(() => {
    if (screen === "join" || gameCode.length !== 4) {
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
          const isGameNotFound =
            response.status === 404 && typeof data.detail === "string" && data.detail === "Game not found";
          if (isGameNotFound) {
            handleGameEnded();
            return;
          }

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

        if (data.round_status === "ongoing") {
          const nextRoundId = typeof data.round_id === "string" ? data.round_id : null;
          const isNewRound = nextRoundId !== null && nextRoundId !== roundInfo?.roundId;

          if (isNewRound) {
            setRoundPresses([]);
            setActivePressStartOffsetMs(null);
            setRoundButtonPressed(false);
            setOnionFrame(0);
            setSubmittedRoundId(null);
            setRoundSubmitState({ status: "idle", message: "" });
          }

          setRoundInfo({
            roundId: nextRoundId,
            startedAtMs: typeof data.started_at_ms === "number" ? data.started_at_ms : null,
            timeLimitMs: typeof data.time_limit_ms === "number" ? data.time_limit_ms : 0,
          });
          setRoundRemainingMs(typeof data.remaining_ms === "number" ? data.remaining_ms : 0);
          setScreen("round");
          return;
        }

        if (data.round_status === "finished") {
          if (screen === "round" && activePressStartOffsetMs !== null) {
            const finishOffset = roundInfo?.timeLimitMs ?? 0;
            setRoundPresses((currentPresses) => [
              ...currentPresses,
              { start_offset_ms: activePressStartOffsetMs, end_offset_ms: finishOffset },
            ]);
            setActivePressStartOffsetMs(null);
            setRoundButtonPressed(false);
          }

          setRoundInfo({
            roundId: data.round_id,
            startedAtMs: typeof data.started_at_ms === "number" ? data.started_at_ms : null,
            timeLimitMs: typeof data.time_limit_ms === "number" ? data.time_limit_ms : 0,
          });
          setRoundRemainingMs(0);
          if (screen === "round") {
            setScreen("waiting");
          }
        }

        if (screen === "lobby") {
          setLobbyState({
            status: data.all_connected ? "success" : "loading",
            message: data.all_connected ? "All players connected. Ready to start!" : "Waiting for players...",
          });
        }
      } catch {
        if (!cancelled) {
          if (screen === "lobby") {
            setLobbyState({ status: "error", message: "Network error while loading lobby" });
          }
        }
      }
    };

    pollLobby();
    const intervalId = window.setInterval(pollLobby, LOBBY_POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [screen, gameCode, roundInfo?.roundId, roundInfo?.timeLimitMs, activePressStartOffsetMs, handleGameEnded]);

  useEffect(() => {
    if (screen !== "round") {
      return undefined;
    }

    const intervalId = window.setInterval(() => {
      setRoundRemainingMs((currentMs) => Math.max(0, currentMs - 100));
    }, 100);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [screen]);

  useEffect(() => {
    if (screen === "round" && roundRemainingMs <= 0) {
      if (activePressStartOffsetMs !== null) {
        const finishOffset = roundInfo?.timeLimitMs ?? 0;
        setRoundPresses((currentPresses) => [
          ...currentPresses,
          { start_offset_ms: activePressStartOffsetMs, end_offset_ms: finishOffset },
        ]);
        setActivePressStartOffsetMs(null);
        setRoundButtonPressed(false);
      }
      setScreen("waiting");
    }
  }, [screen, roundRemainingMs, activePressStartOffsetMs, roundInfo?.timeLimitMs]);

  useEffect(() => {
    if (screen !== "waiting" || !roundInfo?.roundId || submittedRoundId === roundInfo.roundId) {
      return;
    }

    const submitRoundPresses = async () => {
      setRoundSubmitState({ status: "loading", message: "Sending button presses..." });

      try {
        const response = await fetch(`${API_BASE_URL}/submit-round-presses`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            game_id: gameCode,
            round_id: roundInfo.roundId,
            player_name: playerName,
            presses: roundPresses,
          }),
        });

        const data = await response.json();

        if (!response.ok) {
          const isGameNotFound =
            response.status === 404 && typeof data.detail === "string" && data.detail === "Game not found";
          if (isGameNotFound) {
            handleGameEnded();
            return;
          }

          const errorMessage = typeof data.detail === "string" ? data.detail : "Unable to send button presses";
          setRoundSubmitState({ status: "error", message: errorMessage });
          return;
        }

        setSubmittedRoundId(roundInfo.roundId);
        setRoundSubmitState({
          status: "success",
          message: `Sent ${data.press_count ?? roundPresses.length} button presses.`,
        });
      } catch {
        setRoundSubmitState({ status: "error", message: "Network error while sending button presses" });
      }
    };

    submitRoundPresses();
  }, [screen, roundInfo, submittedRoundId, roundPresses, gameCode, playerName, handleGameEnded]);

  useEffect(() => {
    return () => {
      if (pressFeedbackTimeoutRef.current !== null) {
        window.clearTimeout(pressFeedbackTimeoutRef.current);
      }
    };
  }, []);

  const handleLeaveLobby = () => {
    setScreen("join");
    setLobbyData(null);
    setLobbyState({ status: "idle", message: "" });
    setRoundInfo(null);
    setRoundRemainingMs(0);
    setRoundButtonPressed(false);
    setRoundPresses([]);
    setActivePressStartOffsetMs(null);
    setOnionFrame(0);
    setSubmittedRoundId(null);
    setRoundSubmitState({ status: "idle", message: "" });
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
      setRoundInfo(null);
      setRoundRemainingMs(0);
      setRoundButtonPressed(false);
      setRoundPresses([]);
      setActivePressStartOffsetMs(null);
      setOnionFrame(0);
      setSubmittedRoundId(null);
      setRoundSubmitState({ status: "idle", message: "" });
      setJoinState({ status: "idle", message: "Name changed. Join lobby again." });
    }
  };

  const handleNameInputChange = (event) => {
    setNameInput(sanitizePlayerName(event.target.value));
    if (nameState.status !== "idle") {
      setNameState({ status: "idle", message: "" });
    }
  };

  const handleNameInputBlur = () => {
    const nextPlayerName = sanitizePlayerName(nameInput);
    if (nextPlayerName.length === 0) {
      setNameState({ status: "error", message: "Use 1-8 letters only." });
      return;
    }

    window.localStorage.setItem(PLAYER_NAME_STORAGE_KEY, nextPlayerName);
    setPlayerName(nextPlayerName);
    setNameInput(nextPlayerName);
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
      setRoundInfo(null);
      setRoundRemainingMs(0);
      setRoundButtonPressed(false);
      setRoundPresses([]);
      setActivePressStartOffsetMs(null);
      setOnionFrame(0);
      setSubmittedRoundId(null);
      setRoundSubmitState({ status: "idle", message: "" });
      setJoinState({ status: "idle", message: "Name changed. Join lobby again." });
    }
  };

  const getCurrentRoundOffsetMs = () => {
    if (!roundInfo) {
      return 0;
    }

    if (typeof roundInfo.startedAtMs === "number") {
      return Math.max(0, Math.min(roundInfo.timeLimitMs, Date.now() - roundInfo.startedAtMs));
    }

    return Math.max(0, Math.min(roundInfo.timeLimitMs, roundInfo.timeLimitMs - roundRemainingMs));
  };

  const handleRoundButtonDown = (event) => {
    if (activePressStartOffsetMs !== null) {
      return;
    }

    if (typeof event.currentTarget.setPointerCapture === "function") {
      try {
        event.currentTarget.setPointerCapture(event.pointerId);
      } catch {
        // Ignore unsupported pointer capture edge cases.
      }
    }

    const startOffsetMs = getCurrentRoundOffsetMs();
    setActivePressStartOffsetMs(startOffsetMs);
    setOnionFrame((currentFrame) => (currentFrame + 1) % ONION_SPRITE_FRAME_COUNT);
    setRoundButtonPressed(true);
  };

  const handleRoundButtonUp = (event) => {
    if (activePressStartOffsetMs === null) {
      return;
    }

    if (event && typeof event.currentTarget.releasePointerCapture === "function") {
      try {
        if (event.currentTarget.hasPointerCapture(event.pointerId)) {
          event.currentTarget.releasePointerCapture(event.pointerId);
        }
      } catch {
        // Ignore unsupported pointer capture edge cases.
      }
    }

    const endOffsetMs = getCurrentRoundOffsetMs();
    const normalizedEndOffsetMs = Math.max(activePressStartOffsetMs, endOffsetMs);

    setRoundPresses((currentPresses) => [
      ...currentPresses,
      {
        start_offset_ms: activePressStartOffsetMs,
        end_offset_ms: normalizedEndOffsetMs,
      },
    ]);
    setActivePressStartOffsetMs(null);

    if (pressFeedbackTimeoutRef.current !== null) {
      window.clearTimeout(pressFeedbackTimeoutRef.current);
    }
    pressFeedbackTimeoutRef.current = window.setTimeout(() => {
      setRoundButtonPressed(false);
      pressFeedbackTimeoutRef.current = null;
    }, ROUND_BUTTON_FEEDBACK_MS);
  };

  const connectedPlayers = lobbyData?.connected_players ?? [];
  const roundRemainingSeconds = Math.ceil(Math.max(0, roundRemainingMs) / 1000);
  const registeredTapCount = roundPresses.length;
  const registeredHoldCount = roundPresses.filter(
    (press) => press.end_offset_ms - press.start_offset_ms >= HOLD_THRESHOLD_MS,
  ).length;
  const activePressDurationMs =
    activePressStartOffsetMs === null ? 0 : Math.max(0, getCurrentRoundOffsetMs() - activePressStartOffsetMs);
  const isCurrentPressHolding = activePressStartOffsetMs !== null && activePressDurationMs >= HOLD_THRESHOLD_MS;

  if (screen === "round") {
    return (
      <main className="page">
        <TopBanner />
        <section className="menu-card round-card" aria-label="Live round">

          <p className="round-timer" aria-live="polite">
            {roundRemainingSeconds}s
          </p>

          <p className="round-tap-counter" aria-live="polite">
            Taps: {registeredTapCount}
          </p>

          <p className="round-hold-counter" aria-live="polite">
            Holds: {registeredHoldCount}
          </p>

          <p className="round-hold-indicator" aria-live="polite">
            {isCurrentPressHolding ? "Holding..." : ""}
          </p>

          <button
            type="button"
            className={`round-action-onion ${roundButtonPressed ? "round-action-onion-pressed" : ""}`}
            style={{
              backgroundImage: `url(${onionSprite})`,
              backgroundSize: `${ONION_SPRITE_FRAME_COUNT * 100}% 100%`,
              backgroundPosition: `${(onionFrame / (ONION_SPRITE_FRAME_COUNT - 1)) * 100}% center`,
            }}
            aria-label="Tap onion"
            onPointerDown={handleRoundButtonDown}
            onPointerUp={handleRoundButtonUp}
            onPointerCancel={handleRoundButtonUp}
          />
        </section>
      </main>
    );
  }

  if (screen === "waiting") {
    return (
      <main className="page">
        <TopBanner />
        <section className="menu-card waiting-card waiting-layout" aria-labelledby="waiting-title">
          <h1 id="waiting-title" className="title title-shrek waiting-title">
            Waiting
          </h1>
          <p className="waiting-message">Round finished. Waiting for next round.</p>

          <div className="round-info-grid" aria-label="Waiting details">
            <div className="round-field">
              <p className="lobby-label">Game Code</p>
              <p className="lobby-value">{gameCode}</p>
            </div>
            <div className="round-field">
              <p className="lobby-label">Username</p>
              <p className="lobby-value">{playerName}</p>
            </div>
          </div>

          <p className={`join-status join-status-${roundSubmitState.status}`}>{roundSubmitState.message}</p>
        </section>
      </main>
    );
  }

  if (screen === "lobby") {
    return (
      <main className="page">
        <TopBanner />
        <section className="menu-card lobby-layout" aria-labelledby="lobby-title">
          <h1 id="lobby-title" className="title title-shrek lobby-title">
            Lobby
          </h1>

          <div className="lobby-info-grid" aria-label="Lobby details">
            <div className="lobby-field">
              <p className="lobby-label">Game Code</p>
              <p className="lobby-value">{gameCode}</p>
            </div>
            <div className="lobby-field">
              <p className="lobby-label">Username</p>
              <p className="lobby-value">{playerName}</p>
            </div>
            <div className="lobby-field">
              <p className="lobby-label">Connected</p>
              <p className="lobby-value">
                {lobbyData?.connected_count ?? 0}/{lobbyData?.amount_of_players ?? "-"}
              </p>
            </div>
          </div>

          <p className={`join-status join-status-${lobbyState.status}`} aria-live="polite">
            {lobbyState.message}
          </p>

          <p className="lobby-label connected-heading">Connected Players</p>
          <div className="connected-list lobby-connected-list" aria-label="Connected players">
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

          <div className="join-actions">
            <button type="button" className="join-button join-game-button leave-button leave-lobby-button" onClick={handleLeaveLobby}>
              <img className="join-game-button-icon" src={rightHandIcon} alt="" aria-hidden="true" />
              <span>Leave Lobby</span>
            </button>
          </div>

          <details className="debug-menu lobby-debug-menu">
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
      <TopBanner />
      <section className="menu-card join-layout" aria-labelledby="join-game-title">
        <h1 id="join-game-title" className="title title-shrek join-title">
          Join a Game
        </h1>

        <label className="username-label" htmlFor="username-input">
          Username
        </label>
        <input
          id="username-input"
          type="text"
          autoComplete="nickname"
          className="username-input"
          value={nameInput}
          onChange={handleNameInputChange}
          onBlur={handleNameInputBlur}
          maxLength={USERNAME_MAX_LENGTH}
          placeholder="letters only"
        />
        <p className={`join-status join-status-${nameState.status}`} aria-live="polite">
          {nameState.message}
        </p>

        <label className="code-label" htmlFor="game-code">
          Game Code
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
          {isCodeValid ? "Code looks good. Ready to join." : ""}
        </p>

        <div className="join-actions">
          <button
            type="button"
            className="join-button join-game-button"
            onClick={handleJoinClick}
            disabled={!isCodeValid || joinState.status === "loading"}
          >
            <img className="join-game-button-icon" src={rightHandIcon} alt="" aria-hidden="true" />
            <span>{joinState.status === "loading" ? "Joining..." : "Join Game"}</span>
          </button>
        </div>

        <p className={`join-status join-status-${joinState.status}`} aria-live="polite">
          {joinState.message}
        </p>
      </section>
    </main>
  );
}
