# Backlog: drei Vokabellern-Ideen (gegen den Code gegrillt)

**Datum:** 2026-07-30  ·  **Moderation:** PM
**Teilnehmer:** Vater (PO) · Entwickler
**Ziel:** Drei Feature-Ideen fürs Vokabellernen als schätzbare Backlog-Stories festhalten. Recherche und
Rückfragen liefen **gegen den echten Code**, nicht gegen die Doku – damit die offenen Punkte reale Lücken
treffen und nicht Dinge hinterfragen, die längst existieren. Nichts davon ist umgesetzt.

> Ergebnis der Grill-Runde: **zehn Entscheidungen**, **fünf zuvor offene Punkte erledigt**, **eine
> Vorab-Entscheidung revidiert**, **drei Funde außerhalb der Stories** – darunter ein Defekt, der heute
> schon wirkt.

Vier Scoping-Entscheidungen standen vorab fest:

- Lückensatz-Bild: **ein Bild je einzelner Vokabel/Lücke**, keine Gesamt-Szene über mehrere Vokabeln.
- Lückensatz-Lernen: **eigene Position** auf Basis des bestehenden Cloze-Typs, nicht in die
  Vokabel-Lernschleife eingewoben.
- Buchstaben-Tausch: **neue Stufe** im bestehenden Vokabel-Übungstyp, kein eigenständiger Mechanismus.
- Adaptiver Pool: ~~ersetzt Leitner~~ → **revidiert**, siehe Entscheidung 8.

---

## Runde 1 — Idee 1: Lückensätze mit Bild als Vokabel-Vertiefung

