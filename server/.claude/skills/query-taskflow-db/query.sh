#!/usr/bin/env bash
# Run a SQL query against the dockerized TaskFlow Postgres instance.
# Usage:
#   query.sh "select * from users limit 5;"
#   query.sh --csv "select * from users limit 5;"
#   echo "select 1;" | query.sh
set -euo pipefail

SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "$SKILL_DIR/../../.." && pwd)"
cd "$SERVER_DIR"

DB_USER="taskflow"
DB_NAME="taskflow"

FORMAT_FLAG=""
if [ "${1:-}" = "--csv" ]; then
  FORMAT_FLAG="--csv"
  shift
fi

if [ $# -eq 0 ]; then
  # No query argument: read SQL from stdin.
  docker compose exec -T postgres psql -U "$DB_USER" -d "$DB_NAME" $FORMAT_FLAG
else
  docker compose exec -T postgres psql -U "$DB_USER" -d "$DB_NAME" $FORMAT_FLAG -c "$1"
fi
