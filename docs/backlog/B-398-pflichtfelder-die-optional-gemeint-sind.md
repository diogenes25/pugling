---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Pflichtfelder optional gemeint]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-398 · Zwei Felder sind im Vertrag Pflicht, im Code aber optional gemeint

- `BirkenbihlSentenceInput.NaturalTranslation` (`backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:115`) ist nicht-nullbar,
  also antwortet ein fehlendes oder `null` mit `400 … field is required`. `AddSentence` behandelt `null` aber mit `?? ""`
  (`ExerciseControllers.cs` um `:557`) – toter Code; die Absicht war offenbar „optional“. Auch die eigene Meldung für einen leeren
  `LearningSentence` erscheint bei `null` nie.
- `Gap.Answer` (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:80`) heißt in der Summary „Rückfall für Lücken ohne
  Speicher-Bezug“ – eine Lücke mit `vocabKey` und `answer: null` wird trotzdem abgewiesen; Frontend und Tests schicken `""`.

**Frage** an den Nutzer: optional machen (`string?`) oder als Pflicht dokumentieren? Beides ist ein Vertragsthema.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf X, Sprint 10 (frischer `pugling-reviewer`, 🟢). Am Code gelesen, nicht gemessen.
