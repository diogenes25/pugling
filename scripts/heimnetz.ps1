<#
.SYNOPSIS
    Startet Pugling für das Heimnetz: Handy und Rechner im selben WLAN erreichen die App unter einer Adresse (B-468).

.DESCRIPTION
    Baut das Frontend und veröffentlicht die API in einen eigenen Ordner, dann startet die API in der Umgebung
    "Production", gebunden an alle Netzwerkadressen des Rechners. Die API liefert das gebaute Frontend selbst aus
    (/sohn, /vater) – eine Adresse, ein Port, kein Vite-Server.

    Warum so:
    - Production: kein Seed gegen die echte Datenbank, keine Entwickler-Lesesicht auf Anmerkungen im WLAN.
      Der JWT-Schlüssel entsteht beim ersten Start und liegt außerhalb des Repos (%LOCALAPPDATA%\Pugling\jwt.key).
    - Eigener Veröffentlichungs-Ordner: Ein laufender Heimnetz-Server sperrt sonst die Build-Ausgabe, die
      Entwicklung und Test-Tor brauchen.
    - Inhaltswurzel bleibt backend/Pugling.Api: dieselbe appsettings.json und dieselben hochgeladenen Bilder wie
      im Entwicklungsbetrieb.
    - Webroot ist frontend/dist, keine Kopie nach wwwroot: sonst läge im Entwicklungsbetrieb ein veraltetes
      Frontend unter :5200.

    Firewall, feste Adresse und Energiesparen: docs/heimnetz.md.

.PARAMETER Port
    Port im Heimnetz. Vorgabe 5200 – derselbe wie im Entwicklungsbetrieb, damit nie zwei Server gleichzeitig
    in dieselbe Datenbank schreiben: Ist der Port belegt, bricht das Skript ab.

.PARAMETER Database
    Datenbankdatei. Vorgabe ist die echte backend/Pugling.Api/pugling.db. Fehlt die Datei, bricht das Skript ab,
    statt eine leere anzulegen.

.PARAMETER SkipBuild
    Frontend und API nicht neu bauen (schneller Neustart ohne Code-Änderung).

.EXAMPLE
    ./scripts/heimnetz.ps1
#>
param(
    [int]$Port = 5200,
    [string]$Database,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiProject = Join-Path $repo 'backend\Pugling.Api'
$frontend = Join-Path $repo 'frontend'
$webRoot = Join-Path $frontend 'dist'
$stateDir = Join-Path $env:LOCALAPPDATA 'Pugling'
# One folder per port: a second instance (a test on a copy of the database) must not publish over the files of the
# running server - Windows locks them, and the publish fails halfway.
$appDir = Join-Path $stateDir "heimnetz-app-$Port"
$keyFile = Join-Path $stateDir 'jwt.key'
if (-not $Database) { $Database = Join-Path $apiProject 'pugling.db' }
$Database = [System.IO.Path]::GetFullPath($Database)

# 1. Zwei Server auf derselben Datenbank wären zwei Schreiber ohne gemeinsame Sperre.
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
    throw "Port $Port ist belegt – läuft noch ein Entwicklungsserver? Erst beenden."
}

# 2. Eine fehlende Datei würde die Migration still als leere Datenbank anlegen.
if (-not (Test-Path $Database)) {
    throw "Datenbank '$Database' nicht gefunden."
}

# 3. Eigener Signierschlüssel: Production verweigert den Entwicklungs-Schlüssel.
New-Item -ItemType Directory -Force $stateDir | Out-Null
if (-not (Test-Path $keyFile)) {
    $bytes = New-Object byte[] 64
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    Set-Content -Path $keyFile -Value ([Convert]::ToBase64String($bytes)) -NoNewline -Encoding ascii
    Write-Host "Neuer JWT-Schlüssel in $keyFile angelegt."
}
$jwtKey = (Get-Content -Raw $keyFile).Trim()

# 4. Bauen.
if (-not $SkipBuild) {
    Write-Host 'Baue das Frontend …'
    Push-Location $frontend
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw 'Frontend-Build fehlgeschlagen.' }
    } finally { Pop-Location }

    Write-Host 'Veröffentliche die API …'
    dotnet publish $apiProject -c Release -o $appDir --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish fehlgeschlagen.' }
}
if (-not (Test-Path (Join-Path $webRoot 'index.html'))) { throw "Kein gebautes Frontend in '$webRoot' – ohne -SkipBuild starten." }
if (-not (Test-Path (Join-Path $appDir 'Pugling.Api.dll'))) { throw "Keine veröffentlichte API in '$appDir' – ohne -SkipBuild starten." }

# 5. Adressen, unter denen das Handy die App findet: nur Netze mit Standard-Gateway, also WLAN/LAN zum Router –
#    virtuelle Adapter (VirtualBox, Hyper-V, VPN) hätten sonst gleichrangig in der Liste gestanden.
$addresses = Get-NetIPConfiguration -ErrorAction SilentlyContinue |
    Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } |
    ForEach-Object { $_.IPv4Address.IPAddress }
if (-not $addresses) {
    $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
        Select-Object -ExpandProperty IPAddress
}
Write-Host ''
Write-Host "Datenbank: $Database"
Write-Host 'Pugling im Heimnetz:'
foreach ($ip in $addresses) {
    Write-Host "  Sohn:  http://${ip}:$Port/sohn"
    Write-Host "  Vater: http://${ip}:$Port/vater"
}
Write-Host 'Beenden mit Strg+C.'
Write-Host ''

# 6. Starten. Umgebungsvariablen nur für diesen Prozess-Baum; die Inhaltswurzel zeigt auf das Projekt.
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = "http://0.0.0.0:$Port"
$env:ConnectionStrings__Default = "Data Source=$Database"
$env:Jwt__Key = $jwtKey
$env:Seed__Enabled = 'false'
Push-Location $apiProject
try {
    dotnet (Join-Path $appDir 'Pugling.Api.dll') --contentRoot $apiProject --webroot $webRoot
} finally { Pop-Location }
