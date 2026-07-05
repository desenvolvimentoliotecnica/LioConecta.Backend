# Configuração SMTP — Office 365 (Liotecnica)

Documentação e evidências do setup de e-mail transacional em **05/07/2026**.

## Resumo

| Campo | Valor |
|-------|-------|
| Host SMTP | `smtp.office365.com` |
| Porta | `587` (STARTTLS) |
| Remetente | `leonardo.mendes@liotecnica.com.br` |
| Nome exibido | `LioConecta` |
| Usuário SMTP | `leonardo.mendes@liotecnica.com.br` |
| Senha | Armazenada criptografada (AES) na tabela `email_configurations` — **não** versionada no Git |
| Status | Habilitado (`isEnabled: true`) |
| ID da config | `e9d81c17-4d60-4943-83db-ced5f1e9c746` |

## Onde a configuração vive

1. **Banco PostgreSQL** — tabela `email_configurations` (fonte de verdade em runtime).
2. **Código** — `EmailConfigurationDefaults.cs` define host, porta, remetente e TLS para novos ambientes (sem senha).
3. **Seed de desenvolvimento** — `SeedDataService.EnsureEmailConfigurationDevDefaultsAsync` aplica defaults e, se existir a variável `LIO_DEV_SMTP_PASSWORD`, grava a senha criptografada em banco novo.

## Testes realizados

### 1. Salvar configuração (`PUT /api/v1/admin/email/config`)

**Resultado:** sucesso — config persistida com `hasPassword: true`.

Ver arquivo: [`email-evidence/01-save-config.txt`](./email-evidence/01-save-config.txt)

### 2. Teste de conexão e envio (`POST /api/v1/admin/email/config/test`)

Payload incluiu `testRecipient: leonardo.mendes@liotecnica.com.br`.

**Resultado:**

```json
{
  "success": true,
  "message": "Conexao SMTP OK. E-mail de teste enviado para leonardo.mendes@liotecnica.com.br.",
  "detail": null
}
```

Ver arquivo: [`email-evidence/02-smtp-test-send.txt`](./email-evidence/02-smtp-test-send.txt)

### 2b. Teste para Gmail externo (`leonardomendes201704@gmail.com`)

**Data:** 2026-07-05 16:26:13 -03:00

**Resultado:**

```json
{
  "success": true,
  "message": "Conexao SMTP OK. E-mail de teste enviado para leonardomendes201704@gmail.com.",
  "detail": null
}
```

Ver arquivo: [`email-evidence/07-smtp-test-gmail.txt`](./email-evidence/07-smtp-test-gmail.txt)

### 3. Leitura da config (`GET /api/v1/admin/email/config`)

Senha **nunca** retorna na API — apenas `hasPassword: true`.

Ver arquivo: [`email-evidence/03-get-config-redacted.txt`](./email-evidence/03-get-config-redacted.txt)

### 4. Interface admin

Screenshots capturados via Playwright:

- [`email-evidence/04-email-config-page.png`](./email-evidence/04-email-config-page.png) — formulário com valores Office 365 carregados do banco
- [`email-evidence/06-email-hub-page.png`](./email-evidence/06-email-hub-page.png) — hub da fila de e-mail

Rota: `/admin/email/config`  
Hub: `/admin/email`

## Parâmetros de fila (defaults)

| Parâmetro | Valor |
|-----------|-------|
| `timeoutSeconds` | 30 |
| `maxAttempts` | 5 |
| `initialRetryDelaySeconds` | 60 |
| `maxRetryDelaySeconds` | 21600 (6 h) |
| `dispatchBatchSize` | 20 |
| `dispatchIntervalSeconds` | 30 |

## Reproduzir o setup em outro ambiente

### Via API (recomendado)

```powershell
$body = @{
  isEnabled = $true
  fromAddress = "leonardo.mendes@liotecnica.com.br"
  fromName = "LioConecta"
  smtpHost = "smtp.office365.com"
  smtpPort = 587
  smtpUsername = "leonardo.mendes@liotecnica.com.br"
  smtpPassword = "<senha>"
  useStartTls = $true
  timeoutSeconds = 30
  maxAttempts = 5
  initialRetryDelaySeconds = 60
  maxRetryDelaySeconds = 21600
  dispatchBatchSize = 20
  dispatchIntervalSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5148/api/v1/admin/email/config" `
  -Method Put -Body $body -ContentType "application/json"
```

Testar envio:

```powershell
$test = @{
  isEnabled = $true
  fromAddress = "leonardo.mendes@liotecnica.com.br"
  fromName = "LioConecta"
  smtpHost = "smtp.office365.com"
  smtpPort = 587
  smtpUsername = "leonardo.mendes@liotecnica.com.br"
  smtpPassword = "<senha>"
  useStartTls = $true
  timeoutSeconds = 30
  testRecipient = "leonardo.mendes@liotecnica.com.br"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5148/api/v1/admin/email/config/test" `
  -Method Post -Body $test -ContentType "application/json"
```

### Via seed (banco vazio)

Defina antes de subir a API:

```powershell
$env:LIO_DEV_SMTP_PASSWORD = "<senha>"
```

O seed preenche host/porta/usuário a partir de `EmailConfigurationDefaults` e criptografa a senha.

## Segurança

- **Nunca** commitar senha SMTP em `appsettings`, `.env` versionado ou código.
- A senha de teste informada pelo usuário foi persistida **somente** no PostgreSQL local (Docker, porta 5433).
- Rotacionar a senha temporária após validação em produção.

## Referências no código

| Arquivo | Função |
|---------|--------|
| `Infrastructure/Seed/EmailConfigurationDefaults.cs` | Constantes Office 365 |
| `Infrastructure/Services/EmailConfigurationService.cs` | CRUD + AES |
| `Infrastructure/Services/SmtpEmailSender.cs` | MailKit / STARTTLS + anexos |
| `Api/Controllers/AdminEmailController.cs` | Endpoints admin |
| `Api/Controllers/EmailController.cs` | `POST /email/send` e `POST /email/attachments` (usuários) |
| `FrontEnd/src/components/email/` | Modal reutilizável + TipTap |
| `FrontEnd/docs/email-compose-component.md` | Guia de integração do componente |
