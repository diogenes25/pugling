---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/student, lerntechnik/vokabeln, lerntechnik/birkenbihl]
aliases: [Vokabel im Satz, Satzkarte]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Chat mit dem Vater, 2026-11-17"
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-650 · Vokabel im Satz: Karte mit Birkenbihl-Zeile, nur die Zielvokabel ist offen

Heute wird jede Vokabel isoliert abgefragt (laufen ↔ run). Der Vater ist mit der Kombi-Lernkarte
([B-598](B-598-kombi-lernkarte-aufdecken-abschreiben-erinnern.md), [B-643](B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md))
zufrieden, und sie soll bleiben. Manche Wörter merkt sich das Kind aber isoliert schlecht. Diese Wörter soll es
zusätzlich in einem kurzen Satz üben, ähnlich wie im Schultest. Die Karte zeigt den Satz in der Lernsprache. Die
Zielvokabel ist darin eine Lücke. Darunter steht die Wort-für-Wort-Übersetzung nach Birkenbihl, in der alle Wörter
außer der Zielvokabel übersetzt sind. Wo es passt, kommt ein Bild dazu. Beispiel für die Zielvokabel *home*:

```text
Tom runs ____ .
Tom rennt heim.
```

Das Kind schreibt *home*. Der Kontext trägt die Bedeutung, und die Birkenbihl-Zeile sorgt dafür, dass der Satz keine
zweite Hürde ist. Gefragt ist also nur das eine Wort.

## User Story

Als **Vater** möchte ich, dass mein Kind schwer zu merkende Vokabeln in einem kurzen Satz mit Birkenbihl-Zeile übt,
in dem nur die Zielvokabel offen ist. **Damit** verankert es das Wort im Zusammenhang, so wie es im Schultest
abgefragt wird, und nicht nur als Wortpaar.

## Ist-Stand am Code

Die Bausteine existieren einzeln, nur nicht zusammen:

1. **Lernkarte ist reine Vokabel-Mechanik.** `TestStage.LearnCard = 8` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:60`),
   `VocabularyExerciseType.IsLearnCardStage` und `IntroductionStage`
   (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:76-79`). Die Satzkarte wäre dort keine Stufe, sondern
   etwas, das auf die Vokabel *verweist*: ein Wort-Atom hat keinen Satz (Punkt 4).
2. **Ein Lückensatz mit Vokabelbezug gibt es schon (Cloze).** `Gap(Index, Answer, Alternatives, VocabKey, Form,
   Distractors, Prompt)` (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:109`); `VocabKey` bindet die Lücke an
   den Wortspeicher und damit an den Lernfortschritt, `Form` ist die vom Satz verlangte gebeugte Form (*runs* zu *run*,
   Kommentar ebd. Zeilen 91-97), `Prompt` die deutsche Aufgabe der Lücke (B-633). `ClozeExerciseType.ItemsOf` macht je
   Lücke ein Atom (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:231-240`); die Stufen reichen von
   Wortkasten bis freiem Text, dazu Buchstabenkästchen, Buchstaben-Kacheln und Auswahl (`ClozeEntities.cs:7-39`).
   Frage 4 (gebeugte Form) ist damit schon beantwortet, die Lösung steht je Lücke in `Form`.
