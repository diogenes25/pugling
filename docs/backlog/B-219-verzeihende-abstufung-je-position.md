---
tags: [typ/story, status/idee, bereich/training, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Verzeihende Abstufung, Box-Abstieg, Soft Demotion]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-219 · Verzeihende Abstufung als Positions-Option

Ein Fehler setzt ein Item bei uns auf Box 1 zurück, Leitner-rein und hart. Khan Academy stuft aus dem
Prozentwert einer Runde (70–99 % Familiar, 100 % Proficient) und verzeiht in der Mastery Challenge einen
von zwei Fehlern je Skill. Die Idee: eine Option an der `PlanPosition`, mit der ein Fehler das Item nur
**eine** Box senkt und erst ein zweiter Fehler in Folge auf Box 1 zurücksetzt. Der Default bleibt das
heutige Verhalten; der Supervisor entscheidet je Position, wie hart die Wiederholung greift.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht; der harte Reset sitzt an **zwei** Stellen (`PositionPlayService.cs:283`, `ItemProgressService.cs:66`) – zu klären, ob die Milde auch für den plan-übergreifenden Lernstand je Item gilt.
