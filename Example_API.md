# Onion API

## Headers

Use these headers for every request:

```http
x-api-password: ultra-super-secret-password-fr
Content-Type: application/json
```

## Endpoints

### Create Game

`POST /create-game`

**Body**
```json
{
  "amount_of_players": 2
}
```

**Response**
```json
{
  "game_id": "6756",
  "status": "created",
  "amount_of_players": 2
}
```

---

### Get Game

`POST /get-game`

**Body**
```json
{
  "game_id": "9394"
}
```

**Response**
```json
{
  "game_id": "9394",
  "amount_of_players": 2,
  "connected_players": [0, 4099372752],
  "connected_count": 2,
  "all_connected": true,
  "status": "ready"
}
```

---

### Start Round

`POST /start-round`

**Body**
```json
{
  "game_id": "6756",
  "time_limit_ms": 10000
}
```

**Response**
```json
{
  "game_id": "6756",
  "round_id": "7160",
  "status": "started"
}
```

---

### Get Round

`POST /get-round`

**Body**
```json
{
  "game_id": "6756"
}
```

**Response**
```json
{
  "game_id": "6756",
  "round_id": "7160",
  "status": "finished",
  "presses_by_player": {
    "phone": [
      {
        "start_offset_ms": 3343,
        "end_offset_ms": 6541
      }
    ],
    "pc": [
      {
        "start_offset_ms": 1721,
        "end_offset_ms": 1829
      },
      {
        "start_offset_ms": 2298,
        "end_offset_ms": 2456
      },
      {
        "start_offset_ms": 2897,
        "end_offset_ms": 3040
      }
    ]
  }
}
```

---

### End Game

`POST /end-game`

**Body**
```json
{
  "game_id": "7195"
}
```

**Response**
```json
{
  "game_id": "7195",
  "status": "ended"
}
```

## Summary

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/create-game` | Create a game |
| POST | `/get-game` | Check game status |
| POST | `/start-round` | Start a round |
| POST | `/get-round` | Get round results |
| POST | `/end-game` | End a game |