3. **Die Birkenbihl-Zeile gibt es nur im Birkenbihl-Typ.** `ContentItem.Decoding`
   (`backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs:56`) setzt allein `BirkenbihlExerciseType.ItemsOf`
   (`BuiltInExerciseTypes.cs:152-159`); `ClozeExerciseType.ItemsOf` setzt es nicht (`:231-240`). Ausgespielt wird es
   **ohne Bedingung** (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:541-543`), und der Birkenbihl-Typ
   begründet das ausdrücklich damit, dass er nie getippt wird (`BuiltInExerciseTypes.cs:162-171`: wer eine Stufe dieses
   Typs tippen lässt, muss dort entscheiden, ob die Dekodierung mitreisen darf). Auf einer getippten Cloze-Karte würde
   die Zeile die Lösung mitliefern, wenn die Glosse der Zielvokabel darin steht. Das Frontend rendert sie schon
   (`frontend/src/sohn/Lektion.tsx:119`, `frontend/src/components/BirkenbihlDecoding.tsx`).
4. **Kein Satz an der Vokabel.** `Vocabulary` hat kein Satzfeld und keine Verknüpfung zu einem Satz
   (`backend/Pugling.Api/Models/VocabEntities.cs:13-64`, bereits am 2026-11-17 geprüft). Eine Verknüpfung Vokabel → Satz
   in der anderen Richtung (Satz weiß, welche Vokabel er trainiert) leistet nur `Gap.VocabKey` (Punkt 2).
   Sätze des Lehrwerks dürfen nicht in die App (`.claude/skills/lehrwerk-aufbereiten/SKILL.md:20-25`), eigene Sätze müssen
   darum neu entstehen.
5. **Automatische Dekodierung:** `BirkenbihlDecodingService` sucht nur die exakte Oberfläche, ohne Grundform
   (`backend/Pugling.Api/Services/Creator/BirkenbihlDecodingService.cs:48-62`, siehe
   [B-648](B-648-birkenbihl-jedes-wort-eine-vokabel.md), Stufe `geschaetzt`). *runs* fände *run* nicht.
6. **Was „schwer" heißt, ist messbar:** `PositionItemProgress.LastMissed` und `Box`
   (`backend/Pugling.Api/Models/PlanPositionEntities.cs:279`), plan-übergreifend `ItemProgress` mit dem Filter
   `onlyWeak` (Beherrschung unter 50 %, `backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:26,36`).
   Cloze unterstützt Nacharbeit (`SupportsRemediation`, `BuiltInExerciseTypes.cs:202`); die Vokabel nicht.
7. **Bild auf getippter Stufe:** die Regel „Bild nur auf nicht-getippten Stufen" gilt unverändert
   (`backend/Pugling.Api/CLAUDE.md`, Abschnitt Medien); [B-03](B-03-lueckensaetze-mit-bild.md) wurde aus genau diesem
   Grund verworfen.

**Korrekturen beim Grillen (2026-11-19), am Code gemessen:**

- **Die Dekodierung verrät die Lösung durch das Lernwort, nicht durch die Glosse.** `BirkenbihlDecoding` zeigt je Paar das Lernwort
  *über* der Glosse (`frontend/src/components/BirkenbihlDecoding.tsx`, `decoding-word` und `decoding-gloss`). Für *home* stünde also
  `home` über *heim* – das ist die Lösung. Die deutsche Glosse *heim* ist dagegen der **Hinweis**, den das Kind braucht
  (der Beispielsatz der Story zeigt sie ausdrücklich: „Tom rennt heim.“). Der Satz „alle Wörter außer der Zielvokabel
  übersetzt“ im Kopf der Story und die Formulierung „die Glosse der Zielvokabel fehlt“ weiter oben widersprechen dem Beispiel;
  maßgeblich ist das Beispiel (Entscheidung 2).
- **Eine Cloze-Karte schreibt den wortgebundenen Lernstand nicht.** `ItemProgressService.RecordAsync` kehrt ohne `ItemId` und
  `VocabularyId` zurück (`Services/Shared/ItemProgressService.cs:32`); `ClozeExerciseType.ItemsOf` setzt beides nicht
  (`Exercises/BuiltInExerciseTypes.cs:237-238`). Aussage und Kriterium 4 der Story („zählt über `VocabKey` in den Wort-Lernstand“)
  trafen nicht zu (Entscheidung 8).
- **Das Frontend rendert die Zeile schon für jeden Typ:** `Lektion.tsx:119` hängt `BirkenbihlDecoding` ohne Typbedingung an
  `karte.decoding`; das alte Kriterium 7 war im Ausgangszustand grün.
- **Der Cloze hat kein Feld für die Dekodierung:** `ClozeConfig` und `Gap` tragen keines
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:71-85`, `:109`); `ContentItem.Decoding` ist eine Liste von `WordPair`
  (`Services/Shared/ExerciseContentProvider.cs:56`).
- **Der KI-Creator erzeugt Einzelsätze mit je einer Lücke aus der Wortliste schon** (`--schultest`, B-524, mit `VocabKey` und
  `Form`; `backend/Pugling.Agent.Creator/CLAUDE.md`); schwache Wörter liegen im Briefing (`BriefingBuilder.cs:85-87`).

