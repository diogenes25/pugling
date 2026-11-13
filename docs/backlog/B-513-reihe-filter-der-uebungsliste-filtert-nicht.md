---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Reihe-Filter Übungsliste, seriesId Suche]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-456-nachschau-reste-sprint-28.md (Altbestand, abgeteilt im Tageslauf, Sprint 52)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-513 · Der Filter „Reihe" der Übungsliste schränkt ohne Unit nichts ein

`/vater/exercises`: wer Fach und Reihe wählt, aber keine Unit, sieht die Übungen des ganzen Fachs. `VaterExercises.tsx`
liest `seriesId` aus der Query, schickt ihn aber nicht mit (`api.searchExercises` kennt nur `subjectId`/`seriesUnitId`),
und der Server hat keinen Parameter dafür (`ExerciseCatalogController.cs:52`). Die Auswahl sieht wie ein Filter aus und ist
keiner.

Abgeteilt aus [B-456](B-456-nachschau-reste-sprint-28.md) („Altbestand"), weil die Reparatur einen Suchparameter im Backend
braucht (API-First) und B-456 sonst reines Frontend ist.

## Verlauf

- **2026-11-13** — angelegt (Tageslauf, Sprint 52, aus B-456 abgeteilt). Am Code gelesen.
