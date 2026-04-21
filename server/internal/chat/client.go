package chat

import "github.com/gorilla/websocket"

type Client struct {
    hub  *Hub
    conn *websocket.Conn
    send chan []byte
    name string
    room string
}

func NewClient(hub *Hub, conn *websocket.Conn, name, room string) *Client {
    return &Client{
        hub:  hub,
        conn: conn,
        send: make(chan []byte, 256),
        name: name,
        room: room,
    }
}

func (c *Client) Hub() *Hub {
    return c.hub
}

func (c *Client) Conn() *websocket.Conn {
    return c.conn
}

func (c *Client) Send() chan []byte {
    return c.send
}

func (c *Client) Name() string {
    return c.name
}

func (c *Client) Room() string {
    return c.room
}
