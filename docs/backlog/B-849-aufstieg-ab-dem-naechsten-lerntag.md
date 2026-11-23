---
tags: [typ/story, status/abgenommen, bereich/api, bereich/lehrplan, bereich/training, rolle/student]
aliases: [Aufstieg der Aufbau-Stufe, TestAttempt.BuildLevel]
status: abgenommen
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
wartet_auf: ""
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
- **2026-11-23** — in-arbeit (Nachtlauf E, Sprint 122). Gebaut: Naht `PlayedExercise(pos, day)` mit
  `PlayedBuildLevel` (vor `BuildLevelEffectiveOn` die Stufe darunter auf dem Weg), Stempel
  `TestAttempt.BuildLevel = ExaminedBuildLevel(pos, attempt.Day)` beim Start, Aufstieg im `Submit` über
  `PromotedBuildLevel(pos, attempt.BuildLevel)` (nächste Stufe des Wegs, nur wenn über der aktuellen – das ist die
  Idempotenz), Ziel-Filter in `IsGoalMetAsync`, Migration `BuildLevelPromotion` (Bestand der Versuche gestaffelter
  Positionen aus `CurrentBuildLevel` gefüllt). Der Tag einer Runde ist ihr Starttag (`session.Day`/`attempt.Day`), nicht
  „jetzt“: eine Runde über Mitternacht bleibt auf ihrer Übung. Mitgenommen aus dem Review-Hinweis oben: Vater-Bericht und
  Kind-Antwort zur Rückmeldung filtern auf die Stufe; eine Stufe vom Vater gilt sofort und löscht einen wartenden Aufstieg.
- **2026-11-23** — Abweichungen beim Bauen, ohne neue Entscheidung: (a) AK 6 nennt `UntilDone`; dessen Ziel misst aber
  Nacharbeits-Runden, nie Tests (`RemediationDayMetAsync`). Der Filter sitzt im Test-Zweig von `IsGoalMetAsync` und ist
  an einer in der DB auf `Daily` gesetzten gestuften Position geprüft (über die API sperrt B-848 E3 Tages-/Wochenpflicht).
  (b) Ein bestandener Test des **Vaters** (Vorschau, `BySupervisor`) hebt nicht – nach der bestehenden Regel, dass seine
  Versuche außerhalb der Welt des Kindes bleiben (`TestAttempt.BySupervisor`); zum Umstoßen im Protokoll.
  (c) **Nicht** gebaut, weil es eine Entscheidung braucht: ob eine auf der Vorstufe beendete Nacharbeit (`CompletedOn`)
  beim Aufstieg zurückgesetzt wird – das machte „bis erledigt“ auf der neuen Stufe wieder zur Pflicht und damit zum Malus.
  Heute bleibt die Position nach dem Aufstieg pflichtfrei. Offen in B-702 notiert.
- **2026-11-23** — Review `pugling-reviewer` (Agent, nur gelesen): keine 🔴, drei 🟡, alle behoben und je mit Test belegt
  (Fehlerzähler des Sprints 3): (1) ein offener Versuch, den eine Stufenänderung des Vaters überholt hat, wurde gegen die
  Übung der neuen Stufe gewertet und konnte aus der alten geprüften Stufe die Position über die Vater-Entscheidung heben –
  jetzt `IsStale` in `Next`/`Answer`/`Submit` (404 „belongs to a level the position no longer plays“),
  `Ueberholter_Versuch_Wird_Nicht_Mehr_Gewertet`; (2) ein selbst eingeschätzter Test hob unter „nur getippte Tests“ – jetzt
  dieselbe Bedingung wie das Ziel (`!RequireTypedTest || Graded`), `Selbsteinschaetzung_Hebt_Nicht_Wenn_Getippt_Verlangt_Ist`;
  (3) der Bericht ließ Ziel-Tests von vor dem Weg fallen – Bericht und Rückmeldung teilen jetzt ein Prädikat
  `PositionPlayService.AttemptsOnLevel`, `Bericht_Behaelt_Zieltests_Von_Vor_Dem_Weg`. 🟢 ohne Zählung: Migrations-Kommentar
  präzisiert (die Füllung ist eine Näherung, nur für DBs, die `BuildLevelPlayed` schon allein hatten); bewusst offen und
  notiert: nur ein Schritt Historie in `PlayedBuildLevel` (B-702), wartender Aufstieg für den Vater unsichtbar und nicht
  „sofort“ setzbar (B-841), Rückmeldung löst die Karte für „heute“ statt für die offene Runde auf (DTO trägt keine Runde),
  `ExerciseUsageQueries.UsedBy` liest die rohe Stufe (die heute gespielte Vorstufe ist über ihre Lernstand-Zeilen geschützt).
