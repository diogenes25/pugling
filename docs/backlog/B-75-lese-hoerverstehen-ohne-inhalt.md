---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/frontend, rolle/student]
aliases: [Lesetext erreicht das Kind nicht, Hörverstehen ohne Audio]
status: abgenommen
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-73 (Grill-Runde, Entscheidung 1)
nachgeschaut: "2026-08-07"
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

## Schätzung

**M (oberes Ende) · beides · keine Migration · Vertragsbruch ja.**

Der Bruch ist E3 und nur E3: `PracticeCard.Prompt` wird nullable. Alles andere ist additiv — `Passage` auf
vier Records, `AudioUrl` an ein Feld, das es schon gibt.

Kein Schema, keine Migration: Alles liegt in der `ConfigJson` oder im Laufzeit-Record `ContentItem`.

Rund 20 Bearbeitungsstellen, in derselben Größenordnung wie [B-76](B-76-lueckentext-karte-ohne-luecke.md)
(M, ~14) und breiter: vier Verträge statt zwei, drei Typen statt einem, dazu ein Hook, den die Runde nicht
benannt hat (R1). Zu **L** kippt es, wenn dieser Hook eine Umgestaltung von `StageFacets` erzwingt.

### Was die Schätzung an den Entscheidungen korrigiert hat

**E3s Kostenzeile nennt einen Betroffenen zu viel.** Dort steht, `Pugling.Client` ziehe nach. Tut er
nicht: `StudentApi` kennt nur Lernstands- und Medien-Endpunkte, keinen einzigen Spielpfad
([StudentApi.cs](../../backend/Pugling.Client/StudentApi.cs) — sieben Methoden, keine davon
`practice-sessions` oder `tests`). `PracticeCard` und `TestItem` kommen im ganzen Client nicht vor. Der
Bruch trifft also `Pugling.Contracts`, das Frontend, `contract.ts` und die Beispieldateien — sonst nichts.
Die `unknown_field`-Wächter sind ebenfalls unbeteiligt: die prüfen Requests, das hier sind Antworten.

### Risiken

