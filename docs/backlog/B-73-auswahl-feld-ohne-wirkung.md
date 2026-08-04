---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/student]
aliases: [Auswahl ohne Wirkung, Question.Choices tot]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-69 (Entscheidung 4)
---

# B-73 · Das Auswahl-Feld verspricht Multiple-Choice, das Kind bekommt Freitext

## User Story

Als **Creator** möchte ich, dass die Antwortmöglichkeiten, die ich zu einer Verständnisfrage eintrage,
beim Kind auch **ankommen** — oder dass das Feld gar nicht erst da steht, wenn die Ausspielung sie nicht
tragen kann.

## Ist-Stand am Code

### Die Kette ist an genau einer Stelle unterbrochen

> **Nachgeprüft am 2026-08-04** (Stufe `gegrillt`/`geschaetzt`): Alle Belege unten sind gegen den heutigen
> Code aktualisiert — seit der Ausformulierung sind Zeilen verschoben (u. a. durch [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md)/
> [B-76](B-76-lueckentext-karte-ohne-luecke.md)) und `PositionPlayService` ist nach
> `Services/Shared/` umgezogen. Der Kernbefund bleibt unverändert richtig; ein Detail war **falsch**
> angenommen — siehe Entscheidung E3.

| Station | Ort | Trägt die Optionen? |
| --- | --- | --- |
| Editor | [exerciseConfig.tsx:514-522](../../frontend/src/vater/exerciseConfig.tsx) | ✅ ein Feld je Möglichkeit (seit [B-69](B-69-wiederhol-felder-alternativen.md)); trägt seit dieser Story sogar einen Kommentar, der auf B-73 verweist (`:517-518`) |
| Vertrag | `Question.Choices` ([ExerciseConfigs.cs:14](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) | ✅ `List<string>?` |
| Speicherung | `ConfigJson` der Übung | ✅ unverändert |
| **Inhalts-Atome** | `AnswerChecking.FromQuestions` ([BuiltInExerciseTypes.cs:360-363](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) | ❌ **hier fallen sie weg** (Passage/AudioUrl werden inzwischen durchgereicht, `q.Choices` weiterhin nicht) |
| Karten-Facetten | `PositionPlayService.CardFacets` ([Services/Shared/PositionPlayService.cs:140-173](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)) ruft `type.Choices(…)` | ✅ würde sie durchreichen — für **jeden** Typ, unabhängig von `stage` |
| Übungstyp | `ReadingExerciseType` (`:16-30`) / `ListeningExerciseType` (`:33-56`) | ❌ erben `Choices => null` ([ExerciseTypeBase.cs:41](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)) |
| Sohn-Oberfläche | [SohnPractice.tsx:255-259](../../frontend/src/sohn/SohnPractice.tsx), [SohnTest.tsx:161-168](../../frontend/src/sohn/SohnTest.tsx) | ✅ rendert `card.choices`/`item.choices`, wenn `typed` und vorhanden |

Die Stelle, die den Inhalt verliert:

```csharp
// AnswerChecking.FromQuestions, BuiltInExerciseTypes.cs:360-363
public static IReadOnlyList<ContentItem> FromQuestions(IReadOnlyList<Question> questions,
    string? passage = null, string? audioUrl = null) =>
    [.. questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer],
        AudioUrl: Blank(audioUrl), Passage: Blank(passage)))];
```

`q.Choices` wird nicht gelesen. Und `Choices(…)` überschreibt **nur** `VocabularyExerciseType`
([VocabularyExerciseType.cs:54](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)) — Lese-
und Hörverstehen erben die Basis mit `null`.

**Beide Enden stehen also fertig da.** Das ist der wichtigste Befund: Es fehlt nicht die Oberfläche und
nicht der Vertrag, sondern die Verbindung im Übungstyp.

### Warum es zunächst wie keine Ein-Zeilen-Sache aussah — und warum es doch eine ist

Lese- und Hörverstehen sind als **reine Inhalts-Übungen** deklariert (`ReadingExerciseType.Manifest`
[BuiltInExerciseTypes.cs:21-23](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs), `ListeningExerciseType.Manifest` `:38-40`):

```csharp
ExerciseCheckMode.None, null /* PlayRoute */, null /* Method */, [...]
```

Die Ausformulierung nahm daraus an: `SelfAssess` sei die gespielte Stufe, und bei ihr werde die Lösung
aufgedeckt — Optionen anzubieten widerspräche also dem Aufdecken. **Das war beim Nachprüfen (2026-08-04)
falsch:** Weder `ReadingExerciseType` noch `ListeningExerciseType` überschreiben `IsTypedStage` — beide
erben `ExerciseTypeBase.IsTypedStage(int) => true`
([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)), und zwar **für jede
Stufe**, nicht nur für eine bestimmte. `PositionPracticeController.Review`
([Controllers/Student/PositionPracticeController.cs:306](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
prüft die Kind-Antwort für diese Typen darum schon heute serverseitig gegen `item.AcceptedAnswers`
(= `q.Answer`) — die Lösung wird nie aufgedeckt (`Reveal` bleibt `null`), unabhängig vom konfigurierten
`stage`-Wert. Der Klassenkommentar an `ReadingExerciseType` hält das inzwischen fest: „The questions ARE
graded against their solution – `ExerciseCheckMode.None` only says the type has no final-test surface, not
that nothing is checked." Details und die Konsequenz für diese Story: siehe **Entscheidung E3**.

Weiterhin richtig:

- `StageOptions` ist leer (`ExerciseTypeBase.cs:50`), es gibt für diese Typen also **keine** im
  Testmodus/Fahrplan umschaltbare Stufenauswahl — der Supervisor kann heute nicht zwischen Abfrageformen
  wählen (bleibt so, siehe E3).
- Das **Pflichtziel** der Position gilt nicht über Trefferzahl, sondern über **gespielte Runden**
  (`PositionProgressService.cs:62-113`, insb. `PlayedEnough`/`IsGoalDoneAsync`) — das ändert sich durch
  diese Story nicht; nur die einzelne Karten-Antwort wird weiterhin (wie schon heute) gegen die Lösung
  geprüft.

### Was daran nicht wehtut

- **Kein Bestand ist betroffen:** In der lokalen Entwicklungs-DB gibt es **keine einzige** gespeicherte
  `choices`-Liste. Der Fehler ist latent; er kostet heute niemanden Punkte.
- **Keine falsche Bewertung heute:** Ohne Optionen tippt das Kind frei, und die Prüfung läuft (siehe oben)
  bereits gegen `q.Answer` — anders als bei [B-65](B-65-vokabel-mehrere-uebersetzungen.md) entsteht durch
  das fehlende Multiple-Choice keine falsche Note, nur eine schwerere Eingabe als nötig.
  Der Schaden ist vergeudete Creator-Arbeit und ein Versprechen, das die Oberfläche nicht hält.

### Ein Nachbarbefund, der beim Grillen zum Hauptbefund wurde

`FromQuestions` baut den Prompt aus `q.Prompt` — der **Lesetext** (`ReadingConfig.Text`) bzw. das Audio
kommt in keinem `ContentItem` vor. Beim Grillen bestätigt: `PracticeCard` hat gar **kein Feld** für einen
Lesetext ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)), die
Audio-Quelle käme aus `StageFacets` (`ExerciseTypeBase.cs:41`, von `ListeningExerciseType` nicht
überschrieben), und `SohnPractice.tsx` lädt die Übung nie nach. Das Kind bekommt die Frage ohne den Text
und ohne die Aufnahme.

Das ist der größere Defekt und liegt jetzt als [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) mit **P1**
vor dieser Story (E1). Am *laufenden System* nachgespielt ist es weiterhin nicht — das ist der erste
Schritt beim Ausformulieren von B-75.

## Die echte Lücke

Ein Eingabefeld erhebt eine Zusage, die die Ausspielung nicht einlöst: Der Creator tippt Ablenker, das
Kind bekommt sie nie zu sehen, und niemand erfährt davon. Die Verbindung fehlt an **einer** Stelle je Typ
(ein `Choices`-Override) — anders als beim Ausformulieren angenommen, ist dafür **keine** Grundsatzfrage
mehr zu klären: Lese- und Hörverstehen sind bereits heute serverseitig geprüfte Typen (siehe oben,
Entscheidung E3), die Frage „abgefragt oder reine Inhalts-Übung" stellt sich also nicht neu, sondern ist
durch den heutigen Code längst beantwortet.

## Offene Punkte

1. ~~**Ausspielen oder Feld entfernen?**~~ → **E3.** Ausspielen, mit deutlich weniger Aufwand als befürchtet.
2. ~~**Wenn ausspielen: werden die Typen damit prüfbar (`CheckMode`)?**~~ → **E3.** Erledigt sich: sie sind
   es bereits, unabhängig von dieser Story.
3. ~~**Was, wenn nur einige Fragen Optionen haben?**~~ → **E4.** Je Item, wie ursprünglich empfohlen.
4. ~~**Der Lesetext erreicht das Kind offenbar nicht**~~ → **E1** (siehe oben) → **E2**: mit
   [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md)/[B-76](B-76-lueckentext-karte-ohne-luecke.md) inzwischen
   `abgenommen` ist die Blockade aufgehoben.
5. ~~**Braucht das Feld eine Sperre, solange nichts ausgespielt wird?**~~ → **E5.** Nein.
6. ~~**Die Zuordnung gehört dazu**~~ → **E6.** Mitnehmen, Umsetzung präzisiert.

## Entscheidungen

**E1 · Diese Story wartet; der fehlende Lesetext geht vor.** Die Grill-Runde vom 2026-08-02 hat Punkt 4
zuerst aufgerufen und ihn dabei **belegt**: `PracticeCard` hat gar kein Feld für einen Lesetext
([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)), `StageFacets` liefert
für Hörverstehen keine Audio-Quelle ([ExerciseTypeBase.cs:41](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs),
von `ListeningExerciseType` nicht überschrieben), und `SohnPractice.tsx` lädt die Übung nie nach. Das Kind
bekommt also die Frage ohne den Text und ohne die Aufnahme.

*Begründung:* Antwortmöglichkeiten zu einer Frage, deren Inhalt nicht ankommt, sind Politur an etwas
Kaputtem — und die Antwort auf die Punkte 1 bis 3 hängt daran, wie diese Typen künftig überhaupt gespielt
werden. Beide Reparaturen fassen dieselben Stellen an (`FromQuestions`, `PracticeCard`).
*Kosten:* Diese Story liegt still, obwohl sie ausformuliert ist; die Punkte 1, 2, 3 und 5 bleiben **offen**
und werden erst entschieden, wenn [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) steht. Darum bleibt die
Stufe `ausformuliert` und wird **nicht** auf `gegrillt` gesetzt.

**E2 · Die Blockade aus E1 ist aufgehoben.** [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) ist
`abgenommen` (Commits `95a5bd8`…`dab72e3`…`f233712`), [B-76](B-76-lueckentext-karte-ohne-luecke.md)
ebenso (`18b64cc`…`1125ee6`…`2581177`). Lesetext, Aufnahme und Anweisung erreichen das Kind inzwischen
(`PracticeCard`/`TestItem` tragen `Passage`, `AudioUrl` wird gereicht). *Begründung:* Die einzige
Voraussetzung, die diese Story auf `ausformuliert` hielt, ist damit erfüllt — sie kann jetzt gegrillt und
geschätzt werden. *Kosten:* keine; reine Feststellung.

**E3 · Ausspielen — und zwar ohne neue Stufe, ohne `CheckMode`-Wechsel.** Beim Nachprüfen für diese Runde
(2026-08-04) stellte sich heraus, dass die in Punkt 2 befürchtete Kollision („Optionen zeigen UND
gleichzeitig aufdecken") für `ReadingExerciseType`/`ListeningExerciseType` gar nicht bestehen kann: Beide
überschreiben `IsTypedStage` nicht und erben damit `ExerciseTypeBase.IsTypedStage(int) => true`
([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)) — für **jeden**
`stage`-Wert, nicht nur für einen ausgewählten. `PositionPracticeController.Review`
([PositionPracticeController.cs:306](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
prüft die Kind-Antwort für diese Typen darum schon **heute**, unabhängig von dieser Story, gegen
`item.AcceptedAnswers` — die Lösung wird nie aufgedeckt. Die komplette Übertragungs-Infrastruktur
(`Question.Choices` im Vertrag, `PracticeCard.Choices`/`TestItem.Choices`, `CardFacets`, das per
`type.Choices(configJson, items, item, stage)` **immer** aufruft, `SohnPractice.tsx`/`SohnTest.tsx`, die
`card.choices`/`item.choices` bereits rendern) steht fertig und wird schon von `VocabularyExerciseType`
produktiv genutzt. Es fehlt einzig ein `Choices(…)`-Override in beiden Typen, der `q.Choices` der zum
`item.Index` gehörenden Frage zurückgibt (`null`, wenn leer). Keine neue `TestStage`, kein
`IsTypedStage`-Override, kein `CheckMode`-Wechsel, kein neues Vertragsfeld.
*Begründung:* Der ursprüngliche Punkt 2 ging von einer Prämisse aus, die der Code nicht stützt — die
gründliche Nachprüfung dieser Runde hat das vor dem Schätzen aufgedeckt, nicht erst beim Bauen.
*Kosten:* eine `Choices()`-Methode je Typ (~10 Zeilen), keine Migration, kein Vertragsbruch. Der Editor-
Kommentar, der auf diese Story verweist ([exerciseConfig.tsx:517-518](../../frontend/src/vater/exerciseConfig.tsx)),
wird beim Bauen entfernt.

**E4 · Je Item, nicht je Position.** `Choices(configJson, items, item, stage)` schaut die Frage am
`item.Index` an und liefert `q.Choices`, wenn gefüllt, sonst `null` — eine Runde mit gemischten Fragen
(einige mit Optionen, einige frei) ist damit ohne Zusatzaufwand möglich, weil die Entscheidung ohnehin auf
Item-Ebene fällt statt auf Positions-/Stufen-Ebene. *Kosten:* keine über E3 hinaus.

**E5 · Keine Sperre im Editor.** Bestätigt wie vorgeschlagen: Der Hilfetext (`questionChoices`, seit B-69)
und diese Story lösen das Versprechen ein, statt es ein drittes Mal zu markieren. *Kosten:* keine.

**E6 · Die Zuordnung (`MatchingExerciseType`) wird mitgenommen, Umsetzung präzisiert.** Bestätigt am Code:
`MatchingExerciseType` überschreibt `Choices` nicht ([BuiltInExerciseTypes.cs:182-216](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs),
`ItemsOf` bei `:198-202`), `MatchStage.Distractors` ist seedbar (`Seed.cs:369-385`), wirkt aber nirgends —
der Klassenkommentar an `MatchStage` ([StudyPlanEntities.cs:12-19](../../backend/Pugling.Api/Models/StudyPlanEntities.cs))
hält das als „nur halb implementiert" fest. Umsetzung: `Choices()` liefert bei `MatchStage.Distractors`
Ablenker aus den **anderen** Paaren dieser Übung (`.Right`-Werte, dedupliziert, deterministisch rotiert nach
Index — dieselbe Technik wie `VocabularyExerciseType.Choices`/`ClozeExerciseType.Choices`), bei `Direct`
weiterhin `null`. `IsTypedStage` bleibt unverändert (Basis liefert bereits `true`, passt zum bestehenden,
schon heute typ-eigenen `Check(…)`). Kein neues Vertragsfeld: kein gepflegter Distraktor-Pool, keine
Editor-Änderung für Matching nötig — die Ablenker entstehen aus dem vorhandenen `Pairs`-Bestand.
*Kosten:* eine weitere `Choices()`-Methode (~20 Zeilen, analog zur bestehenden Vokabel-Logik) plus die
Tests dafür; kein Frontend-Editor-Feld.

## Akzeptanzkriterien

1. Eine Lese- oder Hörverstehen-Frage mit Antwortmöglichkeiten (`Question.Choices` gefüllt) wird dem Kind
   **mit** diesen Möglichkeiten ausgespielt (Übung/Leitner **und** Klausur); ein Test, der vorher rot ist,
   hält das für beide Typen fest.
2. Eine Frage **ohne** Möglichkeiten verhält sich unverändert (Freitext-Eingabe, Prüfung gegen `q.Answer`).
   Beides darf innerhalb derselben Übung gemischt vorkommen (E4).
3. Die Lösung wird zu keinem Zeitpunkt zugleich mit den Antwortmöglichkeiten aufgedeckt — für
   Lese-/Hörverstehen gilt das schon heute (`Reveal` bleibt `null`), ein Regressionstest hält es fest.
4. Der Testmodus des Vaters (`ExercisePreviewService`) zeigt dieselbe Form (inkl. Optionen) wie die
   Ausspielung beim Kind.
5. Eine Zuordnungs-Position auf `MatchStage.Distractors` liefert Ablenker aus den anderen Paaren der
   Übung; `MatchStage.Direct` bleibt ohne Optionen. Ein Test, der vorher rot ist, hält beide Fälle fest.
6. `CheckMode`/`Manifest` der betroffenen Typen bleiben unverändert (`None` bzw. `StudyPlanTest`);
   Zielerreichung („gespielte Runden" bzw. Matching-`Check`), Punkte und Malus verhalten sich unverändert.
7. Kein neues Vertragsfeld, keine Migration — `Question.Choices`, `PracticeCard.Choices`/`TestItem.Choices`
   bleiben wie sie sind.
8. Volle Suite grün; der Endpunkt-Abdeckungs-Wächter bleibt zufrieden.

## Schätzung

**Größe: S** — am Anker „`childId` aus dem Test-Pfad ziehen" (B-01): drei kleine, unabhängige
`Choices()`-Overrides (Reading, Listening, Matching) nach einem bereits produktiv genutzten Muster
(`VocabularyExerciseType.Choices`, `ClozeExerciseType.Choices`), keine neue Infrastruktur. Kein `M`, weil
die teure Grundsatzfrage (neue Stufe/`CheckMode`) sich durch E3 erledigt hat — das war der Teil, der beim
Ausformulieren nach `M` aussah.

- **`wo: beides`**, mit klarem Schwerpunkt Backend. Backend: drei `Choices()`-Methoden in
  [BuiltInExerciseTypes.cs](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs). Frontend: **ein**
  Aufräumschritt — den Kommentar an [exerciseConfig.tsx:517-518](../../frontend/src/vater/exerciseConfig.tsx)
  entfernen (er verweist genau auf diese Story) und optional den weggefallenen „= Multiple-Choice"-Zusatz
  am `questionChoices`-Hilfetext zurückgeben; `SohnPractice.tsx`/`SohnTest.tsx` brauchen **keine** Änderung
  (rendern `choices` bereits generisch).
- **`migration: nein`** — keine Schemaänderung, keine neue Spalte, kein neues Enum-Mitglied
  (`TestStage.MultipleChoice`/`MatchStage.Distractors` existieren bereits).
- **`vertragsbruch: nein`** — `Question.Choices`, `PracticeCard.Choices`, `TestItem.Choices` bleiben, wie
  sie sind; kein Feld wird hinzugefügt, geändert oder entfernt.

**Risiken:**

- Die Dedup-/Rotations-Logik für Matching-Distraktoren ist ein zweites Mal derselbe Code wie bei
  `VocabularyExerciseType.Choices`/`ClozeExerciseType.Choices` — beim Bauen prüfen, ob ein gemeinsamer
  Helfer (`AnswerChecking`/`StageMechanics`) lohnt, statt ein drittes Mal zu duplizieren (Simplify-Pass).
- `ExercisePreviewService` (Kriterium 4) läuft über denselben `CardFacets`-Pfad wie die Ausspielung
  ([ExercisePreviewService.cs:36](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)) —
  sollte also ohne Zusatzarbeit mitziehen; das ist beim Bauen zu verifizieren, nicht anzunehmen.
- Klausur-Pfad (`PositionTestsController`) nutzt `CardFacets` ebenfalls generisch — ein Regressionstest für
  `Klausur_LiefertDenTextEbenfalls`-artige Fälle sollte auch `choices` mitprüfen, damit ein künftiger
  Umbau des Testpfads die Auswahl nicht erneut verliert.

**Angriffsplan** (Backend zuerst):

1. `ReadingExerciseType.Choices(…)` und `ListeningExerciseType.Choices(…)`: Config deserialisieren,
   `Questions[item.Index].Choices` zurückgeben (leer/`null` → `null`).
2. `MatchingExerciseType.Choices(…)`: bei `MatchStage.Distractors` Ablenker aus den anderen `Pairs`
   erzeugen (dedupliziert, deterministisch rotiert), bei `Direct` `null`.
3. Integrationstests in `ContentExercisePlayTests`/`PositionPlayModesTests`-Stil: Karte, Klausur-Item und
   Preview-Item tragen `choices`, wenn konfiguriert; bleiben `null`, wenn nicht; Matching-Positionen auf
   beiden `MatchStage`-Werten.
4. Frontend: den B-73-Kommentar in `exerciseConfig.tsx` entfernen/aktualisieren (kein API-Vertragswechsel,
   daher kein Contract-Regenerat nötig).
5. `/smoke-test` für den Sohn-Durchstich (Reading-Übung mit Optionen anlegen, als Kind spielen, Optionen
   sichtbar).

**Testweg:** neue Fälle in `backend/Pugling.Api.Tests/ContentExercisePlayTests.cs` (Reading/Listening,
analog zu den bestehenden B-75-Fällen) und in `PositionPlayModesTests.cs` bzw. einer neuen Matching-
Testklasse für `MatchStage.Distractors`; `ExercisePreviewService`-Abdeckung existiert bereits und wird um
den Choices-Fall ergänzt. Kein neuer E2E nötig (kein Frontend-Verhalten ändert sich funktional, nur
Backend-Content); der bestehende `uebungstypen.spec.ts` (der die „Leeds, York, Hull"-Auswahl bereits anlegt)
bleibt als Beleg, dass der Editor unverändert funktioniert. Volle Suite (`dotnet test Pugling.sln -c
Release`) und der Endpunkt-Abdeckungs-Wächter laufen wie immer am Ende mit.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 4.
- **2026-08-02** — ausformuliert am Code. Der Befund ist bestätigt und zugleich entschärft: Die Kette ist
  an **einer** Stelle unterbrochen (`FromQuestions` liest `q.Choices` nicht, und der Typ überschreibt
  `Choices` nicht) — beide Enden, Editor und Sohn-Oberfläche, stehen fertig da. Entschärft, weil Lese- und
  Hörverstehen `CheckMode.None` tragen: Es wird nichts falsch bewertet, und im Bestand liegt **keine
  einzige** gespeicherte `choices`-Liste. Dafür ist die Reparatur keine Zeile: `SelfAssess` deckt die
  Lösung auf, `StageOptions` ist leer — Optionen anzubieten verlangt eine Aussage darüber, wie diese Typen
  abgefragt werden. Nebenbei aufgefallen und **nicht belegt**: Der Trägertext scheint das Kind gar nicht
  zu erreichen (Punkt 4).
- **2026-08-02** — gegrillt, aber **bewusst nicht abgeschlossen**. Die Runde begann mit Punkt 4, weil er
  die Antwort auf Punkt 1 verschieben konnte — und genau das ist eingetreten: Der Befund ist am Vertrag
  belegt (`PracticeCard` hat kein Feld für den Trägertext, das Audio fehlt ebenso), also wiegt er
  schwerer als das Auswahl-Feld. Entschieden wurde deshalb **eine** Sache: Diese Story wartet, der
  fehlende Inhalt wird [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) und geht mit **P1** vor. Die
  übrigen vier Punkte bleiben offen; die Stufe bleibt `ausformuliert`, weil `gegrillt` verlangt, dass
  jeder Punkt entschieden oder ausdrücklich zurückgestellt ist.
- **2026-08-02** — ein sechster offener Punkt aus der Grill-Runde zu
  [B-76](B-76-lueckentext-karte-ohne-luecke.md): Die **Zuordnung** hat dasselbe Muster — `MatchStage`
  kennt eine Stufe `Distractors`, aber beide Stufen liefern am laufenden System identische Karten mit
  `choices: null`. Sie wurde dieser Story zugeschlagen, weil es derselbe Griff an derselben Stelle ist.
  Im Rumpf ist außerdem „Trägertext" durch „Lesetext" ersetzt: Das Wort bezeichnet im Repo den
  Store-Eintrag `ClozeText` (B-76, Entscheidung 3). Die Einträge oberhalb bleiben, wie sie geschrieben
  wurden.
- **2026-08-02** — zwei Nachträge aus dem Review zu [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md), beide
  am **selben Griff** wie der bestehende Zuordnungs-Punkt und darum hier statt in einer neuen Story:
  `Question.Choices` ([ExerciseConfigs.cs:14](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
  wird von `AnswerChecking.FromQuestions` nicht gelesen — Lese- und Hörverstehen können also
  Antwortmöglichkeiten führen, die das Kind nie sieht (der E2E legt genau das an: „Leeds, York, Hull").
  Und `MatchingConfig.Instruction` wird von `ItemsOf` verworfen
  ([BuiltInExerciseTypes.cs:200](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — dieselbe
  Reparatur wie E1 in B-75, das Feld `Passage` steht seit `dab72e3` bereit. Beides ist ein Weg über
  `IExerciseType.Choices` bzw. `ContentItem.Passage`; keiner braucht eine neue Naht.
- **2026-08-04** — gegrillt (autonom getroffen, Nutzerauftrag): Die Blockade aus E1 ist aufgehoben —
  [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) und [B-76](B-76-lueckentext-karte-ohne-luecke.md) sind
  inzwischen `abgenommen`. Alle `Datei:Zeile`-Belege gegen den heutigen Code nachgeprüft und korrigiert
  (u. a. ist `PositionPlayService` nach `Services/Shared/` umgezogen, `FromQuestions` liegt jetzt bei
  Zeile 360). Dabei ein wesentlicher Korrekturfund (E3): Die in Punkt 2 vermutete Kollision zwischen
  Multiple-Choice und Selbsteinschätzungs-Aufdeckung besteht für Lese-/Hörverstehen nicht, weil beide Typen
  `IsTypedStage` nie überschreiben und darum **immer** typgeprüft sind (Basisklasse liefert `true`) — die
  Lösung wird nie aufgedeckt, unabhängig vom `stage`-Wert. Damit erledigen sich Punkt 1 und 2 zusammen ohne
  neue Stufe, ohne `IsTypedStage`-Änderung und ohne `CheckMode`-Wechsel; Punkt 3 (E4) und Punkt 5 (E5) wie
  vorgeschlagen entschieden; Punkt 6/Zuordnung (E6) mitgenommen und mit der bereits im Repo etablierten
  Distraktor-Technik (`VocabularyExerciseType`/`ClozeExerciseType`) präzisiert. Alle sechs Punkte sind damit
  entschieden, Akzeptanzkriterien final.
- **2026-08-04** — geschätzt (autonom getroffen, Nutzerauftrag): `groesse: S` (drei kleine, unabhängige
  `Choices()`-Overrides nach etabliertem Muster — kein `M`, weil die vermutete Grundsatzfrage aus Punkt 2
  sich beim Nachprüfen erledigt hat), `wo: beides` (Backend-Schwerpunkt, ein Aufräumschritt im
  Editor-Kommentar), `migration: nein`, `vertragsbruch: nein`. Angriffsplan, Risiken und Testweg ergänzt;
  Status auf `geschaetzt`.
- **2026-08-04** — **gebaut** (Stufe `in-arbeit`). Gegen den Angriffsplan, mit einer Abweichung: Schritt 1
  und 2 brauchten **keine** drei eigenen Implementierungen, weil die Vokabel-Logik und die für Matching
  geplante identisch sind. Der Ablenker-Pool liegt darum als **ein** Helfer
  `StageMechanics.DistractorPool(items, item, maxDistractors = 3)`
  ([StageMechanics.cs](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs)); `VocabularyExerciseType.Choices`
  ist darauf zurückgeführt (–24 Zeilen) und `MatchingExerciseType.Choices` ist ein Einzeiler dagegen — das
  war das im Risiko benannte „drittes Mal duplizieren", und es ist nicht eingetreten. Lese-/Hörverstehen
  gehen über `AnswerChecking.ChoicesOf(questions, item)`, das die Liste des Autors **unverändert und in
  seiner Reihenfolge** liefert (bewusst keine Rotation: bei je eigener Liste je Frage gibt es keine
  Korrelation über Items zu brechen, aber eine gewollte Ordnung zu zerstören) und Leerzeilen des
  Wiederhol-Feldes verwirft.
  - **Vorher rot, nachher grün:** neue Klasse
    [ExerciseChoicesTests.cs](../../backend/Pugling.Api.Tests/ExerciseChoicesTests.cs) (9 Fälle). Mit
    weggenommener Produktionsänderung (`git stash` der vier `.cs`) **8 von 9 rot**; grün blieb genau
    `Zuordnung_OhneAblenker_BleibtOhneOptionen` — der Wächter für das *unveränderte* Verhalten, der vorher
    und nachher grün sein muss.
  - **Volle Suite:** `dotnet test Pugling.sln -c Release` → **696/696 grün** (vorher 687), 0 Warnungen;
    Endpunkt-Abdeckungs-Wächter zufrieden. Frontend: `tsc -b` ohne Befund, `vitest run` 110/110.
  - **Durchstich am laufenden Server** (`/smoke-test`-Ablauf, Wegwerf-DB, Port 5280): 13/13 Standard-Checks
    grün, danach als **Kind** gespielt (`auth/child`, eigene Session) — die Lesefrage kam mit
    `choices=['Leeds','York','Hull']` und `reveal=null`, die Freitext-Frage derselben Übung mit
    `choices=null`, die Zuordnung auf `Distractors` mit den drei Gegenstücken und sichtbar rotierter
    Reihenfolge (`dog,cat,mouse` / `dog,mouse,cat` / `cat,mouse,dog`). Die echte `pugling.db` blieb
    unangetastet.
  - **Frontend:** der B-73-Kommentar in [exerciseConfig.tsx](../../frontend/src/vater/exerciseConfig.tsx) ist
    weg; der Hilfetext `questionChoices` sagt jetzt wieder, dass das Kind die Möglichkeiten zur Auswahl
    bekommt — und das stimmt ab jetzt.
  - **Nebenwirkung dokumentiert:** Der Klassenkommentar an `MatchStage`
    ([StudyPlanEntities.cs](../../backend/Pugling.Api/Models/StudyPlanEntities.cs)) behauptete „nur halb
    implementiert, kein Code verzweigt auf dieses Enum" — das wäre ab jetzt falsch und ist nachgezogen,
    einschließlich des **verbleibenden** Halbstands: `StageOptions` bleibt leer, es gibt also keinen
    Stufen-Wähler, der `Distractors` anbietet. Der Wert kommt aus dem Seed oder aus einem ausdrücklichen
    `stage` im Request, nicht aus der Oberfläche des Supervisors. Das ist **kein** Akzeptanzkriterium dieser
    Story (E6 schließt eine Editor-Änderung für Matching aus) — aber der Weg dorthin fehlt, und das gehört
    benannt statt stillschweigend geliefert.

  **Was für `abgenommen` noch fehlt:** die beiden Reviewer (`wo: beides` ⇒ `pugling-reviewer` **und**
  `frontend-reviewer`) und der Commit. Beides ist bewusst nicht in diesem Durchgang passiert.
- **2026-08-04** — **abgenommen**, nach beiden Reviews. Sie haben zusammen **einen Blocker** gefunden, den
  der Bau übersehen hatte, und zwei Fallen derselben Familie, die diese Story selbst aufgestellt hätte.

  **Der Blocker (Frontend-Review, rot):** Der Lehrplan-Assistent schreibt seine Vokabel-Stufennummer auf
  **jede** Position (`wizardFinish.ts`, `{ ...input.position, exerciseId }`), und zwei seiner drei Ziele
  setzen `stage: 2`. Mit `MatchStage.Distractors = 2` verwandelte diese Story also „Rückstand aufholen" +
  eine Zuordnung stillschweigend in Multiple-Choice — während der Testmodus des Vaters weiter Freitext
  zeigte, weil `MatchingExerciseType.PreviewStage = Direct` ist. Das verletzte **Akzeptanzkriterium 4** und
  hätte die Bestehensquote einer laufenden Klausur verändert, ohne dass irgendwo etwas anders aussieht.
  Behoben mit demselben Gate, das `VaterExerciseCreate`/`ExerciseEditModal` schon tragen: die Stufe geht nur
  an `type === "Vocabulary"`, sonst entscheidet der Server (Übungs-, dann Typ-Vorgabe). Das Feld sagt jetzt
  auch „(nur Vokabelübungen)" dran, statt still zu wirken — genau die Krankheit, die diese Story kuriert.
  Zwei Fälle in `wizardFinish.test.ts` halten die Weiche.
  - **Ein-Element-Pool (beide Reviews, unabhängig gefunden):** Überlebte kein Ablenker, lieferte
    `DistractorPool` `[item.Answer]` — **eine** Option, und sie ist die Lösung. Über Matching neu
    erreichbar (eine Zuordnung mit *einem* Paar, oder mit durchgehend gleicher rechter Spalte): das Kind
    tippt den einzigen Knopf und besteht mit 100 %. Jetzt `null` bei leerem Ablenker-Pool ⇒ Rückfall auf
    Freitext. Belegt durch eine Theory mit beiden Fällen plus einen Unit-Fall.
  - **Auswahl ohne die richtige Antwort (Frontend-Review):** Bisher folgenlos, weil die Optionen verworfen
    wurden; ab jetzt bekommt das Kind nur diese Knöpfe und **kein** Eingabefeld — jede Antwort falsch,
    Leitner-Kasten stehend, Malus greifend. Der Editor warnt jetzt an der Zeile (Hinweis, **keine** Sperre —
    E5), verglichen wird wie am Server (`StageMechanics.Normalize`-Nachbau).
  - **Anti-Cheat strukturell statt zufällig (Backend-Review):** `CardFacets` fragte die Optionen
    unabhängig von `typed` ab; dass Lösung und Auswahl nie zusammen ausgingen, ergab sich nur aus drei
    unabhängigen Typen. Jetzt `typed ? type.Choices(…) : null` — heute wirkungsgleich, künftig ein Riegel.
  - **Doppelklick auf der neuen Fläche:** `judge` in `SohnPractice` hatte kein Ref-Gate und die Knöpfe kein
    `disabled` (der Klausur-Pfad macht es richtig). Bei *einer* „Prüfen"-Taste war das eine
    Doppelklick-Frage; bei drei Knöpfen nebeneinander ist der Fehlgriff der Normalfall — zwei Tipper
    schickten zwei `review` **und** zwei `next()`, der Zähler sprang von 1/5 auf 3/5 und Karte 2 kam nie.
    Jetzt `useRef`-Sperre plus `disabled={busy}` (die B-49-Regel, hier auf der Fläche eingelöst, die diese
    Story neu erreichbar macht).
  - **A11y der neuen Knopfreihe:** `role="group"` + `aria-label="Antwortmöglichkeiten"` in allen drei
    Renderpfaden (Üben, Klausur, Vorschau) — ein Screenreader las „Leeds, Schaltfläche" ohne Bezug zur
    Frage. Dazu die fehlende `.btn:focus-visible`-Regel (vorbestehend und repo-weit): mit `--ink` statt des
    sonst üblichen `--cyan`, weil ein cyaner Ring auf dem cyanen Vollknopf genau dort unsichtbar wäre, wo er
    nötig ist. Und der React-Key trägt jetzt den Index mit, weil derselbe Optionstext zweimal eintragbar ist
    und `aria-pressed` in der Vorschau sonst beide Knöpfe als gewählt markierte.
  - **Doku-Befund (Backend-Review):** Mein `MatchStage`-Kommentar zählte die Wege zur Stufe unvollständig
    auf. Es sind **vier**, in der Reihenfolge von `StageForDay`: `StageSchedule` → `PlanPosition.Stage` →
    `Exercise.DefaultStage` → Typ-Vorgabe; die ersten drei sind per Request setzbar und **keiner** wird
    gegen `StageOptions` geprüft (→ B-79). Nachgetragen, samt dem Satz, dass eine „2" aus der Vokabel-Skala
    eine Zuordnung umschaltet — genau der Blocker oben.
  - **Testlücken geschlossen:** Ein-Paar-Fall, doppelte rechte Spalte, Hörverstehen in Klausur **und**
    Vorschau (vorher nur Reading belegt), plus zwei Unit-Fälle für die Schutzklauseln (leere Antwort,
    `item.Index` außerhalb der Fragenliste). Im E2E `uebungstypen.spec.ts` nagelt jetzt eine Zeile fest,
    dass die drei Möglichkeiten, die der Spec seit je anlegt, in der Vorschau **ankommen** — das war die
    Lücke: der Spec legte sie an, und niemand prüfte, ob sie je ausgespielt werden.

  **Verifikation nach den Fixes:** `dotnet test Pugling.sln -c Release` **702/702 grün** (von 687 über 696),
  0 Warnungen; `dotnet format --verify-no-changes` ohne Befund; Frontend `tsc -b` clean, `vitest` **111/111**;
  `npx playwright test e2e/uebungstypen.spec.ts` grün (51 s); `markdownlint-cli2` 0 Befunde.
  Reviewer: `pugling-reviewer` (Rückführung zeichenweise gegen das Original verglichen, verhaltensgleich)
  und `frontend-reviewer`.

  **Bewusst nicht mitgenommen** (beide vorbestehend, keiner von dieser Story verursacht): die Stufen-Pille in
  `SohnTest.tsx` ist vokabelspezifisch und beschriftet eine Zuordnungs-Klausur auf `Distractors` als
  „Selbstcheck" — der saubere Weg ist der Anzeigename aus dem Manifest und gehört zu
  [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md). Und `StageOptions` bleibt für Matching leer,
  es gibt also weiter keinen Stufen-Wähler (E6 schließt das aus).
