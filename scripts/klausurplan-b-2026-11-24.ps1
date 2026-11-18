#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (Paket B): Rückbau nach Theodors Part-C-Termin (23.11.) und Friedrichs Petit contrôle (24.11.).

.DESCRIPTION
    Erst am Abend des 24.11. ausführen, nachdem Friedrich den Test geschrieben und beide gespielt haben – der
    Wechsel einer Tagespflicht wirkt sofort. Setzt Paket A (klausurplan-a-2026-11-19.ps1) voraus.

    Theodor (Child 1), Plan 13: Part C und seine Generalprobe werden Kür, Part B wird wieder tägliche Pflicht.

    Friedrich (Child 3)
    - Plan 15: Die täglichen Testpositionen Unité 5 Découvertes / Atelier A (165/166) werden Kür. Die
      Wochenpflichten 167/168 bleiben als Generalprobe für die Contrôle am 8.12. Unité 5 Atelier B wird tägliche
      Pflicht (2 Wörter am Tag, 20 Wörter bis zum 4.12. eingeführt).
    - Plan 16: Generalprobe als Wochenpflicht für die Englisch-Klassenarbeit am 4.12. (Part A und B, 80 %, kein
      Malus). 80 statt 90 %, weil Part A bis dahin nicht vollständig eingeführt ist.

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

# Am echten Server erst ab dem 24.11.; eine Probe gegen eine Kopie darf früher laufen.
if ($BaseUrl -match ':5200$' -and (Get-Date).Date -lt [datetime]'2026-11-24') { throw 'Paket B gilt erst ab dem Abend des 24.11. – Abbruch, nichts geändert.' }

# --- Vorprüfung ---------------------------------------------------------------------------------------------------
$plans = @{ 13 = 1; 15 = 3; 16 = 3 }
$positions = @{}
foreach ($planId in $plans.Keys) {
    $p = Invoke-Api GET "supervisor/study-plans/$planId" $null
    if ($p.childId -ne $plans[$planId] -or -not $p.active) { throw "Plan $planId ist nicht der aktive Plan von Kind $($plans[$planId]) – Abbruch, nichts geändert." }
    $positions[$planId] = @(Invoke-Api GET "supervisor/study-plans/$planId/positions" $null)
}
$expected = @{ 100 = 14; 102 = 15; 165 = 111; 166 = 112; 202 = 131 }
function Pos([int]$planId, [int]$posId) {
    $hit = @($positions[$planId] | Where-Object { $_.id -eq $posId })
    if ($hit.Count -ne 1 -or $hit[0].exerciseId -ne $expected[$posId]) { throw "Position $posId in Plan $planId trägt nicht Übung $($expected[$posId]) – Abbruch, nichts geändert." }
    $hit[0]
}
$null = 100, 102 | ForEach-Object { Pos 13 $_ }
$null = 165, 166, 202 | ForEach-Object { Pos 15 $_ }
if ((Pos 15 165).cadence -eq 'None') { throw 'Pos. 165 ist schon Kür – Paket B ist gelaufen, nichts geändert.' }
if (@($positions[16] | Where-Object { $_.cadence -eq 'Weekly' }).Count -gt 0) { throw 'Plan 16 hat schon eine Generalprobe – Paket B ist gelaufen, nichts geändert.' }
$rehearsalC = @($positions[13] | Where-Object { $_.exerciseId -eq 15 -and $_.cadence -eq 'Weekly' })
if ($rehearsalC.Count -ne 1) { throw 'Generalprobe Part C in Plan 13 nicht gefunden – erst Paket A ausführen. Abbruch, nichts geändert.' }

# --- Sicherung ----------------------------------------------------------------------------------------------------
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $repo 'backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-klausurplan-b-$((Get-Date).ToString('yyyyMMdd'))"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# --- Bausteine ----------------------------------------------------------------------------------------------------
function Duty([int]$perDay) {
    @{ cadence = 'Daily'; stage = 4; newItemsPerDay = $perDay; goalThreshold = 80; requireTypedTest = $true
       useLeitner = $true; pointsGoalMet = 20; penaltyCoins = 2 }
}
$free = @{ cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 }
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
Patch 13 102 $free                      # Part C: Termin vorbei
Patch 13 $rehearsalC[0].id $free        # Generalprobe Part C
Patch 13 100 (Duty 4)                   # Part B: wieder Pflicht

# --- Friedrich ----------------------------------------------------------------------------------------------------
Write-Host ''
Write-Host 'Friedrich – Plan 15 Französisch (Contrôle 8.12.):'
Patch 15 165 $free
Patch 15 166 $free
Patch 15 202 (Duty 4)                   # Atelier B
Write-Host 'Friedrich – Plan 16 Englisch (Klassenarbeit 4.12.):'
Show (Invoke-Api POST 'supervisor/study-plans/16/positions' (Rehearsal 78 80))
Show (Invoke-Api POST 'supervisor/study-plans/16/positions' (Rehearsal 79 80))

Write-Host ''
Write-Host 'Pflichten danach:'
foreach ($planId in 13, 14, 15, 16) {
    Invoke-Api GET "supervisor/study-plans/$planId/positions" $null | Where-Object { $_.cadence -ne 'None' } | ForEach-Object {
        Write-Host ("  Plan {0}  {1,4}  {2,-7} Dosis {3,2}  {4}" -f $planId, $_.id, $_.cadence, $_.newItemsPerDay, $_.exerciseTitle)
    }
}
