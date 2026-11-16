---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Zielsprache tippen, Tipprichtung, Lernsprache schreiben]
status: ausformuliert
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

1. **Welche Stufen zählen als „Schreiben“?** Empfehlung: Buchstabenkästen, Freitext, Buchstabensalat und Hören.
   Auswahl bleibt bei der Richtung der Übung. Begründung: Die Regel des Vaters spricht vom Schreiben, und „walk →
   was heißt das?“ als Auswahl übt Wiedererkennen, das auch gebraucht wird.
2. **Wie wird die Regel umgesetzt?** Empfehlung: **beim Bauen der Karte je Stufe drehen**. Die Kartenliste bleibt
   wie heute (gleiche Zahl, gleiche Indizes), und eine Funktion „Karte für Stufe“ dreht eine vorwärts liegende Karte
   um, wenn die Stufe eine Schreibstufe ist. Anzeige und Bewertung nutzen dieselbe Funktion. Keine Datenumschreibung.
   Die Alternative (Richtung überall fest auf „rückwärts“ setzen) bräuchte für `both` wieder ein Umschreib-Skript wie
   B-472 und nähme dem Kind das Wiedererkennen.
3. **Was wird bei `both` aus der Vorwärtskarte auf einer Schreibstufe?** Nach Punkt 2 würden dann beide Karten eines
   Worts dasselbe abfragen. Optionen: (a) hinnehmen, das Wort wird doppelt geschrieben; (b) die Vorwärtskarte klettert
   nur bis Auswahl und wird nie geschrieben. Empfehlung: **(b)**. Die zwei Karten bekommen dann zwei Aufgaben
   (erkennen und schreiben), wie B-472 sie gemeint hat. Kosten: Die Stufenleiter und die Kalender-Frist
   (`LadderDays`) brauchen eine Obergrenze je Karte. Das ist der teuerste Teil der Story.
4. **Hören:** Nach der Regel hört das Kind das Fremdwort und schreibt das Fremdwort, also ein **Diktat**. Empfehlung:
   genau so, denn Schreiben nach Gehör ist wertvoll. Dann darf die Karte das Wort nicht als Text zeigen, nur Audio
   bzw. Vorlesen. Wo es weder Aufnahme noch Vorlesesprache gibt, fällt die Karte auf Freitext zurück.
5. **Test und Bonus-Training:** Gilt die Regel dort genauso? Empfehlung: ja. Sie laufen über dieselbe Kartenquelle,
   und ein Test, der anderes prüft als das Üben, wäre unfair.
6. **Vorhandener Lernstand:** Karten, die bisher auf Deutsch geschrieben wurden, stehen in Leitner-Boxen, die mit
   Deutsch verdient wurden. Empfehlung: Boxen **nicht** zurücksetzen. Die Stufenleiter und der nächste Test zeigen,
   ob das Wort in der Lernsprache sitzt, und ein Rücksetzen würde verdiente Arbeit wegnehmen.
7. **Benennung im Editor und im Speicher:** Empfehlung: im Vater-Web die Abfragerichtung umbeschriften („Erkennen:
   Fremdwort → Deutsch“) und einen festen Hinweis zeigen, dass immer die Lernsprache geschrieben wird. Die Felder
   `SourceLanguage`/`TargetLanguage` bleiben (Umbenennung wäre ein Vertragsbruch ohne Nutzen für das Kind).
8. **Vorgaben beim Anlegen:** Sollen KI-Creator (`front-to-back`) und `laden.py` (`both`) eine gemeinsame Vorgabe
   bekommen? Empfehlung: nach dieser Story nicht mehr nötig, weil die Richtung nur noch das Erkennen steuert. Bleibt
   offen, falls Punkt 3 anders entschieden wird.

## Akzeptanzkriterien

- [ ] Auf jeder Schreibstufe (Liste nach Punkt 1) zeigt jede Vokabelkarte das deutsche Wort und wertet nur die
  Lernsprache als richtig. Das gilt in Üben, Test, Bonus-Training und Vorschau, unabhängig von der Abfragerichtung.
- [ ] Die Bewertung nutzt dieselbe Drehung wie die Anzeige: Wer *walk* zu „gehen“ tippt, ist richtig; wer „gehen“
  tippt, ist falsch.
- [ ] Kartenzahl, Kartenindizes und gespeicherter Lernstand bleiben unverändert (kein Umschreib-Skript).
- [ ] Auf Stufen ohne Schreiben (Kennenlernen, Selbsteinschätzung, Auswahl nach Punkt 1) gilt die Abfragerichtung
  der Übung wie heute.
- [ ] Integrationstest je Spielpfad mit einer `front-to-back`-Übung auf Freitext. Rote Probe: Ohne die Drehung
  erwartet die Karte „gehen“, der Test fällt.
- [ ] Das Vater-Web sagt beim Anlegen einer Vokabelübung, dass immer die Lernsprache geschrieben wird.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (Richtung vor der Stufe, Stufe je Karte, Tausch ohne
  Audio, Lernstand am Index, drei Vorgaben beim Anlegen), Begriffskollision „Zielsprache“ ↔ `TargetLanguage`
  benannt, acht offene Punkte mit Empfehlung.
