---
tags: [typ/story, status/idee, bereich/vater-web, rolle/supervisor]
aliases: [Heute-Karte veraltet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
unverifiziert: true
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