## Die echte Lücke

Schmaler als die Idee: Lückensatz, Wortbezug, gebeugte Form und Tipp-Stufen gibt es als Cloze. Es fehlen drei Dinge:

1. **Die Birkenbihl-Zeile an einer Cloze-Karte, mit ausgelassener Zielvokabel.** Heute reist `Decoding` unbedingt, und
   eine Zeile, in der die Zielvokabel glossiert ist, verrät die Lösung. Es braucht eine Regel „die Glosse der Lücke
   fehlt in der ausgespielten Zeile", die der **Server** erzwingt (Anti-Cheat), nicht das Frontend.
2. **Die Auswahl der Wörter und der Weg ins Tagespensum.** Nichts verbindet „Wort ist schwach" mit „bekommt eine
   Satzkarte".
3. **Die Herkunft der Sätze** (Creator-Arbeit, KI-Vorschlag) und deren Dekodierung, die an B-648 hängt.

Ob die Karte eine **neue Stufe der Lernkarte**, ein **Cloze mit Dekodierung** oder eine eigene Station ist, ist die
Hauptentscheidung (Frage 1) und bestimmt, ob `wo: beides`, `migration` und `vertragsbruch` ja oder nein sind.
Die Satzkarte als Cloze zu bauen wäre kein Parallel-Stack, sondern das im Repo vorgesehene Muster
(`CLAUDE.md`, „neue Lerntechnik = neuer `ExerciseType` im bestehenden Muster").

## Offene Punkte

1. ~~**Wo im Ablauf, und als welcher Typ?**~~ → Entscheidung 1.
2. ~~**Welche Wörter bekommen eine Satzkarte?**~~ → Entscheidung 3.
3. ~~**Woher kommen die Sätze?**~~ → Entscheidung 4.
4. ~~**Gebeugte Form:**~~ → Entscheidung 6.
5. ~~**Dekodierung automatisch oder von Hand?**~~ → Entscheidung 5.
6. ~~**Wie wird die Zielvokabel in der Zeile ausgespart?**~~ → Entscheidung 2.
7. ~~**Bild:**~~ → Entscheidung 7.
8. ~~**Zählt die Satzkarte für Pflicht und Leitner?**~~ → Entscheidung 8.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Die Satzkarte ist ein Cloze mit Dekodierung, in einer eigenen Position neben der Vokabelliste; die Lernkarte bleibt
   unberührt.** Neu ist ein optionales Feld `Decoding` an `Gap` (die Dekodierung des Satzes, in dem die Lücke steht; additiv,
   JSON in `ConfigJson`, keine Tabelle). Die Reihenfolge „nach dem freien Erinnern“ erzwingt nichts; sie ist die Plan-Reihenfolge,
   die der Vater setzt. Begründung: Der Cloze trägt Satz, Lücke, `VocabKey`, `Form` und die Tipp-Stufen
   (`ExerciseConfigs.cs:109`); ein Wort-Atom der Vokabelübung hat keinen Satz. Verworfen: eine Stufe der Lernkarte (Wort-Atom ohne
   Satz) und eine eigene Station (Parallel-Stack gegen `CLAUDE.md`). Kosten: ein Vertragsfeld, ein Editor für die Dekodierung im
   Cloze-Formular des Vater-Webs. Ausgeschlossener Fall: die Satzkarte als Stufe im selben Atom wie das Wort; gegen die User
   Story („zusätzlich“ geübt) ist das gewollt.
2. **Der Server maskiert das Lernwort aller Lücken-Lösungen in der Dekodierung und lässt die Glosse stehen.** In
   `ClozeExerciseType.ItemsOf` wird jedes Paar, dessen `LearningWord` (ohne Groß-/Kleinschreibung) einer Lösung einer Lücke der
   Config entspricht (`ClozeGaps.SolutionOf`, `BuiltInExerciseTypes.cs:605-606`), mit `LearningWord = ""` ausgegeben; die
   Glosse (*heim*) bleibt als Hinweis. Findet sich zur eigenen Lücke der Karte kein Paar, geht die Dekodierung dieser Karte
   **gar nicht** hinaus. Das Frontend zeigt ein leeres Lernwort als „___“ mit vorgelesenem „Lücke“. Die unbedingte Ausgabe in
   `PositionPlayService.cs:541-543` bleibt dadurch sicher, weil die Maskierung vor ihr, im Typ, passiert; der Hinweis in
   `BuiltInExerciseTypes.cs:162-171` bekommt den Satz dazu. Begründung: Anti-Cheat gehört auf den Server, und das Feld
   `LearningWord` steht **nicht** in der Lösungsfeld-Liste von `ConventionGuardTests`, das Tor fängt es also nicht. Verworfen:
   die Glosse der Zielvokabel weglassen (nähme dem Kind den Hinweis; widerspricht dem Beispiel der Story) und Maskieren im
   Frontend. Kosten: ein Wort wie *run*, das zugleich Lösung und Füllwort ist, wird überall im Satz maskiert; die Ausgabe
   bei fehlendem Paar nimmt dem Kind die Zeile. Ausgeschlossener Fall: eine Lücke, deren Wort der Creator nicht in die
   Dekodierung aufnahm – die Karte läuft dann ohne Zeile. Gegen die User Story gerechnet: der Satz trägt die Bedeutung weiter,
   die Zeile ist Hilfe, nicht Voraussetzung.
3. **Die Auswahl der Wörter macht der KI-Creator aus den schwachen Wörtern des Kindes, ein Vorschlag je Aufruf und keine
   Pflicht.** Er hat sie schon im Briefing (`BriefingBuilder.cs:85-87`, `child.WeakWords`); ein neuer Server-Endpunkt entfällt.
   Die Schwellen bleiben die bestehenden. Verworfen: ein neuer `suggestions`-Endpunkt (zweite Auswahlregel neben `onlyWeak`).
   Kosten: die Auswahl liegt im Agenten statt im Server; wer ohne Agent arbeitet, wählt die Wörter von Hand über `by-word`.
   Ausgeschlossener Fall: eine serverseitig tägliche Satzkarte ohne Zutun des Vaters; gegen die Story („schwer zu merkende
   Vokabeln“) reicht ein Vorschlag, die Pflicht setzt der Vater.
4. **Die Sätze sind eigene Sätze, vom KI-Creator entworfen (Wortschatz der Unit, wie `--schultest`) und vom Creator durchgesehen;
   Sätze des Buchs kommen nie in die App.** Beleg: `.claude/skills/lehrwerk-aufbereiten/SKILL.md:20-23`. Sätze bleiben Cloze-Texte
   und kommen nicht in den Wortspeicher ([B-649](B-649-uebersetzungssaetze-im-vokabelspeicher.md)). Kosten: jeder Satz braucht
   eine Durchsicht. Ausgeschlossener Fall: Schultest-Sätze des Lehrwerks als Quelle.
5. **Gebaut wird nach B-648; bis dahin ist die Story gegrillt, aber nicht baubar.** Die Dekodierung entsteht aus dem Wortspeicher
   (Grundform-Suche), nicht aus dem Modell; B-648 steht auf `geschaetzt`, seine Entscheidungen 10 bis 12 sind ebenfalls
   unbestätigt. Verworfen: von Hand dekodieren und B-648 abwarten (zwei Wege für dieselbe Sache). Kosten: B-650 wartet auf zwei
   Bestätigungen. Ausgeschlossener Fall: eine Satzkarte vor der Grundform-Suche; *runs* fände *run* nicht
   (`BirkenbihlDecodingService.cs:48-62`).
6. **Die gebeugte Form ist die Form des Satzes im Feld `Gap.Form`; keine neue Mechanik.** Beleg: `SolutionOf`,
   `BuiltInExerciseTypes.cs:605-606`. Kosten: keine. Ausgeschlossener Fall: die Grundform als richtige Antwort.
7. **Kein Bild auf der Satzkarte.** Die Regel „Bild nur auf nicht-getippten Stufen“ (`backend/Pugling.Api/CLAUDE.md`, Medien)
   gilt unverändert; B-03 wurde aus diesem Grund verworfen. Kosten: weniger Anker für das Kind. Ausgeschlossener Fall: ein
   Bild am Lückensatz.
8. **Die Satzkarte zählt für Pflicht und Leitner der Position wie jede Cloze-Position und schreibt den wortgebundenen
   `ItemProgress` nicht.** Die Pflicht, ihre Münzen und der Malus sind die der Position und werden vom Vater so gesetzt wie bei
   jeder anderen; diese Story ändert daran nichts. Weil die Karte den Lernstand des Wortes nicht fortschreibt, bleibt ein Wort in der
   Sicht „schwach“, bis die Vokabelübung es hebt; der Agent schlägt darum kein Wort vor, für das das Kind schon eine Satzkarte
   hat. Verworfen: den `ItemProgress` über `VocabKey` mitschreiben (bräuchte `ItemId` und `VocabularyId` im Cloze-Atom, ein
   neuer Pfad in `ItemProgressService`). Kosten: Satzkarten heben die „Schwach“-Liste nicht. Ausgeschlossener Fall: ein Kind, das
   ein Wort im Satz beherrscht und trotzdem weiter als schwach gelistet wird; gegen die User Story („im Zusammenhang
   verankern“) fehlt dann der Rückfluss in die Wortsicht. Das ist die Entscheidung, bei der der Nutzer am ehesten anders
   entscheidet.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Die Karte einer Lücke mit Lösung *home* trägt eine Dekodierung, in der das Paar von *home* `LearningWord = ""` und die Glosse
   *heim* hat; alle anderen Paare sind unverändert. Rot, wenn `ClozeExerciseType.ItemsOf` die Dekodierung unmaskiert durchreicht
   (dann steht *home* im JSON der Karte).
2. Steht zur Lücke kein Paar mit der Lösung in der Dekodierung, trägt die Karte keine Dekodierung. Rot, wenn die Dekodierung
   dann unmaskiert oder gekürzt mitgeht.
3. Über **alle** Cloze-Stufen (je ein Fall pro Stufenwert) enthält die ausgespielte Karte die Lösung in keinem Feld außer den
   Auswahlfeldern der Stufe selbst (Wortkasten, Kacheln, Hinweisbox). Rot, wenn ein Pfad die Dekodierung am Typ vorbei füllt.
4. Eine Lücke mit `Form = "runs"` gilt nur mit „runs“ als richtig, mit „run“ als falsch. Rot, wenn die Wertung die Grundform der
   Vokabel statt der `Form` nimmt (`ClozeGaps.SolutionOf`).
5. Das Ergebnis der Satzkarte schreibt den Lernstand der **Position** fort (`PositionItemProgress` der Lücke). *Absichtserklärung
   für die zweite Hälfte:* der wortgebundene `ItemProgress` bleibt unverändert; dafür gibt es keine rote Änderung, das Kriterium
   hält nur die Entscheidung 8 fest. Rot (erste Hälfte), wenn das Ergebnis den Positionsstand nicht schreibt.
6. Der KI-Creator wählt für den Satzkarten-Entwurf ein Wort mit `LastMissed` bzw. niedriger Beherrschung und kein sicheres und
   schlägt kein Wort vor, für das das Kind schon eine Satzkarte hat. Rot, wenn die Auswahl alle oder keine Wörter liefert
   (Testklasse in `CreatorAgentTests` beim Schätzen).
7. `PUT` eines Cloze mit `Gap.Decoding` speichert das Feld, und die nächste Karte zeigt die geänderte Fassung. Rot, wenn das Feld
   im Vertrag fehlt (`400 unknown_field`) oder beim Speichern verworfen wird.
8. `BirkenbihlDecoding` zeigt ein Paar mit leerem Lernwort als „___“, vorgelesen als „Lücke“, und die Glosse daneben
   (Komponententest in `BirkenbihlDecoding.test.tsx`). Rot, wenn ein leeres Lernwort als leere Spalte erscheint.
9. *Absichtserklärung:* Das Rendern der Zeile auf der Cloze-Karte (`Lektion.tsx:119`) braucht keine Frontend-Änderung; das
   Kriterium fällt nicht, weil es im Ausgangszustand schon gilt, und steht nur da, damit niemand die Typbedingung nachrüstet.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet); die Entscheidungen oben sind weiter unbestätigt, `wartet_auf` bleibt.*

