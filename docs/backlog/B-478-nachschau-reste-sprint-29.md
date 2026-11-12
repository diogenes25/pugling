---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 29, IntroducedCount Pool]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 29/30)
unverifiziert: true
entgangen_bei: [B-04, B-451]
grund: ""
ersetzt_durch: []
---

# B-478 · Nachschau-Reste Sprint 29: „eingeführt" nach dem Verkleinern, Dosis-Sperre über die Übung umgehbar, zwei Kommentare

Vier kleine Funde der Nachschau von Sprint 29 (gelesen, nicht nachgestellt):

1. **`IntroducedCount` nach dem Verkleinern des Pools falsch** (`PositionProgressService.cs:182-183, 204`): die
   gruppierte Abfrage filtert nicht auf `ItemIndex < poolSize`, sie deckelt nur mit `Math.Min`. 60 Wörter eingeführt,
   der Vater senkt `ItemCount` auf 50, von den ersten 50 sind 30 eingeführt → das Kind liest 50/50, die Zeile „noch vor
   dir" verschwindet.
2. **Entscheidung 9 von B-04 über die Übung umgehbar**: geprüft wird nur beim Anlegen/Ändern der Position
   (`PlanPositionsController.cs:173, 253`). Stellt der Creator eine zugewiesene Liste später auf `ordered: false`
   (`ExerciseControllerBase.cs:472`), behält die Position ihre Dosis.
3. **Entscheidung 5 von B-04 nur halb**: `IntroducedCount` sieht nur das Kind (`SohnHome.tsx:141-143`), der Vater
   dieselbe Zahl nur im Positions-Report (`introducedItems`). Entscheidungstext oder „Heute"-Karte angleichen.
4. **Zwei Kommentare**: `VierWochenTests.cs:181` widerspricht seiner Zusicherung bei `:196`; der Blockkommentar in
   `PlanPositionsController.cs:87-111` steht seit `595b429` über den Dosis-Prüfungen statt über `TimeSlotProblem`.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 29/30 im Backlog-Lauf, `pugling-reviewer`).
