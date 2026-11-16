---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Lernkarte, Kombi-Karte, Abschreiben und Erinnern]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

1. **Wann kommt die Lernkarte?** Empfehlung: Sie **ersetzt „Beide zeigen“ als Erstbegegnung** bei Vokabeln und ist
   zusätzlich im Stufenplan wählbar. Begründung: genau der Moment, für den sie gedacht ist; B-419 trägt die Regel
   schon. Alternative: nur wählbar, Erstbegegnung bleibt „Beide zeigen“.
2. **Wertung und Münzen:** Empfehlung: **keine Münzen und keine Leitner-Bewegung**, wie heute bei der
   Erstbegegnung. Das ändert meine Empfehlung aus der ersten Diskussion („höchstens 1 Münze“): Schritt 4 kommt
   Sekunden nach dem Abschreiben, die Lösung lag schon auf dem Gerät, und B-419 hat „Kennenlernen zahlt nicht“
   bewusst entschieden.
3. **Was wird aus [ja]/[nein] in Schritt 2?** Empfehlung: Es geht in beiden Fällen zum Abschreiben (Punkt 4).
   Das Ergebnis wird **nicht** als Lernstand gebucht, nur mitgeschickt; es könnte später einen Einstufungstest
   ([B-587](B-587-einstufungstest-vor-der-vokabel-position.md)) speisen.
4. **Überspringt [ja] das Abschreiben?** Empfehlung: **nein, immer abschreiben.** Abschreiben ist der billige,
   wirksame Schritt; wer „ja“ sagt, hat das Wort gelesen, nicht geschrieben.
5. **Wo wird das Abschreiben geprüft?** Empfehlung: **im Gerät**, weil nichts gezahlt wird. Dafür braucht das
   Frontend dieselbe Normalisierung wie der Server (Groß/klein, Apostroph, Leerzeichen), sonst sperrt der Knopf bei
   „Walk“ oder „don’t“. Alternative: ein Prüf-Aufruf am Server je Tastendruck oder beim OK – genauer, aber ein neuer
   Endpunkt.
6. **Was wird aus Schritt 4?** Empfehlung: Die getippte Antwort geht mit der Karte an den Server, er bewertet sie
   und schreibt sie in die Lernstand-Historie (der Vater sieht sie), aber ohne Treffer und ohne Box-Bewegung.
7. **[Abbruch] in Schritt 4:** Empfehlung: zählt als „nicht gewusst“, die Lösung wird noch einmal gezeigt, und die
   Karte kommt **am Ende derselben Runde** wieder. Ohne Malus. `IntroducedAt` ist trotzdem gesetzt, das Wort gilt
   als eingeführt.
8. **Ein falsches Wort in Schritt 4:** wie Abbruch (Punkt 7)? Empfehlung: ja, dieselbe Folge.
9. **Test, Klausur, Bonus:** Empfehlung: **nicht dort.** Die Lernkarte ist eine Lernstufe; Test und Klausur prüfen,
   das Bonus-Training zahlt Münzen.
10. **Vorlesen:** Soll beim Aufdecken (Schritt 2) das Wort vorgelesen werden (B-252)? Empfehlung: ja, über den
    vorhandenen Vorlese-Knopf; nicht in Schritt 4.

## Akzeptanzkriterien (Entwurf)

- [ ] Eine nie gesehene Vokabelkarte wird in einer Lernrunde als Lernkarte mit den vier Schritten gespielt; im
  Stufenplan ist die Lernkarte wählbar.
- [ ] Schritt 1 zeigt das deutsche Wort, Schritt 2 die Lernsprache mit [ja]/[nein]; beide führen zu Schritt 3.
- [ ] In Schritt 3 bleibt die Lösung sichtbar; [OK] ist erst klickbar, wenn die Eingabe ihr nach den Regeln des
  Servers entspricht („Walk“ und „walk“ gelten gleich).
- [ ] In Schritt 4 ist die Lösung weg; [OK] schickt die Eingabe, [Abbruch] oder eine falsche Eingabe zeigt die Lösung
  und stellt die Karte ans Ende der Runde.
- [ ] Die Lernkarte zahlt keine Münzen, bewegt keine Leitner-Box und setzt `IntroducedAt`; Schritt 4 steht in der
  Lernstand-Historie.
- [ ] Test, Klausur und Bonus-Training spielen keine Lernkarte.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — ausformuliert: Ist-Stand am Code belegt (Erstbegegnung B-419, Anzeige-Stufen zahlen nichts, eine
  Antwort je Karte, Lösung schon beim Client, keine Normalisierung im Frontend), zehn offene Punkte mit Empfehlung.
  Empfehlung zur Wertung gegenüber der ersten Diskussion geändert (keine Münze). Im Refinement für Sprint 82.
