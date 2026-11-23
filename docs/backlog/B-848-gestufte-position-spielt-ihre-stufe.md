---
tags: [typ/story, status/abgenommen, bereich/api, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Stufe spielen, wirksame Übung, Lernstand je Übung]
status: abgenommen
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-701
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-23"
wartet_auf: ""
---

# B-848 · Eine gestufte Position spielt die Übung ihrer aktuellen Stufe

Teil 1 von 2 aus [B-701](B-701-position-spielt-die-aufbau-stufe.md) (geteilt). Baut auf
[B-700](B-700-aufbau-stufe-am-katalog.md) auf (gebaut, `e36a5c79`). Der Aufstieg ist
[B-849](B-849-aufstieg-ab-dem-naechsten-lerntag.md); diese Story setzt die Stufe nur über die Start-Stufe des Vaters.

## User Story

Als *Sohn* möchte ich zuerst die leichtere Übung zu einem Thema spielen, damit ich auf dem Weg zum Stoff Erfolge habe.
Als *Vater* möchte ich das je Position einschalten, indem ich das Ziel wähle und die Start-Stufe festlege.

## Ist-Stand am Code

- Eine Position hängt an genau einer Übung (`PlanPosition.ExerciseId`). `pos.Exercise` wird in `backend/Pugling.Api` an
  61 Stellen gelesen (Typ, Config, Vorgabe-Stufe, Prüfmodus, Titel, `BookPages`). Der Inhalt kommt über eine Naht:
  `PositionPlayService.ItemsOfAsync` (`Services/Shared/PositionPlayService.cs:57`).
- `PositionItemProgress` ist `(PlanPositionId, ItemIndex)` ohne Übung (`Models/PlanPositionEntities.cs:255-300`) und
  wird an 50 Stellen in 16 Dateien gelesen. Dort liegen auch `IntroducedAt`, `ImprintedAt` (Lernkarte) und
  `RemediationDoneAt`.
- Der plan-übergreifende Stand schreibt `pos.ExerciseId` (`PositionTestsController.cs:446`, im Review-Pfad ebenso).
- Die Positionsliste für Kind und Vater kommt aus `PositionProgressService` (`PositionStatus`, `:345-374`), mit Titel
  und `BookPages` der Übung der Position.

## Entscheidungen

Geerbt aus B-701. E1, E2 und E6 wurden am 2026-11-19 bestätigt, E7 und E9 hat der Nutzer am 2026-11-22 im Dialog
entschieden.

1. **(B-701 E1)** `PlanPosition.CurrentBuildLevel` (nullable); `null` heißt keine Stufung, wie bisher.
2. **(B-701 E2)** Der Vater wählt im Positionsformular die Start-Stufe (Vorgabe: die niedrigste vorhandene Vorstufe).
   Das Feld kommt additiv zum Anlegen und Ändern einer Position. Der Server lehnt es ab, wenn die Ziel-Übung keine
   Vorstufe dieser Stufe hat.
3. **(B-701 E6)** Bis B-702 gibt es Stufung nur mit `Cadence = None` oder `UntilDone`. Eine gestufte Position mit
   Tages- oder Wochenpflicht lehnt der Server ab.
4. **(B-701 E7)** Der Positions-Lernstand hängt an der gespielten Übung: `PositionItemProgress.ExerciseId` (Pflicht),
   Schlüssel `(PlanPositionId, ExerciseId, ItemIndex)`, der Bestand wird mit der Übung der Position gefüllt.
5. **(B-701 E9)** Das Kind sieht Titel und Buchseite der gespielten Übung. Der Vater sieht die Ziel-Übung plus
   „Stufe n von m · gerade: <Vorstufe>“.

Für den Bau festgehalten; das folgt aus E8 in B-849 und ist keine neue Produktregel: Die gespielte Stufe entsteht an
**einer** Stelle, `PositionPlayService.PlayedExercise(pos, today)`. B-849 hängt dort „wirkt ab dem nächsten Lerntag“
ein; diese Story liest nur `CurrentBuildLevel`.

## Akzeptanzkriterien

