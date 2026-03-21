CREATE_GAME_PASSWORD_ENV = "CREATE_GAME_PASSWORD"
MAX_GAME_ID_ATTEMPTS = 10000

game_ids: set[str] = set()
game_rounds: dict[str, dict[str, str | int]] = {}
game_player_limits: dict[str, int] = {}
game_players: dict[str, set[str]] = {}
round_presses: dict[str, dict[str, dict[str, list[dict[str, int]]]]] = {}
