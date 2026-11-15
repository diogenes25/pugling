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

# B-583 · Nach einer falschen Antwort erklärt die Übung, warum

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

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 4 des Übernahmeplans; Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)).
- **2026-11-15** — Prioritäts-Beleg von außen (sofatutor-Vergleich): sofatutors Übungen bieten nach dem Fehler eine Video-Hilfe, die an die passende Stelle des Erklärvideos springt. Das Pugling-Gegenstück wäre ein Verweis von der Erklärung auf die Stoffnotiz der Unit (`SeriesUnit.Grammar`/`VocabularyNotes`). Beim Ausformulieren das Verhältnis zu [B-529](B-529-erklaerung-nach-der-antwort.md) klären: beide beschreiben eine Erklärung nach der Antwort. Vergleich in `docs/sofatutor-vergleich-plan.md` (Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-543 auf B-583: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-557). Inhalt unverändert.