**Größe: L** – der Server-Teil allein (ein Feld, eine Maskierung, ein Tor-Test) ist M; dazu kommen der Dekodierungs-Editor im
Cloze-Formular des Vater-Webs, die Leerstellen-Darstellung, die Auswahl im KI-Creator und der Abgleich der Lader-Feldliste. Das
trägt eine Etappe, aber kein XL: alles hängt an einem Feld und einer Maskierung und ist in Reihenfolge baubar.

- **`wo: beides`** – Backend zuerst: `Gap.Decoding` und die Maskierung in `ClozeExerciseType.ItemsOf`; danach Frontend
  (`BirkenbihlDecoding`, Cloze-Formular) und Agent.
- **`migration: nein`** – nachgesehen: `Gap` liegt als JSON in `ConfigJson` (`Pugling.Contracts/Exercise/ExerciseConfigs.cs:109`);
  keine Spalte, keine Tabelle, die Kette bleibt.
- **`vertragsbruch: nein`** – nachgesehen: `Decoding` ist ein neuer **letzter** optionaler Parameter von `Gap` mit Vorgabe `null`;
  bestehende Konfigurationen und Aufrufer bleiben gültig. Neue Konfigurationen mit `decoding` sind für alte Server `400 unknown_field`,
  das ist die gewollte Richtung (`UnmappedMemberHandling.Disallow`, Repo-`CLAUDE.md`). Frontend-Typen werden
  neu erzeugt, die `unknown_field`-Guards ziehen mit dem neuen Feld mit.

