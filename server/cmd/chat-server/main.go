package main

import (
    "log"
    "net/http"

    "overlay-chat/server/internal/chat"
    serverhttp "overlay-chat/server/internal/http"
)

func main() {
    cfg := serverhttp.LoadConfig()

    hub := chat.NewHub(cfg.MaxConnections)
    go hub.Run()

    mux := http.NewServeMux()
    mux.HandleFunc("/healthz", func(w http.ResponseWriter, _ *http.Request) {
        w.WriteHeader(http.StatusOK)
        _, _ = w.Write([]byte("ok"))
    })
    mux.HandleFunc("/ws", serverhttp.NewWSHandler(hub, cfg))

    addr := ":" + cfg.Port
    log.Printf("chat server listening on %s", addr)
    if err := http.ListenAndServe(addr, mux); err != nil {
        log.Fatalf("server failed: %v", err)
    }
}
