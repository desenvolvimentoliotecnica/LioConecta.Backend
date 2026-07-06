# LioConecta DEV Deploy Evidence
Date: 2026-07-06 16:42:29
URL: http://10.0.0.79:8091/

## Containers
- lioconecta-dev-nginx: Up 3 minutes - lioconecta-dev-workers: Up 5 minutes - lioconecta-dev-api: Up 5 minutes (healthy) - lioconecta-dev-postgres: Up 8 minutes (healthy) - lioconecta-dev-redis: Up 8 minutes (healthy)

## Health Checks (from server)
- GET /health: Healthy
- GET /health/ready: Healthy
- GET / (frontend): HTTP 200
- GET /acesso: LioConecta login page renders
- POST /api/v1/auth/login: API responds (401 invalid creds for test user)

## Stack
- PostgreSQL 16 (lioconecta-dev-postgres)
- Redis 7 (lioconecta-dev-redis)
- API .NET 8 (lioconecta-dev-api)
- Workers (lioconecta-dev-workers)
- Nginx SPA + reverse proxy (lioconecta-dev-nginx :8091)

## Deploy command
powershell -File LioConecta.Backend/deploy/dev/deploy-dev.ps1
