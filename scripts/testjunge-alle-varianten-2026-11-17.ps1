#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-17): Testplan für den Testjungen (Child 4), der jede Lernart, jede Stufe und jede
    Positions-Option einmal ausspielt – Stoff ist Friedrichs Englisch (Access 5, Unit 1 Australia).

.DESCRIPTION
    Legt über die API an:
    - fünf Übungen in Unit 1 (Unit 24), die es dort noch nicht gibt: Lückentext mit deutscher Vorgabe, Ablenkern und
      Wortkasten (damit jede Lückentext-Stufe etwas zu zeigen hat), Übersetzung, Birkenbihl (automatisch dekodiert gegen
      den Vokabelspeicher), Leseverstehen und Liste,
    - einen aktiven Englisch-Plan für Kind 4 ab heute für 28 Tage. Ein aktiver Plan schaltet den bisherigen Plan
      desselben Fachs (Plan 11) selbst ab (B-539) – erst wenn alle Positionen stehen; gelöscht wird nichts.
    Nicht dabei: Hörverstehen (braucht eine Audiodatei), freier Text (liefert dem Sohn keine Karte) und die beiden
    Rechen-Typen (kein Englisch-Stoff).

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
$childId = 4
$seriesId = 8
$unitId = 24
$start = '2026-11-17'
$days = 28
$planTitle = 'Englisch: Australia – alle Lernarten (Test)'

. "$PSScriptRoot\_plan-api.ps1"
Connect-PuglingApi $BaseUrl $Pin

# Wiederanlauf: ein fertiger (aktiver) Plan wird übersprungen, Übungen nur angelegt, wenn es sie in der Unit noch
# nicht gibt. Ein inaktiver Plan mit dem Titel ist der Rest eines abgebrochenen Laufs – dann Abbruch vor jedem Schreiben.
Assert-PlansResumable $childId $planTitle

Backup-RealDatabase $BaseUrl 'testjunge-alle-varianten-20261117'

function New-Exercise([string]$route, [string]$title, [int]$order, $config) {
    Get-OrNewExercise $seriesId $unitId $route @{ title = $title; orderIndex = $order; rewardPoints = 10; config = $config }
}

# --- 1. Übungen (eigene Sätze zu den Wörtern aus Part A, nicht aus dem Buch) ------------------------------------
$cloze = New-Exercise 'cloze' 'Unit 1 · Australien-Wörter im Satz' 40 @{
    text = "Life in the {{1}} can be hard because the towns are very {{2}}. Most Australians live in {{3}} areas near the sea. In the north the weather is hot and {{4}}. Be careful: some snakes and spiders are {{5}}. The view of Sydney Harbour at night is {{6}}."
    gaps = @(
        @{ index = 1; answer = 'outback';   prompt = 'das Hinterland Australiens'; distractors = @('bush', 'suburb', 'waterfront') }
        @{ index = 2; answer = 'remote';    prompt = 'abgelegen';                  distractors = @('urban', 'trendy', 'populous') }
        @{ index = 3; answer = 'coastal';   prompt = 'Küsten-';                    distractors = @('sandy', 'circular', 'botanic') }
        @{ index = 4; answer = 'humid';     prompt = 'feucht, feuchtwarm';         distractors = @('sandy', 'stunning', 'remote') }
        @{ index = 5; answer = 'poisonous'; prompt = 'giftig';                     distractors = @('incredible', 'independent', 'urban') }
        @{ index = 6; answer = 'stunning';  prompt = 'atemberaubend';              distractors = @('populous', 'remote', 'coastal') }
    )
    wordBank       = @('outback', 'remote', 'coastal', 'humid', 'poisonous', 'stunning', 'urban', 'trendy')
    cueDistractors = @('städtisch', 'modisch, angesagt', 'Vorort')
}

$translation = New-Exercise 'translation' 'Unit 1 · Sätze über Australien übersetzen' 41 @{
    sourceLang = 'de'; targetLang = 'en'
    items = @(
        @{ source = 'Australien ist ein unabhängiges Land.'; target = 'Australia is an independent country.' }
        @{ source = 'Der Koala schläft in einem Baum.';      target = 'The koala is sleeping in a tree.'; alternatives = @('The koala sleeps in a tree.') }
        @{ source = 'Sydney hat viele angesagte Vororte.';   target = 'Sydney has a lot of trendy suburbs.'; alternatives = @('Sydney has many trendy suburbs.', 'Sydney has lots of trendy suburbs.') }
        @{ source = 'Unser Flug nach Sydney war sehr lang.'; target = 'Our flight to Sydney was very long.' }
    )
}

