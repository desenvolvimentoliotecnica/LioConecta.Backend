# LioConecta Backend

API .NET 8 para a intranet corporativa LioConecta — autenticação LDAP + JWT do portal, PostgreSQL, integrações TOTVS/GLPI/Microsoft Graph e SignalR.

Repositório front-end: [LioConecta-FrontEnd](https://github.com/desenvolvimentoliotecnica/LioConecta-FrontEnd)

## Stack

| Camada | Tecnologia |
|--------|------------|
| API | ASP.NET Core 8, SignalR, Serilog, Swagger |
| Aplicação | Clean Architecture, FluentValidation, MediatR |
| Dados | PostgreSQL 16, EF Core 8, Redis 7 |
| Auth | LDAP corporativo + JWT simétrico do portal (`auth.*` em `app_settings`) |
| Integrações | TOTVS, GLPI, Microsoft Graph |
| Workers | Background sync (TOTVS + Graph) |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (PostgreSQL + Redis locais)

## Autenticação (LDAP + super-admin local)

Em produção o portal usa **somente LDAP** (`auth.provider=ldap`). Um super-admin **local** no banco (`portal_users`) permite bootstrap antes do AD estar configurado.

### Fluxo de bootstrap (primeiro deploy)

1. API sobe e executa seed do super-admin local.
2. Admin acessa `/acesso` no front e loga com a conta local.
3. Em `/admin/configuracoes-backend?category=ldap` preenche AD, salva e testa conexão.
4. Colaboradores passam a logar com credencial LDAP.

### Credencial inicial (super-admin local)

| Campo | Valor |
|-------|-------|
| E-mail | `leonardo.mendes@liotecnica.com.br` |
| Senha | variável `PORTAL_SUPER_ADMIN_PASSWORD` no deploy, ou `ChangeMe@2026` se omitida |

**Altere a senha no primeiro acesso em produção.**

### Endpoints de autenticação

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/v1/auth/login` | E-mail + senha → JWT + `MeDto` (anônimo) |
| `POST` | `/api/v1/auth/logout` | Auditoria de logout (requer token) |
| `POST` | `/api/v1/admin/ldap/test` | Teste de bind LDAP (requer Admin) |

Configuração LDAP e JWT em `app_settings` (categorias `auth` e `ldap`), editável em **Configurações do Backend**.

## Integrações externas

TOTVS, GLPI, Microsoft Graph e Planner usam **sempre APIs reais**. Configure credenciais em Configurações do Backend e reinicie a API após alterar secrets.

| Config | Valor | Efeito |
|--------|-------|--------|
| `auth.provider` | `dev` (Development) | DevAuth — endpoints acessíveis sem token real |
| `auth.provider` | `ldap` (produção) | JWT do portal com chave `auth.jwt_signing_key` |

O front-end deve usar `VITE_USE_MOCK=false` e apontar para esta API. Para desenvolvimento local sem LDAP, use `VITE_AUTH_MODE=dev` no front.

Ver `docs/integrations.md` para credenciais de cada sistema.

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

### Modo dev (sem LDAP)

Com `auth.provider=dev` em Development/Testing, a API usa autenticação de desenvolvimento (usuário Maria Silva, role Admin).

### Front-end

Configure no `.env` do front:

```
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_USE_MOCK=false
VITE_AUTH_MODE=dev
```

Remova `VITE_AUTH_MODE=dev` (ou defina outro valor) quando for testar login LDAP real em `/acesso`.

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
| Autenticação | `POST /api/v1/auth/login`, `POST /api/v1/auth/logout` |
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
- `auth.jwt_signing_key` — chave JWT do portal (obrigatória com `auth.provider=ldap`)
- `ldap.*` — servidor AD/LDAP corporativo
- `PORTAL_SUPER_ADMIN_PASSWORD` — senha bootstrap do super-admin local
- `Totvs:*`, `Glpi:*`, `Graph:*` — credenciais reais (sempre consultadas; sem modo mock)

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
