#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): Mathe-Übungen zu Lambacher Schweizer 9, Kapitel III, für Friedrichs Klassenarbeit am 16.12.

.DESCRIPTION
    Friedrichs Mathe-Hausaufgaben laufen durch Kapitel III „Quadratische Funktionen“ (S. 73–82). Das Kapitel liegt
    seit 18.11. als Seiten-Markdown unter raw/Math/lambacher-schweizer-9-sh-pugling/md (S. 74–101; S. 70–73 fehlen).

    1. Sichert die echte Datenbank (nur am Heimnetz-Server).
    2. Katalog: Reihe „Lambacher Schweizer 9 (Schleswig-Holstein)“ (Fach Mathe), Unit „Kapitel III Quadratische
       Funktionen“ und vier Rechenaufgaben-Übungen (III.2 bis III.5). Die Aufgaben sind selbst geschrieben, keine
       Übernahme aus dem Buch. Alle Antworten sind ganze Zahlen, weil die Prüfung „1,5“ nicht als Dezimalzahl liest.
    3. Friedrichs Lehrbuch „Lambacher Schweizer 9“ mit Kapitel III als aktuellem Stand.
    4. Neuer Plan „Mathe: Kapitel III – Quadratische Funktionen“ (19.11.–20.12.), alle vier Übungen als Kür mit
       Leitner, 4 neue Aufgaben am Tag. Pflicht wird die Scheitelpunktform mit Paket B (24.11. abends).
    5. Klassenarbeit 6 (Mathe, 16.12.): die vier Übungen zugeordnet, Thema nachgetragen.

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
$childId = 3
$mathSubjectId = 2
$seriesName = 'Lambacher Schweizer 9 (Schleswig-Holstein)'
$classTestId = 6

if (-not $Pin) { $Pin = Read-Host -MaskInput 'PIN des Vater-Kontos (Papa)' }

function Invoke-Api([string]$Method, [string]$Path, $Body) {
    $a = @{ Method = $Method; Uri = "$api/$Path"; Headers = $script:headers }
    if ($null -ne $Body) {
        $a.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 10))
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

# --- Vorprüfung ---------------------------------------------------------------------------------------------------
if (@(Invoke-Api GET 'creator/textbook-series?take=500' $null | Where-Object { $_.name -eq $seriesName }).Count -gt 0) {
    throw "Die Reihe '$seriesName' gibt es schon – Skript ist gelaufen, nichts geändert."
}
$plans = @(Invoke-Api GET "supervisor/study-plans?childId=$childId&take=200" $null)
if (@($plans | Where-Object { $_.subjectId -eq $mathSubjectId -and $_.active }).Count -gt 0) {
    throw 'Friedrich hat schon einen aktiven Mathe-Plan – Abbruch, nichts geändert.'
}
$ka = @(Invoke-Api GET "supervisor/class-tests?childId=$childId&take=200" $null | Where-Object { $_.id -eq $classTestId })
if ($ka.Count -ne 1 -or $ka[0].subjectId -ne $mathSubjectId -or $ka[0].scheduledDate -ne '2026-12-16') {
    throw "Klassenarbeit $classTestId ist nicht Friedrichs Mathe-Arbeit am 16.12. – Abbruch, nichts geändert."
}

# --- 1. Sicherung -------------------------------------------------------------------------------------------------
if ($BaseUrl -match ':5200$') {
    $db = Join-Path $repo 'backend\Pugling.Api\pugling.db'
    $backup = "$db.bak-vor-mathe-kapitel3-20261118"
    if (-not (Test-Path $backup)) { Copy-Item $db $backup }
    Write-Host "Sicherung: $backup"
}

