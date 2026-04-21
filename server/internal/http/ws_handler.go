package http

import (
    "encoding/json"
    "net/http"
    "strings"
    "time"

    "overlay-chat/server/internal/chat"

    "github.com/gorilla/websocket"
)

type incomingMessage struct {
    Text string `json:"text"`
}

func NewWSHandler(hub *chat.Hub, cfg Config) http.HandlerFunc {
    upgrader := websocket.Upgrader{
        ReadBufferSize:  1024,
        WriteBufferSize: 1024,
        CheckOrigin: func(r *http.Request) bool {
            return isOriginAllowed(r.Header.Get("Origin"), cfg.AllowedOrigin)
        },
    }

    return func(w http.ResponseWriter, r *http.Request) {
        name := strings.TrimSpace(r.URL.Query().Get("name"))
        room := strings.TrimSpace(r.URL.Query().Get("room"))
        key := strings.TrimSpace(r.URL.Query().Get("key"))

        if cfg.RoomKey != "" && key != cfg.RoomKey {
            http.Error(w, "unauthorized", http.StatusUnauthorized)
            return
        }

        if name == "" {
            name = "anon"
        }
        if room == "" {
            room = "default"
        }

        conn, err := upgrader.Upgrade(w, r, nil)
        if err != nil {
            return
        }

        conn.SetReadLimit(cfg.MaxMessageBytes)

        c := chat.NewClient(hub, conn, name, room)
        hub.Register(c)

        go writePump(c)
        readPump(c)
    }
}

func isOriginAllowed(origin, allowed string) bool {
    origin = strings.TrimSpace(origin)
    allowed = strings.TrimSpace(allowed)

    // Desktop clients often omit Origin. Allow it for native app compatibility.
    if origin == "" {
        return true
    }

    if allowed == "" || allowed == "*" {
        return true
    }

    for _, item := range strings.Split(allowed, ",") {
        if strings.TrimSpace(item) == origin {
            return true
        }
    }

    return false
}

func readPump(c *chat.Client) {
    defer func() {
        c.Hub().Unregister(c)
        _ = c.Conn().Close()
    }()

    c.Conn().SetReadDeadline(time.Now().Add(60 * time.Second))
    c.Conn().SetPongHandler(func(string) error {
        c.Conn().SetReadDeadline(time.Now().Add(60 * time.Second))
        return nil
    })

    for {
        _, payload, err := c.Conn().ReadMessage()
        if err != nil {
            break
        }

        var in incomingMessage
        if err := json.Unmarshal(payload, &in); err != nil {
            continue
        }
        text := strings.TrimSpace(in.Text)
        if text == "" {
            continue
        }

        c.Hub().Broadcast(chat.Message{
            Type:      "chat",
            Room:      c.Room(),
            Name:      c.Name(),
            Text:      text,
            Timestamp: time.Now().UTC(),
        })
    }
}

func writePump(c *chat.Client) {
    ticker := time.NewTicker(54 * time.Second)
    defer func() {
        ticker.Stop()
        _ = c.Conn().Close()
    }()

    for {
        select {
        case msg, ok := <-c.Send():
            c.Conn().SetWriteDeadline(time.Now().Add(10 * time.Second))
            if !ok {
                _ = c.Conn().WriteMessage(websocket.CloseMessage, []byte{})
                return
            }
            if err := c.Conn().WriteMessage(websocket.TextMessage, msg); err != nil {
                return
            }

        case <-ticker.C:
            c.Conn().SetWriteDeadline(time.Now().Add(10 * time.Second))
            if err := c.Conn().WriteMessage(websocket.PingMessage, nil); err != nil {
                return
            }
        }
    }
}
