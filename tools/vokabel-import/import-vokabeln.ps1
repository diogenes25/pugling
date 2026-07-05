#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fuellt den Pugling-Vokabel-Store ueber den Batch-Endpunkt aus JSON-Dateien.

.DESCRIPTION
    Kleines Hilfsprogramm: loggt sich als Vater ein (PIN-Login -> JWT) und schiebt die
    Vokabeln aus einer oder mehreren JSON-Dateien per POST /api/v1/learn/vocabulary/batch
    in den Store. Der Batch ist idempotent: Eintraege mit bereits vorhandenem 'key' kommen
    als Status 'existing' zurueck (kein Fehler), das Skript ist also gefahrlos wiederholbar.

    Die JSON-Dateien liegen im Unterverzeichnis .\data (en-de-top100.json, fr-de-top100.json)
    und folgen exakt dem CreateVocabularyDto-Schema (siehe README.md).

.PARAMETER BaseUrl
    Basis-URL der laufenden API. Default: http://localhost:5200

.PARAMETER FatherId
    Vater-Id fuer den Login. Default: 1

.PARAMETER Pin
    PIN fuer den Login. Default: 1111

.PARAMETER Files
    Ein oder mehrere JSON-Dateien. Default: beide Listen unter .\data

.PARAMETER Tags
    Optionale Tag-Namen, die jeder importierten Vokabel mitgegeben werden
    (create-if-missing), z. B. "Top 100 Englisch".

.EXAMPLE
    ./import-vokabeln.ps1
    Importiert beide Standard-Listen gegen localhost mit Vater 1 / PIN 1111.

.EXAMPLE
    ./import-vokabeln.ps1 -Files ./data/en-de-top100.json -Tags "Top 100 Englisch"
    Importiert nur die Englisch-Liste und taggt sie.
#>
[CmdletBinding()]
param(
    [string]   $BaseUrl  = "http://localhost:5200",
    [int]      $FatherId = 1,
    [string]   $Pin      = "0000",   # Vater 1 = 'Papa' (Seed). Achtung: 1111 ist die SOHN-PIN.
    [string[]] $Files,
    [string[]] $Tags
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Default: beide Listen aus dem eigenen data-Verzeichnis.
if (-not $Files -or $Files.Count -eq 0) {
    $Files = @(
        (Join-Path $scriptDir "data/en-de-top100.json"),
        (Join-Path $scriptDir "data/fr-de-top100.json")
    )
}

# --- 1) Login (Vater) -> JWT ---------------------------------------------------
Write-Host "Login als Vater $FatherId gegen $BaseUrl ..." -ForegroundColor Cyan
try {
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/auth/father" `
        -ContentType "application/json" `
        -Body (@{ fatherId = $FatherId; pin = $Pin } | ConvertTo-Json)
}
catch {
    throw "Login fehlgeschlagen ($($_.Exception.Message)). Laeuft das Backend? Stimmen Id/PIN?"
}
$headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "  OK - eingeloggt als '$($login.name)' (Rolle $($login.role))." -ForegroundColor Green

# --- 2) Dateien einlesen + optional taggen -------------------------------------
$batch = [System.Collections.Generic.List[object]]::new()
foreach ($file in $Files) {
    if (-not (Test-Path $file)) { throw "Datei nicht gefunden: $file" }
    $items = Get-Content -Raw -Path $file | ConvertFrom-Json
    Write-Host "Lese $($items.Count) Vokabeln aus $([System.IO.Path]::GetFileName($file)) ..." -ForegroundColor Cyan
    foreach ($it in $items) {
        if ($Tags) { $it | Add-Member -NotePropertyName tags -NotePropertyValue $Tags -Force }
        $batch.Add($it)
    }
}

# --- 3) Batch-Upload -----------------------------------------------------------
Write-Host "Sende $($batch.Count) Vokabeln an $BaseUrl/api/v1/learn/vocabulary/batch ..." -ForegroundColor Cyan
$body = ConvertTo-Json $batch -Depth 6
$result = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/learn/vocabulary/batch" `
    -Headers $headers -ContentType "application/json" -Body $body

# --- 4) Ergebnis auswerten -----------------------------------------------------
$byStatus = $result | Group-Object status | Sort-Object Name
Write-Host ""
Write-Host "Ergebnis:" -ForegroundColor Yellow
foreach ($g in $byStatus) {
    $color = switch ($g.Name) { "created" { "Green" } "existing" { "DarkGray" } default { "Red" } }
    Write-Host ("  {0,-9} {1}" -f $g.Name, $g.Count) -ForegroundColor $color
}
$errors = $result | Where-Object status -eq "error"
if ($errors) {
    Write-Host "`nFehlerhafte Eintraege:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host ("  #{0}: {1}" -f $_.index, $_.error) -ForegroundColor Red }
}
Write-Host "`nFertig." -ForegroundColor Green