# --- 2. Katalog ---------------------------------------------------------------------------------------------------
$publisher = Invoke-Api POST 'creator/publishers' @{ name = 'Klett' }
$series = Invoke-Api POST 'creator/textbook-series' @{
    name = $seriesName; publisherId = $publisher.id; subjectId = $mathSubjectId; schoolTypes = 'Gymnasium'
    notes = 'Klasse 9, G9, Schleswig-Holstein. Stoff aus den Seitenfotos S. 74–101 (Kapitel III); S. 70–73 fehlen. Eigene Aufgaben, keine Übernahme aus dem Buch.'
}
$unit = Invoke-Api POST "creator/textbook-series/$($series.id)/units" @{
    label = 'Kapitel III Quadratische Funktionen'; grade = 9; orderIndex = 3
    topics = @('f(x) = ax², Normalparabel, Streckfaktor', 'Scheitelpunktform f(x) = a(x − d)² + e',
               'Normalform f(x) = ax² + bx + c und quadratische Ergänzung', 'Funktionsgleichungen aufstellen',
               'Ausgleichskurven (quadratische Regression)')
}
Write-Host ("Reihe {0} '{1}', Unit {2} '{3}'" -f $series.id, $series.name, $unit.id, $unit.label)

function P([string]$prompt, [int]$answer) { @{ prompt = $prompt; answer = $answer } }

