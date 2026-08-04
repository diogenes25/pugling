---
tags: [typ/story, status/geschaetzt, bereich/backend, rolle/supervisor]
aliases: [Stufe ohne Validierung, Stage 99, Unbekannte Stufe deckt die Lösung auf]
status: geschaetzt
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-76 (Review pugling-reviewer, Befund 7)
---

# B-79 · Die Stufe einer Position wird gegen nichts geprüft

`POST`/`PATCH` auf `…/study-plans/{planId}/positions` nehmen `Stage` als nackten `int` entgegen und
schreiben ihn ungeprüft an die Position
([PlanPositionsController.cs:113](../../backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)
und `:155`). Jeder Übungstyp führt aber eine Liste seiner gültigen Stufen (`IExerciseType.StageOptions`),
und gegen die wird nicht abgeglichen. `Stage = 99` ist damit setzbar.

Das bleibt nicht folgenlos, weil die Stufe entscheidet, **ob die Karte die Lösung zeigt**: `IsTypedStage`
fällt für einen unbekannten Wert auf `false` zurück, und `CardFacets` liefert dann `Reveal` mit der
Antwort. Eine unsinnige Stufe macht die Übung also nicht kaputt — sie deckt sie auf.

Der Weg dorthin führt über den Supervisor, nicht über das Kind (die Stufe kommt beim Spielen aus dem
Fahrplan). Es ist also kein Selbstbetrugs-Pfad des Kindes, sondern ein Versehen, das lautlos wirkt: Wer
sich vertippt, bekommt keine Fehlermeldung, sondern eine Position, die die Lösungen verschenkt.

## Warum das jetzt auffällt

Vorbestehend, aber durch [B-76](B-76-lueckentext-karte-ohne-luecke.md) sichtbarer geworden. Seit dessen
Entscheidung E6 sind **alle drei** `ClozeStage`-Werte getippt — ein unbekannter Wert ist damit der einzig
verbliebene Weg zu einer aufgedeckten Lückentext-Karte. Vorher traf derselbe Fehler noch eine reguläre
Stufe und fiel darum weniger auf.

## User Story

Als Supervisor möchte ich, dass eine ungültige Stufe beim Anlegen oder Ändern einer Position abgelehnt
wird, damit ein Vertipper nicht lautlos eine Position erzeugt, die dem Kind die Lösung zeigt statt sie
abzufragen.

## Ist-Stand am Code

- `PlanPositionsController.Create` schreibt `dto.Stage` ungeprüft auf `PlanPosition.Stage`
  (`PlanPositionsController.cs:113`) und `dto.StageSchedule` ungeprüft auf `PlanPosition.StageSchedule`
  (`PlanPositionsController.cs:124`, jeder `StageStep.Stage` einzeln); `Update` tut dasselbe für beide
  Felder (`PlanPositionsController.cs:155` bzw. `:165`). Beide Felder sind nackte `int`/`List<StageStep>`
  im Vertrag (`CreatePositionDto` in `Pugling.Contracts/Supervisor/StudyPlanDtos.cs:100`,
  `UpdatePositionDto` in `:136`; `StageStep` selbst in `Pugling.Contracts/Common/StudyPlanBaseTypes.cs:18`).
- Jeder Übungstyp trägt seine gültigen Stufen über `IExerciseType.StageOptions` (`IExerciseType.cs:90`),
  Standard eine **leere** Liste (`ExerciseTypeBase.cs:50`). Nur zwei eingebaute Typen überschreiben sie mit
  echten Werten: `VocabularyExerciseType.cs:104-111` (fünf `TestStage`-Werte) und
  `BuiltInExerciseTypes.cs:173-178` (drei `ClozeStage`-Werte — `WordBank = 1` bewusst ausgelassen, siehe
  `ClozeEntities.cs:9-11`). Gegen keine dieser Listen wird beim Schreiben abgeglichen.
- Die Stufe entscheidet serverseitig, ob eine Karte typed (geprüft) oder self-assess (Lösung sichtbar)
  ist: `PositionPlayService.CardFacets` liefert `Reveal` als `typed ? null : item.Answer`
  (`PositionPlayService.cs:150`). `typed` kommt aus `IExerciseType.IsTypedStage(int)`; die beiden Typen mit
  echten Stufen werten sie über ein reines Enum-Pattern-Match aus (`StageMechanics.cs:17-18` für Vokabeln,
  `:26-27` für Cloze) — ein Wert, der zu keinem Enum-Glied passt (z. B. `99`), fällt beim
  `is X or Y or Z`-Pattern durch und liefert `false`.
