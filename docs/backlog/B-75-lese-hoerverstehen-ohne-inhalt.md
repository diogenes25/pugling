---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/frontend, rolle/student]
aliases: [Trägertext erreicht das Kind nicht, Hörverstehen ohne Audio]
status: ausformuliert
prio: P1
art: Defekt
quelle: B-73 (Grill-Runde, Entscheidung 1)
---

# B-75 · Lese- und Hörverstehen kommen ohne ihren Inhalt beim Kind an

## User Story

Als **Kind** möchte ich beim Lese- und Hörverstehen **den Text sehen bzw. die Aufnahme hören**, zu der
die Frage gestellt wird, damit ich sie beantworten kann, statt zu raten.

## Ist-Stand am Code

**Am laufenden System durchgespielt** (Integrationstest gegen die echte Pipeline: Fach → Kapitel →
Übung anlegen, Position seeden, als Kind eine Lern-Runde starten). Das ist, was der Server ausliefert:

```json
{"itemIndex":0,"stage":2,"type":"Reading","prompt":"Where does Tom go?","hint":null,
 "answerLength":null,"reveal":null,"choices":null,"audioUrl":null,"imageUrl":null,"imageAlt":null}
```

```json
{"itemIndex":0,"stage":2,"type":"Listening","prompt":"Where is B from?","hint":null,
 "answerLength":null,"reveal":null,"choices":null,"audioUrl":null,"imageUrl":null,"imageAlt":null}
```

Der Text „Tom goes to Brighton in July." stand in der Übung, die Aufnahme-URL ebenfalls. Auf der Karte
steht nur die Frage — über `/cards` **und** über `/next`, in beiden Fällen identisch.

Warum, Stelle für Stelle:

