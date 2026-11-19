---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln, lerntechnik/birkenbihl]
aliases: [Vokabel im Satz, Satzkarte]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17"
grund: ""
ersetzt_durch: []
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

Je mit Empfehlung; die Entscheidung trifft erst die Grill-Runde.

1. **Wo im Ablauf, und als welcher Typ?** Eine Stufe der Lernkarte (Vokabel-Typ), ein Cloze mit Dekodierung oder
   eine eigene Station?
   *Empfehlung:* Cloze mit Dekodierung. Der Cloze trägt Satz, Lücke, `VocabKey`, `Form` und die Tipp-Stufen schon; neu
   ist nur die Dekodierung. Die Lernkarte bliebe unberührt. Die Satzkarte läuft als Position **neben** der Vokabelliste und
   kommt **nach** dem freien Erinnern, nicht anstelle der Kombi-Lernkarte.
2. **Welche Wörter bekommen eine Satzkarte?** Alle oder nur die schweren?
   *Empfehlung:* nur Wörter mit `LastMissed` oder niedriger Beherrschung (`onlyWeak`, Punkt 6). Ein Vorschlag je Tag,
   keine Pflicht für alle. Die Schwellen sind Vater-Sache und gehören nicht in diese Story.
3. **Woher kommen die Sätze?** Lehrwerk, vorhandene Übungen oder KI-Creator?
   *Empfehlung:* eigene Sätze, vom KI-Creator vorgeschlagen und vom Creator durchgesehen, mit Wortschatz aus der Unit.
   Die Sätze des Buchs dürfen nicht übernommen werden (Punkt 4). Sätze bleiben Cloze-Texte und kommen nicht in den
   Wortspeicher ([B-649](B-649-uebersetzungssaetze-im-vokabelspeicher.md)).
4. **Gebeugte Form:** Was wird erwartet?
   *Empfehlung:* die Form des Satzes, im Feld `Gap.Form` (Punkt 2); keine neue Mechanik nötig.
5. **Dekodierung automatisch oder von Hand?**
   *Empfehlung:* automatisch, mit Korrektur durch den Creator – aber erst nach B-648 (Grundform-Suche). Bis dahin
   von Hand; die Story ist darum **nicht vor B-648 baubar** (Abhängigkeit, nicht Duplikat).
6. **Wie wird die Zielvokabel in der Zeile ausgespart?** Die Glosse der Lücke fehlt oder ist durch eine Auslassung
   ersetzt; das erzwingt der Server auf getippten Stufen.
   *Empfehlung:* Server erzwingt es (Regelbruch wäre ein stiller Lösungsverrat), mit eigenem Testfall je getippter
   Cloze-Stufe. Das ist eine Folge von Punkt 3 der Lücke und kein Detail.
7. **Bild:** Gilt das Anti-Cheat-Argument aus B-03 hier?
   *Empfehlung:* ja, vorerst ohne Bild bauen. Die Regel „Bild nur auf ungetippten Stufen" bleibt, die Story wird so
   kleiner.
8. **Zählt die Satzkarte für Pflicht und Leitner?**
   *Empfehlung:* ja, wie jede Cloze-Position (sie zählt über ihre Lücken und `VocabKey` in den
   Wort-Lernstand). Ein eigener Münz-Satz ist nicht nötig.

## Akzeptanzkriterien (Entwurf)

Je Kriterium die Änderung, die es rot macht. Entwurf; endgültig erst nach der Grill-Runde.

1. Eine Cloze-Karte mit Dekodierung liefert die Zeile mit, in der **die Glosse der Zielvokabel fehlt**; alle anderen
   Wörter sind glossiert. *Rot, wenn* die Ausspielung die Dekodierung unverändert durchreicht
   (`PositionPlayService.cs:541-543`): dann steht *heim* in der Zeile.
2. Auf **jeder getippten** Cloze-Stufe enthält kein Feld der Karte die Lösung, auch nicht die Dekodierung.
   *Rot, wenn* eine Stufe (etwa Buchstabenkästchen) die Zeile ungefiltert ausgibt.
3. Eine Lücke mit `Form = "runs"` gilt nur mit „runs" als richtig, mit „run" als falsch.
   *Rot, wenn* die Wertung die Grundform der Vokabel statt der `Form` nimmt (`ClozeGaps.SolutionOf`, `BuiltInExerciseTypes.cs:605-606`).
4. Das Ergebnis der Karte schreibt den Lernstand der Zielvokabel fort (`VocabKey` → `ItemProgress`).
   *Rot, wenn* die Satzkarte ohne Verknüpfung zur Vokabel gespielt wird.
5. Ein Wort mit `LastMissed` wird für die Satzkarte vorgeschlagen, ein sicheres nicht.
   *Rot, wenn* die Auswahl alle Wörter oder keines liefert.
6. Der Creator kann die Dekodierung eines Satzes korrigieren und die Karte danach spielen.
   *Rot, wenn* die Korrektur nicht gespeichert oder ignoriert wird.
7. Im Sohn-Spiel erscheint die Birkenbihl-Zeile auf der Cloze-Karte wie im Birkenbihl-Typ
   (`Lektion.tsx:119`). *Rot, wenn* das Frontend die Zeile nur für den Birkenbihl-Typ rendert.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Chat mit dem Vater; Beispiel „Tom runs ___ / Tom rennt heim“).
- **2026-11-17** — Frage 3 vorab geprüft: Das Vokabelschema hat kein Beispielsatz-Feld. Beispielsätze gibt es nur in der privaten Lehrwerk-Notiz, und wegen des Urheberrechts dürfen sie nicht in die App.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand am Code. Cloze hat Lücke, `VocabKey`, `Form` und Tipp-Stufen schon; es fehlen die Dekodierung mit ausgelassener Zielvokabel (Server-erzwungen), die Auswahl schwacher Wörter und die Herkunft der Sätze. Abhängigkeit von B-648.
