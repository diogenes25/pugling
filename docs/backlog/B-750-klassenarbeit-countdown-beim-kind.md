---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, bereich/training, bereich/api, rolle/student]
aliases: [Klausur-Countdown, Klassenarbeit in der Sohn-App]
status: abgenommen
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
- **2026-11-23** — `geschaetzt` → `in-arbeit` (Nachtlauf I, Sprint 133, Freigabe 8; Quelle B-248 trägt die Freigabe des Nutzers vom
  2026-10-23). Gebaut: `GET api/v1/student/children/{childId}/class-tests/upcoming` (`Controllers/Student/ChildClassTestsController.cs`,
  `ChildOwnershipFilter`, „heute“ aus `LearningDay` – der Risiko-Satz „UTC“ oben ist seit B-730 überholt), Antwortform
  `UpcomingClassTestResponse` ohne Note (`Contracts/Student/ClassTestCountdownDtos.cs`), Client-Methode, Karte
  `frontend/src/sohn/KlassenarbeitKarte.tsx` in beiden Zweigen von `SohnHome` (mit und ohne Tagespflicht). Lesart: die Liste trägt
  alle anstehenden Arbeiten, früheste zuerst; die Sohn-App zeigt die erste (AK 4). Ein Fehler beim Lesen blendet die Karte aus (Hinweis,
  kein Weg; im Kommentar begründet).
- **2026-11-23** — Belege je AK-Fall (Mutanten-Probe nach der letzten Änderung, Tabelle im Protokoll `pm-sitzung-2026-11-23-i.md`):
  AK 1 Status → `KlassenarbeitCountdownTests.Zeigt_Nur_Geplante_Ab_Heute_Frueheste_Zuerst`, M1 rot; Datum → derselbe, M2 rot; Tage →
  derselbe, M4 rot; Datum in der Karte → `KlassenarbeitKarte.test.tsx` „nennt Titel, Fach…“, F9 rot; heute/morgen → F3 rot. AK 2 →
  `Antwort_Traegt_Weder_Note_Noch_Notenkommentar`, M5 (Feld `GradeComment` im DTO) rot. AK 3 → `Fremdes_Kind_Und_Fremder_Vater_…`, M6
  (Filter auf `PlanOwnershipFilter` getauscht) rot. AK 4 früheste → M3 rot (Server), F1 rot (Karte); ohne Arbeit kein Kasten → F2 rot.
  Kindwechsel zeigt keine alte Arbeit → F10 rot.
- **2026-11-23** — Review: `pugling-reviewer` ohne 🔴/🟡 (🟢: Bündel-Regel rückwärts noch in `ExerciseCatalogController`, Kommentar
  präzisiert; doppelte Unterabfrage, belassen). `frontend-reviewer`: 🟡 Datum fehlte in der Karte (AK 1) – behoben mit Test (F9);
  🟢 Kommentar zum Lesefehler präzisiert, zwei ungeprüfte Wächter (`stale`, Bereitschafts-Id) mit Test belegt (F10, F11), E2E-Kommentar
  zur Zeitzone korrigiert. Zwei rote Wächter im vollen Lauf behoben: `UnpaginatedArrayGetCount` 31 → 32 (bewusste Zeile), Platzhalter
  `{classTestId}` → `{id}` wie im Vater-Controller.
- **2026-11-23** — Rollengang (maschinell): E2E `e2e/klassenarbeit-countdown.spec.ts` 1 passed auf den Ausweichports 5231/5204 nach der
  letzten Änderung (Vater plant zwei Arbeiten über die API, das Kind sieht auf der Startseite die frühere mit Fach, Datum, „in 5 Tagen“
  und „0 von 2 Wörtern sicher“); `e2e/pfad.spec.ts` 1 passed als Zeuge für den Zweig mit Tagespflicht.
- **2026-11-24** — `in-arbeit` → `abgenommen` (Nachtlauf I, Sprint 133): Commit `65f4d2a7`; Backend-Suite 2185/2185 grün (Release, nach der letzten Änderung; vorher 2175), Frontend `vitest` 1393/1393, `tsc -b` grün, E2E `klassenarbeit-countdown` + `pfad` 2 passed; beide Reviewer gelaufen (keine offene 🔴/🟡); Rollengang maschinell (E2E, siehe oben).
