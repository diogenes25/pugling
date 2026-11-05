---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Übersetzung prüft VocabularyId nicht]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-386 · Eine Übersetzungsübung speichert eine unbekannte `vocabularyId` ohne Prüfung

Der `TranslationController` prüft `TranslationItem.VocabularyId` beim Anlegen und Ändern nicht
(`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, `ValidateConfigAsync` der Übersetzung und
`NormalizeConfigAsync`, etwa `:420-436`): eine Id, die es nicht gibt, wird still gespeichert – schlimmer als jeder der beiden
Fehlercodes, um die es in B-329 ging. Zu prüfen beim Ausformulieren: was das Spielen einer solchen Karte liefert, und ob die
Prüfung `invalid_reference` meldet wie die Vokabelübung.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-329 (Lauf U, Sprint 4, `pugling-reviewer`, 🟢); außerhalb des Diffs, darum
  eigene Story.