1. (B-701 AK 1) Eine Position mit Vorstufe und `CurrentBuildLevel = 1` liefert beim Üben und im Test die Aufgaben der
   Vorstufe. Rot: ein `ItemsOfAsync`, das weiter `pos.Exercise` liest.
2. (B-701 AK 2) Ohne Vorstufen oder mit `CurrentBuildLevel = null` spielt die Position unverändert die Ziel-Übung. Rot:
   `null` wird als Stufe 0 behandelt.
3. Typ, Config, Vorgabe-Stufe und Prüfmodus kommen von der gespielten Übung; eine Vorstufe darf einen anderen Typ
   haben. Rot: ein Spielpfad, der Typ oder Config der Ziel-Übung liest. Je Pfad ein Fall: Üben, Antwort, Test, Abgabe,
   leichtere Stufe.
4. (B-701 AK 5, E7) Der Lernstand derselben Position auf zwei Übungen liegt getrennt. Ein Stand der Ziel-Übung erscheint
   nicht in der Vorstufe, und umgekehrt. Rot: ein Zugriff ohne Filter auf `ExerciseId`.
5. Der plan-übergreifende Stand schreibt die gespielte Übung. Rot: er schreibt weiter `pos.ExerciseId`.
6. Die Start-Stufe wird gesetzt und zurückgelesen; ohne passende Vorstufe wird sie abgelehnt. Eine Tages- oder
   Wochenpflicht mit Stufe wird abgelehnt.
7. (E9) Die Kind-Übersicht zeigt Titel und Buchseite der Vorstufe. Vater-Übersicht und Bericht zeigen das Ziel plus
   „Stufe n von m“.
8. (B-701 AK 8) Die Migration nimmt Positionen (`CurrentBuildLevel = null`) und den Lernstand (`ExerciseId` = Übung der
   Position) mit. Box, Fälligkeit und Lernkarte bleiben unverändert. Rot: eine Spalte ohne Füllung.

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe. Dazu gehören eine Migration an einer Tabelle mit echtem Lernstand (neuer
Schlüssel), eine zentrale Naht für 61 Lesestellen und 50 Zugriffe mit neuem Filter. Nicht XL, weil der Aufstieg
(B-849) und die Oberfläche (B-841) draußen sind.

- **`migration: ja`** – `PlanPositions.CurrentBuildLevel` und `PositionItemProgress.ExerciseId` samt Schlüsselwechsel;
  gefüllt wird per `UPDATE … FROM PlanPositions`.
- **`vertragsbruch: nein`** – nur additive Felder an Positions-DTOs und `PositionStatus`.

### Risiken

- Ein vergessener `pos.Exercise`-Leser spielt still die Ziel-Übung, ohne Absturz. Gegenmittel: AK 3 je Pfad, dazu ein
  Quelltext-Tor, das in den Spielpfaden `pos.Exercise` nur über `PlayedExercise` erlaubt (beim Bau messen, ob es trägt).
- Der neue Schlüssel trifft Remediation (`RemediationDoneAt`), Lernkarte und Tagesdosis; ihre Tests müssen grün
  bleiben.
- Die echte DB: vor dem Heimnetz-Start eine Probe gegen eine Kopie (Root-`CLAUDE.md`, Schema & Migrationen).

### Angriffsplan

1. Migration mit Spalten, Füllung und neuem Unique-Index; Migrationstest mit Bestand aus Box, Lernkarte und Remediation.
2. `PlayedExercise(pos, today)` einführen und alle Spielpfade darauf umstellen; `PositionItemProgress`-Zugriffe mit
   `ExerciseId`.
3. Positions-Anlegen und -Ändern: Start-Stufe, Validierung, Kadenz-Sperre.
4. Anzeige nach E9 in `PositionProgressService` und im Report.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- `AufbauStufePlayTests` deckt AK 1–7 ab, `AufbauStufePlayMigrationTests` AK 8.
- `PositionPracticeFlowTests`, `PositionTestFlowTests`, `PflichtMalusTests` sowie die Lernkarten- und
  Remediation-Tests bleiben grün.
- Reviewer `pugling-reviewer`.
- Rollengang als Sohn an einer gestuften Position, über die API, bis B-841 die Oberfläche bringt.

## Verlauf

