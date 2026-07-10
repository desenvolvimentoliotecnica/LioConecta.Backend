# Requisitos para fornecedor externo — Write-back de Férias (TOTVS RM Labore)

| Campo | Valor |
|-------|--------|
| Solicitante | LioTecnica / LioConecta |
| Módulo | Férias e ausências (`US-RM-002` / `US-RM-003`) |
| Destinatário | Analista / consultor TOTVS RM · Labore |
| Status | **Superado pela Onda 1B** — portal passou a escrever via SQL direto (journal + rollback) enquanto o serviço oficial não existe; este documento permanece como referência para quando o fornecedor entregar a API |
| Atualizado em | 10/07/2026 |

---

## ⚠️ Atualização Onda 1B (10/07/2026) — decisão de escrita SQL direta

Após spike (`docs/spike-writeback-sql-rm.md`), decidiu-se **não aguardar** o serviço oficial descrito abaixo e implementar write-back via **SQL direto** no `corporerm`, com:

- Modos configuráveis `leave.rm.writeback.mode` = `off` \| `dry_run` \| `apply_rollbackable` \| `apply` (gate de produção via `leave.rm.writeback.allow_prod`);
- `INSERT` em `PFUFERIASPER` (status inicial `P`) + `UPDATE` do saldo em `PFUFERIAS.SALDO`;
- Journal (`rm_writeback_journals`) com SQL reverso por sessão, permitindo rollback administrativo (`POST /api/v1/admin/rm-writeback/sessions/{sessionId}/rollback`) em UAT/homologação;
- Aprovação no portal (`POST /api/v1/rh/leave/management/{id}/approve`) dispara o write-back imediatamente (best-effort) além do worker `leave-writeback`.

O restante deste documento (contrato de serviço oficial via API/DataServer) **permanece válido como objetivo de médio prazo**: quando o fornecedor entregar o serviço, o adapter SQL pode ser substituído sem mudança de contrato interno (`ILeaveRmWriteBack`). Até lá, a escrita SQL direta é a fonte de verdade em `apply`/`apply_rollbackable`.

---

## 1. Contexto

O portal **LioConecta** permite ao colaborador:

1. Consultar saldo e períodos de férias  
2. Abrir solicitação de férias (datas, dias, observações)  
3. Acompanhar status no portal  

**Situação atual da integração**

| Direção | Como funciona hoje |
|---------|--------------------|
| **Leitura (já implementada)** | SQL read-only no banco `corporerm`: `PFUFERIAS` (períodos aquisitivos / saldo) e `PFUFERIASPER` (gozo / programação / situação) |
| **Escrita (Onda 1B — implementada)** | SQL direto no `corporerm` (`TotvsRmSqlLeaveWriteBack`), com journal/rollback. Solicitação continua sendo gravada na base do portal (Postgres) e marcada `pending_rm_sync` até o write-back ser processado |

**Escopo explícito (revisado)**

- Enquanto o serviço oficial do fornecedor não existir, o LioConecta **passa a fazer** `INSERT`/`UPDATE` direto nas tabelas do RM (Onda 1B), com journal reverso para rollback em UAT/homologação e gate `allow_prod` para produção.
- Quando disponível, a criação/atualização no Labore poderá migrar para o **serviço oficial** (DataServer, WebService SOAP ou API REST / middleware homologado pela TOTVS) descrito nas seções abaixo, sem alterar o contrato `ILeaveRmWriteBack` consumido pelo restante do portal.

---

## 2. Objetivo do fornecedor

Entregar um **contrato de serviço de escrita** que permita ao LioConecta registrar no RM Labore um **pedido ou programação de férias** para um colaborador identificado por `CODCOLIGADA` + `CHAPA`, e (recomendado) consultar o status desse registro depois.

Quando o contrato estiver disponível, o portal ligará o write-back (`leave.rm.writeback.enabled`) no adapter já preparado (`ILeaveRmWriteBack`).

---

## 3. Operações solicitadas

### 3.1 Obrigatória — Incluir solicitação / programação de férias

**Nome sugerido (conceitual):** `SubmitVacationRequest` / “Incluir férias do colaborador”.

