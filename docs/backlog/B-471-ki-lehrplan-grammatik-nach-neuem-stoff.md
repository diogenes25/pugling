---
tags: [typ/story, status/idee, bereich/training, rolle/creator]
aliases: [Question tags fehlen im KI-Lehrplan]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: B-463
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-471 · Die Grammatik-Teile des KI-Lehrplans nehmen die Themen alphabetisch statt den neuen Stoff zuerst

Abgespalten aus [B-463](B-463-ki-lehrplan-deckt-ein-sechstel-der-wortliste.md) (Entscheidung 4). Der `plan`-Lauf
verteilt die Grammatik-Themen der Unit reihum auf Lückentext- und Grammatik-Teile (`PlanPlanner.PartTopic`). Die
Themenliste kommt sortiert nach Bezeichnung aus `GET creator/grammar-topics`
(`GrammarTopicsController.cs:61`), also „Past progressive, Present progressive, Question tags, Simple past,
Simple present“. Bei zwei Lückentexten kommen question tags nie dran, obwohl das Profil sie als neuen Stoff der
Unit nennt; Access Unit 1 hat dafür sogar eine fertige Buch-Übung (19). Nicht in B-463 mitgenommen, weil dessen
Ziel (ganze Wortliste lernbar) ohne diesen Punkt erfüllt ist.

## Verlauf

- **2026-11-11** — angelegt (beim Ausformulieren von B-463).
