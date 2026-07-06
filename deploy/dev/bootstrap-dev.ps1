#Requires -Version 5.1
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendRoot = Resolve-Path (Join-Path $ScriptDir "..\..")
$DeployConfigDir = Join-Path $BackendRoot ".deploy"
$ConfigFile = Join-Path $DeployConfigDir "dev.local.env"
$ExampleFile = Join-Path $DeployConfigDir "dev.local.env.example"
function Get-EnvValue([string]$Content, [string]$Key) {
  foreach ($line in ($Content -split "`n")) {
    if ($line -match ("^\s*" + [regex]::Escape($Key) + "\s*=\s*(.*)$")) { return $Matches[1].Trim() }
  }
  return $null
}
if (-not (Test-Path $DeployConfigDir)) { New-Item -ItemType Directory -Path $DeployConfigDir | Out-Null }
$config = @{}
if (Test-Path $ConfigFile) {
  $raw = Get-Content $ConfigFile -Raw
  $config.Host = Get-EnvValue $raw "DEPLOY_HOST"
  $config.User = Get-EnvValue $raw "DEPLOY_USER"
  $config.RemoteDir = Get-EnvValue $raw "DEPLOY_REMOTE_DIR"
  $config.FrontEndRoot = Get-EnvValue $raw "FRONTEND_ROOT"
  $config.BackendRoot = Get-EnvValue $raw "BACKEND_ROOT"
  $config.Password = Get-EnvValue $raw "DEPLOY_PASSWORD"
} else {
  $config.Host = "10.0.0.79"
  $config.User = "administrator"
  $config.RemoteDir = "/home/administrator/lioconecta"
  $config.FrontEndRoot = "C:\Users\leonardo.mendes\Projects\LioConecta-FrontEnd"
  $config.BackendRoot = $BackendRoot.Path
  $config.Password = "New@20278622"
}
$sshKey = Join-Path $env:USERPROFILE ".ssh\lioconecta_dev"
$sshPub = "$sshKey.pub"
$sshDir = Split-Path $sshKey -Parent
if (-not (Test-Path $sshDir)) { New-Item -ItemType Directory -Path $sshDir | Out-Null }
if (-not (Test-Path $sshKey)) { ssh-keygen -t ed25519 -f $sshKey -N '""' -C "lioconecta-dev-deploy" | Out-Null; Write-Host "Chave SSH gerada: $sshKey" }
Import-Module Posh-SSH -ErrorAction Stop
$secPass = ConvertTo-SecureString $config.Password -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential ($config.User, $secPass)
Write-Host "Conectando em $($config.User)@$($config.Host)..."
$session = New-SSHSession -ComputerName $config.Host -Credential $cred -AcceptKey -Force
if (-not $session) { throw "Falha ao conectar via SSH" }
$pubKey = (Get-Content $sshPub -Raw).Trim()
Invoke-SSHCommand -SessionId $session.SessionId -Command "mkdir -p ~/.ssh; chmod 700 ~/.ssh" | Out-Null
$authCmd = "grep -qxF '$pubKey' ~/.ssh/authorized_keys 2>/dev/null || echo '$pubKey' >> ~/.ssh/authorized_keys; chmod 600 ~/.ssh/authorized_keys"
Invoke-SSHCommand -SessionId $session.SessionId -Command $authCmd | Out-Null
Write-Host "Chave SSH instalada no servidor."
$portCmd = 'for p in 8090 8091 8092 8093 8094 8088 9090; do if ! ss -tln 2>/dev/null | awk ''{print $4}'' | grep -q ":$p$"; then echo $p; exit 0; fi; done; echo 8090'
$portResult = Invoke-SSHCommand -SessionId $session.SessionId -Command $portCmd
$httpPort = ($portResult.Output | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1)
if (-not $httpPort) { $httpPort = "8090" }
Write-Host "Porta livre escolhida: $httpPort"
$dockerCheck = Invoke-SSHCommand -SessionId $session.SessionId -Command "docker --version; docker compose version"
if ($dockerCheck.ExitStatus -ne 0) { throw "Docker nao disponivel: $($dockerCheck.Error)" }
Write-Host $dockerCheck.Output
Invoke-SSHCommand -SessionId $session.SessionId -Command "sudo mkdir -p $($config.RemoteDir)/current; sudo chown -R $($config.User):$($config.User) $($config.RemoteDir)" | Out-Null
if ((Test-Path $ConfigFile) -and (Get-EnvValue (Get-Content $ConfigFile -Raw) "POSTGRES_PASSWORD")) {
  $pgPass = Get-EnvValue (Get-Content $ConfigFile -Raw) "POSTGRES_PASSWORD"
} else {
  $pgPass = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ })
}
$content = "DEPLOY_HOST=$($config.Host)`nDEPLOY_USER=$($config.User)`nDEPLOY_REMOTE_DIR=$($config.RemoteDir)`nDEPLOY_SSH_KEY=$sshKey`nLIOSNECTA_HTTP_PORT=$httpPort`nPOSTGRES_PASSWORD=$pgPass`nFRONTEND_ROOT=$($config.FrontEndRoot)`nBACKEND_ROOT=$($config.BackendRoot)"
Set-Content -Path $ConfigFile -Value $content -Encoding UTF8
Set-Content -Path $ExampleFile -Value ($content -replace 'POSTGRES_PASSWORD=.*','POSTGRES_PASSWORD=<gerado-no-bootstrap>') -Encoding UTF8
Remove-SSHSession -SessionId $session.SessionId | Out-Null
Write-Host "Bootstrap concluido. Config: $ConfigFile"
Write-Host "URL prevista: http://$($config.Host):$httpPort/"

