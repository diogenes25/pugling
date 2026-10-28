---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [SohnTest Antwortfehler unsichtbar, action.message veraltet]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf G Teil 2 in Lauf H, Teil 1 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-49, B-305]
wartet_auf: ""
---

# B-320 · Scheitert das Beantworten in der Klausur, sieht das Kind keine Meldung

`frontend/src/sohn/SohnTest.tsx:121`: `if (!ok && action.message) setError(action.message.text);`. `action` ist das Objekt
aus dem Render, in dem `answerAndAdvance` entstand; `message` ist dort der **alte** Zustand – beim ersten Fehlschlag
`null`, also keine Anzeige, bei einem späteren die Meldung des vorigen. Seit [B-49](B-49-sohn-schreibpfade-useaction.md)
(Commit `78844565`) so. Der in [B-305](B-305-arcade-spielweg-auf-englisch.md) eingereichte Formatierer
(`useAction(errorMessage)`) erreicht diesen Bildschirm darum nie; B-305 AK 1 ist auf diesem Pfad nur scheinbar erfüllt.

Vorschlag des Prüfers: den Fehler im `catch` selbst setzen (`setError(errorMessage(e))`) oder `runFor` nutzen; dazu ein
Fall, der einen fehlschlagenden `answerTest` rendert.

`unverifiziert`: aus dem Code hergeleitet, nicht gemessen.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu B-305 in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)),
  Fund GELB 3 des frischen `frontend-reviewer`. P2: das Kind trifft es im Pflichtweg (Klausur), und es sieht dann nichts.