#### Entrada (mínimo)

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `codColigada` | int / string | Sim | Coligada (hoje o portal usa valor fixo de configuração) |
| `chapa` | string | Sim | Matrícula RM, normalizada (mesma usada no holerite) |
| `startDate` | date (ISO `YYYY-MM-DD`) | Sim | Início do gozo / período solicitado |
| `endDate` | date (ISO `YYYY-MM-DD`) | Sim | Fim do período (`endDate` ≥ `startDate`) |
| `days` | int | Condicional | Dias corridos inclusivos; se omitido, RM pode calcular |
| `notes` | string | Não | Observação / substituto / motivo curto |
| `externalCorrelationId` | string (GUID) | Recomendado | ID da solicitação no LioConecta (idempotência / rastreio) |

#### Entrada (desejável / fase 2)

| Campo | Descrição |
|-------|-----------|
| `abonoDays` | Dias de abono pecuniário (0–10, se política permitir) |
| `periodKey` | Identificador do período aquisitivo no RM |
| `requestType` | `gozo` \| `programacao` \| `solicitacao` (confirmar nomenclatura Labore) |

#### Saída (mínimo)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `success` | bool | Se o RM aceitou o registro |
| `externalId` | string | Chave única no RM (para acompanhamento e sync) |
| `status` | string | Status inicial normalizado (ver §4) |
| `message` | string | Mensagem amigável ou técnica para suporte |

#### Comportamento esperado

- Validar saldo / regras Labore **no RM** (o portal já bloqueia saldo insuficiente de forma preliminar).  
- Ser **idempotente** se o mesmo `externalCorrelationId` for reenviado.  
- Em falha de negócio (saldo, conflito de datas, chapa inexistente): responder com código HTTP/SOAP de erro e mensagem clara — **não** gravar registro parcial.

### 3.2 Recomendada — Consultar status da solicitação

**Nome sugerido:** `GetVacationRequestStatus`.

| Entrada | Saída |
|---------|--------|
| `codColigada` + `chapa` + `externalId` (ou período início/fim) | `status`, datas, dias, data última alteração |

Hoje o portal já lê `PFUFERIASPER.SITUACAOFERIAS` via SQL. Um endpoint de status reduz acoplamento a colunas internas e facilita homologação de aprovação/rejeição.

### 3.3 Fora de escopo deste pacote

- Parametrização de política de férias no RM (admin folha)  
- Aprovação gestor/RH **dentro** do portal (aprovação formal continua no fluxo Labore/RH, se assim definido)  
- Escrita SQL direta nas tabelas do corporerm pelo LioConecta  

**Nota (portal):** a fila de gestão `/servicos/ferias-ausencias/gestao` e o PDF/comprovante são somente leitura + notificação; não substituem o write-back Labore descrito neste documento. 

---

## 4. Normalização de status (portal ↔ RM)

O portal usa estes status internos. O fornecedor deve indicar o mapeamento dos códigos Labore equivalentes:

| Status portal | Significado | Exemplo de heurística atual (SQL) |
|---------------|-------------|-----------------------------------|
| `pending` | Aguardando análise / sync | `SITUACAOFERIAS` P/M / ainda não aprovado |
| `approved` | Período programado / aprovado (gozo futuro) | A/G com início futuro |
| `completed` | Gozo já decorrido | Fim do período no passado |
| `rejected` | Cancelado / rejeitado | C/R / cancelado |

Documentar a tabela oficial de `SITUACAOFERIAS` (ou equivalente no serviço) é parte da entrega.

---

## 5. Requisitos não funcionais

| Item | Expectativa |
|------|-------------|
| Protocolo | REST JSON **ou** SOAP/DataServer Labore — preferência REST se existir middleware |
| Autenticação | Usuário de serviço / token / certificado (documentar fluxo) |
| Ambiente | Homolog **e** produção, com mesmas assinaturas |
| Disponibilidade | SLA alinhado ao RM de folha; timeouts e códigos de indisponibilidade documentados |
| Segurança | Sem escrita via SQL ad-hoc; TLS em todas as chamadas |
| Observabilidade | Correlation id / logging do `externalId` e `externalCorrelationId` |
| Volume | Sob demanda (colaborador); picos baixos — fila no portal já existe |

