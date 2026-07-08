# Requisitos para fornecedor externo — Write-back de Férias (TOTVS RM Labore)

| Campo | Valor |
|-------|--------|
| Solicitante | LioTecnica / LioConecta |
| Módulo | Férias e ausências (`US-RM-002` / `US-RM-003`) |
| Destinatário | Analista / consultor TOTVS RM · Labore |
| Status | **Pendente** — portal já lê o RM; escrita ainda não disponível |
| Atualizado em | 08/07/2026 |

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
| **Escrita (pendente deste requisito)** | Solicitação é gravada **somente** na base do portal (Postgres). Fica marcada `pending_rm_sync` até existir API oficial |

**Escopo explícito**

- O LioConecta **não** fará `INSERT`/`UPDATE` direto nas tabelas do RM.  
- A criação/atualização no Labore deve ocorrer via **serviço oficial** (DataServer, WebService SOAP ou API REST / middleware homologado pela TOTVS).

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

Já preparado no backend:

- Fila / status `pending_rm_sync` nas solicitações do portal  
- Interface `ILeaveRmWriteBack`  
- Implementação enfileirada (default) + esqueleto `TotvsRmApiLeaveWriteBack`  
- Setting `leave.rm.writeback.enabled`  
- Worker `leave-writeback`  

**Leitura atual (não substituir, só complementar):** ver `docs/integrations-totvs-rm-leave-sql.md` (branch `feat/spec-ferias-us-rm-002-003` ou documentação equivalente após merge).

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
