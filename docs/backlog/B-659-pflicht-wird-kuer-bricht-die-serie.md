---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Pflicht zur Kür, gelöschte Pflicht bricht die Serie]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Runde 3 (Sprint 87), Review von B-613 (pugling-reviewer, 🟡2)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-659 · Wird eine Pflicht zur Kür (oder gelöscht), bricht die Serie rückwirkend

## Beobachtung

Gefunden im Review von [B-613](B-613-rueckdatierter-plan-bricht-die-serie.md), am Code gelesen (Arbeitsstand von Sprint
87, nicht per Test nachgestellt): Ob eine Position an einem **vergangenen** Tag eine Pflicht trug, liest
`PositionProgressService.HasDutyOn` aus ihrem **heutigen** Takt. Stellt der Vater eine Pflicht auf „Kür“
(`cadence: None`) oder löscht er sie, hat sie an keinem Tag mehr eine Pflicht.

**Szenario:** Plan A mit einer einzigen Tagespflicht, an vier Tagen erfüllt, Serie 4. Der Vater stellt sie auf Kür.
`DutyStateAsync` findet an keinem Tag eine Pflicht, `IsCompleteDay` verlangt mindestens einen Plan mit erfüllter
Pflicht ⇒ alle vier Tage gelten als unvollständig, Serie und `DaysComplete` fallen auf 0. Hat der Plan weitere Pflichten,
fällt nichts – der Schaden trifft nur den Plan, dessen letzte Pflicht wegfällt.

**Widerspricht** B-613 AK 9, letzter Satz („Der umgekehrte Weg (Pflicht → Kür) ändert nichts Rückwirkendes“). Das
Verhalten bestand schon vor Sprint 87; B-613 hat es nicht verursacht und nicht behoben.

## Bewusst nicht in Sprint 87

Das Sprint-Ziel („meine Serie bricht nicht, wenn Papa eine Pflicht nachträgt oder einen alten Plan wieder einschaltet“)
ist ohne diesen Fall erfüllt. Die Abhilfe braucht eine Pflicht-Historie an der Position (etwa ein „Pflicht bis“-Datum
beim Wechsel Takt → Kür und beim Löschen) – eine zweite Migration auf der echten DB und eine Entscheidung, was ein
gelöschter Plan für die Serie bedeutet. Das gehört gegrillt, nicht nebenbei gebaut.

## Offene Fragen (für das Grillen)

1. Soll eine früher erfüllte Pflicht, die heute Kür ist, für vergangene Tage weiter zählen (Historie), oder genügt es,
   dass ein Tag ohne jede Pflicht die Serie **nicht bricht**, sondern überspringt?
2. Was gilt beim Löschen einer Position oder eines ganzen Plans?
