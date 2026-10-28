---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [SohnTest Antwortfehler unsichtbar, action.message veraltet]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 2 in Lauf H, Teil 1 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
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

## User Story

Als Kind in der Klausur möchte ich sehen, wenn meine Antwort nicht ankam – statt dass der Bildschirm einfach bei der
Frage stehen bleibt.

## Ist-Stand am Code

`frontend/src/sohn/SohnTest.tsx:121` (vor dem Fix): `if (!ok && action.message) setError(action.message.text);` –
`action` aus dem Render des Klicks, `message` dort `null`. Gemessen (siehe Verlauf), nicht nur gelesen.

## Akzeptanzkriterien

1. Scheitert das Beantworten, zeigt der Bildschirm die Meldung – schon beim ersten Mal.
2. In der englischen Arcade ist es der englische Satz.
3. Ein von der Sperre verworfener Doppelklick zeigt nichts.

## Entscheidungen

1. Der Fehler wird im `catch` innerhalb von `action.run` gesetzt und weitergeworfen (Vorschlag des Prüfers). Kosten:
   `action.message` hält ihn zusätzlich, ungerendert – harmlos.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch. Testweg: Komponententest `SohnTest.test.tsx` (Bildschirm mit
gemocktem `api`, Vorbild `MyObjectives.test.tsx`).

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu B-305 in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)),
  Fund GELB 3 des frischen `frontend-reviewer`. P2: das Kind trifft es im Pflichtweg (Klausur), und es sieht dann nichts.
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert, geschätzt (XS) und gebaut. **Gemessen:** der neue Fall
  „zeigt den ersten Fehlschlag“ → **1 failed** gegen den Ausgangsstand (Zeile 57, keine Meldung). Mutanten „kein
  `setError` im Fang“ und „kein Weiterrücken“ (Kontrollfall Erfolgsweg) → je **1 failed**. **Review-Fund 🟡 (gezählt):**
  der Fall nahm einen Fehler ohne Code, für den deutscher und Arcade-Formatierer dasselbe sagen – Mutante „deutscher
  `errorMessage` im Fang“ blieb grün → Fall in der englischen Arcade mit `test_attempts_exhausted`, Mutante **1 failed**.
  Kein weiteres `!ok && action.message`-Muster im Frontend (Suche). Vitest **555/555**, Backend **1000/1000**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt.
  Den englischen Fehlerweg im Browser zeigt keine Spec (er braucht einen scheiternden Server). **`abgenommen`.**
- **2026-10-28** — Commit `f96d358` (Lauf H, Teil 2).
