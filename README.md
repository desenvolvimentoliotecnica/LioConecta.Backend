# LioConecta Backend

API .NET 8 para a intranet corporativa LioConecta — autenticação Azure AD, PostgreSQL, integrações TOTVS/GLPI/Microsoft Graph e SignalR.

Repositório front-end: [LioConecta-FrontEnd](https://github.com/desenvolvimentoliotecnica/LioConecta-FrontEnd)

## Stack

| Camada | Tecnologia |
|--------|------------|
| API | ASP.NET Core 8, SignalR, Serilog, Swagger |
| Aplicação | Clean Architecture, FluentValidation, MediatR |
| Dados | PostgreSQL 16, EF Core 8, Redis 7 |
| Auth | Azure AD / Entra ID (Microsoft.Identity.Web) |
| Integrações | TOTVS, GLPI, Microsoft Graph |
| Workers | Background sync (TOTVS + Graph) |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (PostgreSQL + Redis locais)

## Modo simulação (atual)

Por enquanto **não há integração com Azure AD, TOTVS, GLPI ou Graph**. A API opera em modo simulação:

| Config | Valor | Efeito |
|--------|-------|--------|
| `Integrations:UseDevAdapters` | `true` | TOTVS/GLPI/Graph retornam dados mock |
| `AzureAd:ClientId` | vazio | DevAuth — usuário padrão Maria Silva |
| `Auth:UseDevAuth` | `true` | Endpoints acessíveis sem token real |

O front-end deve usar `VITE_USE_MOCK=false` e apontar para esta API — os dados simulados vêm do seed PostgreSQL + adapters de desenvolvimento.

Quando for integrar sistemas reais, defina `Integrations:UseDevAdapters=false` e configure Azure AD / credenciais (ver `docs/azure-ad-setup.md` e `docs/integrations.md`).

## Desenvolvimento local

```bash
# 1. Subir infraestrutura
docker compose up -d

# 2. Restaurar e compilar
dotnet build

# 3. Aplicar migrations (automático no startup da API)
dotnet run --project src/LioConecta.Api

# 4. Workers (sync TOTVS/Graph) — terminal separado
dotnet run --project src/LioConecta.Workers
```

A API inicia em `http://localhost:5000` (ou porta configurada). Swagger: `/swagger`.

### Modo dev (sem Azure AD)

Com `AzureAd:ClientId` vazio, a API usa autenticação de desenvolvimento (usuário Maria Silva). Header opcional:

```
X-Dev-User-Id: maria-silva
```

### Front-end

Configure no `.env` do front:

```
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_USE_MOCK=false
VITE_AZURE_CLIENT_ID=
VITE_AZURE_TENANT_ID=
```

## Estrutura

```
src/
├── LioConecta.Api/           # Controllers, Hubs, Program.cs
├── LioConecta.Application/   # Services, DTOs, interfaces
├── LioConecta.Domain/        # Entidades, enums
├── LioConecta.Infrastructure/# EF Core, repos, adapters
└── LioConecta.Workers/       # Sync jobs
tests/
├── LioConecta.UnitTests/
└── LioConecta.IntegrationTests/
```

## Endpoints principais

| Módulo | Base |
|--------|------|
| Identidade | `GET /api/v1/me` |
| Pessoas | `GET /api/v1/people`, `/org-chart` |
| Feed | `GET/POST /api/v1/feed` |
| Comunicados | `GET /api/v1/comunicados` |
| Serviços | `POST /api/v1/service-requests` |
| Notificações | `GET /api/v1/notifications` |
| Chat | SignalR `/hubs/chat` |
| Health | `GET /health`, `/health/ready` |

OpenAPI completo em `/swagger` (Development).

## Configuração produção (on-prem)

Ver [docs/deploy-onprem.md](docs/deploy-onprem.md).

Variáveis críticas:

- `ConnectionStrings:DefaultConnection` — PostgreSQL
- `ConnectionStrings:Redis` — Redis (SignalR backplane)
- `AzureAd:*` — Tenant, ClientId, Audience
- `Integrations:UseDevAdapters` — `false` em produção
- `Totvs:*`, `Glpi:*`, `Graph:*` — credenciais reais

## Migrations

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/LioConecta.Infrastructure \
  --startup-project src/LioConecta.Api

dotnet ef database update \
  --project src/LioConecta.Infrastructure \
  --startup-project src/LioConecta.Api
```

## Testes

```bash
dotnet test
```

## Licença

Proprietário — Lio Tecnica.
