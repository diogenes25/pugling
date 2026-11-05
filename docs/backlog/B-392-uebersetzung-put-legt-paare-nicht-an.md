---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT verlinkt neue Paare nicht]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-392 · Eine geänderte Übersetzung legt neue Paare nicht im Speicher an

`ExerciseControllerBase.Update` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:454`) ruft nur
`NormalizeConfig`, nicht `NormalizeConfigAsync` – das tut nur `Create` (`:367`). Für die Übersetzung heißt das: ein Paar ohne
`vocabularyId` wird beim `PUT` unverlinkt gespeichert, `_self` bleibt `null`, und das Paar landet nie im Vokabelspeicher.

**Die Oberfläche löst das selbst aus:** `frontend/src/vater/exerciseConfig.tsx:294` setzt die Id auf `null`, sobald Quelle oder Ziel
bearbeitet wurden, mit dem Kommentar „dann muss der Server neu auflösen (er legt es an bzw. findet es)" – der Server tut es beim
`PUT` nicht. Auch die Klassen-Summary des `TranslationController` („created in the store and linked on save") stimmt beim Ändern
nicht.

Am Code bestätigt (Stand `e0fdd3b`). Beim Ausformulieren zu prüfen: ob andere Typen mit `NormalizeConfigAsync` betroffen sind, und
dass die Vokabelübung (Abgleich über `AfterSaveAsync`) unberührt bleibt.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu B-386 (Lauf W, Retro Sprint 8; frischer `pugling-reviewer`, 🟡). Älter als B-386
  (der `PUT` hat nie normalisiert), darum ohne `entgangen_bei`.
