# Azure AD — LioConecta

Guia para configurar autenticação Azure AD / Entra ID com o backend e front-end LioConecta.

## App Registrations necessárias

### 1. LioConecta API (Web API)

| Campo | Valor |
|-------|-------|
| Nome | LioConecta API |
| Tipo | Web API |
| Application ID URI | `api://lioconecta` |
| Scope | `access_as_user` |

**Token configuration:** optional claims `email`, `preferred_username`.

### 2. LioConecta SPA (Single-page application)

| Campo | Valor |
|-------|-------|
| Nome | LioConecta Portal |
| Redirect URIs | `http://localhost:5173`, `https://lioconecta.liotecnica.com.br` |
| Logout URL | mesma origem do portal |

**API permissions:**
- LioConecta API → `access_as_user`
- Microsoft Graph → `User.Read`, `Calendars.Read`, `Tasks.Read`, `Sites.Read.All`, `Presence.Read.All`

## Backend (`appsettings.Production.json`)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-client-id>",
    "Audience": "api://lioconecta"
  },
  "Auth": {
    "UseDevAuth": false
  }
}
```

## Front-end (`.env.production`)

```
VITE_API_BASE_URL=https://api.lioconecta.liotecnica.com.br/api/v1
VITE_AZURE_CLIENT_ID=<spa-client-id>
VITE_AZURE_TENANT_ID=<tenant-id>
VITE_AZURE_API_SCOPE=api://lioconecta/access_as_user
VITE_USE_MOCK=false
```

## RBAC — grupos Azure AD

Mapear grupos AD para roles LioConecta via App Roles ou group claims:

| Role | Grupo AD sugerido |
|------|-------------------|
| HR | GRP-LioConecta-RH |
| TI | GRP-LioConecta-TI |
| Admin | GRP-LioConecta-Admin |
| KioskReader | GRP-LioConecta-Quiosque |

Configure **Token configuration → Groups claim** no App Registration da API.

## Desenvolvimento local

Deixe `AzureAd:ClientId` vazio no `appsettings.Development.json` — a API usa `DevAuthenticationHandler` (Maria Silva).

Header opcional para simular outro usuário:

```
X-Dev-User-Id: julio-schwartzman
```
