---
tags: [typ/story, status/idee, bereich/lernen, rolle/student]
aliases: [leere Kachel in der Wortbank, wordBank aus Leerzeichen verrät die Lösung]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend, .claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 38)
unverifiziert: false
entgangen_bei: [B-487]
grund: ""
ersetzt_durch: []
---

# B-491 · Eine Wortbank aus leeren Einträgen wird zum Pool aus reinen Lösungen

Nachschau von Sprint 38 (frischer `pugling-reviewer` an `9d648bb`; gelesen, `ClozePlayTests` 10/10, der Fehlerfall
gelesen, nicht gefahren). B-487 ergänzt fehlende Lösungen in `ClozeExerciseType.Choices` – leere Einträge der Bank
zählen dort aber als Bank und bleiben als Kachel stehen.

1. **`wordBank: [""]` oder `["  "]` bei einer Lücke `went`** (`BuiltInExerciseTypes.cs:231-232`): vor B-487 war die
   Lücke unlösbar, jetzt sieht das Kind einen leeren Knopf und `went` – die Lösung ist verraten; bei mehreren Lücken
   ist der Pool genau die Lösungen, der Fall, den B-487 für leere `distractors` im Lader ausdrücklich verbietet.
   Das Vater-Web filtert leere Einträge (`RepeatedTextFields.nonEmpty`), `AnswerChecking.ChoicesOf` ebenso – die
   Lückentext-Bank nicht, und der Lader prüft eine handgeschriebene `wordBank` gar nicht (auch `[]`/`null` gehen
   still durch; das Kind tippt dann auf der „Wortbank"-Stufe frei).

## Reste aus derselben Nachschau (🟢)

- Der KI-Creator vergleicht Ablenker mit Lösung und untereinander nur nach Groß/Klein
  (`Drafting/ClozeStrategy.cs:55-60`, `:115-117`), nicht nach der Bewertungsregel: `[went]` + `went.` oder
  `[qu'il]` + `qu’il` geben zwei richtige Kacheln.
- Die Tests aus B-487 laufen nur über die Übungskarte; Klausur und Testmodus sind nur über `CardFacets` mitgedeckt.
- Der Platzhalter einer gelöschten Speichervokabel bleibt ungetestet.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 38 im Backlog-Lauf).
