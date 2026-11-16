---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/student, rolle/supervisor, lerntechnik/lueckentext]
aliases: [Lückentext-Eingabearten, Buchstabenkästen im Lückentext, Auswahl je Lücke]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
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

1. ~~**Welche Stufen kommen dazu?**~~ → Entscheidung 1. Empfehlung: drei, je mit der Übersetzung als Hinweis wie die heutigen
   „Übersetzung + …“-Stufen: **Buchstabenkästen**, **Buchstabensalat**, **Auswahl je Lücke**. Eine Lücke ohne
   Übersetzung (Question tags) zeigt dann einfach keinen Hinweis.
2. ~~**Woher kommen die Ablenker der Auswahl je Lücke?**~~ → Entscheidung 2. Optionen: (a) die Antworten der anderen Lücken der Übung, wie
   bei Vokabeln (`DistractorPool`); (b) eigene Ablenker je Lücke, die der Autor schreibt (neues optionales Feld an
   `Gap`); (c) aus der gemeinsamen Wortbank. Empfehlung: **(a) als Standard, (b) als Ergänzung**. Für Question tags
   sind die anderen Lücken genau die richtigen Ablenker („isn't she“, „can't you“). Wo der Autor ähnliche Formen will
   („didn't he“ neben „doesn't he“), schreibt er sie an die Lücke. Das neue Feld ist additiv im Vertrag.
3. ~~**Wie viele Optionen?**~~ → Entscheidung 3. Empfehlung: richtig + bis zu 4 wie bei der Verb-Übung Nr. 67, nicht 3 wie bei Vokabeln.
   Bei weniger Lücken und ohne eigene Ablenker entsprechend weniger, mindestens aber 2 Ablenker; sonst fällt die Karte
   auf Kästen zurück.
4. ~~**Buchstabensalat bei mehreren Wörtern:**~~ → Entscheidung 4. „didn't he“ kann die heutige Mechanik nicht mischen (nur ein Wort,
   2–12 Zeichen). Empfehlung: wie bei Vokabeln **auf Kästen zurückfallen**, Karte für Karte (B-05). Ein Salat aus
   ganzen Wörtern wäre eine eigene Story.
5. ~~**Stufenwahl im Creator-Formular:**~~ → Entscheidung 5. Empfehlung: das Feld „Standard-Abfrageform“ auch für den Lückentext zeigen,
   mit den Stufen aus dem Manifest statt einer eigenen Liste im Frontend. Lehrplan-Station und Stufenplan je Tag
   funktionieren dann ohne weitere Arbeit.
6. ~~**Klettern je Lücke wie bei Vokabeln?**~~ → Entscheidung 6. Empfehlung: **nein**, nicht in dieser Story. Der Lückentext hat keinen
   Lernstand je Lücke; der Weg von leicht nach schwer läuft über den Stufenplan je Tag.
7. ~~**Münzen im Bonus:**~~ → Entscheidung 7. Empfehlung: Auswahl und Salat zahlen wie Auswahl (1 Münze), Kästen wie Tippen (3). Der Salat
   ist Ordnen, nicht Erinnern; ohne Regel würde er heute 3 zahlen (`ExtraTrainingService.cs:387`).
8. ~~**Test und Klausur:**~~ → Entscheidung 8. Gilt die gewählte Stufe dort genauso? Empfehlung: ja, über dieselbe Kartenquelle. Die
   Klausur-Stufe legt die Lehrplan-Station fest wie heute.
9. ~~**Regelaufgaben und Übersetzung**~~ → Entscheidung 9., die nur Freitext kennen: Empfehlung: **nicht in dieser Story.** Beide haben
   gar keine Stufen; das wäre ein zweiter Typ mit eigener Frage. Eigene Idee-Story anlegen, wenn gewünscht.
10. ~~**Anlege-Wege:**~~ → Entscheidung 10. Brauchen KI-Creator und `laden.py` eine Änderung? Empfehlung: nein für die Stufen; die
    optionalen Ablenker je Lücke (Punkt 2b) lernen sie erst, wenn sie gebraucht werden.

## Entscheidungen

Gegrillt mit dem Vater am 2026-11-16, eine Frage nach der anderen.

1. **Drei neue Stufen: Buchstabenkästen, Buchstabensalat, Auswahl je Lücke**, jede mit der Übersetzung als Hinweis,
   wo es eine gibt. *Begründung:* dieselbe Treppe wie bei Vokabeln (auswählen, ordnen, Kästen, frei); eine
   Grammatik-Lücke ohne Übersetzung zeigt einfach keinen Hinweis. *Kosten:* drei neue `ClozeStage`-Werte, die Liste
   an der Lehrplan-Station wächst von 4 auf 7.
