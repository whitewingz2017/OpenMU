#requires -version 5.1
[CmdletBinding()]
param(
    [string]$ServerDirectory,
    [string]$ContactEmail,
    [string]$PublicIp = '143.14.88.45',
    [ValidateRange(1,65535)][int]$BackendPort = 5000,
    [switch]$PlanOnly,
    [switch]$LibraryOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-GatewayConfiguration {
    param([string]$Address, [int]$Port, [string]$Root, [string]$Email)
    $parsed = $null
    if (-not [Net.IPAddress]::TryParse($Address, [ref]$parsed) -or $parsed.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) {
        throw 'PublicIp must be an IPv4 address.'
    }
    if ($Port -lt 1 -or $Port -gt 65535) { throw 'Invalid backend port.' }
    $mail = New-Object Net.Mail.MailAddress($Email)
    if ($mail.Address -ne $Email -or $Email -match '[\s{}"\\]') { throw 'Enter a plain email address.' }
    if ($Root -match '[\r\n"]') { throw 'Invalid installation path.' }
    $caddyRoot = $Root.Replace('\','/')
    return @"
{
    admin off
    auto_https disable_redirects
    storage file_system "$caddyRoot/certificates"
    log default {
        output file "$caddyRoot/gateway.log" {
            roll_size 10MiB
            roll_keep 5
        }
    }
    servers {
        protocols h1 h2
    }
}

https://$Address {
    tls {
        issuer acme {
            dir https://acme-v02.api.letsencrypt.org/directory
            email $Email
            profile shortlived
            disable_http_challenge
        }
    }
    @registration path /api/accounts/test-registration
    handle @registration {
        reverse_proxy http://127.0.0.1:$Port
    }
    handle {
        respond "Not found" 404
    }
}
"@
}

function Enable-RegistrationConfiguration {
    param([string]$Json)
    $settings = $Json | ConvertFrom-Json
    if ($null -eq $settings -or $settings -is [array] -or $settings -isnot [pscustomobject]) { throw 'appsettings.json must contain a JSON object.' }
    if (-not $settings.PSObject.Properties['TestRegistration']) {
        $settings | Add-Member NoteProperty TestRegistration ([pscustomobject]@{})
    }
    if ($null -eq $settings.TestRegistration -or $settings.TestRegistration -isnot [pscustomobject]) {
        throw 'TestRegistration must be a JSON object.'
    }
    $settings.TestRegistration | Add-Member NoteProperty Enabled $true -Force
    $settings.TestRegistration | Add-Member NoteProperty TrustLoopbackProxy $true -Force
    return ($settings | ConvertTo-Json -Depth 100)
}

function Get-HttpStatus {
    param([string]$Uri)
    try {
        return [int](Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 10 -MaximumRedirection 0).StatusCode
    } catch {
        $responseProperty = $_.Exception.PSObject.Properties['Response']
        if ($responseProperty -and $null -ne $responseProperty.Value) { return [int]$responseProperty.Value.StatusCode }
        return 0
    }
}

if ($LibraryOnly) { return }

$installRoot = Join-Path $env:ProgramData 'TwinMURegistration'
$taskName = 'TwinMU-RegistrationGateway'
$registrationUrl = "https://$PublicIp/api/accounts/test-registration"

if ($PlanOnly) {
    Write-Output "PLAN ONLY: no files, services, firewall rules or tasks will be changed."
    Write-Output "Backend: http://127.0.0.1:$BackendPort/api/accounts/test-registration"
    Write-Output "Client URL: $registrationUrl"
    Write-Output "Install folder: $installRoot"
    Write-Output 'Back up the selected server appsettings.json and AdminPanel DLL; install the bundled proxy-aware DLL.'
    Write-Output 'Enable TestRegistration.Enabled and TestRegistration.TrustLoopbackProxy, preserving other settings.'
    Write-Output 'Install Caddy as a startup task; open TCP 443 for that executable; obtain/renew a public IP certificate.'
    Write-Output 'IIS, port 8088, the database, and game server ports are not reconfigured.'
    exit 0
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Right-click Setup-Registration.bat and choose Run as administrator ON THE VPS.'
}
if (-not [Environment]::Is64BitOperatingSystem) { throw 'This package requires 64-bit Windows.' }

Write-Host 'TwinMU registration setup for your VPS'
Write-Host "Public HTTPS address: $registrationUrl"
Write-Host 'Your existing IIS website on port 8088 is kept as-is.'
Write-Host "TCP 443 must reach this VPS directly, including through your provider firewall."
Write-Host "Caddy obtains a free public certificate from Let's Encrypt and automatically renews it."
Write-Host "CA terms: https://letsencrypt.org/repository/"
if (-not $ServerDirectory) { $ServerDirectory = Read-Host 'Full folder containing your PUBLISHED OpenMU appsettings.json and DLLs' }
$ServerDirectory = (Resolve-Path -LiteralPath $ServerDirectory.Trim('"')).Path
$configPath = Join-Path $ServerDirectory 'appsettings.json'
$serverDll = Join-Path $ServerDirectory 'MUnique.OpenMU.Web.AdminPanel.dll'
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf) -or -not (Test-Path -LiteralPath $serverDll -PathType Leaf)) {
    throw 'Select the published server folder containing BOTH appsettings.json and MUnique.OpenMU.Web.AdminPanel.dll.'
}
if (-not $ContactEmail) { $ContactEmail = Read-Host 'Email address for the certificate account' }
$configurationText = Get-GatewayConfiguration -Address $PublicIp -Port $BackendPort -Root $installRoot -Email $ContactEmail
$newSettings = Enable-RegistrationConfiguration -Json ([IO.File]::ReadAllText($configPath))

