#requires -version 5.1
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Setup-Registration.ps1') -LibraryOnly

$json = '{"DetailedErrors":true,"Database":{"AssumeExternallyProvisioned":false},"Serilog":{"Properties":{"Application":"MUnique.OpenMU"}},"TestRegistration":{"OtherSetting":123}}'
$changed = Enable-RegistrationConfiguration -Json $json | ConvertFrom-Json
if (-not $changed.TestRegistration.Enabled -or -not $changed.TestRegistration.TrustLoopbackProxy) { throw 'Registration flags were not enabled.' }
if ($changed.TestRegistration.OtherSetting -ne 123 -or $changed.Serilog.Properties.Application -ne 'MUnique.OpenMU' -or $changed.Database.AssumeExternallyProvisioned) { throw 'Existing settings were changed.' }
$twice = Enable-RegistrationConfiguration -Json (Enable-RegistrationConfiguration -Json $json)
if ($twice -ne (Enable-RegistrationConfiguration -Json $json)) { throw 'Configuration editing is not repeatable.' }
$empty = Enable-RegistrationConfiguration -Json '{}' | ConvertFrom-Json
if (-not $empty.TestRegistration.Enabled) { throw 'New registration section was not added.' }
foreach ($invalid in @('null','[]','{"TestRegistration":null}','{"TestRegistration":true}')) {
    $rejected = $false
    try { $null = Enable-RegistrationConfiguration -Json $invalid } catch { $rejected = $true }
    if (-not $rejected) { throw 'Invalid server configuration was accepted.' }
}

$caddy = Join-Path $PSScriptRoot 'payload\caddy\caddy.exe'
$temporary = Join-Path $env:TEMP ('TwinMURegistrationValidation-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
$config = Join-Path $temporary 'Caddyfile'
$errors = Join-Path $temporary 'caddy.stderr'
try {
    $text = Get-GatewayConfiguration -Address '143.14.88.45' -Port 5000 -Root 'C:\ProgramData\TwinMURegistration' -Email 'test@example.com'
    [IO.File]::WriteAllText($config,$text)
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue' # Windows PowerShell treats native stderr warnings as error records.
        $adaptedText = (& $caddy adapt --config $config --adapter caddyfile 2>$errors | Out-String)
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previousPreference }
    if ($code -ne 0) { throw 'Caddy rejected the generated configuration.' }
    $adapted = $adaptedText | ConvertFrom-Json
    $policy = $adapted.apps.tls.automation.policies[0]
    if ($policy.subjects[0] -ne '143.14.88.45' -or $policy.issuers[0].module -ne 'acme' -or $policy.issuers[0].profile -ne 'shortlived' -or -not $policy.issuers[0].challenges.http.disabled) { throw 'Public IP certificate automation is not configured correctly.' }
    if ($adaptedText -notmatch '127.0.0.1:5000' -or $adaptedText -notmatch '/api/accounts/test-registration') { throw 'Registration upstream or route missing.' }
    if ($adaptedText -match '"module"\s*:\s*"internal"') { throw 'A locally trusted certificate issuer was selected.' }
} finally {
    # Remove only the exact files created for this validation, with no recursive deletion.
    foreach ($file in @($config,$errors)) { if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file } }
    Remove-Item -LiteralPath $temporary
}
Write-Output 'PASS: Windows PowerShell 5.1 configuration preservation, repeated runs, invalid inputs, and Caddy public-IP certificate/upstream configuration.'
Write-Output 'No gateway, certificate, scheduled task or firewall rule was installed by these tests.'
