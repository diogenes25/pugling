---
tags: [typ/story, status/gegrillt]
aliases: []
status: gegrillt
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-639 · Dezimalkomma im Zahlenvergleich der Rechenaufgaben

Eine Rechenaufgabe mit der Lösung 2,5 wertet die Antwort „2,5“ als falsch – auf beiden Wertungspfaden, aus zwei
verschiedenen Gründen. Fällt heute nicht auf, weil die Seed-Rechenaufgaben ganzzahlig sind
(`backend/Pugling.Api/Data/Seed.cs:1292`); mit Mathe Klasse 9 (gerundete Lösungen wie −0,4 und 2,3) wird es zur Regel.

## User Story

Als Schüler möchte ich eine Dezimalzahl so eintippen, wie ich sie in der Schule schreibe (mit Komma), damit eine
richtige Rechnung nicht an der Schreibweise scheitert.

## Ist-Stand am Code

Zwei Pfade werten eine Zahlenantwort, und sie tun es verschieden:

1. **Katalog-Check** (`Check`, Testmodus des Vaters und Rechen-Drill): `AnswerChecking.NumericMatch`
   (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:669`) parst mit `NumberStyles.Number` und
   `CultureInfo.InvariantCulture`. Dort ist das Komma ein Tausendertrenner. Nachgestellt am 2026-11-17 mit genau
   diesem Aufruf: „2,5“ → 25, „0,6“ → 6, „−0,4“ → −4 – die Eingabe wird nicht abgelehnt, sondern **als andere Zahl
   gelesen**. Aufrufer: `ArithmeticExerciseType.Check` (Zeile 484) und `CheckGenerated` (Zeile 694).
2. **Spielen in einer Position** (Üben/Test): `ArithmeticExerciseType.ItemsOf` legt die Lösung als Text in
   Invariant-Schreibweise ab (`BuiltInExerciseTypes.cs:471`, also „2.5“), und gewertet wird über
   `AnswerGrader.Matches` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:26`) per **Textvergleich** nach
   `StageMechanics.Normalize`. „2,5“ ≠ „2.5“. Am Code gelesen, nicht per Test nachgestellt.

Im Frontend wandelt nichts ein Komma um (keine Fundstelle unter `frontend/src`).

## Die echte Lücke

Breiter als die Idee vermutete – das Komma ist nur das sichtbarste Symptom:

- Pfad 1 liest das Komma als Tausendertrenner und vergleicht gegen eine falsche Zahl.
- Pfad 2 vergleicht gar nicht numerisch. Damit scheitert dort nicht nur „2,5“, sondern auch „2.50“ oder „+3“, und
  die **`Tolerance` der Aufgabe wirkt beim Spielen nicht** – sie gilt nur im Katalog-Check. Eine Aufgabe, die im
  Testmodus des Vaters als richtig durchgeht, kann beim Kind falsch sein.

## Offene Punkte

1. ~~**Was ist „1.234“?**~~ → Entscheidung 1.
2. ~~**Ein numerischer Vergleich für beide Pfade?**~~ → Entscheidung 2.
3. ~~**Gehört die Toleranz beim Spielen dazu oder in eine eigene Story?**~~ Gegenstandslos: sie folgt aus
   Entscheidung 2 – dieselbe Regel auf beiden Pfaden schließt die Toleranz ein.

## Entscheidungen

1. **Punkt und Komma sind beide Dezimaltrenner; Tausendertrenner gibt es nicht.** „1.234“ ist 1,234. Begründung:
   Schulaufgaben kommen ohne Tausendertrenner aus, und jede Mehrdeutigkeit entfällt – auch die vom Taschenrechner
   abgetippte „2.5“ stimmt. Kosten: wer „1.000“ für tausend tippt, liegt falsch.
2. **Eine Zahlenantwort wird auf beiden Pfaden numerisch verglichen, nach einer Regel, die der Übungstyp vorgibt.**
   Begründung: was im Testmodus des Vaters richtig ist, muss beim Kind richtig sein; „2.50“, „+3“ und „2,5“ sind
   dieselbe Zahl. Folge: die `Tolerance` der Aufgabe wirkt auch beim Spielen. Kosten: Eingriff in den geteilten
   `AnswerGrader`, den alle Typen nutzen – größere Änderung als eine Komma-Ersetzung, und die Regressionstests der
   Text-Typen müssen unverändert grün bleiben.

## Akzeptanzkriterien

1. Eine Rechenaufgabe mit Lösung 2,5 wertet „2,5“, „2.5“ und „2.50“ als richtig – im Katalog-Check und beim Spielen
   in einer Position.
2. „25“ ist für dieselbe Aufgabe auf beiden Pfaden falsch (die rote Probe: heute ist „2,5“ im Katalog-Check genau
   diese 25).
3. Eine Aufgabe mit Lösung 1,234 wertet „1.234“ als richtig und „1234“ als falsch.
4. Eine Aufgabe mit Toleranz 0,05 und Lösung 2,33 wertet „2,3“ beim Spielen als richtig, „2,2“ als falsch.
5. Eine Aufgabe mit Lösung 3 wertet „+3“ als richtig; eine leere Eingabe und „drei“ bleiben falsch.
6. Der Rechen-Drill wertet eine gerundete Lösung mit Komma wie mit Punkt.
7. Die Text-Typen (Vokabel, Lückentext, Liste) vergleichen unverändert – „2,5“ als Vokabel-Antwort wird nicht zur Zahl.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-17** — ausformuliert: Parse-Verhalten nachgestellt („2,5“ → 25), zweiter Pfad über `AnswerGrader` am Code
  gelesen; Lücke ist breiter (Textvergleich und wirkungslose Toleranz beim Spielen).
- **2026-11-17** — gegrillt (Dialog): beide Trennzeichen dezimal, kein Tausendertrenner; ein numerischer Vergleich
  für beide Pfade, Toleranz damit auch beim Spielen. Akzeptanzkriterien final (7).
