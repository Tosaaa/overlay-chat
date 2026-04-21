# Overlay Chat for Slay the Spire 2

A lightweight transparent chat overlay client for Windows + a Raspberry Pi-hosted Go WebSocket server.

## Stack
- Client: C# WPF (`.NET 8`, transparent always-on-top overlay)
- Server: Go (`gorilla/websocket`)
- Transport: WebSocket

## Repository Layout
- `client/OverlayChat.Client` - Windows overlay app
- `server` - Go WebSocket chat server
- `docs` - design and deployment notes

## Quick Start
1. Start server:
   - `cd server`
   - set environment values (`ROOM_KEY`, optional `ALLOWED_ORIGIN`, `PORT`)
   - `go mod tidy`
   - `go run ./cmd/chat-server`
2. Edit client settings in `client/OverlayChat.Client/appsettings.json`:
   - `Connection.ServerUrl`
   - `Connection.Name`
   - `Connection.Room`
   - `Connection.RoomKey` (must match server `ROOM_KEY`)
3. Run client on Windows:
   - VS Code: Run and Debug -> `OverlayChat.Client (WPF)`
   - or terminal: `dotnet run --project ./client/OverlayChat.Client/OverlayChat.Client.csproj`

## Docker (Raspberry Pi)
1. Prepare server env:
   - `cp server/.env.example server/.env`
   - edit `server/.env` (`ROOM_KEY`, `ALLOWED_ORIGIN`, `CF_TUNNEL_TOKEN`, etc.)
2. Start full stack (chat server + cloudflared tunnel):
   - `docker compose up -d`
3. Set client URL to your Cloudflare hostname:
   - `wss://<your-domain>/ws`

## Overlay Controls
- Drag window: drag top bar area
- Resize window: drag edges/corners
- Close app: `X` button
- Open settings: `S` button

## Chat/Input Controls
- Send message: `Enter` while input box is focused
- Click-through toggle: global hotkey (`Overlay.ToggleHotkey`, default `Ctrl+Shift+O`)
- Optional global input focus: enable `Overlay.FocusInputWithEnter`
  - first `Enter`: move focus to input
  - next `Enter`: send message

## Appearance Settings (in-app)
The settings panel supports:
- Background opacity (text remains fully readable)
- Chat font size
- Chat text color via RGB sliders

Settings are saved back to `client/OverlayChat.Client/appsettings.json`.

## Overlay Hotkey
- Configure in `client/OverlayChat.Client/appsettings.json`:
   - `Overlay.ToggleHotkey` (default: `Ctrl+Shift+O`)
   - `Overlay.StartClickThrough` (default: `false`)
   - `Overlay.FocusInputWithEnter` (default: `false`)
- Press the hotkey to toggle click-through mode on/off.

## Raspberry Pi Deployment Notes
- Build server binary for ARM Linux or run natively on Pi.
- Use **Cloudflare Tunnel** to expose the server securely (see `docs/docker-deploy.md`).
- Cloudflare terminates TLS/WSS; your Pi only needs to serve plain WS internally.
- Add a room key and simple auth before opening to the internet.

## Cloudflare Tunnel
- Recommended flow: `chat-server (ws://chat-server:8080)` + `cloudflared`
- Cloudflare provides public HTTPS/WSS endpoint, then forwards to internal WS via tunnel.
- Setup guide: `docs/docker-deploy.md`

## Deployment Docs
- Raspberry Pi (native): `docs/raspberry-pi-deploy.md`
- Docker (Pi): `docs/docker-deploy.md`

## Quick Troubleshooting
- If messages show with wrong name/text, rebuild client and rerun.
- If connection fails, verify `ROOM_KEY` on server and `Connection.RoomKey` on client are identical.
- If global Enter option does not enable, another app may already capture Enter as a global key.
