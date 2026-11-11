---
tags: [typ/story, status/in-arbeit, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Stufenleiter je Karte, Karteikarte vor Auswahl vor Schreiben, Kartenstufe aus dem Fach]
status: in-arbeit
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: Rückmeldung von Theodor nach dem ersten echten Lerntag (2026-11-11), vom Vater im Dialog weitergegeben
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: "E2E erstbegegnung.spec.ts (Port 5200 belegt); Ausrollen"
---

# B-476 · Die Stufe einer Vokabelkarte steigt mit ihrem Leitner-Fach: erst umdrehen, dann auswählen, dann schreiben

Theodor (12) nach dem ersten Tag auf der echten Datenbank: die Vokabelübungen sind zu schwer, es wird „quasi gleich
mit dem Schreiben der Übersetzung begonnen". Der Vater schlägt die klassische Folge vor: neue Wörter erst als
Karteikarte (umdrehen, ja/nein), dann mit Auswahl möglicher Antworten, erst am Ende frei geschrieben.

## User Story

Als *Sohn* möchte ich ein neues Wort erst wiedererkennen, dann aus einer Auswahl treffen und erst danach frei
schreiben müssen, damit ich beim Üben lerne, statt an der schwersten Form zu scheitern – und als *Vater* möchte ich,
dass die Stufe, die ich an der Position einstelle, das **Ziel** bleibt, auf das jedes Wort hinarbeitet.

## Ist-Stand am Code

- **Die Stufe gilt für den ganzen Tag, nicht für die Karte.** `PositionPlayService.StageForDay`
  (`Services/Shared/PositionPlayService.cs:56-64`): `StageSchedule` → `Position.Stage` → `Exercise.DefaultStage` →
  Typ-Default. Einzige Ausnahme je Karte ist die Erstbegegnung aus B-419 (`IsIntroduction`, `:81-83`;
  `CardStage`, `Controllers/Student/PositionPracticeController.cs:148-149`).
- **Theodors Positionen** (Plan 5, Positionen 26–29, gelesen auf einer Kopie der echten DB): `Stage = 4` (Freitext),
  kein `StageSchedule`, `UseLeitner`, `RequireTypedTest`, Ziel täglich 80 %. Nach der Kennenlernkarte kommt jedes
  Wort sofort zum Tippen.
- **Alle nötigen Stufen gibt es** (`Models/StudyPlanEntities.cs:37-56`): `SelfAssess` (umdrehen, ja/nein),
  `MultipleChoice`, `LetterBoxes`, `FreeText`. Der Vertrag trägt die Stufe je Karte (`PracticeCard.Stage`,
  `Pugling.Contracts/Student/PracticeDtos.cs:74`), das Frontend rendert je Karte (`SohnPractice.tsx:311`, `:420`).
- **Das Fach je Karte gibt es** (`PositionItemProgress.Box`, `Models/PlanPositionEntities.cs:208`): richtig → ein
  Fach höher und Intervall `[0,1,2,4,7,14]` (`PositionPlayService.cs:27`, `ApplyReview` `:374-390`), falsch → Fach 1,
  heute wieder fällig.
- **Selbsteinschätzung bewegt mit `RequireTypedTest` heute kein Fach** (`PositionPracticeController.cs:386`:
  `scored = … (typed || !pos.RequireTypedTest) …`) – eine Karte auf Fach 1 käme dort nie weiter.

## Die echte Lücke

Eine Regel „Stufe der Karte = f(Fach, Zielstufe)". Alles darunter ist da.

## Entscheidungen

Vom Vater im Dialog vom 2026-11-11 freigegeben („Ja, Story und bauen" auf den Vorschlag mit drei offenen Punkten;
die drei Punkte nach der dortigen Empfehlung entschieden, Vetorecht beim Vater).

