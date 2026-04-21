# Raspberry Pi Deploy

## 1. Build and run
- On Pi:
  - `cd ~/overlay-chat/server`
  - `go mod tidy`
  - `go build -o chat-server ./cmd/chat-server`
  - `./chat-server`

## 2. Networking model (Cloudflare Tunnel)
- Keep `8080` private in LAN.
- Do not expose router port forwarding for `8080/80/443`.
- Public traffic should enter through Cloudflare Tunnel.

## 3. Optional systemd
- Copy `server/systemd/overlay-chat.service` to `/etc/systemd/system/overlay-chat.service`
- Enable service:
  - `sudo systemctl daemon-reload`
  - `sudo systemctl enable overlay-chat`
  - `sudo systemctl start overlay-chat`

## 4. Recommended before public access
- Use Cloudflare Tunnel for public WSS endpoint
- Restrict `ALLOWED_ORIGIN`
- Add room key or token auth
- Add rate limit per IP

## 5. Cloudflare tunnel setup
- Follow `docs/docker-deploy.md` for compose-based `cloudflared` run.
- Tunnel route should point to `http://chat-server:8080`.
- Client URL should be `wss://<your-domain>/ws`.

## 6. Docker deploy
- Follow `docs/docker-deploy.md`
