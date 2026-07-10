# TOTVS RM — Férias (SQL read + write, Onda 1B)

Spike de integração para `spec-ferias` (leitura) e `docs/spike-writeback-sql-rm.md` (escrita, Onda 1B).

**Decisão Onda 1B (10/07/2026):** escrita via `ILeaveRmWriteBack` passou a ser **SQL direto** no `corporerm`
(`TotvsRmSqlLeaveWriteBack`), em vez de aguardar API Labore oficial. Roteamento por `ChainedLeaveRmWriteBack`
conforme `leave.rm.writeback.mode` (`off` \| `dry_run` \| `apply_rollbackable` \| `apply`); ver
`docs/requisitos-totvs-ferias-writeback.md` §Atualização Onda 1B para o contrato de serviço oficial (objetivo futuro).

## Tabelas consultadas / escritas

| Tabela | Uso |
|--------|-----|
| `PFUFERIAS` | Períodos aquisitivos, saldo (`SALDO`), flags `PERIODOABERTO` / `PERIODOPERDIDO`. Write-back faz `UPDATE SALDO` |
| `PFUFERIASPER` | Períodos de gozo/programação (`DATAINICIO`, `DATAFIM`, `NRODIASFERIAS`, `SITUACAOFERIAS`). Write-back faz `INSERT` (status inicial `P`) |

Ambas filtradas por `CODCOLIGADA` + `CHAPA` (via `TotvsRmConstants.CodColigada` e matrícula normalizada).

> **Spike 2026-07-10:** nesta CORPORERM **não existem** `SALDOPER`, `NRODIAS`, `DTVENCFERIAS`, `DTINIGOZO`, `DTFIMGOZO`.  
> Vencimento concessivo ≈ `FIMPERAQUIS + 1 ano`. Ver `docs/spike-ferias-rm.md` e `docs/spike-writeback-sql-rm.md`.

### Resolução do período aberto (write-back)

`TotvsRmSqlLeaveWriteBack` seleciona o período de `PFUFERIAS` com `PERIODOABERTO = 1 AND SALDO >= @dias`,
ordenado por `FIMPERAQUIS` (mais antigo primeiro). Sem período elegível → falha de negócio (sem gravação parcial).

### Journal e rollback

Cada write-back grava uma entrada em `rm_writeback_journals` (SQL forward + reverso, chaves RM em JSON).
Rollback administrativo: `POST /api/v1/admin/rm-writeback/sessions/{sessionId}/rollback` (`admin.totvs.manage`).
Em `apply` (produção), o registro é mantido para auditoria mesmo após rollback não ser mais aplicável no RM.

## Mapeamento de status RM → portal

| RM (`SITUACAOFERIAS` / heurística) | Portal |
|-----------------------------------|--------|
| `P` (pendente/em programação — inclusive write-back Onda 1B) | `pending` |
| `A`, `G`, `D` (programado/deferido) com gozo futuro | `approved` |
| `A`, `G`, `D` com gozo passado | `completed` |
| `C`, `R`, cancelado | `rejected` |

Amostra observada em `PFUFERIASPER.SITUACAOFERIAS` (spike 10/07): `F` (~10k, finalizado), `M` (81, legado),
`D` (56, programado/deferido), `P` (31, pendente/em programação).

Registros criados no portal usam `dataSource=portal` e `rmSyncStatus=pending_rm_sync` até o adapter de write-back
confirmar (`synced` | `dry_run` | `failed`).

## Contagem de dias (solicitação)

Default portal: **dias corridos inclusivos** (`end - start + 1`), alinhado à prática CLT para férias.