1. **Die Kartenstufe folgt dem Fach, die eingestellte Stufe ist die Obergrenze.** Fach 1 → Selbsteinschätzung,
   Fach 2 → Auswahl, ab Fach 3 → Zielstufe des Tages; im obersten Fach der Position (`MaxBox`) immer die Zielstufe,
   sonst bliebe eine Karte bei `MaxBox` 1 oder 2 für immer unter dem Ziel (Review-Fund). Ist die Zielstufe leichter als die Sprosse, gilt die Zielstufe
   (Ziel „Selbsteinschätzung" bleibt Selbsteinschätzung). Die Erstbegegnung (B-419) bleibt davor. Begründung: wirkt
   ohne Zutun in jedem schon angelegten Plan, auch in Theodors. Kosten: eine Bedingung in der Kartenprojektion.
2. **Die Sprosse „umdrehen" verschiebt das Fach, zahlt aber keine Münzen** und zählt nicht für die Beherrschung –
   auch nicht mit `RequireTypedTest`. Begründung: ein „ja" ist eine Selbstauskunft; geprüft wird ab der Auswahl, und
   wer dort falsch liegt, fällt auf Fach 1 zurück. Ohne Fachbewegung bliebe die Karte unter `RequireTypedTest` für
   immer auf Fach 1. Kosten: ein Zweig im Review.
3. **Keine Buchstabenkästchen-Zwischenstufe.** Der Vater hat drei Schritte beschrieben; jede weitere Sprosse
   verschiebt das erste Schreiben um ein Intervall.
4. **Aufstieg je Lerntag über die Leitner-Intervalle, keine Migration.** Mit den Standardintervallen: Tag 1 kennenlernen
   und umdrehen, Tag 3 auswählen, ab Tag 7 schreiben. Begründung: der Abstand ist das Lernprinzip; ein Aufstieg
   innerhalb einer Sitzung bräuchte eine neue Spalte. Kosten: frei geschrieben wird ein Wort frühestens nach etwa
   einer Woche.
5. **Nur der Lern-Modus, nur Leitner-Positionen, nur Vokabelkarten.** Ohne Leitner bewegt sich kein Fach (die Karte
   bliebe auf Sprosse 1); der Info-Modus bucht nichts. Die Leiter ist ein Haken am Übungstyp
   (`IExerciseType.CardStage`), Standard „Zielstufe"; andere Typen bleiben unverändert.
6. ~~Der Abschlusstest bleibt unverändert auf der Zielstufe.~~ → **revidiert vom Vater am 2026-11-11** („Ja, Test
   anpassen“, nach dem Review-Fund: der tägliche Freitext-Test prüfte Wörter, die Theodor am Einführungstag nur
   umgedreht hatte – Malus-Risiko ab dem Ausrollen). **Der Test folgt der Leiter, aber nie unter „geprüft“:** jedes Wort
   wird mindestens als Auswahl abgefragt (Fach 1 und 2, auch ein im Test kalt eingeführtes), ab Fach 3 bzw. im obersten
   Fach der Position auf der Zielstufe. Die Stufe steht je Ergebniszeile (`TestItemResult.StageValue`, gibt es schon),
   `TestAttempt.StageValue` bleibt die Zielstufe. Gilt nur mit Leitner (sonst bewegt sich kein Fach) und nicht, wenn der
   Vater in der Vorschau eine Stufe ausdrücklich wählt. Begründung: eine Selbstauskunft ist keine Prüfung – die
   Pflicht bleibt „geprüft bestanden“, `RequireTypedTest` hält (Auswahl zählt als geprüft, `StageMechanics.IsTyped`).
   Kosten: Stufe je Zeile statt je Versuch an drei Stellen des Testcontrollers; Ziel und Malus bleiben unberührt.
7. **Spätestens sieben Tage nach seiner Einführung wird ein Wort geschrieben – im Test und beim Üben.** Vom Vater
   am 2026-11-11 entschieden („Nach 7 Tagen schriftlich“) nach dem Delta-Review-Fund, dass das Kind das Fach und damit die
   Form selbst steuert (nicht üben → Fach 1; beim Üben absichtlich falsch → Fach 1). Danach dreimal nachgeschärft:
   - ~~Nur der Test, nicht das Üben.~~ → **„Üben zieht mit“** (Vater, in der Review-Sitzung „nachtlauf“ entschieden und
     von dort übermittelt): nach Ablauf der Frist fragt auch das Üben auf der Zielstufe. Sonst würde ein Wort, das
     ehrlich immer wieder auf Fach 1 fällt, ab Tag 7 schriftlich geprüft, ohne je schriftlich geübt worden zu sein.
   - ~~„Ab dem Soll-Tag“~~ (Vater, ebenso übermittelt; dazu eine Min-Regel der Entwicklung: spätere Einführung zählt
     nicht) → **„Ab echter Einführung“** (Vater, hier direkt, nach dem Review-Fund „Dosis-Rückstand“): die Tagesdosis
     holt einen nicht gespielten Tag nie nach, also schiebt jeder verpasste Tag alle späteren Wörter dauerhaft hinter
     ihren Soll-Tag. Nach sieben verpassten Tagen (fünf Tage die Woche geübt: nach vier Wochen; zwei Wochen Ferien: auf
     einen Schlag) käme jedes neue Wort für den Rest des Plans kalt und sofort schriftlich – Malus ohne Trick, genau
     das, was die Leiter verhindern soll.

   **Leiterstart** (`PositionPlayService.LadderStart`) = der Tag der tatsächlichen Einführung (`IntroducedAt`). Ein nie
   eingeführtes Wort (nur im Test möglich – beim Üben ist es eine Einführungskarte): unter einer Dosis **heute**, weil
   der Test es selbst einführt; ohne Dosis die Aufnahme der Position in den Plan (nie vor dem Planstart), weil dort nur
   das Üben einführt und das Kind der Frist sonst durch Nicht-Üben entkäme. Ab Leiterstart + 7 Tage: Zielstufe, gleich
   in welchem Fach. Jedes Wort bekommt so höchstens eine Woche Leiter – begrenzt, egal was das Kind tut.
   **Bewusst offen** (vom Vater so entschieden): wer unter Dosis weder übt noch testet, bekommt jede neue Portion wieder
   als Auswahl. Dagegen steht der Münz-Malus für die gerissene Pflicht, nicht die Frist.
   Begründung der sieben Tage: genau der ehrliche Weg – mit den Standardintervallen wird ein täglich geübtes Wort an Tag 7
   zum ersten Mal geschrieben (Tag 1 zeigen und umdrehen, Tag 3 auswählen, Tag 7 schreiben; siehe Entscheidung 4). Die
   Frist trifft also nur ein Wort, das das Kind niedrig hält. (Eine Zwischenfassung behauptete „nach etwa drei Tagen“ –
   falsch gerechnet, vom zweiten Review gefunden.) Kosten: `LadderStart` und ein Vergleich in `CardStage` und
   `TestItemStage`; keine Migration (`IntroducedAt`, `CreatedAt` gibt es schon). `PlanPosition.CreatedAt` stempelt jetzt
   mit dem `TimeProvider`, derselben Uhr wie die Tageslogik.

## Akzeptanzkriterien

1. Eingeführte Karte auf Fach 1, Zielstufe Freitext → Karte kommt als `SelfAssess` (Lösung als `reveal`).
2. Fach 2 → `MultipleChoice` mit `choices`; ab Fach 3 → Freitext wie bisher.
3. Zielstufe leichter als die Sprosse (Ziel `SelfAssess`, Fach 2) → `SelfAssess`.
4. „Gewusst" auf der Sprosse Selbsteinschätzung → Fach 2, **0** Münzen – auch mit `RequireTypedTest`.
5. Falsche Auswahl auf Fach 2 → Fach 1, nächste Runde wieder Selbsteinschätzung.
6. Review wird gegen die Kartenstufe gewertet, nicht gegen die Tagesstufe (Auswahl auf Fach 2 wird als Auswahl geprüft).
7. Position ohne Leitner und Info-Modus: unverändert die Tagesstufe.
8. Test (Zielstufe Freitext, Leitner): Wort auf Fach 1 oder 2 bzw. noch nicht eingeführt → Auswahl; ab Fach 3 → Freitext;
   gewertet wird je Frage in ihrer Form; Ergebniszeilen tragen ihre Stufe.
9. Test ohne Leitner, Zielstufe Selbsteinschätzung oder vom Vater gewählte Vorschau-Stufe → eine Stufe für alle Fragen
   wie bisher.
10. Einführung sieben oder mehr Tage her → Test **und Üben** auf der Zielstufe, auch auf Fach 1; sechs Tage → noch
    Auswahl (Test) bzw. Umdrehkarte (Üben).
11. Nie eingeführtes Wort im Test: ohne Dosis Leiterstart = Aufnahme der Position (bzw. Planstart, wenn später); unter
    Dosis = Testtag, auch wenn das Kind weit hinter der Dosis liegt (kalt → Auswahl). Ein eingeführtes Wort zählt immer
    ab seiner Einführung, auch wenn die Position schon länger im Plan steht.

## Verlauf

- **2026-11-11** — angelegt aus Theodors Rückmeldung; Ist-Stand an Code und (Kopie der) echten DB belegt; Entscheidungen
  wie oben; Bau begonnen.
- **2026-11-11** — gebaut (Backend; das Frontend rendert die Stufe schon je Karte, keine Änderung):
  `IExerciseType.CardStage` (Standard: Zielstufe), Leiter in `VocabularyExerciseType.CardStage`,
  `PositionPlayService.CardStage`/`IntroducedBoxesAsync`, im `PositionPracticeController` dieselbe Kartenstufe für
  `/cards`, `/next`, Review, Folgekarte und Bildwechsel; Umdrehkarten stehen wie Einführungen vorn (Combo).
  Test-Grundlage: `TestApi.MarkIntroduced` setzt eingeführte Wörter jetzt auf Fach 3 („Leiter hinter sich“), damit die
  Bestandstests ihre Bedeutung behalten; vier Zusicherungen auf feste Fachnummern relativ nachgezogen; die
  Vier-Wochen-Simulation antwortet je Kartenform (sonst blieb jedes Wort auf Fach 1); B-419 AK 2 umgeschrieben
  (`ZweiteRunde_FragtDieEingefuehrtenWoerter_Ab_AlsUmdrehkarte`), die Neu-Punkte liegen jetzt in `KartenstufeTests`.
- **2026-11-11** — Verifikation: Backend **1301/1301** (`dotnet test Pugling.sln -c Release`), `dotnet format` ohne Fund.
  Fälle → Tests (`KartenstufeTests`): AK 1+2 `Fach_Bestimmt_Die_Kartenform_Bis_Zur_Zielstufe`, AK 3
  `Zielstufe_Ist_Die_Obergrenze` (6 Fälle), AK 4 `Umdrehkarte_Gewusst_Steigt_Ein_Fach_Ohne_Muenzen_Auch_Mit_RequireTypedTest`
  und `Erster_Gepruefter_Treffer_Nach_Der_Umdrehkarte_Zahlt_Die_Neu_Punkte`, AK 5 `Falsche_Auswahl_Faellt_Zurueck_Auf_Die_Umdrehkarte`,
  AK 6 `Review_Wertet_Die_Form_Der_Karte_Nicht_Die_Des_Tages`, AK 7 `Ohne_Leitner_Und_Im_Infomodus_Gilt_Die_Tagesstufe`;
  dazu `Folgekarte_Im_Review_Folgt_Der_Leiter`, `Next_Liefert_Die_Karte_Auf_Ihrer_Sprosse`,
  `Umdrehkarten_Stehen_In_Der_Runde_Vorn`, `Oberstes_Fach_Der_Position_Fragt_Die_Zielstufe`.
- **2026-11-11** — Mutanten mit `.claude/scripts/mutant-probe.py`, alle rot am benannten Test, Grundlinie danach grün.
  M5–M8 liefen im ersten Durchgang „ungemessen (kompiliert nicht)“ – der Stop-Hook baute parallel; mit kompilierbaren
  Varianten neu gemessen:

  | Mutante | Stelle | gemessen |
  |---|---|---|
  | M1 keine Leiter | `VocabularyExerciseType.CardStage` | rot |
  | M2 Fach 3 statt 2 → Auswahl | `VocabularyExerciseType.CardStage` | rot |
  | M3 Obergrenze ignoriert | `VocabularyExerciseType.CardStage` | rot |
  | M4 ohne `UseLeitner`-Bedingung | `PositionPlayService.CardStage` | rot |
  | M5 Info-Modus nicht ausgenommen | `PositionPlayService.CardStage` | rot |
  | M6 Umdrehkarte zahlt | `Review` (`scored`) | rot |
  | M7 Umdrehkarte steigt nicht | `Review` (`climbs`) | rot |
  | M8 Umdrehkarte zählt als Review | `Review` (`ReviewCount`) | rot |
  | M9 Umdrehkarten nicht vorn | `IntroductionsFirstAsync` | rot |
  | M10 Review wertet Tagesstufe | `Review` | rot |
  | M11 Folgekarte ohne Leiter | `Review` (`next`) | rot |
  | M12 `MaxBox` ignoriert | `PositionPlayService.CardStage` | rot |
  | M13 `/next` ohne Leiter | `Next` | rot |

- **2026-11-11** — Review `pugling-reviewer`: keine 🔴. 🟡 `MaxBox` < 3 hält die Karte unter dem Ziel → behoben (Entscheidung 1
  ergänzt, M12). 🟡 Swagger-Text des Review veraltet → nachgezogen. 🟡 **Der tägliche Pflicht-Test bleibt Freitext**, prüft also
  Wörter, die Theodor am Einführungstag nicht getippt hat – formal ändert die Story an Ziel und Malus nichts, praktisch steigt
  das Malus-Risiko ab dem Ausrollen; **vor dem Ausrollen beim Vater zu entscheiden** (Entscheidung 6, zurückgestellt). 🟢
  `PositionReportService` liest die Beherrschung aus `Box`, die die Umdrehkarte bewegt – akzeptiert (die plan-übergreifende
  Sicht `ItemProgress` bleibt unberührt, `countsForMastery: false`). 🟢 veralteter Testkommentar in `VierWochenTests` → nachgezogen.
- **2026-11-11** — E2E `frontend/e2e/erstbegegnung.spec.ts` umgebaut (Fall 1: Leitner, Runde 2 = Karteikarte – der
  Rollengang dieser Story; Fall 2: ohne Leitner, die getippten Fälle von B-420/B-421), **nicht gelaufen**: Port 5200 trägt den
  Heimnetz-Server mit der echten DB, die Playwright-Konfiguration startet ihren Server auf demselben Port. Darum bleibt die Story
  auf `in-arbeit`.
- **2026-11-11** — Entscheidung 6 revidiert („Ja, Test anpassen“) und gebaut: `PositionPlayService.TestItemStage`, Stufe je
  Ergebniszeile im `PositionTestsController` (`Start` friert ein, `Next`/`Answer`/`Submit` und die Lernhistorie lesen die
  Zeile), Frontend `SohnTest.tsx` benennt die Stufe der Frage statt der des Versuchs. Tests: `Test_Fragt_Fach_1_Und_2_Als_Auswahl_Ab_Fach_3_Schriftlich`
  (AK 8, inkl. Historie), `Test_Fragt_Ein_Ungesehenes_Wort_Als_Auswahl`, `Test_Mit_Einer_Stufe_Fuer_Alle_Fragen` (AK 9),
  Vitest „benennt die Stufe der Frage, nicht die des Versuchs“ (rot mit dem alten Etikett gemessen). Backend **1306/1306**,
  Vitest **742/742**, `tsc -b` sauber. Mutanten, alle rot am benannten Test, Grundlinie danach grün:

  | Mutante | Stelle | gemessen |
  |---|---|---|
  | T1 Test immer Zielstufe | `TestItemStage` | rot |
  | T2 ohne `UseLeitner`-Bedingung | `TestItemStage` | rot |
  | T3 Umdrehstufe nicht angehoben | `TestItemStage` | rot |
  | T4 ungesehenes Wort = oberstes Fach | `TestItemStage` | rot |
  | T5 Vorschau-Stufe des Vaters ignoriert | `Start` | rot |
  | T6 `Next` mit Stufe des Versuchs | `Next` | rot |
  | T7 Historie mit Stufe des Versuchs | `Submit` | rot |

  Äquivalent und darum nicht gemessen: die Bedingung „Zielstufe ungeprüft“ in `TestItemStage` (die Schleife liefert dann
  ohnehin die Zielstufe) und das je Zeile gelesene „getippt“ in `Answer`/`Submit` (Auswahl und Freitext werden beide vom
  Grader geprüft) – vom Delta-Review bestätigt.
- **2026-11-11** — Delta-Review `pugling-reviewer`: keine 🔴. **🟡 Produktfund, offen beim Vater: das Kind steuert das Fach
  und damit die Testform.** Wer nicht übt (unter einer Dosis führt der Test die Wörter kalt auf Fach 1 ein) oder beim Üben
  absichtlich falsch antwortet (falsch → Fach 1), bekommt im Pflicht-Test dauerhaft Auswahlfragen und erfüllt die Pflicht
  ohne je zu tippen. 🟡 Testkommentar AK 9 behauptete drei rote Fälle, einer ist äquivalent → umformuliert. 🟢 Berichte des
  Vaters unterscheiden Auswahl- und Tipptreffer nicht (`PositionReportService`, `ItemResultDto` ohne Stufe); 🟢 `MaxBox` 2
  im Test ungetestet (Fach 1 geht dort direkt auf die Zielstufe, regelkonform); 🟢 plan-übergreifende Beherrschung steigt
  auch aus Auswahltreffern.
- **2026-11-11** — Entscheidung 7 („Nach 7 Tagen schriftlich“) gebaut: `PositionPlayService.TestLadderDays` und die Frist in
  `TestItemStage`; `PositionTestsController.Start` reicht `IntroducedAt` bzw. für ein nie eingeführtes Wort die Aufnahme der
  Position (ohne Dosis) oder den Testtag (mit Dosis) durch. Tests: `Test_Fragt_Nach_Sieben_Tagen_Schriftlich_Auch_Auf_Fach_1`
  (7 Tage → schriftlich, 6 → Auswahl), `Nie_Geuebtes_Wort_Zaehlt_Ab_Aufnahme_Der_Position` (3 Fälle). Backend **1310/1310**.
  Mutanten, alle rot am benannten Test, Grundlinie danach grün: D1 Frist entfernt, D2 `<` statt `<=`, D3 sechs Tage, N1 nie
  eingeführtes Wort ohne Ersatzdatum, N2 Planstart statt Aufnahme der Position, N3 Dosis-Wort mit altem Datum (zuerst grün –
  der Dosis-Fall fehlte; dritten Fall ergänzt, dann rot). Zwei Läufe waren „kompiliert nicht“ durch einen parallelen Testlauf
  bzw. eine untypisierte Mutante und sind neu gemessen.
- **2026-11-11** — Delta-Review `pugling-reviewer`: keine 🔴. 🟡 ohne Dosis setzt nur das Üben `IntroducedAt` – wer nie übt,
  entkam der Frist → behoben über das Aufnahmedatum der Position (Reviewer-Variante b; Variante a, den Test einführen zu
  lassen, verworfen: das Wort verlöre beim nächsten Üben seine Kennenlernkarte aus B-419). 🟡 Begründung der sieben Tage
  rechnete falsch → in Kommentar und Entscheidung 7 korrigiert. 🟢 Leerzeile am Dateiende → entfernt. Die Behebung ist
  nicht noch einmal reviewt (Selbst-Check: Mutanten N1–N3).
- **2026-11-11** — Review der Sitzung „nachtlauf“ (Stand `54f67fd`, acht Funde, nicht nachgemessen übermittelt): alle am
  Code nachvollzogen. Behoben: (1) Frist nur im Test → `CardStage` prüft sie auch (Entscheidung „Üben zieht mit“);
  (2) Dosis-Zweig ab Testtag → Soll-Tag (`LadderStart`); (3) `CreatedAt` mit der Wanduhr → `PlanPositionsController` und die
  Test-Helfer `SeedLeitnerPositionCore`/`MarkIntroduced` stempeln über den `TimeProvider`; (4) Position älter als der Plan
  ungetestet → Fall ergänzt; (5) Dosis-Zweig ungetestet → `Kaltes_Dosiswort_Zaehlt_Ab_Seinem_Soll_Tag` mit Rang; (6) zwei
  widersprüchliche Begründungen der sieben Tage → die Rechnung „Tag 7“ stimmt, die Korrektur „etwa drei Tage“ war falsch,
  zurückgenommen; (7) toter Null-Pfad → `TestItemStage` nimmt einen nicht-nullbaren `ladderStart`, Konstante heißt
  `LadderDays`; (8) Bedingungskette → in `LadderStart` als benannte Schritte. Neu: Min-Regel (spätere Einführung zählt
  nicht) mit `Spaet_Eingefuehrtes_Wort_Verlaengert_Die_Frist_Nicht`, `Ueben_Zieht_Mit_Nach_Sieben_Tagen_Geschrieben`,
  `Position_Traegt_Das_Datum_Der_Serveruhr`. Mutanten, alle rot am benannten Test, Grundlinie danach grün:

  | Mutante | Stelle | gemessen |
  |---|---|---|
  | P1 Frist nicht im Üben | `CardStage` | rot |
  | P2 Dosis ohne Rang | `LadderStart` | rot |
  | P3 Dosis-Wort ab „heute“ (fern) | `LadderStart` | rot |
  | P4 Planstart ignoriert | `LadderStart` | rot |
  | P5 Aufnahme der Position ignoriert | `LadderStart` | rot |
  | P6 frühere Einführung ignoriert | `LadderStart` | rot |
  | P7 spätere Einführung zählt | `LadderStart` | rot |
  | P8 `CreatedAt` mit der Wanduhr | `PlanPositionsController` | rot |

  Ersetzt: der Fall „kaltes Dosis-Wort einer 30 Tage alten Position → Auswahl“ aus `54f67fd` widerspricht der Soll-Tag-
  Entscheidung und ist durch den Dosis-Fall mit Rang ersetzt.
- **2026-11-11** — Review-Fund „Dosis-Rückstand“ (Sitzung „nachtlauf“, an `PositionPlayService` nachvollzogen:
  `WithinDailyDose`/`DailyTestPortion` holen nicht nach, `LadderStart` rechnete nach Kalender) → Entscheidung 7 auf
  „Ab echter Einführung“ umgestellt; Unterfall „nie eingeführt unter Dosis“ vom Vater direkt entschieden (Testtag).
  `LadderStart(pos, plan, introducedAt, day)` ohne Rang und Min-Regel; `CardStage` braucht Plan und Index nicht mehr
  (beim Üben ist ein Wort immer eingeführt) und vergleicht `IntroducedAt` direkt. Tests: `Kaltes_Dosiswort_Zaehlt_Ab_Dem_Testtag_Auch_Hinter_Der_Dosis`
  (ersetzt den Soll-Tag-Fall; 30 Tage alte Position, einmal gespielt), `Spaet_Eingefuehrtes_Wort_Bekommt_Seine_Woche`
  (ersetzt den Min-Regel-Fall). Mutanten mit `.claude/scripts/mutant-probe.py` an `d240926 + Arbeitsbaum`, alle rot am
  benannten Test, Grundlinie danach grün:

  | Mutante | Stelle | gemessen |
  |---|---|---|
  | Q1 kaltes Dosis-Wort ab Aufnahme der Position | `LadderStart` | rot |
  | Q2 tatsächliche Einführung ignoriert | `LadderStart` | rot |
  | Q3 Frist nicht im Üben | `CardStage` | rot |
  | Q4 Frist im Üben einen Tag zu spät | `CardStage` | rot |
  | Q5 ohne Dosis: Aufnahme der Position ignoriert | `LadderStart` | rot |
  | Q6 ohne Dosis: Planstart ignoriert | `LadderStart` | rot |

  Die Mutanten P2, P3, P6, P7 der Vorrunde prüfen Regeln, die es nicht mehr gibt.

