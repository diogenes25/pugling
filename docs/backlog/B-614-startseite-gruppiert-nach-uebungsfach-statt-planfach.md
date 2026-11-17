---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Fach der Übung statt des Plans]
status: idee
prio: P3
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

# B-614 · Die Startseite gruppiert nach dem Fach der Übung, nicht nach dem Fach des Plans

`PositionStatus.SubjectName` kommt aus `SubjectNameOf` (`Series.Subject?.Name ?? Series.SubjectName`, B-606), die
Startseite gruppiert danach (`tagessicht.ts`). Seit B-539 trägt jeder Plan ein Pflichtfach – die Gruppierung ignoriert
es: eine fachfremde Übung landet unter einer anderen Überschrift, ein Freitext „English" neben dem Katalognamen
„Englisch" ergibt zwei Gruppen, Positionen zweier Pläne teilen sich eine. Naheliegend: den Fachnamen des **Plans**
liefern.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟢, `pugling-reviewer`), `entgangen_bei: [B-606]`.
