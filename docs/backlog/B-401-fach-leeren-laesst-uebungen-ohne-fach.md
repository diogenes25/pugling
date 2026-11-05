---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Fach leeren trotz Übungen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-401 · Eine Reihe mit Übungen darf ihr Fach leeren, obwohl eine Reihe ohne Fach keine Übung tragen darf

B-106 T-01 verweigert das **Anlegen** einer Übung in einer Reihe ohne Fach (`series_without_subject`). Ein `PATCH` der Reihe mit
`clearSubject: true` geht aber durch, solange keine Übung Themen oder Kategorien des alten Fachs trägt (seit B-291 geprüft) – danach
stehen Übungen in einer Reihe, die keine tragen dürfte. Zu klären beim Ausformulieren: dieselbe 409 wie B-291 (dann zählt jede Übung),
oder ist eine fachlose Reihe mit Bestand ein zulässiger Zwischenzustand?

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-291 (Lauf Z, Sprint 14; `pugling-reviewer`, 🟢). Am Code gelesen, nicht gemessen.
