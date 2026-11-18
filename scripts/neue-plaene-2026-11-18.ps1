#requires -Version 7
<#
.SYNOPSIS
    Einmal-Skript (2026-11-18): neue Lehrpläne für Theodor und Friedrich, die alten vier (8, 9, 10, 12) gehen aus.

.DESCRIPTION
    Legt über die API an:
    - die Übung „Unit 1 · Nacharbeit Vokabeltest vom 9.11." (Lückentext mit deutscher Vorgabe je Lücke),
    - je Kind und Fach einen Plan ab 18.11. für 28 Tage. Ein aktiver Plan schaltet den bisherigen Plan desselben
      Fachs selbst ab (B-539) – erst wenn alle Positionen stehen; gelöscht wird nichts.
    Wenige Tagespflichten (Vokabeln mit Lernkarte und Leiter), der Rest ist Kür; Lückentexte laufen als
    „Nacharbeit" (leichter schalten, erledigt erst frei getippt).

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
. "$PSScriptRoot\_plan-api.ps1"
$start = '2026-11-18'
$days = 28

Connect-PuglingApi $BaseUrl $Pin

# Wiederanlauf: fertige (aktive) Pläne werden übersprungen, die Übung nur angelegt, wenn es sie noch nicht gibt.
# Ein inaktiver Plan mit einem der Titel ist der Rest eines abgebrochenen Laufs – dann Abbruch vor jedem Schreiben.
$titles = @{
    thEn = 'Englisch: Unit 1 – Lernkarte und Test-Nacharbeit'
    thLa = 'Latein: Lektion 1 – Wörter mit Lernkarte'
    frFr = 'Französisch: Unité 1 – Wörter mit Lernkarte'
    frEn = 'Englisch: Unit 1 Australia – Wörter mit Lernkarte'
}
Assert-PlansResumable 1, 3 @($titles.Values)

# Sicherung der echten Datenbank, bevor etwas geschrieben wird (nur am Heimnetz-Server).
Backup-RealDatabase $BaseUrl 'neue-plaene-20261118'

