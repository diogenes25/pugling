---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Lernkarte, Kombi-Karte, Abschreiben und Erinnern]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
grund: ""
ersetzt_durch: []
---

# B-598 · Kombi-Lernkarte: aufdecken, einschätzen, abschreiben, aus dem Gedächtnis schreiben

## User Story

Als **Vater** möchte ich, dass mein Kind ein neues Wort in **einer** Karte in vier Schritten lernt –

1. Deutscher Begriff (*gehen*) → **[Aufdecken]**
2. Lösung (*walk*) → Gewusst? **[ja] [nein]**
3. Abschreiben, das Fremdwort bleibt sichtbar → **[OK]** erst, wenn es richtig dasteht
4. Das Fremdwort verschwindet und wird aus dem Gedächtnis geschrieben → **[OK] [Abbruch]**

–, damit es das Wort beim ersten Kontakt nicht nur sieht, sondern einmal selbst schreibt, bevor es später abgefragt
wird.

## Ist-Stand am Code

- **Die Erstbegegnung gibt es schon (B-419).** Eine nie gesehene Karte wird in einer Lernrunde auf der
  `IntroductionStage` gespielt, der ersten reinen Anzeige-Stufe des Typs (`PositionPlayService.cs:126-127`,
  Regel `IsIntroduction` `:136-138`, angewandt in `CardStage` `:159-166`). Beim Vokabeltyp ist das
  „Beide zeigen“ (`ShowBoth`, `VocabularyExerciseType.cs:73`, `StudyPlanEntities.cs:39-40`).
- **Eine Anzeige-Stufe zahlt nichts und bewegt nichts.** `PositionPracticeController.cs:411` (`displayOnly`),
  `:465` (`scored` verlangt `!displayOnly`), `:516-517` (Lernstand ohne Treffer, `countsForMastery: scored`). Erster
  Kontakt setzt `IntroducedAt` (`:453-455`). Begründung aus B-419: „Kennenlernen darf nicht der billigste Weg zu
  Münzen sein.“
- **Eine Karte schickt genau eine Antwort.** `ReviewDto(ItemIndex, GivenAnswer, WasKnown)`
  (`PracticeDtos.cs:101`) an `POST …/review` (`PositionPracticeController.cs:373`). Eine Karte mit vier Schritten
  hat damit heute keinen Ort für mehrere Ergebnisse; beide Felder sind aber schon optional.
- **Die Stufen der Vokabel-Leiter.** `TestStage` (`StudyPlanEntities.cs:37-56`): Beide zeigen, Selbsteinschätzung,
  Buchstabenkästen, Freitext, Hören, Auswahl, Buchstabensalat. Die Leiter nach Leitner-Box
  (`VocabularyExerciseType.cs:82-96`, B-476): Box 1 Selbsteinschätzung, Box 2 Auswahl, darüber die Zielstufe.
- **Die Lösung liegt auf Anzeige- und Selbsteinschätzungs-Stufen schon beim Client** (`Reveal`,
  `PositionPlayService.CardFacets`, Kommentar `:343-348`). Auf einer Karte, die Schritt 2 und 3 zeigt, ist Schritt 4
  darum kein geprüftes Erinnern im Sinne des Anti-Cheat.
- **Die Sohn-Karte kennt zwei Phasen.** `SohnPractice.tsx`: Vorderseite, nach „Aufdecken“ die Rückseite mit
  `reveal` (`:486-489`), eine Anzeige-Karte bekommt „Weiter“ (`:512`); `judge` schickt `wasKnown` oder
  `givenAnswer` (`:231`). Schritte „Abschreiben“ und „Erinnern“ gibt es nicht.
- **Das Frontend vergleicht keine Antworten.** Die Normalisierung (Groß/klein, Apostroph, Leerzeichen) lebt nur im
  Server (`StageMechanics.Normalize`, B-440/B-497); im Frontend gibt es kein Gegenstück (`frontend/src/lib`, nur
  `interests.ts` normalisiert Schlagwörter).
