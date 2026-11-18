#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): VIVA 1 Lektion 4–6 für Theodor laden und Plan 14 auf den Schulstand umstellen.

.DESCRIPTION
    Laut WebUntis (Auguste-Viktoria-Schule) hat Theodor bis 17.11. die Vokabeln von Lektion 5 gelernt; Lektion 6
    kommt als Nächstes. Pugling kannte nur Lektion 1–3.

    1. Sichert die echte Datenbank (nur am Heimnetz-Server).
    2. Lädt raw/latin/viva-1-pugling über laden.py (Units L4–L6, Wortlisten, Start-Übungen) und stellt Theodors
       Lateinbuch auf Lektion 5. Vorhandenes (L1–3) meldet der Lader als „schon da“.
    3. Plan 14: Lektion 1 wird von Pflicht zu Kür; neu sind Lektion 5 und 4 als tägliche Pflicht (je 2 Wörter am
       Tag), Lektion 6 und die neue Grammatik als Kür. Gelöscht wird nichts.

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
$planId = 14
$childId = 1

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

# Nicht zweimal umstellen: steht eine L5-Wortliste schon im Plan, ist das Skript bereits gelaufen.
$plan = Invoke-Api GET "supervisor/study-plans/$planId" $null
if ($plan.childId -ne $childId) { throw "Plan $planId gehört nicht Kind $childId – Abbruch." }
$before = Invoke-Api GET "supervisor/study-plans/$planId/positions" $null
if ($before | Where-Object { $_.exerciseTitle -like 'Lektion 5 *' }) { throw "Plan $planId enthält schon Lektion 5 – Abbruch, nichts geändert." }

# --- 1. Sicherung -------------------------------------------------------------------------------------------------
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $repo 'backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-latein-l4-l6-20261118"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# --- 2. Laden -----------------------------------------------------------------------------------------------------
$env:PUGLING_PIN = $Pin
try {
    & python -I -X utf8 (Join-Path $repo '.claude\skills\lehrwerk-aufbereiten\laden.py') (Join-Path $repo 'raw\latin\viva-1-pugling') --base $BaseUrl --child $childId
    if ($LASTEXITCODE -ne 0) { throw "laden.py endete mit Code $LASTEXITCODE – Plan bleibt unverändert." }
}
finally { Remove-Item Env:PUGLING_PIN -ErrorAction SilentlyContinue }

# --- 3. Plan 14 ---------------------------------------------------------------------------------------------------
$catalog = Invoke-Api GET 'creator/exercises?seriesId=7&take=500' $null
function ExerciseId([string]$title) {
    $hit = @($catalog | Where-Object { $_.title -eq $title })
    if ($hit.Count -ne 1) { throw "Übung '$title' nicht eindeutig gefunden ($($hit.Count) Treffer)." }
    $hit[0].id
}
function Duty([int]$ex, [int]$perDay) {
    @{ exerciseId = $ex; stage = 4; cadence = 'Daily'; newItemsPerDay = $perDay; goalThreshold = 80
       requireTypedTest = $true; useLeitner = $true; pointsGoalMet = 20; penaltyCoins = 2 }
}
function FreeVocab([int]$ex, [int]$perDay) {
    @{ exerciseId = $ex; stage = 4; cadence = 'None'; newItemsPerDay = $perDay
       requireTypedTest = $true; useLeitner = $true; pointsGoalMet = 0; penaltyCoins = 0 }
}
function FreeCloze([int]$ex) { @{ exerciseId = $ex; cadence = 'None'; remediation = $true; pointsGoalMet = 0; penaltyCoins = 0 } }
function Free([int]$ex) { @{ exerciseId = $ex; cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 } }

# Lektion 1 von Pflicht zu Kür: kennt er aus der Schule seit August.
foreach ($pos in $before | Where-Object { $_.exerciseTitle -like 'Lektion 1 · Wörter*' -and $_.cadence -ne 'None' }) {
    $r = Invoke-Api PATCH "supervisor/study-plans/$planId/positions/$($pos.id)" @{ cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 }
    Write-Host ("  {0,4}  Kür jetzt       {1}" -f $r.id, $r.exerciseTitle)
}

$new = @(
    (Duty (ExerciseId 'Lektion 5 · Wörter Mit Schwert und Netz') 4),
    (Duty (ExerciseId 'Lektion 4 · Wörter Nächtliches Opfer') 4),
    (FreeVocab (ExerciseId 'Lektion 6 · Wörter Ein krönender Abschluss') 4),
    (Free (ExerciseId 'Lektion 4 · 3. Deklination: Nominativ und Akkusativ')),
    (FreeCloze (ExerciseId 'Lektion 4 · Präpositionen mit Akkusativ')),
    (Free (ExerciseId 'Lektion 5 · Ablativ')),
    (FreeCloze (ExerciseId 'Lektion 5 · in mit Akkusativ oder Ablativ')),
    (Free (ExerciseId 'Lektion 6 · Präsens aller Personen und esse')),
    (Free (ExerciseId 'Lektion 6 · Fragen mit -ne, nonne, num'))
)
foreach ($pos in $new) {
    $r = Invoke-Api POST "supervisor/study-plans/$planId/positions" $pos
    $kind = if ($r.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($r.cadence)" }
    Write-Host ("  {0,4}  {1,-15} {2}" -f $r.id, $kind, $r.exerciseTitle)
}

$l5 = @($catalog | Where-Object { $_.title -eq 'Lektion 5 · Wörter Mit Schwert und Netz' })[0]
$null = Invoke-Api PATCH "supervisor/study-plans/$planId" @{
    title       = 'Latein: Lektion 4–6 – Wörter mit Lernkarte'
    seriesUnitId = $l5.seriesUnitId
    description = 'VIVA 1, Stand der Schule 18.11.: Lektion 5 gelernt, Lektion 6 kommt. Pflicht täglich: Wörter Lektion 5 und 4, je 2 neue Wörter am Tag als Lernkarte. Lektion 1–3 und 6 sowie die Grammatik sind Kür; Lektion 6 wird Pflicht, sobald die Schule damit anfängt.'
}

Write-Host ''
Write-Host "Plan $planId danach:"
$after = Invoke-Api GET "supervisor/study-plans/$planId/positions" $null
$after | ForEach-Object {
    $kind = if ($_.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($_.cadence)" }
    Write-Host ("  {0,4}  {1,-15} {2}" -f $_.id, $kind, $_.exerciseTitle)
}
