import time

from fastapi import APIRouter, Body, Header, HTTPException, status

from auth import verify_create_game_password
from round_logger import start_round_countdown_logger
from services import (
    build_game_status,
    compute_round_status,
    generate_game_id,
    generate_round_id,
    get_finished_round_presses,
    normalize_presses,
)
from state import game_ids, game_player_limits, game_players, game_rounds, round_presses

router = APIRouter()


@router.post("/create-game")
def create_game(
    x_api_password: str | None = Header(default=None),
    amount_of_players: int = Body(embed=True, ge=1),
) -> dict[str, str | int]:
    verify_create_game_password(x_api_password)
    game_id = generate_game_id()
    game_player_limits[game_id] = amount_of_players
    game_players[game_id] = set()
    print(f"Game created: {game_id}, amount_of_players: {amount_of_players}", flush=True)
    return {
        "game_id": game_id,
        "status": "created",
        "amount_of_players": amount_of_players,
    }


@router.post("/end-game")
def end_game(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str]:
    verify_create_game_password(x_api_password)

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


@router.post("/join-game")
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


@router.post("/get-game")
def get_game(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str | int | bool | list[str] | None]:
    verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    return build_game_status(game_id)


@router.post("/get-game-public")
def get_game_public(
    game_id: str = Body(embed=True),
) -> dict[str, str | int | bool | list[str] | None]:
    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    return build_game_status(game_id)


@router.post("/start-round")
def start_round(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
    time_limit_ms: int = Body(embed=True, ge=1),
) -> dict[str, str]:
    verify_create_game_password(x_api_password)

    if game_id not in game_ids:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Game not found",
        )

    round_id = generate_round_id()
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
    start_round_countdown_logger(game_id, round_id, started_at_ms, time_limit_ms)
    return {
        "game_id": game_id,
        "round_id": round_id,
        "status": "started",
    }


@router.post("/get-round")
def get_round(
    x_api_password: str | None = Header(default=None),
    game_id: str = Body(embed=True),
) -> dict[str, str | dict[str, list[dict[str, int]]] | None]:
    verify_create_game_password(x_api_password)

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

    round_status, *_ = compute_round_status(round_data)
    round_id = str(round_data["round_id"])
    presses_by_player: dict[str, list[dict[str, int]]] | None = None

    if round_status == "finished":
        presses_by_player = get_finished_round_presses(game_id, round_id)

    return {
        "game_id": game_id,
        "round_id": round_id,
        "status": round_status,
        "presses_by_player": presses_by_player,
    }


@router.post("/submit-round-presses")
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
    normalized_presses = normalize_presses(presses, time_limit_ms)

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
