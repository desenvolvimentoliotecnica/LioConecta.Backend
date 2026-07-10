# Spike — Férias a tirar / a vencer (CORPORERM)

**Data:** 2026-07-10  
**Base:** `CORPORERM` @ `172.19.30.3` (user `rm_readonly_voltage`)  
**Coligada:** `1`  
**CSV completo:** [`spike-ferias-rm-resultado.csv`](./spike-ferias-rm-resultado.csv) (377 períodos)

## Tabelas

| Tabela | Uso |
|--------|-----|
| `PFUFERIAS` | Período aquisitivo + saldo (`SALDO`) |
| `PFUFERIASPER` | Gozo/programação (`DATAINICIO`, `DATAFIM`, `NRODIASFERIAS`, `SITUACAOFERIAS`) |
| `PFUNC` | Nome + `CODSITUACAO` (excluir `D` = demitido) |

### Schema real vs código atual

O `TotvsRmLeaveRepository` assume colunas que **não existem** nesta base:

| Código (errado) | Real em CORPORERM |
|-----------------|-------------------|
| `PFUFERIAS.SALDOPER` | `SALDO` |
| `PFUFERIAS.NRODIAS` | *(não existe)* |
| `PFUFERIAS.DTVENCFERIAS` | *(não existe — usar `FIMPERAQUIS + 1 ano`)* |
| `PFUFERIASPER.DTINIGOZO` / `DTFIMGOZO` | `DATAINICIO` / `DATAFIM` |
| `PFUFERIASPER.NRODIAS` | `NRODIASFERIAS` |

Campos úteis extras em `PFUFERIAS`: `PERIODOABERTO`, `PERIODOPERDIDO`, `MOTIVOPERDA`, `FALTAS`, `BONUS`.

## Critério do relatório

1. `CODCOLIGADA = 1`
2. `SALDO > 0` e `PERIODOPERDIDO = 0`
3. Período aquisitivo **já encerrado** (`FIMPERAQUIS < hoje`) — exclui saldo “projetado” de período em aberto
4. Funcionário **não demitido** (`PFUNC.CODSITUACAO <> 'D'`)
5. **Vencimento concessivo** = `DATEADD(year, 1, FIMPERAQUIS)` (CLT; não há coluna de vencimento no RM)

Classificação:

| SituacaoSaldo | Regra |
|---------------|--------|
| `VENCIDO` | vencimento < hoje |
| `VENCE_30D` | vence em até 30 dias |
| `VENCE_90D` | vence em 31–90 dias |
| `A_TIRAR` | vence depois de 90 dias |

## Resultado (10/07/2026)

| Situação | Períodos | Funcionários | Saldo (dias) |
|----------|----------|--------------|--------------|
| VENCIDO | 202 | 201 | 4.231 |
| VENCE_30D | 7 | 7 | 103 |
| VENCE_90D | 15 | 15 | 269 |
| A_TIRAR | 153 | 153 | 2.839 |
| **Total** | **377** | **~323*** | **7.442** |

\*Um funcionário pode ter mais de um período; o CSV lista períodos.

### Vencem em até 30 dias

| Chapa | Nome | Vencimento | Saldo |
|-------|------|------------|-------|
| 00000113 | LUCIANA MARIA VALDEVINO | 2026-07-31 | 5 |
| 00002816 | GABRIEL TEIXEIRA DIAS | 2026-08-02 | 18 |
| 00002817 | MARIANA HELEN DIAS PIPOLO | 2026-08-02 | 15 |
| 00000109 | RAFAELLA VENTURA DUDAS | 2026-08-02 | 18 |
| 00002762 | GEOVANNA MACHADO DE OLIVEIRA | 2026-08-04 | 9 |
| 00000088 | SIMONE NUNES DOS REIS | 2026-08-07 | 20 |
| 00000245 | ANA PAULA DA SILVA MORI | 2026-08-08 | 18 |

### Vencem em 31–90 dias (amostra)

| Chapa | Nome | Vencimento | Saldo |
|-------|------|------------|-------|
| 00003721 | JEFFERSON FERREIRA LIMA | 2026-08-14 | 15 |
| 99001429 | CARLOS ALBERTO GONCALVES MIRANDA | 2026-08-17 | 20 |
| 11500126 | JOAO VITOR SILVA LOPES | 2026-08-31 | 18 |
| 00000800 | ARTUR SOARES ARAUJO | 2026-09-01 | 30 |
| 00000241 | CLAUDIA GOMES DA SILVA | 2026-09-02 | 18 |
| … | *(lista completa no CSV / saída do spike)* | | |

## SQL de referência

```sql
SELECT
  LTRIM(RTRIM(F.CHAPA)) AS Chapa,
  LTRIM(RTRIM(P.NOME)) AS Nome,
  CAST(F.INICIOPERAQUIS AS date) AS InicioAquisitivo,
  CAST(F.FIMPERAQUIS AS date) AS FimAquisitivo,
  CAST(DATEADD(year, 1, F.FIMPERAQUIS) AS date) AS VencimentoConcessivo,
  CAST(F.SALDO AS decimal(10,2)) AS SaldoDias,
  DATEDIFF(day, CAST(GETDATE() AS date), DATEADD(year, 1, F.FIMPERAQUIS)) AS DiasParaVencer
FROM dbo.PFUFERIAS F WITH (NOLOCK)
INNER JOIN dbo.PFUNC P WITH (NOLOCK)
  ON P.CODCOLIGADA = F.CODCOLIGADA
 AND LTRIM(RTRIM(P.CHAPA)) = LTRIM(RTRIM(F.CHAPA))
WHERE F.CODCOLIGADA = 1
  AND ISNULL(F.SALDO, 0) > 0
  AND ISNULL(F.PERIODOPERDIDO, 0) = 0
  AND F.FIMPERAQUIS < CAST(GETDATE() AS date)
  AND P.CODSITUACAO <> 'D';
```

## Próximos passos (se for para produto)

1. ~~Corrigir `TotvsRmLeaveRepository` para o schema real~~ — feito (SALDO, DATAINICIO/DATAFIM, NRODIASFERIAS, vencimento `FIMPERAQUIS+1ano`, filtro `PERIODOPERDIDO`)
2. Calcular vencimento como `FIMPERAQUIS + 1 ano` (ou confirmar regra com RH se houver exceção).
3. Validar com RH se saldos `VENCIDO` antigos ainda são operacionais ou lixo histórico (há muitos períodos com anos de atraso).
4. Opcional: cruzar com `PFUFERIASPER` (`SITUACAOFERIAS`) para descontar gozo já programado do saldo exibido.
