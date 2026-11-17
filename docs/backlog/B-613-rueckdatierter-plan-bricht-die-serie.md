---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Serie bricht nachträglich]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
---

# B-613 · Ein rückdatierter oder wieder eingeschalteter Plan bricht die Serie nachträglich

Die Kind-Serie (B-606, Entscheidung 6) zählt für jeden **heute aktiven** Plan jede offene Pflicht seiner ganzen Laufzeit
(`PositionProgressService.IsCompleteDay`/`ChildStreakAsync`/`DutyStateAsync`). Anders als der Malus (`FirstDueDay`,
Positionen erst ab ihrem Anlegen) achtet sie nicht darauf, seit wann eine Position besteht.

- Der Vater legt ein zweites Fach mit `StartDate` in der Vergangenheit an (`StudyPlansController` erlaubt es) ⇒ die
  Serie fällt auf 0, auch die Truhenstufe; Missionen (`CompleteChildDaysAsync`) verlieren rückwirkend Tage.
- Der Vater schaltet einen alten Plan wieder ein (B-539 schaltet dabei den neueren desselben Fachs ab) ⇒ die Tage, an
  denen der alte offen war, zählen plötzlich als unvollständig.

Teil 1 (Tage vor `FirstDueDay` einer Position nie offen) folgt der Malus-Regel; Teil 2 (Wiedereinschalten) braucht eine
Entscheidung, weil das Modell keinen Aktivierungsverlauf kennt (B-539 Entscheidung 6).

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
