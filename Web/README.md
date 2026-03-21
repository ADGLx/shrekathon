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
