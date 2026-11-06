---
tags: [typ/story, status/idee, bereich/beides, rolle/creator]
aliases: [Verwendung ohne Ziel-Etappen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-406]
---

# B-409 · „Verwendung" an der Übung zeigt keine Ziel-Etappen – der Verweis in der Fachwechsel-Meldung ist darum nur halb wahr

Der deutsche Satz zu `series_subject_in_use` (B-406) verweist für „wo sie bei deinen Kindern steckt" auf den Knopf „Verwendung".
`ExerciseCatalogController.Usage` (um `:248-274`) liefert aber nur Pläne und Klassenarbeiten plus `OtherLearnersCount` – keine
Ziel-Etappen (`KeyResult`), die das Löschen ebenso sperren (`ExerciseUsageQueries.CountBlockingAsync`). Hängt eine Übung nur an einem Ziel,
zeigt „Verwendung" nichts, und das Löschen antwortet trotzdem 409 – derselbe Widerspruch, den Anmerkung 14 beheben sollte.

Dazu zwei Beleg-Mängel aus derselben Nachschau: die ersten beiden AK-Zeilen von [B-406](B-406-fachwechsel-meldung-loeschen-kann-gesperrt-sein.md)
zitieren überholte Zusicherungen (`in einem Lehrplan`, `study plan` statt `nirgends mehr benutzt`, `only while nothing uses it`), und der
Summary von `Ausweg_Loeschen_Geht_Bei_Einer_Verplanten_Uebung…` spricht noch vom „take it out of the plan first".

Zu entscheiden beim Ausformulieren: Ziel-Etappen in `UsageResponse` aufnehmen (die vollständige Antwort) oder „Ziel" aus dem Verweis nehmen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf AB, Sprint 18 (Retro Sprint 19; frischer `pugling-reviewer`, `git show 98e684d`, 🟡).
  Increment **dieses** Laufs: **Limit 2**, Lauf AB endet nach Sprint 19.
