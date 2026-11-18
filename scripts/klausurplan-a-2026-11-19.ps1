#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18, Paket A): Pläne 13–16 an den nächsten Klausuren ausrichten – gilt ab 19.11.

.DESCRIPTION
    Abgleich WebUntis ↔ Pugling vom 18.11. (Vorschlag vom Vater bestätigt):

    Theodor (Child 1)
    - Plan 13 Englisch: Unit 1 Part C ist laut Hausaufgabe zum 23.11. zu lernen („the rest!“) → tägliche Pflicht
      mit 4 Wörtern am Tag (18 Wörter in fünf Tagen) und eine Wochenpflicht „Generalprobe“ (90 %, kein Malus).
      Part B wird solange Kür (Tageslast höchstens 4), Part A bleibt Pflicht.
    - Plan 14 Latein: Lektion 6 kommt in der Schule als Nächstes → Pflicht; Lektion 4 wird Kür.
    - Klassenarbeit 1 („Vokabeltest Unit 1“ am 20.11.) ist laut WebUntis nicht angesetzt; sie wird auf den 23.11.
      verlegt und heißt „Englisch: Vokabeln Unit 1 Part C“. Zugeordnete Übungen bleiben.

    Friedrich (Child 3)
    - Plan 15 Französisch: unverändert (Test am 24.11.).
    - Plan 16 Englisch: Klassenarbeit am 4.12. → Part A mit 4 statt 2 Wörtern am Tag, Part B neu tägliche Pflicht.

    Beide: Die Pläne 13–16 enden am 15.12.; danach wären sie nicht mehr spielbar. Neues Ende 20.12.

    Gelöscht wird nichts. Die PIN des Vater-Kontos wird verdeckt abgefragt und nirgends gespeichert.

.PARAMETER BaseUrl
    Vorgabe ist der Heimnetz-Server. Für eine Probe gegen eine Kopie: -BaseUrl http://127.0.0.1:<port>.

.PARAMETER Pin
    Nur für die Probe gegen eine Kopie mit Test-PIN. Am echten Server weglassen – dann fragt das Skript verdeckt.
