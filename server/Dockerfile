FROM --platform=$BUILDPLATFORM golang:1.22-alpine AS build
WORKDIR /app
ARG TARGETOS
ARG TARGETARCH
COPY go.mod go.sum* ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=${TARGETOS:-linux} GOARCH=${TARGETARCH:-arm64} go build -o /out/chat-server ./cmd/chat-server

FROM alpine:3.20
WORKDIR /srv
COPY --from=build /out/chat-server /usr/local/bin/chat-server
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/chat-server"]
