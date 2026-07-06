#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL_FILE="${SCRIPT_DIR}/fix-audit-events.sql"
echo "Applying DB fixes (audit_events schema)..."
docker exec -i lioconecta-dev-postgres psql -U lioconecta -d lioconecta -v ON_ERROR_STOP=1 < "$SQL_FILE"
echo "DB fixes applied."