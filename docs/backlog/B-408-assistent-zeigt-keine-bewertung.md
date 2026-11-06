---
tags: [typ/story, status/idee, bereich/frontend, rolle/vater]
aliases: [Assistent ohne Bewertung]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-408 · Der Plan-Assistent zeigt in seiner Übungsauswahl keine Bewertung – sein Zwilling im Planbau schon

Seit [B-270](B-270-bewertung-im-positions-formular.md) zeigt die Übungsauswahl im Planbau (`PlanPositions.tsx`) je Zeile die Bewertung und
sortiert auf Wunsch „beste zuerst". Der Assistent (`VaterWizard.tsx`) nennt der Code ausdrücklich ihren Zwilling (gleiche Filterleiste,
gleiche Zeilen) – dort fehlt beides. Ein Vater sieht den Unterschied direkt. Dazu als Verfeinerung für beide: beim Umsortieren die Auswahl
behalten, wenn die gewählte Übung in der neuen Seite noch steht.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-270 (Lauf AB, Sprint 19; `frontend-reviewer`, 🟡/🟢). Bewusst nicht im Sprint: B-270 nennt nur
  den Planbau, und eine zweite Fläche ist Produktumfang (`Wunsch`), kein Herstellen.
