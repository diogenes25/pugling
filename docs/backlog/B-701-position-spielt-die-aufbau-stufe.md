---
tags: [typ/story, status/geschaetzt, bereich/api, bereich/lehrplan, rolle/student, rolle/supervisor]
aliases: [Gestufte Position, Aufstieg nach Test, Position auf Vorstufe]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-536
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-701 · Eine Position spielt ihre aktuelle Aufbau-Stufe und steigt nach dem bestandenen Test auf

Teil 2 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md). Baut auf
[B-700](B-700-aufbau-stufe-am-katalog.md) auf: hat die Ziel-Übung einer Position Vorstufen, spielt die Position die
Übung der **aktuellen Stufe** statt immer die Ziel-Übung. Aufstieg nach bestandenem Test. **Nicht** Teil dieser
Story: Abstieg, Malus und was das Pflichtziel misst – das steht in
[B-702](B-702-abstieg-und-pflichtziel-der-aufbau-stufe.md) und braucht die Entscheidung des Nutzers.

## User Story

Als *Sohn* möchte ich zuerst die leichtere Übung zu einem Thema spielen und erst nach einem bestandenen Test die
nächste Stufe bekommen, damit ich auf dem Weg zum Stoff Erfolge habe. Als *Vater* möchte ich das je Position
einschalten, indem ich nur das Ziel wähle.

## Ist-Stand am Code

- Eine Position hängt an genau einer Übung (`PlanPosition.ExerciseId`,
  `backend/Pugling.Api/Models/PlanPositionEntities.cs:24`); der Inhalt wird an **einer** Stelle geholt:
  `PositionPlayService` ruft `content.ItemsOfAsync(pos.Exercise, childId)`
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:59`). Das ist die Naht für die Stufe.
- Der Lernstand der Position hängt an `PlanPositionId` **und `ItemIndex`** (`PositionItemProgress`,
  `PlanPositionEntities.cs:281-295`). Ein Index meint in Stufe 1 eine andere Aufgabe als in Stufe 3 – ein Stufenwechsel
  darf den Stand der alten Stufe also nicht weiterverwenden.
- Der Lernstand je Vokabel ist plan-übergreifend und hängt an `(ChildId, ExerciseId, Item)`
  (`itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, …)`,
  `backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:446`).
- Der Test wird bestanden über `attempt.Passed = ScorePercent >= passPercent`
  (`PositionTestsController.cs:460`), `passPercent` ist `GoalThreshold` oder der Standard. Ein Versuch trägt
  `PlanPositionId`, aber keine Übung (`TestAttempt`, `backend/Pugling.Api/Models/StudyPlanEntities.cs:170-195`).
- Die Pflichtziel-Messung `IsGoalMetAsync` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:195`) liest
  bestandene Versuche der Position; sie kennt keine Stufe.
- Nicht gefunden: irgendein Feld, das „aktuelle Stufe" einer Position trägt.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.* Geerbt aus B-536: Entscheidung 1
(eine Position je Thema, die Stufen führt) und der Aufstieg aus Entscheidung 3. Der Abstieg und Entscheidung 4
(Pflichtziel) sind **nicht** übernommen, sie wandern nach B-702.

1. **Die aktuelle Stufe ist ein Feld der Position: `PlanPosition.CurrentBuildLevel` (nullable).** `null` heißt „keine
   Stufung, wie bisher". Hat die Ziel-Übung keine Vorstufen, bleibt es `null`. Begründung: kein Bestand ändert sich,
   die Position kommt ohne neue Tabelle aus. Verworfene Alternative: die Stufe aus den Versuchen herleiten – jede
   Lesung müsste die Versuchshistorie auswerten, und ein Vater könnte nichts von Hand setzen. Kosten: ein Zustand,
   der außerhalb eines Versuchs geändert wird und darum an einer Stelle serialisiert sein muss. Schließt aus: eine
   Position, deren Stufe sich nur mit Rückwirkung neu berechnen ließe.
2. **Beim Einschalten startet die Position auf der niedrigsten Stufe, die der Vater im Positionsformular als
   „Start" wählt (Standard: niedrigste vorhandene Vorstufe).** Begründung: B-536 verlangt „Aufgaben, die ich gerade
   so schaffe"; ein Start auf der Ziel-Stufe wäre das Bisherige. Kosten: ein Feld mehr im Formular
   ([B-704](B-704-aufbau-weg-in-der-oberflaeche.md)). Schließt aus: einen automatischen Einstieg aus dem
   Lernstand (der gehört zum Lehrer-Agenten, B-680).
