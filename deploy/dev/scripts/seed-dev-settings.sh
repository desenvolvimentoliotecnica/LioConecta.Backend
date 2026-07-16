#!/usr/bin/env bash
set -euo pipefail
PORT="${LIOSNECTA_HTTP_PORT:-8090}"
HOST="${LIOSNECTA_PUBLIC_HOST:-10.0.0.79}"
ORIGIN="http://${HOST}:${PORT}"
COMPASS_PORT="${COMPASS_HTTP_PORT:-8094}"
COMPASS_ORIGIN="http://${HOST}:${COMPASS_PORT}"
CORS="[\"${ORIGIN}\",\"${COMPASS_ORIGIN}\",\"http://localhost:5173\",\"http://localhost:5174\"]"
echo "Seeding app_settings (origin=${ORIGIN}, compass=${COMPASS_ORIGIN})..."
docker exec lioconecta-dev-postgres psql -U lioconecta -d lioconecta -v ON_ERROR_STOP=1 <<SQL
UPDATE app_settings SET "Value" = 'redis:6379', "UpdatedAt" = NOW() WHERE "Key" = 'redis.connection';
UPDATE app_settings SET "Value" = '${CORS}', "UpdatedAt" = NOW() WHERE "Key" = 'cors.allowed_origins';
SQL
echo "Settings seeded."