$exercises = @(
    @{ title = 'Kapitel III · f(x) = ax²: Funktionswerte und Streckfaktor'; pages = 'S. 74–77'
       description = 'Funktionswerte von f(x) = ax² berechnen, die n²-Regel und den Streckfaktor a aus einem Punkt bestimmen. Antworten sind ganze Zahlen.'
       problems = @(
        (P 'f(x) = 3x². Berechne f(2).' 12),
        (P 'f(x) = 3x². Berechne f(−2).' 12),
        (P 'f(x) = −2x². Berechne f(3).' -18),
        (P 'f(x) = 0,5x². Berechne f(4).' 8),
        (P 'f(x) = 0,25x². Berechne f(−6).' 9),
        (P 'f(x) = −x². Berechne f(5).' -25),
        (P 'f(x) = −3x². Berechne f(−2).' -12),
        (P 'Bei f(x) = ax² wird der x-Wert verdreifacht. Mit welcher Zahl wird der Funktionswert multipliziert?' 9),
        (P 'Bei f(x) = ax² wird der x-Wert verfünffacht. Mit welcher Zahl wird der Funktionswert multipliziert?' 25),
        (P 'Die Parabel von f(x) = ax² geht durch P(2|20). Wie groß ist a?' 5),
        (P 'Die Parabel von f(x) = ax² geht durch P(3|−18). Wie groß ist a?' -2),
        (P 'f(x) = 2x². Für welches positive x ist f(x) = 50?' 5)) },
    @{ title = 'Kapitel III · Scheitelpunktform ablesen und verschieben'; pages = 'S. 78–83'
       description = 'Scheitelpunkt S(d|e) und Streckfaktor aus f(x) = a(x − d)² + e ablesen, Verschiebungen der Normalparabel und Punkte auf verschobenen Parabeln.'
       problems = @(
        (P 'f(x) = (x − 3)² + 5. Wie lautet die x-Koordinate des Scheitelpunkts?' 3),
        (P 'f(x) = (x − 3)² + 5. Wie lautet die y-Koordinate des Scheitelpunkts?' 5),
        (P 'f(x) = (x + 4)² − 1. Wie lautet die x-Koordinate des Scheitelpunkts?' -4),
        (P 'f(x) = (x + 4)² − 1. Wie lautet die y-Koordinate des Scheitelpunkts?' -1),
        (P 'f(x) = −2(x − 1)² + 7. Wie groß ist der Streckfaktor a?' -2),
        (P 'f(x) = x² − 6. Wie lautet die y-Koordinate des Scheitelpunkts?' -6),
        (P 'Die Normalparabel wird um 5 nach rechts verschoben: f(x) = (x − d)². Wie groß ist d?' 5),
        (P 'Die Normalparabel wird um 2 nach links und um 3 nach oben verschoben: f(x) = (x − d)² + e. Wie groß ist d?' -2),
        (P 'Die Normalparabel wird um 2 nach links und um 3 nach oben verschoben: f(x) = (x − d)² + e. Wie groß ist e?' 3),
        (P 'Eine verschobene Normalparabel hat den Scheitelpunkt S(1|5). Wie lautet die y-Koordinate von P(3|?)' 9),
        (P 'Eine verschobene Normalparabel hat den Scheitelpunkt S(−2|−4). Wie lautet die y-Koordinate von P(1|?)' 5),
        (P 'f(x) = 2(x − 1)² − 3. Berechne f(3).' 5),
        (P 'f(x) = −(x + 2)² + 6. Berechne f(1).' -3)) },
    @{ title = 'Kapitel III · Normalform und quadratische Ergänzung'; pages = 'S. 83–87'
       description = 'Von der Scheitelpunktform zur Normalform ausmultiplizieren, y-Achsenabschnitt ablesen, quadratisch ergänzen und den Scheitelpunkt aus der Normalform bestimmen.'
       problems = @(
        (P 'Schreibe f(x) = (x − 3)² + 1 in Normalform f(x) = x² + bx + c. Wie groß ist b?' -6),
        (P 'Schreibe f(x) = (x − 3)² + 1 in Normalform f(x) = x² + bx + c. Wie groß ist c?' 10),
        (P 'Schreibe f(x) = 2(x + 1)² − 5 in Normalform f(x) = ax² + bx + c. Wie groß ist b?' 4),
        (P 'Schreibe f(x) = 2(x + 1)² − 5 in Normalform f(x) = ax² + bx + c. Wie groß ist c?' -3),
        (P 'f(x) = 3x² − 2x + 7. Bei welchem y-Wert schneidet der Graph die y-Achse?' 7),
        (P 'Welche Zahl ergänzt x² − 10x + ☐ zu einer binomischen Formel?' 25),
        (P 'Welche Zahl ergänzt x² + 12x + ☐ zu einer binomischen Formel?' 36),
        (P 'f(x) = x² − 6x + 11. Wie lautet die x-Koordinate des Scheitelpunkts?' 3),
        (P 'f(x) = x² − 6x + 11. Wie lautet die y-Koordinate des Scheitelpunkts?' 2),
        (P 'f(x) = x² + 8x + 10. Wie lautet die x-Koordinate des Scheitelpunkts?' -4),
        (P 'f(x) = x² + 8x + 10. Wie lautet die y-Koordinate des Scheitelpunkts?' -6),
        (P 'f(x) = 2x² − 8x + 3. Wie lautet die y-Koordinate des Scheitelpunkts?' -5),
        (P 'f(x) = −x² + 4x + 1. Wie groß ist der größte Funktionswert?' 5)) },
    @{ title = 'Kapitel III · Funktionsgleichungen aufstellen'; pages = 'S. 88–91'
       description = 'Aus Scheitelpunkt und Punkt den Streckfaktor bestimmen (Scheitelpunktform) und aus y-Achsenabschnitt und zwei Punkten die Normalform aufstellen.'
       problems = @(
        (P 'Eine Parabel hat den Scheitelpunkt S(2|−4) und geht durch P(4|4): f(x) = a(x − 2)² − 4. Wie groß ist a?' 2),
        (P 'Eine Parabel hat den Scheitelpunkt S(0|5) und geht durch P(2|−3): f(x) = ax² + 5. Wie groß ist a?' -2),
        (P 'Eine Parabel hat den Scheitelpunkt S(−1|3) und geht durch P(1|15). Wie groß ist der Streckfaktor a?' 3),
        (P 'Eine Parabel hat den Scheitelpunkt S(3|1) und geht durch P(5|−7). Wie groß ist der Streckfaktor a?' -2),
        (P 'Eine verschobene Normalparabel hat den Scheitelpunkt S(4|−2). In Normalform f(x) = x² + bx + c: Wie groß ist b?' -8),
        (P 'Eine verschobene Normalparabel hat den Scheitelpunkt S(4|−2). In Normalform f(x) = x² + bx + c: Wie groß ist c?' 14),
        (P 'f(x) = ax² + bx + c schneidet die y-Achse bei 2 und geht durch P(1|5) und Q(−1|3). Wie groß ist c?' 2),
        (P 'f(x) = ax² + bx + c schneidet die y-Achse bei 2 und geht durch P(1|5) und Q(−1|3). Wie groß ist a?' 2),
        (P 'f(x) = ax² + bx + c schneidet die y-Achse bei 2 und geht durch P(1|5) und Q(−1|3). Wie groß ist b?' 1),
        (P 'Eine Parabel f(x) = ax² + bx + c geht durch (0|−1), (1|2) und (2|9). Wie groß ist a?' 2),
        (P 'Eine Parabel f(x) = ax² + bx + c geht durch (0|−1), (1|2) und (2|9). Wie groß ist b?' 1),
        (P 'Ein parabelförmiger Bogen ist 4 m hoch und am Boden 4 m breit. Mit S(0|4) gilt f(x) = ax² + 4. Wie groß ist a?' -1)) }
)