# --- 1. Übung: Theodors Vokabeltest vom 9.11. zum Nacharbeiten ---------------------------------------------------
# Deutsche Vorgaben wörtlich vom Testblatt; die englischen Rahmensätze sind eigene (nicht aus dem Buch).
# Ablenker sind die Ausweichwörter, die im Test nicht zählten.
$tasks = @(
    @{ de = 'Richtung Osten';                                   en = '{0}';                              a = 'eastbound';        d = 'in the east', 'eastern' }
    @{ de = 'Westeuropa (two words)';                           en = '{0}';                              a = 'Western Europe';   d = 'West Europe', 'Westeurope' }
    @{ de = 'Ich hätte gern Reis anstelle von/statt Pommes.';   en = "I'd like rice {0} chips.";         a = 'instead of';       d = 'in place', 'except' }
    @{ de = 'Während des Films schlief ich ein.';               en = 'I fell asleep {0} the film.';      a = 'during';           d = 'while', 'meanwhile' }
    @{ de = 'Pink und orange passen nicht zusammen.';           en = "Pink and orange don't {0}.";       a = 'go together';      d = 'fit together', 'come together' }
    @{ de = 'in einem Hotel übernachten';                       en = 'to {0} a hotel';                   a = 'stay at';          d = 'sleep in', 'live at'; alt = 'stay in' }
    @{ de = 'erstaunlich, unglaublich';                         en = '{0}';                              a = 'amazing';          d = 'unbelievable', 'amusing' }
    @{ de = 'Inhalt';                                           en = '{0}';                              a = 'content';          d = 'contest', 'context' }
    @{ de = 'Er reagierte nicht.';                              en = "He didn't {0}.";                   a = 'react';            d = 'answer', 'reply' }
    @{ de = 'Der Löwe brüllt.';                                 en = 'The lion {0}.';                    a = 'roars';            d = 'shouts', 'screams' }
    @{ de = 'eine vierköpfige Familie';                         en = '{0}';                              a = 'a family of four'; d = 'a four family', 'a family with four' }
    @{ de = 'etw. erwarten';                                    en = '{0}';                              a = 'expect';           d = 'wait', 'except'; alt = 'to expect', 'expect sth.', 'to expect sth.' }
    @{ de = 'Dein Englisch wird sich verbessern.';              en = 'Your English will {0}.';           a = 'improve';          d = 'get better', 'prove' }
    @{ de = 'Ich mag Musik!';                                   en = 'I {0} music!';                     a = 'am into';          d = 'like', 'am in'; alt = "'m into" }
    @{ de = 'Eintritt';                                         en = '{0}';                              a = 'entry';            d = 'entrance', 'enter' }
    @{ de = 'Dunkelheit';                                       en = '{0}';                              a = 'darkness';         d = 'the dark', 'darkly' }
    @{ de = 'Ich rufe dich an, sobald ich zu Hause bin.';       en = "I'll call you {0} I'm at home.";   a = 'as soon as';       d = 'when', 'so soon' }
    @{ de = 'auf eine Schlange hinweisen';                      en = 'to {0} a snake';                   a = 'point out';        d = 'point at', 'show on' }
    @{ de = 'Landschildkröte';                                  en = '{0}';                              a = 'tortoise';         d = 'turtle', 'toad' }
    @{ de = 'Alle reden gleichzeitig.';                         en = 'Everybody is talking {0}.';        a = 'at one time';      d = 'at the same time', 'in one time' }
)
$lines = @(); $gaps = @()
for ($i = 0; $i -lt $tasks.Count; $i++) {
    $t = $tasks[$i]; $n = $i + 1
    $lines += ('{0}. ' -f $n) + ($t.en -f "{{$n}}")
    $gap = @{ index = $n; answer = $t.a; prompt = $t.de; distractors = @($t.d) }
    if ($t.alt) { $gap.alternatives = @($t.alt) }
    $gaps += $gap
}
$nacharbeit = Get-OrNewExercise 5 7 'cloze' @{
    title        = 'Unit 1 · Nacharbeit Vokabeltest vom 9.11.'
    orderIndex   = 30
    rewardPoints = 10
    config       = @{ text = ($lines -join "`n"); gaps = $gaps }
}

# --- 2. Pläne ------------------------------------------------------------------------------------------------------
# Die Dosis zählt Karten: bei „beide Richtungen“ hat jedes Wort zwei, 4 Karten sind also 2 neue Wörter am Tag.
# Tagespflicht Vokabeln: neue Wörter als Lernkarte (kommt von selbst), Test getippt, 80 %, 20 Münzen / 2 Malus.
function Duty([int]$ex, [int]$perDay) {
    @{ exerciseId = $ex; stage = 4; cadence = 'Daily'; newItemsPerDay = $perDay; goalThreshold = 80
       requireTypedTest = $true; useLeitner = $true; pointsGoalMet = 20; penaltyCoins = 2 }
}
# Kür Vokabeln: dieselbe Lernkarte, kleine Dosis, kein Ziel und kein Malus.
function FreeVocab([int]$ex, [int]$perDay) {
    @{ exerciseId = $ex; stage = 4; cadence = 'None'; newItemsPerDay = $perDay
       requireTypedTest = $true; useLeitner = $true; pointsGoalMet = 0; penaltyCoins = 0 }
}
# Kür Lückentext als Nacharbeit: frei beginnen, je Lücke leichter schalten, erledigt erst frei getippt.
function FreeCloze([int]$ex) {
    @{ exerciseId = $ex; cadence = 'None'; remediation = $true; pointsGoalMet = 0; penaltyCoins = 0 }
}
function Free([int]$ex) { @{ exerciseId = $ex; cadence = 'None'; pointsGoalMet = 0; penaltyCoins = 0 } }

