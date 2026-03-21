All the web stuff goes here pls

## Dockerized dev setup

This repo now includes:

- `backend/`: FastAPI app
- `frontend/`: React app (Vite)
- `docker-compose.yml`: runs both services for development

### Start everything

```bash
docker compose up --build
```

### URLs

- Frontend: `http://localhost:5173`
- Backend docs: `http://localhost:8001/docs`

### API

- `POST /create-game`
- `POST /join-game`
- `POST /get-game`
- `POST /get-game-public`
- `POST /end-game`
- `POST /start-round`
- `POST /get-round`

Example request:

```bash
cp backend/.env.example backend/.env
# then set CREATE_GAME_PASSWORD in backend/.env

curl -X POST http://localhost:8001/create-game \
  -H "content-type: application/json" \
  -H "x-api-password: your-password"
  -d '{"amount_of_players":4}'
```

Example response:

```json
{"game_id":"1234","status":"created","amount_of_players":4}
```

End game example request:

```bash
curl -X POST http://localhost:8001/end-game \
  -H "content-type: application/json" \
  -H "x-api-password: your-password" \
  -d '{"game_id":"1234"}'
```

Join game example request:

```bash
curl -X POST http://localhost:8001/join-game \
  -H "content-type: application/json" \
  -d '{"game_id":"1234","player_id":0}'
```

Join game example response:

```json
{"game_id":"1234","player_id":0,"status":"joined","joined_count":1}
```

Notes:

- `player_id` must be a non-negative integer.
- Frontend generates a random per-device `player_id`, stores it in `localStorage`, and reuses it for joins.
- Joining with the same `player_id` again returns status `already_joined`.
- Joining fails if the game is already full.

Get game example request:

```bash
curl -X POST http://localhost:8001/get-game \
  -H "content-type: application/json" \
  -H "x-api-password: your-password" \
  -d '{"game_id":"1234"}'
```

Get game example response:

```json
{"game_id":"1234","amount_of_players":4,"connected_players":[0,1],"connected_count":2,"all_connected":false,"status":"waiting"}
```

`all_connected` becomes `true` and `status` becomes `ready` once every player slot is connected.

Get game public example request:

```bash
curl -X POST http://localhost:8001/get-game-public \
  -H "content-type: application/json" \
  -d '{"game_id":"1234"}'
```

`/get-game-public` returns the same payload as `/get-game` without requiring `x-api-password`.

Start round example request:

```bash
curl -X POST http://localhost:8001/start-round \
  -H "content-type: application/json" \
  -H "x-api-password: your-password" \
  -d '{"game_id":"1234","time_limit_ms":30000}'
```

Start round example response:

```json
{"game_id":"1234","round_id":"5678","status":"started"}
```

Get round example request:

```bash
curl -X POST http://localhost:8001/get-round \
  -H "content-type: application/json" \
  -H "x-api-password: your-password" \
  -d '{"game_id":"1234"}'
```

Get round example response:

```json
{"game_id":"1234","round_id":"5678","status":"ongoing"}
```

If the round timer has elapsed, status becomes `finished`.
