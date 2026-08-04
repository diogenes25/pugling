---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/frontend, rolle/student]
aliases: [Birkenbihl ohne Dekodierung, Wort-für-Wort kommt nicht an]
status: geschaetzt
prio: P2
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-76 (Grill-Runde, Entscheidung 1)
---

# B-78 · Die Birkenbihl-Dekodierung erreicht das Kind nicht

Die Birkenbihl-Methode **ist** die Wort-für-Wort-Dekodierung: Ein Satz der Lernsprache steht über seiner
positionsgenauen Entschlüsselung in der Muttersprache, grammatikunabhängig. Genau diese Zuordnung kommt
beim Kind nicht an.

`BirkenbihlExerciseType.ItemsOf` baut
`new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation])`
([BuiltInExerciseTypes.cs:99](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — die
`Decoding` des Satzes ([ExerciseConfigs.cs:249](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
wird nicht gelesen. Übrig bleibt Satz → natürliche Übersetzung, also eine gewöhnliche
Übersetzungskarte. Die Methode, die dem Übungstyp den Namen gibt, findet nicht statt.

Die Übung liegt als Position im geseedeten Plan
([Seed.cs:389-390](../../backend/Pugling.Api/Data/Seed.cs), `GoalCadence.None`).

## User Story

Als **Kind** möchte ich bei einer Birkenbihl-Übung zu jedem Satz die Wort-für-Wort-Dekodierung sehen
(jedes Wort der Lernsprache mit seiner wörtlichen Übersetzung darunter, positionsgenau), damit ich die
Methode nutzen kann, für die die Übung angelegt wurde — statt einer gewöhnlichen Satz-Übersetzungskarte,
die mir die Struktur der Fremdsprache verschweigt.

## Ist-Stand am Code

Der Defekt zieht sich durch die **gesamte Kette** vom Übungstyp bis zur Oberfläche, jede Station belegt:

1. **Der Übungstyp liest die Dekodierung nicht.**
   `BirkenbihlExerciseType.ItemsOf`
   ([BuiltInExerciseTypes.cs:117-122](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) baut je
   Satz `new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation])` — vier
   Argumente, keins davon `s.Decoding`. Die Struktur existiert im Vertrag:
   `BirkenbihlSentence(int SentenceId, string LearningSentence, string NaturalTranslation, List<WordPair> Decoding)`
   ([ExerciseConfigs.cs:249](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)), `WordPair(int WordId,
   string LearningWord, string? Gloss, int? VocabularyId, string? Self)`
   ([ExerciseConfigs.cs:257-258](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) — sie wird
   nur nie gelesen.
   Der genau dafür geschriebene Test belegt es unfreiwillig: `ExerciseContentProviderTests.cs:171-186`
   (`Birkenbihl_SatzIstPromptNatuerlicheUebersetzungIstAntwort`) füttert `ItemsOf` mit einer
   `BirkenbihlConfig`, deren Satz zwei `WordPair`s trägt, und prüft danach nur `item.Prompt`/`item.Answer` —
   die Dekodierung geht in den Testdaten unter, ohne dass eine Assertion sie vermisst.

2. **`ContentItem` hätte auch kein Feld dafür.**
   `ContentItem` ([ExerciseContentProvider.cs:21-33](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs))
   trägt `Index, Prompt, Answer, AcceptedAnswers, Hint, GapIndex, AudioUrl, Passage, ItemId, VocabularyId,
   ImageUrl, ImageAlt` — keine Liste für Wortpaare. Selbst wenn `ItemsOf` die Dekodierung läse, gäbe es
   nichts, was sie durch die typ-agnostische Projektion trüge.

3. **`PracticeCard` (der Draht zum Frontend) hätte ebenfalls kein Feld dafür.**
   `PracticeCard(int ItemIndex, int Stage, string Type, string? Prompt, string? Hint, int? AnswerLength,
   string? Reveal, IReadOnlyList<string>? Choices, string? AudioUrl, string? ImageUrl, string? ImageAlt,
   int? GapIndex, string? Passage, bool AnyOrder)`
   ([PracticeDtos.cs:48-51](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) — dieselbe Lücke wie
   bei `ContentItem`, jetzt im Vertrag. `PreviewItem`
   ([ExercisePreviewDtos.cs:17-18](../../backend/Pugling.Contracts/Creator/ExercisePreviewDtos.cs)), das der
   Vater-Testmodus nutzt, hat sie ebenso nicht.

4. **Der eine Verdrahtungspunkt für beide** ist `PositionPlayService.CardFacets`
   ([PositionPlayService.cs:140-173](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)):
   Er reicht `item.Passage` unconditional durch (kein Anti-Cheat-Fall, „das Material, nicht die Lösung“,
   Zeile 165-167) und wird sowohl von `PositionPracticeController` fürs `PracticeCard`
   ([PositionPracticeController.cs:107-108](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
   als auch von `ExercisePreviewService` fürs `PreviewItem`
   ([ExercisePreviewService.cs:111-114](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs))
   aufgerufen. Genau hier wurde für B-75 schon einmal ein Content-Feld (`Passage`) additiv durchgereicht —
   derselbe Mechanismus fehlt hier für die Dekodierung.

5. **Das Frontend hat keine Darstellung dafür.** `SohnPractice.tsx` rendert generisch `<Passage
   text={card.passage} />` als **einen Fließtext-Block**
   ([SohnPractice.tsx:231](../../frontend/src/sohn/SohnPractice.tsx),
   [Passage.tsx:12-21](../../frontend/src/components/Passage.tsx): `{text}` in einem `<div>`, kein
   Layout für Wort-Paare) und `<ClozePrompt text={card.prompt} .../>` daneben. Es gibt keine Komponente,
   die eine Liste von `{LearningWord, Gloss}`-Paaren zeilen- bzw. positionsgenau nebeneinander/untereinander
   anordnet. Eine Suche nach `wordByWord`/`autoDecode`/`vocabLinked` (den drei Capabilities, die das
   Manifest schon deklariert, [BuiltInExerciseTypes.cs:115](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs))
   im Frontend ergibt **keinen** Treffer — die Capability-Flaggen werden nirgends gelesen.

6. **Die Stufe widerspricht der eigenen Doku.** `ExerciseTypeBase.IsTypedStage`
   ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)) liefert
   unconditional `true`; `BirkenbihlExerciseType` überschreibt sie nicht (nur `Vocabulary`- und
   `Cloze`-Typ tun das). Die Karte verlangt also eine getippte Antwort (`typed = true` →
   `LetterBoxes`/Texteingabe), obwohl der Vertrag ausdrücklich sagt: „Learning happens through
   reading/listening to the decoding – the method deliberately forgoes active testing (which is why this
   type has no `/check`)“ ([ExerciseConfigs.cs:224-225](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)).
   `SupportsItemProgress` bleibt beim Basiswert `false`
   ([ExerciseTypeBase.cs:53](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)) — es gibt also
   ohnehin keinen Leitner-Kasten für diesen Typ, während die Karte heute so aussieht, als gäbe es einen.

7. **Die kreatorseitige Pflege der Dekodierung ist vollständig gebaut und ohne Abnehmer.**
   `BirkenbihlController` erzeugt/normalisiert `WordId`s
   ([ExerciseControllers.cs:420-455](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs))
   und liefert einen Vokabel-Store-Selbstlink je Wort (`ConfigForResponse`, Zeile 429-436);
   `BirkenbihlDecodingService` tokenisiert Sätze und schlägt jedes Wort im Store nach
   ([BirkenbihlDecodingService.cs:27-40](../../backend/Pugling.Api/Services/Creator/BirkenbihlDecodingService.cs)).
   Der ganze Auto-Dekodier- und Wort-Austausch-Apparat (`.../words/{wordId}`) pflegt exakt die Daten, die
   in der Ausspielung verloren gehen — heute pflegt der Vater etwas, das niemand zu sehen bekommt.

**Nicht am laufenden System nachgespielt**, aber die Belegkette ist lückenlos: Der Konstruktor-Aufruf in
Schritt 1 nimmt nachweislich nur vier Argumente, `ContentItem`/`PracticeCard`/`PreviewItem` haben
nachweislich kein Feld für Wortpaare, und das Frontend hat nachweislich keine Komponente dafür. Ein
Live-Nachspielen könnte nur bestätigen, was der Code bereits eindeutig zeigt — kein Erkenntnisgewinn, der
den Aufwand rechtfertigt.

## Die echte Lücke

Schmaler als „ein Feld fehlt“: Es ist eine **komplette Kette** (Übungstyp → `ContentItem` →
`PracticeCard`/`PreviewItem` → Frontend-Komponente), an der **jede** Station das Feld nicht kennt — plus
ein zweiter, unabhängiger Defekt an derselben Stelle: Die Stufe verlangt Tippen, obwohl der Typ laut
eigener Doku keine aktive Abfrage vorsieht. Beide Defekte zusammen ergeben das Symptom „gewöhnliche
Übersetzungskarte“: Ohne Dekodierung fehlt der Kern der Methode, mit der falschen Stufe käme die
Dekodierung neben eine Eingabeaufforderung zu stehen, die der Typ nicht haben sollte.

Nicht Teil der Lücke: der Auto-Dekodier-/Store-Abgleich selbst (Schritt 7) funktioniert und ist getestet
(`BirkenbihlDecodingServiceTests.cs`) — das Problem ist ausschließlich die fehlende Ausspiel-Seite.

## Offene Punkte

- ~~Zuerst eine Birkenbihl-Position durchspielen und die Karte ansehen.~~ → durch die Code-Kette in
  Schritt 1-5 ersetzt: Belegstärke ist hier höher als ein einzelner Testlauf, ein Nachspielen würde nur
  bestätigen, was die vier zitierten Konstruktor-/Record-Definitionen bereits beweisen.
- ~~Wie die Dekodierung auf die Karte kommt: als eigene strukturierte Liste, oder als vorformatierter
  Text?~~ → siehe Entscheidung 1.
- ~~Was die Stufe bedeutet (`IsTypedStage`).~~ → siehe Entscheidung 3.
- ~~Ob der Dekodierungs-Editor des Vaters ein Gegenstück in der Ausspielung braucht.~~ → siehe
  Entscheidung 4: ja, das **ist** der Kern dieser Story.

## Entscheidungen

1. **Die Dekodierung reist als eigene strukturierte Liste (`IReadOnlyList<WordPair>`), nicht als
   vorformatierter Fließtext.** Begründung: `WordPair` existiert bereits im Vertrag
   ([ExerciseConfigs.cs:257-258](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) und trägt
   schon `_self`/`VocabularyId` für den Store-Link — ein Fließtext („Zeile 1 Original, Zeile 2 Gloss“)
   müsste diese Information wegwerfen und bräche die positionsgenaue Ausrichtung, sobald ein Wort im
   Frontend umbricht (unterschiedliche Wortlängen zwischen Lern- und Muttersprache). Kosten: ein neues
   additives Feld auf drei C#-Records (`ContentItem`, `PracticeCard`, `PreviewItem`) statt eines reinen
   String-Zusammenbaus — mehr Code, aber die einzige Form, die die Methode tatsächlich abbildet.

2. **Verdrahtungspunkt ist `PositionPlayService.CardFacets`, dieselbe Stelle wie beim `Passage`-Feld aus
   B-75.** Begründung: Es ist bereits der eine Ort, an dem `PracticeCard` und `PreviewItem` ihre
   Content-Facetten gemeinsam beziehen ([PositionPlayService.cs:140-173](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs));
   ein zweiter, paralleler Pfad wäre genau der Fehler, den B-75 dort schon vermieden hat. Die Dekodierung
   ist **Material, keine Lösung** (wie `Passage`) und wird darum **unconditional** durchgereicht, nicht
   hinter `typed` versteckt. Kosten: ein zusätzliches Tupel-Feld in `CardFacets` plus die zwei Aufrufer
   (`PositionPracticeController.cs:107-108`, `ExercisePreviewService.cs:111-114`) müssen es abgreifen.

3. **`BirkenbihlExerciseType` überschreibt `IsTypedStage(int stage) => false`.** Begründung: Der Typ
   dokumentiert selbst „deliberately forgoes active testing“
   ([ExerciseConfigs.cs:224-225](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)); der
   geerbte Standardwert `true` ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs))
   widerspricht dem direkt und würde die frisch verdrahtete Dekodierung neben eine Tipp-Aufforderung
   stellen, die es laut Doku nicht geben soll. Ohne diese Korrektur wäre die Story nur halb behoben.
   Kosten: Die Karte wechselt von Texteingabe auf Umdrehen+Selbsteinschätzung (`Gewusst!`/`Nochmal`,
   dieselbe Mechanik wie bei den Vokabel-Anzeigestufen) — ein Verhaltenswechsel, der einen eigenen
   Regressionstest braucht (nicht nur Datenverdrahtung). **Risiko, das offen bleibt**: `Birkenbihl` bleibt
   bei `SupportsItemProgress => false` ([ExerciseTypeBase.cs:53](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs));
   die Kombination „nicht getippt + kein Item-Fortschritt“ existiert bei keinem anderen Typ (nur
   `Vocabulary`/`Cloze` sind je nicht-getippt, beide mit `SupportsItemProgress => true`). Die
   Selbsteinschätzung bewertet dann pro Karte 0 Punkte ohne Box-Bewegung (dokumentiertes Verhalten,
   `ReviewOutcome`-Summary in [PracticeDtos.cs:66-67](../../backend/Pugling.Contracts/Student/PracticeDtos.cs):
   „not Leitner-based … points fields are 0, but grading and cursor still advance“) — das ist kein Fehler,
   aber ein bisher unbespielter Pfad und gehört in den Testweg (siehe Schätzung).

4. **`PreviewItem` bekommt dasselbe additive Feld; `TestItem` bleibt außen vor.** Begründung: Der
   Vater-Testmodus soll dieselbe Ausspielung zeigen wie der echte Lauf
   (`ExercisePreviewService.cs:104`: „not a parallel one“) — das Wort-für-Wort-Bild gehört also auch in
   `ExercisePreviewModal.tsx`. `TestItem` bleibt unberührt, weil Birkenbihl keine `/check`-Oberfläche hat
   (`ExerciseCheckMode.None` mit `null, null` in der Manifest-Konstruktion,
   [BuiltInExerciseTypes.cs:113-115](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — ein
   Feld auf einem DTO zu pflegen, das dieser Typ nie befüllt, wäre totes Gewicht. Kosten: keine zusätzlichen,
   `PreviewItem` läuft über denselben `CardFacets`-Aufruf wie `PracticeCard`.

5. **Eigene, kleine Frontend-Komponente statt Erweiterung von `Passage`/`ClozePrompt`.** Begründung:
   `Passage` ist bewusst ein einzelner Fließtext-Block mit `tabIndex`/`role="group"` für *einen*
   zusammenhängenden Text ([Passage.tsx:1-11](../../frontend/src/components/Passage.tsx)) — eine Liste von
   Wort-Paaren mit eigenem Zeilenumbruch-Verhalten ist ein anderes Rendering-Problem und würde `Passage`
   mit einer Sonderform verunreinigen, die nur ein Typ braucht. Kosten: eine neue Komponente
   (`BirkenbihlDecoding.tsx` o. ä.) plus ihr eigener Test, nach demselben Muster wie `Passage.test.tsx`.

6. **Zurückgestellt: Sprachcode-Normalisierung und Homonym-Nachauflösung bleiben außerhalb dieser
   Story.** Begründung: [B-17](B-17-birkenbihl-sprachcodes.md) ist bereits eine eigene Frage zur
   Sprachcode-Normalisierung; ein zweiter, unklarer Umfang in dieser Defekt-Story würde sie unnötig
   aufblähen. Diese Story korrigiert ausschließlich, dass die schon vorhandene Dekodierung nicht
   ausgespielt wird — sie ändert nicht, *wie* die Dekodierung entsteht.

## Akzeptanzkriterien

1. Für eine Birkenbihl-Position liefert `POST .../practice-sessions/{id}/cards` (bzw. der Card-Endpunkt der
   Praxis-Session) je Karte eine Liste von Wort-Paaren (Lernwort + Gloss, positionsgenau in Satzreihenfolge),
   nicht nur Prompt/Reveal.
2. `SohnPractice` zeigt diese Wort-Paare auf der Vorderseite der Karte an (Lernsatz + Dekodierung
   zusammen), nicht nur den Fließsatz.
3. Die Karte verlangt **keine** Texteingabe mehr (Umdrehen/Selbsteinschätzung statt Eingabefeld/`LetterBoxes`);
   die natürliche Übersetzung erscheint weiterhin beim Umdrehen (`Reveal`).
4. Der Vater-Testmodus (`ExercisePreviewModal`) zeigt dieselbe Wort-für-Wort-Ansicht für eine
   Birkenbihl-Übung.
5. Ein Regressionstest, der **vor** der Änderung rot ist (Erweiterung von
   `ExerciseContentProviderTests.cs:171-186` um eine Assertion auf die Dekodierung), ist danach grün.
6. Bestehende Übungstypen (Vocabulary, Cloze, Reading, …) ändern ihr Verhalten nicht — die Änderungen an
   `CardFacets`/`ContentItem`/`PracticeCard`/`PreviewItem` sind rein additiv.

## Schätzung

**Größe: M** — vergleichbar mit B-75/B-77 (Inhalt erreicht das Kind nicht, additives Feld durch dieselbe
Kette `ContentItem → CardFacets → PracticeCard/PreviewItem → Frontend-Komponente`), zusätzlich ein
isolierter Verhaltens-Fix (`IsTypedStage`). Kein XL: die Kette ist bekannt und wurde für `Passage` (B-75)
bereits einmal denselben Weg entlang gebaut — dieses Mal ohne den dortigen Bruch (`Prompt` wird hier nicht
angefasst).

- **`wo: beides`** — Backend (Datenkette + Typverhalten) und Frontend (neue Anzeige-Komponente,
  `ExercisePreviewModal`).
- **`migration: nein`** — `BirkenbihlConfig`/`WordPair` sind bereits Teil der `ConfigJson`-Spalte
  (JSON-Spalte, kein Schema-Feld); die neuen Felder auf `ContentItem`/`PracticeCard`/`PreviewItem` sind
  reine C#-Records ohne EF-Zuordnung. Keine Migration nötig.
- **`vertragsbruch: nein`** — `PracticeCard`/`PreviewItem` bekommen ein **additives** optionales Feld;
  anders als bei B-75 (`Prompt` wurde dort nullable, das war der Bruch) wird hier kein bestehendes Feld
  verändert oder entfernt. `Pugling.Client` und die `unknown_field`-Guards (Request-seitig) sind nicht
  betroffen, da es sich um ein Response-Feld handelt.

**Risiken:**

- Die Kombination „nicht getippt + `SupportsItemProgress => false`“ ist neu (siehe Entscheidung 3) — der
  Selbsteinschätzungs-Pfad für nicht-Leitner-Karten muss in der Praxis (nicht nur laut Doc-Kommentar)
  fehlerfrei mit 0 Punkten/ohne Box-Bewegung durchlaufen.
- Zwei Aufrufer von `CardFacets` (`PositionPracticeController`, `ExercisePreviewService`) müssen beide
  angepasst werden — ein vergessener zweiter Aufrufer wäre ein halb behobener Defekt (dieselbe Klasse
  Fehler, die B-75 in seinem eigenen Ist-Stand als Fallstrick nennt).

**Angriffsplan** (Backend zuerst, API-First):

1. `ContentItem` um `Decoding` (additiv) erweitern; `BirkenbihlExerciseType.ItemsOf` befüllt es aus
   `s.Decoding`.
2. `BirkenbihlExerciseType.IsTypedStage` auf `false` überschreiben.
3. `PositionPlayService.CardFacets` gibt `item.Decoding` unconditional im Tupel zurück (wie `Passage`).
4. `PracticeCard` und `PreviewItem` bekommen das additive Feld; `PositionPracticeController` und
   `ExercisePreviewService` reichen es durch.
5. Backend-Tests: `ExerciseContentProviderTests.cs:171-186` um eine Decoding-Assertion erweitern; ein
   Integrationstest über `PositionPracticeController` (Card-Antwort enthält Decoding, `typed=false`); ggf.
   `ExerciseCheckEndpointTests.cs` gegenprüfen, dass sich an der `/check`-losen Natur nichts ändert.
6. Frontend: `npm run gen:contract` (Vertragstypen ziehen automatisch nach), neue Komponente für die
   Wort-Paar-Ansicht + `Component-Test` nach dem Muster von `Passage.test.tsx`, Einbindung in
   `SohnPractice.tsx` und `ExercisePreviewModal.tsx`.
7. `/smoke-test` gegen eine Birkenbihl-Position (Karte enthält Decoding, keine Texteingabe mehr).

**Testweg:** `ExerciseContentProviderTests` (erweiterte Assertion, Backend-Einheit) + ein neuer/erweiterter
Integrationstest in `Pugling.Api.Tests` über `PositionPracticeController` (Card-Response-Form) + ein
Vitest-Komponententest für die neue Frontend-Komponente + `/smoke-test` für den End-to-End-Check. Vor der
Abnahme: `pugling-reviewer` **und** `frontend-reviewer` (`wo: beides`).

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-76, Entscheidung 1. `prio: P2` statt P1: Die Position
  ist geseedet, trägt aber `GoalCadence.None` — sie ist keine Pflicht, und niemand verliert Münzen daran.
  Nicht vom Nutzer ausdrücklich bestätigt.
- **2026-08-03** — ausformuliert: Die Kette Übungstyp → `ContentItem` → `PracticeCard`/`PreviewItem` →
  Frontend trägt an **jeder** Station kein Feld für die Dekodierung (`BuiltInExerciseTypes.cs:117-122`,
  `ExerciseContentProvider.cs:21-33`, `PracticeDtos.cs:48-51`), und `IsTypedStage` widerspricht mit `true`
  der eigenen Typ-Doku „forgoes active testing“ (`ExerciseTypeBase.cs:38` vs. `ExerciseConfigs.cs:224-225`).
- **2026-08-03** — gegrillt: alle Offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschaetzt: Größe M, Hauptrisiko ist der neue Selbsteinschätzungs-Pfad ohne
  Item-Fortschritt (Entscheidung 3); Testweg über erweiterten `ExerciseContentProviderTests`,
  einen Integrationstest über `PositionPracticeController`, einen Frontend-Komponententest und
  `/smoke-test` (autonom getroffen, Nutzerauftrag 2026-08-04).
