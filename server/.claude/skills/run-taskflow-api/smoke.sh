#!/usr/bin/env bash
# Build, launch, and smoke-test the TaskFlow API end to end.
# Run from anywhere; paths are resolved relative to this script.
set -euo pipefail

SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "$SKILL_DIR/../../.." && pwd)"
cd "$SERVER_DIR"

PORT=5151
BASE_URL="http://localhost:$PORT"
LOG_FILE="/tmp/taskflow-api.log"

echo "==> Starting Postgres (docker compose)"
docker compose up -d

echo "==> Waiting for Postgres to accept connections"
for i in $(seq 1 30); do
  docker compose exec -T postgres pg_isready -U taskflow > /dev/null 2>&1 && break
  sleep 1
done

echo "==> Building solution"
dotnet build

echo "==> Applying EF Core migrations"
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api

echo "==> Launching API in background (log: $LOG_FILE)"
nohup dotnet run --project src/TaskFlow.Api --no-build &> "$LOG_FILE" &
API_PID=$!

echo "==> Waiting for $BASE_URL/health"
ready=0
for i in $(seq 1 30); do
  if curl -sf "$BASE_URL/health" > /dev/null; then
    ready=1
    break
  fi
  sleep 1
done

if [ "$ready" -ne 1 ]; then
  echo "!! API did not become healthy in time. Last log lines:"
  tail -30 "$LOG_FILE"
  lsof -ti:$PORT -sTCP:LISTEN | xargs -r kill
  exit 1
fi

echo "==> /health"
curl -s "$BASE_URL/health"; echo

echo "==> / (root)"
curl -s -o /dev/null -w "%{http_code}\n" "$BASE_URL/"

echo "==> /swagger/index.html"
curl -s -o /dev/null -w "%{http_code}\n" "$BASE_URL/swagger/index.html"

echo "==> 404 on unknown route"
curl -s -o /dev/null -w "%{http_code}\n" "$BASE_URL/nope"

echo "==> Stopping API (pid $API_PID / port $PORT)"
lsof -ti:$PORT -sTCP:LISTEN | xargs -r kill

echo "==> Done"