$plans = @(
    @{  # Theodor – Englisch (4 Wochen: Part A und B sind die Listen seiner 13 Testfehler)
        childId = 1; title = $titles.thEn; subjectId = 1; seriesUnitId = 7
        description = 'Unit 1 This is London. Pflicht täglich: Part A und Part B, je 2 neue Wörter am Tag als Lernkarte mit „Mach''s mir leichter“. Pflicht wöchentlich: der Vokabeltest vom 9.11. als Nacharbeit. Rest ist Kür. Ersetzt Plan 9.'
        positions = @(
            (Duty 13 4), (Duty 14 4),
            @{ exerciseId = $nacharbeit.id; stage = 3; cadence = 'Weekly'; remediation = $true; goalThreshold = 80
               pointsGoalMet = 30; penaltyCoins = 2 },
            (FreeVocab 15 4), (FreeVocab 16 4),
            (FreeCloze 19), (FreeCloze 20), (FreeCloze 21), (Free 22), (Free 67)
        )
    },
    @{  # Theodor – Latein
        childId = 1; title = $titles.thLa; subjectId = 5; seriesUnitId = 21
        description = 'VIVA 1. Pflicht täglich: beide Wortlisten der Lektion 1, je 2 neue Wörter am Tag als Lernkarte. Lektion 2 und 3 sind Kür, bis Lektion 1 sitzt. Ersetzt Plan 10.'
        positions = @(
            (Duty 68 4), (Duty 69 4),
            (FreeVocab 70 4), (FreeVocab 71 4),
            (Free 72), (Free 73), (Free 74), (Free 75), (FreeCloze 76), (Free 77)
        )
    },
    @{  # Friedrich – Französisch
        childId = 3; title = $titles.frFr; subjectId = 4; seriesUnitId = 13
        description = 'Découvertes 2, Unité 1. Pflicht täglich: La rentrée und Atelier A, je 2 neue Wörter am Tag als Lernkarte. Atelier B, Unité 2 und die Grammatik sind Kür; Lückentexte lassen sich leichter schalten. Ersetzt Plan 8.'
        positions = @(
            (Duty 34 4), (Duty 35 4),
            (FreeVocab 36 4), (FreeVocab 37 4), (FreeVocab 33 4),
            (FreeCloze 41), (FreeCloze 42), (FreeCloze 43), (Free 44),
            (Free 45), (FreeCloze 46), (Free 47), (Free 39), (Free 40)
        )
    },
    @{  # Friedrich – Englisch
        childId = 3; title = $titles.frEn; subjectId = 1; seriesUnitId = 24
        description = 'Access 5, Unit 1. Pflicht täglich: Part A, 2 neue Wörter am Tag als Lernkarte. Part B und C und die Grammatik sind Kür. Ersetzt Plan 12.'
        positions = @(
            (Duty 78 4),
            (FreeVocab 79 4), (FreeVocab 80 4),
            (FreeCloze 93), (Free 94), (Free 95)
        )
    }
)

foreach ($def in $plans) {
    $planBody = @{
        childId = $def.childId; title = $def.title; subjectId = $def.subjectId; seriesUnitId = $def.seriesUnitId
        startDate = $start; durationDays = $days; description = $def.description
    }
    foreach ($r in @(New-StudyPlan $planBody $def.positions)) {
        $kind = if ($r.cadence -eq 'None') { 'Kür    ' } else { "Pflicht $($r.cadence)" }
        $extra = @()
        if ($r.newItemsPerDay) { $extra += "$($r.newItemsPerDay / 2) neue Wörter/Tag" }
        if ($r.remediation) { $extra += 'Nacharbeit' }
        if ($r.pointsGoalMet) { $extra += "$($r.pointsGoalMet)/$($r.penaltyCoins) Münzen" }
        Write-Host ("  {0,4}  {1,-15} {2}  [{3}]" -f $r.id, $kind, $r.exerciseTitle, ($extra -join ', '))
    }
}

Write-Host ''
Write-Host 'Aktive Pläne danach:'
foreach ($childId in 1, 3) {
    foreach ($p in (Get-ApiList "supervisor/study-plans?childId=$childId")) {
        if ($p.active) { Write-Host "  Kind $childId  Plan $($p.id)  $($p.title)" }
    }
}