$birkenbihl = New-Exercise 'birkenbihl' 'Unit 1 · Birkenbihl: Australien Wort für Wort' 42 @{
    learningLang = 'en'; nativeLang = 'de'; sentences = @()
}
# Nur die Sätze, die die (womöglich schon vorhandene) Übung noch nicht hat.
$known = @($birkenbihl.config.sentences | ForEach-Object learningSentence)
foreach ($s in @(
        @{ learningSentence = 'The outback is remote and dry.';     naturalTranslation = 'Das australische Hinterland ist abgelegen und trocken.' }
        @{ learningSentence = 'Many people live in coastal cities.'; naturalTranslation = 'Viele Menschen leben in Städten an der Küste.' }
        @{ learningSentence = 'Some spiders are poisonous.';         naturalTranslation = 'Manche Spinnen sind giftig.' })) {
    if ($known -contains $s.learningSentence) { continue }
    $null = Invoke-Api POST "creator/textbook-series/$seriesId/units/$unitId/birkenbihl/$($birkenbihl.id)/sentences" $s
}

$reading = New-Exercise 'reading' 'Unit 1 · Lesen: Ein Tag in Sydney' 43 @{
    text = "Mia and her family arrived in Sydney after a long flight. On the first day they walked along the waterfront to Circular Quay. From there they could see the Opera House and the Harbour Bridge. In the afternoon they visited the Botanic Garden, where hundreds of flying foxes were hanging in the trees. In the evening they took a ferry to Manly, a trendy suburb with a sandy beach."
    questions = @(
        @{ prompt = 'How did Mia''s family travel to Sydney?';     choices = @('by plane', 'by ship', 'by train');                answer = 'by plane' }
        @{ prompt = 'What could they see from Circular Quay?';     choices = @('the Opera House', 'Uluru', 'the Great Barrier Reef'); answer = 'the Opera House' }
        @{ prompt = 'Which animals were in the Botanic Garden?';   choices = @('flying foxes', 'kangaroos', 'camels');            answer = 'flying foxes' }
        @{ prompt = 'How did they get to Manly?';                  choices = @('by ferry', 'by bus', 'on foot');                  answer = 'by ferry' }
    )
}

$list = New-Exercise 'list' 'Unit 1 · Die sechs Bundesstaaten Australiens' 44 @{
    instruction = 'Nenne die sechs Bundesstaaten (states) Australiens auf Englisch.'
    ordered = $false
    items = @(
        @{ value = 'New South Wales' }, @{ value = 'Queensland' }, @{ value = 'South Australia' },
        @{ value = 'Tasmania' }, @{ value = 'Victoria' }, @{ value = 'Western Australia' }
    )
}

