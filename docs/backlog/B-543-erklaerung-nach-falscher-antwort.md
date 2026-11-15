---
tags: [typ/story, status/idee, bereich/training, rolle/student, rolle/creator]
aliases: [Erklärung nach dem Fehler, Warum falsch]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/rockettutor-vergleich-plan.md#wo-rockettutor-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-543 · Nach einer falschen Antwort erklärt die Übung, warum

RocketTutors Kern ist ein KI-Tutor, der erklärt, warum eine Antwort falsch ist. Pugling zeigt nach der
Antwort nur das erwartete Ergebnis (`Expected`). Am Item gibt es einen optionalen `Hint`
(`backend/Pugling.Api/Models/ExerciseItemEntities.cs:34`), aber keine Erklärung, die *nach* der Antwort
kommt (etwa: „ne … pas umschließt das Verb", „unregelmäßig: went, nicht goed"). Die Idee: eine kurze
Erklärung je Item oder je Übung, die der KI-Creator **offline** beim Erzeugen mitschreibt und die der
Creator vor dem Zuweisen sieht und ändern kann. Der Server gibt sie wie `Expected` erst nach der Antwort
heraus. Das hält „kein LLM im Backend" und die Lösungsfeld-Regel ein: die Erklärung ist ein Reveal, kein
Lösungsfeld vor der Antwort. Zu klären: auf welcher Ebene sie hängt (Item, Übung, Vokabel im Store), ob
sie bei richtiger Antwort auch erscheint, und wie sie sich vom `Hint` abgrenzt. Beim Ausformulieren gegen
den Code prüfen, ob einzelne Übungstypen schon etwas Ähnliches tragen.

## Verlauf

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 4 des Übernahmeplans; Sammel-Story [B-541](B-541-rockettutor-vergleich-sammel-story.md)).