### Reihenfolge und Abhängigkeiten

1. **B-648 zuerst** (Grundform-Suche im `BirkenbihlDecodingService`, `geschaetzt`, L, beides). Ohne sie fände die automatische
   Dekodierung *runs* nicht (`BirkenbihlDecodingService.cs:48-62`).
2. Dann B-650. Der Server-Teil (Feld, Maskierung, AK 1 bis 5, 7) braucht B-648 technisch **nicht** – die Dekodierung kann von
   Hand im Config stehen – und ließe sich vorziehen; Entscheidung 5 legt es trotzdem hinter B-648, damit es nur einen Weg gibt.
3. Beide Stories hängen an unbestätigten Sonnet-Entscheidungen (B-648 Entscheidungen 10 bis 12, B-650 Entscheidungen 1 bis 8).

### Risiken

1. **Die Lösung kann auf einem zweiten Weg mitreisen.** `PositionPlayService.cs:541-543` gibt `Decoding` unbedingt aus; sicher
   ist das nur, solange alle Füller maskieren. Zwei Gaps im selben Text teilen sich `c.Text`; die Maskierung muss die Lösungen
   **aller** Lücken des Textes erfassen, nicht nur der Karte (Entscheidung 2 verlangt das; AK 1 prüft nur eine Lücke, der Bau
   ergänzt einen Zwei-Lücken-Fall).
