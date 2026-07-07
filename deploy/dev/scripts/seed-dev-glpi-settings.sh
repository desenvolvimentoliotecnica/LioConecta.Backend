#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "Seeding GLPI and helpdesk settings from local dev export..."
docker exec -i lioconecta-dev-postgres psql -U lioconecta -d lioconecta -v ON_ERROR_STOP=1 < "$SCRIPT_DIR/seed-dev-glpi-settings.sql"
echo "GLPI settings migrated."
