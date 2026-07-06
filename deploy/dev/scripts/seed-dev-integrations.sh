#!/usr/bin/env bash
# Optional: copy Graph/TOTVS credentials from env vars into app_settings.
# Usage (on deploy host or via ssh):
#   GRAPH_TENANT_ID=... GRAPH_CLIENT_ID=... GRAPH_CLIENT_SECRET=... bash seed-dev-integrations.sh
set -euo pipefail

if [[ -z "${GRAPH_TENANT_ID:-}" || -z "${GRAPH_CLIENT_ID:-}" || -z "${GRAPH_CLIENT_SECRET:-}" ]]; then
  echo "Graph env vars not set — skipping real integrations seed."
  exit 0
fi

echo "Seeding Graph credentials and disabling dev adapters..."
docker exec -i lioconecta-dev-postgres psql -U lioconecta -d lioconecta -v ON_ERROR_STOP=1 \
  -v tenant="$GRAPH_TENANT_ID" \
  -v client="$GRAPH_CLIENT_ID" \
  -v secret="$GRAPH_CLIENT_SECRET" <<'SQL'
UPDATE app_settings SET "Value" = :'tenant', "UpdatedAt" = NOW() WHERE "Key" = 'graph.tenant_id';
UPDATE app_settings SET "Value" = :'client', "UpdatedAt" = NOW() WHERE "Key" = 'graph.client_id';
UPDATE app_settings SET "Value" = :'secret', "UpdatedAt" = NOW() WHERE "Key" = 'graph.client_secret';
UPDATE app_settings SET "Value" = 'false', "UpdatedAt" = NOW() WHERE "Key" = 'integrations.use_dev_adapters';
SQL
echo "Graph integration settings applied."
