#!/usr/bin/env bash
set -euo pipefail
PORT="${LIOSNECTA_HTTP_PORT:-8090}"
ORIGIN="http://10.0.0.79:${PORT}"
CORS="[\"${ORIGIN}\",\"http://localhost:5173\"]"
echo "Seeding DEV app_settings (origin=${ORIGIN})..."
docker exec lioconecta-dev-postgres psql -U lioconecta -d lioconecta -v ON_ERROR_STOP=1 <<SQL
UPDATE app_settings SET "Value" = 'redis:6379', "UpdatedAt" = NOW() WHERE "Key" = 'redis.connection';
UPDATE app_settings SET "Value" = '${CORS}', "UpdatedAt" = NOW() WHERE "Key" = 'cors.allowed_origins';
SQL
echo "DEV settings seeded."