---

## 6. Entregáveis esperados do fornecedor

1. **Documento de contrato** (OpenAPI, WSDL ou especificação DataServer)  
2. **Credenciais de homolog** + permissões mínimas no módulo de férias  
3. **Exemplos** de request/response (sucesso, saldo insuficiente, chapa inválida, período conflitante)  
4. **Mapa de status** RM → valores acima  
5. Confirmação de quais processos Labore são atualizados (ex.: geração em `PFUFERIASPER`) — somente para auditoria  
6. Contato técnico e prazo estimado de disponibilidade em homolog  

---

## 7. Critérios de aceite (LioConecta)

O write-back será considerado pronto para ativação quando:

- [ ] Chamada com `CHAPA` + período válido cria registro visível no Labore / `PFUFERIASPER` (ou tela oficial de férias)  
- [ ] Resposta devolve `externalId` estável  
- [ ] Reenvio com o mesmo `externalCorrelationId` não duplica período  
- [ ] Erros de negócio retornam mensagem utilizável no portal (sem 500 genérico)  
- [ ] Consulta de status (serviço ou leitura SQL documentada) reflete aprovação/rejeição após ação do RH no RM  
- [ ] Em ≤ 24h após aprovação no RM, o status no portal fica coerente após sync (já previsto no worker de leitura)

---

## 8. Integração lado LioConecta (referência interna)

Já implementado no backend (Onda 1B):

- Fila / status `pending_rm_sync` nas solicitações do portal
- Interface `ILeaveRmWriteBack`, roteada por `ChainedLeaveRmWriteBack` conforme `leave.rm.writeback.mode`:
  - `off` → fila local (`QueuedLeaveRmWriteBack`, sem escrita no RM)
  - `dry_run` / `apply_rollbackable` / `apply` → SQL direto (`TotvsRmSqlLeaveWriteBack`)
- Setting legado `leave.rm.writeback.enabled` (mapeia para `apply`/`off` quando `leave.rm.writeback.mode` não estiver definido) + settings novos `leave.rm.writeback.mode` e `leave.rm.writeback.allow_prod`
- Journal de write-back (`RmWriteBackJournal` / `IRmWriteBackJournalService`) com rollback administrativo
- Worker `leave-writeback` (processa `RmSyncStatus=pending_rm_sync`) + disparo imediato best-effort na aprovação

Esqueleto de API oficial (`TotvsRmApiLeaveWriteBack`) permanece no código para quando o fornecedor entregar o contrato descrito neste documento — hoje não é utilizado pelo roteador.

**Leitura atual (não substituir, só complementar):** ver `docs/integrations-totvs-rm-leave-sql.md`.

---

## 9. Mensagem pronta para o analista TOTVS

> Precisamos de um **serviço oficial TOTVS RM Labore (API REST, SOAP ou DataServer)** para o LioConecta **criar solicitação/programação de férias** por `CODCOLIGADA` + `CHAPA` + período (início/fim/dias).  
>  
> Hoje o portal **apenas lê** `PFUFERIAS` e `PFUFERIASPER` via SQL.  
> **Não** faremos `INSERT`/`UPDATE` direto no corporerm.  
>  
> Pedimos: contrato (OpenAPI/WSDL), autenticação, payload de entrada/saída com `externalId`, regras de validação (saldo, conflito, chapa), mapa de status e ambiente de homolog com exemplos.  
>  
> Com isso ativamos o write-back no adapter já preparado no LioConecta.

---

## 10. Histórico

| Data | Evento |
|------|--------|
| 08/07/2026 | Requisitos redigidos a partir da implementação read-only + fila write-back da spec `spec-ferias` |
| 10/07/2026 | Onda 1B: decisão de escrita SQL direta (`docs/spike-writeback-sql-rm.md`) implementada como solução interina; documento mantido como objetivo de médio prazo (serviço oficial do fornecedor) |
