---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Nachschau-Reste Sprint 52]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 52 in der Retro von Sprint 54/55)
unverifiziert: false
entgangen_bei: [B-443, B-456]
grund: ""
ersetzt_durch: []
---

# B-515 · Nachschau-Reste Sprint 52: Fokus bei leerer Runde, eine Zahl, die beim Blättern verschwindet

Nachschau von Sprint 52 (frischer `frontend-reviewer` an `59fc0a8`, gelesen, Vitest 68/68). Kein 🔴/🟡, drei 🟢:

1. `VaterExercises.tsx:191`: `gemesseneZahl` blendet die Zahl auch beim Blättern/Sortieren aus (`skip`/`sort`/`dir` stehen
   in den Abhängigkeiten); nach einer gescheiterten Seitenabfrage bleibt sie weg, während der `Pager` `total` zeigt.
   B-456 verlangte nur „gehört zu den Filtern".
2. `SohnPractice.tsx:163`/`:287`: liefert die Runde nach „Jetzt abfragen" keine Karten (`empty`) oder scheitert (`error`),
   fällt der Fokus auf `body`; `focusAfterLoad` bleibt danach `true` stehen.
3. `SohnPractice.test.tsx:375`: der Fokus-Test prüft nur `INPUT|BUTTON`, nicht das Antwortfeld der neuen Karte.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 52, Retro Sprint 54/55).
