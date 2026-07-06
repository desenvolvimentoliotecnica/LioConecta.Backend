# Integrações — LioConecta Backend

Documentação das integrações externas e adapters de desenvolvimento.

## Modo desenvolvimento

Com `Integrations:UseDevAdapters=true` (padrão em Development), os adapters retornam dados mock alinhados ao front-end LioConecta.

## TOTVS

**Adapter:** `ITotvsAdapter` → `TotvsAdapter` / `DevTotvsAdapter`

| Operação | Endpoint interno | Uso |
|----------|------------------|-----|
| Sync colaboradores | Worker `TotvsSyncWorker` | Organograma, diretório, perfis |
| Contracheque | `GetPayslipAsync` | `/servicos/contracheque` |
| Férias | `GetVacationBalanceAsync`, `SubmitVacationRequestAsync` | `/servicos/ferias` |
| Benefícios | `GetBenefitsAsync` | `/servicos/beneficios` |
| Ponto | `GetTimeClockAsync` | `/servicos/ponto` |

**Config produção:**
```json
{
  "Integrations": { "UseDevAdapters": false },
  "Totvs": {
    "BaseUrl": "https://totvs.internal/api",
    "ApiKey": "<secret>"
  }
}
```

## GLPI

**Adapter:** `IGlpiAdapter` → `GlpiAdapter` / `DevGlpiAdapter`

| Operação | Quando |
|----------|--------|
| `CreateTicketAsync` | Submissão de solicitação TI/Facilities |
| `GetTicketStatusAsync` | Polling de status |

**Config:**
```json
{
  "Glpi": {
    "BaseUrl": "https://glpi.internal/apirest.php",
    "AppToken": "<token>",
    "UserToken": "<token>"
  }
}
```

Requer GLPI 10+ com API REST habilitada.

## Microsoft Graph

**Adapter:** `IGraphAdapter` → `GraphAdapter` / `DevGraphAdapter`

| Operação | Worker / API |
|----------|--------------|
| `GetDirectoryUsersAsync` | Worker `graph-directory-sync` → tabela `people`, `GET /api/v1/people/directory` |
| `GetUserPhotoBytesAsync` | Worker `graph-directory-sync` → cache local `/media/people/{slug}.jpg` |
| `GetDocumentsAsync` | GraphSyncWorker → módulo Documentos |
| `GetCalendarEventsAsync` | GraphSyncWorker → Calendário |
| `GetPlannerTasksAsync` | Activities API |
| `GetUserPresenceAsync` | Chat presença |
| `SyncUserPhotosAsync` | Fotos de perfil |

**Config:**
```json
{
  "Graph": {
    "TenantId": "<tenant>",
    "ClientId": "<app-id>",
    "ClientSecret": "<secret>"
  }
}
```

Permissões Graph: `User.Read.All` (diretório e organograma), `User.ReadBasic.All` (fotos, se aplicável), `User.Read`, `Calendars.Read`, `Tasks.Read`, `Sites.Read.All`, `Presence.Read.All`.

**Worker `graph-directory-sync`:** sincroniza usuários `@liotecnica.com.br` do Entra ID para `people` (identidade primária). O worker `totvs-employee-sync` enriquece `EmployeeId`, `BirthDate` e `HireDate` sem sobrescrever dados Graph. Disparo manual: `POST /api/v1/admin/workers/graph-directory-sync/trigger`. Intervalo padrão: 60 min (`workers.graph_directory_sync_interval_minutes`).

## Resiliência

- **Polly** circuit breaker nos adapters HTTP
- Modo degradado: serve cache PostgreSQL quando integração offline
- Workers re-tentam no próximo ciclo (TOTVS 30 min, Graph 60 min)
