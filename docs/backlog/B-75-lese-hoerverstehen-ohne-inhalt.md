---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Trägertext erreicht das Kind nicht, Hörverstehen ohne Audio]
status: idee
prio: P1
art: Defekt
quelle: B-73 (Grill-Runde, Entscheidung 1)
unverifiziert: true
---

# B-75 · Lese- und Hörverstehen kommen ohne ihren Inhalt beim Kind an

Zwei der zwölf Übungstypen liefern beim Spielen nur die **Frage** aus — der Text, zu dem sie gehört, und
die Aufnahme, über die sie geht, kommen nicht mit. Das Kind sieht „Where is B from?" und sonst nichts.

Belegt ist das am Vertrag und an den Aufrufwegen, **nicht** am laufenden System (das ist der erste Schritt
beim Ausformulieren):

- Die Karte hat gar kein Feld dafür: `PracticeCard` trägt `Prompt`, `Hint`, `AnswerLength`, `Reveal`,
  `Choices`, `AudioUrl`, `ImageUrl`, `ImageAlt`
  ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) — für einen
  Trägertext ist kein Platz vorgesehen.
- `Prompt` ist die Frage, nicht der Text: `AnswerChecking.FromQuestions` baut
  `new ContentItem(i, q.Prompt, q.Answer, [q.Answer])`
  ([BuiltInExerciseTypes.cs:307](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)).
  `ReadingConfig.Text` ([ExerciseConfigs.cs:60](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
  wird dabei nicht gelesen.
- Auch das Audio fehlt: Die Audio-Quelle einer Karte kommt aus `StageFacets`
  ([ExerciseTypeBase.cs:41](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs), Vorgabe
  `(null, null, null)`), und `ListeningExerciseType` überschreibt es nicht (`:25-35`) — `ListeningConfig.AudioUrl`
  (`ExerciseConfigs.cs:104`) erreicht die Karte nie. Das Transkript ist als Ersatz ausgeschlossen: Es ist
  laut Vertrag „for the creator only, never for the child (anti-cheat)" (`:105`).
- Nachgeladen wird nichts: [SohnPractice.tsx](../../frontend/src/sohn/SohnPractice.tsx) ruft nur
  `startSession`, `cards`, `review` und `heartbeat` — die Übung selbst nie.

**Ein Begriffs-Widerspruch steckt mit drin.** Der Vertrag nennt die Fragen „the **graded** content atoms
of the exercise" (`ExerciseConfigs.cs:61`, `:107`), das Manifest derselben Typen sagt
`ExerciseCheckMode.None` — „pure content exercise, no automatic check"
(`BuiltInExerciseTypes.cs:10,18`). Beide Sätze können nicht stimmen. Welcher gilt, entscheidet mit, wie
diese Typen überhaupt gespielt werden sollen.

Gefunden beim Grillen von [B-73](B-73-auswahl-feld-ohne-wirkung.md) (Antwortmöglichkeiten kommen
ebenfalls nicht an). Jene Story wartet ausdrücklich auf diese hier: Aufgabenformen zu einer Frage, deren
Text fehlt, wären Politur an etwas Kaputtem — und beide Reparaturen fassen dieselben Stellen an.

**Zu prüfen beim Ausformulieren:** zuerst eine Lese-Position am laufenden System durchspielen, statt es
aus dem Code zu schließen; ob im Bestand überhaupt solche Positionen liegen; ob der Trägertext ein neues
Feld auf `PracticeCard` braucht (Vertragsbruch) oder in den `Prompt` gehört; und was der Testmodus des
Vaters heute zeigt — er läuft über denselben `ItemsOf`-Weg.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-73. P1 vom Nutzer gesetzt: Zwei von zwölf
  Übungstypen sind beim Kind nicht bloß ungenau, sondern inhaltsleer.
