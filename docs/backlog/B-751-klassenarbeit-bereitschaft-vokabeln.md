---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/training, bereich/auswertung, rolle/student]
aliases: [Bereitschaft wackelig sicher, Klausur-Bereitschaft]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-248
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-751 · Das Kind sieht, wie sicher es im Vokabelstoff seiner Klassenarbeit schon ist

Teil (b) von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md), geteilt beim Schätzen am 2026-11-20. Die Bereitschaftsanzeige über die
Vokabel-Items des Bündels. Das Backend ist unabhängig von [B-750](B-750-klassenarbeit-countdown-beim-kind.md); die Anzeige sitzt in
derselben Karte und kommt darum nach ihr. Die Auflösung des Bündels wird hier als **geteilte Funktion** aus dem Controller geholt, damit
(c) [B-752](B-752-vorbereitungs-pflicht-im-fenster.md) sie mitbenutzen kann.

## User Story

Als *Schüler* möchte ich wissen, wie sicher ich im Vokabelstoff meiner Klassenarbeit schon bin, damit ich weiß, wo ich noch üben muss.

## Ist-Stand am Code

Übernommen aus B-248 (gemessen 2026-11-19), am 2026-11-20 gegen den Code gelesen.

- Das Bündel wird nur im Controller aufgelöst: `LoadRelevantExercisesAsync` (`backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:316-327`,
  **privat**) – direkte Übungen plus alle mit einem verlinkten Tag. Ein zweiter Leser: nicht gefunden; `ExerciseUsageQueries` zählt nur
  `KlassenarbeitExercise` (`Services/Shared/ExerciseUsageQueries.cs:71-94`).
- Ein Beherrschungsmaß gibt es nur für Vokabel-Items: `ItemProgress` je (Kind, `ExerciseItem`) mit `MasteryPercent`, Schwelle
  `WeakBelowPercent = 50` (`Models/ItemProgressEntities.cs:18-48`). Für andere Übungstypen: nicht gefunden.
- Vokabel-Items kommen aus der Item-Tabelle (`StoreResolution.ItemTable`, `Services/Shared/ExerciseContentResolver.cs:28-40`); eine Zeile
  `ItemProgress` entsteht erst mit der ersten Antwort.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt; übernommen aus B-248.*

1. **Bereitschaft nur für Vokabel-Bündel, aus dem vorhandenen Beherrschungsmaß** (B-248 Entscheidung 9). Anzeige: Anteil der Vokabel-Items des
   Bündels, die nicht schwach sind (Schwelle `WeakBelowPercent` = 50); „wackelig“ und „sicher“ benennen diesen Anteil mit der vorhandenen
   Schwelle, es gibt keine neue. Für andere Typen steht „kein Maß“ (Feld leer, nicht geschätzt). Begründung: ein Maß für Nicht-Vokabel gibt es nicht
   (`ItemProgress` ist vokabelgebunden). Kosten: Grammatik-Bündel haben keine Anzeige. Verworfen: ein Maß für Nicht-Vokabel.
2. **Die Auflösung des Bündels wird eine geteilte Funktion** (B-248 AK 3). Direkte und Tag-Übungen ohne Dubletten, aus dem Controller in einen
   Dienst unter `Services/Shared`; Controller und neue Leser rufen ihn. Begründung: ein zweiter Leser darf die Regel nicht abschreiben.
   Kosten: ein Umbau am `practice`- und `repeat`-Pfad, der grün bleiben muss.
3. **Teil (b) steht neben (a), nicht in ihm** (B-248 Entscheidung 1). Der Endpunkt ist ein eigener Lesepfad je Klassenarbeit; die Karte aus B-750
   zeigt ihn an, wenn vorhanden. Kosten: ein zweiter Aufruf der Sohn-App.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht (aus B-248 AK 3 und 4).

1. Das Bündel des Kindes umfasst direkte Übungen **und** Tag-Übungen ohne Dubletten, über **eine** geteilte Funktion, die auch der
   Vater-Pfad `practice` benutzt. *Rot, wenn* die Auflösung nur `KlassenarbeitExercise` liest oder im Controller abgeschrieben steht.
2. Die Bereitschaft nennt für ein Vokabel-Bündel den Anteil der nicht schwachen Items mit der Schwelle 50 und für ein Bündel ohne Vokabeln
   „kein Maß“, nie eine Schätzung. *Rot, wenn* ein Nicht-Vokabel-Bündel eine Zahl liefert oder die Schwelle von `ItemProgress.WeakBelowPercent` abweicht.
