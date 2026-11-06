---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Gespielte Übung sperrt Fachwechsel dauerhaft]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-407 · Eine gespielte Übung mit einer Art des alten Fachs sperrt den Fachwechsel ihrer Reihe dauerhaft

Seit B-291 sperrt eine Übung mit einer Kategorie („Art") des alten Fachs den Fachwechsel der Reihe. Die Art ist im Bearbeiten-Dialog nicht
änderbar, also bleibt nur Löschen – und das ist gesperrt, solange die Übung benutzt wird (`exercise_in_use`). Ist ihre Lehrplan-Position
schon gespielt, lässt sie sich nicht mehr entfernen (`position_has_data`), steckt sie im Plan eines fremden Kindes, kommt der Creator gar
nicht heran. Dann kann die Reihe ihr Fach **nie** mehr wechseln.

**Frage:** Ist das hinnehmbar (ein Fachwechsel einer benutzten Reihe ist selten und eher ein Fehler), oder braucht es einen Ausweg – etwa
die Art im Bearbeiten-Dialog änderbar bzw. entfernbar machen (dann gilt wieder die Regel, dass ein `PUT` die Kategorie gegen das Fach der
Reihe prüft)?

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-406 (Lauf AB, Sprint 18; `pugling-reviewer`, 🔴 am Satz – der versprach einen Ausweg, den
  es in diesen Fällen nicht gibt; der Satz ist berichtigt, die Grenze selbst ist eine Produktfrage). Am Code gelesen, nicht gemessen.
