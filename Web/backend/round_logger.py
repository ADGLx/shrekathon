import threading
import time

from state import game_rounds


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


def start_round_countdown_logger(
    game_id: str, round_id: str, started_at_ms: int, time_limit_ms: int
) -> None:
    thread = threading.Thread(
        target=_log_round_countdown,
        args=(game_id, round_id, started_at_ms, time_limit_ms),
        daemon=True,
    )
    thread.start()