3. Dasselbe Ergebnis erhält nur das Kind der Arbeit; ein anderes Kind und ein Vater ohne Betreuung bekommen `403`/`404`. *Rot, wenn* die Route
   ohne Betreuungs-Prüfung hängt.
4. `practice` und `repeat` des Vater-Controllers liefern nach dem Umbau dieselben Übungen wie vorher. *Rot, wenn* der Umbau das Bündel ändert
   (Gegenprobe: im Ausgangszustand schon so, fällt nur durch eine falsche Verlagerung).

## Schätzung

**Größe: M** – wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein Dienst wird aus dem Controller gelöst, ein Lesepfad mit
einer Zählung über `ExerciseItem` und `ItemProgress` entsteht, im Frontend ein Zusatz an der Karte.

- **`migration: nein`** – nachgesehen: nur lesen.
- **`vertragsbruch: nein`** – ein neues DTO unter `Contracts/Student`; `PracticeResponse` und `ExerciseBrief` bleiben.
- **`wo: beides`** – Backend zuerst; die Frontend-Anzeige hängt an der Karte von B-750.

### Risiken

- **Nicht gesehene Items:** ein Item ohne `ItemProgress`-Zeile ist nicht „sicher“. Entscheidung 1 sagt „Anteil der nicht schwachen Items“ und
  meint dem Wortlaut nach nur Items mit Zeile; der Nenner **muss** alle Items des Bündels sein, sonst stünde bei zehn Wörtern, von denen zwei geübt
  sind, „100 % sicher“. Vorschlag für das Bauen (Lesart, keine neue Entscheidung): Nenner = alle Items, Zähler = Items mit `MasteryPercent >= 50`;
  ein Testfall hält es fest.
- **Etiketten:** „wackelig“ und „sicher“ hat keine zweite Schwelle (Entscheidung 1). Wie die beiden Wörter an der einen Zahl hängen, ist nicht
  festgelegt; das Minimum ist die Zahl „x von y Wörtern sicher“ ohne Etikett.
- **Umbau am Vater-Pfad:** `practice` und `repeat` (`KlassenarbeitenController.cs:276`) laufen über denselben Dienst; AK 4 deckt es ab.
- **Mehrere Vokabel-Übungen mit demselben Wort:** das Maß hängt am Item der **Übung**, nicht am Wort; dieselbe Vokabel in zwei Übungen zählt zweimal.
  Das Wort-Maß (`VocabularyId`) wäre die Alternative und ist hier nicht entschieden.
- **Fremde Frontend-Arbeit** in `sohn/*` (siehe B-750).

### Angriffsplan

1. Dienst `KlassenarbeitBundleResolver` (Name vorläufig) unter `Services/Shared`: Übungen eines Bündels, ohne Dubletten; Controller umstellen.
2. Lesepfad `GET …/children/{childId}/class-tests/{id}/readiness` unter `Controllers/Student`: Bündel auflösen, Vokabel-Übungen filtern
   (`StoreResolution.ItemTable`), Items zählen, `ItemProgress` des Kindes verbinden.
3. DTO unter `Contracts/Student`; Frontend: Typen, `api`, Anzeige in der Karte von B-750.
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `KlassenarbeitBereitschaftTests` in `backend/Pugling.Api.Tests`: AK 1 (direkt, Tag, beides, Dublette), AK 2 (Anteil, „kein Maß“,
  Item ohne Zeile), AK 3.
- `KlassenarbeitenTests` bleibt unverändert grün und trägt AK 4 (`Uebung_Zuweisen_Note_Nachtragen_TauchtImVorbereitenUndWiederholenAuf`,
  `Repeat_LiefertSchlechtBenoteteSeedArbeit`).
- `frontend/src/sohn/SohnHome.test.tsx`: Anzeige mit Zahl und mit „kein Maß“; E2E im Spec von B-750 (`klassenarbeit-countdown.spec.ts`) um
  die Bereitschaft erweitern.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`; Rollengang als Kind.

## Verlauf

- **2026-11-20** — angelegt beim Teilen von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) und geschätzt (Vorbereitungslauf,
  Sonnet): M, beides, keine Migration, kein Vertragsbruch. Der Nenner der Bereitschaft (alle Items des Bündels) steht als Risiko.
