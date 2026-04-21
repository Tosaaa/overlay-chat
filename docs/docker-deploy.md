# Docker Deploy (Raspberry Pi)

## Goal
Run the chat server on Raspberry Pi with Docker Compose using Cloudflare Tunnel.

Public WSS (`wss://.../ws`) is terminated at Cloudflare edge. Your Pi only serves internal WS (`ws://chat-server:8080/ws`).

## Files Used
- `docker-compose.yaml`
- `server/Dockerfile`
- `server/.env` (local secret config, ignored by git)
- `server/.env.example` (template)

## 1. Prepare Environment
1. Copy env template:
   - `cp server/.env.example server/.env`
2. Edit `server/.env` values:
   - `PORT=8080`
   - `ROOM_KEY=<strong-secret>`
   - `ALLOWED_ORIGIN=https://chat.example.com`
   - `MAX_MESSAGE_BYTES=1024`
   - `MAX_CONNECTIONS=200`
   - `CF_TUNNEL_TOKEN=<cloudflare-tunnel-token>`

`ALLOWED_ORIGIN` must match your public client origin (for example `https://chat.example.com`).

## 2. Start Stack
- `docker compose up -d`

This starts the `chat-server` container, which listens on host port `8080`.

## 3. Cloudflare Tunnel Setup (Host Level)
If you are already running `cloudflared` as a service on your Raspberry Pi:
1. Update your tunnel configuration (usually `/etc/cloudflared/config.yml` or via Cloudflare Dashboard).
2. Point the public hostname (e.g., `chat.example.com`) to `http://localhost:8080`.
3. Restart `cloudflared` if necessary.

If you prefer running `cloudflared` inside Docker, you can restore the `cloudflared` service in `docker-compose.yaml` using your `CF_TUNNEL_TOKEN`.

## 4. Verify Server Health
- `docker compose ps`
- `docker compose logs -f chat-server`
- `curl http://127.0.0.1:8080/healthz`

## 4. Client Setting
Set endpoint in `client/OverlayChat.Client/appsettings.json`:
- `Connection.ServerUrl = "wss://chat.example.com/ws"`
- `Connection.RoomKey = "<same-room-key-as-server>"`

Important: client still uses `wss://` publicly. The server does not need TLS/WSS listener.

## 5. Update / Rebuild
- `docker compose build --no-cache chat-server && docker compose up -d chat-server`

## 6. Stop
- `docker compose down`

## Troubleshooting
- If container starts but clients cannot join:
  - verify `ROOM_KEY` matches client `Connection.RoomKey`
- If cloudflared fails to connect:
  - verify `CF_TUNNEL_TOKEN` is valid and tunnel routes to `http://chat-server:8080`
- If WebSocket handshake fails from client:
  - verify `ALLOWED_ORIGIN` includes `https://<your-domain>`
  - verify client URL is exactly `wss://<your-domain>/ws`
