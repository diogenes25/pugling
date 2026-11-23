#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-22): Friedrichs Französisch-Plan 15 für Sonntag 22. und Montag 23.11. auf die Contrôle am 24.11. ausrichten.

.DESCRIPTION
    Vorher laden (legt die vier neuen Übungen der Unité 5 an, Wörter und alte Übungen bleiben unberührt):
        python -I -X utf8 .claude/skills/lehrwerk-aufbereiten/laden.py raw/france/decouvertes-serie-bleue-2-pugling --pin <PIN>

    Dann hängt dieses Skript vier tägliche Pflichten an Plan 15 (tippen, 90 %, 30 Münzen, kein Malus):
    - Testsätze Deutsch -> Französisch (30 ganze Sätze im Format des Vorbereitungstests vom 20.11.)
    - Testsätze mit Lücke (das Lückenformat des Tests)
    - me, te, nous, vous
    - dire und die indirekte Rede
    Zusammen mit der Wochenpflicht „Generalprobe“ (Pos. 167/168) sind das die Übungen für heute und morgen.

    Rückbau nach der Contrôle: klausurplan-b-2026-11-24.ps1 kennt diese Positionen nicht – danach von Hand auf Kür stellen.
    Die PIN wird verdeckt abgefragt und nirgends gespeichert.
#>
param(
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
$api = "$BaseUrl/api/v1"
$childId = 3
$planId = 15
$seriesId = 6
$unitNr = 5

if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    try { $r = Invoke-RestMethod @a; $r }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

$plan = Invoke-Api GET "supervisor/study-plans/$planId" $null
if ($plan.childId -ne $childId -or -not $plan.active) { throw "Plan $planId ist nicht Friedrichs aktiver Plan – Abbruch, nichts geändert." }

$units = Invoke-Api GET "creator/textbook-series/$seriesId/units" $null
$unit = $units | Where-Object { $_.orderIndex -eq $unitNr } | Select-Object -First 1
if (-not $unit) { throw "Unité $unitNr nicht gefunden." }
$exercises = Invoke-Api GET "creator/exercises?seriesUnitId=$($unit.id)&take=500" $null
function ExerciseId([string]$title) {
    $hit = @($exercises | Where-Object { $_.title -eq $title })
    if ($hit.Count -ne 1) { throw "Übung '$title' nicht eindeutig ($($hit.Count) Treffer) – erst laden.py laufen lassen." }
    $hit[0].id
}
$titles = 'Unité 5 · Testsätze Deutsch → Französisch', 'Unité 5 · Testsätze mit Lücke', 'Unité 5 · me, te, nous, vous', 'Unité 5 · dire und die indirekte Rede'
$ids = $titles | ForEach-Object { ExerciseId $_ }

$present = (Invoke-Api GET "supervisor/study-plans/$planId/positions?pageSize=200" $null).exerciseId
if (@($ids | Where-Object { $present -contains $_ }).Count -gt 0) { throw 'Eine der Übungen hängt schon an Plan 15 – Abbruch, nichts geändert.' }

if ($BaseUrl -match ':5200$') {
    $db = Join-Path $PSScriptRoot '..\backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-testvorbereitung-20261122"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

foreach ($id in $ids) {
    $r = Invoke-Api POST "supervisor/study-plans/$planId/positions" @{
        exerciseId = $id; stage = 4; cadence = 'Daily'; useLeitner = $false; requireTypedTest = $true
        newItemsPerDay = 0; goalThreshold = 90; pointsGoalMet = 30; penaltyCoins = 0 }
    Write-Host ("  {0,4}  Pflicht täglich  {1}" -f $r.id, $r.exerciseTitle)
}