- **Das Bonus-Training** beginnt jede Vokabelübung mit Selbsteinschätzung, ohne Münzen
  (`ExtraTrainingService.cs:377`).
- **Richtung:** Nach [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md) wird auf Schreibstufen immer die
  Lernsprache geschrieben; die Lernkarte schreibt in Schritt 3 und 4.

## Die echte Lücke

Es fehlt eine **Stufe, die aus mehreren Schritten besteht**, und ein Ort, an dem ihre Ergebnisse ankommen. Die
Erstbegegnung, die Nicht-Wertung einer Anzeige-Stufe und die Darstellung der Lösung sind da. Neu sind der
Stufenwert, die vier Phasen in der Sohn-Karte, die Prüfung des Abschreibens und die Frage, was Schritt 2 und 4 im
Lernstand hinterlassen.

## Offene Punkte

1. ~~**Wann kommt die Lernkarte?**~~ → Entscheidung 1. Empfehlung: Sie **ersetzt „Beide zeigen“ als Erstbegegnung** bei Vokabeln und ist
   zusätzlich im Stufenplan wählbar. Begründung: genau der Moment, für den sie gedacht ist; B-419 trägt die Regel
   schon. Alternative: nur wählbar, Erstbegegnung bleibt „Beide zeigen“.
2. ~~**Wertung und Münzen:**~~ → Entscheidung 2. Empfehlung: **keine Münzen und keine Leitner-Bewegung**, wie heute bei der
   Erstbegegnung. Das ändert meine Empfehlung aus der ersten Diskussion („höchstens 1 Münze“): Schritt 4 kommt
   Sekunden nach dem Abschreiben, die Lösung lag schon auf dem Gerät, und B-419 hat „Kennenlernen zahlt nicht“
   bewusst entschieden.
3. ~~**Was wird aus [ja]/[nein] in Schritt 2?**~~ → Entscheidung 3. Empfehlung: Es geht in beiden Fällen zum Abschreiben (Punkt 4).
   Das Ergebnis wird **nicht** als Lernstand gebucht, nur mitgeschickt; es könnte später einen Einstufungstest
   ([B-587](B-587-einstufungstest-vor-der-vokabel-position.md)) speisen.
4. ~~**Überspringt [ja] das Abschreiben?**~~ → Entscheidung 4. Empfehlung: **nein, immer abschreiben.** Abschreiben ist der billige,
   wirksame Schritt; wer „ja“ sagt, hat das Wort gelesen, nicht geschrieben.
5. ~~**Wo wird das Abschreiben geprüft?**~~ → Entscheidung 5. Empfehlung: **im Gerät**, weil nichts gezahlt wird. Dafür braucht das
   Frontend dieselbe Normalisierung wie der Server (Groß/klein, Apostroph, Leerzeichen), sonst sperrt der Knopf bei
   „Walk“ oder „don’t“. Alternative: ein Prüf-Aufruf am Server je Tastendruck oder beim OK – genauer, aber ein neuer
   Endpunkt.
6. ~~**Was wird aus Schritt 4?**~~ → Entscheidung 6. Empfehlung: Die getippte Antwort geht mit der Karte an den Server, er bewertet sie
   und schreibt sie in die Lernstand-Historie (der Vater sieht sie), aber ohne Treffer und ohne Box-Bewegung.
7. ~~**[Abbruch] in Schritt 4:**~~ → Entscheidung 7. Empfehlung: zählt als „nicht gewusst“, die Lösung wird noch einmal gezeigt, und die
   Karte kommt **am Ende derselben Runde** wieder. Ohne Malus. `IntroducedAt` ist trotzdem gesetzt, das Wort gilt
   als eingeführt.
