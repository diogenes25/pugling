---
tags: [typ/story, status/gegrillt, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Zielsprache tippen, Tipprichtung, Lernsprache schreiben]
status: gegrillt
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (Richtung vor der Stufe, Stufe je Karte, Tausch ohne
  Audio, Lernstand am Index, drei Vorgaben beim Anlegen), Begriffskollision „Zielsprache“ ↔ `TargetLanguage`
  benannt, acht offene Punkte mit Empfehlung.
- **2026-11-16** — gegrillt mit dem Vater: acht Entscheidungen (Schreibstufen, Drehen je Stufe, Obergrenze der
  Vorwärtskarte bei `both`, Diktat, überall gleich, Boxen bleiben, Feld „Erkennen“, Vorgaben bleiben),
  Akzeptanzkriterien final.