3. **Aufstieg: ein bestandener Test der aktuellen Stufe setzt `CurrentBuildLevel` eine Stufe höher, höchstens bis zur
   Ziel-Übung.** Begründung: Aufstieg aus B-536, Entscheidung 3, dort ohne weitere Bedingung. Verworfene Alternative:
   zwei bestandene Tests in Folge – sicherer, aber doppelt so langer Weg, und das Kind soll Erfolge haben. Kosten:
   ein Zufallstreffer reicht für den Aufstieg; der nächste Test fängt ihn auf, der Abstieg ist B-702. Schließt aus:
   einen Aufstieg ohne Test.
4. **Der Stufenwechsel ist atomar mit dem Abschluss des Versuchs und braucht keinen eigenen Endpunkt.** Er läuft in
   dem Pfad, der `attempt.Passed` setzt. Der positionsgebundene Stand (`PositionItemProgress`) der **verlassenen Stufe
   bleibt liegen** und wird nicht auf die neue Stufe übertragen; die neue Stufe beginnt mit leerem
   positionsgebundenem Stand. Der plan-übergreifende Stand je Übung bleibt unberührt (er hängt an der Übung). Kosten:
   Wörter oder Items, die auf Stufe 1 als „gelernt" galten, zählen auf Stufe 2 nicht. Schließt aus: einen Stand, der
   über Stufen hinweg falsch indiziert.
5. **Der Versuch merkt sich die Stufe, auf der er gespielt wurde (`TestAttempt.BuildLevel`, nullable).**
   Begründung: B-702 und die Vater-Auswertung brauchen „auf welcher Stufe wurde das bestanden"; nachträglich lässt sich
   das nicht rekonstruieren. Kosten: eine weitere nullable Spalte. Schließt aus: Auswertungen, die Stufen
   vermischen.
6. **Bis B-702 entschieden ist, misst das Pflichtziel unverändert – ein Test der Vorstufe zählt nicht für das Ziel.**
   Folge: eine gestufte Position darf in dieser Story nur mit `Cadence = None` oder `UntilDone` ausgeliefert werden;
   die Oberfläche (B-704) schaltet die Wahl erst frei, wenn B-702 gebaut ist. Begründung: sonst bliebe eine Woche
   genau so gesperrt, wie es B-535 gelöst hat. Kosten: die Funktion ist bis B-702 für Pflichten gesperrt. Schließt
   aus: eine Auslieferung, die Friedrich im Pflichtfall wieder sperrt.

## Akzeptanzkriterien

1. Eine Position, deren Ziel-Übung eine Vorstufe hat und deren `CurrentBuildLevel` 1 ist, liefert beim Üben und im
   Test die Aufgaben der **Vorstufe**, nicht der Ziel-Übung. Rot macht ihn: ein `ItemsOfAsync`, das weiter
   `pos.Exercise` liest.
2. Eine Position ohne Vorstufen und eine mit `CurrentBuildLevel = null` spielen unverändert die Ziel-Übung. Rot macht
   ihn: jede Verzweigung, die `null` als Stufe 0 behandelt.
3. Ein bestandener Test auf Stufe 1 setzt `CurrentBuildLevel` auf 2; ein nicht bestandener lässt ihn unverändert. Rot
   macht ihn: ein Aufstieg ohne Prüfung von `Passed`.
4. Auf der obersten Stufe (Ziel-Übung) steigt nichts mehr; `CurrentBuildLevel` überschreitet sie nie. Rot macht ihn:
   ein fehlender Deckel.
5. Nach dem Aufstieg ist der positionsgebundene Stand der neuen Stufe leer und der der alten Stufe unverändert. Rot
   macht ihn: ein Aufstieg, der `PositionItemProgress` weiterverwendet (falscher `ItemIndex`).
6. Ein `TestAttempt` einer gestuften Position trägt die gespielte Stufe; einer ungestuften `null`. Rot macht ihn:
   ein Versuch, der die Stufe nicht schreibt.
7. Ein Aufstieg bucht keine zusätzlichen Münzen und keinen Malus (B-702 hat den Rest). Rot macht ihn: ein Aufstieg, der
   in den Punkte-Pfad eingreift.
8. Die Migration nimmt vorhandene Positionen und Versuche mit `null` mit. Rot macht ihn: eine Spalte ohne Nullbarkeit.

## Schätzung

**Größe: L** — wie eine DB-Umbau-Etappe (E6): zwei neue Spalten an Schemata mit Bestandsdaten, eine Änderung an der
zentralen Inhaltsauswahl, die jeden Spielpfad trägt, und ein Zustandsübergang im Testabschluss. Mehr als ein M, weil
Üben, Test, Vorschau und Mengen-Zuordnung (`PositionPlayService`, `PositionPracticeController`,
`PositionTestsController`) dieselbe Naht benutzen.

- **`migration: ja`** — `PlanPositions.CurrentBuildLevel` und `TestAttempts.BuildLevel` sind Schema; die Migration
  hängt an die Kette (nach B-700) und nimmt die echte DB mit.
