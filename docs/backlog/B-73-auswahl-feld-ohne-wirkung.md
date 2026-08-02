---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Auswahl ohne Wirkung, Question.Choices tot]
status: idee
prio: P2
art: Defekt
quelle: B-69 (Entscheidung 4)
unverifiziert: true
---

# B-73 · Das Auswahl-Feld verspricht Multiple-Choice, das Kind bekommt Freitext

Der Editor bietet bei Lese- und Hörverstehen ein Feld „Auswahl (kommagetrennt = Multiple-Choice)"
([exerciseConfig.tsx:506](../../frontend/src/vater/exerciseConfig.tsx)), es wird als
`Question.Choices` gespeichert ([ExerciseConfigs.cs:14](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
und beim Bearbeiten wieder angezeigt (`:215`). Ausgespielt wird es nie:

- `AnswerChecking.FromQuestions` baut die Inhalte als `new ContentItem(i, q.Prompt, q.Answer, [q.Answer])`
  ([BuiltInExerciseTypes.cs:307](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — die
  Optionen fallen dabei weg.
- Nur `VocabularyExerciseType` überschreibt `Choices`
  ([VocabularyExerciseType.cs:54](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs));
  `ReadingExerciseType` und `ListeningExerciseType` erben die Basis, die `null` liefert
  ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)).

Der Creator baut also Ablenker, die nie erscheinen, und die Frage bleibt Freitext. Es gibt keine Meldung
und keinen Hinweis — nur ein Feld, das aussieht, als täte es etwas.

Gefunden beim Grillen von [B-69](B-69-wiederhol-felder-alternativen.md); jene Story stellt die
**Oberfläche** des Feldes auf Einzelfelder um und lässt die Engine ausdrücklich in Ruhe, weil das zwei
verschiedene Fehler sind.

**Zu prüfen beim Ausformulieren:** ob die Ausspielung die Optionen tragen soll (dann überschreiben
Reading/Listening `Choices`, und die Stufe muss zu Multiple-Choice passen) oder ob das Feld verschwindet;
ob im Bestand überhaupt Übungen mit gefüllten `Choices` liegen; und was der Abschlusstest tut, dessen
Stufen aus dem Fahrplan kommen.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 4.
