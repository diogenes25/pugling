---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, rolle/supervisor, lerntechnik/lueckentext]
aliases: [Lückentext-Eingabearten, Buchstabenkästen im Lückentext, Auswahl je Lücke]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater, Beispiel Question tags)
grund: ""
ersetzt_durch: []
---

# B-599 · Lückentext: dieselben Eingabearten wie bei Vokabeln

## User Story

Als **Vater** möchte ich bei einem Lückentext wählen, **wie** mein Kind die Lücke füllt – Buchstabenkästen (Länge
bekannt), Buchstabensalat, Auswahl aus der richtigen und ähnlichen Formen für genau diese Lücke, oder Freitext –,
damit ich Grammatik wie die Question tags („He walks away, ___?“) erst mit Hilfe und dann frei üben lassen kann,
so wie bei Vokabelkarten.

## Ist-Stand am Code

- **Der Lückentext kennt vier Stufen.** `ClozeStage` (`ClozeEntities.cs:7-24`): Übersetzung + Wortbank,
  Übersetzung + Freitext, Freitext, Wortkasten + Freitext (B-524). Das Manifest bietet genau diese vier an
  (`BuiltInExerciseTypes.cs:305-311`). Alle vier sind geprüfte Stufen (`StageMechanics.cs:27-29`). Vorgabe ist die
  Wortbank (`BuiltInExerciseTypes.cs:208`), die Vorschau spielt Übersetzung + Freitext (`:210`).
- **Die Auswahl ist eine gemeinsame Wortbank, nicht eine Auswahl je Lücke.** `ClozeExerciseType.Choices`
  (`BuiltInExerciseTypes.cs:240-260`) gibt auf der Wortbank-Stufe die ganze, alphabetisch sortierte Bank der Übung
  heraus, für jede Lücke dieselbe (`ChoicesShareOnePool => true`, `:189`; ausgewertet in
  `PositionPlayService.cs:417`). Ohne Bank (`ClozeConfig.WordBank`, `ExerciseConfigs.cs:78`) ist die Stufe Freitext.
- **Eine Lücke trägt keine eigenen Ablenker.** `Gap(Index, Answer, Alternatives, VocabKey, Form)`
  (`ExerciseConfigs.cs:98-99`). Eigene Optionen je Frage gibt es nur beim Leseverständnis (`Question.Choices`,
  `ExerciseConfigs.cs:14`; `AnswerChecking.ChoicesOf`, `BuiltInExerciseTypes.cs:553ff`).
- **Die Bausteine für Kästen und Salat sind typ-neutral.** Der Basistyp hat die Haken `StageFacets` (Länge und
  Muster der Kästen), `LetterTileSource` (Salat) und `Choices` (`ExerciseTypeBase.cs:52-62`). Der Vokabeltyp füllt
  sie (`VocabularyExerciseType.cs:104`, `:118-122`, `:128-133`), der Lückentext nicht. Die Mechanik dahinter ist
  geteilt: Kästen-Muster mit Trennzeichen (`StageMechanics.LetterBoxPattern`, `StageMechanics.cs:96`, B-66),
  Salat nur für **ein** Wort aus 2–12 Buchstaben oder Ziffern (`StageMechanics.cs:104-108`), Auswahl aus den
  Antworten der anderen Einträge (`StageMechanics.DistractorPool`, `StageMechanics.cs:177`).
- **Die Sohn-Karte zeichnet das schon für jeden Typ.** `Lektion.tsx:154-167`: `choices` → Knöpfe, `letterTiles` →
  Salat, `answerLength` + `answerPattern` → Kästen. Die Karte fragt nicht nach dem Typ.
- **Die Stufenwahl läuft für jeden Typ über das Manifest.** Lehrplan-Station: `PlanPositions.tsx:827` liest die
  Stufen aus `types.stages(...)`; Stufenplan je Tag über `StageSchedule` (`PositionPlayService.StageForDay`,
  `PositionPlayService.cs:73`). Eine neue `ClozeStage` erscheint dort ohne Frontend-Arbeit. **Aber:** Im
  Creator-Formular gibt es die Standard-Abfrageform nur für Vokabeln (`ExerciseEditModal.tsx:440-447`).
- **Eine Lücke klettert nicht.** `CardStage` gibt für jeden Typ außer Vokabeln die Zielstufe zurück
  (`ExerciseTypeBase.cs:49`); der Lückentext hat keinen Lernstand je Lücke.
- **Bonus-Training:** Eine Stufe mit Optionen zahlt wie Auswahl (1 Münze), sonst wie Tippen (3)
  (`ExtraTrainingService.cs:387`). Der Salat hat keine `Choices` und würde damit 3 Münzen zahlen.
- **Echte DB** (read-only, 2026-11-16): 21 Lückentexte, 7 mit Wortbank, keiner mit eigener Standardstufe, 7 als
  Lehrplan-Station verplant. Die Question-Tags-Übung Nr. 19 hat keine Bank und wird darum als Freitext gespielt.
- **Anlege-Wege:** Der KI-Creator schreibt Ablenker für eine gemeinsame Bank (`ClozeStrategy.cs:21`, mindestens 2),
  `laden.py` kennt `wordBank` und `cueDistractors` (`laden.py:41`). Keiner setzt eine Stufe.

## Die echte Lücke

