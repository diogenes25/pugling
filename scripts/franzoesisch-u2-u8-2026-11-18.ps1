#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): Découvertes 2, Vokabeln Unité 2 (Rest) bis 8 laden und Plan 15 entlasten.

.DESCRIPTION
    Laut WebUntis (Altes Gymnasium) ist Friedrichs Klasse bei Unité 5; Test am 24.11. (S. 187 bis dire = Unité 5
    Découvertes + Atelier A), Klassenarbeit am 8.12.

    1. Sichert die echte Datenbank (nur am Heimnetz-Server).
    2. Lädt raw/france/decouvertes-serie-bleue-2-pugling über laden.py (neue Wortlisten Unité 2 Rest bis 8) und
       stellt Friedrichs Französischbuch auf Unité 5. Vorhandenes meldet der Lader als „schon da“.
    3. Plan 15: die Vorbereitung auf den 24.11. bleibt; Unité 1 wird von Pflicht zu Kür; Unité 5 Atelier B sowie
       Unité 3 und 4 kommen als Kür dazu. Gelöscht wird nichts.
    4. Klassenarbeit am 8.12.: Unité 5 Atelier B wird zugeordnet.

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
$repo = Join-Path $PSScriptRoot '..'
$planId = 15
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

$plan = Invoke-Api GET "supervisor/study-plans/$planId" $null
if ($plan.childId -ne $childId) { throw "Plan $planId gehört nicht Kind $childId – Abbruch." }
$before = Invoke-Api GET "supervisor/study-plans/$planId/positions" $null
if ($before | Where-Object { $_.exerciseTitle -like 'Unité 5 · Wörter Atelier B*' }) { throw "Plan $planId enthält schon Unité 5 Atelier B – Abbruch, nichts geändert." }
$tests = Invoke-Api GET "supervisor/class-tests?childId=$childId&take=200" $null
$contrôle = @($tests | Where-Object { $_.scheduledDate -eq '2026-12-08' -and $_.subjectId -eq 4 })
if ($contrôle.Count -ne 1) { throw "Klassenarbeit Französisch am 8.12. nicht eindeutig gefunden ($($contrôle.Count)) – Abbruch." }

# --- 1. Sicherung -------------------------------------------------------------------------------------------------
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $repo 'backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-franzoesisch-u2-u8-20261118"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# --- 2. Laden -----------------------------------------------------------------------------------------------------
$env:PUGLING_PIN = $Pin
try {
    & python -I -X utf8 (Join-Path $repo '.claude\skills\lehrwerk-aufbereiten\laden.py') (Join-Path $repo 'raw\france\decouvertes-serie-bleue-2-pugling') --base $BaseUrl --child $childId
    if ($LASTEXITCODE -ne 0) { throw "laden.py endete mit Code $LASTEXITCODE – Plan bleibt unverändert." }
}
finally { Remove-Item Env:PUGLING_PIN -ErrorAction SilentlyContinue }

# --- 3. Plan 15 ---------------------------------------------------------------------------------------------------
$catalog = Invoke-Api GET 'creator/exercises?seriesId=6&take=500' $null
function ExerciseId([string]$title) {
    $hit = @($catalog | Where-Object { $_.title -eq $title })
    if ($hit.Count -ne 1) { throw "Übung '$title' nicht eindeutig gefunden ($($hit.Count) Treffer)." }
    $hit[0].id
}
function FreeVocab([int]$ex, [int]$perDay) {
    @{ exerciseId = $ex; stage = 4; cadence = 'None'; newItemsPerDay = $perDay
       requireTypedTest = $true; useLeitner = $true; pointsGoalMet = 0; penaltyCoins = 0 }
}

# Unité 1 von Pflicht zu Kür: der Test am 24.11. ist Unité 5, die Vorbereitung darauf bleibt unberührt.
foreach ($pos in $before | Where-Object { $_.exerciseTitle -like 'Unité 1 · Wörter*' -and $_.cadence -ne 'None' }) {
    $r = Invoke-Api PATCH "supervisor/study-plans/$planId/positions/$($pos.id)" @{ cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 }
    Write-Host ("  {0,4}  Kür jetzt       {1}" -f $r.id, $r.exerciseTitle)
}

$atelierB = ExerciseId 'Unité 5 · Wörter Atelier B'
$new = @(
    (FreeVocab $atelierB 4),
    (FreeVocab (ExerciseId 'Unité 3 · Wörter Découvertes') 4),
    (FreeVocab (ExerciseId 'Unité 3 · Wörter Atelier A') 4),
    (FreeVocab (ExerciseId 'Unité 3 · Wörter Atelier B') 4),
    (FreeVocab (ExerciseId 'Unité 4 · Wörter Découvertes') 4),
    (FreeVocab (ExerciseId 'Unité 4 · Wörter Atelier A') 4),
    (FreeVocab (ExerciseId 'Unité 4 · Wörter Atelier B') 4)
)
foreach ($pos in $new) {
    $r = Invoke-Api POST "supervisor/study-plans/$planId/positions" $pos
    Write-Host ("  {0,4}  Kür neu         {1}" -f $r.id, $r.exerciseTitle)
}

# --- 4. Klassenarbeit 8.12. ---------------------------------------------------------------------------------------
$k = Invoke-Api POST "supervisor/class-tests/$($contrôle[0].id)/exercises" @{ exerciseIds = @($atelierB) }
Write-Host ''
Write-Host ("Klassenarbeit {0} ({1}): {2} Übungen zugeordnet" -f $k.klassenarbeit.id, $k.klassenarbeit.scheduledDate, @($k.assignedExercises).Count)

Write-Host ''
Write-Host "Plan $planId danach:"
$after = Invoke-Api GET "supervisor/study-plans/$planId/positions" $null
$after | ForEach-Object {
    $kind = if ($_.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($_.cadence)" }
    Write-Host ("  {0,4}  {1,-15} {2}" -f $_.id, $kind, $_.exerciseTitle)
}
