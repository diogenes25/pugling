#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): Friedrichs anstehende Prüfungen aus WebUntis als Klassenarbeiten anlegen.

.DESCRIPTION
    WebUntis (Altes Gymnasium) nennt für Friedrich bis Weihnachten vier Prüfungen in Fächern, die Pugling kennt.
    Bisher stand keine davon in Pugling. Die passenden Übungen werden direkt zugeordnet, damit die Übungssicht
    der Klassenarbeit darauf hinarbeitet. Geschichte (2.12.) und Chemie (7.12.) fehlen, weil Pugling diese Fächer
    nicht kennt. Gelöscht wird nichts; eine schon vorhandene Klassenarbeit mit gleichem Datum und Fach bricht ab.

    Die PIN des Vater-Kontos wird verdeckt abgefragt und nirgends gespeichert.

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
$childId = 3

if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $args = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $args.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        $args.ContentType = 'application/json; charset=utf-8'
    }
    try { Invoke-RestMethod @args }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

# Fächer: 4 Französisch, 1 Englisch, 2 Mathe. Übungen: Unité 5 (Découvertes 2) und Access 5 Unit 1.
$tests = @(
    @{ title = 'Französisch: Petit contrôle Unité 5'; subjectId = 4; scheduledDate = '2026-11-24'
       topic = 'Laut WebUntis: neuer Wortschatz S. 187 (Arcachon) bis S. 189 (dire) und die boîtes bleues.'
       exerciseIds = @(111, 112, 56, 54, 55) },
    @{ title = 'Englisch: Klassenarbeit'; subjectId = 1; scheduledDate = '2026-12-04'
       topic = 'Laut WebUntis ohne Thema. Vermutlich Unit 1 Australia (ggf. schon Unit 2) – bei Friedrich nachfragen.'
       exerciseIds = @(78, 79, 80, 93, 94, 95) },
    @{ title = 'Französisch: Contrôle (Klassenarbeit)'; subjectId = 4; scheduledDate = '2026-12-08'
       topic = 'Laut WebUntis „!!!Contrôle!!!“ ohne Thema. Vermutlich Unité 5 (ggf. Anfang Unité 6) – bei Friedrich nachfragen.'
       exerciseIds = @(111, 112, 56, 54, 55) },
    @{ title = 'Mathe: Klassenarbeit'; subjectId = 2; scheduledDate = '2026-12-16'
       topic = 'Laut WebUntis ohne Thema. Hausaufgaben zuletzt Buch S. 73–82 (Lambacher Schweizer 9).'
       exerciseIds = @() }
)

$existing = Invoke-Api GET "supervisor/class-tests?childId=$childId&take=200" $null
foreach ($t in $tests) {
    if ($existing | Where-Object { $_.scheduledDate -eq $t.scheduledDate -and $_.subjectId -eq $t.subjectId }) {
        throw "Am $($t.scheduledDate) gibt es schon eine Klassenarbeit in Fach $($t.subjectId) – Abbruch, nichts angelegt."
    }
}

foreach ($t in $tests) {
    $body = @{ childId = $childId; title = $t.title; topic = $t.topic; subjectId = $t.subjectId; scheduledDate = $t.scheduledDate }
    if ($t.exerciseIds.Count -gt 0) { $body.exerciseIds = $t.exerciseIds }
    $r = Invoke-Api POST 'supervisor/class-tests' $body
    Write-Host ("  {0,4}  {1}  {2,-40} {3} Übungen" -f $r.klassenarbeit.id, $r.klassenarbeit.scheduledDate, $r.klassenarbeit.title, @($r.assignedExercises).Count)
}

Write-Host ''
Write-Host 'Klassenarbeiten von Friedrich danach:'
$after = Invoke-Api GET "supervisor/class-tests?childId=$childId&take=200" $null
$after | Sort-Object scheduledDate | ForEach-Object {
    Write-Host ("  {0,4}  {1}  {2,-12} {3}" -f $_.id, $_.scheduledDate, $_.subjectName, $_.title)
}
