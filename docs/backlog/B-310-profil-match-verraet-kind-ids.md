---
tags: [typ/story, status/idee, bereich/backend, bereich/sicherheit, rolle/creator]
aliases: [profiles/match 404 gegen 403, Kind-Id-Abfrage per Match]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-308/B-309 in Lauf F, Teil 5 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-310 · `GET creator/profiles/match` verrät per 404 gegen 403, ob es eine Kind-Id gibt

`CreatorProfilesController.Match` antwortet **404**, wenn das Kind nicht existiert, und **403**, wenn es existiert,
der Aufrufer es aber nicht betreut. Jeder Creator kann so durchprobieren, welche Kind-Ids es gibt – der Kommentar
am Endpunkt nennt genau diese Sorte Seitenkanal als Grund für die Betreuungsprüfung. Der Prüfer verweist auf eine
Repo-Regel „gibt es nicht“ und „nicht meins“ antworten beide 404; ob und wo sie steht, ist **nicht geprüft**
(`unverifiziert`) – das ist der erste Schritt, zusammen mit der Frage, ob andere Kind-Endpunkte es genauso halten.

**Behebung (Vorschlag, wenn die Regel trägt):** beide Fälle 404; der Fang im Agenten (`ShowProfilesAsync`, B-308)
bräuchte dann nur noch `NotFound`. Achtung: Statuscode-Änderung am Vertrag – `vertragsbruch` vor dem Schätzen klären.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-308/B-309 (Lauf F, Teil 5, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  🟡 außerhalb des Diffs; eigene Story, weil das Sprint-Ziel ohne sie erfüllt ist.