- **Der Trägertext wird beim Aufbereiten fallengelassen.** `AnswerChecking.FromQuestions` baut je Frage
  `new ContentItem(i, q.Prompt, q.Answer, [q.Answer])`
  ([BuiltInExerciseTypes.cs:19-20, 33-34](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs));
  `ReadingConfig.Text` ([ExerciseConfigs.cs:57-59](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
  wird dabei nie gelesen.
- **Die Karte hätte auch kein Feld dafür.** `PracticeCard` trägt `Prompt`, `Hint`, `AnswerLength`,
  `Reveal`, `Choices`, `AudioUrl`, `ImageUrl`, `ImageAlt`
  ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) — für einen
  Trägertext ist nichts vorgesehen. Auch `ContentItem` hat keins
  ([ExerciseContentProvider.cs:17-28](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)).
- **Das Audio fällt an einer anderen Stelle aus.** Die Audio-Quelle einer Karte kommt aus `StageFacets`
  ([PositionPlayService.cs:113](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs) →
  [PositionPracticeController.cs:108](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs));
  die Vorgabe ist `(null, null, null)`
  ([ExerciseTypeBase.cs:41](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)), und
  `ListeningExerciseType` überschreibt sie nicht (`BuiltInExerciseTypes.cs:25-35`). Überschrieben wird sie
  einzig von `VocabularyExerciseType`
  ([:92-95](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)).
- **Das Transkript ist kein Ersatz** — es ist laut Vertrag „for the creator only, never for the child
  (anti-cheat)" (`ExerciseConfigs.cs:105`).
- **Nachgeladen wird nichts.** [SohnPractice.tsx](../../frontend/src/sohn/SohnPractice.tsx) ruft nur
  `startSession`, `cards`, `review`, `heartbeat` — nie die Übung selbst.
- **Der Vater sieht dieselbe Leere.** Sein Testmodus liefert für beide Typen
  `{"prompt":"Where does Tom go?", …, "audioUrl":null}` — kein Text, keine Aufnahme. Wer die Übung vor dem
  Zuweisen ausprobiert, bemerkt den Mangel also, kann ihn aber auch dort nicht umgehen.

Zwei Randbefunde, beide am laufenden System gemessen und beide **entgegen** der ersten Vermutung:

- **Bewertet wird sehr wohl.** Eine falsche Antwort auf eine Lese-Frage liefert
  `{"wasCorrect":false,"expected":"Brighton","awarded":0,"box":1}`. `ExerciseCheckMode.None` ist also
  **kein** Widerspruch zum „graded content atoms" im Vertrag: `CheckMode` regelt nur die
  Abschlusstest-Fläche (`StudyPlanTest`) und darüber, ob das Pflichtziel über gespielte Runden zählt
  ([ExerciseTypeManifest.cs:13-17](../../backend/Pugling.Contracts/Common/ExerciseTypeManifest.cs),
  [PositionProgressService.cs:94](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)).
  Auch der Testmodus des Vaters bewertet (`scorePercent: 100`). Irreführend ist allein die Zeile am Typ
  selbst — „pure content exercise, no automatic check" (`BuiltInExerciseTypes.cs:10`).
- **Die Stufe ist wirkungslos.** Die Position stand auf `SelfAssess` (2), die Karte kam trotzdem mit
  `reveal: null` und verlangte Freitext — `ExerciseTypeBase.IsTypedStage` gibt für diese Typen immer
  `true` zurück (`:35`). Die Umdreh-Karte gibt es für Lese- und Hörverstehen also gar nicht.

**Was die Testsuite dazu sagt: nichts.** `PositionPlayModesTests` seedet seit Längerem echte
Lese-Positionen ([:134-155](../../backend/Pugling.Api.Tests/PositionPlayModesTests.cs)) und spielt sie
fünfmal durch — geprüft wird aber nur die Pflichtziel-Logik, nie der **Inhalt** der Karte. Genau die
Fehlerklasse „Regel getestet, Grenzfall offen" aus [docs/testplan.md](../testplan.md).

## Die echte Lücke

Es sind **zwei** Lücken mit unterschiedlichem Preis, und sie werden leicht als eine gelesen:

1. **Hörverstehen ist billig zu reparieren.** `ContentItem` *hat* bereits ein `AudioUrl`-Feld, und
   `PracticeCard` auch. Es fehlt nur die Verdrahtung: `ItemsOf` müsste `AudioUrl` aus der Config an jedes
   Item hängen und `StageFacets` es durchreichen — kein Vertragsbruch, kein neues Feld.
2. **Leseverstehen braucht eine Vertragsentscheidung.** Für den Trägertext gibt es **nirgends** ein Feld.
   Er ist außerdem der einzige Inhalt, der zur *Übung* gehört und nicht zur *Karte* — er wiederholte sich
   sonst auf jeder Frage.

Dazu kommt eine Frontend-Lücke, die auch nach einer Backend-Reparatur bliebe: Die Sohn-Ansicht rendert
`card.audioUrl ? AudioButton : prompt` ([SohnPractice.tsx:226-228](../../frontend/src/sohn/SohnPractice.tsx)) —
ein **Entweder-oder**. Beim Hörverstehen braucht das Kind aber beides, Aufnahme *und* Frage. Das Feld
`Manifest.Renderer`, das dafür gedacht war, wird im handgeschriebenen Frontend an keiner Stelle gelesen
(nur im generierten `contract.ts`): die Übungs-Ausspielung hat genau eine Karten-Darstellung für alle
zwölf Typen.

## Offene Punkte

1. **Wie kommt der Trägertext zum Kind?** Neues additives Feld auf `PracticeCard` (z. B. `Passage`) oder
   in den `Prompt` gefaltet? — *Empfehlung: eigenes Feld.* `Prompt` ist die Frage; er landet in der
   Historie (`HistoryResponse`), in `PreviewItemOutcome` und in den Auswertungen. Ein Absatz Fließtext
   davor würde diese Ansichten unlesbar machen. Additiv ⇒ kein Vertragsbruch.
2. **Karte oder Runde?** Der Text gehört zur Übung, nicht zur Frage. — *Empfehlung: trotzdem auf der
   Karte.* Der Server bleibt Herr der Ausspielung, die Offline-Reserve über `/cards` funktioniert
   unverändert, und die Wiederholung kostet nur Bytes. Ein Feld auf `SessionResponse` wäre sparsamer, aber
   die Info-/Lern-/Klausur-Wege müssten es alle drei tragen.
3. **Hörverstehen: `ItemsOf` oder `StageFacets`?** Beides zusammen (`AudioUrl` ans Item, `StageFacets`
   überschrieben) ist der Weg, den `VocabularyExerciseType` schon geht. — *Empfehlung: diesem Muster
   folgen*, kein neuer Mechanismus.
4. **Was zeigt die Sohn-Ansicht beim Hörverstehen?** — *Empfehlung: Aufnahme **und** Frage.* Das
   Entweder-oder in `SohnPractice.tsx:226` stammt von der Vokabel-Hörstufe, wo das Zeigen des Wortes die
   Aufgabe zerstören würde; beim Hörverstehen ist die Frage kein Verrat.
5. **Bleibt der erzwungene Freitext?** `IsTypedStage => true` macht jede eingestellte Stufe wirkungslos.
   — *Empfehlung: hier nicht entscheiden*, das ist der Kern von
   [B-73](B-73-auswahl-feld-ohne-wirkung.md), die ausdrücklich auf diese Story wartet.
6. **Die irreführende Typ-Zeile korrigieren?** „pure content exercise, no automatic check" ist am
   Verhalten gemessen falsch. — *Empfehlung: ja, mitnehmen* — ein Satz, und er hat diese Story eine
   Runde lang in die falsche Richtung gelenkt.
7. **Vater-Testmodus mitreparieren?** Er zeigt dieselbe Leere. — *Empfehlung: ja.* Er läuft über
   denselben `ItemsOf`-Weg; für den Trägertext braucht `PreviewItem` dasselbe additive Feld. Sonst prüft
   der Vater etwas anderes, als das Kind spielt. Abzugrenzen von
   [B-15](B-15-testmodus-weitere-typen.md) (Vorschau für die *nicht* prüfbaren Typen) — hier geht es um
   zwei Typen, die belegt **doch** prüfen.

## Akzeptanzkriterien (Entwurf)

- Eine Lese-Position liefert dem Kind auf jeder Karte den Text der Übung.
- Eine Hörverstehen-Position liefert eine abspielbare Aufnahme; das Transkript bleibt draußen.
- Die Sohn-Ansicht zeigt beim Hörverstehen Aufnahme **und** Frage.
- Der Testmodus des Vaters zeigt dasselbe wie die Karte des Kindes.
- Regressionstest, der vorher rot ist: eine gespielte Lese- und Hörverstehen-Position, geprüft **auf den
  Karteninhalt** — die Zusicherung, die `PositionPlayModesTests` fünfmal ausgelassen hat.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-73. P1 vom Nutzer gesetzt: Zwei von zwölf
  Übungstypen sind beim Kind nicht bloß ungenau, sondern inhaltsleer.
- **2026-08-02** — ausformuliert. Am laufenden System durchgespielt statt aus dem Code geschlossen; das
  hat sich gelohnt: Der vermutete Vertrags-Widerspruch um `CheckMode.None` ist **keiner** (bewertet wird
  sehr wohl, `CheckMode` meint die Abschlusstest-Fläche), dafür kamen drei ungeplante Befunde dazu — die
  eingestellte Stufe ist bei diesen Typen wirkungslos, der Testmodus des Vaters zeigt dieselbe Leere, und
  die bestehenden Tests spielen Lese-Positionen fünfmal durch, ohne je in die Karte zu sehen.
