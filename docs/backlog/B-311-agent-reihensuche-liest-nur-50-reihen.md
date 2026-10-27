---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [Reihensuche des Agenten nur erste Seite, ResolveLocationAsync take 50]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf F Teil 5 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-308]
wartet_auf: ""
---

# B-311 · Der Agent sucht Reihe und Unit nur in den ersten 50 Reihen

`AgentCommands.ResolveLocationAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs`, um Zeile 455) ruft
`creator.ListSeriesAsync(ct: ct)` ohne Paging; der Client nimmt dann `take = 50` (`Pugling.Client/CreatorApi.cs`).
Wer nur `--series X` oder nur `--series-unit Y` angibt, wird bei einer gültigen Reihe/Unit jenseits der ersten 50 mit
einer **falschen Fehlbedienung (Exit 2)** abgewiesen („Es gibt keine Reihe mit Unit …“ bzw. „Unit Y gibt es nicht …
in keiner Reihe“). Der Prüfer nennt es dieselbe Paging-Falle wie früher bei den Interessen-Tags (Bezug nicht
geprüft).
Nebenbei: eine unbekannte `--series 987654` ohne Unit bekommt die falsche Diagnose statt ihrer Id.

**Vorschlag des Prüfers:** mit `--series` erst `GetSeriesAsync(id)` (404 → Fehlbedienung wie
`ResolveSubjectIdAsync`), dann `ListUnitsAsync`; die Reihenliste nur ohne `--series` und dann über alle Seiten.

`unverifiziert`: aus dem Code hergeleitet, nicht gemessen. Erster Schritt beim Ausformulieren: ein Test mit 51 Reihen
(die Test-DB hat durch `FreshUnitAsync` ohnehin mehr als 50) – rot, wenn die Behauptung stimmt.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 1 (🟡) des frischen
  `pugling-reviewer` ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1). Geschwister außerhalb des Diffs:
  B-308 versprach „falsche Id → genau eine Fehlbedienung“, hier wird eine **richtige** Id als falsch abgewiesen.
