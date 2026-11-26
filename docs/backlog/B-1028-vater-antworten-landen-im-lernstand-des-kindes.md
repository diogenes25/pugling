---
tags: [typ/story, status/idee, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Vater-Antworten im Lernstand]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#iteration-193--umgesetzt
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1028 · Antworten des Vaters (Vorschau, Nachtrag) landen im Lernstand und in der Auswertung des Kindes

Fund des `pugling-reviewer` zu B-733 (2026-11-26), bewusst aus Sprint 193 herausgelassen: das Sprint-Ziel hält ohne ihn, und die
Ursache liegt auf der Schreibseite, nicht in der Auswertung.

Ein Vater, der den Test einer Position probehalber macht (`TestAttempt.BySupervisor`) oder für das Kind übt, schreibt über
`ItemProgressService.RecordAsync(..., countsForMastery: true)` (`PositionTestsController.cs:480`, `PositionPracticeController.cs:756`)
eine `ItemReviewEvent`-Zeile auf das Kind und bewegt dessen `ItemProgress` (Box, Beherrschung, `IntroducedAt`). Die Zeile trägt
keinen Hinweis, wer geantwortet hat. Seit B-733 erscheinen diese Antworten zusätzlich als Antworten, Fehlerarten und Problemwörter
des Kindes und bleiben in einem abgeschlossenen Tag für immer stehen. Beim Nachtrag eines vergangenen Tags steht `At` auf jetzt,
die Sitzung auf dem alten Tag.

Zu klären: nichts schreiben, wenn kein Student antwortet, oder die Zeile kennzeichnen (Spalte, Migration) und filtern.