- Der dritte eingebaute Typ mit sichtbarem `Stage`-Feld, Matching, überschreibt `IsTypedStage` **gar
  nicht** (Standard `ExerciseTypeBase.cs:38` liefert immer `true`) — dort ist die Stufe laut eigenem
  Kommentar „gespeichert und bei der Ausspielung ignoriert" (`StudyPlanEntities.cs:13-18`). Die Lücke
  betrifft also konkret Vokabel- und Cloze-Positionen, nicht jeden Übungstyp.
- Derselbe rohe `int? stage` fließt auch über den Vorschau-Endpunkt des Creators
  (`ExercisePreviewController.cs:50` `[FromQuery] int? stage`, `:68` `PreviewCheckDto.Stage`) in
  `ExercisePreviewService.BuildAsync`/`CheckAsync` (`ExercisePreviewService.cs:35`, `:53`) — dort ist der
  Aufrufer aber der Ersteller/Adoptierende der Übung selbst (`[Authorize(Roles = Roles.Creator)]`, keine
  Ownership-Filterung laut Kommentar `ExercisePreviewController.cs:16-19`), der die Lösung ohnehin sehen
  darf; eine unsinnige Stufe zeigt dort dasselbe, was eine reguläre Selbsteinschätzungs-Stufe absichtlich
  zeigt. Kein zusätzliches Sicherheitsproblem — siehe Entscheidung 4.
- Der Weg zum Kind führt ausschließlich über den Supervisor (`[Authorize(Roles = Roles.Supervisor)]`,
  `PlanPositionsController.cs:23`); das Kind liest die Stufe nur über `PositionPlayService.StageForDay`
  (`PositionPlayService.cs:56-64`), das exakt `pos.Stage`/`StageSchedule` weiterreicht, ohne selbst zu
  wählen.
- Derselbe Controller validiert bereits ein anderes Feld auf dieselbe Weise: `ThresholdProblem` prüft
  `GoalThreshold` gegen 1–100 und liefert `this.ProblemWithCode(ApiErrors.ValidationError, …)`
  (`PlanPositionsController.cs:68-71`, aufgerufen `:88` und `:148`) — ein direktes Vorbild für die neue
  Prüfung.

## Die echte Lücke

Nicht „jeder Übungstyp" ist betroffen, sondern genau die, deren `IsTypedStage` einen `int` in ein Enum
castet und per Pattern-Match auswertet: heute Vokabel und Cloze. Für sie öffnet eine Stufe außerhalb der
eigenen `StageOptions` unbeabsichtigt die Lösung, weil „nicht getippt" mit „self-assess" gleichgesetzt
wird. Für Typen mit leerer `StageOptions`-Liste (z. B. Matching, Essay, Reading) hat der Wert dagegen
**keine** Wirkung — eine Prüfung „Stage muss in `StageOptions` stehen" darf darum **nicht** greifen, wenn
die Liste leer ist, sonst würde sie plötzlich jede Stufe für diese Typen ablehnen (sie hätte dort keine
echte Bedeutung).

Ein kleinerer, unabhängiger Befund aus derselben Runde steht am Ende der Datei und bleibt unangetastet
(kein Teil dieser Story).

## Offene Punkte

- ~~Ob `StageOptions` wirklich für alle Typen vollständig ist — sonst weist die Validierung gültige Stufen
  ab.~~ → siehe Entscheidung 2 (leere Liste = keine Prüfung).
- ~~Ob es Bestandsdaten mit einer Stufe außerhalb der Liste gibt.~~ → siehe Entscheidung 3.
- ~~Wo die Prüfung sitzt (Controller oder geteilte Hilfsfunktion) und ob sie einen `ApiErrors`-Code
  braucht.~~ → siehe Entscheidung 1.
- ~~Ob `IsTypedStage` zusätzlich fail-safe werden sollte (unbekannte Stufe → `true` statt `false`).~~ →
  siehe Entscheidung 5.
