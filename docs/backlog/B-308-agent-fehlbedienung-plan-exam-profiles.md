---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [Fehlbedienung je Teil wiederholt, profiles --child unbekannt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-306/B-307 in Lauf F, Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-308 · Zwei Rest-Pfade des Agenten bei einer falschen Id: `plan`/`exam` melden sie je Teil, `profiles` evtl. roh

Zwei Hinweise des Prüfers aus Lauf F, Teil 4, beide außerhalb des Diffs, beide **nicht gemessen**:

1. `plan`/`exam` mit `--profile`, `--title`, einer unbekannten `--series` und einer `--series-unit`: die Fehlbedienung
   (seit [B-307](B-307-create-mit-unbekannter-reihe-rohe-404.md) keine rohe 404 mehr) kommt erst aus dem Briefing, und
   `PlanPlanner.RunAsync` (~Zeile 161) bzw. `ExamPlanner.RunAsync` (~Zeile 76) fangen `AgentUsageException` je Teil –
   dieselbe Meldung erscheint N-mal als gescheiterter Teil, statt den Lauf einmal mit Exit 2 zu beenden. Kein
   Modellaufruf geht verloren.
2. `profiles --child <unbekannt>`: `AgentCommands.ShowProfilesAsync` (~Zeile 161) ruft `MatchProfilesAsync` ohne
   `NotFound`-Fang. Ob der Server dort 404 antwortet, ist ungeprüft.

**Erster Schritt:** beide messen (Test mit `--series 987654` im `plan`-Verb; `profiles --child 987654`). Fällt keiner,
ist die Story `verworfen`.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-306/B-307 (Lauf F, Teil 4, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Funde 6 und 7 (🟢, außerhalb des Diffs). Eigene Story, weil das Sprint-Ziel ohne sie erfüllt ist; `idee`, weil
  unverifiziert.
