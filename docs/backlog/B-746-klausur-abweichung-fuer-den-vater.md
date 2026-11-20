---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/frontend, rolle/supervisor]
aliases: [Abweichung Prognose Note, Eichung von Hand]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-683
grund: ""
ersetzt_durch: []
---

# B-746 · Prognose und Note nebeneinander: die Abweichung für den Vater

Das Modell eicht sich im ersten Bau nicht von selbst ([B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md), Entscheidung 2). Statt
dessen sieht der Vater je Klassenarbeit die Prognose-Stufe neben der echten Note und stellt den Notenschlüssel
([B-740](B-740-notenschluessel-percenttograde.md)) bei Bedarf von Hand nach. Backend zuerst: ein additives Feld an der Klassenarbeit;
danach die Spalte in der Klassenarbeiten-Liste.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-683.*

## User Story

Als *Vater* möchte ich nach einer Arbeit sehen, was die Prognose erwartet hat und was die Note war, damit ich der Prognose trauen
oder den Schlüssel nachstellen kann.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`.

- `KlassenarbeitResponse` trägt keine Prognose (`backend/Pugling.Contracts/Supervisor/ClassTestDtos.cs`); der Mapper steht im
  `KlassenarbeitenController` (`Map(...)`, Aufrufe `:117`, `:165`).
- Die Klassenarbeiten-Seite des Vaters: `frontend/src/vater/VaterClassTests.tsx` mit Tabelle (Notenzelle `GradeCell`, `:135`,
  `:159`) und der Liste „zu wiederholen" (`:268`); Test `VaterClassTests.test.tsx`.
- Der Schnappschuss `PredictedLevel` entsteht mit [B-744](B-744-klausur-prognose-schnappschuss.md); die Abweichung ist daraus und
  aus der Note berechenbar, eine eigene Speicherung ist nicht nötig.

## Entscheidungen

1. **Die Abweichung wird nicht gespeichert, sondern aus `PredictedLevel` und `Grade` abgeleitet; das Feld `PredictedLevel` steht
   additiv an der `KlassenarbeitResponse`** (B-683 Entscheidung 2). Kosten: Wird der Schlüssel später geändert, ändert sich die
   angezeigte Abweichung älterer Arbeiten nicht (die Stufe ist am Klausurtag festgeschrieben, die Note-zu-Stufe-Umrechnung aber
   nicht). Verworfen: automatisches Nachstellen des Schlüssels. Schließt aus: „wird nachgeschärft" im Sinn von „von selbst".
2. **Nur der Vater sieht die Abweichung** (Rahmen D, B-683 Entscheidung 6): Das Kind sieht seine Klausur nicht.

## Akzeptanzkriterien

1. Die Klassenarbeiten-Liste zeigt je Zeile mit Schnappschuss und Note die Prognose-Stufe neben der Note und ihre Differenz; ohne
   Schnappschuss zeigt sie „keine Prognose". *Rot, wenn* eine Zeile ohne Schnappschuss eine Stufe zeigt (der Ausgangszustand zeigt
   keine, der Test prüft die Zeile mit Schnappschuss und die ohne).
2. `KlassenarbeitResponse.PredictedLevel` ist leer, solange nichts gesetzt wurde, und trägt danach die Zahl. *Rot, wenn* das Feld im
   Vertrag fehlt oder bei fehlendem Schnappschuss eine Null liefert.
3. Ein Kind erreicht die Abweichung nicht. *Rot, wenn* ein Student-Endpunkt `Klassenarbeit` mit `PredictedLevel` liefert (Absichts-
   erklärung; es gibt heute keinen).

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01) plus eine Spalte in einer Tabelle: ein additives Feld, eine Zelle.

- **`wo: beides`**, Backend zuerst: ein Feld im Antwort-DTO und im Mapper, danach die Tabellenspalte.
- **`migration: nein`**: das Feld kommt aus der Spalte aus B-744.
- **`vertragsbruch: nein`**: neues optionales Feld an `KlassenarbeitResponse`. Ein positionaler Record bekommt ein neues Argument am
  Ende; Aufrufer des Konstruktors (Mapper, Tests) ziehen mit, nach außen additiv. `contract.ts` wird neu erzeugt.

### Risiken

- **Positionaler Record:** `KlassenarbeitResponse` ist ein Record; ein Argument am Ende verlangt, jeden Konstruktoraufruf in
  Controller und Tests anzufassen (Suche vor dem Bau). Kein Bruch auf dem Draht.
- **`unknown_field`:** Das Frontend schickt `KlassenarbeitResponse` nie zurück; kein Risiko dort. `UpdateClassTestDto` bekommt
  kein Feld.
- **Zeigen heißt nicht eichen:** Der Vater braucht einen Ort, an dem er den Schlüssel nachstellt; das ist eine Datei
  (`appsettings.json`, [B-740](B-740-notenschluessel-percenttograde.md)) und für ihn kaum nutzbar. Das ist die Folge der
  Entscheidung (Kosten), kein Defekt dieser Story.

### Angriffsplan

1. `PredictedLevel` an `KlassenarbeitResponse` (Contracts) und im Mapper des Controllers; Tests, die den Konstruktor aufrufen,
   nachziehen.
2. OpenAPI und `frontend/src/lib/contract.ts` neu erzeugen.
3. Spalte in `VaterClassTests.tsx`: „Prognose / Note / Differenz".
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Vitest, Playwright.

### Testweg

- Backend: `KlassenarbeitenTests` (vorhanden) um einen Fall ergänzen, dazu `KlassenarbeitPrognoseTests` (neu, aus
  [B-744](B-744-klausur-prognose-schnappschuss.md)) für das gesetzte Feld (AK 2); `ContractDocumentTests` läuft mit.
- Frontend: `frontend/src/vater/VaterClassTests.test.tsx` (vorhanden) für AK 1, ein Fall mit, einer ohne Schnappschuss.
- Rollengang: E2E-Spec `frontend/e2e/klassenarbeit-abweichung.spec.ts` (neu), Vater trägt eine Note ein und sieht die Zeile; das
  ist zugleich der Rollengang für B-744 und B-745.
- Vor der Abnahme: beide Reviewer (`pugling-reviewer`, `frontend-reviewer`).

## Verlauf

- **2026-11-20** — angelegt aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (geteilt); `geschaetzt`: S, beides (Backend
  zuerst), keine Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