2. **Ablenker aus den anderen Lücken; eigene Ablenker an der Lücke haben Vorrang.** Neues optionales Feld an `Gap`.
   *Begründung:* Jeder vorhandene Lückentext (auch Nr. 19) ist sofort spielbar; wo die feinen Unterschiede zählen
   (*doesn't she* gegen *don't she*), schreibt sie der Autor an die Lücke – „1 richtig + 4 ähnliche“ wie bei Nr. 67.
   *Kosten:* additives Feld im Vertrag (die Config ist JSON, keine Migration), Eingabe im Creator-Formular;
   abgeleitete Ablenker können offensichtlich falsch sein.
3. **Richtig + bis zu 4 Ablenker; unter 2 Ablenkern wird die Karte als Buchstabenkästen gespielt.** *Begründung:*
   Vorgabe des Vaters (wie Nr. 67); eine 50:50-Auswahl wäre Raten; der Rückfall folgt dem Muster aus B-05.
   *Kosten:* 5 Knöpfe brauchen am Handy Platz – B-489 ist Regressionszeuge, der Rollengang prüft es.
4. **Ein Salat, den die Mechanik nicht mischen kann (mehrere Wörter, nicht 2–12 Zeichen), fällt Karte für Karte auf
   Kästen zurück**; die Feld-Erklärung im Editor sagt das. *Begründung:* erprobte Regel ohne neue Mechanik, die Kästen
   geben dieselbe Hilfe. *Kosten:* Bei Question tags ist der Salat praktisch wirkungslos; ein Wort-Salat wäre eine
   eigene Story.
5. **Die „Standard-Abfrageform“ gibt es auch beim Lückentext, ihre Liste kommt aus dem Server-Manifest; das
   Vokabelfeld zieht mit.** *Begründung:* Der Creator legt die passende Eingabeart gleich mit an, der Vater
   überstimmt je Station; Frontend-Regel „Übungstypen aus dem Server-Manifest“. *Kosten:* `VOCAB_FORMS`
   (`exerciseConfig.tsx:109`) entfällt zugunsten des Manifests – ein kleiner Umbau mit eigenem Testfall.
6. **Kein Klettern je Lücke;** von leicht nach schwer führt der Stufenplan je Tag. *Begründung:* Der Stufenplan ist
   da und typ-neutral; Klettern je Lücke bräuchte Lernstand je Lücke, ein Umbau wie B-472. *Kosten:* Eine sichere
   Lücke wird so lange mit Hilfe gefragt, wie der Plan es sagt, eine schwere zum Plan-Termin frei.
7. **Bonus-Münzen: Auswahl und Salat 1, Kästen 3; der Salat zählt bei Lückentext und Vokabeln wie Auswahl.**
   *Begründung:* Salat ist Ordnen, nicht Erinnern; mit 3 Münzen wäre er der bequemste Weg zu vollen Münzen.
   *Kosten:* eine Regel mehr in `ExtraTrainingService.FormFor`; eine auf Kästen zurückgefallene Salat-Karte zahlt 3.
   Für Vokabeln greift die Regel heute nicht, weil das Bonus-Training ihnen keinen Salat stellt
   (`ExtraTrainingService.cs:376-379`) – sie ist als einheitliche Regel festgelegt.
8. **Test und Klausur spielen die neuen Stufen wie das Üben; die Stufe legt die Lehrplan-Station fest.**
   *Begründung:* Ein Test, der eine geübte Eingabeart nicht kennt, wäre ein Bruch; die Zielstufe bleibt die Wahl des
   Vaters. *Kosten:* Eine Station auf Auswahl macht ihren Test leichter – wie heute bei der Wortbank.
9. **Regelaufgaben und Übersetzung nicht hier**, sondern in
   [B-600](B-600-eingabearten-fuer-regelaufgaben-und-uebersetzung.md). *Begründung:* Dort fehlt das ganze
   Stufen-Gerüst, und ganze Sätze passen nicht in Kästen. *Kosten:* Auswahl gibt es bis dahin nur über Lückentext
   oder Leseverständnis (wie Nr. 67).
