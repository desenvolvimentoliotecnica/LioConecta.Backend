# Deploy on-premises — LioConecta Backend

Runbook para instalação do backend LioConecta em datacenter Lio Tecnica.

## Componentes

| Componente | Função | Porta sugerida |
|------------|--------|----------------|
| Reverse proxy (IIS/Nginx) | TLS, roteamento | 443 |
| LioConecta.Api | API REST + SignalR | 8080 (interno) |
| LioConecta.Workers | Sync TOTVS/Graph | — |
| PostgreSQL 16 | Banco principal | 5432 |
| Redis 7 | Cache + SignalR backplane | 6379 |

## Pré-requisitos

- Windows Server 2019+ ou Linux com .NET 8 Runtime
- PostgreSQL 16 acessível pela API e Workers
- Redis 7 para múltiplas instâncias da API
- App Registration Azure AD (SPA + API)
- Conectividade para TOTVS, GLPI e Microsoft Graph

## Azure AD

1. Registrar **LioConecta API** (Web API) com scope `access_as_user`
2. Registrar **LioConecta SPA** (Single-page application) com redirect URIs do portal
3. Configurar permissões Graph: `User.Read`, `Calendars.Read`, `Tasks.Read`, `Sites.Read.All`
4. Preencher `appsettings.Production.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-client-id>",
    "Audience": "api://lioconecta"
  }
}
```

## Publicação

```bash
dotnet publish src/LioConecta.Api -c Release -o ./publish/api
dotnet publish src/LioConecta.Workers -c Release -o ./publish/workers
```

### IIS (Windows)

- Application Pool: **No Managed Code**, identidade de serviço
- Instalar [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
- WebSocket habilitado (SignalR)
- Sticky sessions **ou** Redis backplane obrigatório

### Nginx (Linux)

```nginx
location / {
    proxy_pass http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

## Migrations

Executar antes do primeiro deploy ou deixar `MigrateAsync()` no startup (recomendado dev/staging; produção pode preferir pipeline dedicado).

## Health checks

- Liveness: `GET /health`
- Readiness: `GET /health/ready` (PostgreSQL + Redis)

Configure monitoramento para alertar se readiness falhar por > 2 minutos.

## Segredos

- **Não** commitar credenciais
- Usar variáveis de ambiente ou vault on-prem
- Rotacionar `Graph:ClientSecret` e tokens GLPI periodicamente

## Integrações

| Sistema | Config | Notas |
|---------|--------|-------|
| TOTVS | `Totvs:BaseUrl`, `Totvs:ApiKey` | Sync a cada 30 min via Worker |
| GLPI | `Glpi:BaseUrl`, tokens REST | API v10+ |
| Graph | App registration + client secret | Delta sync recomendado |

Definir `Integrations:UseDevAdapters=false` em produção.

## Backup

- PostgreSQL: backup diário com retenção 30 dias
- Redis: persistência RDB opcional (cache reconstruível)

## Rollback

1. Parar Workers
2. Deploy versão anterior da API
3. Reverter migration se necessário: `dotnet ef database update <MigrationAnterior>`
