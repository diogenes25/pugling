---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/training, bereich/api, rolle/student]
aliases: [Klausur-Countdown, Klassenarbeit in der Sohn-App]
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

# B-750 · Das Kind sieht in der Sohn-App, wann seine nächste Klassenarbeit ist

Teil (a) von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md), geteilt beim Schätzen am 2026-11-20. Der Teil ohne Münzwirkung: ein
Lesepfad für das Kind und eine Anzeige „Englisch-Arbeit in 5 Tagen“. Unabhängig von (b) [B-751](B-751-klassenarbeit-bereitschaft-vokabeln.md)
und (c) [B-752](B-752-vorbereitungs-pflicht-im-fenster.md).

## User Story

Als *Schüler* möchte ich sehen, wann meine nächste Klassenarbeit ist, damit ich weiß, wofür ich gerade lerne.

## Ist-Stand am Code

Übernommen aus B-248 (gemessen 2026-11-19), am 2026-11-20 gegen den Code gelesen.

- Die Klassenarbeit ist reine Vater-Sache: Entität mit `ScheduledDate`, `Status`, Fach optional
  (`backend/Pugling.Api/Models/KlassenarbeitEntities.cs:67-88`), einziger Controller
  `api/v1/supervisor/class-tests` (`Controllers/Supervisor/KlassenarbeitenController.cs:18`), Betreuung über `AuthAccess.OwnsChildAsync`.
- Ein Student-Endpunkt oder -DTO zur Klassenarbeit: **nicht gefunden** (`Controllers/Student`, `Contracts/Student`).
- Die Sohn-App kennt sie nicht: in `frontend/src/sohn/` kommt „Klassenarbeit“ nur als Kommentar vor (`SohnTest.tsx:43`).
- Das Muster für einen Lesepfad des Kindes ist `ChildTodayController` (Route `ApiRoutes.Student + "/children/{childId:int}/today"`,
  `Controllers/Student/ChildTodayController.cs:14`).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt; übernommen aus B-248.*

1. **Teil (a) ist eine eigene Story ohne Münzwirkung** (B-248 Entscheidung 1). Begründung: unabhängig baubar, berührt weder Pflicht noch Malus.
   Kosten: eine Story mehr. Schließt aus: ein Kind, das die Anzeige erst mit der Pflicht bekommt.
2. **Gezeigt wird nur, was das Kind nicht überrascht und nicht entmutigt: Titel, Fach, Datum, Tage bis zum Termin; keine Note, kein Kommentar.**
   Ergänzt beim Schätzen aus der Rolle (`Grade`/`GradeComment` sind Vater-Daten). Begründung: die User Story verlangt „wann“, nicht „wie war es“.
   Kosten: eine eigene Antwortform statt `KlassenarbeitResponse`. Schließt aus: eine Note, die das Kind über diesen Weg liest.
3. **Die Anzeige gilt der nächsten geplanten Arbeit (`Status = Planned`, Termin heute oder später).** Abgesagte, geschriebene und
   vergangene stehen nicht dort (AK 1). Kosten: eine nicht auf „geschrieben“ gesetzte, überfällige Arbeit verschwindet am Tag danach.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht (aus B-248 AK 1 und 2, hier geschärft).

1. Ein Kind sieht seine nächste geplante Klassenarbeit mit Titel, Fach, Datum und Tagen bis zum Termin (heute = 0); eine abgesagte, eine
   geschriebene und eine mit vergangenem Termin steht nicht dort. *Rot, wenn* der Endpunkt `Status` oder das Datum ignoriert.
2. Die Antwort enthält weder Note noch Notenkommentar. *Rot, wenn* `Grade` oder `GradeComment` in der Antwortform stehen.
3. Ein anderes Kind und ein Vater ohne Betreuung dieses Kindes sehen sie nicht (`403`/`404`, wie die übrigen Student-Lesepfade). *Rot, wenn* die
   Route ohne Betreuungs-Prüfung hängt.
4. Hat das Kind mehrere geplante Arbeiten, zeigt die Sohn-App die mit dem frühesten Termin; ohne Arbeit erscheint nichts (kein leerer
   Kasten). *Rot, wenn* die späteste gezeigt wird oder die Karte ohne Inhalt steht.

## Schätzung

**Größe: M** – wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein kleiner Lesepfad (Backend S) plus eine Karte in
`SohnHome` mit Test und E2E (Frontend S bis M). Kein Schema, keine Pflicht.

- **`migration: nein`** – nachgesehen: nur lesen, `Klassenarbeit` hat alle Felder.
- **`vertragsbruch: nein`** – nachgesehen: ein **neues** DTO unter `Contracts/Student` (der Name `ClassTestDtos` ist im Supervisor-Ordner
  belegt, darum ein eigener Dateiname); kein bestehendes Feld ändert sich.
- **`wo: beides`** – Backend zuerst, dann die Karte in `frontend/src/sohn/SohnHome.tsx`.

### Risiken

- **Fremde Frontend-Arbeit im Arbeitsbaum:** `frontend/src/sohn/SohnApp.tsx`, `SohnPractice.tsx`, `SohnTest.tsx` und
  `frontend/package.json` sind ungecommittet verändert (LearningDad). Die Karte gehört nach `SohnHome.tsx` (nicht verändert) und
  `lib/api.ts`/`lib/types.ts`; ein Bau, der `SohnApp.tsx` berühren muss, kollidiert.
- **Endpunkt-Abdeckung:** jeder neue Endpunkt braucht `EndpointCoverageGuard`-Treffer und ein Doku-Beispiel (`DocsCaptureTests`).
- **Zeitzone:** „Tage bis zum Termin“ rechnet in UTC wie der Rest (`CLAUDE.md`, Zeit/UTC); nahe Mitternacht zeigt das Kind ggf. einen
  Tag zu viel oder zu wenig.
- **Texte:** die Sohn-App übersetzt über `useTranslation` (`SohnHome.tsx`); ein neuer Text braucht seine Schlüssel in allen Sprachen.

### Angriffsplan

1. `Contracts/Student`: neues Antwort-DTO (Titel, Fach, Datum, Tage).
2. Controller unter `Controllers/Student` nach dem Muster `ChildTodayController`: Betreuung prüfen, nächste geplante Arbeit lesen.
3. Frontend: `lib/types.ts`, `lib/api.ts`, Karte in `SohnHome.tsx`, Texte.
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`; im Frontend `tsc` und `vitest` über `./node_modules/.bin/`.

### Testweg

- Neue Klasse `KlassenarbeitCountdownTests` in `backend/Pugling.Api.Tests` (Aufbau über `TestApi`, Muster `KlassenarbeitenTests`): AK 1
  (je Status und Datum ein Fall), AK 2, AK 3 (anderes Kind, fremder Vater), AK 4 (zwei Arbeiten).
- `frontend/src/sohn/SohnHome.test.tsx`: die Karte erscheint mit Tagen, ohne Arbeit gar nicht (AK 4).
- Neue E2E `frontend/e2e/klassenarbeit-countdown.spec.ts` (Muster `assistent-klassenarbeit.spec.ts`): Vater plant eine Arbeit, das Kind
  sieht den Countdown. Das ist zugleich der Rollengang.
- Vor der Abnahme: Agenten `pugling-reviewer` und `frontend-reviewer`; `/smoke-test`.

## Verlauf

- **2026-11-20** — angelegt beim Teilen von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) und geschätzt (Vorbereitungslauf,
  Sonnet): M, beides, keine Migration, kein Vertragsbruch. Entscheidungen stellvertretend, unbestätigt.