#>
param(
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
$api = "$BaseUrl/api/v1"
$repo = Join-Path $PSScriptRoot '..'
$endDate = '2026-12-20'

if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    # Über eine Variable zurückgeben: so wird eine JSON-Liste aufgezählt statt als ein Objekt weitergereicht.
    try { $r = Invoke-RestMethod @a; $r }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

# --- Vorprüfung: richtige Pläne, richtige Positionen, noch nicht gelaufen ------------------------------------------
$plans = @{ 13 = 1; 14 = 1; 15 = 3; 16 = 3 }
$positions = @{}
foreach ($planId in $plans.Keys) {
    $p = Invoke-Api GET "supervisor/study-plans/$planId" $null
    if ($p.childId -ne $plans[$planId] -or -not $p.active) { throw "Plan $planId ist nicht der aktive Plan von Kind $($plans[$planId]) – Abbruch, nichts geändert." }
    $positions[$planId] = @(Invoke-Api GET "supervisor/study-plans/$planId/positions" $null)
}
# Position-Id → erwartete Übung; schützt davor, dass eine Id inzwischen etwas anderes trägt.
$expected = @{ 99 = 13; 100 = 14; 102 = 15; 193 = 114; 194 = 113; 195 = 115; 133 = 78; 134 = 79 }
function Pos([int]$planId, [int]$posId) {
    $hit = @($positions[$planId] | Where-Object { $_.id -eq $posId })
    if ($hit.Count -ne 1 -or $hit[0].exerciseId -ne $expected[$posId]) { throw "Position $posId in Plan $planId trägt nicht Übung $($expected[$posId]) – Abbruch, nichts geändert." }
    $hit[0]
}
$null = 99, 100, 102 | ForEach-Object { Pos 13 $_ }
$null = 193, 194, 195 | ForEach-Object { Pos 14 $_ }
$null = 133, 134 | ForEach-Object { Pos 16 $_ }
if (@($positions[13] | Where-Object { $_.exerciseId -eq 15 }).Count -gt 1) { throw 'Plan 13 hat schon eine Generalprobe für Part C – Paket A ist gelaufen, nichts geändert.' }
$tests = @(Invoke-Api GET 'supervisor/class-tests?childId=1&take=200' $null)
$ka1 = @($tests | Where-Object { $_.id -eq 1 })
if ($ka1.Count -ne 1 -or $ka1[0].scheduledDate -ne '2026-11-20') { throw 'Klassenarbeit 1 steht nicht mehr am 20.11. – Abbruch, nichts geändert.' }

# --- Sicherung ----------------------------------------------------------------------------------------------------
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $repo 'backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-klausurplan-a-20261118"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# --- Bausteine ----------------------------------------------------------------------------------------------------
# Pflicht wie in den Vorlagen; Dosis zählt Karten (je Wort zwei), 4 = 2 Wörter am Tag.
function Duty([int]$perDay) {
    @{ cadence = 'Daily'; stage = 4; newItemsPerDay = $perDay; goalThreshold = 80; requireTypedTest = $true
       useLeitner = $true; pointsGoalMet = 20; penaltyCoins = 2 }
}
$free = @{ cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 }
# Generalprobe: alle Wörter frei getippt, ohne Leiter und Dosis, einmal je Woche; kein Malus.
function Rehearsal([int]$ex, [int]$threshold) {
    @{ exerciseId = $ex; stage = 4; cadence = 'Weekly'; useLeitner = $false; requireTypedTest = $true
       newItemsPerDay = 0; goalThreshold = $threshold; pointsGoalMet = 30; penaltyCoins = 0 }
}
function Show($r) {
    $kind = if ($r.cadence -eq 'None') { 'Kür' } else { "Pflicht $($r.cadence)" }
    Write-Host ("  {0,4}  {1,-15} Dosis {2,2}  {3}" -f $r.id, $kind, $r.newItemsPerDay, $r.exerciseTitle)
}
function Patch([int]$planId, [int]$posId, $body) { Show (Invoke-Api PATCH "supervisor/study-plans/$planId/positions/$posId" $body) }

# --- Theodor ------------------------------------------------------------------------------------------------------
Write-Host ''
Write-Host 'Theodor – Plan 13 Englisch:'
Patch 13 102 (Duty 8)            # Part C: 4 Wörter am Tag bis 23.11.
Patch 13 100 $free               # Part B: Kür bis nach dem 23.11.
Show (Invoke-Api POST 'supervisor/study-plans/13/positions' (Rehearsal 15 90))
Write-Host 'Theodor – Plan 14 Latein:'
Patch 14 195 (Duty 4)            # Lektion 6: kommt in der Schule als Nächstes
Patch 14 194 $free               # Lektion 4: Kür

$k = Invoke-Api PATCH 'supervisor/class-tests/1' @{
    title         = 'Englisch: Vokabeln Unit 1 Part C'
    scheduledDate = '2026-11-23'
    topic         = 'Laut WebUntis Hausaufgabe zum 23.11.: „Vocab Unit 1, part C (the rest!)“. Ein Test ist nicht angekündigt.'
}
Write-Host ("Klassenarbeit {0}: {1} am {2}" -f $k.id, $k.title, $k.scheduledDate)

# --- Friedrich ----------------------------------------------------------------------------------------------------
Write-Host ''
Write-Host 'Friedrich – Plan 16 Englisch (Klassenarbeit 4.12.):'
Patch 16 133 @{ newItemsPerDay = 8 }   # Part A: 4 Wörter am Tag
Patch 16 134 (Duty 4)                  # Part B: neu Pflicht

# --- Laufzeit -----------------------------------------------------------------------------------------------------
Write-Host ''
foreach ($planId in 13, 14, 15, 16) {
    $r = Invoke-Api PATCH "supervisor/study-plans/$planId" @{ endDate = $endDate }
    Write-Host ("Plan {0} läuft {1} bis {2}: {3}" -f $r.id, $r.startDate, $r.endDate, $r.title)
}

Write-Host ''
Write-Host 'Pflichten danach:'
foreach ($planId in 13, 14, 15, 16) {
    Invoke-Api GET "supervisor/study-plans/$planId/positions" $null | Where-Object { $_.cadence -ne 'None' } | ForEach-Object {
        Write-Host ("  Plan {0}  {1,4}  {2,-7} Dosis {3,2}  {4}" -f $planId, $_.id, $_.cadence, $_.newItemsPerDay, $_.exerciseTitle)
    }
}
