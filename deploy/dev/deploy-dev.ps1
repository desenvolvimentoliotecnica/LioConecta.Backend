#Requires -Version 5.1
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendRoot = Resolve-Path (Join-Path $ScriptDir "..\..")
$DeployConfigDir = Join-Path $BackendRoot ".deploy"
$ConfigFile = Join-Path $DeployConfigDir "dev.local.env"
function Get-EnvValue([string]$Content, [string]$Key) {
  foreach ($line in ($Content -split "`n")) {
    if ($line -match ("^\s*" + [regex]::Escape($Key) + "\s*=\s*(.*)$")) { return $Matches[1].Trim() }
  }
  return $null
}
if (-not (Test-Path $ConfigFile)) { throw "Config nao encontrada. Execute bootstrap-dev.ps1 primeiro." }
$raw = Get-Content $ConfigFile -Raw
$hostName = Get-EnvValue $raw "DEPLOY_HOST"
$user = Get-EnvValue $raw "DEPLOY_USER"
$remoteDir = Get-EnvValue $raw "DEPLOY_REMOTE_DIR"
$sshKey = Get-EnvValue $raw "DEPLOY_SSH_KEY"
$httpPort = Get-EnvValue $raw "LIOSNECTA_HTTP_PORT"
$pgPass = Get-EnvValue $raw "POSTGRES_PASSWORD"
$frontRoot = Get-EnvValue $raw "FRONTEND_ROOT"
$backRoot = Get-EnvValue $raw "BACKEND_ROOT"
$staging = Join-Path $env:TEMP "lioconecta-deploy-$(Get-Date -Format yyyyMMddHHmmss)"
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Write-Host "Staging: $staging"
Write-Host "Building frontend..."
Push-Location $frontRoot
$env:VITE_API_BASE_URL = "/api/v1"
$env:VITE_COMPASS_APP_URL = "http://10.0.0.79:8094"
$env:VITE_USE_MOCK = "false"
$env:VITE_OBSERVABILITY_ENABLED = "true"
$env:VITE_AZURE_CLIENT_ID = ""
$env:VITE_AZURE_TENANT_ID = ""
npm ci --silent 2>$null; if ($LASTEXITCODE -ne 0) { npm install --silent }
npm run build
if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }
Pop-Location
Write-Host "Preparing deploy bundle..."
Copy-Item -Recurse -Force (Join-Path $frontRoot "dist") (Join-Path $staging "frontend-dist")
$backendDest = Join-Path $staging "backend"
New-Item -ItemType Directory -Path $backendDest -Force | Out-Null
Write-Host "Publishing backend locally..."
dotnet publish (Join-Path $backRoot "src\LioConecta.Api\LioConecta.Api.csproj") -c Release -o (Join-Path $staging "api-publish\publish") /p:ErrorOnDuplicatePublishOutputFiles=false
if ($LASTEXITCODE -ne 0) { throw "API publish failed" }
dotnet publish (Join-Path $backRoot "src\LioConecta.Workers\LioConecta.Workers.csproj") -c Release -o (Join-Path $staging "workers-publish\publish")
if ($LASTEXITCODE -ne 0) { throw "Workers publish failed" }
Copy-Item -Force (Join-Path $ScriptDir "Dockerfile.api.runtime") $staging
Copy-Item -Force (Join-Path $ScriptDir "Dockerfile.workers.runtime") $staging
Copy-Item -Force (Join-Path $ScriptDir "docker-compose.dev.yml") $staging
Copy-Item -Recurse -Force (Join-Path $ScriptDir "nginx") $staging
Copy-Item -Force (Join-Path $ScriptDir "scripts\seed-dev-settings.sh") (Join-Path $staging "seed-dev-settings.sh")
New-Item -ItemType Directory -Path (Join-Path $staging "scripts") -Force | Out-Null
foreach ($sh in @("seed-dev-glpi-settings.sh", "seed-dev-glpi-settings.sql", "fix-audit-events.sql", "apply-db-fixes.sh")) {
  $src = Join-Path $ScriptDir "scripts\$sh"
  $dst = Join-Path $staging "scripts\$sh"
  $text = [IO.File]::ReadAllText($src) -replace "`r`n", "`n"
  [IO.File]::WriteAllText($dst, $text)
}
# Normalize LF for root seed script too
$seedPath = Join-Path $staging "seed-dev-settings.sh"
[IO.File]::WriteAllText($seedPath, ([IO.File]::ReadAllText($seedPath) -replace "`r`n", "`n"))
Set-Content -Path (Join-Path $staging ".env") -Value "POSTGRES_PASSWORD=$pgPass`nLIOSNECTA_HTTP_PORT=$httpPort" -Encoding ASCII
$sshTarget = "$user@$hostName"
$sshArgs = @("-i", $sshKey, "-o", "StrictHostKeyChecking=accept-new", "-o", "BatchMode=yes")
Write-Host "Syncing to server..."
ssh @sshArgs $sshTarget "mkdir -p $remoteDir/current"
scp @sshArgs -r "$staging/*" "${sshTarget}:$remoteDir/current/"
scp @sshArgs (Join-Path $staging ".env") "${sshTarget}:$remoteDir/current/.env"
# Permissions MUST be fixed before nginx starts serving frontend-dist.
# Otherwise nginx (non-root) hits Permission denied on index.html and returns 500
# (SPA try_files redirect cycle). Historical deploys also hung on a late chmod -R.
Write-Host "Fixing frontend-dist permissions (before nginx)..."
ssh @sshArgs $sshTarget "chmod -R a+rX $remoteDir/current/frontend-dist; chmod +x $remoteDir/current/seed-dev-settings.sh $remoteDir/current/scripts/apply-db-fixes.sh $remoteDir/current/scripts/*.sh"
Write-Host "Starting containers..."
$remoteCmd = "cd $remoteDir/current; export POSTGRES_PASSWORD='$pgPass'; export LIOSNECTA_HTTP_PORT='$httpPort'; docker compose -f docker-compose.dev.yml --env-file .env up -d --build --remove-orphans; docker compose -f docker-compose.dev.yml restart nginx"
ssh @sshArgs $sshTarget $remoteCmd
Write-Host "Waiting for health..."
$baseUrl = "http://${hostName}:$httpPort"
$ready = $false
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Seconds 5
  try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing -TimeoutSec 10
    if ($resp.StatusCode -eq 200) { $ready = $true; break }
  } catch {}
  Write-Host "  ... aguardando ($($i+1)/40)"
}
if (-not $ready) {
  ssh @sshArgs $sshTarget "cd $remoteDir/current; docker compose -f docker-compose.dev.yml logs --tail=80"
  throw "Health check failed for $baseUrl/health/ready"
}