- **2026-11-22** — Aus B-701 geteilt, nachdem die Story im Dialog gegrillt war; der Nutzer hat die Entscheidungen
  bestätigt. Geschätzt: L, backend, Migration ja, Vertrag additiv.
- **2026-11-23** — in-arbeit (Nachtlauf D, Sprint 121). Gebaut nach den Entscheidungen 1–5 (Freigabe 8). Drei
  Bau-Entscheidungen, die aus den AK folgen und keine neue Produktregel sind:
  - Die Naht liest die neue Navigation `Exercise.Prerequisites`; `null` heißt „nicht geladen“ und **wirft** auf einer
    gestuften Position. Das ist das Quelltext-Tor aus den Risiken, nur als Laufzeit-Tor: ein vergessenes Include wird ein
    lauter Fehler statt eines still gespielten Ziels.
  - `Stage`/`StageSchedule` der Position gelten nicht, solange eine Vorstufe gespielt wird: der Vater hat sie gegen den
    Typ der Ziel-Übung gesetzt (AK 3: die Vorgabe-Stufe kommt von der gespielten Übung). Eigener Fund vor dem Review,
    **gezählt** (Zähler 1/5).
  - Eine gespielte Vorstufe ist nicht löschbar (FK Restrict am Lernstand, `ExerciseUsageQueries` zählt die Position) –
    409 `exercise_in_use` statt 500; die Item-Sperren (B-328) gelten für sie, sobald sie Lernstand trägt.
  Bewusst unverändert: `MetricsService.NewWords` zählt eingeführte Wörter über beide Übungen (ein Wort, das auf der
  Vorstufe und am Ziel neu war, zählt zweimal); die Test-Trefferquote im Vater-Bericht mischt Versuche beider Stufen, bis
  B-849 den Versuch mit `BuildLevel` stempelt; ein Wechsel der Stufe durch den Vater mitten in einer offenen Runde
  oder einem offenen Test ist nicht gesperrt (B-849 macht den Wechsel tageweise).
  Rote Probe vor dem Bau gibt es für einen Wunsch nicht; der Beleg ist die Mutanten-Tabelle unten.
  Verifikation je AK (Fall → Test → Mutante, gemessen nach der letzten Code-Änderung, `mutant-probe.py`, 15:15):
  - AK 1, Üben/Test → `AufbauStufePlayTests.Ueben_Spielt_Die_Vorstufe_Mit_Ihrem_Typ`, `Test_Spielt_Die_Vorstufe_Bis_Zur_Abgabe` → M1 rot.
  - AK 2, `null`/Stufe 3 → `Ohne_Stufe_Und_Auf_Der_Zielstufe_Spielt_Die_Zieluebung` → M2 rot. Die Mutante „Stufe 3 als
    Vorstufen-Stufe gelesen“ (Runde 1, M3) ist **äquivalent**: ohne Vorstufe auf Stufe 3 fällt die Naht ohnehin auf das
    Ziel zurück – gemessen grün, darum aus Runde 2 genommen, kein Beleg.
  - AK 3, je Pfad: Üben → M7, Test-Next → M9, Sammel-Abgabe (`Sammelabgabe_Wertet_Mit_Dem_Typ_Der_Vorstufe`, nach Runde 1
    ergänzt: M10 war dort grün) → M10, Positions-Stufe → M22, leichtere Stufe → `Leichtere_Stufe_Bleibt_Auf_Der_Vorstufe`
    (dieselbe Naht wie M1; ein eigener Mutant nur für `Easier` fehlt – der Pfad liest nichts außer `ItemsOfAsync`/`TypeOf`).
  - AK 4 → `Lernstand_Liegt_Je_Gespielter_Uebung_Getrennt` → M4 (Lesefilter), M5 (Zeile anlegen).
  - AK 5 → derselbe Test (Historie `ItemReviewEvent.ExerciseId`) → M8.
  - AK 6 → `Startstufe_Wird_Gesetzt_Gelesen_Und_Geprueft` → M11–M15.
  - AK 7 → `Kind_Sieht_Die_Vorstufe_Vater_Das_Ziel_Mit_Stufe` → M16–M19.
  - AK 8 → `AufbauStufePlayMigrationTests.Bestand_Bekommt_Die_Uebung_Seiner_Position` → M20; dazu Probe gegen eine Kopie
    der echten `pugling.db` (SQLite-Backup, Stand `TowerGameRuns`): 662 Lernstand-Zeilen, 0 ungefüllt, 0 abweichend von der
    Übung ihrer Position, `foreign_key_check` leer. EF warnt wie bei `AufbauStufe`, dass der Tabellen-Neubau nicht in
    einer Transaktion läuft (bekannt, siehe Gedächtnis „nicht-transaktionale Altmigration“) – das Startskript sichert vorher.
  - Naht wirft → `Gespielte_Uebung_Ohne_Geladene_Vorstufen_Wirft` → M6; Löschschutz → `Gespielte_Vorstufe_Ist_Nicht_Loeschbar` → M21, M28.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 AK1 Inhalt | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:110 | red | red |  |
  | M2 AK2 null | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:35 | red | red |  |
  | M4 AK4 Karten-Filter | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:220 | red | red |  |
  | M5 AK4 Zeile anlegen | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:717 | red | red |  |
  | M6 Naht wirft | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:36 | red | red |  |
  | M7 AK3 Typ Ueben | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:488 | red | red |  |
  | M8 AK5 Historie | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:915 | red | red |  |
  | M9 AK3 Typ Test-Next | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:235 | red | red |  |
  | M10 AK3 Typ Test-Abgabe | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:380 | red | red |  |
  | M22 Stufe der Position | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:47 | red | red |  |
  | M11 AK6 Vorstufe fehlt | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:163 | red | red |  |
  | M12 AK6 ohne Weg | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:161 | red | red |  |
  | M13 AK6 Kadenz | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:198 | red | red |  |
  | M14 AK6 Kadenz nach PATCH | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:408 | red | red |  |
  | M15 AK6 PATCH setzt | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:484 | red | red |  |
  | M16 AK7 Kind Titel | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:380 | red | red |  |
  | M17 AK7 Kind Buchseite | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:387 | red | red |  |
  | M18 AK7 Bericht Stufe | backend/Pugling.Api/Services/Supervisor/PositionReportService.cs:67 | red | red |  |
  | M19 AK7 Stufe n | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:74 | red | red |  |
  | M20 AK8 Fuellung | backend/Pugling.Api/Data/Migrations/20261123125626_BuildLevelPlayed.cs:29 | red | red |  |
  | M21 Loeschschutz | backend/Pugling.Api/Services/Shared/ExerciseUsageQueries.cs:66 | red | red |  |
  | M23 offene Runde | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:419 | red | red |  |
  | M24 Ausfuehrungsrecht | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:265 | red | red |  |
  | M25 Aenderung statt gesendet | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:406 | red | red |  |
  | M26 Nacharbeit Anlegen | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:267 | red | red |  |
  | M27 Nacharbeit PATCH | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:417 | red | red |  |
  | M28 Verwendungsanzeige | backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs:290 | red | red |  |

  Grundlinie nach dem Zurückspielen grün. Runde 1 (14:40, vor dem Review) hatte 18/21 rot; M2 kompilierte nicht
  (umformuliert), M3 äquivalent, M10 grün (Lücke → Test ergänzt).
  Review (`pugling-reviewer`): kein 🔴, sieben 🟡, fünf 🟢.
  - 🟡 offene Runde/offener Test beim Stufenwechsel → 409 `build_level_in_play` (Test `Stufe_Wechselt_Nicht_Unter_Einer_Offenen_Runde`, M23) – **gezählt**.
  - 🟡 Ausführungsrecht nur am Ziel geprüft → auch an der Vorstufe (`Private_Vorstufe_Braucht_Das_Ausfuehrungsrecht`, M24) – **gezählt**.
  - 🟡 PATCH prüfte die gesendete statt der geänderten Stufe → Änderung (M25) – **gezählt**.
  - 🟡 Nacharbeit am Ziel statt an der gespielten Übung geprüft → beide (`Nacharbeit_Wird_An_Der_Gespielten_Vorstufe_Geprueft`, M26, M27) – **gezählt**.
  - 🟡 Verwendungsanzeige ≠ 409-Zählung → `UsedBy` auch in der Anzeige (M28) – außerhalb des Diffs, ohne Zählung.
  - 🟡 Pflicht-Sperre auch auf der Zielstufe, keine Rückkehr zu `null` → nach B-702 notiert (dort wird die Sperre aufgehoben).
  - 🟡 Trefferquote im Bericht/Antwort zur Rückmeldung mischen Stufen; `CompletedOn` überlebt den Aufstieg → nach B-849 notiert.
  - 🟢 Wahl bei zwei Vorstufen derselben Stufe jetzt explizit (kleinste Id); Kommentar an `AnyBlockingAsync` – behoben,
    ohne Zählung. 🟢 Turm-Spiel und Bonus-Training spielen das Ziel, `NewWords` zählt doppelt, „unerreichbar“-Anzeige auf
    einer Vorstufe ohne getippte Stufe – zurückgestellt (keine Pflicht kann dort entstehen), ohne Zählung.
  Ein roter Wächter vor dem Abschluss: `ProblemTextGuardTests` – der neue 409-Code hatte keinen deutschen Text im Vater-Web
  (`frontend/src/lib/api.ts`), behoben. **Zähler:** wörtlich nach Freigabe 10 (Reviewer-🟡 mit Code-Änderung) 4/5; mit dem
  eigenen Fund (Positions-Stufe) und dem roten Wächter 6 – nach der strengeren Lesart ist das die Endlosschleifen-Grenze,
  darum endet der Lauf nach diesem Sprint (Protokoll `pm-sitzung-2026-11-23-d.md`).
  Suite nach der letzten Backend-Änderung: 2148/2149 (Release), die eine rote war der Wächter oben; danach
  `ProblemTextGuardTests` 2/2, Vitest `src/lib` 196/196.
