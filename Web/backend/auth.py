import os

from fastapi import HTTPException, status

from state import CREATE_GAME_PASSWORD_ENV


def verify_create_game_password(x_api_password: str | None) -> None:
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
