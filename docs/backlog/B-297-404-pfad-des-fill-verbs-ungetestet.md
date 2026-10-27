---
tags: [typ/story, status/idee, bereich/test, bereich/agent, rolle/creator]
aliases: [404-Pfad des fill-Verbs ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 7 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-292]
wartet_auf: ""
---

# B-297 · Der 404-Pfad des `fill`-Verbs (Unit gehört nicht zur Reihe) ist ungetestet

Teil 7 hat die Unit-Suche von `ListUnitsAsync().FirstOrDefault` auf `GetUnitAsync` mit
`catch (PuglingApiException) when NotFound → AgentUsageException` umgestellt (`UnitNotesFiller.cs`). Kein Test fährt
den Pfad: ohne den `catch` käme eine rohe `PuglingApiException` (Exit 1 mit Stacktrace statt Exit 2), und die
Suite bliebe grün.

**Behebung:** ein Test mit zwei eigenen Reihen (`FreshUnitAsync`), `UnitFillRequest(reiheA, unitVonB, …)` →
`AgentUsageException`, Meldung nennt Unit und Reihe; pinnt zugleich die reihen-gebundene 404 des Controllers.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 7 (`4a3fa61`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 2 des frischen Prüfers; `entgangen_bei: B-292`.
- **2026-10-27** — Commit `1ee9bf0` (Lauf E, Teil 8; nicht gepusht).