$created = @()
$order = 0
foreach ($e in $exercises) {
    $r = Invoke-Api POST "creator/textbook-series/$($series.id)/units/$($unit.id)/arithmetic" @{
        title = $e.title; orderIndex = $order++; rewardPoints = 10; description = $e.description
        gradeMin = 9; gradeMax = 9; schoolTypes = 'Gymnasium'; bookPages = $e.pages; defaultUseLeitner = $true
        source = 'Eigene Aufgaben zu Lambacher Schweizer 9, Kapitel III'
        config = @{ problems = $e.problems }
    }
    Write-Host ("  Übung {0,4}  {1,2} Aufgaben  {2}" -f $r.id, $e.problems.Count, $r.title)
    $created += $r
}

# --- 3. Lehrbuch --------------------------------------------------------------------------------------------------
$book = Invoke-Api POST "supervisor/children/$childId/textbooks" @{
    title = 'Lambacher Schweizer 9'; subjectId = $mathSubjectId; grade = 9; publisher = 'Klett'
    currentChapter = 'Kapitel III Quadratische Funktionen'; seriesId = $series.id; currentUnitId = $unit.id
}
Write-Host ("Lehrbuch {0}: {1}, Stand {2}" -f $book.id, $book.title, $book.currentChapter)

# --- 4. Plan ------------------------------------------------------------------------------------------------------
$plan = Invoke-Api POST 'supervisor/study-plans' @{
    childId = $childId; title = 'Mathe: Kapitel III – Quadratische Funktionen'; subjectId = $mathSubjectId
    startDate = '2026-11-19'; durationDays = 32; seriesUnitId = $unit.id
    description = 'Klassenarbeit am 16.12. Lambacher Schweizer 9, Kapitel III. Zuerst alles Kür (4 neue Aufgaben am Tag); ab 25.11. ist die Scheitelpunktform tägliche Pflicht, nach der Englisch-Arbeit (4.12.) folgen Normalform und Aufstellen.'
}
Write-Host ("Plan {0} '{1}' {2} bis {3}" -f $plan.id, $plan.title, $plan.startDate, $plan.endDate)
foreach ($ex in $created) {
    $r = Invoke-Api POST "supervisor/study-plans/$($plan.id)/positions" @{
        exerciseId = $ex.id; cadence = 'None'; useLeitner = $true; newItemsPerDay = 4; pointsGoalMet = 0; penaltyCoins = 0
    }
    Write-Host ("  {0,4}  Kür  Dosis {1}  {2}" -f $r.id, $r.newItemsPerDay, $r.exerciseTitle)
}

# --- 5. Klassenarbeit ---------------------------------------------------------------------------------------------
$null = Invoke-Api PATCH "supervisor/class-tests/$classTestId" @{
    topic = 'Lambacher Schweizer 9, Kapitel III Quadratische Funktionen (Hausaufgaben S. 73–82): ax², Scheitelpunktform, Normalform, quadratische Ergänzung, Aufstellen. Thema laut WebUntis offen.'
}
$k = Invoke-Api POST "supervisor/class-tests/$classTestId/exercises" @{ exerciseIds = @($created.id) }
Write-Host ("Klassenarbeit {0} ({1}): {2} Übungen zugeordnet" -f $k.klassenarbeit.id, $k.klassenarbeit.scheduledDate, @($k.assignedExercises).Count)
