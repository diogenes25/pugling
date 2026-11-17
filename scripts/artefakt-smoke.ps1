#requires -Version 7
<#
.SYNOPSIS
    Startet das veröffentlichte Artefakt (API + gebautes Frontend) gegen eine Wegwerf-DB und prüft, ob es antwortet (B-47).

.DESCRIPTION
    Baut das Frontend, veröffentlicht die API in einen Wegwerf-Ordner, kopiert `frontend/dist` nach `<ordner>/wwwroot` und
    startet die App in derselben Form wie `heimnetz.ps1` – Production, eigene SQLite-Datei, zufälliger `Jwt:Key`, kein Seed,
    nur auf 127.0.0.1. Dann drei Prüfungen: `/health` → 200, `/` und `/sohn` → 200 mit `<div id="root">` (zwei
    verschiedene Wege: Standarddatei und SPA-Rückfall). Prozess und Ordner werden in jedem Fall entfernt.

    Läuft lokal (Windows) und in CI (Linux) – darum keine Windows-only-Cmdlets und kein fester Pfad unter /tmp.
    Die echte Instanz (`heimnetz.ps1`, :5200, echte pugling.db) berührt das Skript nie.

.PARAMETER Port
    Port des Probestarts; Vorgabe 5299 (unüblich, nie 5200).
.PARAMETER SkipBuild
    Frontend-Build überspringen und ein vorhandenes `frontend/dist` nehmen (für die Gegenprobe nach einem grünen Lauf).
.PARAMETER EmptyWebRoot
    Gegenprobe: `wwwroot` bleibt leer – der Lauf muss an der Seitenprüfung rot enden, nicht an `/health`.

.EXAMPLE
    pwsh scripts/artefakt-smoke.ps1
.EXAMPLE
    pwsh scripts/artefakt-smoke.ps1 -SkipBuild -EmptyWebRoot   # muss mit Exit 2 enden (rot an der Seitenprüfung)
#>
param(
    [int]$Port = 5299,
    [switch]$SkipBuild,
    [switch]$EmptyWebRoot
)

$ErrorActionPreference = 'Stop'
if ($Port -eq 5200) { throw 'Port 5200 gehört der echten Instanz (heimnetz.ps1) – einen anderen wählen.' }

$repo = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $repo 'frontend'
$frontendDist = Join-Path $frontend 'dist'
$apiProject = Join-Path $repo 'backend' 'Pugling.Api'
$work = Join-Path ([IO.Path]::GetTempPath()) ("pugling-artefakt-" + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$appDir = Join-Path $work 'app'
$webRoot = Join-Path $appDir 'wwwroot'
$database = Join-Path $work 'smoke.db'
$log = Join-Path $work 'app.log'
$base = "http://127.0.0.1:$Port"

# Belegter Port: sofort rot mit klarer Meldung, statt in der Retry-Schleife auf eine fremde App zu warten.
$probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
try { $probe.Start() } catch { throw "Port $Port ist belegt – mit -Port einen freien wählen." } finally { $probe.Stop() }

$process = $null
# Restored in `finally`: run from an open prompt, the variables would otherwise point a later `dotnet run` at a deleted DB.
$envNames = 'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS', 'ConnectionStrings__Default', 'Jwt__Key', 'Seed__Enabled'
$savedEnv = @{}; foreach ($n in $envNames) { $savedEnv[$n] = [Environment]::GetEnvironmentVariable($n) }
$pageCheckFailed = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null

    if (-not $SkipBuild -and -not $EmptyWebRoot) {
        Write-Host 'Baue das Frontend …'
        Push-Location $frontend
        try {
            npm ci --no-audit --no-fund
            if ($LASTEXITCODE -ne 0) { throw 'npm ci fehlgeschlagen.' }
            npm run build
            if ($LASTEXITCODE -ne 0) { throw 'Frontend-Build fehlgeschlagen.' }
        } finally { Pop-Location }
    }

    Write-Host 'Veröffentliche die API …'
    dotnet publish $apiProject -c Release -o $appDir --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish fehlgeschlagen.' }

    if (Test-Path $webRoot) { Remove-Item -Recurse -Force $webRoot }
    New-Item -ItemType Directory -Path $webRoot | Out-Null
    if (-not $EmptyWebRoot) {
        if (-not (Test-Path (Join-Path $frontendDist 'index.html'))) { throw "Kein gebautes Frontend in '$frontendDist'." }
        Copy-Item -Recurse -Path (Join-Path $frontendDist '*') -Destination $webRoot
    }

    # Dieselbe Umgebung wie der echte Betrieb (heimnetz.ps1), nur mit fremder DB und zufälligem Schlüssel.
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ASPNETCORE_URLS = $base
    $env:ConnectionStrings__Default = "Data Source=$database"
    $env:Jwt__Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $env:Seed__Enabled = 'false'

    Write-Host "Starte das Artefakt auf $base …"
    $process = Start-Process -FilePath 'dotnet' -PassThru -NoNewWindow -RedirectStandardOutput $log `
        -RedirectStandardError "$log.err" -WorkingDirectory $appDir `
        -ArgumentList @((Join-Path $appDir 'Pugling.Api.dll'), '--contentRoot', $appDir, '--webroot', $webRoot)

    # Bereitschaft per Retry gegen /health, mit Frist – und mit einem Blick auf den Prozess: ein Fail-fast (etwa ohne
    # Jwt:Key) soll als solcher gemeldet werden, nicht als Timeout.
    $deadline = (Get-Date).AddSeconds(60)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) {
            $tail = (Get-Content -Tail 20 -ErrorAction SilentlyContinue $log, "$log.err") -join "`n"
            throw "Das Artefakt hat sich beendet (Exit $($process.ExitCode)), bevor /health antwortete:`n$tail"
        }
        try {
            $health = Invoke-WebRequest -Uri "$base/health" -TimeoutSec 3 -SkipHttpErrorCheck
            if ($health.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw '/health hat in 60 s nicht mit 200 geantwortet.' }
    Write-Host '/health: 200'

    foreach ($path in '/', '/sohn') {
        $page = Invoke-WebRequest -Uri "$base$path" -TimeoutSec 10 -SkipHttpErrorCheck
        $pageCheckFailed = $true
        if ($page.StatusCode -ne 200) { throw "$path antwortete mit $($page.StatusCode) statt 200 – fehlt das gebaute Frontend in wwwroot?" }
        if ($page.Content -notmatch '<div id="root">') { throw "$path liefert kein gebautes Frontend (kein <div id=""root"">)." }
        $pageCheckFailed = $false
        Write-Host "${path}: 200 mit dem gebauten Frontend"
    }
    Write-Host 'Artefakt-Smoke grün.'
}
catch {
    Write-Host "Artefakt-Smoke rot: $($_.Exception.Message)"
    # Exit 2 = rot an der Seitenprüfung (das, was die Gegenprobe mit -EmptyWebRoot sehen muss), 1 = alles andere.
    exit ($pageCheckFailed ? 2 : 1)
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(10000) | Out-Null
    }
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $work
    foreach ($n in $envNames) { [Environment]::SetEnvironmentVariable($n, $savedEnv[$n]) }
}
exit 0
