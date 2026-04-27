#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="$ROOT_DIR/client/OverlayChat.Client/OverlayChat.Client.csproj"
RID="${1:-win-x64}"
OUTPUT_DIR="$ROOT_DIR/artifacts/overlay-chat-client-minimal-${RID}"
SAFE_SETTINGS_PATH="$ROOT_DIR/client/OverlayChat.Client/appsettings.example.json"
USE_LOCAL_APPSETTINGS="${USE_LOCAL_APPSETTINGS:-0}"

echo "[overlay-chat] minimal publish start"
echo "  project: $PROJECT_PATH"
echo "  rid:     $RID"
echo "  output:  $OUTPUT_DIR"

dotnet publish "$PROJECT_PATH" \
  -c Release \
  -r "$RID" \
  --self-contained false \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -p:DebugType=none \
  -o "$OUTPUT_DIR"

if [[ "$USE_LOCAL_APPSETTINGS" != "1" && -f "$SAFE_SETTINGS_PATH" ]]; then
  cp "$SAFE_SETTINGS_PATH" "$OUTPUT_DIR/appsettings.json"
  echo "[overlay-chat] replaced output appsettings.json with safe template"
  echo "  template: $SAFE_SETTINGS_PATH"
elif [[ "$USE_LOCAL_APPSETTINGS" == "1" ]]; then
  echo "[overlay-chat] keeping local appsettings.json in output (USE_LOCAL_APPSETTINGS=1)"
fi

echo "[overlay-chat] done"
ls -la "$OUTPUT_DIR"