- **`vertragsbruch: nein`** — nachgesehen: `PositionResponse`
  (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:150`) und `AttemptResponse`
  (`backend/Pugling.Contracts/Student/TestDtos.cs:58`) bekommen höchstens ein trailing optionales Feld; ein
  Request-Feld für die Startstufe ist additiv. Kein bestehendes Feld ändert Name oder Bedeutung.
- **`wo: backend`** — Anzeige in [B-704](B-704-aufbau-weg-in-der-oberflaeche.md).

### Risiken

- **Die Naht trägt jeden Spielpfad:** ein Fehler in der Stufenwahl trifft jeden Typ. AK 2 schützt den Normalfall.
- **`ItemIndex` ist je Übung:** Entscheidung 4 ist die Antwort; vergisst man sie, erscheint ein Stand, der zur falschen
  Aufgabe gehört (stiller Datenfehler, kein Absturz).
- **Rennen beim Abschluss:** zwei parallele Versuchs-Abschlüsse dürfen nicht doppelt aufsteigen; der Aufstieg muss
  idempotent über den Versuch laufen (Muster: Ref-Gate/Einmalbuchung wie bei Zielpunkten, vgl.
  `PositionGoalReward` mit Unique-Index).
- **Pflichtziel:** Entscheidung 6 verschiebt das Problem bewusst nach B-702; wer B-701 ohne B-702 für Pflichtpositionen
  freischaltet, sperrt die Woche wieder.
- **Vorschau:** der Testmodus des Vaters (`ExercisePreviewService`) spielt Katalog-Übungen, keine Positionen – er bleibt
  unberührt.

### Angriffsplan

1. Entity-Felder und Migration (hängt nach B-700).
2. Eine Funktion `EffectiveExercise(pos)` neben `PositionPlayService.ItemsOfAsync`; alle Aufrufer der Naht nutzen sie.
3. Aufstieg im Abschlusspfad des Tests, idempotent; `TestAttempt.BuildLevel` schreiben.
4. Positions-Anlegen/-Ändern: Startstufe als optionales Feld, Validierung (Ziel-Übung hat Vorstufen).
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `AufbauStufePlayTests` in `backend/Pugling.Api.Tests` (Aufbau über `TestApi`, Muster
  `PositionPracticeFlowTests`/`PositionTestFlowTests`): AK 1–4 und 6–7.
- AK 5: ein Fall in derselben Klasse, der `PositionItemProgress` vor und nach dem Aufstieg liest.
- Neue Klasse `AufbauStufePlayMigrationTests` nach dem Muster `ShopStockPerChildMigrationTests`: AK 8.
- `PositionPracticeFlowTests`, `PositionTestFlowTests` und `PflichtMalusTests` laufen unverändert grün (AK 2, 7).
- Rollengang vor der Abnahme als Sohn an einer gestuften Position; E2E erst mit B-704 (`aufbau-stufen.spec.ts`).
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test`.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt und geschätzt im Vorbereitungslauf (Sonnet): L, backend, Migration ja,
  Vertrag additiv. Abstieg und Pflichtziel bewusst nicht enthalten (B-702). Entscheidungen stellvertretend,
  unbestätigt.
- **2026-11-19** — Entscheidungen bestätigt. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
- **2026-11-22** — Nachprüfung vor dem Bau (Nachtlauf 2026-11-22-c, Opus): **nicht gebaut, angehalten nach Freigabe 8.**
  Am Code gemessen: `pos.Exercise` wird in `backend/Pugling.Api` an 61 Stellen gelesen (Typ, Config, Vorgabe-Stufe,
  Prüfmodus, Titel, `BookPages`), `PositionItemProgress` an 50 Stellen in 16 Dateien. Entscheidung 4 („der Stand der
  verlassenen Stufe bleibt liegen, die neue beginnt leer") lässt sich mit dem Schlüssel `(PlanPositionId, ItemIndex)`
  nicht halten. Sie braucht eine dritte Schlüsselspalte (Stufe) an `PositionItemProgress` oder ein Löschen beim
  Aufstieg. Das ist eine Schema-Entscheidung jenseits der zwei Spalten dieser Schätzung. Offen und vor dem Bau zu
  entscheiden: (a) Stufe als Schlüsselteil des Positions-Lernstands oder Verwerfen beim Aufstieg; (b) was mit einer
  laufenden Übungs-Sitzung (eingefrorene Reihenfolge), dem Lernkarten-Stand (`ImprintedAt`) und der Tagesdosis beim
  Aufstieg geschieht; (c) welche Übung der Vater in Übersicht und Bericht sieht (Ziel oder aktuelle Stufe, Titel,
  `BookPages`); (d) Teilen in zwei Stories, etwa „Stufe spielen (wirksame Übung an allen Spielpfaden,
  stufengebundener Stand)" und „Aufstieg nach bestandenem Test samt `TestAttempt.BuildLevel` und Ziel-Filter". Die
  Größe ist mit diesen Antworten eher XL als L.
