---
tags: [typ/story, status/ausformuliert]
aliases: []
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-646
grund: ""
ersetzt_durch: []
---

# B-649 · Übersetzungssätze im Vokabelspeicher

Teil (c) aus der Teilung von [B-646](B-646-vokabel-schema-nachfuellen.md). Nicht in B-647/B-648 enthalten, weil
deren Ziel ohne diese Story erfüllt ist. Bis hierhin tragen die Sätze nach B-646 Entscheidung 7 die Wortart
`Phrase`.

## User Story

Als **Creator** möchte ich, dass der Vokabelspeicher Wörter und feste Wendungen der Lernsprache enthält und
keine ganzen Übungssätze mit vertauschter Sprachrichtung. **Damit** bleibt „`SourceLanguage` = Lernsprache"
(B-597) überall wahr, und Suchen und Auswertungen über den Speicher treffen nur Vokabeln.

## Ist-Stand am Code

Belegt in B-646, Abschnitt „Die 29 deutschen Einträge sind Übersetzungssätze":

- Auf der DB-Kopie vom 2026-11-17 stammen 29 Einträge mit `SourceLanguage = de` aus sechs Übersetzungsübungen
  mit `sourceLang: de`: #12, #23, #52, #62, #65, #101.
- Die Übersetzungsübung verankert jedes Satzpaar im Speicher
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:437-445`).

## Die echte Lücke

Die Übersetzungsübung nutzt den Wortspeicher als Ablage für Satzpaare. Bei `sourceLang: de` widerspricht das der
Festlegung zur Sprachrichtung (`backend/Pugling.Api/Models/VocabEntities.cs:17-23`).

## Offene Punkte

1. Sollen Übersetzungssätze überhaupt im Vokabelspeicher liegen? *Empfehlung:* nein. Die Übersetzungsübung hält
   ihre Satzpaare selbst; verankert werden nur Wörter, die in den Sätzen vorkommen (wie bei Birkenbihl, B-648).
2. Was passiert mit den 29 bestehenden Einträgen und ihren Lernständen? *Empfehlung:* erst zählen, ob
   `ItemProgress` an ihnen hängt. Ohne Lernstand werden sie entfernt, mit Lernstand bleiben sie als `Phrase`
   stehen.
3. Wie ändert sich der Vertrag der Übersetzungsübung (`vocabularyId` je Paar)? Das ist nachzusehen, bevor
   geschätzt wird.

## Verlauf

- **2026-11-17** — aus B-646 geteilt (Entscheidung 8). Stufe `ausformuliert`: Der Ist-Stand ist in B-646 belegt, die offenen Punkte sind noch nicht gegrillt.