8. ~~**Ein falsches Wort in Schritt 4:**~~ → Entscheidung 8. wie Abbruch (Punkt 7)? Empfehlung: ja, dieselbe Folge.
9. ~~**Test, Klausur, Bonus:**~~ → Entscheidung 9. Empfehlung: **nicht dort.** Die Lernkarte ist eine Lernstufe; Test und Klausur prüfen,
   das Bonus-Training zahlt Münzen.
10. ~~**Vorlesen:**~~ → Entscheidung 10. Soll beim Aufdecken (Schritt 2) das Wort vorgelesen werden (B-252)? Empfehlung: ja, über den
    vorhandenen Vorlese-Knopf; nicht in Schritt 4.

## Entscheidungen

Gegrillt mit dem Vater am 2026-11-16, eine Frage nach der anderen (6–10 auf „nimm deine Empfehlung“).

1. **Die Lernkarte ist die neue Erstbegegnung bei Vokabeln und zusätzlich im Stufenplan wählbar;** „Beide zeigen“
   bleibt wählbar. *Begründung:* Genau dieser Moment ist gemeint, und die Regel „neue Karte zuerst zeigen“ (B-419)
   trägt sie schon; wählbar dient sie auch wackeligen Wörtern. *Kosten:* Jedes neue Wort braucht deutlich länger als
   ein „Weiter“; die erste Runde einer Tagesdosis wird spürbar länger. `IntroductionStage` darf nicht mehr nur „die
   erste Anzeige-Stufe“ sein.
2. **Keine Münzen, keine Leitner-Bewegung;** das Wort gilt danach als eingeführt. *Begründung:* Schritt 4 prüft das
   Kurzzeitgedächtnis, die Lösung lag schon auf dem Gerät; B-419 hat „Kennenlernen zahlt nicht“ bewusst entschieden.
   *Kosten:* mehr Arbeit als heute ohne Lohn – im Rollengang gegen [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md)
   ansehen.
3. **[ja] und [nein] führen beide zu Schritt 3; die Antwort wird gespeichert, aber nicht gewertet.** *Begründung:*
   Bei einem neuen Wort sagt „ja“ wenig; gespeichert kann es später einen Einstufungstest
   ([B-587](B-587-einstufungstest-vor-der-vokabel-position.md)) oder die Auswertung speisen. *Kosten:* ein Wert, den
   heute nichts anzeigt; ein Historien-Eintrag mehr je neuem Wort.
4. **Abgeschrieben wird immer, auch nach „ja“.** *Begründung:* Abschreiben ist das erste eigene Schreiben des Worts
   (Ziel von B-597); ein ungewertetes „ja“ wäre sonst ein Knopf zum Abkürzen. *Kosten:* ein paar Sekunden je
   bekanntem Wort.
5. **Das Abschreiben wird im Gerät geprüft, mit denselben Vergleichsregeln wie der Server; ein gemeinsamer Satz
   Testfälle hält beide gleich.** *Begründung:* Es wird nichts gezahlt, also gewinnt ein Austricksen nichts; der Knopf
   reagiert sofort. *Kosten:* die Regeln von `StageMechanics.Normalize` (Groß/klein, Apostroph B-440, Leerzeichen
   B-497) zweimal, in C# und TypeScript; ohne den gemeinsamen Test laufen sie auseinander.
6. **Schritt 4 geht an den Server, wird bewertet und steht in der Lernstand-Historie – ohne Treffer, Münze oder
   Box-Bewegung.** Beide Ergebnisse der Karte (Schritt 2 und 4) kommen in **einem** Aufruf (`WasKnown` und
   `GivenAnswer` des `ReviewDto`). *Begründung:* Für den Vater ist das erste Erinnern die interessanteste Information
   der Karte; der Server bleibt die eine Bewertungsquelle. *Kosten:* eine Regel mehr im Review-Pfad
   (`PositionPracticeController.cs:373ff`), der bei dieser Stufe beide Felder auswertet.