Es fehlen **drei Lückentext-Stufen** und **eine Auswahl je Lücke**. Fast alles darunter ist da: die Haken am
Basistyp, die geteilte Mechanik, die Darstellung beim Sohn und die Stufenwahl über das Manifest. Neu sind die
Stufenwerte, die drei Haken im Lückentext, die Herkunft der Ablenker je Lücke und das Stufenfeld im Creator-Formular.

Nach [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md) ist beim Lückentext nichts zu drehen: Die Lösung einer
Lücke steht schon in der Lernsprache.

## Offene Punkte

1. **Welche Stufen kommen dazu?** Empfehlung: drei, je mit der Übersetzung als Hinweis wie die heutigen
   „Übersetzung + …“-Stufen: **Buchstabenkästen**, **Buchstabensalat**, **Auswahl je Lücke**. Eine Lücke ohne
   Übersetzung (Question tags) zeigt dann einfach keinen Hinweis.
2. **Woher kommen die Ablenker der Auswahl je Lücke?** Optionen: (a) die Antworten der anderen Lücken der Übung, wie
   bei Vokabeln (`DistractorPool`); (b) eigene Ablenker je Lücke, die der Autor schreibt (neues optionales Feld an
   `Gap`); (c) aus der gemeinsamen Wortbank. Empfehlung: **(a) als Standard, (b) als Ergänzung**. Für Question tags
   sind die anderen Lücken genau die richtigen Ablenker („isn't she“, „can't you“). Wo der Autor ähnliche Formen will
   („didn't he“ neben „doesn't he“), schreibt er sie an die Lücke. Das neue Feld ist additiv im Vertrag.
3. **Wie viele Optionen?** Empfehlung: richtig + bis zu 4 wie bei der Verb-Übung Nr. 67, nicht 3 wie bei Vokabeln.
   Bei weniger Lücken und ohne eigene Ablenker entsprechend weniger, mindestens aber 2 Ablenker; sonst fällt die Karte
   auf Kästen zurück.
4. **Buchstabensalat bei mehreren Wörtern:** „didn't he“ kann die heutige Mechanik nicht mischen (nur ein Wort,
   2–12 Zeichen). Empfehlung: wie bei Vokabeln **auf Kästen zurückfallen**, Karte für Karte (B-05). Ein Salat aus
   ganzen Wörtern wäre eine eigene Story.
5. **Stufenwahl im Creator-Formular:** Empfehlung: das Feld „Standard-Abfrageform“ auch für den Lückentext zeigen,
   mit den Stufen aus dem Manifest statt einer eigenen Liste im Frontend. Lehrplan-Station und Stufenplan je Tag
   funktionieren dann ohne weitere Arbeit.
6. **Klettern je Lücke wie bei Vokabeln?** Empfehlung: **nein**, nicht in dieser Story. Der Lückentext hat keinen
   Lernstand je Lücke; der Weg von leicht nach schwer läuft über den Stufenplan je Tag.
7. **Münzen im Bonus:** Empfehlung: Auswahl und Salat zahlen wie Auswahl (1 Münze), Kästen wie Tippen (3). Der Salat
   ist Ordnen, nicht Erinnern; ohne Regel würde er heute 3 zahlen (`ExtraTrainingService.cs:387`).
8. **Test und Klausur:** Gilt die gewählte Stufe dort genauso? Empfehlung: ja, über dieselbe Kartenquelle. Die
   Klausur-Stufe legt die Lehrplan-Station fest wie heute.
9. **Regelaufgaben und Übersetzung**, die nur Freitext kennen: Empfehlung: **nicht in dieser Story.** Beide haben
   gar keine Stufen; das wäre ein zweiter Typ mit eigener Frage. Eigene Idee-Story anlegen, wenn gewünscht.
10. **Anlege-Wege:** Brauchen KI-Creator und `laden.py` eine Änderung? Empfehlung: nein für die Stufen; die
    optionalen Ablenker je Lücke (Punkt 2b) lernen sie erst, wenn sie gebraucht werden.

## Akzeptanzkriterien (Entwurf)

- [ ] Ein Lückentext lässt sich in Creator-Formular, Lehrplan-Station und Stufenplan auf Buchstabenkästen,
  Buchstabensalat oder Auswahl je Lücke stellen.
- [ ] Buchstabenkästen zeigen Länge und Trennzeichen der Lösung („didn't he“ → Kästen mit Apostroph und Lücke).
- [ ] Buchstabensalat mischt die Buchstaben eines Worts; eine Lösung aus mehreren Wörtern wird als Kästen gespielt.
- [ ] Die Auswahl je Lücke zeigt die Lösung dieser Lücke und Ablenker nach Punkt 2, ohne dass die Position der
  richtigen Antwort sie verrät.
- [ ] Bewertung, Test, Klausur und Bonus behandeln die neuen Stufen wie die alten; Münzen nach Punkt 7.
- [ ] Die abgenommenen Stories B-489 (Wortbank am Handy) und B-486 (Lückentext im Testmodus) bleiben grün.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Eingabearten (Beispiel Question tags, Übung Nr. 19).
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (vier Stufen, gemeinsame Bank, typ-neutrale Haken und
  Darstellung, Stufenwahl über das Manifest, Creator-Formular nur für Vokabeln), zehn offene Punkte mit Empfehlung.
  Im Refinement für Sprint 82.