- **2026-11-23** — abgenommen (`828cb8e5`, Sprint 121). Rollengang maschinell: Sohn und Vater über
  `AufbauStufePlayTests` (15 Fälle durch die HTTP-Pipeline, Kind-Login und Vater-Login wie im echten Betrieb) – eine
  Oberfläche für die Start-Stufe gibt es erst mit B-841, die API ist bis dahin der einzige Weg. Regressionszeugen E2E gegen
  frisch gestartete Server (5211/5184): `full-flow`, `aufbau-weg-editor`, `lernkarte-leichter`, `nacharbeit` – 4 passed.
  **Menschlicher Check offen:** vor dem nächsten Heimnetz-Start läuft die Migration `BuildLevelPlayed` gegen die echte DB
  (an einer Kopie geprobt, siehe oben).
- **2026-11-23** — Nachschau (Nachtlauf E, beim Bau von B-849 über dieselbe Naht): Prüfpunkte (a) jeder Aufrufer von
  `PlayedExercise`/`PlayedExerciseId` per grep gegen „lädt er `Exercise.Prerequisites`?“ – alle Positions-Loader tun es
  (Compiler-Liste von 35 Aufrufern beim Umbau auf `PlayedExercise(pos, day)`, keiner wirft im Lauf der 2170 Tests);
  (b) die Sperre `InPlayTodayAsync` gegen offene Versuche und Runden **früherer** Tage – Fund: beide bleiben beantwortbar
  und werten nach einem Stufenwechsel gegen die neue Übung → [B-880](B-880-ueberholter-testversuch-nach-stufenwechsel.md)
  (Test, im Sprint 122 behoben) und [B-881](B-881-ueberholte-uebungsrunde-nach-stufenwechsel.md) (Runde, offen).
- **2026-11-23** — Nach `/code-review` des ungepushten Stands, vom Nutzer beauftragt: „bewusst unverändert“ gilt für
  `MetricsService.NewWords` nicht mehr. Eine gestufte Position zählt neue Wörter nur noch auf der Übung, die sie zuerst
  eingeführt hat; sonst zahlte eine Mission „neue Wörter“ die Wörter des Wegs je Stufe einmal. Inhalte, die erst eine
  spätere Stufe bringt, zählen nicht (die vorsichtige Seite). Test
  `AufbauStufeAufstiegTests.Neue_Woerter_Zaehlen_Nur_Auf_Der_Zuerst_Gespielten_Stufe`, rot gegen den alten Stand (2 statt 1).
