# Architecture

## Components
1. Overlay Client (Windows, C# WPF)
- Borderless transparent always-on-top window
- Message list + compact input panel
- WebSocket client for real-time chat
- Keybind to show/hide input panel

2. Chat Server (Raspberry Pi, Go)
- Single process WebSocket hub
- Room-based broadcast (initially one default room)
- Connection lifecycle and heartbeat

## Data Flow
1. Client connects to `wss://<public-domain>/ws?name=<nickname>&room=<room>`
2. Cloudflare edge terminates TLS/WSS and forwards through tunnel
3. `cloudflared` delivers request to `ws://chat-server:8080/ws`
4. Server upgrades HTTP to WebSocket and registers client
5. Client sends text payload
6. Server validates, wraps with metadata, broadcasts to room
7. All room clients render message in overlay

## Security Baseline for Public Exposure
- Use Cloudflare Tunnel so no inbound ports are exposed
- Require room key token
- Add origin checks and rate limiting
- Enforce max message size and max connections per IP

## Suggested Evolution
1. MVP: single room, in-memory broadcast
2. v1: room key, reconnect, username colors
3. v2: persistence and moderation controls
4. v3: encrypted private rooms

## Related Deployment Docs
- `docs/raspberry-pi-deploy.md`
- `docs/docker-deploy.md`
