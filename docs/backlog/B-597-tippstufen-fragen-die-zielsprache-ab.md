---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Zielsprache tippen, Tipprichtung, Lernsprache schreiben]
status: abgenommen
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
grund: ""
ersetzt_durch: []
wartet_auf: ""
entgangen_bei: []
nachgeschaut: "2026-11-17: Drehung an jeder Karten-/Wertungsstelle nachgerechnet (CardFacets, grader.Matches, IsMisspelled, MatchOpenEntry gegen CardForStage/AtStage; PositionPracticeController :223/:461/:572, PositionTestsController :240/:275/:399, ExercisePreviewService :88/:128, ExtraTrainingService :434); Obergrenze auf jedem Stufen-Weg (PositionPlayService :180/:228, Tests :193); Rot-Probe SchreibstufeLernspracheTests :124/:170 gegen Mutante gerechnet. Funde: B-607 (Hinweis verschwindet), B-608 (Münz-Ansage)"
---

# B-597 · Getippt wird immer die Zielsprache

Regel des Vaters: Wo das Kind eine Vokabel schreibt, schreibt es das **Fremdwort**. Auf das kommt es an.

> **Begriff:** „Zielsprache“ meint in dieser Story die Sprache, die das Kind lernt (Englisch, Französisch, Latein).
> Im Vokabelspeicher heißt diese Sprache aber `SourceLanguage`, und `TargetLanguage` ist das **Deutsche**
> (`VocabEntities.cs:17-18`, Vorgabe im Editor `sourceLang: "en", targetLang: "de"` in
> `exerciseConfig.tsx:180`). Die Story schreibt darum **Lernsprache** (wie [B-472](B-472-jedes-wort-in-beide-richtungen.md),
> „Lernsprache → Muttersprache“). Ob die Feldnamen selbst nachgezogen werden, ist eine eigene Frage (Offener Punkt 7).

## User Story

Als **Vater** möchte ich, dass mein Kind auf jeder Stufe, auf der es eine Vokabel **schreibt**, das Wort der
**Lernsprache** schreibt (deutsch gezeigt, *walk* getippt), damit es übt, was in der Klassenarbeit zählt: das
Fremdwort richtig schreiben. Das soll nicht davon abhängen, welche Abfragerichtung jemand beim Anlegen der Übung
gewählt hat.

## Ist-Stand am Code

- **Die Karte zeigt kanonisch das Fremdwort und erwartet Deutsch.** `ExerciseContentResolver.cs:101` baut
  jede Karte als `Prompt = v.Word`, `Answer = v.Translation`. Gelesen am Speicher: `Word` steht in
  `SourceLanguage` (`ExerciseContentResolver.cs:108-109`, Vorlesen in `v.SourceLanguage`).
- **Die Richtung wird pro Übung gewählt und vor der Stufe angewendet.** `ExerciseContentResolver.cs:34-38` liest
  `VocabularyConfig.Direction` und gibt sie an `ExerciseContentProvider.WithDirection` (`ExerciseContentProvider.cs:84-90`):
  `front-to-back` lässt die Karte, `back-to-front` tauscht jede, `both` macht aus Wort *i* die Karten 2*i*
  (vorwärts) und 2*i*+1 (getauscht). Die Kartenliste entsteht damit **ohne Kenntnis der Stufe**. Alle Spielpfade
  holen sie über denselben Einstieg: Üben (`PositionPracticeController.cs:265/309/349/392`), Test
  (`PositionTestsController.cs:115/226/260/354`), Bonus (`ExtraTrainingService.cs:352`), Vorschau
  (`ExercisePreviewService.cs:30/56`).
- **Die Stufe hängt an der Karte, nicht nur am Tag.** `PositionPlayService.CardStage` (`PositionPlayService.cs:159-166`):
  neue Karte = Kennenlernen (B-419), danach klettert sie mit ihrer Leitner-Box die Stufenleiter des Typs hoch
  (B-476). In einer Runde können also Karten auf Auswahl und Karten auf Freitext nebeneinander liegen.
- **„Typed“ heißt im Code „geprüft“, nicht „geschrieben“.** `StageMechanics.IsTyped` (`StageMechanics.cs:17-19`) zählt
  Buchstabenkästen, Freitext, Hören, **Auswahl** und Buchstabensalat. Geschrieben wird nur auf vier davon; Auswahl
  ist ein Antippen.
- **Der Tausch wirft das Audio weg.** `ExerciseContentProvider.Swap` (`ExerciseContentProvider.cs:107-118`) setzt
  `AudioUrl = null` und `SpeechLang = null`, weil das Vorlesen nach dem Tausch die Lösung spräche. Die Hör-Stufe
  (`TestStage.Audio`, `StudyPlanEntities.cs:47-48`: „Wort wird vorgelesen → Übersetzung tippen“) liest also heute
  nur vorwärts gefragte Karten vor.
