package chat

import (
    "encoding/json"
    "log"
    "time"
)

type Message struct {
    Type      string    `json:"type"`
    Room      string    `json:"room"`
    Name      string    `json:"name"`
    Text      string    `json:"text"`
    Timestamp time.Time `json:"timestamp"`
}

type Hub struct {
    rooms          map[string]map[*Client]struct{}
    register       chan *Client
    unregister     chan *Client
    broadcast      chan Message
    maxConnections int
}

func NewHub(maxConnections int) *Hub {
    return &Hub{
        rooms:          make(map[string]map[*Client]struct{}),
        register:       make(chan *Client),
        unregister:     make(chan *Client),
        broadcast:      make(chan Message, 256),
        maxConnections: maxConnections,
    }
}

func (h *Hub) Run() {
    for {
        select {
        case c := <-h.register:
            if h.connectionCount() >= h.maxConnections {
                close(c.send)
                _ = c.conn.Close()
                continue
            }
            if _, ok := h.rooms[c.room]; !ok {
                h.rooms[c.room] = make(map[*Client]struct{})
            }
            h.rooms[c.room][c] = struct{}{}
            log.Printf("client joined room=%s name=%s", c.room, c.name)

        case c := <-h.unregister:
            roomClients, ok := h.rooms[c.room]
            if !ok {
                continue
            }
            if _, exists := roomClients[c]; exists {
                delete(roomClients, c)
                close(c.send)
            }
            if len(roomClients) == 0 {
                delete(h.rooms, c.room)
            }
            log.Printf("client left room=%s name=%s", c.room, c.name)

        case msg := <-h.broadcast:
            roomClients, ok := h.rooms[msg.Room]
            if !ok {
                continue
            }
            payload, err := json.Marshal(msg)
            if err != nil {
                continue
            }
            for c := range roomClients {
                select {
                case c.send <- payload:
                default:
                    close(c.send)
                    delete(roomClients, c)
                }
            }
        }
    }
}

func (h *Hub) Register(c *Client) {
    h.register <- c
}

func (h *Hub) Unregister(c *Client) {
    h.unregister <- c
}

func (h *Hub) Broadcast(msg Message) {
    h.broadcast <- msg
}

func (h *Hub) connectionCount() int {
    total := 0
    for _, room := range h.rooms {
        total += len(room)
    }
    return total
}
