import random
import time

from fastapi import HTTPException, status

from state import (
    MAX_GAME_ID_ATTEMPTS,
    game_ids,
    game_player_limits,
    game_players,
    game_rounds,
    round_presses,
)


def build_game_status(game_id: str) -> dict[str, str | int | bool | list[str] | None]:
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


def generate_game_id() -> str:
    for _ in range(MAX_GAME_ID_ATTEMPTS):
        game_id = f"{random.randint(0, 9999):04d}"
        if game_id not in game_ids:
            game_ids.add(game_id)
            return game_id

    raise HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="Unable to create game id",
    )


def generate_round_id() -> str:
    active_round_ids = {round_data["round_id"] for round_data in game_rounds.values()}
    for _ in range(MAX_GAME_ID_ATTEMPTS):
        round_id = f"{random.randint(0, 9999):04d}"
        if round_id not in active_round_ids:
            return round_id

    raise HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="Unable to create round id",
    )


def compute_round_status(round_data: dict[str, str | int]) -> tuple[str, int, int]:
    now_ms = time.time_ns() // 1_000_000
    started_at_ms = int(round_data["started_at_ms"])
    time_limit_ms = int(round_data["time_limit_ms"])
    round_status = "finished" if now_ms >= started_at_ms + time_limit_ms else "ongoing"
    return round_status, started_at_ms, time_limit_ms


def normalize_presses(
    presses: list[dict[str, int]],
    time_limit_ms: int,
) -> list[dict[str, int]]:
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

    return normalized_presses


def get_finished_round_presses(game_id: str, round_id: str) -> dict[str, list[dict[str, int]]]:
    return round_presses.get(game_id, {}).get(round_id, {})