- ~~Ob der Creator-Vorschau-Endpunkt (`ExercisePreviewController`) dieselbe Prüfung braucht.~~ → siehe
  Entscheidung 4.

## Entscheidungen

1. **Ort und Fehlercode**: die Prüfung sitzt als private Hilfsfunktion `StageProblem` im
   `PlanPositionsController`, analog zu `ThresholdProblem` (`PlanPositionsController.cs:68-71`), aufgerufen
   in `Create` und `Update` vor dem Schreiben; Fehlercode ist der bestehende `ApiErrors.ValidationError`.
   Begründung: ein Vorbild lebt schon im selben Controller, kein neuer Vertragswert nötig — dieselbe
   Fehlerklasse „Wert außerhalb der zulässigen Menge" wie bei der Schwelle. Kosten: die Prüfung lebt im
   Supervisor-Controller, nicht als geteilte Hilfsfunktion neben `StageForDay` — vertretbar, weil aktuell
   nur ein Schreibpfad existiert (der Vorschau-Endpunkt ist bewusst ausgenommen, siehe Entscheidung 4).
2. **Leere `StageOptions` bedeutet „jede Stufe erlaubt"**, keine Ablehnung. Begründung: Für Typen ohne
   eigene Stufenliste (Matching, Essay, Reading, …) hat der Wert keine Bedeutung (siehe „Die echte
   Lücke"); eine pauschale Prüfung würde dort gültige Werte ablehnen. Kosten: keine — das ist dieselbe
   Regel, die `IsTypedStage`/`CardFacets` implizit schon fahren, nur jetzt auch am Schreibpfad.
3. **Keine Migration/kein Backfill für Bestandsdaten.** `Stage` bleibt `int?`, eine bestehende Position mit
   einer Stufe außerhalb der Liste bleibt lesbar — nur `Create`/`Update` werden schärfer. Begründung:
   Bestandsschutz statt rückwirkender Korrektur, im Sinne von „keine Migration ohne Not"
   ([CLAUDE.md](../../CLAUDE.md), EF-Migrationen). Ob heute bereits eine Bestandsposition betroffen ist,
   wird beim Bauen einmalig per Ad-hoc-Abfrage gegen `pugling.db` geprüft (kein Automatismus, kein
   Backfill-Skript) — ein Treffer würde separat gemeldet, nicht in dieser Story mitgelöst. Kosten: eine
   bereits kaputte Bestandsposition bleibt bis zur nächsten manuellen Änderung kaputt.
4. **Der Creator-Vorschau-Endpunkt (`ExercisePreviewController`) bekommt keine Prüfung.** Begründung:
   Aufrufer ist der Ersteller/Adoptierende der Übung selbst, der die Lösung ohnehin einsehen darf (der
   Katalog ist global lesbar); eine unsinnige Stufe zeigt dort nur, was eine reguläre Self-Assess-Stufe
   auch zeigt — kein Übertritt einer Vertrauensgrenze. Kosten: eine inkonsistente Fehlermeldung bei
   falscher Eingabe dort (der Adult bekommt eine seltsame Karte statt eines `400`) — als kosmetischer Rest
   notiert, keine eigene Story wert.
5. **`IsTypedStage` bleibt fail-open**, kein zweites Netz in `StageMechanics`. Begründung: Mit der
   Eingangsprüfung (Entscheidung 1) kann eine ungültige Stufe eine Position gar nicht mehr erreichen — ein
   zweites Netz wäre Verteidigung gegen einen Weg, der nicht mehr existiert, und bräuchte eigene
   Testabdeckung für einen rein hypothetischen Pfad. Kosten: kein Fail-safe mehr, falls ein künftiger
   dritter Schreibpfad (z. B. ein Seed/Script) die Stufe direkt in der DB setzt — akzeptiert, weil ein
   Seed ohnehin von Hand gegen `StageOptions` gepflegt werden müsste.

## Akzeptanzkriterien

1. `POST …/study-plans/{planId}/positions` mit `stage` außerhalb der `StageOptions` des referenzierten
   Übungstyps (z. B. eine Vokabel-Position mit `stage: 99`) liefert `400 validation_error`, keine Position
   wird angelegt.
2. Dieselbe Prüfung gilt für jeden einzelnen `Stage`-Wert in `stageSchedule`.
3. `PATCH …/positions/{positionId}` mit ungültigem `stage` bzw. `stageSchedule` liefert ebenso
   `400 validation_error`, die bestehende Position bleibt unverändert.
4. Ein Übungstyp mit leerer `StageOptions`-Liste (z. B. Matching) akzeptiert weiterhin jeden `int`-Wert für
   `stage` — die Prüfung wird nicht pauschal für alle Typen scharf.
5. Ein gültiger Stufenwert (einer aus `StageOptions`) wird weiterhin wie bisher angenommen
   (Regressionsfreiheit).

## Schätzung

**Größe: S** — eine neue private Prüf-Hilfsfunktion analog zu `ThresholdProblem`, zwei Aufrufstellen
(Create/Update), kein neues DTO-Feld, kein neuer `ApiErrors`-Code, kein Schema. Vergleichbar im Umfang mit
dem S-Anker B-01 (Feld aus dem Test-Pfad ziehen), deutlich kleiner als die M-Anker B-03/B-10.

- **wo**: backend
- **migration**: nein — keine Schemaänderung; `Stage`/`StageSchedule` bleiben wie sie sind, nur eine
  schärfere Eingangsprüfung davor.
- **vertragsbruch**: nein — keine DTO-Signatur ändert sich; die Verschärfung ist reines Server-Verhalten
  (ein Request, der vorher fälschlich `201`/`200` bekam, bekommt jetzt `400` — kein neues Feld, kein neuer
  Typ im Vertrag).
- **Risiken**: `StageOptions` könnte für einen künftigen Typ unvollständig gepflegt sein und dadurch
  gültige Werte abweisen — abgefedert durch Akzeptanzkriterium 5 und dadurch, dass die Prüfung nur bei
  nicht-leerer Liste greift (Entscheidung 2).
- **Angriffsplan**: Backend zuerst und einzig — Hilfsfunktion in `PlanPositionsController` ergänzen, an
  beiden Schreibstellen (`Create`, `Update`) verdrahten, Tests ergänzen. Kein Frontend-Anteil in dieser
  Story.
- **Testweg**: `PlanPositionCrudTests.cs` — neuer Test analog zu
  `Position_SchwelleAusserhalbProzent_WirdAbgewiesen` (`PlanPositionCrudTests.cs:182`): eine Vokabel-Position
  mit `stage: 99` wird bei `POST` und `PATCH` abgewiesen, eine Matching-Position mit demselben Wert wird
  weiterhin angenommen (deckt Akzeptanzkriterium 4 ab).

## Ein kleinerer Befund aus derselben Runde

`ClozeExerciseType.Choices` deserialisiert die ganze `ClozeConfig` **je Karte**
([BuiltInExerciseTypes.cs:143](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — bei 20
Lücken 20 Parses derselben Zeichenkette für einen `GET …/cards`. Unkritisch und bewusst nicht mit B-76
behoben (der Typ ist zustandslos, ein Cache wäre die erste Ausnahme davon); hier notiert, damit es nicht
verlorengeht.

## Verlauf

- **2026-08-02** — angelegt aus dem `pugling-reviewer`-Befund zum Commit `1125ee6` (B-76), Punkt 7.
  `prio: P2`: Es wirkt heute an niemandem — es braucht erst einen Vertipper des Supervisors —, aber die
  Folge ist eine stillschweigend aufgedeckte Übung. Nicht vom Nutzer bestätigt.
- **2026-08-03** — ausformuliert: Ist-Stand mit `Datei:Zeile` belegt (`PlanPositionsController.cs:113/124/155/165`,
  `StageMechanics.cs:17-18/26-27`, `PositionPlayService.cs:150`); die Lücke präzisiert auf Vokabel-/Cloze-Typen
  (Matching bewusst ausgenommen, `StudyPlanEntities.cs:13-18`) und den Creator-Vorschau-Endpunkt als
  ungefährdet erkannt (`ExercisePreviewController.cs:16-19`).
- **2026-08-03** — gegrillt: alle Offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, Angriffsplan eine Prüf-Hilfsfunktion analog `ThresholdProblem` an
  zwei Schreibstellen, Testweg `PlanPositionCrudTests.cs` (autonom getroffen, Nutzerauftrag 2026-08-04).
