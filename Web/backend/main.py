import os
import random
import threading
import time
from pathlib import Path

from dotenv import load_dotenv
from fastapi import Body, FastAPI, Header, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(title="Shrekathon API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


CREATE_GAME_PASSWORD_ENV = "CREATE_GAME_PASSWORD"
MAX_GAME_ID_ATTEMPTS = 10000
game_ids: set[str] = set()
game_rounds: dict[str, dict[str, str | int]] = {}
game_player_limits: dict[str, int] = {}
game_players: dict[str, set[int]] = {}


def _verify_create_game_password(x_api_password: str | None) -> None:
    expected_password = os.getenv(CREATE_GAME_PASSWORD_ENV)
    if not expected_password:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Server misconfigured: missing {CREATE_GAME_PASSWORD_ENV}",
        )

    if x_api_password != expected_password:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Unauthorized",
        )


def _generate_game_id() -> str:
    for _ in range(MAX_GAME_ID_ATTEMPTS):
        game_id = f"{random.randint(0, 9999):04d}"
        if game_id not in game_ids:
            game_ids.add(game_id)
            return game_id

    raise HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="Unable to create game id",
    )


def _generate_round_id() -> str:
    active_round_ids = {round_data["round_id"] for round_data in game_rounds.values()}
    for _ in range(MAX_GAME_ID_ATTEMPTS):
        round_id = f"{random.randint(0, 9999):04d}"
        if round_id not in active_round_ids:
            return round_id

    raise HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="Unable to create round id",
    )


def _log_round_countdown(
    game_id: str, round_id: str, started_at_ms: int, time_limit_ms: int
) -> None:
    end_at_ms = started_at_ms + time_limit_ms

    while True:
        current_round = game_rounds.get(game_id)
        if not current_round or current_round["round_id"] != round_id:
            return

        now_ms = time.time_ns() // 1_000_000
        remaining_ms = end_at_ms - now_ms
        remaining_seconds = max(0, (remaining_ms + 999) // 1000)
        print(
            "Round timer: "
            f"game_id={game_id}, round_id={round_id}, time_left_seconds={remaining_seconds}",
            flush=True,
        )

        if remaining_ms <= 0:
            print(f"Round finished: game_id={game_id}, round_id={round_id}", flush=True)
            return

        time.sleep(1)


def _start_round_countdown_logger(
    game_id: str, round_id: str, started_at_ms: int, time_limit_ms: int
) -> None:
    thread = threading.Thread(
        target=_log_round_countdown,
        args=(game_id, round_id, started_at_ms, time_limit_ms),
        daemon=True,
    )
    thread.start()


load_dotenv(Path(__file__).resolve().parent / ".env")


@app.post("/create-game")
def create_game(
    x_api_password: str | None = Header(default=None),
    amount_of_players: int = Body(embed=True, ge=1),
) -> dict[str, str | int]:
    _verify_create_game_password(x_api_password)
    game_id = _generate_game_id()
    game_player_limits[game_id] = amount_of_players
    game_players[game_id] = set()
    print(f"Game created: {game_id}, amount_of_players: {amount_of_players}", flush=True)
    return {
        "game_id": game_id,
        "status": "created",
        "amount_of_players": amount_of_players,
    }


@app.post("/end-game")
def end_game(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str]:
    _verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    game_rounds.pop(game_id, None)
    game_player_limits.pop(game_id, None)
    game_players.pop(game_id, None)
    game_ids.remove(game_id)
    print(f"Game ended: {game_id}", flush=True)
    return {"game_id": game_id, "status": "ended"}


@app.post("/join-game")
def join_game(
    game_id: str = Body(embed=True),
    player_id: int = Body(embed=True, ge=0),
) -> dict[str, str | int]:
    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    amount_of_players = game_player_limits[game_id]
    players = game_players[game_id]
    if player_id in players:
        return {
            "game_id": game_id,
            "player_id": player_id,
            "status": "already_joined",
            "joined_count": len(players),
        }

    if len(players) >= amount_of_players:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="Game is full",
        )

    players.add(player_id)
    print(
        "Player joined: "
        "game_id="
        f"{game_id}, player_id={player_id}, "
        f"joined_count={len(players)}/{amount_of_players}",
        flush=True,
    )
    return {
        "game_id": game_id,
        "player_id": player_id,
        "status": "joined",
        "joined_count": len(players),
    }


@app.post("/get-game")
def get_game(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str | int | bool | list[int]]:
    _verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    amount_of_players = game_player_limits[game_id]
    connected_players = sorted(game_players[game_id])
    connected_count = len(connected_players)

    return {
        "game_id": game_id,
        "amount_of_players": amount_of_players,
        "connected_players": connected_players,
        "connected_count": connected_count,
        "all_connected": connected_count == amount_of_players,
        "status": "ready" if connected_count == amount_of_players else "waiting",
    }


@app.post("/start-round")
def start_round(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
    time_limit_ms: int = Body(embed=True, ge=1),
) -> dict[str, str]:
    _verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    round_id = _generate_round_id()
    started_at_ms = time.time_ns() // 1_000_000
    game_rounds[game_id] = {
        "round_id": round_id,
        "time_limit_ms": time_limit_ms,
        "started_at_ms": started_at_ms,
    }
    print(
        "Round started: "
        f"game_id={game_id}, round_id={round_id}, time_limit_ms={time_limit_ms}",
        flush=True,
    )
    _start_round_countdown_logger(game_id, round_id, started_at_ms, time_limit_ms)
    return {
        "game_id": game_id,
        "round_id": round_id,
        "status": "started",
    }


@app.post("/get-round")
def get_round(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str]:
    _verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    round_data = game_rounds.get(game_id)
    if not round_data:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Round not found",
        )

    now_ms = time.time_ns() // 1_000_000
    started_at_ms = int(round_data["started_at_ms"])
    time_limit_ms = int(round_data["time_limit_ms"])
    round_status = "finished" if now_ms >= started_at_ms + time_limit_ms else "ongoing"

    return {
        "game_id": game_id,
        "round_id": str(round_data["round_id"]),
        "status": round_status,
    }
