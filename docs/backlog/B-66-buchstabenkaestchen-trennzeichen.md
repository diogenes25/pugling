---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/frontend, lerntechnik/vokabeln, rolle/student]
aliases: [Buchstabenkästchen Trennzeichen, Leerzeichen tippen]
status: ausformuliert
prio: P2
art: Defekt
quelle: remark #13 (Punkte 1 und 3)
---

# B-66 · Das Buchstabenkästchen lässt Leer- und Satzzeichen tippen, die schon feststehen

## User Story

Als Sohn möchte ich bei der Buchstabeneingabe nur die **Buchstaben** tippen, weil Leerzeichen und
Satzzeichen ohnehin durch die Lösung vorgegeben sind — sie zu tippen ist Fummelei, kein Lernen.

## Ist-Stand am Code

- Die Zahl der Kästchen ist schlicht die Zeichenzahl der Lösung: `item.Answer.Length`
  ([VocabularyExerciseType.cs:80](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)), gereicht
  als `AnswerLength` über
  [PositionPlayService.cs:109-124](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs) an
  `PracticeCard` ([PositionPracticeController.cs:109](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
  und `TestItem` ([PositionTestsController.cs:73](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs)).
- `LetterBoxes` rendert genau so viele **gleichartige** Einzelfelder und kennt keine Trennzeichen
  ([LetterBoxes.tsx:15,32-48](../../frontend/src/components/LetterBoxes.tsx)); jedes Feld ist
  `maxLength={1}` und springt nach jeder Eingabe weiter (`:17-23`).
- Verwendet an drei Stellen: Sohn-Übung
  ([SohnPractice.tsx:246](../../frontend/src/sohn/SohnPractice.tsx)), Sohn-Test
  ([SohnTest.tsx:162](../../frontend/src/sohn/SohnTest.tsx)) und Vater-Vorschau
  ([ExercisePreviewModal.tsx:130](../../frontend/src/vater/ExercisePreviewModal.tsx)).
- Der Vergleich normalisiert: trimmen, kleinschreiben, Mehrfach-Leerzeichen falten — **aber nicht sortieren**
  ([StageMechanics.cs:25-26](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs)).

## Die echte Lücke

Zwei unabhängige Dinge, die in der Anmerkung zusammen auftraten:

**(a) Trennzeichen bekommen ein Kästchen.** Bei `to grow up` sind das drei Fummel-Felder ohne Lernwert. Der
Server sagt heute nur *wie viele Zeichen*, nicht *welche davon feststehen* — die Information, die die
Oberfläche bräuchte, verlässt das Backend gar nicht.

**(b) Wortreihenfolge zählt.** `sehr groß` ≠ `groß sehr`. Das ist **kein Defekt, sondern eine offene
fachliche Entscheidung**: bei einer mehrteiligen Übersetzung mag die Reihenfolge egal sein, bei einem
englischen Satz nicht.

Punkt 2 der Anmerkung („mehrere Übersetzungen gleicher Länge erkennen") gehört nicht hierher, sondern zu
[B-65](B-65-vokabel-mehrere-uebersetzungen.md) — er hängt an der fehlenden Gleichwertigkeit, nicht an der
Länge.

## Offene Punkte

1. **Wie erfährt die Oberfläche, welche Zeichen feststehen?** *Empfehlung: `AnswerLength` (`int?`) um eine
   Maske ergänzen* — etwa `AnswerPattern: "__ ____ __"` mit Unterstrich = zu tippen. Additiv, kein
   Vertragsbruch. **Anti-Cheat prüfen:** die Maske verrät die Wortstruktur — bei einer Vokabel ist das
   dieselbe Information wie die Länge, bei einem Satz mehr.
2. **Ein Kästchen je Wort-Gruppe oder durchlaufend mit festen Feldern?** *Empfehlung: durchlaufend, die
   festen Felder gefüllt und nicht fokussierbar* — die Sprung-Logik (`LetterBoxes.tsx:17-27`) muss sie dann
   überspringen, vorwärts wie per Backspace.
3. **Was gilt als Trennzeichen?** Leerzeichen sicher; Bindestrich, Apostroph (`don't`), Komma? *Empfehlung:
   alles, was nicht Buchstabe oder Ziffer ist* — sonst wird die Liste zur Pflegeaufgabe.
4. **Soll die Wortreihenfolge egal sein?** *Empfehlung: nein, nicht pauschal.* Wenn, dann als Eigenschaft
   der Übung (Vokabel-Übung ja, Satz-Übung nein) — als globale Regel in `Normalize` würde sie beim
   Lückentext und beim Zuordnen mit gelten, wo sie falsch ist.
5. **Zählt die Vater-Vorschau mit?** Ja — sie nutzt dieselbe Komponente
   (`ExercisePreviewModal.tsx:130`), sonst prüft der Vater etwas anderes, als das Kind spielt.

## Akzeptanzkriterien

1. Bei einer mehrteiligen Lösung sind Leer- und Satzzeichen im Kästchen-Feld bereits gesetzt und nicht
   eingebbar.
2. Das Weiterspringen (vorwärts und per Backspace) überspringt die festen Felder in beide Richtungen.
3. Die eingereichte Antwort bleibt unverändert bewertbar — die Bewertung wandert nicht ins Frontend.
4. Sohn-Übung, Sohn-Test und Vater-Vorschau verhalten sich gleich.
5. Ein Komponententest zu `LetterBoxes` (RTL), der heute rot ist: Tippen über eine Trennstelle hinweg.
6. Punkt 4 ist entweder umgesetzt oder in dieser Story ausdrücklich zurückgestellt.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #13 (Punkte 1 und 3; Punkt 2 zeigt auf
  [B-65](B-65-vokabel-mehrere-uebersetzungen.md)); Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#d--buchstabeneingabe-13-punkt-1-und-3).
