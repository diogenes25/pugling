#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): Friedrichs Französisch-Plan 15 auf den Vokabeltest am Di 24.11. ausrichten.

.DESCRIPTION
    Der Test („petit contrôle“) prüft Découvertes Série bleue 2, Vocabulaire S. 187 (Arcachon) bis S. 189 (dire)
    plus die blauen Kästen: 39 Wörter in zwei Übungen („Unité 5 · Wörter Découvertes – Arcachon“ mit 10,
    „Unité 5 · Wörter Atelier A – Die Fahrt nach Arcachon“ mit 29). Ziel: Montag 23.11. alle Wörter frei schreiben.

    Ablauf, den die Positionen erzwingen:
    - Mi 18. bis Fr 20.: täglich 14 neue Wörter (4 + 10) als Lernkarte, am Freitag die letzten 11. Am Samstag ist
      nichts mehr neu.
    - Leiter mit Abstand 1 Tag je Kasten: Ein Wort kommt jeden Lerntag wieder, Kasten 1 Selbsteinschätzung,
      Kasten 2 Auswahl, ab Kasten 3 frei getippt (Deutsch → Französisch). Ein Fehler wirft das Wort auf Kasten 1
      zurück, also kommt es am nächsten Tag wieder. Höchster Kasten 6, damit vor Dienstag kein Wort „fertig“ ist
      und aus der Wiederholung fällt.
    - Sonntag frei: Die beiden Pflichten tragen keinen Malus (nur 30 Münzen, wenn erfüllt). Die App kennt keinen
      freien Tag; ein Malus hier würde den Sonntag bestrafen.
    - Wochenpflicht „Generalprobe“: alle Wörter frei getippt, ohne Dosis und ohne Leiter, 90 % – am Samstag und
      am Montag. Die Leiter allein brächte die Freitagswörter bis Montag nur bis zur Auswahl.
    - Schwelle überall 90 % (Ziel ist eine 1).
    - Kür: die drei Grammatik-Übungen der Unité 5 (dire, le/la/les, indirekte Rede).

    Ein aktiver Plan je Fach: Darum wird Plan 15 verlängert, nicht ersetzt; der Lernstand von Unité 1 bleibt.
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
$planId = 15
$planTitle = 'Französisch: Unité 1 – Wörter mit Lernkarte'
$seriesId = 6   # Découvertes Série bleue 2
$unitNr = 5

if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        $a.ContentType = 'application/json; charset=utf-8'
    }
    try { Invoke-RestMethod @a }
    catch { throw "$Method $Path fehlgeschlagen: $($_.ErrorDetails.Message ?? $_.Exception.Message)" }
}

$script:headers = @{}
$login = Invoke-Api POST 'auth/adult' @{ adultId = 1; pin = $Pin }
$script:headers = @{ Authorization = "Bearer $($login.token)" }
Write-Host "Angemeldet als $($login.name)."

# Der richtige Plan, und er läuft.
$plan = Invoke-Api GET "supervisor/study-plans/$planId" $null
if ($plan.childId -ne $childId -or $plan.title -ne $planTitle -or -not $plan.active) {
    throw "Plan $planId ist nicht Friedrichs aktiver Plan '$planTitle' – Abbruch, nichts geändert."
}

# Die Übungen der Unité 5 (geladen von laden.py) über ihre Titel finden.
$units = Invoke-Api GET "creator/textbook-series/$seriesId/units" $null
$unit = $units | Where-Object { $_.orderIndex -eq $unitNr } | Select-Object -First 1
if (-not $unit) { throw "Unité $unitNr nicht in der Reihe gefunden." }
$exercises = Invoke-Api GET "creator/exercises?seriesUnitId=$($unit.id)&take=500" $null
function ExerciseId([string]$title) {
    $hit = @($exercises | Where-Object { $_.title -eq $title })
    if ($hit.Count -ne 1) { throw "Übung '$title' nicht eindeutig gefunden ($($hit.Count) Treffer) – erst laden.py laufen lassen." }
    $hit[0].id
}
$decouvertes = ExerciseId 'Unité 5 · Wörter Découvertes – Arcachon'
$atelierA = ExerciseId 'Unité 5 · Wörter Atelier A – Die Fahrt nach Arcachon'
$grammar = 'Unité 5 · dire', "Unité 5 · le, la, l', les", 'Unité 5 · Indirekte Rede und Frage' | ForEach-Object { ExerciseId $_ }

# Nicht zweimal anhängen.
$present = (Invoke-Api GET "supervisor/study-plans/$planId/positions?pageSize=200" $null).exerciseId
if ($present -contains $decouvertes -or $present -contains $atelierA) {
    throw 'Die Unité-5-Wörter hängen schon an Plan 15 – Abbruch, nichts geändert.'
}

# Sicherung der echten Datenbank, bevor etwas geschrieben wird (nur am Heimnetz-Server).
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $PSScriptRoot '..\backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-vokabeltest-20261118"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# Pflicht bis zum Test: neue Wörter als Lernkarte, täglich wiederholt, ab Kasten 3 frei getippt.
$leiter = @{ stage = 4; useLeitner = $true; requireTypedTest = $true; maxBox = 6; boxIntervalDays = @(0, 1, 1, 1, 1, 1, 1) }
function TestDuty([int]$ex, [int]$cardsPerDay) {
    $leiter + @{ exerciseId = $ex; cadence = 'Daily'; newItemsPerDay = $cardsPerDay; goalThreshold = 90
                 pointsGoalMet = 30; penaltyCoins = 0 }
}
# Generalprobe: alle Wörter frei getippt, kein Leiter, keine Dosis. Wochenpflicht (Woche Mo–So): einmal bis
# Sonntag, also am Samstag, und in der Testwoche einmal am Montag. Nötig, weil die Leiter ein Wort vom Freitag
# bis Montag nur bis zur Auswahl bringt (Kasten 1 Selbsteinschätzung, Kasten 2 Auswahl, erst ab 3 getippt).
function Rehearsal([int]$ex) {
    @{ exerciseId = $ex; stage = 4; cadence = 'Weekly'; useLeitner = $false; requireTypedTest = $true
       newItemsPerDay = 0; goalThreshold = 90; pointsGoalMet = 30; penaltyCoins = 0 }
}
function Free([int]$ex) { @{ exerciseId = $ex; cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 } }

# Die Dosis zählt Karten, jedes Wort hat zwei: 8 = 4 Wörter, 20 = 10 Wörter am Tag.
$positions = @(
    (TestDuty $decouvertes 8),
    (TestDuty $atelierA 20),
    (Rehearsal $decouvertes),
    (Rehearsal $atelierA)
) + ($grammar | ForEach-Object { Free $_ })

Write-Host ''
Write-Host "Plan ${planId}: $($plan.title)"
foreach ($pos in $positions) {
    $r = Invoke-Api POST "supervisor/study-plans/$planId/positions" $pos
    $kind = if ($r.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($r.cadence)" }
    $extra = @()
    if ($r.newItemsPerDay) { $extra += "$($r.newItemsPerDay / 2) neue Wörter/Tag" }
    if ($r.useLeitner) { $extra += "Leiter $($r.boxIntervalDays -join '/') bis Kasten $($r.maxBox)" }
    if ($r.pointsGoalMet -or $r.penaltyCoins) { $extra += "$($r.pointsGoalMet)/$($r.penaltyCoins) Münzen" }
    Write-Host ("  {0,4}  {1,-15} Stufe {2}  {3}  [{4}]" -f $r.id, $kind, $r.stage, $r.exerciseTitle, ($extra -join ', '))
}
