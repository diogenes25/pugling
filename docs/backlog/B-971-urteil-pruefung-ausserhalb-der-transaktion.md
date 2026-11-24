---
tags: [typ/story, status/idee, rolle/supervisor, bereich/training]
aliases: [Zwei Urteile gleichzeitig]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "docs/pm-sitzung-2026-11-24-k.md (pugling-reviewer, Sprint 151)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-971 · Zwei gleichzeitige Urteile über dieselbe Meldung können eine gebuchte Korrektur als „abgewiesen“ zeigen

`ExerciseFeedbackService.CheckChange` prüft „berechtigt ist endgültig“ vor der Transaktion von
`FeedbackCorrectionService.JudgeAsync`, und `ExerciseFeedback` trägt kein Concurrency-Token. Schicken Vater und Mutter
im selben Augenblick „berechtigt“ und „abgewiesen“, kann die Korrektur gebucht sein, während die Meldung „abgewiesen“
zeigt. Unwahrscheinlich (zwei Klicks in derselben Sekunde), aber die Anzeige wäre dann falsch.

Möglicher Weg: den Ausgang nach `BeginTransaction` neu lesen und erst dann prüfen. Gefunden im Review von Sprint 151
(🟢, ohne Zählung), gegen den Code noch nicht reproduziert.

## Verlauf

- **2026-11-24** — angelegt (Nachtlauf K, Sprint 151, Review-Hinweis 🟢 ohne Zählung).