2. **Das Tor fängt `LearningWord` nicht** (nicht in der Lösungsfeld-Liste von `ConventionGuardTests`); die Sicherung ist allein
   AK 1 bis 3. Rot-Probe: Maskierung aus `ItemsOf` entfernen, AK 1 muss fallen.
3. **Die Lader-Feldliste muss mitziehen:** `laden.py` führt die Gap-Felder als Kopie
   (`["index", "answer", "alternatives", "vocabKey", "form", "distractors", "prompt"]`), und `LadenFieldListTests` hält sie am
   Vertrag. Wird `decoding` nur im Vertrag ergänzt, fällt dieser Test rot; beide Stellen im selben Commit ändern.
4. **Speicherort je Lücke:** `Decoding` hängt an `Gap`, der Satz (`ClozeConfig.Text`) ist aber für alle Lücken derselbe. Bei
   mehreren Lücken im Satz steht dieselbe Dekodierung mehrfach und kann auseinanderlaufen. Entscheidung 1 nimmt das hin; ob das
   Feld an `ClozeConfig` (je Text) besser sitzt, ist eine Frage an den Nutzer bei der Bestätigung.
5. **Die Rückseite der Maskierung:** Ist die Lösung zugleich ein Füllwort (*run* … *to run*), maskiert der Server beide Stellen;
   das ist hingenommen (Kosten in Entscheidung 2), im Test als Fall festhalten.

### Angriffsplan

