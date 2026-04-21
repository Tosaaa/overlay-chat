package http

import (
    "os"
    "strconv"
    "strings"
)

type Config struct {
    Port            string
    AllowedOrigin   string
    RoomKeys        map[string]string
    MaxMessageBytes int64
    MaxConnections  int
}

func LoadConfig() Config {
    return Config{
        Port:            envOr("PORT", "8080"),
        AllowedOrigin:   envOr("ALLOWED_ORIGIN", "*"),
        RoomKeys:        parseRoomKeys(envOr("ROOM_KEYS", "")),
        MaxMessageBytes: envInt64Or("MAX_MESSAGE_BYTES", 4096),
        MaxConnections:  envIntOr("MAX_CONNECTIONS", 100),
    }
}

func parseRoomKeys(s string) map[string]string {
    keys := make(map[string]string)
    if s == "" {
        return keys
    }

    pairs := strings.Split(s, ",")
    for _, pair := range pairs {
        parts := strings.SplitN(strings.TrimSpace(pair), ":", 2)
        if len(parts) == 2 {
            keys[parts[0]] = parts[1]
        }
    }
    return keys
}

func envOr(k, d string) string {
    v := os.Getenv(k)
    if v == "" {
        return d
    }
    return v
}

func envIntOr(k string, d int) int {
    v := os.Getenv(k)
    if v == "" {
        return d
    }
    n, err := strconv.Atoi(v)
    if err != nil {
        return d
    }
    return n
}

func envInt64Or(k string, d int64) int64 {
    v := os.Getenv(k)
    if v == "" {
        return d
    }
    n, err := strconv.ParseInt(v, 10, 64)
    if err != nil {
        return d
    }
    return n
}
