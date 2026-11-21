---
tags: [typ/story, status/idee, bereich/auswertung, rolle/supervisor]
aliases: [Status offen trotz Antworten]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 97 im Nachtlauf 2026-11-21 (frontend-reviewer und pugling-reviewer), docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-749, B-770]
---

# B-777 · Die Vater-Startseite zeigt „offen“ bei einem Kind, das heute schon geantwortet hat

`HeuteKind` (`frontend/src/vater/VaterDashboard.tsx:111-114`) wählt „dran“ über `c.practiced`. Das Backend setzt
`Practiced` als `day.GoalsMet > 0 || points > 0` (`ChildrenDashboardService.cs:33-34`), und Punkte gibt es erst für ein
erreichtes Positionsziel. „Geübt“ heißt damit faktisch „mindestens ein Ziel geschafft“. Ein Kind mit 25 Antworten und noch
keinem vollen Ziel steht auf **„offen“**, während dieselbe Karte darunter „25 Antworten“ zeigt. B-770 hat die
Antwortzahl eingeführt, damit der Vater den Fleiß nicht aus Punkten abliest, und der Status liest die Zahl nicht. Kein
Test fängt das: In `VaterDashboard.test.tsx` haben beide Kinder `practiced: true`.

Mögliche Reparatur (zu entscheiden): „dran“ bei `practiced || answersToday > 0`, oder serverseitig `Practiced` um
`answers > 0` erweitern. Rote Probe: ein Kind mit `practiced: false, answersToday: 12` erwartet „dran“.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von Sprint 97 im Nachtlauf (`idee`, `entgangen_bei: [B-749, B-770]`).
