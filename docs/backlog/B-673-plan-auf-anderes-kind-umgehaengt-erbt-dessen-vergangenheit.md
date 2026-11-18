---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Plan umhängen erbt Malus]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer, Hinweis 2) – docs/pm-sitzung-2026-11-18.md, Runde 5"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-673 · Ein aktiver Plan, den der Vater einem anderen Kind gibt, bringt die Vergangenheit des ersten Kindes mit

Aus der Nachschau von Sprint 87, am Code gelesen, älter als B-613: Der PATCH mit `childId`
(`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:185-189`) wechselt den Besitzer, lässt aber
`ActivatedAt` und die Positionshistorie stehen. Die Serie des neuen Kindes wird an den vergangenen Tagen des Plans
gemessen, `FirstPlayDaysAsync` zählt das Spiel des ersten Kindes, und nie abgerechnete Perioden (14-Tage-Fenster) kosten
das neue Kind Malus.

Ob das Umhängen eines laufenden Plans überhaupt ein gewollter Weg ist, ist eine Produktfrage – beim Ausformulieren klären.

## Naheliegend (Reviewer)

Beim Kindwechsel eines aktiven Plans `ActivatedAt = heute` (wie ein neues Einschalten).