# Validate all bundled executables before privileged installation.
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'payload-manifest.json') -Raw | ConvertFrom-Json
foreach ($file in $manifest.Files) {
    $payloadFile = Join-Path $PSScriptRoot $file.Path
    if ((Get-FileHash -LiteralPath $payloadFile -Algorithm SHA256).Hash -ne $file.Sha256) { throw "Package integrity check failed: $($file.Path)" }
}
$patchDll = Join-Path $PSScriptRoot 'payload\MUnique.OpenMU.Web.AdminPanel.dll'
$oldIdentity = [Reflection.AssemblyName]::GetAssemblyName($serverDll)
$newIdentity = [Reflection.AssemblyName]::GetAssemblyName($patchDll)
if ($oldIdentity.Name -ne $newIdentity.Name -or $oldIdentity.Version -ne $newIdentity.Version) {
    throw 'Server assembly version differs from this patch. Publish the updated server source instead of applying this DLL.'
}
foreach ($reference in $manifest.ServerReferences) {
    $dependency = Join-Path $ServerDirectory ($reference.Name + '.dll')
    if (-not (Test-Path -LiteralPath $dependency) -or [Reflection.AssemblyName]::GetAssemblyName($dependency).Version.ToString() -ne $reference.Version) {
        throw "Server dependency differs: $($reference.Name). Publish the updated server source instead."
    }
}

$oldTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
$installedCaddy = Join-Path $installRoot 'caddy.exe'
if ($oldTask -and @($oldTask.Actions | Where-Object { $_.Execute -eq $installedCaddy }).Count -ne 1) {
    throw 'A different scheduled task already uses this name. No existing task was changed.'
}
$listeners = @(Get-NetTCPConnection -State Listen -LocalPort 443 -ErrorAction SilentlyContinue)
foreach ($listener in $listeners) {
    $owner = Get-Process -Id $listener.OwningProcess -ErrorAction Stop
    if (-not $oldTask -or $owner.Path -ne $installedCaddy) {
        throw "TCP 443 is already used by $($owner.ProcessName). No IIS binding or process was stopped."
    }
}
if ((Test-Path -LiteralPath $installRoot) -and -not (Test-Path -LiteralPath (Join-Path $installRoot 'installed-by-twinmu.txt'))) {
    throw "The installation folder already exists without a setup marker: $installRoot"
}

