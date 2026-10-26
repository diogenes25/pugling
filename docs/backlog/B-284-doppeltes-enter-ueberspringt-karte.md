---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Doppeltes Enter überspringt eine Karte]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-05 in Lauf D (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-284 · Ein doppeltes Enter während der Prüfung überspringt eine Karte

In `frontend/src/sohn/SohnPractice.tsx` (`judge`) läuft `next()` bewusst außerhalb von `action.run`, und zwar immer.
Verwirft `action.run` einen zweiten Aufruf wegen der Sperre (`false`), weil ein Review noch läuft, ruft `judge` trotzdem
`next()` auf. Ein doppeltes Enter überspringt dann eine Karte. Das gilt für Buchstabenkästchen und Textfeld schon
länger, nicht erst seit [B-05](B-05-buchstaben-tausch.md). Aus Code gelesen (`frontend-reviewer`), nicht gemessen.

Vorschlag: `next()` nur aufrufen, wenn der Lauf nicht verworfen wurde.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-05 in Lauf D ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
