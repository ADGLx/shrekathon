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

## Dockerized prod setup

Production compose file:

- `docker-compose.prod.yml`: builds backend and frontend production images
- frontend is served by Nginx
- Nginx proxies API requests from `/api/*` to `onion-api:8000`

### Start prod stack

```bash
docker compose -f docker-compose.prod.yml up --build -d
```

### Stop prod stack

```bash
docker compose -f docker-compose.prod.yml down
```

### Prod URL

- App + API gateway: `http://localhost`

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
- `POST /submit-round-presses`

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
  -d '{"game_id":"1234","player_name":"shreky"}'
```

Join game example response:

```json
{"game_id":"1234","player_name":"shreky","status":"joined","joined_count":1}
```

Notes:

- `player_name` must be letters only (`A-Z`/`a-z`) with max length `8`.
- Frontend generates a random per-device username, stores it in `localStorage`, and reuses it for joins.
- Joining with the same `player_name` again returns status `already_joined`.
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

Both `/get-game` and `/get-game-public` also include current round fields:

- `round_id`: current round id or `null` when no round exists yet
- `round_status`: `none`, `ongoing`, or `finished`
- `started_at_ms`: round start timestamp in ms, or `null`
- `time_limit_ms`: configured round time limit in ms, or `null`
- `remaining_ms`: remaining time in ms (`0` when finished/no active round)

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
{"game_id":"1234","round_id":"5678","status":"ongoing","presses_by_player":null}
```

If the round timer has elapsed, status becomes `finished`.

Get round finished response example:

```json
{"game_id":"1234","round_id":"5678","status":"finished","presses_by_player":{"shreky":[{"start_offset_ms":120,"end_offset_ms":450}],"fiona":[]}}
```

Frontend behavior note:

- While in lobby, players poll `/get-game-public` and auto-transition to the round screen when `round_status` becomes `ongoing`.
- After the timer ends, players transition to a waiting screen until the next round starts.

Submit round presses example request:

```bash
curl -X POST http://localhost:8001/submit-round-presses \
  -H "content-type: application/json" \
  -d '{"game_id":"1234","round_id":"5678","player_name":"shreky","presses":[{"start_offset_ms":120,"end_offset_ms":450},{"start_offset_ms":980,"end_offset_ms":1150}]}'
```

Submit round presses example response:

```json
{"game_id":"1234","round_id":"5678","player_name":"shreky","status":"received","press_count":2}
```

`start_offset_ms` and `end_offset_ms` are relative to round start (`0..time_limit_ms`) for cleaner comparison across players.
