---
tags: [typ/story, status/gegrillt, bereich/backend, bereich/frontend, rolle/student]
aliases: [Lesetext erreicht das Kind nicht, Hörverstehen ohne Audio]
status: gegrillt
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

- **Der Lesetext wird beim Aufbereiten fallengelassen.** `AnswerChecking.FromQuestions` baut je Frage
  `new ContentItem(i, q.Prompt, q.Answer, [q.Answer])`
  ([BuiltInExerciseTypes.cs:19-20, 33-34](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs));
  `ReadingConfig.Text` ([ExerciseConfigs.cs:57-59](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
  wird dabei nie gelesen.
- **Die Karte hätte auch kein Feld dafür.** `PracticeCard` trägt `Prompt`, `Hint`, `AnswerLength`,
  `Reveal`, `Choices`, `AudioUrl`, `ImageUrl`, `ImageAlt`
  ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) — für einen
  Lesetext ist nichts vorgesehen. Auch `ContentItem` hat keins
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
2. **Leseverstehen braucht eine Vertragsentscheidung.** Für den Lesetext gibt es **nirgends** ein Feld.
   Er ist außerdem der einzige Inhalt, der zur *Übung* gehört und nicht zur *Karte* — er wiederholte sich
   sonst auf jeder Frage.

Dazu kommt eine Frontend-Lücke, die auch nach einer Backend-Reparatur bliebe: Die Sohn-Ansicht rendert
`card.audioUrl ? AudioButton : prompt` ([SohnPractice.tsx:226-228](../../frontend/src/sohn/SohnPractice.tsx)) —
ein **Entweder-oder**. Beim Hörverstehen braucht das Kind aber beides, Aufnahme *und* Frage. Das Feld
`Manifest.Renderer`, das dafür gedacht war, wird im handgeschriebenen Frontend an keiner Stelle gelesen
(nur im generierten `contract.ts`): die Übungs-Ausspielung hat genau eine Karten-Darstellung für alle
zwölf Typen.

## Offene Punkte

1. ~~**Wie kommt der Lesetext zum Kind?**~~ → **E1**
2. ~~**Karte oder Runde?**~~ → **E1**
3. ~~**Hörverstehen: `ItemsOf` oder `StageFacets`?**~~ — keine Entscheidung, sondern ein Faktum:
   `StageFacets(item, stage)` bekommt die Config gar nicht zu sehen
   ([ExerciseTypeBase.cs:41](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)). Die Aufnahme
   **muss** also über `ContentItem.AudioUrl` gehen, so wie beim Vokabel-Typ. Es gibt keinen zweiten Weg.
4. ~~**Was zeigt die Sohn-Ansicht beim Hörverstehen?**~~ → **E3**
5. **Bleibt der erzwungene Freitext?** `IsTypedStage => true` macht jede eingestellte Stufe wirkungslos.
   — **Ausdrücklich zurückgestellt:** das ist der Kern von
   [B-73](B-73-auswahl-feld-ohne-wirkung.md), die auf diese Story wartet. Hier zu entscheiden hieße, jener
   Story ihre einzige Frage wegzunehmen.
6. ~~**Die irreführende Typ-Zeile korrigieren?**~~ — ja, ohne Gegenrede: ein Satz Doku, der diese Story
   eine Runde lang in die falsche Richtung gelenkt hat. Er wird mit der Reparatur richtiggestellt.
7. ~~**Vater-Testmodus mitreparieren?**~~ → **E4**

## Entscheidungen

Aus der Grill-Runde vom 2026-08-02. Zwei Befunde aus der Runde selbst stehen darin: der Lückentext taugt
**nicht** als Vorbild (E2), und das Frontend trägt heute eine Anti-Cheat-Regel, die dem Server gehört (E3).

### E1 · Der Text kommt als eigenes Feld auf die Karte, nicht in den `Prompt`

`PracticeCard`, `TestItem` und `PreviewItem` bekommen ein additives `Passage`; `ContentItem` trägt es
mit. In der Oberfläche heißt es schlicht „Text".

*Begründung.* Der `Prompt` ist die **Frage** und wird als solche weiterverwendet — in `ItemOutcome`, in
`PreviewItemOutcome`, in der Historie. Ein Absatz Fließtext davor macht jede dieser Zeilen unlesbar, und
zwar in jeder Zeile neu. Der naheliegende Gegenvorschlag — falten wie beim Lückentext, der seinen ganzen
Text als `Prompt` führt (`BuiltInExerciseTypes.cs:118`) — fällt weg, weil dieser Präzedenzfall in der
Runde selbst als defekt entlarvt wurde (siehe E2). Beim Lückentext *ist* der Text außerdem die Aufgabe;
beim Leseverstehen sind es zwei Dinge, und ein Feld kann nur eines tragen.

*Kosten.* Vier Felder (`ContentItem`, `PracticeCard`, `TestItem`, `PreviewItem`) und ein Zweig in der
Sohn-Ansicht. Der Text wiederholt sich auf jeder Frage — bewusst: der Server bleibt Herr der Ausspielung,
und die Offline-Reserve über `/cards` funktioniert unverändert. Die Variante „einmal je Runde" wäre
sparsamer, müsste aber von drei Wegen (Info/Lern/Klausur) **plus** `/cards` getragen werden.
Additiv ⇒ für sich genommen **kein** Vertragsbruch.

**Die Grammatik kommt mit** (nachgetragen aus der Grill-Runde zu
[B-76](B-76-lueckentext-karte-ohne-luecke.md), Entscheidung 1). `GrammarConfig.Instruction` — „die
übergreifende Anweisung, etwa *Setze das Verb ins Simple Past*" — wird von `ItemsOf` verworfen
([BuiltInExerciseTypes.cs:64](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)); am laufenden
System kam die Karte nur mit „He (go) to school.". Das ist derselbe Defekt wie beim Lesetext, nur milder
(der Einzel-Prompt trägt für sich) — und **dieselbe** Reparatur: übungsweiter Text auf der Karte, also
E1. Kein neuer offener Punkt, nur ein dritter Typ, der dasselbe Feld füllt.

Der Begriff dafür ist übrigens **nicht** „Trägertext": Das Wort ist im Repo schon der Store-Eintrag
`ClozeText` (Entscheidung 3 in B-76). Diese Story sagt darum „Lesetext" bzw. „Text der Übung"; das Feld
heißt `Passage`.

### E2 · Der Lückentext wird eine eigene Story und geht vor

Der Befund aus der Runde ist [B-76](B-76-lueckentext-karte-ohne-luecke.md), P1.

*Begründung.* Beim Suchen nach einem Vorbild stellte sich heraus, dass der Lückentext denselben Defekt
hat, nur gefährlicher: zwei Lücken liefern zweimal dieselbe Karte, die Wortbank kommt nicht an, und die
Vorlagensyntax `{{1}}` steht sichtbar im Text. Er liegt außerdem als Position **im Seed** — er wirkt
heute, Lese- und Hörverstehen nicht.

*Kosten.* Zwei Vertragsänderungen statt einer. Dafür bleibt keine der beiden Stories größer als L, und
sie widersprechen sich nicht: E1 ist additiv, ein Feld für den Text und eines für die Lückennummer
vertragen sich. B-75 **wartet nicht** auf B-76 — nur die Bau-Reihenfolge steht fest.

### E3 · Der Server lässt den `Prompt` weg, wo die Aufnahme ihn ersetzen muss

Auf der Vokabel-Hörstufe liefert die Karte künftig keinen `Prompt` mehr. Das Frontend rendert schlicht,
was da ist — Aufnahme und Frage nebeneinander, wo beides ankommt.

*Begründung.* Heute schickt die Karte auf dieser Stufe **beides**, und `SohnPractice.tsx:226` versteckt
den Prompt von sich aus. Damit liegt eine Anti-Cheat-Entscheidung im Frontend — direkt unter einem
Kommentar, der das Gegenteil behauptet („das Frontend rendert nur, was da ist"). Ein Zweig auf
`card.type` würde diese Regel dort festschreiben und jeden künftigen Audio-Typ zwingen, sich im Frontend
einzutragen.

*Kosten.* `PracticeCard.Prompt` wird nullable — das ist ein **Vertragsbruch**: `Pugling.Client`, die
Frontend-Typen und die Beispieldateien ziehen nach. Damit steht B-75 auf `vertragsbruch: ja`.

### E4 · Der Testmodus des Vaters wird mitrepariert, B-15 auf ihren Rest eingegrenzt

`PreviewItem` bekommt dasselbe Feld wie die Karte. Zusätzlich wird
[B-15](B-15-testmodus-weitere-typen.md) richtiggestellt.

*Begründung.* Der Testmodus läuft über denselben `ItemsOf`-Weg und zeigt belegt dieselbe Leere. Wer die
Übung vor dem Zuweisen ausprobiert, muss sehen, was sein Kind spielt — sonst ist die Vorschau eine
Beruhigung statt einer Prüfung. Nebenbei fiel auf, dass B-15 („Vorschau für die nicht-prüfbaren Typen")
fünf Typen nennt und bei vieren falsch liegt: Reading, Listening, Grammar und Birkenbihl bauen alle
Inhalts-Atome und haben damit eine Vorschau — Reading und Listening werden dort sogar **bewertet**
(`scorePercent: 100` im Lauf). Ohne Vorschau ist einzig **Essay** (`ItemsOf` gibt `[]` zurück,
`ExercisePreviewService:30,49` steigt bei leer aus).

*Kosten.* Ein Vertragsfeld mehr und eine fremde Story angefasst. Der Gegenwert: B-15 schrumpft von fünf
Typen auf einen, und eine widerlegte Behauptung bleibt nicht als Arbeitsvorrat stehen.

## Akzeptanzkriterien

1. Eine Lese-Position liefert dem Kind auf jeder Karte den Text der Übung — in `passage`, nicht im
   `prompt`.
2. Eine Hörverstehen-Position liefert eine abspielbare Aufnahme **und** die Frage; das Transkript bleibt
   draußen.
3. Auf der Vokabel-Hörstufe kommt kein `prompt` mehr an, und die Sohn-Ansicht enthält keine
   typabhängige Verzweigung mehr.
4. Der Testmodus des Vaters zeigt dieselben Inhalte wie die Karte des Kindes.
5. Regressionstest, der vorher rot ist: eine gespielte Lese- und Hörverstehen-Position, geprüft **auf den
   Karteninhalt** — die Zusicherung, die `PositionPlayModesTests` fünfmal ausgelassen hat.
6. B-15 nennt nur noch die Typen, die belegt keine Vorschau haben.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-73. P1 vom Nutzer gesetzt: Zwei von zwölf
  Übungstypen sind beim Kind nicht bloß ungenau, sondern inhaltsleer.
- **2026-08-02** — ausformuliert. Am laufenden System durchgespielt statt aus dem Code geschlossen; das
  hat sich gelohnt: Der vermutete Vertrags-Widerspruch um `CheckMode.None` ist **keiner** (bewertet wird
  sehr wohl, `CheckMode` meint die Abschlusstest-Fläche), dafür kamen drei ungeplante Befunde dazu — die
  eingestellte Stufe ist bei diesen Typen wirkungslos, der Testmodus des Vaters zeigt dieselbe Leere, und
  die bestehenden Tests spielen Lese-Positionen fünfmal durch, ohne je in die Karte zu sehen.
- **2026-08-02** — gegrillt, vier Entscheidungen. Die Suche nach einem Vorbild hat die Runde zweimal
  gedreht: Der Lückentext führt seinen Text schon im `Prompt` — sprach also zunächst *gegen* ein eigenes
  Feld —, bis das Nachspielen zeigte, dass er selbst kaputt ist (zwei Lücken, zweimal dieselbe Karte;
  daraus wurde [B-76](B-76-lueckentext-karte-ohne-luecke.md), P1, mit Vorrang). Und die Frage „was zeigt
  die Sohn-Ansicht?" entpuppte sich als Zuständigkeitsfrage: Das Frontend versteckt heute den Prompt der
  Vokabel-Hörstufe selbst — eine Anti-Cheat-Regel im Renderer, direkt unter dem Kommentar, der das
  Gegenteil behauptet. Sie wandert zum Server, und dafür wird `Prompt` nullable: aus `vertragsbruch`
  wird damit **ja**. Punkt 5 bleibt ausdrücklich bei B-73.
- **2026-08-02** — nachgetragen aus der Grill-Runde zu
  [B-76](B-76-lueckentext-karte-ohne-luecke.md): Die **Grammatik** kommt unter E1 dazu (ihre übergreifende
  Anweisung fällt genauso weg wie der Lesetext, gleiche Reparatur), und der Begriff **„Trägertext" ist
  hier durchgehend falsch** — er bezeichnet im Repo den Store-Eintrag `ClozeText`. Die Prosa sagt jetzt
  „Lesetext", das Feld heißt `Passage`. Die Stufe bleibt `gegrillt`: kein neuer offener Punkt, ein dritter
  Typ am selben Feld und eine Wortkorrektur.
