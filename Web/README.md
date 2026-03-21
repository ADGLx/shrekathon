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

Example request:

```bash
cp backend/.env.example backend/.env
# then set CREATE_GAME_PASSWORD in backend/.env

curl -X POST http://localhost:8001/create-game \
  -H "x-api-password: your-password"
```

Example response:

```json
{"game_id":"demo-123","status":"created"}
```

End game example request:

```bash
curl -X POST http://localhost:8001/end-game \
  -H "content-type: application/json" \
  -H "x-api-password: your-password" \
  -d '{"game_id":"1234"}'
```