Write-Host "Applying DB fixes..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; bash scripts/apply-db-fixes.sh"
Write-Host "Smoke test login..."
$loginBody = '{"email":"leonardo.mendes@liotecnica.com.br","password":"ChangeMe@2026"}'
try {
  $loginResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -TimeoutSec 20
  if (-not $loginResp.accessToken) { throw "Login smoke test: resposta sem accessToken" }
  Write-Host "Login smoke test OK (bootstrap)"
} catch {
  throw "Login smoke test failed: $_"
}
Write-Host "Seeding DEV settings..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; LIOSNECTA_HTTP_PORT=$httpPort bash seed-dev-settings.sh"
Write-Host "Migrating GLPI settings from local dev export..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; chmod +x scripts/seed-dev-glpi-settings.sh; bash scripts/seed-dev-glpi-settings.sh"
ssh @sshArgs $sshTarget "cd $remoteDir/current; docker compose -f docker-compose.dev.yml restart api workers nginx"
Start-Sleep -Seconds 10
$health = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing
$homePage = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
Write-Host ""
Write-Host "=== DEPLOY DEV OK ==="
Write-Host "URL: $baseUrl"
Write-Host "Health: $($health.Content)"
Write-Host "Home HTTP: $($homePage.StatusCode)"
$evidenceDir = Join-Path $backRoot "deploy\dev\evidence"
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
@{ url = $baseUrl; health = $health.Content; homeStatus = $homePage.StatusCode; timestamp = $ts } | ConvertTo-Json | Set-Content (Join-Path $evidenceDir "deploy-$ts.json")
Remove-Item -Recurse -Force $staging
Write-Host "Evidence: $(Join-Path $evidenceDir "deploy-$ts.json")"



