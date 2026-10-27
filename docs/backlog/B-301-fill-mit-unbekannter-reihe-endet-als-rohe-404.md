---
tags: [typ/story, status/in-arbeit, bereich/agent, rolle/creator]
aliases: [fill --series unbekannt, rohe 404 bei unbekannter Reihe]
status: in-arbeit
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 9 (docs/pm-sitzung-2026-10-27-nacht.md, Lauf F Teil 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22, B-297]
wartet_auf: ""
---

# B-301 · `fill --series 99999` endet als rohe 404 statt als Fehlbedienung

[B-297](B-297-404-pfad-des-fill-verbs-ungetestet.md) hat die eine Hälfte des Id-Paars belegt: eine Unit einer
anderen Reihe wird zur `AgentUsageException` (Exit 2, Gebrauchstext). Die andere Hälfte – eine Reihe, die es nicht
gibt – fängt `UnitNotesFiller.RunAsync` gar nicht: `GetSeriesAsync` wirft eine rohe `PuglingApiException`,
`Program.cs` druckt „Die Pugling-API antwortete mit 404“ und endet mit **1**. Das ist eine Codelücke, keine reine
Testlücke.

**Behebung:** dasselbe `catch (… when NotFound)` um den Reihen-Abruf, Meldung „Reihe {id} gibt es nicht.“; ein Test
mit unbekannter Reihe, der den Typ, die Id in der Meldung und `chat.Calls == 0` zusichert.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 9 (`aab2ef9`) in Lauf F, Teil 1
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Fund 2 (🟡) des frischen Prüfers; `entgangen_bei: B-22` (dort
  entstand der Reihen-Abruf) und `B-297` (deren Sprint-Ziel war „eine rohe 404 fällt rot“). Als `Defekt` unter
  Freigabe 1 ausformuliert und geschätzt (XS).
