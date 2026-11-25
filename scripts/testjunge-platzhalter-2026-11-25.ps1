#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (B-626, Blick am Gerät): eine Testübung mit Platzhalter-Vokabeln für den Testjungen (Child 4).

.DESCRIPTION
    Legt in Access Unit 1 (Reihe 8, Unit 24) die Vokabelübung „Test: Platzhalter (B-626)“ an – fünf Einträge aus dem
    Vokabelspeicher, deren Wortliste Platzhalter druckt:

      117 (to) turn to sb.              115 (to) turn sth. up/down       102 (to) point sth. out (to sb.)
       78 (to) be home to sth.          147 (to) scare sb.

    und hängt sie zweimal in den aktiven Plan 17 des Testjungen: auf Stufe 3 (Buchstabenkästchen) und auf Stufe 4
    (Freitext). Beide als Kür, ohne Leitner (die Karte springt nach der Einführung direkt auf die Stufe der Position),
    ohne Münzen, ohne Malus. Die erste Runde zeigt jedes Wort einmal als Lernkarte (B-419), ab der zweiten Runde kommen
    die Kästchen bzw. das freie Tippen.

    Vorher wird geprüft, dass Plan, Unit und Vokabeln stimmen und die Übung noch nicht existiert – sonst Abbruch, ohne
    etwas zu ändern. Gelöscht wird nichts. Die PIN wird verdeckt abgefragt und nirgends gespeichert.

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
if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 6))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    try { $r = Invoke-RestMethod @a; $r }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

$title = 'Test: Platzhalter (B-626)'
$planId = 17; $childId = 4; $seriesId = 8; $unitId = 24
# String keys: an [ordered] dictionary indexes an int key by position, not by key.
$words = [ordered]@{
    '117' = '(to) turn to sb.'
    '115' = '(to) turn sth. up/down'
    '102' = '(to) point sth. out (to sb.)'
    '78'  = '(to) be home to sth.'
    '147' = '(to) scare sb.'
}

# --- Vorprüfung: erst alles lesen, dann schreiben ---------------------------------------------------------------
$plan = Invoke-Api GET "supervisor/study-plans/$planId" $null
if ($plan.childId -ne $childId -or -not $plan.active) { throw "Plan $planId ist nicht der aktive Plan des Testjungen – Abbruch, nichts geändert." }
$existing = @(Invoke-Api GET "creator/exercises?seriesUnitId=$unitId&take=200" $null | Where-Object { $_.title -eq $title })
if ($existing.Count -gt 0) { throw "Die Übung '$title' gibt es schon (Id $($existing[0].id)) – Abbruch, nichts geändert." }
foreach ($id in $words.Keys) {
    $v = Invoke-Api GET "creator/vocabulary/$id" $null
    if ($v.word -cne $words[$id]) { throw "Vokabel $id heißt '$($v.word)', erwartet '$($words[$id])' – Abbruch, nichts geändert." }
}

# --- Anlegen ----------------------------------------------------------------------------------------------------
$exercise = Invoke-Api POST "creator/textbook-series/$seriesId/units/$unitId/vocabulary" @{
    title       = $title
    description = 'Testübung für den Blick am Gerät: Platzhalter und Klammern stehen in den Kästchen fest, getippt wird nur der Kern.'
    orderIndex  = 99
    rewardPoints = 0
    config      = @{ direction = 'front-to-back'; refs = @($words.Keys | ForEach-Object { @{ vocabularyId = [int]$_ } }) }
}
Write-Host "Übung $($exercise.id) angelegt: $title"

foreach ($stage in 3, 4) {
    $pos = Invoke-Api POST "supervisor/study-plans/$planId/positions" @{
        exerciseId    = $exercise.id
        stage         = $stage
        cadence       = 'None'
        useLeitner    = $false
        pointsGoalMet = 0
        penaltyCoins  = 0
    }
    Write-Host ("Position {0} in Plan {1}: Stufe {2} ({3})" -f $pos.id, $planId, $stage, ($stage -eq 3 ? 'Buchstabenkästchen' : 'Freitext'))
}
Write-Host 'Fertig. Erste Runde = Lernkarten, ab der zweiten Runde Kästchen bzw. freies Tippen.'
