---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Nacharbeit ohne Inhalte kostet täglich Malus]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-636]
---

# B-668 · Bei leerem Pool endet „täglich, bis erledigt“ nie, und der Malus läuft weiter

## Beobachtung

Aus der Nachschau von [B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md), am Code gelesen, nicht nachgestellt:
`LastTaskDoneOnAsync` liefert bei leerem Pool `null` (`PositionProgressService.cs`, `total == 0`), also stempelt weder der
Rundenstart noch der PATCH ein Ende; `RemediationDayMetAsync` verlangt eine nicht leere Runde. Leert der Creator die
Übung (oder fällt der Pool sonst auf 0), ist die Pflicht unerfüllbar und kostet jeden Tag Malus.

Verwandt, älter (B-635): eine beendete Nacharbeit, die der Vater zurück auf „Tagesziel“ stellt, lebt mit leerer Runde
wieder auf.

## Naheliegend

Bei leerem Pool ein Ende ohne Bonus stempeln, oder den Tag als erfüllt werten – beim Grillen entscheiden.
