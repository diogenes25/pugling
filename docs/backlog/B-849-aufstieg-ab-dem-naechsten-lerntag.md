---
tags: [typ/story, status/geschaetzt, bereich/api, bereich/lehrplan, bereich/training, rolle/student]
aliases: [Aufstieg der Aufbau-Stufe, TestAttempt.BuildLevel]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-701
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: "B-848 (gestufte Position spielt ihre Stufe) – baut auf PlayedExercise auf"
---

# B-849 · Aufstieg nach bestandenem Test, wirksam ab dem nächsten Lerntag

Teil 2 von 2 aus [B-701](B-701-position-spielt-die-aufbau-stufe.md) (geteilt). Setzt
[B-848](B-848-gestufte-position-spielt-ihre-stufe.md) voraus. Abstieg und Pflichtziel bleiben in
[B-702](B-702-abstieg-und-pflichtziel-der-aufbau-stufe.md).

## User Story

Als *Sohn* möchte ich nach einem bestandenen Test die nächste Stufe bekommen, damit ich sehe, dass ich vorankomme.

## Ist-Stand am Code

- Bestanden wird über `attempt.Passed = ScorePercent >= passPercent` (`PositionTestsController.cs:460`). Ein Versuch
  trägt `PlanPositionId`, aber keine Stufe (`TestAttempt`, `Models/StudyPlanEntities.cs:170-195`).
- `IsGoalMetAsync` (`Services/Shared/PositionProgressService.cs:195`) liest die bestandenen Versuche der Position, ohne
  auf die Stufe zu achten.

## Entscheidungen

Geerbt aus B-701. E3–E6 wurden am 2026-11-19 bestätigt, E8 hat der Nutzer am 2026-11-22 im Dialog entschieden.

1. **(E3)** Ein bestandener Test der gespielten Stufe hebt die Stufe um eins, höchstens bis zur Ziel-Übung.
2. **(E4)** Der Aufstieg ist atomar mit dem Abschluss des Versuchs, hat keinen eigenen Endpunkt und ist idempotent über
   den Versuch.
3. **(E5)** `TestAttempt.BuildLevel` (nullable) merkt sich die gespielte Stufe.
4. **(E6)** Ein Test einer Vorstufe zählt nicht für das Pflichtziel; `IsGoalMetAsync` filtert auf die Ziel-Stufe.
5. **(E8)** Der Aufstieg wirkt ab dem nächsten Lerntag. Der Test setzt die Stufe sofort, gespielt wird sie ab morgen
   (`PlanPosition.BuildLevelEffectiveOn`, ein `DateOnly` aus `LearningDay`). Heute laufen Sitzung, Tagesdosis und
   Lernkarten auf der alten Stufe zu Ende.

## Akzeptanzkriterien

1. (B-701 AK 3) Ein bestandener Test auf Stufe 1 setzt die Stufe auf 2; ein nicht bestandener lässt sie stehen. Rot:
   Aufstieg ohne Prüfung von `Passed`.
2. (B-701 AK 4) Auf der Ziel-Stufe steigt nichts mehr. Rot: fehlender Deckel.
3. (E8) Am Tag des Aufstiegs spielt die Position weiter die alte Stufe, am nächsten Lerntag (deutsche Zeit) die neue.
   Rot: ein `PlayedExercise`, das `BuildLevelEffectiveOn` nicht liest. Je ein Fall um 23:30 und um 00:30.
4. (B-701 AK 6) Ein Versuch einer gestuften Position trägt die gespielte Stufe, der einer ungestuften `null`.
5. (B-701 AK 7) Der Aufstieg bucht keine Münzen und keinen Malus.
6. (E6) Ein bestandener Test einer Vorstufe erfüllt das Ziel einer `UntilDone`-Position nicht, einer der Ziel-Stufe
   schon.
7. (E4) Zwei parallele Abschlüsse heben die Stufe nur einmal. Rot: ein Zähler zeigt zwei Aufstiege.
8. Die Migration nimmt Versuche und Positionen mit `null` mit.

## Schätzung

**Größe: M** – ein Zustandsübergang im Testabschluss, ein Filter im Ziel und zwei nullable Spalten. Nicht L, weil die
Spielpfade in B-848 schon auf `PlayedExercise` laufen.

- **`migration: ja`** – `TestAttempts.BuildLevel` und `PlanPositions.BuildLevelEffectiveOn`.
- **`vertragsbruch: nein`** – `AttemptResponse` bekommt höchstens ein trailing optionales Feld.

### Angriffsplan

1. Migration.
2. Aufstieg im Abschlusspfad mit Einmal-Sperre.
3. `PlayedExercise` liest `BuildLevelEffectiveOn`.
4. Ziel-Filter in `IsGoalMetAsync`.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- `AufbauStufeAufstiegTests` deckt AK 1–7 ab; AK 3 mit eingefrorener Berlin-Uhr wie in `TagesbegriffTests`.
- Ein Migrationstest deckt AK 8 ab.
- `PflichtMalusTests` und `PositionTestFlowTests` bleiben grün.
- Reviewer `pugling-reviewer`.
- Rollengang als Sohn über zwei Lerntage.

## Verlauf

- **2026-11-22** — Aus B-701 geteilt, nachdem die Story im Dialog gegrillt war; der Nutzer hat die Entscheidungen
  bestätigt. Geschätzt: M, backend, Migration ja, Vertrag additiv. Wartet auf B-848.
- **2026-11-23** — Hinweise aus dem Review von B-848 (Nachtlauf D, Sprint 121), hier hingehörend, weil diese Story den
  Versuch mit der Stufe stempelt (E5): Die Trefferquote je Aufgabe im Vater-Bericht (`PositionReportService`) und die
  Kind-Antwort zu einer Rückmeldung (`ExerciseFeedbackService.ChildAnswerAsync`) gruppieren Testergebnisse nur nach
  `ItemIndex` und mischen darum Versuche beider Stufen – mit `TestAttempt.BuildLevel` filtern. Außerdem: eine auf der
  Vorstufe beendete Nacharbeit hinterlässt `CompletedOn` an der Position; beim Aufstieg muss das zurückgesetzt werden, sonst
  wird „bis erledigt“ auf der nächsten Stufe nie wieder Pflicht. B-848 sperrt bis dahin nur den Wechsel unter einer offenen
  Runde oder einem offenen Test von heute (`build_level_in_play`).