10. **KI-Creator und `laden.py` bleiben unverändert;** eigene Ablenker je Lücke durch den KI-Creator sind die
    Folge-Idee [B-601](B-601-ki-creator-schreibt-ablenker-je-luecke.md). *Begründung:* Die Stufen funktionieren mit
    jedem vorhandenen Lückentext. *Kosten:* KI-Lückentexte bekommen feine Ablenker erst durch Nachpflege.

## Akzeptanzkriterien

- [ ] Ein Lückentext lässt sich im Creator-Formular (Standard-Abfrageform), an der Lehrplan-Station und im
  Stufenplan je Tag auf Buchstabenkästen, Buchstabensalat oder Auswahl je Lücke stellen; die Listen kommen aus dem
  Manifest, auch die der Vokabeln.
- [ ] Alle drei Stufen zeigen die Übersetzung der Lücke als Hinweis, wo es eine gibt, sonst keinen.
- [ ] Buchstabenkästen zeigen Länge und Trennzeichen der Lösung („didn't he“ → Kästen mit Apostroph und Lücke).
- [ ] Buchstabensalat mischt die Buchstaben eines Worts (2–12 Zeichen); jede andere Lösung wird als Kästen gespielt.
- [ ] Die Auswahl je Lücke zeigt die Lösung und bis zu 4 Ablenker: die eigenen der Lücke, sonst Antworten anderer
  Lücken; die Position der richtigen Antwort verrät nichts. Unter 2 Ablenkern wird die Karte als Kästen gespielt.
- [ ] Eine Lücke trägt optional eigene Ablenker (additives Feld), das Creator-Formular kann sie pflegen.
- [ ] Test und Klausur spielen die Stufe der Station; die Bewertung ist dieselbe wie beim Üben.
- [ ] Im Bonus zahlen Auswahl und Salat 1 Münze je Treffer, Kästen 3; das gilt für Lückentext und Vokabeln.
- [ ] B-489 (Wortbank am Handy) und B-486 (Lückentext im Testmodus) bleiben grün.

## Schätzung

**Größe M**, am Anker B-03 (vokabel-basierter Batch-Pfad im `MediaSelector`): ein Typ bekommt bekannte Haken neu
gefüllt, dazu eine Regel im Bonus und ein Formularfeld. Neue Mechanik entsteht nicht – Kästen-Muster, Salat und
Ablenker-Pool sind geteilt (`StageMechanics.cs:96`, `:104-108`, `:177`), und die Sohn-Karte zeichnet alle drei
schon (`Lektion.tsx:154-167`).

- **`wo: beides`** – Backend zuerst (Stufen, Haken, Ablenker je Lücke, Bonus-Regel), dann Vater-Web
  (Standard-Abfrageform aus dem Manifest, Ablenker-Eingabe je Lücke, Feld-Erklärung).
- **`migration: nein`** – Stufen liegen als Zahl an Station und Übung (`PlanPositionEntities.cs:32`,
  `LearnEntities.cs:109`, Stufenplan als JSON `:135`); neue `ClozeStage`-Werte werden angehängt, Bestandswerte
  bleiben. Die Ablenker je Lücke leben in der Config (JSON), keine Spalte.
- **`vertragsbruch: nein`** – `Gap` bekommt ein **optionales** Feld (`ExerciseConfigs.cs:98-99`), additiv. Die
  Stufenliste kommt als Daten aus dem Manifest. `ClozeStage` liegt nicht im Vertragsprojekt.

### Risiken

1. **Die Auswahl je Lücke wird nicht gemischt.** `ChoicesShareOnePool` ist für den Lückentext typweit `true`
   (`BuiltInExerciseTypes.cs:189`); dann gibt `PositionPlayService.cs:417` die Optionen **ungemischt** heraus. Für
   die gemeinsame Wortbank ist das gewollt (sortiert, auf jeder Karte gleich), für die neue Stufe stünde die
   richtige Antwort immer vorn. Gegenmittel: der Schalter wird stufenabhängig; Test mit roter Probe auf die Position
   der Lösung.
2. **Die Form der Lösung verrät sie.** Die Wortbank kennt dafür schon Regeln (Großschreibung am Satzanfang B-501,
   Schlusszeichen, Vollständigkeit B-487). Eine Auswahl je Lücke mit „Doesn't she“ neben „isn't it“ hätte dasselbe
   Leck. Gegenmittel: dieselbe Angleichung (`InBankForm`) auf die Optionen je Lücke anwenden.
3. **Doppelte Ablenker.** Zwei Lücken mit gleicher Antwort („isn't it“ zweimal) wären ein Ablenker, der zugleich
   richtig ist. Gegenmittel: Ablenker, die der Grader als Lösung der Lücke akzeptiert, fallen heraus (wie beim
   Leseverständnis, `BuiltInExerciseTypes.cs:553ff`), danach erst die Untergrenze 2 prüfen.
4. **`VOCAB_FORMS` hat fünf Nutzer** (`ExerciseEditModal.tsx`, `VaterExerciseCreate.tsx`, `VaterWizard.tsx:684`,
   `PlanPositions.tsx`, `PlanPositionsStufe.test.tsx`). Der Assistent nutzt die Liste für seine Vorgaben; beim
   Umstellen aufs Manifest darf er seine Stufen nicht verlieren. Gegenmittel: jeden Nutzer einzeln umstellen, der
   Assistent behält seine festen Werte.
5. **Bonus-Regel an der falschen Stelle.** Die Salat-Erkennung muss an der Karte hängen (Rückfall auf Kästen zahlt
   3), nicht an der Stufe allein. Gegenmittel: `FormFor` fragt `LetterTileSource` je Lücke, Test mit einer
   Mehrwort-Lücke.

### Angriffsplan

1. **Backend, Stufen:** drei `ClozeStage`-Werte, `StageOptions`, `IsTyped` (`StageMechanics.cs:27-29`).
2. **Backend, Haken im Lückentext:** `StageFacets` (Kästen, auch als Rückfall), `LetterTileSource` (Salat, ein
   Wort 2–12 Zeichen), `Choices` je Lücke (eigene Ablenker vor abgeleiteten, bis 4, unter 2 → Kästen), Mischen
   stufenabhängig (Risiko 1), Form-Angleichung (Risiko 2), Doppelte raus (Risiko 3).
3. **Vertrag:** optionales Feld für Ablenker an `Gap`, Swagger-Doku; `docs/openapi/v1.json` und `contract.ts`
   neu erzeugen.
4. **Bonus:** Salat zahlt wie Auswahl, für Lückentext und Vokabeln (`ExtraTrainingService.FormFor`).
5. **Frontend:** Standard-Abfrageform aus dem Manifest für Lückentext und Vokabeln, `VOCAB_FORMS` je Nutzer
   ablösen (Risiko 4); Ablenker-Eingabe je Lücke im Lückentext-Editor; Feld-Erklärung zum Salat-Rückfall in
   `lib/fieldHelp.ts`.
6. Reviewer: `pugling-reviewer`, dann `frontend-reviewer`; Rollengang als Sohn an Übung Nr. 19 auf allen drei
   Stufen.

### Testweg

- **Integrationstests** in `Pugling.Api.Tests`: `ClozePlayTests` für Kästen mit Trennzeichen, Salat mit
  Rückfall bei „didn't he“, Auswahl je Lücke mit eigenen und abgeleiteten Ablenkern, Untergrenze, Mischen (rote
  Probe: ohne stufenabhängiges Mischen steht die Lösung immer auf Knopf 0), Doppelte; Test und Klausur über
  dieselbe Station; `ExtraTrainingTests` für 1/1/3 Münzen und den Rückfall (rote Probe: ohne Regel zahlt Salat 3);
  `SchulVokabeltestTests` bleiben grün.
- **Unit:** `StageMechanicsTests` für `IsTyped` der neuen Stufen.
- **Vitest:** `PlanPositionsStufe.test.tsx` und der Editor-Test für die Liste aus dem Manifest, auch beim Assistenten;
  Ablenker-Eingabe je Lücke (Hin- und Rückweg).
- **E2E:** `lueckentext-am-handy.spec.ts` (B-489) bleibt grün und bekommt die Auswahl je Lücke mit 5 Knöpfen bei
  375 px dazu; `/smoke-test`.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Eingabearten (Beispiel Question tags, Übung Nr. 19).
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (vier Stufen, gemeinsame Bank, typ-neutrale Haken und
  Darstellung, Stufenwahl über das Manifest, Creator-Formular nur für Vokabeln), zehn offene Punkte mit Empfehlung.
  Im Refinement für Sprint 82.
- **2026-11-16** — gegrillt mit dem Vater: zehn Entscheidungen, Akzeptanzkriterien final; Folge-Ideen B-600 und B-601
  angelegt.
- **2026-11-16** — geschätzt: M, beides, keine Migration, kein Vertragsbruch; fünf Risiken, das erste (Auswahl
  je Lücke ungemischt wegen `ChoicesShareOnePool`) am Code gefunden.
