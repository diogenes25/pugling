---
tags: [typ/story, status/abgenommen, bereich/test, bereich/agent, rolle/creator]
aliases: [404-Pfad des fill-Verbs ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
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
- **2026-10-27** — in Lauf E, Teil 9 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS, nur Tests) und gebaut: `Fill_Mit_Unit_Einer_Anderen_Reihe_Ist_Eine_Fehlbedienung` –
  zwei eigene Reihen (beide gehören dem Aufrufer, nur die reihen-gebundene 404 des Controllers kann ablehnen),
  Anfrage (Reihe A, Unit von B) → `AgentUsageException` mit Unit und Reihe im Text, `chat.Calls == 0`. Mutante
  `when (NotFound)` → `when (Gone)` (rohe `PuglingApiException`) → **1 failed**. `Fill_*` **9/9**, Suite **971/971 `exit=0`**.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)): kein 🟡/🔴, Zähler
  bleibt 0; der Prüfer hat je Zusicherung die Mutante benannt (Filter ≠ NotFound → rohe `PuglingApiException`;
  Controller-Suche ohne `u.SeriesId == seriesId` → Unit gefunden, keine Ausnahme; Suche hinter `DraftAsync` →
  `chat.Calls == 1`). Suite **972/972 `exit=0`**. Rollengang: Tests sind der Weg. **`abgenommen`.**
- **2026-10-27** — Commit `aab2ef9` (Lauf E, Teil 9; nicht gepusht).
