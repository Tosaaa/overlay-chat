package http

import (
    "os"
    "strconv"
)

type Config struct {
    Port            string
    AllowedOrigin   string
    RoomKey         string
    MaxMessageBytes int64
    MaxConnections  int
}

func LoadConfig() Config {
    return Config{
        Port:            envOr("PORT", "8080"),
        AllowedOrigin:   envOr("ALLOWED_ORIGIN", "*"),
        RoomKey:         envOr("ROOM_KEY", ""),
        MaxMessageBytes: envInt64Or("MAX_MESSAGE_BYTES", 1024),
        MaxConnections:  envIntOr("MAX_CONNECTIONS", 200),
    }
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