7. **[Abbruch] zählt als „nicht gewusst“: Lösung zeigen, die Karte kommt höchstens einmal am Ende derselben Runde
   wieder, ohne Malus;** das Wort gilt als eingeführt. *Begründung:* „noch nicht drin“ ist ein Grund für eine
   Wiederholung, nicht für Strafe. *Kosten:* Runden mit vielen Abbrüchen werden länger; die Grenze „einmal“ verhindert
   eine Endlosschleife und braucht Platz in der eingefrorenen Rundenfolge.
8. **Ein falsches Wort in Schritt 4 hat dieselbe Folge wie Abbruch;** die Historie unterscheidet „aufgegeben“ und
   „falsch“. *Begründung:* gleiche Folge, kein Anreiz für den einen oder anderen Knopf. *Kosten:* ein Tippfehler kostet
   eine Wiederholung.
9. **Nur in Lernrunden** – nicht in Test, Klausur, Bonus-Training oder Info-Modus; eine Station mit Lernkarte im
   Stufenplan spielt im Test ihre Zielstufe. *Begründung:* Die Lernkarte zeigt die Lösung (Leck in einer Prüfung) und
   zahlt nichts (gehört nicht ins Bonus-Training). *Kosten:* Ein Wort, das zuerst im Bonus-Training auftaucht, lernt
   Theodor dort ohne Lernkarte.
10. **Ein Vorlese-Knopf für das Fremdwort in Schritt 2 und 3, auf Knopfdruck; in Schritt 4 keiner.** *Begründung:*
    Ein neues Wort einmal hören; auf Knopfdruck, weil ein selbst startender Ton am Handy stört. *Kosten:* Der Knopf
    liest das Lösungswort statt des Fragewortes (`Lektion.tsx:96` liest heute `prompt`); die Klangqualität prüft erst
    ein echtes Gerät (wie [B-31](B-31-geraete-vorbehalt-klang.md)).

## Akzeptanzkriterien

- [ ] Eine nie gesehene Vokabelkarte wird in einer Lernrunde als Lernkarte gespielt; im Stufenplan sind Lernkarte und
  „Beide zeigen“ wählbar.
- [ ] Schritt 1 zeigt das deutsche Wort mit [Aufdecken]; Schritt 2 das Fremdwort mit [ja]/[nein] und Vorlese-Knopf;
  beide Antworten führen zu Schritt 3.