Write-Host ''
Write-Host 'Stop the OpenMU SERVER now so its AdminPanel DLL can be updated. IIS can keep running.'
[void](Read-Host 'Press Enter after stopping OpenMU to install the gateway and accept the CA terms')
$lock = [IO.File]::Open($serverDll,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$lock.Dispose()

New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Set-Content -LiteralPath (Join-Path $installRoot 'installed-by-twinmu.txt') -Value 'TwinMU registration gateway'
# The SYSTEM startup task must not execute binaries writable by normal users.
& icacls.exe $installRoot '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Unable to protect the gateway installation directory.' }
if ($oldTask) {
    Stop-ScheduledTask -TaskName $taskName
    for ($attempt=0; $attempt -lt 20; $attempt++) {
        if (-not (Get-NetTCPConnection -State Listen -LocalPort 443 -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Milliseconds 500
    }
    if (Get-NetTCPConnection -State Listen -LocalPort 443 -ErrorAction SilentlyContinue) { throw 'The previous gateway has not stopped.' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'payload\caddy\caddy.exe') -Destination $installedCaddy -Force
$gatewayFile = Join-Path $installRoot 'Caddyfile'
[IO.File]::WriteAllText($gatewayFile,$configurationText,(New-Object Text.UTF8Encoding($false)))
$previousPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue' # Caddy emits informational messages to stderr even on success.
    & $installedCaddy validate --config $gatewayFile --adapter caddyfile 2>(Join-Path $installRoot 'validation.log')
    $validationCode = $LASTEXITCODE
} finally { $ErrorActionPreference = $previousPreference }
if ($validationCode -ne 0) { throw 'Gateway configuration validation failed; server files were not changed.' }

$backup = Join-Path $installRoot ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $configPath -Destination (Join-Path $backup 'appsettings.json')
Copy-Item -LiteralPath $serverDll -Destination (Join-Path $backup 'MUnique.OpenMU.Web.AdminPanel.dll')
try {
    Copy-Item -LiteralPath $patchDll -Destination $serverDll -Force
    [IO.File]::WriteAllText($configPath,$newSettings,(New-Object Text.UTF8Encoding($false)))
} catch {
    Copy-Item -LiteralPath (Join-Path $backup 'MUnique.OpenMU.Web.AdminPanel.dll') -Destination $serverDll -Force
    Copy-Item -LiteralPath (Join-Path $backup 'appsettings.json') -Destination $configPath -Force
    throw
}

$firewallName = 'TwinMU-Registration-HTTPS'
$existingRule = Get-NetFirewallRule -Name $firewallName -ErrorAction SilentlyContinue
if ($existingRule) { Remove-NetFirewallRule -Name $firewallName }
New-NetFirewallRule -Name $firewallName -DisplayName 'TwinMU Registration HTTPS (Caddy)' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443 -Program $installedCaddy -Profile Any | Out-Null
$action = New-ScheduledTaskAction -Execute $installedCaddy -Argument ('run --config "' + $gatewayFile + '" --adapter caddyfile') -WorkingDirectory $installRoot
$trigger = New-ScheduledTaskTrigger -AtStartup
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable -MultipleInstances IgnoreNew
$taskPrincipal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $taskPrincipal -Force | Out-Null
Start-ScheduledTask -TaskName $taskName

$clientSettings = @{MuOnlineSettings=@{RegistrationUrl=$registrationUrl}} | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $installRoot 'appsettings.client-registration.json'),$clientSettings,(New-Object Text.UTF8Encoding($false)))
Write-Host "Backups saved to: $backup"
Write-Host 'Start OpenMU again using your normal server startup method.'
[void](Read-Host 'Press Enter after the admin panel on port 5000 is running again')
$backendStatus = Get-HttpStatus -Uri "http://127.0.0.1:$BackendPort/api/accounts/test-registration"
if ($backendStatus -ne 405) { throw "Backend verification returned $backendStatus, expected 405. Check that OpenMU restarted on port $BackendPort. Gateway is installed; do not submit account details until verification passes." }

Write-Host 'Waiting up to 3 minutes for public certificate issuance and HTTPS verification...'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$verified = $false
for ($attempt=0; $attempt -lt 18; $attempt++) {
    if ((Get-HttpStatus -Uri $registrationUrl) -eq 405) { $verified=$true; break }
    Start-Sleep -Seconds 5
}
if (-not $verified) {
    Write-Host "Gateway installed, but HTTPS is NOT verified. Check inbound TCP 443 in the VPS provider firewall and $installRoot\gateway.log."
    Write-Host 'Keep the gateway task running; it retries certificate issuance and handles renewals.'
    exit 2
}
Write-Host ''
Write-Host "SUCCESS: trusted HTTPS and backend routing verified: $registrationUrl"
Write-Host 'Set MuOnlineSettings.RegistrationUrl to that address in the CLIENT configuration and rebuild your client.'
Write-Host "Client configuration example: $installRoot\appsettings.client-registration.json"
Write-Host 'The browser still shows 405; Submit in the game sends the POST request that creates an account.'
