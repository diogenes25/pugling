---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Study plan not found, veraltete Plan-Id im Trophäenweg]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-23-nacht.md (dritter Lauf, Rollengang Sprint 1)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-254 · Der Trophäenweg zeigt dem Kind „Study plan not found." statt einer Erklärung

Im Rollengang des dritten Nachtlaufs (Wegwerf-DB, Kind #1 ohne Plan, im Browser eine gemerkte Plan-Id
`pugling.plan.1` aus einer älteren DB) zeigte die Startseite richtig „Noch keine Mission", der Tab „Weg"
(`/sohn/progress`) aber die rohe englische Serverantwort „Study plan not found. (Ref: …)". Vermutung, noch
nicht am Code belegt: `SohnProgress` lädt mit der in `localStorage` gemerkten Plan-Id, ohne sie gegen die
Pläne des Kindes zu prüfen – derselbe Zustand entsteht, wenn der Vater den gemerkten Plan löscht. Zu klären:
woher die Startseite ihre richtige Aussage nimmt und warum der Weg sie nicht teilt; ob die gemerkte Id
verworfen werden muss, sobald der Server 404 meldet. Außerhalb des Diffs von Sprint 1 gefunden, darum eigene
Story und kein Zählerfund.

## Verlauf

- **2026-10-23** — angelegt im Rollengang von NP-1 (Browser, Freigabe 6); ungeprüft.