**R1 · E3 braucht einen Typ-Hook, den die Runde nicht benannt hat.** „Der Server lässt den `Prompt` weg,
wo die Aufnahme ihn ersetzen muss" ist keine Regel, die sich ableiten lässt: Die naheliegende Fassung
(„Audio da ⇒ kein Prompt") ist genau falsch, denn beim **Hörverstehen** müssen Aufnahme *und* Frage
ankommen — das ist Akzeptanzkriterium 2. Es unterscheidet also der Typ, nicht die Karte. Heute kann er das
nicht sagen: `StageFacets` liefert `(LetterBoxLength, AudioUrl, ImageUrl)`
([IExerciseType.cs:70](../../backend/Pugling.Api/Exercises/IExerciseType.cs)), und `Prompt` läuft ohnehin
an `CardFacets` vorbei — die Controller lesen `item.Prompt` direkt.

*Empfehlung:* ein eigener Hook `bool AudioReplacesPrompt(int stage)` (Vorgabe `false`, überschrieben nur
von `VocabularyExerciseType`), und `CardFacets` liefert den `Prompt` künftig mit. Ein viertes Tupel-Element
an `StageFacets` wäre billiger zu schreiben und teurer zu lesen — drei gleichartige Fassetten plus ein
Schalter in einem namenlosen Tupel. Nicht vom Nutzer bestätigt.

**R2 · B-76 hat den Rendering-Zweig gerade besetzt.** Beide Sohn-Ansichten geben den Prompt seit
`1125ee6` an `ClozePrompt({ text: string })`
([SohnPractice.tsx:229](../../frontend/src/sohn/SohnPractice.tsx),
[SohnTest.tsx:150](../../frontend/src/sohn/SohnTest.tsx)). Ein nullable `prompt` bricht dort zuerst den
Typecheck — gewollt, es ist die Stelle, die eine Entscheidung braucht: nichts rendern, wenn nichts da ist.
Kein Konflikt mit B-76, aber die Reihenfolge steht damit fest.

**R3 · Kein Seed, kein Nutzer betroffen.** Weder Lese- noch Hörverstehen liegen als Position im Seed
(`Seed.cs` kennt keine `ReadingConfig`/`ListeningConfig`) — anders als B-76 wirkt dieser Defekt heute an
niemandem. Das ändert die Dringlichkeit nicht (P1 steht), wohl aber die Beweisführung: Der
Regressionstest muss seine Übung selbst anlegen, und ein Live-Beleg braucht ebenfalls erst eine.

**R4 · `Question.Choices` fällt an derselben Stelle mit.** `Question(string Prompt, List<string>? Choices,
string Answer)` ([ExerciseConfigs.cs:14](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) hat
ein Auswahlfeld, und `AnswerChecking.FromQuestions` liest es nicht — dieselbe Wegwerf-Bewegung wie beim
Lesetext, ein Feld weiter. Das gehört nach [B-73](B-73-auswahl-feld-ohne-wirkung.md) (dort offener Punkt
zum Auswahl-Feld), **nicht** in diese Story: `Choices` hat mit `Choices` einen eigenen Weg, seit B-76 sogar
mit Zugriff auf die Config. Hier nur festgehalten, damit es nicht ein zweites Mal überrascht.

### Angriffsplan

Backend zuerst. Punkt 0, weil die Runde ihn offen gelassen hat.

0. **R1 entscheiden** — ohne den Hook lässt sich E3 nicht bauen.
1. **`ContentItem`** bekommt `Passage`; gefüllt wird es von `ReadingExerciseType` aus `ReadingConfig.Text`
   und von `GrammarExerciseType` aus `GrammarConfig.Instruction`. **Hörverstehen füllt es nicht** — es hat
   keinen Text, es hat eine Aufnahme, und das Transkript ist laut Vertrag für das Kind gesperrt.
   `AnswerChecking.FromQuestions` nimmt den Text als Parameter mit, es baut für Lese- **und**
   Hörverstehen (bei letzterem bleibt er leer).
2. **Hörverstehen**: `ListeningExerciseType.ItemsOf` hängt `ListeningConfig.AudioUrl` an jedes
   `ContentItem` — der einzige Weg, `StageFacets` sieht die Config nicht (offener Punkt 3). Das Transkript
   bleibt draußen.
3. **Vertrag**: `Passage` additiv auf `PracticeCard`, `TestItem`, `PreviewItem`; `Prompt` auf
   `PracticeCard` nullable (E3). `CardFacets` reicht beides durch — sie ist seit B-76 die Stelle, an der
   die Karte entsteht.
4. **Die irreführende Typ-Zeile** richtigstellen (offener Punkt 6): „pure content exercise, no automatic
   check" an `ReadingExerciseType` ([BuiltInExerciseTypes.cs:10](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs))
   — bewertet wird sehr wohl, `CheckMode` meint die Abschlusstest-Fläche.
5. **Artefakte**: `docs/openapi/v1.json`, `openapi-examples.generated.json`, `docs/api-examples/`
   (schreiben die `DocsCaptureTests` im Lauf), `npm run gen:contract`.
6. **Frontend**: ein Textblock über der Frage, in **beiden** Sohn-Ansichten; `ClozePrompt` nimmt einen
   optionalen Text (R2); der Entweder-oder-Zweig `audioUrl ? Audio : Prompt` wird zum Nebeneinander, und
   der Kommentar darüber stimmt dann wieder.

### Testweg

- **Regressionstest, vorher rot** (`Pugling.Api.Tests`, neue Klasse `ContentExercisePlayTests`): je eine
  gespielte Lese-, Hörverstehen- und Grammatik-Position, geprüft auf `passage` bzw. `audioUrl` **auf der
  Karte** — die Zusicherung, die `PositionPlayModesTests` an seiner eigenen Lese-Position fünfmal
  ausgelassen hat (`SeedReadingPositionAsync`, [:134-155](../../backend/Pugling.Api.Tests/PositionPlayModesTests.cs)).
- **Anti-Cheat, derselbe Lauf:** das Transkript kommt **nicht** auf der Karte an. Eine Zusicherung, die
  billig ist und deren Bruch teuer wäre.
- **E3:** die Vokabel-Hörstufe liefert `prompt: null`, jede andere Stufe einen Prompt.
- **Klausur:** derselbe Durchlauf über `…/tests/{attemptId}/next`, wie bei B-76.
- **Testmodus des Vaters** (E4): `ExercisePreviewTests` um `passage`/`audioUrl` erweitern — es ist derselbe
  `ItemsOf`-Weg, aber ein eigener Projektionspfad (`ExercisePreviewService:80`).
- **Frontend:** Komponententest auf den Textblock und auf „Aufnahme **und** Frage" (Vitest, Muster
  `ClozePrompt.test.tsx`).
- Kein `/smoke-test` zwingend: kein neuer Endpunkt. Ein Live-Beleg braucht wegen R3 erst eine selbst
  angelegte Übung — lohnt sich trotzdem, weil `Prompt` nullable wird und das die Sohn-Ansicht trifft.

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
- **2026-08-02** — geschätzt: **M (oberes Ende) · beides · keine Migration · Vertragsbruch ja**. Zwei
  Korrekturen an den Entscheidungen: E3s Kostenzeile nennt `Pugling.Client` als Betroffenen, der aber gar
  keinen Spielpfad kennt (`StudentApi` hat sieben Methoden, keine davon `practice-sessions`) — der Bruch
  ist kleiner als angenommen. Und E3 verlangt einen **Typ-Hook, den die Runde nicht benannt hat** (R1):
  „Audio ersetzt den Prompt" lässt sich nicht aus der Karte ableiten, weil beim Hörverstehen genau das
  Gegenteil gilt; das entscheidet der Typ, und heute kann er es nicht sagen. Empfehlung steht mit
  Begründung in der Schätzung, nicht bestätigt. Dazu R4 als Fund am Rande: `Question.Choices` fällt an
  derselben Stelle mit weg — das gehört nach B-73 und wurde bewusst **nicht** in diese Story gezogen.
- **2026-08-02** — gebaut. **R1 selbst entschieden** statt vorgelegt: Die Runde hatte die *Produkt*-Frage
  schon beantwortet (E3, der Server lässt den Prompt weg); offen war nur die Form des internen Hooks, und
  beide Varianten liefern dasselbe Verhalten. Es wurde ein eigener `IExerciseType.AudioReplacesPrompt(int
  stage)` (Vorgabe `false`, überschrieben nur von `VocabularyExerciseType`) statt eines vierten
  Tupel-Elements an `StageFacets` — drei gleichartige Fassetten plus ein Schalter in einem namenlosen Tupel
  wären billiger zu schreiben und teurer zu lesen gewesen.
  **Ein Sicherheitsnetz kam dazu, das die Schätzung nicht hatte:** Der Prompt fällt nur weg, wenn wirklich
  eine Aufnahme ankommt. Eine Vokabel ohne `PronunciationAudioUrl` auf der Hörstufe ergäbe sonst eine
  Karte, die auf beiden Wegen leer ist — nichts zu lesen und nichts zu hören. Der erste Testlauf ist genau
  darüber gestolpert, weil die Fixture kein Audio hatte; beide Fälle sind jetzt festgenagelt.
  R2 traf ein wie beschrieben: Der nullable `prompt` brach den Typecheck an genau den zwei Stellen, die
  B-76 besetzt hatte (`SohnPractice.tsx:229`, `SohnTest.tsx:150`) — `ClozePrompt` nimmt jetzt einen
  fehlenden Text an und rendert dann **nichts** statt eines leeren Kastens.
  Verifikation: **651 Backend-Tests grün** (7 neu in `ContentExercisePlayTests`, alle 7 vorher rot),
  **82 Frontend-Tests grün**, `full-flow.spec.ts` und `uebungstypen.spec.ts` grün, `/smoke-test` grün.
  Live gegen `localhost:5280`, weil es (R3) keine geseedete Position gibt:
  Lesen → `passage: "Tom goes to Brighton in July."` auf **beiden** Karten, `prompt` bleibt die Frage;
  Hören → `audioUrl` gesetzt, `passage: null`, das Transkript nirgends.
  Akzeptanzkriterium 6 war schon erfüllt: B-15 wurde am 2026-08-02 auf `Essay` eingegrenzt.
  **Offen für die Abnahme:** beide Reviewer (`wo: beides`).
- **2026-08-02** — beide Reviewer gelaufen, sieben Befunde behoben. **Der schwerste war wieder einer, den
  dieser Commit erst scharf gemacht hat:** Im Testmodus des Vaters stand der Entweder-oder-Zweig
  `audioUrl ? Audio : Frage` noch — und seit `ListeningExerciseType` seine Aufnahme an *jedes* Item hängt,
  trug jedes Item ein `audioUrl`, also lief der Frage-Zweig **nie**. Die Verständnisfrage war unsichtbar,
  drei Zeilen unter dem Kommentar, der E4 zitiert. Dazu mountete die Vorschau je Frage einen Abspieler auf
  dieselbe Quelle, die beim Öffnen alle zugleich lossprangen; die Aufnahme steht jetzt **einmal** oben.
  Weiter behoben:
  **(a)** Das Sicherheitsnetz hatte ein Loch — `audioUrl is not null` ließ den Leerstring durch, und
  `PronunciationAudioUrl` ist unvalidiertes Freitext. Jetzt `!string.IsNullOrWhiteSpace`, dazu werden
  leere Config-Strings in `ItemsOf` zu `null` normalisiert (beide Fälle mit eigenem Test).
  **(b)** Der Vorschau-Pfad war eine **zweite** Anti-Cheat-Stelle: eine handgeschriebene Kopie mit dem
  Kommentar „mirrors PositionTestsController.ToItem", die genau dann zurückfiel, als das Original das
  Verschweigen lernte. `Present` läuft jetzt durch `CardFacets`, `PreviewItem.Prompt` ist nullable.
  **(c)** Die Transkript-Zusicherung prüfte auf „Leeds" — zugleich die **Lösung**, konnte also „Transkript
  geleakt" nicht von „Antwort geleakt" unterscheiden; das `.Replace` daneben war ein No-op.
  **(d)** `AudioButton` trug ein festes `aria-label="Vokabel anhören"`, während sichtbar „Anhören" stand
  (WCAG 2.5.3) — und konnte eine minutenlange Aufnahme nur neu starten, nicht anhalten. Der sichtbare Text
  ist jetzt der Name; wo die Aufnahme *Material* neben einer Frage ist, kommen die Bedienelemente dazu.
  **(e)** Der Textblock war ein Scroll-Bereich ohne Tastaturzugang und ohne Namen — und stand dreimal von
  Hand da. Beides löst das neue Bauteil `Passage` (`tabIndex`, `role="group"`, eigener Test).
  Zwei Befunde gingen nach außen: [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) (neu, P1 — über die
  Tags ist jede fremde `ConfigJson` für ein Kind lesbar, **inklusive** der Transkripte, die diese Story
  gerade von der Karte fernhält) und zwei Nachträge an [B-73](B-73-auswahl-feld-ohne-wirkung.md)
  (`Question.Choices` und `MatchingConfig.Instruction` fallen an denselben Griffen weg).
  Der E2E hatte die Vorschau-Regression nicht sehen können, weil er nur die Grammatik ausspielte; er prüft
  jetzt auch das Hörverstehen. **Gegenprobe gemacht:** die neue Zusicherung absichtlich falsch gesetzt, Lauf
  war rot, dann zurück — sie läuft wirklich.
- **2026-08-02** — **abgenommen.** Alle sechs Akzeptanzkriterien belegt, beide Reviewer gelaufen, ihre
  Befunde behoben oder ausgelagert. Commits `dab72e3` (Bau) und der Review-Nachlauf.
  **654 Backend-Tests grün** (10 in `ContentExercisePlayTests`), **86 Frontend-Tests grün**,
  `uebungstypen.spec.ts` und `full-flow.spec.ts` grün, `/smoke-test` grün, live gegen `localhost:5280`
  belegt.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `Passage` weiterhin additiv auf den Content-/
  Karten-Records steht und `AudioReplacesPrompt` weiterhin nur auf der Audio-Stufe greift — hält
  (`PracticeDtos.cs:72,74`, `VocabularyExerciseType.cs:81`). Kein Fund.
