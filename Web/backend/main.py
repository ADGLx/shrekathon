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
game_players: dict[str, set[str]] = {}
round_presses: dict[str, dict[str, dict[str, list[dict[str, int]]]]] = {}


def _build_game_status(
    game_id: str,
) -> dict[str, str | int | bool | list[str] | None]:
    amount_of_players = game_player_limits[game_id]
    connected_players = sorted(game_players[game_id])
    connected_count = len(connected_players)
    round_data = game_rounds.get(game_id)

    round_id: str | None = None
    started_at_ms: int | None = None
    time_limit_ms: int | None = None
    remaining_ms = 0
    round_status = "none"

    if round_data:
        now_ms = time.time_ns() // 1_000_000
        round_id = str(round_data["round_id"])
        started_at_ms = int(round_data["started_at_ms"])
        time_limit_ms = int(round_data["time_limit_ms"])
        end_at_ms = started_at_ms + time_limit_ms
        remaining_ms = max(0, end_at_ms - now_ms)
        round_status = "finished" if remaining_ms == 0 else "ongoing"

    return {
        "game_id": game_id,
        "amount_of_players": amount_of_players,
        "connected_players": connected_players,
        "connected_count": connected_count,
        "all_connected": connected_count == amount_of_players,
        "status": "ready" if connected_count == amount_of_players else "waiting",
        "round_id": round_id,
        "round_status": round_status,
        "started_at_ms": started_at_ms,
        "time_limit_ms": time_limit_ms,
        "remaining_ms": remaining_ms,
    }


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
    round_presses.pop(game_id, None)
    game_player_limits.pop(game_id, None)
    game_players.pop(game_id, None)
    game_ids.remove(game_id)
    print(f"Game ended: {game_id}", flush=True)
    return {"game_id": game_id, "status": "ended"}


@app.post("/join-game")
def join_game(
    game_id: str = Body(embed=True),
    player_name: str = Body(embed=True, min_length=1, max_length=8, pattern=r"^[A-Za-z]+$"),
) -> dict[str, str | int]:
    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    amount_of_players = game_player_limits[game_id]
    players = game_players[game_id]
    if player_name in players:
        return {
            "game_id": game_id,
            "player_name": player_name,
            "status": "already_joined",
            "joined_count": len(players),
        }

    if len(players) >= amount_of_players:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="Game is full",
        )

    players.add(player_name)
    print(
        "Player joined: "
        "game_id="
        f"{game_id}, player_name={player_name}, "
        f"joined_count={len(players)}/{amount_of_players}",
        flush=True,
    )
    return {
        "game_id": game_id,
        "player_name": player_name,
        "status": "joined",
        "joined_count": len(players),
    }


@app.post("/get-game")
def get_game(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str | int | bool | list[str] | None]:
    _verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    return _build_game_status(game_id)


@app.post("/get-game-public")
def get_game_public(
    game_id: str = Body(embed=True),
) -> dict[str, str | int | bool | list[str] | None]:
    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    return _build_game_status(game_id)


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
) -> dict[str, str | dict[str, list[dict[str, int]]] | None]:
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
    round_id = str(round_data["round_id"])
    presses_by_player: dict[str, list[dict[str, int]]] | None = None

    if round_status == "finished":
        presses_by_player = round_presses.get(game_id, {}).get(round_id, {})

    return {
        "game_id": game_id,
        "round_id": round_id,
        "status": round_status,
        "presses_by_player": presses_by_player,
    }


@app.post("/submit-round-presses")
def submit_round_presses(
    game_id: str = Body(embed=True),
    round_id: str = Body(embed=True),
    player_name: str = Body(embed=True, min_length=1, max_length=8, pattern=r"^[A-Za-z]+$"),
    presses: list[dict[str, int]] = Body(embed=True),
) -> dict[str, str | int]:
    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    current_round = game_rounds.get(game_id)
    if not current_round:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Round not found",
        )

    current_round_id = str(current_round["round_id"])
    if round_id != current_round_id:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="Round id mismatch",
        )

    players = game_players[game_id]
    if player_name not in players:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Player not found in game",
        )

    time_limit_ms = int(current_round["time_limit_ms"])
    normalized_presses: list[dict[str, int]] = []

    for index, press in enumerate(presses):
        start_offset_ms = press.get("start_offset_ms")
        end_offset_ms = press.get("end_offset_ms")

        if not isinstance(start_offset_ms, int) or not isinstance(end_offset_ms, int):
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=f"Invalid press at index {index}: offsets must be integers",
            )

        if start_offset_ms < 0 or end_offset_ms < 0:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=f"Invalid press at index {index}: offsets must be non-negative",
            )

        if start_offset_ms > end_offset_ms:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=f"Invalid press at index {index}: start_offset_ms must be <= end_offset_ms",
            )

        if start_offset_ms > time_limit_ms or end_offset_ms > time_limit_ms:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=f"Invalid press at index {index}: offsets exceed round time limit",
            )

        normalized_presses.append(
            {
                "start_offset_ms": start_offset_ms,
                "end_offset_ms": end_offset_ms,
            }
        )

    if game_id not in round_presses:
        round_presses[game_id] = {}
    if round_id not in round_presses[game_id]:
        round_presses[game_id][round_id] = {}

    round_presses[game_id][round_id][player_name] = normalized_presses

    print(
        "Round presses received: "
        f"game_id={game_id}, round_id={round_id}, "
        f"player_name={player_name}, press_count={len(normalized_presses)}",
        flush=True,
    )

    return {
        "game_id": game_id,
        "round_id": round_id,
        "player_name": player_name,
        "status": "received",
        "press_count": len(normalized_presses),
    }
