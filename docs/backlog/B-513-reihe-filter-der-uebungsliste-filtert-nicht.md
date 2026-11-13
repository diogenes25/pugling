---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Reihe-Filter Übungsliste, seriesId Suche]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-456-nachschau-reste-sprint-28.md (Altbestand, abgeteilt im Tageslauf, Sprint 52)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-513 · Der Filter „Reihe" der Übungsliste schränkt ohne Unit nichts ein

`/vater/exercises`: wer Fach und Reihe wählt, aber keine Unit, sieht die Übungen des ganzen Fachs. `VaterExercises.tsx`
liest `seriesId` aus der Query, schickt ihn aber nicht mit (`api.searchExercises` kennt nur `subjectId`/`seriesUnitId`),
und der Server hat keinen Parameter dafür (`ExerciseCatalogController.cs:52`). Die Auswahl sieht wie ein Filter aus und ist
keiner.

Abgeteilt aus [B-456](B-456-nachschau-reste-sprint-28.md) („Altbestand"), weil die Reparatur einen Suchparameter im Backend
braucht (API-First) und B-456 sonst reines Frontend ist.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **API-First:** die Katalogsuche bekommt `seriesId` (alle Units einer Reihe), additiv; `Pugling.Client` und
   `api.searchExercises` reichen ihn durch.
2. Die Übungsliste schickt `seriesId` nur, wenn keine Unit gewählt ist (die Unit ist enger), und der Filterwechsel
   setzt `skip` zurück wie bei den anderen Filtern.

## Akzeptanzkriterien

1. `GET creator/exercises?subjectId=&seriesId=A` liefert die Übungen beider Units von A und keine aus B.
2. Die Übungsliste sendet `seriesId` bei gewählter Reihe ohne Unit.

## Verlauf

- **2026-11-13** — angelegt (Tageslauf, Sprint 52, aus B-456 abgeteilt). Am Code gelesen.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 57).
- **2026-11-13** — gebaut (Sprint 57). Server-Parameter `seriesId`, Client, `api.searchExercises`, Übungsliste
  (`reiheFilter`). Review: 🟡 derselbe Fehler in der Übungsauswahl des Planbaus (`ExerciseFilterBar`: Reihe nur lokale
  Zwischenstufe) → im Sprint behoben (Reihe ist Filter, in `kriterien` des Planbaus; Zähler 1); 🟢 Ziel-Auswahl
  (`VaterZiele`) und veralteter Kommentar ebenfalls nachgezogen. `docs/openapi/v1.json` neu geschrieben.
  - AK 1 → `ExerciseSearchSeriesTests.Reihe_Filtert_Alle_Ihre_Units_Und_Nur_Sie`; X1 (Filter aus), X2 (nach Unit statt Reihe) → rot.
  - AK 2 → `VaterExercises.test.ts` „sendet die Reihe nur ohne Unit"; X3, X4 → rot. Planbau → `ExerciseFilterBar.test.tsx`
    (Y1: Reihe nicht weitergegeben → rot; Y2: Fachwechsel lässt Reihe stehen → rot). Verdrahtung in `PlanPositions`
    (`kriterien`) und `VaterZiele` ohne eigenen Test.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1439/1439, Vitest 825/825, `tsc -b` sauber, Playwright 140/140
    auf 5201/5174; Mutanten X1–X4, Y1, Y2, Z1, Z2 8/8 rot nach der letzten Änderung.
  - Rollengang Vater: E2E-Suite (Planbau, Übungsliste) gegen echten Server und UI; Sohn (B-515): Komponententests,
    E2E-Suite grün; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 59/60, frischer Reviewer, gelesen; Filter-Wege, Render-Phasen-Zustand bzw. NEU-Erkennung gegen die echten Unit-Notizen nachgerechnet). Befund: 🟡 Ziel-Wähler „ganze Reihe" speichert das Fach; 🟡 Planbau-Verdrahtung ohne Test → [B-518](B-518-nachschau-reste-sprint-57.md).
