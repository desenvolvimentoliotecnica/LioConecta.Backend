# Spike — Fonte RM Banco de Horas (corporeRM)

**Data:** 2026-07-10  
**Base:** `CORPORERM` @ `172.19.30.3` (user `rm_readonly_voltage`)

## Tabelas encontradas

| Tabela | Uso |
|--------|-----|
| `ASALDOBANCOHOR` | Saldo sintético por período (CHAPA, INICIOPER, FIMPER, EXTRA*, ATRASO*, FALTA*) |
| `ABANCOHORFUN` | Movimentos diários (extras por faixa, atraso, falta) |
| `ABANCOHORFUNDETALHE` | Detalhe analítico por evento |
| `ASALDOBANCOHORFUNDETALHE` | Saldo analítico |
| `ACOMPFUN` | Compensação por período |

## Decisão de implementação

- **Saldo:** último período em `ASALDOBANCOHOR`  
  `saldoMin = EXTRAANT+EXTRAATU − ATRASOANT−ATRASOATU − FALTAANT−FALTAATU`
- **Extrato:** últimos 90 dias de `ABANCOHORFUN` (crédito = extras; débito = atraso+falta); fallback por período se vazio
- Valores RM em **minutos** → portal em **horas** (`/ 60`)
