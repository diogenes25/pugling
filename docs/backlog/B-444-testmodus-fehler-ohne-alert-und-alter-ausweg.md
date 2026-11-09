---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Testmodus-Fehler ohne alert]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-444 · Der Testmodus meldet einen Fehler stumm, und der Text zu `exercise_empty` nennt einen Ausweg

Gefunden im Review zu B-424 (Lauf AF, Sprint 26, `frontend-reviewer`, 🟢). Seit B-424 erreicht der Vater den Testmodus auch aus
der Zuweisen-Perspektive; zwei ältere Mängel werden damit sichtbarer:

1. Der Fehlerkasten in `frontend/src/vater/ExercisePreviewModal.tsx` trägt kein `role="alert"`; der Fokus sitzt beim Öffnen auf
   „×", ein nachgeladener Fehler wird nicht angesagt.
2. Der deutsche Text zu `exercise_empty` (`frontend/src/lib/api.ts:87`) nennt einen Ausweg („Füge erst Wörter hinzu …") – gegen die
   Meldungsregel vom 2026-11-07 – und sagt „Wörter" auch bei Lückentext oder Liste.

## Verlauf

- **2026-11-09** — angelegt aus dem Review zu B-424; außerhalb des Sprint-Diffs, darum nicht gezählt und nicht in diesem Sprint gebaut.
