---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor, rolle/student]
aliases: [Übungsmeldungen verschmelzen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#iteration-193--umgesetzt
grund: ""
ersetzt_durch: []
entgangen_bei: [B-767]
nachgeschaut: ""
---

# B-1029 · Zwei Meldungen zu zwei verschiedenen Übungsantworten verschmelzen zu einer

Fund des `pugling-reviewer` zu B-1027 (2026-11-26), bewusst aus Sprint 193 herausgelassen. Der Zusammenlege-Schlüssel einer Meldung
(`ExerciseFeedbackService.cs:51-53`) enthält die beim Melden eingefrorene **Testantwort**; für eine Antwort aus dem **Üben** ist er
`null`. Tippt das Kind „amazong“, meldet „zu streng“, tippt „amazinng“ und meldet wieder, entsteht ein Eintrag mit Zahl 2, der nur
noch „amazinng“ zeigt – gegen Entscheidung 3 von B-767 („ein Eintrag je Antwort“). B-1027 begrenzt die gezeigte Antwort auf die
letzte Meldung; welche Antwort die erste Meldung meinte, geht trotzdem verloren.

Naheliegend: die Übungsantwort beim Melden einfrieren wie die Testantwort (Id der `ItemReviewEvent`-Zeile) und in den Schlüssel
nehmen.
