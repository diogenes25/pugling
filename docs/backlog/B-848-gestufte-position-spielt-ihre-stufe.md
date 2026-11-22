---
tags: [typ/story, status/geschaetzt, bereich/api, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Stufe spielen, wirksame Übung, Lernstand je Übung]
status: geschaetzt
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