**User Story:** Als Vater möchte ich Lückensätze anlegen, die eine gelernte Vokabel in einem Beispielsatz
zeigen („The ___ banana"), damit mein Sohn das Wort im Kontext statt isoliert lernt – wo sinnvoll mit einem
passenden Bild.

### Ist-Stand am Code

- Der Übungstyp existiert vollständig: `ClozeExerciseType`
  ([Exercises/BuiltInExerciseTypes.cs](../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) + Store
  `ClozeText` ([Models/ClozeEntities.cs](../backend/Pugling.Api/Models/ClozeEntities.cs)), Route
  `api/v1/creator/cloze-texts`.
- Eine Lücke verweist per `Gap.VocabKey` bereits auf den Vokabel-Store; die Lösung kommt live aus
  `Vocabulary.Word` ([Contracts/Exercise/ExerciseConfigs.cs](../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)).
- Bild↔Vokabel ist **n:m** und existiert: `MediaLink.VocabularyId`
  ([Models/MediaEntities.cs](../backend/Pugling.Api/Models/MediaEntities.cs)).
- **Das Frontend ist fertig.** Es gibt keine Cloze-Komponente – `sohn/SohnPractice.tsx:199` rendert
  `card.imageUrl` generisch, typ- und stufenagnostisch („das Frontend rendert nur, was da ist"). Liefert der
  Server ein Bild, erscheint es ohne eine Zeile Frontend-Arbeit.
- `ContentItem` ([Services/Shared/ExerciseContentProvider.cs:17-28](../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs))
  trägt `GapIndex` und `ItemId` als Geschwister-Felder; `ImageUrl`/`ImageAlt` hängen an keinem von beiden.
- Anti-Cheat-Stufen passen: `StageMechanics.IsTyped(ClozeStage)` zählt nur `TranslationFreeText` und
  `FreeText` als getippt – bebilderbar sind `WordBank` und `TranslationWordBank`.

### Die echte Lücke

Nicht „`ResolveClozeRefsAsync` ruft den `MediaSelector` nicht auf" (stimmt, wäre aber trivial), sondern:
**der `MediaSelector` kann keine Vokabel-Batches.** Sein einziger Batch-Einstieg ist
`SelectForItemsAsync(childId, (ItemId, VocabularyId)[])`
([Services/Shared/MediaSelector.cs:55-91](../backend/Pugling.Api/Services/Shared/MediaSelector.cs)) – er
**verlangt** eine `ExerciseItem.Id`, schlüsselt sein Ergebnis darüber, und seine Genauigkeits-Kaskade („hat
das Item eigene Bilder, zählt ausschließlich diese Menge") ist item-zentriert. Cloze-Lücken haben **kein
`ExerciseItem`**, sie leben in der `ConfigJson`. Die Annahme „fast eine Kopie von
`ResolveVocabularyItemsAsync`" trägt deshalb nicht.

Immerhin: `ChildMediaPick` kennt `VocabularyId` **oder** `ExerciseItemId` als Träger, und
`ReshuffleAsync(childId, int? vocabularyId, int? exerciseItemId, …)` bedient beide – nur der Batch-Pfad fehlt.

### Entscheidungen

1. **Träger der Bildwahl ist die Vokabel** (`ChildMediaPick.VocabularyId`). Folge: Das Kind sieht im
   Lückensatz dasselbe Motiv wie auf der Karteikarte – gewollt, Bildkonstanz *ist* der Merkeffekt. Kosten:
   ein vokabel-basierter Batch-Pfad im `MediaSelector`. Keine Migration, keine Modelländerung.
2. **Der Abschlusstest bleibt bildlos** – auch auf nicht-getippten Stufen, bewusst.
3. Folglich wird der Vertrag ehrlich: `childId` fliegt aus dem Test-Pfad (4 `ItemsOfAsync`-Aufrufe in
   `PositionTestsController`), `imageUrl`/`imageAlt` fliegen aus `TestItem` und dem Contract-Record. Ein
   Feld, das immer `null` ist, ist genau die stille Lüge, gegen die das Projekt sonst mit
   `unknown_field`-Guards kämpft. Behebt nebenbei **Fund 1**.
4. **Jede Lücke mit `VocabKey`** bekommt ein Bild, sofern eines hinterlegt ist – keine zusätzliche
   Wortart-Regel. Die Bildzuordnung ist bereits kuratiert; hängt an „yellow" kein Motiv, gibt es keins.
   Die Kuratierung *ist* der Filter.

### Akzeptanzkriterien

1. Ein Cloze-Gap mit `VocabKey` bekommt `ImageUrl`/`ImageAlt` aus dem `MediaSelector`, sofern für die
   Vokabel ein Bild hinterlegt ist und die Stufe nicht getippt ist.
2. Kein Treffer ⇒ kein Bild (kein Notnagel).
3. Cloze-Position wie jede andere einplanbar – keine neue UI.
4. Bildkonstanz über `ChildMediaPick`, Träger = Vokabel.
5. Der Abschlusstest liefert und zeigt **kein** Bild; `TestItem` trägt die Felder nicht mehr.

### Erledigte offene Punkte

- ~~Rendert die Cloze-Frontend-Komponente ein Bild-Feld?~~ Gegenstandslos – es gibt keine Cloze-Komponente.
  **Der eingeplante Spike entfällt.**
- ~~`ContentItem`-Kompatibilität (GapIndex vs. ItemId)~~ Unproblematisch.

### Anmerkung zur Stufenwahl

`WordBank` (=1) steht im `ClozeStage`-Enum, aber **nicht** in den `StageOptions` des Typs
(`BuiltInExerciseTypes.cs:131-135` bietet nur `TranslationWordBank`, `TranslationFreeText`, `FreeText`). Der
Vater kann `WordBank` nicht wählen – in der Praxis erscheint das Bild auf **genau einer** Stufe, der
Standardstufe `TranslationWordBank`.

**Größe: M** – unverändert groß, aber mit verschobenem Schwerpunkt: Frontend ≈ 0, dafür ein neuer Batch-Pfad
im `MediaSelector` samt Freeze-Verhalten. Kein Schema-/Migrations-Bedarf.

---

## Runde 1 — Idee 2: Buchstaben-Tausch-Eingabe (Anagramm)

**User Story:** Als Sohn möchte ich eine Vokabel auch lernen, indem ich durcheinandergewürfelte Buchstaben
in die richtige Reihenfolge bringe, als Abwechslung zum Tippen und Auswählen.

### Ist-Stand am Code

- Die Übungsschleife (`sohn/SohnPractice.tsx`, `sohn/SohnTest.tsx`) ist bewusst **stage-agnostisch** – sie
  branched auf mitgelieferte Felder (`choices`, `answerLength`, `reveal`), nicht auf einen Stufen-Enum.
- `components/LetterBoxes.tsx` ist reine Einzelfeld-Eingabe mit bekannter Länge – **kein** Anagramm, und
  **ohne Obergrenze und ohne Leerzeichen-Behandlung**: ein Leerzeichen wäre ein leer zu lassendes Kästchen
  mitten im Wort. Das Mehrwort-Problem ist im Projekt nicht gelöst, nur bisher nicht aufgefallen.
- **Keine Drag-&-Drop-Library** im Projekt (`package.json` kennt weder `dnd` noch `sortable`).
- Antwortweg unverändert: `AnswerDto(ItemIndex, GivenAnswer, WasKnown)`, Grading per `AnswerGrader.Matches`.
  **`StageMechanics.Normalize` trimmt, kleinschreibt und fasst Mehrfach-Leerzeichen zusammen** –
  Groß-/Kleinschreibung der Kacheln ist grading-irrelevant.
- **Für „stabilen Zufall" gibt es bereits drei Muster:** Sitzungsreihenfolge *einfrieren*
  (`PositionPlayService:138`), *Seed speichern* und neu erzeugen (`BuiltInExerciseTypes.cs:242-251`),
  *deterministischer FNV-1a-Hash* (`MediaSelector.cs:260`, „bewusst **kein** `Random`").

### Die echte Lücke

Auf getippten Stufen kennt das Kind die Lösung nicht (`Reveal == null`) – fürs Anagramm muss der Server die
**gemischten Buchstaben selbst** mitschicken, nicht nur eine Länge wie bei `LetterBoxes`. Ein neues
Facet-Feld, kein neuer Mechanismus.

### Entscheidungen

5. **Seed je Sitzung** (Muster Rechen-Drill), *nicht* „deterministisch je Item/Kind". Begründung: die
   ursprüngliche Formulierung hätte über Wochen dieselbe Kachelanordnung erzeugt – das Kind lernt dann das
   Muster der Kacheln statt der Schreibweise. Die Anforderung, die einen Reload absichern sollte, hätte den
   Lerneffekt der Übung sabotiert.
6. **Einwortig und bis ~12 Zeichen.** Ein Item, das nicht passt, fällt **für sich** auf `LetterBoxes`
   zurück – dieselbe getippte Mechanik, dieselbe `RequireTypedTest`-Wertung, nur ohne Kacheln. Keine Übung
   wird unspielbar, egal was der Vater später an den Items ändert. Preis: eine Sitzung kann zwei
   Darstellungen zeigen.
7. **Drag & Drop bleibt der Hauptweg**, mit gleichwertiger Tastatur-Alternative daneben. Die Direktheit auf
   dem Tablet ist bei einer Kinder-App ein eigenes Argument; die neue Abhängigkeit wird in Kauf genommen.

### Akzeptanzkriterien

1. Neuer `TestStage`-Wert (z. B. `LetterScramble`) im Vokabel-Typ, im Creator wählbar.
2. `StageFacets` liefert die gemischten Buchstaben; gemischt aus einem **je Sitzung eingefrorenen Seed**.
3. Nicht geeignete Items werden für dieses Item als `LetterBoxes` ausgespielt.
4. Neue Komponente rendert die Kacheln; zusammengesetztes Wort geht wie gehabt als `GivenAnswer` – **kein**
   Änderungsbedarf am Grading.
5. Gleichwertige Tastatur-/Screenreader-Bedienung neben dem Ziehen.
6. Stufe zählt als „getippt" für `RequireTypedTest`.

### Erledigter offener Punkt

- ~~Doppelte Buchstaben („banana")~~ Kein Sonderfall. Gewertet wird der zusammengesetzte String; zwei
  gleiche Kacheln sind austauschbar, jede Anordnung, die das Wort ergibt, ist richtig.

**Größe: M** – Server-Teil klein und musterkonform; Frontend mittel wegen neuer Library **und** zweiter
Bedienart, die dieselbe Logik nochmal abbilden muss.

---

## Runde 1 — Idee 3: Adaptiver Vokabel-Pool je Position

**User Story:** Als Vater möchte ich an einer Vokabel-Position einen größeren Pool (z. B. 100 Vokabeln) mit
einer täglichen Teilmenge (z. B. 20/Tag) hinterlegen, wobei im Abschlusstest nicht gekonnte Vokabeln in den
nächsten Tag nachrücken – sodass über die Laufzeit jede Pool-Vokabel möglichst einmal korrekt im
Abschlusstest vorkommt.

### Ist-Stand am Code — der entscheidende Fund

`DueItemIndicesAsync` ([Services/Shared/PositionPlayService.cs:75-89](../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)):

```csharp
var poolSize = PoolSize(pos, items.Count);          // ItemCount begrenzt
var due = Enumerable.Range(0, poolSize)             // ← nur die ERSTEN N Items
    .Where(x => ScopeMatch(...) && (!dueOnly || !pos.UseLeitner || IsDue(x.Prog, day)));
```

`ItemCount` ist **kein Tageskontingent, sondern ein Abschneiden**: `Enumerable.Range(0, poolSize)` sieht nur
die ersten N Items, und der Fortschritt wird mit `p.ItemIndex < poolSize` gefiltert. Vokabel 21 bis 100 einer
Übung mit `ItemCount = 20` sind **nie** dran – nicht heute, nicht nächste Woche, nie.

**Die Umkehrung gilt aber auch:** „Durchgefallenes kommt wieder, Gekonntes seltener" **macht Leitner heute
schon** (`IsDue` + Box). Es fehlen exakt zwei Dinge – ein **Deckel auf die Tagesmenge** und ein **Pool, der
nicht abgeschnitten wird**. Beides sitzt in *dieser einen Methode*. Die ursprüngliche L/XL-Schätzung kam
nicht von der Mechanik, sondern von der ungeklärten Semantik drumherum.

### Entscheidungen

8. **Leitner wird erweitert, nicht ersetzt** – die Vorab-Entscheidung „Pool-Modus ersetzt Leitner" ist
   **revidiert**. Sie fiel auf der Annahme, Leitner könne das nicht; die Lücke ist schmaler. Konkret:
   `ItemCount` wird vom Abschneiden entkoppelt (= Pool), dazu ein **Tagesdeckel** auf die fällige Menge.
   „Durchgefallen kommt morgen wieder" ist Leitners Box-Rücksetzung. Kein zweites Fälligkeitskonzept – damit
   entfällt auch die befürchtete Kollision mit `SettleClosedPeriodsAsync`.
9. **„Geschafft" = einmal korrekt im Abschlusstest, mit Rückfall.** Die Vokabel rückt aus der täglichen
   Auswahl; fällt sie später durch, kommt sie zurück. **Keine Migration** – `PositionReportService` rechnet
   die Trefferquote je Item bereits aus abgeschlossenen `TestAttempt`-Ergebnissen aus, es fehlt nur die
   Abfrage an der richtigen Stelle. Verhindert „einmal richtig geraten = für immer gelernt".
10. **„Pool durch" lebt als `LearnGoal`**, nicht als Positions-Ziel. Das Positions-Ziel bleibt unangetastet
    (`GoalThreshold`, `Cadence`, `PenaltyCoins`) – es ist die **Pflicht**-Mechanik, und der Münz-Malus
    braucht eine Periode, die man reißen kann. „Pool durch bis zur Klassenarbeit" ist ein **Ergebnis**-Ziel
    mit Stichtag, und dafür existiert `LearnGoal` samt Übungs-Scope und überfällig-Semantik
    ([Models/LearnGoalEntities.cs](../backend/Pugling.Api/Models/LearnGoalEntities.cs)). Kosten: höchstens
    ein additiver `LearnGoalMetric`-Wert, weil das vorhandene `MasteredPercent` an `Box ≥ MaxBox` hängt und
    „geschafft" hier am Test festgemacht ist.

### Akzeptanzkriterien

1. `ItemCount` (oder ein Nachfolgefeld) begrenzt den **Pool**, schneidet die Item-Liste nicht mehr ab; ein
   Tagesdeckel begrenzt die **täglich ausgespielte Menge** aus den fälligen Items.
2. Zuvor durchgefallene vor noch nie gezeigten (die bestehende Strategie `WeakestFirst` deckt das
   vermutlich ab – zu prüfen).
3. Eine im Abschlusstest korrekt beantwortete Pool-Vokabel wird nicht mehr ausgespielt; fällt sie später
   durch, kehrt sie zurück.
4. Fortschrittsanzeige „X von Y geschafft" über die Laufzeit.
5. Ein `LearnGoal` auf Übungs-Scope kann „Pool durch bis Datum" verbindlich machen.

### Erledigte offene Punkte

- ~~Kollision mit `SettleClosedPeriodsAsync` bei ausgelassenen Tagen~~ Entfällt mit Entscheidung 8.
- ~~Braucht das Ziel-Modell eine Erweiterung?~~ Nein, siehe Entscheidung 10.
- ~~Neues „Mastered"-Flag oder neue Tabelle?~~ Weder noch – aus `TestAttempt` ableitbar.

**Größe: M** (vorher L/XL) – zwei Änderungen in `DueItemIndicesAsync`, eine Test-Treffer-Abfrage, ein
additiver Metrik-Wert, eine Fortschrittsanzeige. **Die separate Design-Etappe ist nicht mehr nötig.** Offen
bleibt nur, ob der Tagesdeckel ein neues Feld braucht oder `ItemCount` umgedeutet wird – das entscheidet
über eine Migration.

---

## Runde 2 — Funde außerhalb der Stories

Beim Prüfen gegen den Code aufgefallen, gehört **nicht** zu den drei Ideen:

### Fund 1 — Defekt: der Abschlusstest friert Bildwahlen ein, die er nie zeigt

`SelectForItemsAsync` **schreibt** die Bildwahl fest (`MediaSelector.cs:79-90`: `AddRange` +
`SaveFreezeAsync` → `SaveChangesAsync`) und entfernt dabei überholte Wahlen (`context.Superseded`). Der
Abschlusstest reicht `childId` durch, also friert **jeder Testlauf** die Motive des Kindes ein – für Bilder,
die `SohnTest.tsx` gar nicht rendert (die Komponente liest nur `audioUrl`). Der Test entscheidet damit still,
welches Bild das Kind später in der Übungsschleife sieht.

**Betrifft heute schon die Vokabel-Karteikarten**, unabhängig von allen drei Ideen. Entscheidung 3 behebt es
nebenbei: `childId` wird in `ResolveVocabularyItemsAsync` ausschließlich für Bilder benutzt, der Test-Pfad
kann ihn ersatzlos weglassen – Nebenwirkung weg, dazu eine Batch-Abfrage weniger je Testfrage.

### Fund 2 — Das Formular erklärt `ItemCount` falsch herum

Ein Vater, der `ItemCount = 20` an einer 100-Vokabel-Übung setzt, nimmt still 80 Vokabeln dauerhaft aus dem
Lernbetrieb. **Nachgeprüft am 2026-07-30: Das Formular warnt nicht – es behauptet das Gegenteil.**

Der Hilfetext hinter dem ⓘ am Feld „Inhalte" ([lib/fieldHelp.ts:32-36](../frontend/src/lib/fieldHelp.ts),
gesetzt in `vater/PlanPositions.tsx:167`) lautet:

> **Inhalte je Durchgang** — „Wie viele Vokabeln/Aufgaben eine Sitzung höchstens vorlegt. Leer = alle
> Inhalte der Übung. Eine kleine Zahl macht die tägliche Pflicht kurz genug, dass sie auch wirklich
> passiert."

„je Durchgang" und „eine Sitzung legt höchstens vor" beschreiben ein **rotierendes Tageskontingent** – also
genau das, was Idee 3 erst bauen soll. Der letzte Satz **ermutigt aktiv zur kleinen Zahl** und lockt den
Vater damit in die Einstellung, die den Großteil seiner Vokabeln stilllegt. `defaultItemCount`
([fieldHelp.ts:101-105](../frontend/src/lib/fieldHelp.ts), am Übungs-Formular) wiederholt den Fehler:
„Vorschlag, wie viele Inhalte eine Sitzung zeigen soll."

Damit ist das kein fehlender Hinweis, sondern ein **irreführender**: der Vater trifft eine Entscheidung,
deren Wirkung ihm das Formular falsch erklärt. Zwei Wege:

- **Sofort (XS):** beide Texte ehrlich machen – „begrenzt die Übung dauerhaft auf die ersten N Inhalte;
  die übrigen werden nie abgefragt".
- **Mit Idee 3 (dann obsolet):** sobald `ItemCount` vom Abschneiden entkoppelt ist, *stimmt* der heutige
  Text – dann beschreibt er das neue Verhalten korrekt und muss nur um den Tagesdeckel ergänzt werden.

### Fund 3 — Kleinigkeit: Cloze-Vorschau zeigt nie ein Bild

`PreviewStage` des Cloze-Typs ist `TranslationFreeText`, also eine getippte Stufe. Die Vater-Vorschau bekäme
das in Idee 1 ergänzte Bild nie zu sehen. Kein Fehler, aber beim Umsetzen mitdenken – sonst hält der Vater
das Feature für kaputt.

---

## PM-Synthese & Priorisierung

| Prio | Item | Größe | Wo | Status |
|---|---|---|---|---|
| P1 | **Fund 1**: Test friert unsichtbare Bildwahlen ein | S | `PositionTestsController`, `TestItem` | Roadmap |
| P2 | **Fund 2**: `fieldHelp` erklärt `ItemCount` falsch herum | XS | `lib/fieldHelp.ts` | Roadmap |
| P3 | **Idee 1**: Lückensätze mit Bild | M | `MediaSelector`, `ExerciseContentResolver` | Roadmap |
| P4 | **Idee 3**: adaptiver Pool (Leitner-Erweiterung) | M | `PositionPlayService`, `LearnGoal` | Roadmap |
| P5 | **Idee 2**: Buchstaben-Tausch | M | Vokabel-Typ + neue Frontend-Komponente | Roadmap |
| P6 | **Fund 3**: Cloze-`PreviewStage` bebilderbar machen | XS | `BuiltInExerciseTypes.cs` | Roadmap |

Nichts davon ist umgesetzt – reine Ideensammlung, kein Commit.

## Offene Roadmap

1. **Fund 1 zuerst.** Er wirkt heute, verfälscht die Bildwahl echter Kinder und ist mit Entscheidung 3 fast
   kostenlos zu beheben.
2. **Fund 2 direkt danach.** Zwei Sätze in `fieldHelp.ts`, aber der heutige Text lädt den Vater aktiv dazu
   ein, den Großteil seiner Vokabeln stillzulegen – solange Idee 3 nicht gebaut ist, ist das die
   schädlichste Zeile im Vater-Web gemessen an ihrem Aufwand.
3. **Idee 1** – kleinster verbleibender Rest, sobald Fund 1 den Test-Pfad ohnehin angefasst hat.
4. **Idee 3** – größter Lerneffekt fürs Kind; einzige verbleibende Vorfrage ist neues Feld vs. Umdeutung
   von `ItemCount` (entscheidet über eine Migration). Macht Fund 2 nachträglich gegenstandslos, weil der
   heutige Hilfetext dann *stimmt*.
5. **Idee 2** – zuletzt, weil als einzige mit einer neuen Abhängigkeit verbunden.
6. **Fund 3** – Kleinkram, bei Idee 1 nebenbei mitnehmen.

Alle drei Ideen sind nach dem Grillen **M** und musterkonform anschlussfähig. Idee 3 ist damit **kein**
eigenständiges Architektur-Thema mehr und braucht keine vorgeschaltete Design-Etappe (anders als noch im
ursprünglichen Entwurf angenommen).
