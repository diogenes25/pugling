---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Birkenbihl prüft VocabularyId nicht]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-387 · Eine Birkenbihl-Übung speichert eine unbekannte `vocabularyId` an einem dekodierten Wort ohne Prüfung

Der `BirkenbihlController` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, ab `:462`) überschreibt weder
`ValidateConfigAsync` noch `NormalizeConfigAsync`: `POST …/birkenbihl` mit `sentences[0].decoding[0].vocabularyId = 987654` → `201`,
und `_self` zeigt ins Leere. Der Wort-`PATCH` desselben Felds (`:580-586`) antwortet dagegen `400 invalid_reference` – zwei Wege
zum selben Feld, zwei Antworten. Dieselbe Klasse wie [B-386](B-386-uebersetzung-speichert-unbekannte-vokabel-id.md) (Übersetzung).
Beim Ausformulieren zu prüfen: ob die Löschsperre im Vokabelspeicher Birkenbihl-Verweise kennt (B-386 ergänzt nur Übersetzungen),
und ob der CRUD-Weg wie der Wort-`PATCH` auch das Sprachpaar prüfen soll.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`,
  `git show f4cf656`, 🟡). Außerhalb des Diffs von B-329 – der Weg antwortete nie `validation_error`, sondern nahm die Id still an –,
  darum ohne `entgangen_bei`, wie B-386.
