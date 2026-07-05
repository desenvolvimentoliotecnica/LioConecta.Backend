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

Permissões Graph: `User.Read`, `Calendars.Read`, `Tasks.Read`, `Sites.Read.All`, `Presence.Read.All`.

## Resiliência

- **Polly** circuit breaker nos adapters HTTP
- Modo degradado: serve cache PostgreSQL quando integração offline
- Workers re-tentam no próximo ciclo (TOTVS 30 min, Graph 60 min)