# --- 2. Plan -------------------------------------------------------------------------------------------------------
# Pflicht mit Malus nur an zwei Stellen (täglich Lernkarte, wöchentlich Nacharbeit); alles Übrige ist Kür, damit der
# Testjunge jede Variante ausprobieren kann, ohne dass die Münzen ins Minus laufen.
$partA = 78; $partB = 79; $partC = 80; $bookCloze = 93; $grammar = 94; $matching = 95
$positions = @(
    # Vokabelkarten: jede Stufe einmal, dazu Leiter, Dosis, Reihenfolgen, Bonusse, Stufenfahrplan. Neue Wörter kommen immer
    # erst als Kennenlernen/Lernkarte; die Stufen-Positionen laufen ohne Leiter, damit die Zielstufe schon in der zweiten
    # Runde erscheint (mit Leiter erst ab Kasten 3, also nach Tagen). Die Hinkarte endet bei der Auswahl (B-597).
    @{ label = 'Lernkarte → Leiter (Pflicht täglich)'; exerciseId = $partA; stage = 4; cadence = 'Daily'; newItemsPerDay = 4; useLeitner = $true; requireTypedTest = $true; goalThreshold = 80; pointsGoalMet = 20; penaltyCoins = 2 }
    @{ label = 'Stufe 1 Beide zeigen';         exerciseId = $partB; stage = 1; itemCount = 10; useLeitner = $false }
    @{ label = 'Stufe 2 Selbsteinschätzung, zufällig'; exerciseId = $partB; stage = 2; itemCount = 10; useLeitner = $false; orderStrategy = 'Random' }
    @{ label = 'Stufe 3 Buchstabenkästchen, nachsichtig, der Reihe nach (Pflicht wöchentlich)'; exerciseId = $partB; stage = 3; itemCount = 10; useLeitner = $true; forgivingDemotion = $true; orderStrategy = 'Serial'; cadence = 'Weekly'; pointsGoalMet = 15; penaltyCoins = 0 }
    @{ label = 'Stufe 4 Freitext';             exerciseId = $partB; stage = 4; itemCount = 10; useLeitner = $false; requireTypedTest = $true }
    @{ label = 'Stufe 5 Diktat';               exerciseId = $partB; stage = 5; itemCount = 10; useLeitner = $false; requireTypedTest = $true }
    @{ label = 'Stufe 6 Auswahl, neueste zuerst, Tempo-Bonus'; exerciseId = $partB; stage = 6; itemCount = 10; useLeitner = $false; orderStrategy = 'NewestWeighted'; speedThresholdSeconds = 8; speedBonusPoints = 5 }
    @{ label = 'Stufe 7 Buchstabensalat';      exerciseId = $partB; stage = 7; itemCount = 10; useLeitner = $false }
    @{ label = 'Stufe 8 Lernkarte';            exerciseId = $partC; stage = 8; itemCount = 6; useLeitner = $false }
    @{ label = 'Stufenfahrplan 1→2→6→3→4, Leiter 3 Kästen, Zeitfenster ×2, Combo'; exerciseId = $partC; stage = 4; itemCount = 10; scope = 'New'; useLeitner = $true
       maxBox = 3; boxIntervalDays = @(0, 1, 3, 7)
       stageSchedule = @(@{ dayNumber = 1; stage = 1 }, @{ dayNumber = 2; stage = 2 }, @{ dayNumber = 4; stage = 6 }, @{ dayNumber = 7; stage = 3 }, @{ dayNumber = 10; stage = 4 })
       timeSlots = @(@{ name = 'Nachmittag'; start = '15:00:00'; end = '18:00:00'; multiplier = 2.0 })
       comboThreshold = 3; comboBonusPoints = 10 }
    # Lückentext: jede Stufe einmal am neuen Text mit Vorgabe und Ablenkern, dazu die Nacharbeit.
    @{ label = 'Lücke Stufe 2 Wortkasten';     exerciseId = $cloze.id; stage = 2; useLeitner = $true }
    @{ label = 'Lücke Stufe 3 Übersetzung + Freitext'; exerciseId = $cloze.id; stage = 3; useLeitner = $true }
    @{ label = 'Lücke Stufe 4 nur Freitext';   exerciseId = $cloze.id; stage = 4; useLeitner = $true; requireTypedTest = $true }
    @{ label = 'Lücke Stufe 5 Schultest-Kasten'; exerciseId = $cloze.id; stage = 5; useLeitner = $true; requireTypedTest = $true }
    @{ label = 'Lücke Stufe 6 Buchstabenkästchen'; exerciseId = $cloze.id; stage = 6; useLeitner = $true }
    @{ label = 'Lücke Stufe 7 Buchstabensalat'; exerciseId = $cloze.id; stage = 7; useLeitner = $true }
    @{ label = 'Lücke Stufe 8 Auswahl je Lücke'; exerciseId = $cloze.id; stage = 8; useLeitner = $true }
    @{ label = 'Lücke Nacharbeit (Pflicht wöchentlich)'; exerciseId = $cloze.id; stage = 3; remediation = $true; cadence = 'Weekly'; goalThreshold = 80; pointsGoalMet = 30; penaltyCoins = 2 }
    @{ label = 'Buch-Lückentext (Grammatik)';  exerciseId = $bookCloze }
    # Zuordnung beide Stufen, dann die Typen ohne Stufen.
    @{ label = 'Zuordnung direkt';             exerciseId = $matching; stage = 1 }
    @{ label = 'Zuordnung mit Ablenkern, Leiter'; exerciseId = $matching; stage = 2; useLeitner = $true }
    @{ label = 'Grammatik';                    exerciseId = $grammar }
    @{ label = 'Übersetzung';                  exerciseId = $translation.id }
    @{ label = 'Birkenbihl';                   exerciseId = $birkenbihl.id }
    @{ label = 'Lesen (ganz durchlesen = Ziel)'; exerciseId = $reading.id; goalThreshold = 100 }
    @{ label = 'Liste';                        exerciseId = $list.id }
)

$bodies = foreach ($def in $positions) {
    $body = @{}
    foreach ($k in $def.Keys) { if ($k -ne 'label') { $body[$k] = $def[$k] } }
    if (-not $body.ContainsKey('cadence')) { $body.cadence = 'None'; $body.pointsGoalMet = 0; $body.penaltyCoins = 0 }
    $body
}
$created = @(New-StudyPlan @{
        childId = $childId; title = $planTitle; subjectId = 1; seriesUnitId = $unitId
        startDate = $start; durationDays = $days
        description = 'Testplan: jede Lernart, jede Stufe und jede Positions-Option einmal, Stoff Access 5 Unit 1 (Friedrichs Englisch). Pflicht nur die Lernkarte (täglich) und die Lückentext-Nacharbeit (wöchentlich), Rest Kür. Ersetzt Plan 11.'
    } @($bodies))
for ($i = 0; $i -lt $created.Count; $i++) {
    $r = $created[$i]
    $kind = if ($r.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($r.cadence)" }
    Write-Host ("  {0,4}  {1,-15} {2}  [{3}]" -f $r.id, $kind, $r.exerciseTitle, $positions[$i].label)
}

Write-Host ''
Write-Host "Pläne von Kind $childId danach:"
foreach ($p in (Get-ApiList "supervisor/study-plans?childId=$childId")) {
    Write-Host ("  Plan {0,3}  {1}  {2}" -f $p.id, ($p.active ? 'aktiv' : 'aus  '), $p.title)
}
