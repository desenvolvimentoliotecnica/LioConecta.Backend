# TOTVS RM — Férias (SQL read-only)

Spike de integração para `spec-ferias`. Escrita no RM fica via `ILeaveRmWriteBack` (API Labore futura).

## Tabelas consultadas

| Tabela | Uso |
|--------|-----|
| `PFUFERIAS` | Períodos aquisitivos, saldo (`SALDOPER`), dias adquiridos (`NRODIAS`) |
| `PFUFERIASPER` | Períodos de gozo/programação (`DTINIGOZO`, `DTFIMGOZO`, `NRODIAS`, `SITUACAOFERIAS`) |

Ambas filtradas por `CODCOLIGADA` + `CHAPA` (via `TotvsRmConstants.CodColigada` e matrícula normalizada).

## Mapeamento de status RM → portal

| RM (`SITUACAOFERIAS` / heurística) | Portal |
|-----------------------------------|--------|
| `P`, `M`, vazio + datas futuras | `pending` |
| `A`, `G` gozo futuro | `approved` |
| gozo passado | `completed` |
| `C`, `R`, cancelado | `rejected` |

Registros criados no portal usam `dataSource=portal` e `rmSyncStatus=pending_rm_sync` até o adapter de write-back confirmar.

## Contagem de dias (solicitação)

Default portal: **dias corridos inclusivos** (`end - start + 1`), alinhado à prática CLT para férias.
