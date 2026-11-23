#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-19): Theodors Part C bis zum Vokabeltest am 23.11. aufholen.

.DESCRIPTION
    Stand am Abend des 19.11. (Auswertung des Tages): Von 18 Wörtern in Part C (Übung 15, Plan 13, Pos. 102) sind
    erst 8 eingeführt, keines über Kasten 2, die Generalprobe (Pos. 209) lief noch nie. Bei 4 neuen Wörtern am Tag
    wäre das letzte Wort erst am Sonntag dran – ohne Puffer.

    - Pos. 102: Dosis 8 → 10 Karten = 5 neue Wörter am Tag. Die zehn fehlenden Wörter sind so am Samstag eingeführt,
      der Sonntag bleibt zum freien Tippen.
    - Mit -GeneralprobeTaeglich wird Pos. 209 von Wochen- zu Tagespflicht (weiter 90 %, kein Malus). Erst am
      Samstag-Morgen mitlaufen lassen: Freitag wären noch nicht alle Wörter eingeführt, die Probe könnte nicht
      bestehen. Danach bis zum Test täglich; Paket B (klausurplan-b-2026-11-24.ps1) macht 209 am 24.11. zur Kür.

    Die Dosis zählt Karten (je Wort zwei), darum immer gerade ([[dosis-zaehlt-karten]]). Gelöscht wird nichts. Die PIN
    des Vater-Kontos wird verdeckt abgefragt und nirgends gespeichert.

.PARAMETER BaseUrl
    Vorgabe ist der Heimnetz-Server. Für eine Probe gegen eine Kopie: -BaseUrl http://127.0.0.1:<port>.

.PARAMETER Pin
    Nur für die Probe gegen eine Kopie mit Test-PIN. Am echten Server weglassen – dann fragt das Skript verdeckt.

.PARAMETER GeneralprobeTaeglich
    Stellt Pos. 209 von Wochen- auf Tagespflicht um (am Samstag-Morgen aufrufen).
#>
param(
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin,
    [switch]$GeneralprobeTaeglich
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_plan-api.ps1"
Connect-PuglingApi $BaseUrl $Pin

# --- Vorprüfung ---------------------------------------------------------------------------------------------------
$plan = Invoke-Api GET 'supervisor/study-plans/13' $null
if ($plan.childId -ne 1 -or -not $plan.active) { throw 'Plan 13 ist nicht der aktive Plan von Theodor – Abbruch, nichts geändert.' }
$positions = Get-ApiList 'supervisor/study-plans/13/positions'
$p102 = @($positions | Where-Object { $_.id -eq 102 })
if ($p102.Count -ne 1 -or $p102[0].exerciseId -ne 15) { throw 'Position 102 trägt nicht Übung 15 (Part C) – Abbruch, nichts geändert.' }
if ($GeneralprobeTaeglich) {
    $p209 = @($positions | Where-Object { $_.id -eq 209 })
    if ($p209.Count -ne 1 -or $p209[0].exerciseId -ne 15) { throw 'Position 209 trägt nicht Übung 15 (Generalprobe Part C) – Abbruch, nichts geändert.' }
}

Backup-RealDatabase $BaseUrl 'theodor-part-c-aufholen-20261119'

# --- Änderung -----------------------------------------------------------------------------------------------------
function Show($r) {
    Write-Host ("  {0,4}  {1,-7} Dosis {2,2}  Ziel {3} %  Malus {4}  {5}" -f $r.id, $r.cadence, $r.newItemsPerDay, $r.goalThreshold, $r.penaltyCoins, $r.exerciseTitle)
}

Write-Host ''
Write-Host 'Theodor – Plan 13 Englisch:'
Show (Invoke-Api PATCH 'supervisor/study-plans/13/positions/102' @{ newItemsPerDay = 10 })   # 5 Wörter am Tag
if ($GeneralprobeTaeglich) {
    Show (Invoke-Api PATCH 'supervisor/study-plans/13/positions/209' @{ cadence = 'Daily' })
}

Write-Host ''
Write-Host 'Pflichten danach:'
Get-ApiList 'supervisor/study-plans/13/positions' | Where-Object { $_.cadence -ne 'None' } | ForEach-Object { Show $_ }
