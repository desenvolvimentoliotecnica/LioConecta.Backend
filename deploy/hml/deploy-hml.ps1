#Requires -Version 5.1
# Deploy HML — mesma stack/portas do DEV, host 10.0.0.80
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DevDir = Resolve-Path (Join-Path $ScriptDir "..\dev")
$BackendRoot = Resolve-Path (Join-Path $ScriptDir "..\..")
$DeployConfigDir = Join-Path $BackendRoot ".deploy"
$ConfigFile = Join-Path $DeployConfigDir "hml.local.env"
function Get-EnvValue([string]$Content, [string]$Key) {
  foreach ($line in ($Content -split "`n")) {
    if ($line -match ("^\s*" + [regex]::Escape($Key) + "\s*=\s*(.*)$")) { return $Matches[1].Trim() }
  }
  return $null
}
if (-not (Test-Path $ConfigFile)) { throw "Config nao encontrada: $ConfigFile" }
$raw = Get-Content $ConfigFile -Raw
$hostName = Get-EnvValue $raw "DEPLOY_HOST"
$user = Get-EnvValue $raw "DEPLOY_USER"
$remoteDir = Get-EnvValue $raw "DEPLOY_REMOTE_DIR"
$sshKey = Get-EnvValue $raw "DEPLOY_SSH_KEY"
$httpPort = Get-EnvValue $raw "LIOSNECTA_HTTP_PORT"
$publicHost = Get-EnvValue $raw "LIOSNECTA_PUBLIC_HOST"
if (-not $publicHost) { $publicHost = $hostName }
$pgPass = Get-EnvValue $raw "POSTGRES_PASSWORD"
$frontRoot = Get-EnvValue $raw "FRONTEND_ROOT"
$backRoot = Get-EnvValue $raw "BACKEND_ROOT"
$staging = Join-Path $env:TEMP "lioconecta-hml-deploy-$(Get-Date -Format yyyyMMddHHmmss)"
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Write-Host "Staging: $staging"
Write-Host "Building frontend (HML)..."
Push-Location $frontRoot
$env:VITE_API_BASE_URL = "/api/v1"
$env:VITE_USE_MOCK = "false"
$env:VITE_OBSERVABILITY_ENABLED = "true"
$env:VITE_AZURE_CLIENT_ID = ""
$env:VITE_AZURE_TENANT_ID = ""
$env:VITE_COMPASS_APP_URL = "http://10.0.0.80:8094"
$env:VITE_UNILIO_APP_URL = "http://10.0.0.80:8096"
npm ci --silent 2>$null; if ($LASTEXITCODE -ne 0) { npm install --silent }
npm run build
if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }
Pop-Location
Write-Host "Preparing deploy bundle..."
Copy-Item -Recurse -Force (Join-Path $frontRoot "dist") (Join-Path $staging "frontend-dist")
Write-Host "Publishing backend locally..."
dotnet publish (Join-Path $backRoot "src\LioConecta.Api\LioConecta.Api.csproj") -c Release -o (Join-Path $staging "api-publish\publish") /p:ErrorOnDuplicatePublishOutputFiles=false
if ($LASTEXITCODE -ne 0) { throw "API publish failed" }
dotnet publish (Join-Path $backRoot "src\LioConecta.Workers\LioConecta.Workers.csproj") -c Release -o (Join-Path $staging "workers-publish\publish")
if ($LASTEXITCODE -ne 0) { throw "Workers publish failed" }
Copy-Item -Force (Join-Path $DevDir "Dockerfile.api.runtime") $staging
Copy-Item -Force (Join-Path $DevDir "Dockerfile.workers.runtime") $staging
Copy-Item -Force (Join-Path $DevDir "docker-compose.dev.yml") $staging
Copy-Item -Recurse -Force (Join-Path $DevDir "nginx") $staging
Copy-Item -Force (Join-Path $DevDir "scripts\seed-dev-settings.sh") (Join-Path $staging "seed-dev-settings.sh")
New-Item -ItemType Directory -Path (Join-Path $staging "scripts") -Force | Out-Null
foreach ($sh in @("seed-dev-glpi-settings.sh", "seed-dev-glpi-settings.sql", "fix-audit-events.sql", "apply-db-fixes.sh")) {
  $src = Join-Path $DevDir "scripts\$sh"
  $dst = Join-Path $staging "scripts\$sh"
  $text = [IO.File]::ReadAllText($src) -replace "`r`n", "`n"
  [IO.File]::WriteAllText($dst, $text)
}
$seedPath = Join-Path $staging "seed-dev-settings.sh"
[IO.File]::WriteAllText($seedPath, ([IO.File]::ReadAllText($seedPath) -replace "`r`n", "`n"))
Set-Content -Path (Join-Path $staging ".env") -Value "POSTGRES_PASSWORD=$pgPass`nLIOSNECTA_HTTP_PORT=$httpPort" -Encoding ASCII
$sshTarget = "$user@$hostName"
$sshArgs = @("-i", $sshKey, "-o", "StrictHostKeyChecking=accept-new", "-o", "BatchMode=yes")
Write-Host "Syncing to HML ($hostName)..."
ssh @sshArgs $sshTarget "mkdir -p $remoteDir/current"
scp @sshArgs -r "$staging/*" "${sshTarget}:$remoteDir/current/"
scp @sshArgs (Join-Path $staging ".env") "${sshTarget}:$remoteDir/current/.env"
Write-Host "Fixing frontend-dist permissions (before nginx)..."
ssh @sshArgs $sshTarget "chmod -R a+rX $remoteDir/current/frontend-dist; chmod +x $remoteDir/current/seed-dev-settings.sh $remoteDir/current/scripts/apply-db-fixes.sh $remoteDir/current/scripts/*.sh"
Write-Host "Starting containers (postgres/redis -> api -> nginx -> workers)..."
# Start API before workers to avoid concurrent MigrateAsync race on cold DB.
# Use single-line ssh commands (LF-safe); PowerShell here-strings send CRLF and break bash.
ssh @sshArgs $sshTarget "cd $remoteDir/current; export POSTGRES_PASSWORD='$pgPass'; export LIOSNECTA_HTTP_PORT='$httpPort'; docker compose -f docker-compose.dev.yml --env-file .env up -d --build postgres redis"
Start-Sleep -Seconds 8
ssh @sshArgs $sshTarget "cd $remoteDir/current; export POSTGRES_PASSWORD='$pgPass'; export LIOSNECTA_HTTP_PORT='$httpPort'; docker compose -f docker-compose.dev.yml --env-file .env up -d --build api"
ssh @sshArgs $sshTarget "cd $remoteDir/current; export POSTGRES_PASSWORD='$pgPass'; export LIOSNECTA_HTTP_PORT='$httpPort'; docker compose -f docker-compose.dev.yml --env-file .env up -d nginx"
Write-Host "Waiting for API health (migrations+seed)..."
$baseUrl = "http://${hostName}:$httpPort"
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
  Start-Sleep -Seconds 5
  try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing -TimeoutSec 10
    if ($resp.StatusCode -eq 200) { $ready = $true; break }
  } catch {}
  Write-Host "  ... aguardando API ($($i+1)/60)"
}
if (-not $ready) {
  ssh @sshArgs $sshTarget "cd $remoteDir/current; docker compose -f docker-compose.dev.yml logs api --tail=100"
  throw "Health check failed for $baseUrl/health/ready"
}
Write-Host "Starting workers..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; export POSTGRES_PASSWORD='$pgPass'; export LIOSNECTA_HTTP_PORT='$httpPort'; docker compose -f docker-compose.dev.yml --env-file .env up -d --build workers"

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
Write-Host "Seeding HML settings..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; LIOSNECTA_HTTP_PORT=$httpPort LIOSNECTA_PUBLIC_HOST=$publicHost bash seed-dev-settings.sh"
Write-Host "Migrating GLPI settings from local export..."
ssh @sshArgs $sshTarget "cd $remoteDir/current; chmod +x scripts/seed-dev-glpi-settings.sh; bash scripts/seed-dev-glpi-settings.sh"
ssh @sshArgs $sshTarget "cd $remoteDir/current; docker compose -f docker-compose.dev.yml restart api workers nginx"
$readyAfterRestart = $false
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Seconds 5
  try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing -TimeoutSec 10
    if ($resp.StatusCode -eq 200) { $readyAfterRestart = $true; break }
  } catch {}
  Write-Host "  ... aguardando pos-restart ($($i+1)/40)"
}
if (-not $readyAfterRestart) {
  throw "Health check failed after restart for $baseUrl/health/ready"
}
$health = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing
$readyHealth = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
$homePage = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
Write-Host ""
Write-Host "=== DEPLOY HML OK ==="
Write-Host "Frontend: $baseUrl/"
Write-Host "Backend API: $baseUrl/api/v1"
Write-Host "Health: $($health.Content)"
Write-Host "Ready: $($readyHealth.StatusCode) $($readyHealth.Content)"
Write-Host "Home HTTP: $($homePage.StatusCode)"
$evidenceDir = Join-Path $backRoot "deploy\hml\evidence"
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
@{ url = $baseUrl; frontend = "$baseUrl/"; api = "$baseUrl/api/v1"; health = $health.Content; ready = $readyHealth.Content; homeStatus = $homePage.StatusCode; timestamp = $ts } | ConvertTo-Json | Set-Content (Join-Path $evidenceDir "deploy-$ts.json")
Remove-Item -Recurse -Force $staging
Write-Host "Evidence: $(Join-Path $evidenceDir "deploy-$ts.json")"
