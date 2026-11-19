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
    - Webroot ist eine Kopie von frontend/dist im Veröffentlichungs-Ordner, nicht frontend/dist selbst: ein späterer
      `npm run build` im Repo schöbe sonst dem laufenden, älteren Server ein neueres Frontend unter (B-533: die
      Bonus-Seite rief einen Endpunkt, den der Server noch nicht kannte). Und nicht backend/Pugling.Api/wwwroot:
      dort läge im Entwicklungsbetrieb ein veraltetes Frontend unter :5200.

    Firewall, feste Adresse und Energiesparen: docs/heimnetz.md.

.PARAMETER Port
    Port im Heimnetz. Vorgabe 5200 – derselbe wie im Entwicklungsbetrieb, damit nie zwei Server gleichzeitig
    in dieselbe Datenbank schreiben: Ist der Port belegt, bricht das Skript ab.

.PARAMETER Database
    Datenbankdatei. Vorgabe ist die echte backend/Pugling.Api/pugling.db. Fehlt die Datei, bricht das Skript ab,
    statt eine leere anzulegen. Vor jedem Start entsteht daneben eine Sicherung <datei>.bak-auto-<Zeitstempel>;
    die letzten 20 bleiben.

.PARAMETER SkipBuild
    Frontend und API nicht neu bauen (schneller Neustart ohne Code-Änderung).

.PARAMETER AllowUncommittedMigrations
    Startet gegen die echte Datenbank, obwohl unter backend/Pugling.Api/Data/Migrations etwas ungecommittet ist. Ohne
    den Schalter bricht das Skript dann ab: Der Start spielte eine Migration ein, an der noch gearbeitet wird.

.PARAMETER NoTailscale
    Die HTTPS-Adresse über Tailscale nicht einrichten. Ohne den Schalter richtet das Skript sie ein, sobald
    Tailscale installiert ist und der Port der Vorgabe entspricht (nur die echte Instanz bekommt die Adresse,
    nie eine Test-Instanz auf einer Kopie). Warum HTTPS: Nur dann installiert Chrome auf Android Pugling als
    eigene App, die Family Link auf „Immer erlaubt" stellen kann – docs/heimnetz.md.

.EXAMPLE
    ./scripts/heimnetz.ps1
#>
param(
    [int]$Port = 5200,
    [string]$Database,
    [switch]$SkipBuild,
    [switch]$NoTailscale,
    [switch]$AllowUncommittedMigrations
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiProject = Join-Path $repo 'backend\Pugling.Api'
$frontend = Join-Path $repo 'frontend'
$frontendDist = Join-Path $frontend 'dist'
$stateDir = Join-Path $env:LOCALAPPDATA 'Pugling'
# One folder per port: a second instance (a test on a copy of the database) must not publish over the files of the
# running server - Windows locks them, and the publish fails halfway.
$appDir = Join-Path $stateDir "heimnetz-app-$Port"
# The frontend is served from a copy next to the published API, never from frontend/dist: any later `npm run build`
# in the repo would otherwise swap a newer frontend under the running, older API (B-533: the bonus page called an
# endpoint the server did not have yet).
$webRoot = Join-Path $appDir 'wwwroot'
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

# 2a. Keine ungecommittete Migration in die echte Datenbank: Der Build nimmt den Arbeitsstand, wie er gerade ist, und
#     der Start spielt jede Migration darin ein. Eine Migration, an der eine Sitzung noch arbeitet, landete so am
#     2026-11-19 in pugling.db, bevor ihr Sprint abgenommen war - danach ist sie eingefroren, jede Korrektur muss als
#     neue Migration hinten an. Gilt nur für die echte Datenbank; eine Probe auf einer Kopie (-Database) darf das.
$realDatabase = [System.IO.Path]::GetFullPath((Join-Path $apiProject 'pugling.db'))
if (-not $SkipBuild -and -not $AllowUncommittedMigrations -and $Database -eq $realDatabase) {
    $pending = git -C $repo status --porcelain -- 'backend/Pugling.Api/Data/Migrations'
    if ($LASTEXITCODE -ne 0) { throw 'git status fehlgeschlagen – ohne ihn lässt sich nicht prüfen, ob eine Migration noch in Arbeit ist.' }
    if ($pending) {
        throw ("Ungecommittete Migration im Arbeitsstand – der Start spielte sie in die echte Datenbank ein:`n" +
            ($pending -join "`n") + "`nErst committen (dann ist sie abgenommen und eingefroren), " +
            "oder bewusst mit -AllowUncommittedMigrations starten.")
    }
}

# 2b. Sicherung vor jedem Start: Ab hier kann eine neue Migration die Datenbank umbauen, und sie trägt den
#     Lernstand der Kinder. Der Server läuft nicht (Port frei), die Dateien sind also ruhig. Ein -wal aus einem
#     unsauberen Ende wird mitkopiert, sonst fehlten der Sicherung die letzten Antworten.
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "$Database.bak-auto-$stamp"
Copy-Item $Database $backup
if (Test-Path "$Database-wal") { Copy-Item "$Database-wal" "$backup-wal" }
Write-Host "Sicherung: $backup"
# Nur die automatischen Sicherungen ausdünnen (die letzten 20 bleiben); von Hand angelegte bleiben unberührt.
Get-ChildItem -Path (Split-Path $Database) -Filter "$(Split-Path $Database -Leaf).bak-auto-*" |
    Where-Object { $_.Name -notlike '*-wal' } | Sort-Object Name -Descending | Select-Object -Skip 20 |
    ForEach-Object { Remove-Item $_.FullName, "$($_.FullName)-wal" -ErrorAction SilentlyContinue }

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

    if (Test-Path $webRoot) { Remove-Item -Recurse -Force $webRoot }
    Copy-Item -Recurse $frontendDist $webRoot
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

# 5b. HTTPS über Tailscale: `tailscale serve` terminiert TLS mit einem echten Zertifikat für <rechner>.<tailnet>.ts.net
#     und reicht an localhost:$Port weiter (die API wertet X-Forwarded-For/-Proto dieses Hops aus). Die Freigabe
#     bleibt in Tailscale gespeichert; läuft der Server nicht, antwortet die Adresse mit einem Fehler statt mit Pugling.
$tailscale = Get-Command tailscale -ErrorAction SilentlyContinue
if (-not $tailscale -and (Test-Path "$env:ProgramFiles\Tailscale\tailscale.exe")) {
    $tailscale = Get-Command "$env:ProgramFiles\Tailscale\tailscale.exe"
}
if ($NoTailscale) {
    Write-Host 'Tailscale: per -NoTailscale ausgelassen.'
} elseif (-not $tailscale) {
    Write-Host 'Tailscale: nicht installiert – keine HTTPS-Adresse (Einrichtung: docs/heimnetz.md).'
} elseif ($Port -ne 5200) {
    Write-Host "Tailscale: ausgelassen, Port $Port ist nicht die echte Instanz."
} else {
    & $tailscale.Source serve --bg $Port | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'tailscale serve fehlgeschlagen – HTTPS-Zertifikate in der Tailscale-Konsole aktiviert? (docs/heimnetz.md)'
    } else {
        $dnsName = ((& $tailscale.Source status --json | ConvertFrom-Json).Self.DNSName).TrimEnd('.')
        Write-Host 'Pugling über Tailscale (HTTPS, als App installierbar):'
        Write-Host "  Sohn:  https://$dnsName/sohn"
        Write-Host "  Vater: https://$dnsName/vater"
    }
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