- **Der Lernstand hängt am Kartenindex.** `PositionItemProgress` wird über `ItemIndex` gelesen
  (`PositionPlayService.cs:146-148`); B-472 hat beim Einführen von `both` alle gespeicherten Indizes
  (`PositionItemProgress`, `TestItemResult`, `PracticeSessions.Order`, `TestAttempts.Order`) per Einmal-Skript
  umgeschrieben (B-472, Entscheidung 5). Der plan-übergreifende `ItemProgress` dagegen hängt am Wort, beide
  Richtungen schreiben in dieselbe Zeile (B-472, Entscheidung 4).
- **Die Wege zum Anlegen wählen verschiedene Vorgaben.** Vater-Web: `front-to-back` (`exerciseConfig.tsx:180`,
  Auswahl `:471-474`, `VaterExerciseCreate.tsx:504-507`), Beschriftung „vorne → hinten“ ohne Hinweis, welche Sprache
  geschrieben wird. KI-Creator: `Direction = "front-to-back"` (`VocabularyStrategy.cs:139`). `laden.py`: `both`
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:457`).
- **Gemessen an der echten DB** (read-only, 2026-11-16): 17 Vokabelübungen, davon 7 auf `front-to-back` (auf jeder
  Schreibstufe wird Deutsch getippt) und 10 auf `both` (die Hälfte der Schreibkarten fragt Deutsch).

## Die echte Lücke

Es fehlt **eine Regel an der Stufe**. Heute entscheidet eine Einstellung beim Anlegen, die drei Werkzeuge
verschieden setzen und die im Editor nicht sagt, was sie fürs Schreiben bedeutet. Was gebraucht wird: Auf einer
Schreibstufe ist die Karte immer „deutsch gezeigt, Lernsprache erwartet“. Das muss **für Anzeige und Bewertung
gleich** gelten, also an einer Stelle, durch die Karte und Prüfung beide laufen. Kartenzahl und Indizes dürfen sich
dabei nicht verschieben, sonst trifft der gespeicherte Lernstand die falsche Karte (Lehre aus B-472).

## Offene Punkte

1. ~~**Welche Stufen zählen als „Schreiben“?**~~ → Entscheidung 1. Empfehlung: Buchstabenkästen, Freitext, Buchstabensalat und Hören.
   Auswahl bleibt bei der Richtung der Übung. Begründung: Die Regel des Vaters spricht vom Schreiben, und „walk →
   was heißt das?“ als Auswahl übt Wiedererkennen, das auch gebraucht wird.
2. ~~**Wie wird die Regel umgesetzt?**~~ → Entscheidung 2. Empfehlung: **beim Bauen der Karte je Stufe drehen**. Die Kartenliste bleibt
   wie heute (gleiche Zahl, gleiche Indizes), und eine Funktion „Karte für Stufe“ dreht eine vorwärts liegende Karte
   um, wenn die Stufe eine Schreibstufe ist. Anzeige und Bewertung nutzen dieselbe Funktion. Keine Datenumschreibung.
   Die Alternative (Richtung überall fest auf „rückwärts“ setzen) bräuchte für `both` wieder ein Umschreib-Skript wie
   B-472 und nähme dem Kind das Wiedererkennen.
3. ~~**Was wird bei `both` aus der Vorwärtskarte auf einer Schreibstufe?**~~ → Entscheidung 3. Nach Punkt 2 würden dann beide Karten eines
   Worts dasselbe abfragen. Optionen: (a) hinnehmen, das Wort wird doppelt geschrieben; (b) die Vorwärtskarte klettert
   nur bis Auswahl und wird nie geschrieben. Empfehlung: **(b)**. Die zwei Karten bekommen dann zwei Aufgaben
   (erkennen und schreiben), wie B-472 sie gemeint hat. Kosten: Die Stufenleiter und die Kalender-Frist
   (`LadderDays`) brauchen eine Obergrenze je Karte. Das ist der teuerste Teil der Story.
4. ~~**Hören:**~~ → Entscheidung 4. Nach der Regel hört das Kind das Fremdwort und schreibt das Fremdwort, also ein **Diktat**. Empfehlung:
   genau so, denn Schreiben nach Gehör ist wertvoll. Dann darf die Karte das Wort nicht als Text zeigen, nur Audio
   bzw. Vorlesen. Wo es weder Aufnahme noch Vorlesesprache gibt, fällt die Karte auf Freitext zurück.
5. ~~**Test und Bonus-Training:**~~ → Entscheidung 5. Gilt die Regel dort genauso? Empfehlung: ja. Sie laufen über dieselbe Kartenquelle,
   und ein Test, der anderes prüft als das Üben, wäre unfair.
6. ~~**Vorhandener Lernstand:**~~ → Entscheidung 6. Karten, die bisher auf Deutsch geschrieben wurden, stehen in Leitner-Boxen, die mit
   Deutsch verdient wurden. Empfehlung: Boxen **nicht** zurücksetzen. Die Stufenleiter und der nächste Test zeigen,
   ob das Wort in der Lernsprache sitzt, und ein Rücksetzen würde verdiente Arbeit wegnehmen.
7. ~~**Benennung im Editor und im Speicher:**~~ → Entscheidung 7. Empfehlung: im Vater-Web die Abfragerichtung umbeschriften („Erkennen:
   Fremdwort → Deutsch“) und einen festen Hinweis zeigen, dass immer die Lernsprache geschrieben wird. Die Felder
   `SourceLanguage`/`TargetLanguage` bleiben (Umbenennung wäre ein Vertragsbruch ohne Nutzen für das Kind).
8. ~~**Vorgaben beim Anlegen:**~~ → Entscheidung 8. Sollen KI-Creator (`front-to-back`) und `laden.py` (`both`) eine gemeinsame Vorgabe
   bekommen? Empfehlung: nach dieser Story nicht mehr nötig, weil die Richtung nur noch das Erkennen steuert. Bleibt
   offen, falls Punkt 3 anders entschieden wird.

## Entscheidungen

Gegrillt mit dem Vater am 2026-11-16, eine Frage nach der anderen.

1. **Schreibstufen sind Buchstabenkästen, Freitext, Buchstabensalat und Hören.** Die Auswahl folgt weiter der
   Abfragerichtung der Übung. *Begründung:* Die Regel spricht vom Schreiben; „*walk* – was heißt das?“ übt das
   Wiedererkennen, das Text- und Hörverständnis brauchen. *Kosten:* Die Auswahl übt keine Rechtschreibung; wer dort
   das Fremdwort heraussuchen lassen will, stellt die Übung auf „Deutsch → Fremdwort“.
2. **Die Karte wird beim Bauen je Stufe gedreht.** Die Kartenliste bleibt (gleiche Zahl, gleiche Indizes); eine
   Funktion „Karte für Stufe“ dreht eine vorwärts liegende Karte auf einer Schreibstufe um, und Anzeige wie Bewertung
   laufen durch dieselbe Funktion. *Begründung:* Lernstand, Leitner-Boxen und laufende Tests bleiben gültig, kein
   Umschreib-Skript gegen die echte DB. *Kosten:* Dieselbe Karte erscheint je nach Stufe vorwärts oder gedreht; der
   Positions-Bericht muss das beschriften.
3. **Bei `both` klettert die Vorwärtskarte nur bis zur Auswahl, die Rückwärtskarte bis zum Schreiben.** *Begründung:*
   Sonst fragten beide Karten eines Worts auf Schreibstufen dasselbe ab; so hat jede eine Aufgabe (erkennen,
   schreiben), wie B-472 „beide“ gemeint hat. *Kosten:* Stufenleiter (`CardStage`), Kalender-Frist (`LadderDays`) und
   Teststufe brauchen eine Obergrenze je Karte; der teuerste Teil der Story.
4. **Die Hör-Stufe wird Diktat.** Die Karte zeigt das Wort nicht als Text und liest es vor, aus der Aufnahme oder
   sonst über die Sprachausgabe des Geräts (B-252); gibt es für die Sprache keine Stimme, fällt die Karte auf Freitext
   zurück. *Begründung:* Schreiben nach Gehör kann keine andere Stufe, und keine der 265 Vokabeln in Übungen hat eine
   Aufnahme (echte DB, 2026-11-16) – ohne Sprachausgabe wäre die Stufe für Theodor gar nicht spielbar. *Kosten:*
   Die Qualität hängt vom Gerät ab, Wörter wie *read* können falsch klingen; die Beschriftung „Hören → tippen“ wird
   „Diktat“.
5. **Die Regel gilt überall gleich:** Üben, Abschlusstest, Bonus-Training, Vorschau. *Begründung:* Ein Test, der
   anderes prüft als das Üben, wäre unfair, und das Bonus-Training würde sonst Münzen fürs Schreiben deutscher Wörter
   zahlen. *Kosten:* Tests von `both`-Listen bestehen zur Hälfte aus Auswahl (Entscheidung 3) und werden etwas
   leichter.
6. **Die Leitner-Boxen bleiben, wie sie sind.** *Begründung:* Die Box ist verdiente Arbeit; ein Wort, das in der
   Lernsprache noch nicht sitzt, fällt beim ersten Fehler von selbst zurück. Kein Skript gegen die echte DB.
   *Kosten:* In den ersten Tagen nach der Umstellung mehr Fehler, kurz weniger Münzen, eventuell ein gerissenes
   Tagesziel mit Malus.
7. **Das Editor-Feld heißt „Erkennen“** mit „Fremdwort → Deutsch / Deutsch → Fremdwort / beides“ und dem festen
   Hinweis „Geschrieben wird immer das Fremdwort“. Die Vertragsfelder `SourceLanguage`/`TargetLanguage` und
   `direction` bleiben. *Begründung:* Die Beschriftung sagt, was die Einstellung jetzt tut; eine Umbenennung im
   Vertrag kostete Frontend, Client, KI-Creator, `laden.py` und eine Migration, ohne dass das Kind etwas merkt.
   *Kosten:* `TargetLanguage` = Deutsch bleibt im Code missverständlich; ein Kommentar am Feld stellt es klar.
8. **Die Vorgaben der Anlege-Wege bleiben:** Vater-Web und KI-Creator „Fremdwort → Deutsch“, `laden.py` „beides“.
   *Begründung:* Keine Vorgabe ist nach Entscheidung 2 und 3 mehr falsch, alle schreiben die Lernsprache; „beides“
   für Buch-Wortlisten war die Entscheidung aus B-472. *Kosten:* Buch-Listen zählen weiter doppelt in der Tagesdosis;
   ob die 10er-Grenze Wörter oder Karten zählt, ist als offener Punkt nach
   [B-596](B-596-vokabeluebung-hoechstens-zehn-vokabeln.md) gewandert.
9. **Nachgereicht beim Schätzen: Das Diktat darf die Lösung im Datenpaket tragen.** Ohne Aufnahme liest die
   Sprachausgabe des Geräts vor und braucht dafür den Text im Client; das Kind sieht ihn nicht, die
   Entwicklerwerkzeuge zeigen ihn. *Begründung:* App im Hausgebrauch, ein 12-Jähriger mit Netzwerk-Tab ist
   unwahrscheinlich, und ohne Sprachausgabe wäre das Diktat für keine der 265 Vokabeln spielbar. *Kosten:* Eine
   bewusste Ausnahme vom Anti-Cheat-Grundsatz „keine Lösung vor der Antwort“; das Feld wird so benannt und
   kommentiert, dass ein späterer Leser die Ausnahme erkennt. Serverseitig erzeugte Aufnahmen bleiben der Weg, sie
   zurückzunehmen.

## Akzeptanzkriterien

- [ ] Auf jeder Schreibstufe (Buchstabenkästen, Freitext, Buchstabensalat, Diktat) zeigt eine Vokabelkarte das
  deutsche Wort (beim Diktat: liest das Fremdwort vor) und wertet nur die Lernsprache als richtig – in Üben, Test,
  Bonus-Training und Vorschau, unabhängig von der Abfragerichtung.
- [ ] Die Bewertung nutzt dieselbe Drehung wie die Anzeige: Wer *walk* zu „gehen“ schreibt, ist richtig; wer „gehen“
  schreibt, ist falsch.
- [ ] Kennenlernen, Selbsteinschätzung und Auswahl folgen der Abfragerichtung der Übung wie heute.
- [ ] Bei `both` kommt die Vorwärtskarte eines Worts nie auf eine Schreibstufe – weder über die Stufenleiter noch
  über die Kalender-Frist noch im Test; sie steht dort höchstens auf Auswahl.
- [ ] Das Diktat zeigt das Wort nicht als Text; ohne Aufnahme liest das Gerät in der Lernsprache vor, ohne Stimme
  für die Sprache wird die Karte als Freitext gespielt.
- [ ] Kartenzahl, Kartenindizes und Leitner-Boxen bleiben unverändert; es gibt kein Umschreib-Skript.
- [ ] Das Vater-Web zeigt das Feld „Erkennen“ mit den drei Optionen und den Hinweis „Geschrieben wird immer das
  Fremdwort“; `TargetLanguage` trägt einen Kommentar, dass es die Muttersprache ist.
- [ ] Integrationstests: eine `front-to-back`-Übung auf Freitext in Üben, Test und Bonus (rote Probe: ohne die
  Drehung erwartet die Karte „gehen“), eine `both`-Übung über die Kalender-Frist (rote Probe: ohne Obergrenze steht
  die Vorwärtskarte auf Freitext), das Diktat ohne Prompt-Text.

## Schätzung

**Größe L.** Am Anker gemessen: Die Drehung je Stufe allein wäre ein **S** (eine Funktion an der Kartenquelle, wie
`childId` aus dem Testpfad ziehen, B-01). Die Obergrenze der Vorwärtskarte bei `both` (Entscheidung 3) ist ein **M**:
Sie greift an drei Stellen, die heute nur die Zielstufe kennen – Stufenleiter (`PositionPlayService.CardStage`,
`PositionPlayService.cs:159-166`), Kalender-Frist (`LadderDays`, `:164` und `:196`) und Teststufe – und muss im Bonus
(`ExtraTrainingService.FormFor`) genauso gelten. Das Diktat (Entscheidung 4) ist ein weiteres **S–M** mit Frontend.
Zusammen liegt das an einer Etappe wie E6. Wird es beim Bauen zu groß, ist die Teilungslinie das Diktat: eine eigene
Story, die auf der Drehung aufsetzt.

- **`wo: beides`** – Backend zuerst (Kartenquelle, Obergrenze, Diktat-Karte), dann Vater-Web (Feld „Erkennen“,
  Hinweis, Stufenname „Diktat“ in `exerciseConfig.tsx:117`) und Sohn-Karte (Diktat ohne Text,
  `Lektion.tsx:94-96`).
- **`migration: nein`** – keine Schemaänderung: Richtung, Stufen und Lernstand bleiben, wie sie gespeichert sind
  (Entscheidungen 2 und 6).
- **`vertragsbruch: nein`** – `direction` und seine Werte bleiben (Entscheidung 7). Braucht das Diktat ein Feld für
  den vorzulesenden Text (siehe Risiko 1), ist es **additiv** an `PracticeCard`/`TestItem`, also kein Bruch.

### Risiken

1. ~~**Das Diktat über die Sprachausgabe des Geräts verrät die Lösung im Datenpaket.**~~ → hingenommen, Entscheidung 9. Die Sprachausgabe braucht den
   Text im Client (`SpeakButton text={karte.prompt}`, `Lektion.tsx:96`). Beim Diktat ist dieser Text genau die
   Lösung. Mit Aufnahme wäre es nur eine URL, aber keine der 265 Vokabeln hat eine. Das Kind sieht das Wort nicht,
   im Netzwerk-Tab stünde es aber. Das lag beim Grillen nicht auf dem Tisch. **Vor dem Bau** eine Rückfrage an den
   Vater: hinnehmen (12-Jähriger, Hausgebrauch) oder das Diktat erst mit serverseitig erzeugten Aufnahmen bauen
   (eigene Story, deutlich größer). Die Lösungsfeld-Regel (`ConventionGuardTests`) fängt das nicht, weil das Feld
   nicht `Answer` heißt.
2. **Anzeige und Bewertung auseinander.** Wird die Drehung nur in der Karten-Projektion gemacht, wertet der
   Server-Vergleich noch die ungedrehte Antwort. Gegenmittel: Die Drehung sitzt an der `ContentItem`-Stufe, bevor
   `CardFacets` und `Grader` sie sehen, und ein Test schickt die Lernsprache **und** die Muttersprache.
3. **Die Obergrenze vergisst einen Pfad.** Vier Stellen wählen eine Stufe (Leiter, Frist, Test, Bonus). Gegenmittel:
   eine Funktion „höchste Stufe der Karte“ am Typ, die alle vier fragen, und je Pfad ein Test.
4. **Multiple Choice bei `both`.** Entscheidung 7 aus B-472 rotiert die Knöpfe nach Index. Ändert die Obergrenze, auf
   welcher Stufe Karten stehen, ändert sich die Verteilung nicht – trotzdem im Test prüfen, dass die Position der
   richtigen Antwort nicht die Richtung verrät.
5. **Tests von `both`-Listen werden leichter** (Kosten von Entscheidung 5). Kein Risiko für den Code, aber für die
   Pflicht – im Rollengang ansehen, ob 80 % noch eine echte Hürde sind.

### Angriffsplan

1. **Backend, Drehung:** `IExerciseType` bekommt „Karte für Stufe“ (Vokabeltyp: auf Schreibstufen gedreht, sonst
   unverändert). Alle Spielpfade wenden sie an, bevor Karte und Bewertung entstehen. Tests rot, dann grün.
2. **Backend, Obergrenze:** „höchste Stufe der Karte“ (Vorwärtskarte einer `both`-Liste = Auswahl) in
   `CardStage`, Kalender-Frist, Teststufe und `ExtraTrainingService.FormFor`.
3. **Backend, Diktat** (Risiko 1 hingenommen, Entscheidung 9): Stufe zeigt keinen Text, Vorlesen über Aufnahme oder
   Sprachausgabe, ohne Stimme Freitext. Stufenname „Diktat“ im Manifest.
4. **Kommentar** an `TargetLanguage` (Muttersprache).
5. **Frontend:** Feld „Erkennen“ mit drei Optionen und Hinweis in `exerciseConfig.tsx` und
   `VaterExerciseCreate.tsx`; Diktat-Karte in `Lektion.tsx`; Positions-Bericht beschriftet gedrehte Karten.
6. Reviewer: `pugling-reviewer`, dann `frontend-reviewer`; Rollengang als Sohn an einer `both`- und einer
   `front-to-back`-Liste.

### Testweg

- **Integrationstests** in `Pugling.Api.Tests`: Drehung in Üben und Test an `BeideRichtungenTests` /
  `ContentExercisePlayTests` (rote Probe: ohne Drehung erwartet die Freitext-Karte „gehen“), Bonus an
  `ExtraTrainingTests`, Obergrenze über Leiter und Frist an `KartenstufeTests` (rote Probe: ohne Obergrenze steht
  die Vorwärtskarte nach 7 Tagen auf Freitext), Diktat an `VorlesenTests` (rote Probe: Karte trägt einen Prompt-Text).
- **Unit:** `StageMechanicsTests` für „ist Schreibstufe“ (Auswahl ausdrücklich nicht).
- **Vitest:** Feld „Erkennen“ und Hinweis im Editor, Diktat-Karte ohne Text.
- **E2E:** eine Spec als Sohn, die eine `front-to-back`-Liste auf Freitext spielt und *walk* zu „gehen“ tippt, nach
  dem Muster von `bonus.spec.ts`; dazu `/smoke-test`.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (Richtung vor der Stufe, Stufe je Karte, Tausch ohne
  Audio, Lernstand am Index, drei Vorgaben beim Anlegen), Begriffskollision „Zielsprache“ ↔ `TargetLanguage`
  benannt, acht offene Punkte mit Empfehlung.
- **2026-11-16** — gegrillt mit dem Vater: acht Entscheidungen (Schreibstufen, Drehen je Stufe, Obergrenze der
  Vorwärtskarte bei `both`, Diktat, überall gleich, Boxen bleiben, Feld „Erkennen“, Vorgaben bleiben),
  Akzeptanzkriterien final.
- **2026-11-16** — geschätzt: L, beides, keine Migration, kein Vertragsbruch; Risiko 1 (Diktat verrät die Lösung
  im Datenpaket) braucht vor dem Bau eine Rückfrage beim Vater.
- **2026-11-16** — Risiko 1 vom Vater hingenommen (Entscheidung 9), `wartet_auf` geleert.
- **2026-11-16** — in Arbeit (Sprint 82). **Gebaut** an einer Senke: `IExerciseType.CardCeiling` („höchste Stufe der
  Karte“, Entscheidung 3) und `IExerciseType.CardForStage` („Karte für Stufe“, Entscheidung 2); die gedrehte Form
  (`ContentItem.Written`) baut der `ExerciseContentResolver`, weil nur er die Sprachen kennt – so verzeiht die gedrehte
  Karte den Artikel der Lernsprache (B-421) und trennt Bedeutungen (B-462). Die Vorwärtskarte einer `both`-Liste trägt
  `RecognitionOnly`. Diktat: `Swap` reicht Aufnahme und Sprache als `DictationAudioUrl`/`DictationLang` weiter, die
  Hörstufe gibt sie heraus; ohne Aufnahme trägt die Karte `DictationSpokenAnswer` (Entscheidung 9, so benannt und am
  DTO kommentiert) und als Prompt das deutsche Wort für Geräte ohne Stimme.
- **2026-11-16** — **Aufrufstellen einzeln entschieden** (Auftrag: jede Stelle über `ItemsOfAsync`):
  - `PositionPracticeController` `IntroductionsFirstAsync` – Stufe mit Obergrenze (sortiert Selbsteinschätzungs-Sprossen
    richtig); `Cards`/`Next` über `BuildCard` – Stufe mit Obergrenze, Karte und Geschwister über `AtStage`;
    `ReshuffleImage` – Stufe mit Obergrenze, Bild-Prüfung an der gedrehten Karte (das Bild bleibt beim Drehen, die
    Stufe entscheidet); `Review` – Stufe mit Obergrenze, Bewertung, Tippfehler-Nachbarn, Mengen-Treffer, Historie und
    Lösung im Ergebnis über `AtStage`.
  - `PositionTestsController` `Start` – Zeilenstufe über `TestItemStage` (mit Obergrenze), auch die frei gewählte
    Stufe des Vaters; `Next` – Frage über `AtStage`; `Answer` und Sammel-`Submit` – Bewertung über `CardForStage` der
    Zeilenstufe; Ergebnis-Karten über `AtStage` der Zeilenstufe.
  - `ExtraTrainingService` `BuildRun` – je Karte Obergrenze und Drehung; Bewertung über `ExercisePreviewService.CheckAsync`
    (dort `GradeByIndex` je Karte); `ContentFingerprint` liest die rohen Karten – unverändert richtig, weil die
    Drehung aus denselben Zeilen folgt.
  - `ExercisePreviewService` `BuildAsync`/`Present` und `CheckAsync`/`GradeByIndex` – je Karte; Mengen-Wertung
    (`GradeAsSet`) unverändert, nur Listen werten als Menge, nie Vokabeln.
  - `PositionReportService` – rohe Karte (die Richtung der Übung) plus die Kennzeichen `TurnedWhenWritten` und
    `RecognitionOnly` an `ItemReport` (additiv).
  - `PositionPlayService.DueItemIndicesAsync`, `PositionProgressService` (`:180`, `:228`, `:400`) – zählen nur Karten
    (Poolgröße, Ziel), keine Stufe: unverändert.
- **2026-11-16** — **Folgen am Bestand** (beim Bau sichtbar geworden, keine Entscheidung umgestoßen): 72 Integrationstests
  und 39 KI-Creator-Tests tippten auf Freitext das deutsche Wort und sind nachgezogen. Der Selbsttest des KI-Creators
  erwartet jetzt die Vorderseite (`VocabularyStrategy.ExpectedAnswers`). Deutsche Alternativen (B-65), die Artikel-Tipps
  aus dem Speicher (B-446) und Autor-Tipps einer Zeile gelten jetzt nur noch dort, wo Deutsch die Antwort ist (Auswahl,
  Selbsteinschätzung); die Tests prüfen sie dort. Die Hör-Stufe einer rückwärts gefragten Karte trägt jetzt die Aufnahme
  – als Diktat ist sie die Aufgabe (Entscheidung 4), auf jeder anderen Stufe bleibt sie weg
  (`AntiCheatTests.cs:307`).
- **2026-11-16** — **Belege je Akzeptanzkriterium und Fall** (Testdatei:Zeile in `backend/Pugling.Api.Tests/`):
  - AK 1/2, Üben: `SchreibstufeLernspracheTests.cs:102` (Freitext zeigt „gehen“, „gehen“ falsch, die Lernsprache
    richtig); Artikel der Lernsprache verziehen `ArtikelToleranzTests.cs:47`, `:155`; Bedeutungen der Lernsprache
    getrennt `PositionTestFlowTests.cs:215` (front-to-back-Fall).
  - AK 1/2, Test (Einzel- und Sammelantwort, Ergebnis): `SchreibstufeLernspracheTests.cs:120`.
  - AK 1/2, Bonus: `ExtraTrainingTests.cs:140`. Vorschau: `ExercisePreviewTests.cs:27`, `ArtikelToleranzTests.cs:115`.
  - AK 1, Schreibstufen (Entscheidung 1): `SchreibstufeLernspracheTests.cs:93` (je Stufe, Auswahl ausdrücklich nicht).
  - AK 3, Erkennen folgt der Richtung: Auswahl `BeideRichtungenTests.cs:64`, `:182`; Selbsteinschätzung/Vorlesen
    `VorlesenTests.cs:64`, `:83`; Kennenlernen `PositionPracticeFlowTests.cs:114`.
  - AK 4, Vorwärtskarte bei `both`: Frist `SchreibstufeLernspracheTests.cs:148`, oberstes Fach (Leiter) `:170`, Test
    des Kindes und Stufe des Vaters `:186`, Vorschau `:212`, Bonus `ExtraTrainingTests.cs:169`.
  - AK 5, Diktat: mit Aufnahme ohne Prompt `ContentExercisePlayTests.cs:117`; ohne Aufnahme Fremdwort zum Vorlesen,
    deutsches Wort für Freitext `ContentExercisePlayTests.cs:147`; Aufnahme nur im Diktat `AntiCheatTests.cs:307`.
    Frontend: `frontend/src/sohn/Lektion.test.tsx` (Diktat ohne Text / Rückfall ohne Stimme),
    `frontend/src/sohn/aufgabenform.test.ts` (Diktat nur mit Stimme).
  - AK 6, Zahl und Indizes unverändert: `BeideRichtungenTests.cs:40` (Indizes 0–7), ohne Umschreib-Skript.
  - AK 7, Feld „Erkennen“, Hinweis, „Diktat“: `frontend/src/vater/RecognitionField.test.tsx`; Kommentar an
    `Vocabulary.TargetLanguage`. Bericht: `SchreibstufeLernspracheTests.cs:230`,
    `frontend/src/vater/PlanPositions.test.ts` (`schreibweise`).
- **2026-11-16** — **Rote Probe** nach dem letzten Backend-Edit, `.claude/scripts/mutant-probe.py` (Release, weil die
  E2E-Suite parallel Debug fuhr). Der erste Lauf kollidierte mit dem Test-Tor (paralleler Release-Build, 16 Zeilen
  „kompiliert nicht“, M23 mit zu enger Erwartung); diese 18 Zeilen sind ohne Störung wiederholt, Grundlinie danach
  grün. M22 ist eine äquivalente Mutante: Die Vorwärtskarte bleibt auf einer Schreibstufe ohnehin ungedreht, die
  Bewertung ändert sich nicht – erwartet grün.

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| M1 Drehung weg | `VocabularyExerciseType.cs:100` | red | red |
| M2 Obergrenze weg | `VocabularyExerciseType.cs:93` | red | red |
| M3 Diktat keine Schreibstufe | `VocabularyExerciseType.cs:107` | red | red |
| M4 Diktat liest Kartenaudio | `VocabularyExerciseType.cs:142` | red | red |
| M5 Üben ohne Obergrenze | `PositionPlayService.cs:162` | red | red |
| M6 Test ohne Obergrenze | `PositionPlayService.cs:208` | red | red |
| M7 kein Vorlese-Wort | `PositionPlayService.cs:434` | red | red |
| M8 keine Erkennungskarte | `ExerciseContentProvider.cs:100` | red | red |
| M9 Tausch ohne Diktat-Aufnahme | `ExerciseContentProvider.cs:133` | red | red |
| M10 Tausch ohne Diktat-Sprache | `ExerciseContentProvider.cs:134` | red | red |
| M11 keine gedrehte Form | `ExerciseContentResolver.cs:122` | red | red |
| M12 gedrehte Form ohne Artikel/Bedeutungen | `ExerciseContentResolver.cs:122` | red | red |
| M13 Übungskarte ungedreht | `PositionPracticeController.cs:200` | red | red |
| M14 Üben wertet ungedreht | `PositionPracticeController.cs:418` | red | red |
| M15 Vater-Stufe ohne Obergrenze | `PositionTestsController.cs:190` | red | red |
| M16 Testfrage ungedreht | `PositionTestsController.cs:238` | red | red |
| M17 Einzelantwort ungedreht | `PositionTestsController.cs:272` | red | red |
| M18 Sammelantwort ungedreht | `PositionTestsController.cs:396` | red | red |
| M19 Ergebnis ungedreht | `PositionTestsController.cs:427` | red | red |
| M20 Bonus ohne Obergrenze | `ExtraTrainingService.cs:409` | red | red |
| M21 Bonuskarte ungedreht | `ExtraTrainingService.cs:411` | red | red |
| M22 Vorschau-Wertung ohne Obergrenze | `ExercisePreviewService.cs:88` | green | green |
| M23 Vorschau-Wertung ungedreht | `ExercisePreviewService.cs:90` | red | red |
| M24 Vorschau-Karte ungedreht | `ExercisePreviewService.cs:130` | red | red |
| M25 Vorschau-Karte ohne Obergrenze | `ExercisePreviewService.cs:128` | red | red |
| M26 Bericht ohne Kennzeichen | `PositionReportService.cs:55` | red | red |
| M27 KI-Selbsttest erwartet Rückseite | `VocabularyStrategy.cs:130` | red | red |
| M28 Bonus zahlt den Laufsatz | `ExtraTrainingService.cs:201` | red | red |

- **2026-11-16** — **Reviews.** `pugling-reviewer`: keine Blocker, Release-Build und 1737/1737 grün. Fund 1 (Bonus zahlte bei
  `both` für jede Auswahl-Karte den Tipp-Satz) behoben: Ein Treffer zahlt nach der Form seiner Karte
  (`ExtraTrainingService.CardRatesAsync`), belegt `ExtraTrainingTests.cs:169` und M28 oben. Fund 2 (ein vor der
  Umstellung begonnener Test friert eine Vorwärtskarte auf Freitext ein und fragt sie einmal ungedreht) **bewusst
  hingenommen**: Anzeige und Bewertung bleiben gleich, kein Betrug, und er betrifft nur Versuche, die beim Deploy offen
  sind. Aufräum-Funde: doppelter Stufen-Ausdruck in der Vorschau zusammengefasst; die Kartenliste in
  `IntroductionsFirstAsync` bleibt (zwei Abfragen je Rundenstart), die quadratische `AtStage`-Schleife bei ≤ 50 Karten
  ebenso. `frontend-reviewer`: keine Blocker; Ansage für Screenreader beim Diktat, Pflicht-Parameter der Stimme und
  Kleinkram werden nach dem E2E-Lauf eingearbeitet.
- **2026-11-16** — **Frontend-Review eingearbeitet:** Die Ansage für den Screenreader sagt beim Diktat die Aufgabe,
  nicht das verborgene deutsche Wort (`istDiktat`, eine Stelle für Karte und Ansage); `aufgabenform` verlangt die Stimme
  als Pflicht-Argument; Stufenname „Diktat“/„Dictation“ auch in der Arcade; Feld-Erklärung „Auch richtig“ nach der neuen
  Regel; Label des Felds „Erkennen“ per `htmlFor`. Bewusst gelassen: Auf der ersten Diktatkarte blitzt das deutsche Wort
  kurz auf, bis Chrome die Stimmen nachgeladen hat (keine Lösung, im Code kommentiert). Die Verdrahtung der Ansage in den
  drei Bildschirmen hat keinen eigenen Vitest – sie ist eine Zeile über `istDiktat`, das getestet ist.
- **2026-11-16** — **Verifikation:** `dotnet build Pugling.sln` und `dotnet format Pugling.sln` sauber;
  `dotnet test Pugling.sln -c Release`: **1737 passed**; Vitest **1133 passed** (102 Dateien); `tsc -b` sauber.
  E2E (Ausweichports 5201/5174): voller Lauf **149 passed, 11 failed**; neun davon tippten das deutsche Wort
  (nachgezogen: `belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel-voll`, `buchstaben-ordnen`, `erfolge-woche-frist`,
  `erstbegegnung`, `pfad`, `truhe`, `vater-zeitachse`), zwei liefen, während ich Spec-Dateien änderte und Vite neu lud.
  Wiederholung der elf: **22 passed, 1 failed** – `feldhilfe.spec.ts:32` (Popover „Ziel-Rhythmus“, vom Diff nicht
  berührt) fällt einzeln in einem von drei Läufen und ist als
  [B-602](B-602-feldhilfe-ziel-rhythmus-flackert.md) abgelegt. Abnahme wartet auf den Rollengang am Sprint-Ende.
- **2026-11-16** — abgenommen in Sprint 82 (9602db8d): Rollengang `frontend/e2e/sprint-82-rollengang.spec.ts` **5 passed** gegen frisch gestarteten Server (Ausweichports), volle Suiten am Endstand: `dotnet test` 1772 passed, Vitest 1144 passed, Playwright **166 passed**; Protokoll `docs/pm-sitzung-2026-11-16-backlog-lauf.md` (Runde 82). Geräteprüfung offen: Klang des Diktats am Handy.
