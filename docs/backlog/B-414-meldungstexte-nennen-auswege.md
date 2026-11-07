---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Meldungen mit Auswegen]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-07.md#runde-2--review
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-414 · Bestehende Meldungstexte nennen noch Auswege – gegen die Meldungsregel vom 2026-11-07

Gefunden im Review zu B-409 (`frontend-reviewer`, 🟡); alter Text, nicht der Diff von B-409. Seit `5143e75` gilt (`pm-loop` Step 5):
eine Meldung nennt, was sperrt, und keine Auswege; eine Fläche, auf die sie zeigt, muss die Sperre zeigen. Zwei Texte in
`GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts`) zählen noch Auswege auf:

- `series_subject_in_use`: „Grammatik-Themen nimmst du … heraus", „eine Übung … kannst du nur löschen …".
- `exercise_in_use`: „Entferne sie dort zuerst." (und der Verweis, siehe [B-413](B-413-loeschsperre-unit-verweist-auf-verwendung-ohne-unit-ziele.md)).

Beim Ausformulieren zuerst per `grep` alle Einträge der Tabelle und die englischen `detail`-Texte der Sperren (`ExerciseUsageQueries.Explain`:
„Remove it there first.") gegen die Regel lesen – nicht nur diese zwei. Berührt [B-411](B-411-tor-409-codes-haben-deutschen-text.md), das
neue Texte nach derselben Regel schreibt; die Reihenfolge der beiden klärt die Schätzung.

## Verlauf

- **2026-11-07** — angelegt aus dem Review zu B-409 (Lauf AC, Sprint 20).
