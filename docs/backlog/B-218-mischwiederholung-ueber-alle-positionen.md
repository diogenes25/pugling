---
tags: [typ/story, status/idee, bereich/training, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Mischwiederholung, Mastery Challenge, Kind-weite Wiederholung]
status: idee
prio: P2
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

# B-218 · Eine Mischwiederholung über alle Positionen des Kindes

Leitner läuft bei uns **pro Position**: fällige Karten werden nur in der Übungsrunde genau dieser
Position wieder vorgelegt. Khan Academy legt daneben alle 12 Stunden eine „Mastery Challenge" aus sechs
Fragen zu drei Skills vor, gewählt nach Zeit seit der letzten Wiederholung und aktueller Stufe; zwei
Fehler je Skill senken die Stufe, einer wird verziehen. Die Idee: eine kind-weite Wiederholungsrunde
aus den fälligsten Items aller spielbaren Positionen, zeitlich gedeckelt, mit derselben
Server-Autorität wie die Positions-Runde. Abzugrenzen von [B-04](B-04-adaptiver-vokabel-pool.md), das
den Pool **innerhalb** einer Position adaptiv macht.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (alle Student-Routen hängen an `positions/{id}`). Haken: die Anti-Cheat-Sperre „ein aktiver Plan“ (`plan_inactive`) macht daraus faktisch eine plan-weite Runde – „kind-weit“ hieße über inaktive Pläne hinweg. Grundlage: `ItemProgress` (`ItemProgressService.cs:66`). Mit B-04/B-219 zusammen grillen.