1. `Gap` um `List<WordPair>? Decoding = null` erweitern, mit Dokukommentar (Vertrag).
2. `ClozeExerciseType.ItemsOf` füllt `ContentItem.Decoding` aus der Lücke und maskiert das Lernwort jedes Paares, das einer
   Lösung eines beliebigen Gaps des Textes entspricht (`ClozeGaps.SolutionOf`); fehlt das Paar der eigenen Lücke, bleibt `Decoding`
   `null`. Hinweis im Kommentar von `BirkenbihlExerciseType.IsTypedStage` (`BuiltInExerciseTypes.cs:162-171`) ergänzen.
3. `laden.py`-Feldliste um `decoding` ergänzen (gleicher Commit, `LadenFieldListTests` bleibt grün).
4. `BirkenbihlDecoding.tsx`: leeres `learningWord` als „___“ mit vorgelesenem „Lücke“; Cloze-Formular
   (`frontend/src/vater/ClozeTexts.tsx`, `exerciseConfig.tsx`) um die Dekodierung erweitern (build + read-back).
5. KI-Creator: Auswahl nach `LastMissed`/niedriger Beherrschung aus `child.WeakWords` (`BriefingBuilder.cs:85-87`), kein Wort
   mit vorhandener Satzkarte.
6. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Reviewer `pugling-reviewer` und `frontend-reviewer`.

### Testweg

- `ClozePlayTests` (vorhanden): AK 1 bis 3 – je ein Fall pro Cloze-Stufenwert, dazu der Zwei-Lücken-Fall; Rot-Probe: die
  Maskierung herausnehmen.
- `ClozePlayTests`: AK 4 (`Form = "runs"` gilt, „run“ nicht) und AK 5 (Positionsstand wird geschrieben; die zweite Hälfte bleibt
  Absichtserklärung).
- `UnknownFieldTests` bzw. `ClozePlayTests`: AK 7, PUT mit `Gap.Decoding` und Read-back.
- `LadenFieldListTests`: bleibt grün, nachdem `laden.py` das Feld führt (Rot-Probe: Feld nur im Vertrag ergänzen).
- `CreatorAgentTests`: AK 6, Auswahl der Wörter.
- `frontend/src/components/BirkenbihlDecoding.test.tsx`: AK 8.
- Rollengang als Vater (Cloze mit Dekodierung anlegen, vorspielen) und als Sohn (Karte mit „___“) vor der Abnahme.

**Bau-Sperre:** nicht vor B-648 und nicht vor der Bestätigung der Entscheidungen durch den Nutzer.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Chat mit dem Vater; Beispiel „Tom runs ___ / Tom rennt heim“).
- **2026-11-17** — Frage 3 vorab geprüft: Das Vokabelschema hat kein Beispielsatz-Feld. Beispielsätze gibt es nur in der privaten Lehrwerk-Notiz, und wegen des Urheberrechts dürfen sie nicht in die App.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand am Code. Cloze hat Lücke, `VocabKey`, `Form` und Tipp-Stufen schon; es fehlen die Dekodierung mit ausgelassener Zielvokabel (Server-erzwungen), die Auswahl schwacher Wörter und die Herkunft der Sätze. Abhängigkeit von B-648.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt, `wartet_auf` gesetzt): Entscheidungen 1 bis 8.
  Am Code korrigiert: die Dekodierung verrät die Lösung über das Lernwort, nicht über die Glosse (Entscheidung 2 folgt dem Beispiel
  der Story, nicht ihrem Kopftext); eine Cloze-Karte schreibt den wortgebundenen Lernstand nicht (Entscheidung 8); das Frontend
  rendert die Zeile schon typunabhängig. Baubar erst nach B-648. Akzeptanzkriterien final, zwei als Absichtserklärung beschriftet.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet; `wartet_auf` bleibt): L, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (`Gap.Decoding` additiv). Baureihenfolge nach B-648. Neu am Code: die `laden.py`-Feldliste führt die Gap-Felder und muss mit
  `decoding` im selben Commit mitziehen (`LadenFieldListTests`).
- **2026-11-19** — Entscheidungen 2 und 8 vom Nutzer bestätigt (Lernwort maskieren, Glosse stehen lassen; Satzkarte füttert den Wort-Lernstand nicht). Die übrigen Entscheidungen warten weiter, darum bleibt `wartet_auf`.
