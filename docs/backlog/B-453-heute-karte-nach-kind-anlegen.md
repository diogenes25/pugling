---
tags: [typ/story, status/in-arbeit, bereich/vater-web, rolle/supervisor]
aliases: [Heute-Karte veraltet]
status: in-arbeit
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-453 · Nach „Kind anlegen" sagt die Karte „Heute" weiter „Noch keine Kinder"

Beobachtet im Rollengang am 2026-11-10 mit einem frisch registrierten Vater: nach dem Anlegen des ersten Kindes
zeigt die Tabelle „Kinder" die neue Zeile, die Tabelle „Heute" darüber bleibt bei „Noch keine Kinder", bis die
Seite neu geladen wird. Vermutung: `addChild` lädt nur die Kinderliste neu, nicht die Tagesübersicht
(`frontend/src/vater/VaterDashboard.tsx`). Für das Ziel des Fertigstellungs-Laufs nicht tragend, darum `idee`.

## Verlauf

- **2026-11-10** — angelegt aus Rollengang 0 des Fertigstellungs-Laufs (Befund Z6).
- **2026-11-10** — Sprint 30: belegt am Code (`VaterDashboard.tsx`, `addChild` lud nur `children` neu, die Karte hängt an `today`), behoben mit `today.reload()`; Regressionszeile in `e2e/ziel-vier-wochen.spec.ts` (Karte „Heute" nennt das Kind direkt nach dem Anlegen).
