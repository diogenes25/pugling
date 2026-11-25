#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (PM-Sitzung 2026-11-25, Runde 4): der Malus je verpasster Tagespflicht steigt von 2 auf 20 Münzen.

.DESCRIPTION
    Gemessen an einer Kopie: Friedrich hat vom 22. bis 24.11. keine seiner vier Tagespflichten erfüllt und trotzdem bis zu
    457 Münzen am Tag verdient; der Malus kostete ihn 4 bzw. 8 Münzen. Entscheidung des Vaters: Schwänzen kostet, was
    Erfüllen bringt (PointsGoalMet = 20), und zwar nur an den Tagespflichten beider Söhne.

      Theodor   Plan 13: 99, 100   Plan 14: 193, 195
      Friedrich Plan 15: 202       Plan 16: 133, 134   Plan 19: 211

    Unverändert bleiben Theodors Wochen-Nacharbeit 101 (Malus 2) und Friedrichs Generalproben (ohne Malus).

    Die Abrechnung nimmt den Malus, der beim Abrechnen gilt (PositionProgressService.SettleClosedPeriodsAsync). Darum
    werden vorher beide Söhne angemeldet – das rechnet alle geschlossenen Tage zum alten Satz ab –, und erst danach wird
    umgestellt. Der neue Satz gilt ab dem heutigen Tag, den die Sohn-App an jeder Pflicht als Preis anzeigt.

    Vorher wird geprüft, dass jede Position noch täglich ist und 2 Münzen Malus trägt – sonst bricht das Skript ab, ohne
    etwas zu ändern. Gegen den Heimnetz-Server wird die echte DB vorher gesichert. Die PINs werden verdeckt abgefragt und
    nirgends gespeichert.

.PARAMETER BaseUrl
    Vorgabe ist der Heimnetz-Server. Für eine Probe gegen eine Kopie: -BaseUrl http://127.0.0.1:<port>.

.PARAMETER Pin
    Nur für die Probe gegen eine Kopie mit Test-PIN (gilt dann für alle drei Konten). Am echten Server weglassen.
#>
param(
    [string]$BaseUrl = 'http://127.0.0.1:5200',
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_plan-api.ps1"

$newPenalty = 20
# Plan -> Positionen; String-Schlüssel, weil ein [ordered]-Dictionary einen int-Schlüssel als Index liest.
$targets = [ordered]@{
    '13' = @(99, 100); '14' = @(193, 195)
    '15' = @(202); '16' = @(133, 134); '19' = @(211)
}

# Erst die geschlossenen Tage zum alten Satz abrechnen: der Kind-Login ist die Naht dafür.
foreach ($childId in 1, 3) {
    $childPin = if ($Pin) { $Pin } else { Read-Host -MaskInput "PIN von Kind $childId" }
    $null = Invoke-RestMethod -Method POST -Uri "$($BaseUrl.TrimEnd('/'))/api/v1/auth/child" `
        -ContentType 'application/json' -Body (@{ childId = $childId; pin = $childPin } | ConvertTo-Json)
    Write-Host "Kind $childId angemeldet: geschlossene Tage zum alten Satz abgerechnet."
}

Connect-PuglingApi $BaseUrl $Pin

# Dann alles prüfen, bevor etwas geschrieben wird.
foreach ($planId in $targets.Keys) {
    foreach ($posId in $targets[$planId]) {
        $p = Invoke-Api GET "supervisor/study-plans/$planId/positions/$posId" $null
        if ($p.cadence -ne 'Daily' -or $p.penaltyCoins -ne 2) {
            throw "Position $posId (Plan $planId): Taktung '$($p.cadence)', Malus $($p.penaltyCoins) – erwartet Daily/2. Abbruch, nichts geändert."
        }
    }
}

Backup-RealDatabase $BaseUrl 'malus-20-2026-11-25'

foreach ($planId in $targets.Keys) {
    foreach ($posId in $targets[$planId]) {
        $p = Invoke-Api PATCH "supervisor/study-plans/$planId/positions/$posId" @{ penaltyCoins = $newPenalty }
        Write-Host ("Plan {0} Pos. {1}: Malus {2} – {3}" -f $planId, $posId, $p.penaltyCoins, $p.exerciseTitle)
    }
}
