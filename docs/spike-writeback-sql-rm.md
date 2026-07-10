# Spike — Write-back SQL direto (corporeRM)

**Data:** 2026-07-10  
**Base:** `CORPORERM` via Db Explorer `totvs-rm`  
**Decisão Onda 1B:** escrita SQL direta (sem API Labore), com journal + rollback.

## Achado crítico — schema real ≠ queries de leitura atuais

O repositório de leitura de férias usa colunas **inexistentes** nesta base:

| Código antigo (errado) | Schema real |
|------------------------|-------------|
| `PFUFERIASPER.DTINIGOZO` | `DATAINICIO` |
| `PFUFERIASPER.DTFIMGOZO` | `DATAFIM` |
| `PFUFERIASPER.NRODIAS` | `NRODIASFERIAS` |
| `PFUFERIAS.SALDOPER` / `NRODIAS` | `SALDO` (sem `NRODIAS` / `DTVENCFERIAS`) |

**Ação:** corrigir `TotvsRmLeaveRepository` no mesmo PR do write-back, senão o sync RO não enxerga inserts.

## Férias — `PFUFERIAS` / `PFUFERIASPER`

### PK / identidade

- `PFUFERIAS`: `(CODCOLIGADA, CHAPA, FIMPERAQUIS)`
- `PFUFERIASPER`: mesma chave de período aquisitivo + intervalo de gozo (`DATAINICIO`/`DATAFIM`)

### `SITUACAOFERIAS` (amostra)

| Código | Contagem | Interpretação portal |
|--------|----------|----------------------|
| `F` | ~10k | Finalizado / gozado |
| `M` | 81 | (manter mapeamento legado) |
| `D` | 56 | Programado / deferido |
| `P` | 31 | Pendente / em programação |

Write-back inicial: inserir com **`P`** (pedido portal); após approve portal pode promover para **`D`**.

### INSERT mínimo (programação)

```sql
INSERT INTO dbo.PFUFERIASPER (
  CODCOLIGADA, CHAPA, FIMPERAQUIS,
  DATAINICIO, DATAFIM,
  NRODIASFERIAS, NRODIASFERIASCORRIDOS,
  NRODIASABONO, NRODIASABONOCORRIDOS, POSICAOABONO,
  PAGA1APARC13O, FERIASPERDIDAS, FALTAS,
  SITUACAOFERIAS, OBSERVACAO,
  RECCREATEDBY, RECCREATEDON
) VALUES (
  @CodColigada, @Chapa, @FimPerAquis,
  @DataInicio, @DataFim,
  @NroDias, @NroDias,
  0, 0, 0,
  0, 0, 0,
  'P', @Observacao,  -- OBSERVACAO contém marker E2E-WB-...
  'lioconecta', SYSUTCDATETIME()
);

UPDATE dbo.PFUFERIAS
SET SALDO = SALDO - @NroDias,
    RECMODIFIEDBY = 'lioconecta',
    RECMODIFIEDON = SYSUTCDATETIME()
WHERE CODCOLIGADA = @CodColigada
  AND LTRIM(RTRIM(CHAPA)) = @Chapa
  AND FIMPERAQUIS = @FimPerAquis
  AND SALDO >= @NroDias;
```

`@FimPerAquis` = período aberto (`PERIODOABERTO = 1` e `SALDO >= dias`) mais antigo ou o escolhido no pedido.

### Reverse (rollback)

```sql
DELETE FROM dbo.PFUFERIASPER
WHERE CODCOLIGADA = @CodColigada
  AND LTRIM(RTRIM(CHAPA)) = @Chapa
  AND FIMPERAQUIS = @FimPerAquis
  AND DATAINICIO = @DataInicio
  AND DATAFIM = @DataFim
  AND OBSERVACAO LIKE '%' + @Marker + '%';

UPDATE dbo.PFUFERIAS
SET SALDO = SALDO + @NroDias,
    RECMODIFIEDBY = 'lioconecta-rollback',
    RECMODIFIEDON = SYSUTCDATETIME()
WHERE CODCOLIGADA = @CodColigada
  AND LTRIM(RTRIM(CHAPA)) = @Chapa
  AND FIMPERAQUIS = @FimPerAquis;
```

## Ponto — `ABATFUN` (+ espelho `AAFHTFUN`)

### Colunas relevantes `ABATFUN`

`CODCOLIGADA, CHAPA, DATA, BATIDA` (minutos desde 00:00), `STATUS`, `NATUREZA`, `DATAINSERCAO`, `RECCREATEDBY`, …

### Naturezas (`ANATUBAT`)

| CODINTERNO | Uso |
|------------|-----|
| 0/1 | Coletado relógio |
| 4 | Entrada alterada pelo usuário |
| 5 | Saída alterada pelo usuário |

### INSERT mínimo (ajuste)

```sql
INSERT INTO dbo.ABATFUN (
  CODCOLIGADA, CHAPA, DATA, BATIDA, STATUS, NATUREZA,
  DATAINSERCAO, RECCREATEDBY, RECCREATEDON, DATAREFERENCIAALTERADA
) VALUES (
  @CodColigada, @Chapa, @Data, @BatidaMinutos, 'C', @Natureza,
  SYSUTCDATETIME(), 'lioconecta', SYSUTCDATETIME(), 1
);
```

`AAFHTFUN` é derivado/processado — **não** escrever no write-back 1B (só batidas). Sync RO continua lendo o espelho.

### Reverse

```sql
DELETE FROM dbo.ABATFUN
WHERE CODCOLIGADA = @CodColigada
  AND LTRIM(RTRIM(CHAPA)) = @Chapa
  AND DATA = @Data
  AND BATIDA = @BatidaMinutos
  AND RECCREATEDBY = 'lioconecta'
  AND NATUREZA IN (4, 5);
```

(Journal guarda PKs exatas por linha.)

## Modos e segurança

| Mode | Comportamento |
|------|----------------|
| `off` | Não processa |
| `dry_run` | Monta SQL + valida período/saldo/chapa; não executa |
| `apply_rollbackable` | Executa + journal; UAT chama rollback |
| `apply` | Executa + journal; rollback só admin |

Gate: `*.rm.writeback.allow_prod` default `false` — `apply*` só em homolog (ou flag explícita).

## Riscos

- Labore pode recalcular/ignorar linhas sem passar por tela oficial.
- Triggers não inventariados nesta spike (readonly user).
- Corrida com sync RO — após write, invalidar cache / aguardar próximo sync.
- Credencial atual do portal é **readonly** (`rm_readonly_*`) — write-back exige connection string com permissão INSERT/UPDATE/DELETE (setting separado ou user de escrita).

## Checkpoint

Schema e SQL mínimo **viáveis** para férias e ponto. Prosseguir com journal + adapters SQL; corrigir leitura de férias no mesmo ciclo.
