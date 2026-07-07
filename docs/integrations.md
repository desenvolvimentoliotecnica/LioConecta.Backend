# Integrações — LioConecta Backend

Documentação das integrações externas. **Todas usam adapters reais** (TOTVS, GLPI, Microsoft Graph, Planner) — não há modo mock de integrações.

Configure credenciais em **Configurações do Backend** (`/admin/configuracoes-backend`) e reinicie a API após alterar chaves sensíveis.

## TOTVS

**Adapter:** `ITotvsAdapter` → `TotvsAdapter`

| Operação | Endpoint interno | Uso |
|----------|------------------|-----|
| Sync colaboradores | Worker `TotvsSyncWorker` | Organograma, diretório, perfis |
| Contracheque | `GetPayslipAsync` | `/servicos/contracheque` |
| Férias | `GetVacationBalanceAsync`, `SubmitVacationRequestAsync` | `/servicos/ferias` |
| Benefícios | `GetBenefitsAsync` | `/servicos/beneficios` |
| Ponto | `GetTimeClockAsync` | `/servicos/ponto` |

**Config:**
```json
{
  "Totvs": {
    "BaseUrl": "https://totvs.internal/api",
    "ApiKey": "<secret>"
  }
}
```

## GLPI

**Adapter:** `IGlpiAdapter` → `GlpiAdapter`

| Operação | Endpoint BFF | Quando |
|----------|--------------|--------|
| `SearchTicketsByRequesterAsync` | `GET /api/v1/ti/help-desk/tickets/mine` | Acompanhar chamados do usuário logado |
| `SearchAllTicketsAsync` | `GET /api/v1/ti/help-desk/tickets/all` | Fila completa (TI/Admin) |
| `GetTicketDetailAsync` | `GET /api/v1/ti/help-desk/tickets/{id}` | Detalhe do chamado |
| `CreateTicketAsync` | `POST /api/v1/ti/help-desk/tickets` | Abrir chamado (wizard) |
| `GetAllItilCategoriesAsync` | `GET /api/v1/ti/help-desk/categories` | Catálogo por área |
| `GetEntitiesAsync` | `GET /api/v1/ti/help-desk/entities` | Entidades GLPI |

Credenciais lidas em runtime via `IAppSettingsProvider` (sem `appsettings.json`). Configure `glpi.*` e reinicie a API após alterar credenciais.

## Microsoft Graph

**Adapter:** `IGraphAdapter` → `GraphAdapter`

Workers: `GraphSyncWorker`, `GraphDirectorySyncWorker` — sincronizam fotos e diretório Azure AD.

| Config | Chave |
|--------|-------|
| Tenant | `graph.tenant_id` |
| App registration | `graph.client_id` |
| Secret | `graph.client_secret` |

## Microsoft Planner

**Adapter:** `IPlannerAdapter` → `GraphPlannerAdapter`

Reutiliza credenciais Graph. Habilitar em `planner.enabled` e informar `planner.plan_id`.
