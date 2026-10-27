---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [profiles --subject unbekannt, falsche Kein-Profil-Meldung]
status: idee
prio: P3
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

# B-312 · `profiles --subject <unbekannt>` endet mit Exit 0 und der Behauptung, es gebe kein Profil

`AgentCommands.cs` (um Zeile 189–193): mit einem Fach-Filter, der keinen Treffer hat – auch einer Fach-Id, die es
nicht gibt –, druckt der Agent „Es gibt noch kein Creator-Profil. Ohne Profil arbeitet der Agent als Generalist.“
und endet mit 0. Das ist falsch, sobald Profile existieren; die Listen-Route prüft die Fach-Id nicht
(`CreatorProfilesController.cs:41`).

**Vorschlag des Prüfers:** mit Filter „Kein Profil für Fach X“ melden, oder die Fach-Id vorher prüfen
(`ResolveSubjectIdAsync`-Muster → Exit 2).

`unverifiziert`: aus dem Code hergeleitet. Ob eine unbekannte Fach-Id eine Fehlbedienung sein soll, folgt aus der
Linie von B-301/B-307/B-308 (falsche Id → Exit 2); die Meldung bei bekanntem Fach ohne Profil ist reine Wortwahl.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 2 (🟢)
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1).