- [ ] In Schritt 3 bleibt das Fremdwort sichtbar (mit Vorlese-Knopf); [OK] ist erst klickbar, wenn die Eingabe nach
  den Regeln des Servers stimmt („Walk“, „don’t“/„don't“ gelten). Ein gemeinsamer Testfall-Satz läuft gegen C# und
  TypeScript.
- [ ] In Schritt 4 ist das Fremdwort weg und es gibt keinen Vorlese-Knopf; [OK] schickt die Eingabe, [Abbruch] oder
  eine falsche Eingabe zeigt die Lösung und stellt die Karte **einmal** ans Ende der Runde.
- [ ] Ein Aufruf trägt Schritt 2 (`WasKnown`) und Schritt 4 (`GivenAnswer`); der Server speichert beide in der
  Historie, unterscheidet „aufgegeben“ und „falsch“ und bucht weder Münze noch Treffer noch Box-Bewegung; `IntroducedAt`
  ist gesetzt.
- [ ] Test, Klausur, Bonus-Training und Info-Modus spielen keine Lernkarte.
- [ ] Integrationstests mit roter Probe: Lernkarte zahlt nichts (Probe: mit Treffer-Wertung steigt die Box),
  Wiederholung höchstens einmal (Probe: ohne Grenze kommt die Karte ein drittes Mal), keine Lernkarte im Test.

## Schätzung

**Größe L**, am Anker E6 gemessen: Die Stufe selbst ist klein, aber sie ist die erste, die **zeigt und trotzdem
schreiben lässt**. Die Regel „Anzeige-Stufe zahlt nichts“ hängt heute an `IsDisplayOnlyStage`, und das an zehn
Stellen im Backend (`PositionPracticeController.cs:131`, `:200`, `:411`, `:442`, `:465`, `:516`;
`PositionTestsController.cs:153`; `PositionPlayService.cs:109`, `:127`, `:137`; `PositionProgressService.cs:62`)
und zwei im Frontend (`SohnPractice.tsx`, `aufgabenform.ts`). Jede muss für die Lernkarte einzeln entschieden
werden. Dazu kommen die Wiederholung in der eingefrorenen Rundenfolge, eine Karte mit vier Phasen und die
Vergleichsregeln in TypeScript mit gemeinsamem Testfall-Satz.

- **`wo: beides`** – Backend zuerst (Stufe, Erstbegegnung, Review mit zwei Ergebnissen, Wiederholung), dann die
  Sohn-Karte (vier Phasen, Vorlesen der Lösung, Prüfung beim Abschreiben).
- **`migration: nein`** – Stufen sind Zahlen (`PlanPositionEntities.cs:32`), die Rundenfolge ist eine Liste in
  JSON (`PracticeSession.Order`, `StudyPlanEntities.cs:108`), eine Wiederholung hängt einen Index an. Die beiden
  Ergebnisse der Karte werden als **zwei** `ItemReviewEvent`-Zeilen gespeichert (`ItemProgressEntities.cs:61-88`):
  Schritt 2 auf der Stufe Selbsteinschätzung mit `WasCorrect` = gewusst, Schritt 4 auf der Lernkarten-Stufe mit
  `GivenAnswer`; „aufgegeben“ ist `GivenAnswer = null`. Eine eigene Spalte wäre sauberer, braucht aber eine
  Migration – Abwägung siehe Risiko 4.
- **`vertragsbruch: nein`** – `ReviewDto` trägt `WasKnown` und `GivenAnswer` schon (`PracticeDtos.cs:101`). Neu und
  additiv: die Vorlesesprache des Lösungsworts an der Karte und ein Hinweis „kommt am Rundenende wieder“ im
  Ergebnis.

### Risiken

1. **Die Lernkarte ist keine Anzeige-Stufe, soll aber wie eine nichts zahlen.** Wird sie als `IsDisplayOnlyStage`
   markiert, zeigt die Sohn-Karte nur „Weiter“ (`SohnPractice.tsx:512`) und der Review wertet `GivenAnswer` nicht.
   Wird sie es nicht, zahlt `scored` (`PositionPracticeController.cs:465`). Gegenmittel: eine eigene Eigenschaft am
   Typ („zählt nie als Treffer“), und jede der zwölf Stellen wird nach der pm-loop-Regel einzeln gelesen und
   entschieden – je Stelle eine Zeile im `## Verlauf`.
2. **Die Erstbegegnung wählt „die erste Anzeige-Stufe“** (`PositionPlayService.cs:127`). Die Lernkarte ist das
   nicht. Gegenmittel: Der Typ nennt seine Einführungsstufe selbst; Test, dass eine neue Karte als Lernkarte kommt
   und ein Typ ohne Lernkarte unverändert bleibt.
3. **Wiederholung in der eingefrorenen Folge.** `Order` und `Cursor` tragen die Runde, `CardShownAt` stempelt die
   aktuelle Karte (B-530). Ein angehängter Index darf weder den Cursor verschieben noch eine Karte ein drittes Mal
   bringen. Gegenmittel: Grenze aus der Folge selbst ablesen (Index höchstens zweimal), Test über Wiederaufnahme
   einer Runde.
4. **Zwei Historien-Zeilen statt einer Spalte.** Auswertungen, die Zeilen zählen (Fehlerliste, Vokabel-Fortschritt
   `by-word`), sähen eine Lernkarte als zwei Antworten. Gegenmittel: Beide Zeilen mit `countsForMastery: false`;
   prüfen, welche Lesepfade Zeilen zählen statt `ItemProgress` zu lesen. Wenn einer das tut, ist eine Spalte mit
   Migration die bessere Wahl – das entscheidet der Bau, nicht diese Schätzung.
5. **Vergleichsregeln laufen auseinander.** Gegenmittel: eine Datei mit Testfällen (Eingabe, Lösung, erwartet),
   gelesen von einem xUnit-Test gegen `StageMechanics.Normalize`/`AnswerGrader` und einem Vitest gegen die
   TypeScript-Kopie.
6. **Abhängigkeit von B-597.** Die Lernkarte schreibt die Lernsprache, also gilt die Drehung je Stufe. Gegenmittel:
   B-597 zuerst bauen; die Lernkarte steht in dessen Liste der Schreibstufen.

### Angriffsplan

1. B-597 ist gebaut (Drehung je Stufe).
2. **Backend, Stufe:** `TestStage.LearnCard` (Name im Bau), `StageOptions`, Eigenschaft „zählt nie als Treffer“,
   Einführungsstufe nennt der Typ.
3. **Backend, zwölf Stellen:** jede `IsDisplayOnlyStage`-Stelle lesen und für die Lernkarte entscheiden (Risiko 1).
4. **Backend, Review:** bei der Lernkarte `WasKnown` und `GivenAnswer` auswerten, zwei Historien-Zeilen,
   `IntroducedAt`, keine Münze, kein Treffer, keine Box; falsch/Abbruch → Index einmal ans Ende von `Order`.
5. **Backend, Ausschlüsse:** Test, Klausur, Bonus, Info-Modus spielen keine Lernkarte.
6. **Vergleichsregeln** in TypeScript mit gemeinsamem Testfall-Satz (Risiko 5).
7. **Frontend:** Karte mit vier Phasen in `SohnPractice.tsx`/`Lektion.tsx`, Vorlesen des Lösungsworts in Phase 2
   und 3, Sperre beim Abschreiben, Stufenname im Manifest.
8. Reviewer: `pugling-reviewer`, dann `frontend-reviewer`; Rollengang als Sohn mit einer frischen Station.

### Testweg

- **Integrationstests** in `Pugling.Api.Tests`: `ErstbegegnungTests` (neue Karte kommt als Lernkarte; rote Probe:
  ohne eigene Einführungsstufe kommt „Beide zeigen“), Review ohne Münze und Box (rote Probe: mit Treffer-Wertung
  steigt die Box), zwei Historien-Zeilen mit „aufgegeben“/„falsch“, Wiederholung höchstens einmal auch über eine
  wieder aufgenommene Runde (rote Probe: ohne Grenze kommt die Karte ein drittes Mal), keine Lernkarte in
  `PositionTestsController`, im Bonus (`ExtraTrainingTests`) und im Info-Modus.
- **Gemeinsamer Testfall-Satz** für die Vergleichsregeln: xUnit und Vitest lesen dieselbe Datei.
- **Vitest:** Karte durchläuft die vier Phasen, [OK] in Phase 3 erst bei richtiger Eingabe, kein Vorlese-Knopf in
  Phase 4.
- **E2E:** eine Spec als Sohn mit einer frischen Station: neue Karte, aufdecken, abschreiben, erinnern, Abbruch →
  Karte am Rundenende; `/smoke-test`. Der Klang des Vorlesens bleibt ein Gerätecheck (B-31).

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (Erstbegegnung B-419, Anzeige-Stufen zahlen nichts, eine
  Antwort je Karte, Lösung schon beim Client, keine Normalisierung im Frontend), zehn offene Punkte mit Empfehlung.
  Empfehlung zur Wertung gegenüber der ersten Diskussion geändert (keine Münze). Im Refinement für Sprint 82.
- **2026-11-16** — gegrillt mit dem Vater: zehn Entscheidungen, Akzeptanzkriterien final.
- **2026-11-16** — geschätzt: L, beides, keine Migration (zwei Historien-Zeilen statt neuer Spalte, Risiko 4),
  kein Vertragsbruch; zwölf Stellen der Regel „Anzeige-Stufe“ als Kern der Arbeit.