- **2026-11-23** — AK → Test (`backend/Pugling.Api.Tests/AufbauStufeAufstiegTests.cs`), Mutante → rot (Tabelle unten,
  gemessen nach der letzten Änderung, 22/22 rot am benannten Test, Grundlinie danach grün):
  AK 1 bestanden/nicht → `Bestandener_Test_Hebt_Die_Stufe_Nicht_Bestandener_Nicht` (M1); AK 1 nächste Stufe des Wegs →
  `Aufstieg_Springt_Zur_Naechsten_Stufe_Des_Wegs` (M2); AK 2 → `Auf_Der_Ziel_Stufe_Steigt_Nichts` (M3); AK 3 23:30 →
  `Aufstieg_Um_2330_Gilt_Ab_Mitternacht` (M4), 00:30 → `Aufstieg_Um_0030_Gilt_Ab_Dem_Naechsten_Lerntag` (M4, M5 UTC-Tag);
  Runde/Test über Mitternacht → `Runde_Ueber_Mitternacht_Bleibt_Auf_Ihrer_Stufe` (M15), `Test_Ueber_Mitternacht_…` (M16,
  M17); AK 4 → `Versuch_Traegt_Die_Gespielte_Stufe` (M6 Stempel aus der Position, M7 freie Position gestempelt); AK 5 →
  `Aufstieg_Bucht_Nichts` (M8); AK 6 → `Nur_Der_Test_Des_Ziels_Erfuellt_Das_Pflichtziel` je Vorstufe/Ziel (M9); AK 7 →
  `Zweiter_Bestandener_Test_Am_Selben_Tag_Hebt_Nicht_Noch_Einmal` (M10), `Parallele_Abgaben_Heben_Einmal` als
  beschriftete Absichtserklärung (beide Anfragen laden Stufe 1 – keine Mutante macht ihn rot); AK 8 →
  `AufbauStufeAufstiegMigrationTests` (M18); Vater-Vorschau (M11), Vater-Stufe sofort (M12), Bericht (M13, M13b),
  Rückmeldung (M14), überholter Versuch (M19, M20), Selbsteinschätzung (M21). Suite 2170/2170 (Release), E2E
  `aufbau-weg-editor` + `test-durchgefallen-ueben` 2 passed, Migration an einer Kopie der echten DB geprobt (118 Versuche,
  208 Positionen, alle neuen Felder leer).

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| M1 Passed | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:483 | red | red |  |
| M2 +1 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:99 | red | red |  |
| M3 Deckel | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:97 | red | red |  |
| M4 EffectiveOn | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:36 | red | red |  |
| M5 UTC-Tag | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:487 | red | red |  |
| M6 Stempel aus Position | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:195 | red | red |  |
| M7 freie Position gestempelt | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:75 | red | red |  |
| M8 Buchung | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:487 | red | red |  |
| M9 Ziel-Filter | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:220 | red | red |  |
| M10 aus Position statt Versuch | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:484 | red | red |  |
| M11 Vater-Vorschau | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:483 | red | red |  |
| M12 Vater loescht nicht | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:492 | red | red |  |
| M13 Bericht ohne Filter | backend/Pugling.Api/Services/Supervisor/PositionReportService.cs:44 | red | red |  |
| M13b Bericht exakte Stufe | backend/Pugling.Api/Services/Supervisor/PositionReportService.cs:44 | red | red |  |
| M14 Rueckmeldung | backend/Pugling.Api/Services/Shared/ExerciseFeedbackService.cs:131 | red | red |  |
| M15 Runde: Tag jetzt | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:487 | red | red |  |
| M16 Test next: Tag jetzt | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:245 | red | red |  |
| M17 Test answer: Tag jetzt | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:282 | red | red |  |
| M18 Migration ohne Fuellung | backend/Pugling.Api/Data/Migrations/20261123151427_BuildLevelPromotion.cs:29 | red | red |  |
| M19 Answer ohne Stale | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:280 | red | red |  |
| M20 Submit ohne Stale | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:390 | red | red |  |
| M21 ungewertet hebt | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:483 | red | red |  |

Grundlinie nach dem Zurückspielen: green.
- **2026-11-23** — abgenommen (Nachtlauf E, Sprint 122), Commit `2bf20d88`. Rollengang maschinell: Sohn über die
  HTTP-Pipeline mit Kind-Login (`AufbauStufeAufstiegTests`, Berlin-Host mit eingefrorener Uhr über zwei Lerntage), Vater
  über die Supervisor-API (Bericht, Stufe setzen), Creator als Regressionszeuge (E2E `aufbau-weg-editor` grün).
  Mensch offen: Migration `BuildLevelPromotion` beim nächsten Heimnetz-Start (an einer Kopie geprobt).
