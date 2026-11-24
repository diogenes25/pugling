---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, rolle/supervisor]
aliases: []
status: geschaetzt
prio: P1
art: Defekt
groesse: S
wo: "backend"
migration: "nein"
vertragsbruch: "nein"
quelle: "Rollengang Runde 138 an einer Kopie der echten DB (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-757]
wartet_auf: ""
---

# B-948 · „etwa 0 Karten je Tag“, obwohl das Kind für die Arbeit täglich Pflichtkarten spielt

Beobachtet an einer Kopie der echten DB (2026-11-24): Friedrichs Englisch-Arbeit am 4.12. (209 Wörter, 21 sicher) zeigt dem Vater
„etwa 0 Karten je Tag“, ebenso die Französisch-Arbeit am 8.12. und die Mathe-Arbeit am 16.12. Alle Bündel-Übungen liegen schon als
Positionen im aktiven Plan des Fachs; die Vorbereitung legt also nichts dazu, und die Zahl zählt nur die Zusatzlast.

## User Story

Als Vater möchte ich in der Klassenarbeiten-Liste sehen, wie viele Karten das Kind für diese Arbeit täglich als Pflicht spielt, damit
ich nicht glaube, für eine Arbeit in zehn Tagen passiere nichts.

## Ist-Stand am Code (2026-11-24, `f4324888`)

- `ClassTestPrepService.CardsPerDayAsync` (`backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:106-126`): vor dem Start die
  Summe über `NewCandidatesAsync` (nur Übungen, die **noch nicht** im Plan liegen, `:171-183`), nach dem Start die Summe über die
  Positionen mit `SourceKlassenarbeitId == k.Id`. Vorhandene Positionen der Bündel-Übungen zählen nie.
- Die Oberfläche schreibt jede Zahl als „etwa N Karten je Tag“ (`frontend/src/vater/VaterClassTests.tsx:188`).

## Die echte Lücke

Die Zahl heißt „was die Vorbereitung abverlangt“, zählt aber nur, was sie **neu anlegt**.

## Entscheidungen

1. **Nutzer, 2026-11-24: Gesamtlast zählen.** Die Zahl umfasst die Pflicht-Positionen der Bündel-Übungen, die schon im aktiven Plan
   des Fachs liegen. Verworfen: „0 mit Server-Grund“ (ehrlich, aber der Vater erfährt die Last nicht).
2. Gezählt wird eine Position, wenn sie heute eine **tägliche** Pflicht trägt (`Cadence == Daily`, `DutySince` leer oder ≤ heute,
   `DutyUntil` leer oder ≥ heute) – stellvertretend entschieden. Eine Kür- oder Wochenposition verlangt nichts „je Tag“. Kosten: Eine
   wöchentliche Pflicht der Bündel-Übung erscheint nicht in der Zahl.
3. Je Position gilt die Regel aus B-946: `NewItemsPerDay`, sonst `PositionPlayService.PoolSize`. Vor dem Start kommen die Kandidaten
   dazu, die die Vorbereitung neu anlegen würde (wie bisher). Nach dem Start sind deren Positionen Pflicht-Positionen des Plans und
   fallen unter Punkt 2.

## Akzeptanzkriterien

1. Vor dem Start: Eine Bündel-Übung, die als tägliche Pflicht-Position im Plan liegt, zählt mit ihrer Tagesdosis.
2. Vor dem Start: Eine Bündel-Übung als Kür-Position (`Cadence None`) zählt nicht.
3. Eine tägliche Position derselben Übung in einem **anderen** Plan (anderes Fach oder inaktiv) zählt nicht.
4. Eine tägliche Position, deren `DutyUntil` vor heute liegt, zählt nicht.
5. Nach dem Start: Gemachte Positionen und vorhandene tägliche Positionen der Bündel-Übungen zählen beide.
6. Bestehende Fälle aus B-757/B-946 (Dosis, Deckel, kleine Übung) bleiben grün.

## Schätzung

- **Größe:** S. Eine Methode, keine Migration, kein Vertragsbruch (die Zahl bleibt `prepCardsPerDay`).
- **Risiko:** Die Liste rechnet je Arbeit `ItemsOfAsync` für Positionen ohne Dosis; dieselbe Größenordnung wie B-946.
- **Testweg:** Integrationstests in `VorbereitungsPflichtTests`, Mutante je AK; E2E `klassenarbeit-vorbereitung` als Rollengang.

## Angriffsplan

`CardsPerDayAsync` umbauen: (a) tägliche Pflicht-Positionen im aktiven Plan des Fachs, deren Übung im Bündel liegt oder die für die
Arbeit gemacht wurden, je mit der B-946-Regel; (b) vor dem Start plus `NewCandidatesAsync`.

## Verlauf

- **2026-11-24** — angelegt im Rollengang Runde 138, Entscheidung 1 vom Nutzer, `geschaetzt` für Sprint 138.
