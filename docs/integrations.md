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

| Operação | Endpoint BFF | Quando |
|----------|--------------|--------|
| `SearchTicketsByRequesterAsync` | `GET /api/v1/ti/help-desk/tickets/mine` | Acompanhar chamados do usuário logado |
| `SearchAllTicketsAsync` | `GET /api/v1/ti/help-desk/tickets/all` | Fila completa (TI/Admin) |
| `GetTicketDetailAsync` | `GET /api/v1/ti/help-desk/tickets/{id}` | Detalhe do chamado |
| `GetItilCategoriesAsync` | `GET /api/v1/ti/help-desk/categories` | Dropdown de categorias no modal de abertura |
| `CreateTicketAsync` | `POST /api/v1/ti/help-desk/tickets` | Abertura de chamado (GLPI primeiro, depois `service_requests`) |
| Teste de conexão | `POST /api/v1/admin/glpi/test` | Admin — valida `initSession` |

**Pré-requisito para criação:** o colaborador logado deve existir no GLPI com o mesmo e-mail corporativo do portal. Caso contrário a API retorna **422** (`GlpiRequesterNotFoundException`). Falhas de comunicação com o GLPI retornam **502**.

**Payload de criação (`POST /tickets`):**

```json
{
  "subject": "VPN instável",
  "priority": "media",
  "categoryId": 12,
  "description": "Descrição com pelo menos 10 caracteres."
}
```

`categoryId` vem de `GET /categories` (search `ITILCategory` no GLPI, cache 5 min no adapter).

**Config (portal `/admin/configuracoes-backend`, categoria GLPI — persistido em `app_settings`):**

| Chave | Descrição |
|-------|-----------|
| `glpi.base_url` | URL da API (ex.: `https://servicedesk.liotecnica.com.br/api.php/v1`) |
| `glpi.portal_url` | URL do portal web para links |
| `glpi.app_token` | App-Token |
| `glpi.user_token` | User-Token do usuário de serviço |

Credenciais lidas em runtime via `IAppSettingsProvider` (sem `appsettings.json`). Desative `integrations.use_dev_adapters` e reinicie a API para usar o adapter real.

## Microsoft Graph

**Adapter:** `IGraphAdapter` → `GraphAdapter` / `DevGraphAdapter`

| Operação | Worker / API |
|----------|--------------|
| `GetDirectoryUsersAsync` | Worker `graph-directory-sync` → tabela `people`, `GET /api/v1/people/directory` |
| `GetUserPhotoBytesAsync` | Worker `graph-directory-sync` → cache local `/media/people/{slug}.jpg` |
| `GetDocumentsAsync` | GraphSyncWorker → módulo Documentos |
| `GetCalendarEventsAsync` | GraphSyncWorker → Calendário |
| `GetPlannerTasksAsync` | Legado — ver **Microsoft Planner** abaixo |
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

Permissões Graph: `User.Read.All` (diretório e organograma), `User.ReadBasic.All` (fotos, se aplicável), `User.Read`, `Calendars.Read`, `Tasks.ReadWrite.All` (Planner — application), `Sites.Read.All`, `Presence.Read.All`.

**Worker `graph-directory-sync`:** sincroniza usuários `@liotecnica.com.br` do Entra ID para `people` (identidade primária). O worker `totvs-employee-sync` enriquece `EmployeeId`, `BirthDate` e `HireDate` sem sobrescrever dados Graph. Disparo manual: `POST /api/v1/admin/workers/graph-directory-sync/trigger`. Intervalo padrão: 60 min (`workers.graph_directory_sync_interval_minutes`).

## Microsoft Planner

**Adapter:** `IPlannerAdapter` → `GraphPlannerAdapter` / `DevPlannerAdapter`

| Operação | Endpoint BFF |
|----------|--------------|
| Listar tarefas do plano | `GET /api/v1/planner/tasks` |
| Listar colunas (buckets) | `GET /api/v1/planner/buckets` |
| Criar tarefa (atribuída ao usuário logado) | `POST /api/v1/planner/tasks` |
| Atualizar tarefa (somente assignee) | `PATCH /api/v1/planner/tasks/{id}` |
| Excluir tarefa (somente assignee) | `DELETE /api/v1/planner/tasks/{id}` |
| Teste admin | `POST /api/v1/admin/planner/test` |

**Config (`app_settings`, categoria `planner` — reutiliza credenciais `graph.*`):**

| Chave | Descrição |
|-------|-----------|
| `planner.enabled` | Liga integração em Minhas Atividades |
| `planner.plan_id` | GUID do plano da equipe |
| `planner.default_bucket_id` | Coluna padrão para novas tarefas |
| `planner.plan_title` | Cache do nome do plano (preenchido no teste) |

**Azure AD:** conceder `Tasks.ReadWrite.All` (Application) com admin consent na mesma app registration do Graph sync.

## Resiliência

- **Polly** circuit breaker nos adapters HTTP
- Modo degradado: serve cache PostgreSQL quando integração offline
- Workers re-tentam no próximo ciclo (TOTVS 30 min, Graph 60 min)
