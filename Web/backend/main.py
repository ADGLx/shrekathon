import os
import random
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


load_dotenv(Path(__file__).resolve().parent / ".env")


@app.post("/create-game")
def create_game(
    x_api_password: str | None = Header(default=None),
    amount_of_players: int = Body(embed=True, ge=1),
) -> dict[str, str | int]:
    _verify_create_game_password(x_api_password)
    game_id = _generate_game_id()
    print(f"Game created: {game_id}, amount_of_players: {amount_of_players}")
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

    game_ids.remove(game_id)
    print(f"Game ended: {game_id}")
    return {"game_id": game_id, "status": "ended"}
