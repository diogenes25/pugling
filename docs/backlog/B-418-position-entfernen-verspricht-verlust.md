---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Position entfernen kündigt Verlust an]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08.md#runde-2--review
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-418 · „Position entfernen" kündigt den Verlust des Fortschritts an – den es nie gibt, weil der Server dann ablehnt

Gefunden im Frontend-Review zu Sprint 21 (`frontend-reviewer`, Nebenbefund zu B-411), bewusst nicht im Sprint (älterer Code). Die
Bestätigungsfrage in `frontend/src/vater/PlanPositions.tsx:688` lautet „Fortschritt und Auswertung dieser Position gehen verloren". Hat die
Position aber Übungs- oder Testdaten, lehnt der Server mit `409 position_has_data` ab (`PlanPositionsController.cs:272`). Der Vater bestätigt
also einen Verlust und liest danach, dass das Entfernen nicht geht. Ohne Daten gibt es nichts zu verlieren. Die Ankündigung ist in keinem der
beiden Fälle wahr. Am Code geprüft am 2026-11-08.

## Verlauf

- **2026-11-08** — angelegt aus dem Frontend-Review zu Sprint 21 (Lauf AD).
