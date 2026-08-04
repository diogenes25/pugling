---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/frontend, lerntechnik/vokabeln, rolle/student]
aliases: [Buchstabenkästchen Trennzeichen, Leerzeichen tippen]
status: geschaetzt
prio: P2
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: remark #13 (Punkte 1 und 3)
---

# B-66 · Das Buchstabenkästchen lässt Leer- und Satzzeichen tippen, die schon feststehen

## User Story

Als Sohn möchte ich bei der Buchstabeneingabe nur die **Buchstaben** tippen, weil Leerzeichen und
Satzzeichen ohnehin durch die Lösung vorgegeben sind — sie zu tippen ist Fummelei, kein Lernen.

## Ist-Stand am Code

> Belege am 2026-08-04 gegen den heutigen Code nachgeprüft — die Datei stand seit dem 2026-08-02, mehrere
> Zeilennummern waren verschoben (unten korrigiert), an der Aussage selbst ändert das nichts.

- Die Zahl der Kästchen ist schlicht die Zeichenzahl der Lösung: `item.Answer.Length`
  ([VocabularyExerciseType.cs:93](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs), war `:80`),
  gereicht als `AnswerLength` über die geteilte Projektion
  [PositionPlayService.cs:140-172](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)
  (`CardFacets`, war `:109-124`) an `PracticeCard`
  ([PositionPracticeController.cs:108-109](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs),
  passt noch), `TestItem`
  ([PositionTestsController.cs:75](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs), war `:73`)
  **und** — im Ist-Stand nicht erwähnt, aber derselbe Aufruf — `PreviewItem`
  ([ExercisePreviewService.cs:111-114](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)):
  `CardFacets` ist die *eine* geteilte Stelle, an der auch die Vater-Vorschau hängt, nicht bloß Übung und Test.
- `LetterBoxes` rendert genau so viele **gleichartige** Einzelfelder und kennt keine Trennzeichen
  ([LetterBoxes.tsx:15,32-48](../../frontend/src/components/LetterBoxes.tsx), Zeilen passen); jedes Feld ist
  `maxLength={1}` und springt nach jeder Eingabe weiter (`:17-23`, Sprunglogik in `setChar`/`onKeyDown`).
- Verwendet an drei Stellen: Sohn-Übung
  ([SohnPractice.tsx:264](../../frontend/src/sohn/SohnPractice.tsx), war `:246`), Sohn-Test
  ([SohnTest.tsx:171](../../frontend/src/sohn/SohnTest.tsx), war `:162`) und Vater-Vorschau
  ([ExercisePreviewModal.tsx:189](../../frontend/src/vater/ExercisePreviewModal.tsx), war `:130` — größte
  Verschiebung, 59 Zeilen).
- Der Vergleich normalisiert: trimmen, kleinschreiben, Mehrfach-Leerzeichen falten — **aber nicht sortieren**
  ([StageMechanics.cs:30-31](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs), war `:25-26`).

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

1. ~~Wie erfährt die Oberfläche, welche Zeichen feststehen?~~ → siehe Entscheidung 1.
2. ~~Ein Kästchen je Wort-Gruppe oder durchlaufend mit festen Feldern?~~ → siehe Entscheidung 2.
3. ~~Was gilt als Trennzeichen?~~ → siehe Entscheidung 3.
4. ~~Soll die Wortreihenfolge egal sein?~~ → siehe Entscheidung 4 (zurückgestellt, nicht Teil dieser Story).
5. ~~Zählt die Vater-Vorschau mit?~~ → siehe Entscheidung 5.

## Entscheidungen

1. **Die Maske reist als eigenes additives Feld `AnswerPattern` (`string?`), neben dem bestehenden
   `AnswerLength`.** Muster: Unterstrich = zu tippende Stelle, jedes andere Zeichen steht fest und wird
   1:1 übernommen (`"to grow up"` → `"__ _____ __"`). Sitzt an der *einen* geteilten Stelle
   `PositionPlayService.CardFacets` ([PositionPlayService.cs:140-172](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)),
   die `IExerciseType.StageFacets` (`(int? LetterBoxLength, string? AudioUrl, string? ImageUrl)`,
   [IExerciseType.cs:78](../../backend/Pugling.Api/Exercises/IExerciseType.cs)) um die Maske ergänzt, statt
   sie nur aus der Länge zu leiten — nur `VocabularyExerciseType` liefert heute `LetterBoxLength`
   ([VocabularyExerciseType.cs:92-95](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)), die
   beiden anderen Typen (`ListeningExerciseType`, `ExerciseTypeBase`-Default) liefern `null` und bleiben
   unverändert. **Begründung:** additiv, kein Vertragsbruch (Client/Frontend/`unknown_field`-Guards ziehen
   nicht mit); alt zwischengespeicherte PWA-Clients ignorieren das neue Feld einfach und fallen auf reines
   Tippen zurück. **Kosten:** drei DTOs wachsen um ein Feld (`PracticeCard`, `TestItem`, `PreviewItem` — alle
   drei hängen an `CardFacets`), die `StageFacets`-Tupel-Signatur wächst in allen drei Implementierungen
   (`IExerciseType`, `ExerciseTypeBase`, `VocabularyExerciseType`, `BuiltInExerciseTypes.ListeningExerciseType`).
   **Anti-Cheat-Prüfung (aus Punkt 1):** kein neues Leck — bei einer Vokabel verrät die Maske exakt dieselbe
   Information wie die schon ausgelieferte Länge (Wortzahl aus den Leerzeichen ist bei einem mehrteiligen
   Suchbegriff ohnehin über `AnswerLength` + Prompt/Hint erschließbar); bei einem Satz ist die Struktur
   gröber sichtbar, aber die Interpunktion selbst trägt keine Lösung.
2. **Durchlaufende Kästchen-Reihe, feste Felder vorausgefüllt und nicht fokussierbar** (Sprung-Logik
   überspringt sie in beide Richtungen). **Begründung:** ein zweites Layout je Wort-Gruppe bräche die
   bestehende `maxLength={1}`-Reihe in mehrere DOM-Blöcke und verkompliziert Fokus-Steuerung ohne
   Lernwert-Gewinn — die Kästchen bleiben optisch eine Reihe, nur einzelne Felder sind gesperrt. **Kosten:**
   `LetterBoxes.tsx` bekommt eine zweite Vorwärts-/Rückwärts-Skip-Schleife (aktuell ein Einzelschritt in
   `setChar`/`onKeyDown`, [LetterBoxes.tsx:17-27](../../frontend/src/components/LetterBoxes.tsx)) statt eines
   Einzelschritts — mehrere aufeinanderfolgende feste Felder (z. B. `", "` oder ein Bindestrich neben einem
   Leerzeichen) müssen in einem Rutsch übersprungen werden, nicht nur eines.
3. **Trennzeichen = alles, was nicht Buchstabe oder Ziffer ist** (`char.IsLetterOrDigit` negiert, angewandt
   je Zeichen der Lösung). **Begründung:** eine Positivliste (Leerzeichen, Bindestrich, Apostroph, Komma, …)
   ist eine Pflegeaufgabe, die bei der ersten fremdsprachigen Lösung mit einem vergessenen Zeichen bricht;
   die Negation ist geschlossen und sprachunabhängig. **Kosten:** Unicode-Sonderfälle (z. B. `œ` in
   Französisch) zählen `char.IsLetterOrDigit` bereits als Buchstabe — kein Zusatzaufwand, aber ein Fall, den
   ein Komponententest nicht separat prüfen muss.
4. **Wortreihenfolge bleibt zurückgestellt, nicht Teil dieser Story.** Nur die Fixier-Maske wird gebaut; ob
   `sehr groß` und `groß sehr` gleichwertig gelten sollen, ist eine eigene fachliche Entscheidung
   (übungsweit unterschiedlich, siehe Ist-Stand) und gehört — sollte sie gebraucht werden — als eigene Story
   an `StageMechanics.Normalize`/`AnswerGrader`, nicht hier huckepack. **Begründung:** die Maske braucht diese
   Entscheidung nicht (sie markiert nur feste Zeichen, ändert die Bewertung nicht); sie in dieselbe Änderung
   zu packen vermischt einen reinen UI-Defekt mit einer Bewertungsregel, die andere Übungstypen (Lückentext,
   Zuordnen) mitbeträfe. **Kosten:** keine — Akzeptanzkriterium 6 verlangt genau diese ausdrückliche
   Zurückstellung.
5. **Die Vater-Vorschau zählt mit**, weil sie dieselbe Komponente nutzt
   (`ExercisePreviewModal.tsx:189`) und über denselben `CardFacets`-Pfad geht (`PreviewItem`,
   [ExercisePreviewService.cs:111-114](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)).
   **Begründung:** Entscheidung 1 fließt automatisch mit, sobald `PreviewItem` das neue Feld trägt — es gibt
   keinen zweiten Pfad zu pflegen. **Kosten:** keine zusätzlichen, nur die dritte DTO-Erweiterung aus
   Entscheidung 1.

## Akzeptanzkriterien

1. Bei einer mehrteiligen Lösung sind Leer- und Satzzeichen im Kästchen-Feld bereits gesetzt und nicht
   eingebbar (`AnswerPattern` markiert sie, Trennzeichen = alles außer Buchstabe/Ziffer, Entscheidung 3).
2. Das Weiterspringen (vorwärts und per Backspace) überspringt die festen Felder in beide Richtungen, auch
   mehrere aufeinanderfolgende (Entscheidung 2).
3. Die eingereichte Antwort bleibt unverändert bewertbar — die Bewertung wandert nicht ins Frontend; der
   Server vergleicht wie heute über `StageMechanics.Normalize`/`AnswerGrader`.
4. Sohn-Übung, Sohn-Test und Vater-Vorschau verhalten sich gleich (alle drei hängen an derselben
   `CardFacets`-Projektion, Entscheidung 5).
5. Ein Komponententest zu `LetterBoxes` (RTL), der vor der Umsetzung rot ist: Tippen über eine Trennstelle
   hinweg (Cursor überspringt ein festes Feld vorwärts und beim Löschen rückwärts).
6. Die Wortreihenfolge-Frage (ursprünglich Punkt 4) ist in dieser Story ausdrücklich zurückgestellt
   (Entscheidung 4) — kein Umsetzungsbedarf hier.

## Schätzung

- **Größe:** `M` — kein Einzeiler (Anker XS/S), aber auch kein DB-Umbau (Anker L): eine geteilte
  Backend-Projektion wächst um ein Feld und zieht drei Contracts nach, dazu eine echte
  Verhaltensänderung im Frontend (bidirektionale Skip-Logik über mehrere feste Felder hinweg) an einer
  gemeinsam genutzten Komponente mit drei Aufrufstellen. Vergleichbar mit dem M-Anker
  (vokabel-basierter Batch-Pfad im `MediaSelector`, B-03): mehrere Stellen, aber ein Kern-Mechanismus,
  keine neue Ebene.
- **`wo`:** `beides` — Backend liefert die Maske, Frontend setzt sie um; Backend zuerst (API-First).
- **`migration`:** `nein` — `AnswerPattern` ist reine Laufzeit-Ableitung aus `item.Answer`, keine neue
  Spalte, kein Entity-Feld.
- **`vertragsbruch`:** `nein` — additive Felder auf drei bestehenden Response-DTOs (`PracticeCard`,
  `TestItem`, `PreviewItem`); kein Client-/Frontend-Bruch, keine `unknown_field`-Berührung (nur lesende
  Felder wachsen).

### Risiken

- **Anti-Cheat-Feinheit:** die Maske zeigt bei einem Satz mehr Struktur als die reine Länge (Wortzahl und
  -längen statt nur Gesamtlänge). Bewertet in Entscheidung 1 als hinnehmbar, weil keine Lösungsinformation
  selbst durchsickert — aber bei einem künftigen Übungstyp mit sehr kurzen, wenigen Wörtern (z. B. Ja/Nein)
  neu zu prüfen.
- **Skip-Logik-Bug-Fläche:** mehrere aufeinanderfolgende feste Felder (Komma+Leerzeichen, Bindestrich neben
  Leerzeichen) sind die Stelle, an der ein Einzelschritt-Fix falsch bleibt, wenn er nicht als Schleife
  gebaut wird — genau das deckt Akzeptanzkriterium 5 ab.
- **Cache/PWA:** ein alter Service-Worker-Client, der `AnswerPattern` noch nicht kennt, fällt auf reines
  Tippen zurück (kein neues Risiko, additive Felder sind immer abwärtskompatibel) — keine Migration
  nötig, keine Sonderbehandlung.

### Angriffsplan (Backend zuerst)

1. **Backend:** `IExerciseType.StageFacets`-Tupel um `string? AnswerPattern` erweitern
   ([IExerciseType.cs:78](../../backend/Pugling.Api/Exercises/IExerciseType.cs),
   `ExerciseTypeBase.cs:44`, `VocabularyExerciseType.cs:92-95`,
   `BuiltInExerciseTypes.cs:54` unverändert außer Signatur); Muster-Berechnung (Buchstabe/Ziffer → `_`,
   sonst Zeichen behalten) als kleiner Helfer, z. B. neben `StageMechanics` oder direkt in
   `VocabularyExerciseType`.
2. **Backend:** `PositionPlayService.CardFacets` reicht das Feld durch
   ([PositionPlayService.cs:140-172](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs));
   `PracticeCard`, `TestItem`, `PreviewItem` bekommen je ein additives `AnswerPattern`-Feld mit
   `/// <summary>` (Contracts-Projekt); die drei Aufrufstellen
   (`PositionPracticeController.BuildCard`, `PositionTestsController.ToItem`,
   `ExercisePreviewService`) übernehmen es aus der Tupel-Projektion.
3. **Backend-Test:** Unit-Test für die Muster-Berechnung (mehrteilige Lösung, Satzzeichen, Unicode-Buchstabe)
   plus ein Integrationstest, der `PracticeCard`/`TestItem` für eine mehrteilige Vokabel abruft und
   `AnswerPattern` gegen die erwartete Maske prüft (neue Testklasse oder Ergänzung einer bestehenden
   Position-Play-Testklasse).
4. **Frontend:** `LetterBoxes.tsx` bekommt eine `pattern`-Variante (fixe Felder vorausgefüllt, nicht
   fokussierbar, in `setChar`/`onKeyDown` übersprungen — vorwärts wie per Backspace, auch über mehrere
   aufeinanderfolgende feste Felder hinweg).
5. **Frontend:** die drei Aufrufstellen (`SohnPractice.tsx:264`, `SohnTest.tsx:171`,
   `ExercisePreviewModal.tsx:189`) reichen `card.answerPattern`/`item.answerPattern` statt nur der Länge
   durch (Fallback auf reines `length`, falls `answerPattern` fehlt — Rückwärtskompatibilität zu Entscheidung 1).
6. **Frontend-Test:** neuer RTL-Komponententest zu `LetterBoxes` (Akzeptanzkriterium 5), vorher rot.
7. `npm run gen:contract` nach dem Backend-Merge, damit die Frontend-Typen nachziehen.

### Testweg

- Backend: neuer Unit-Test für die Muster-Berechnung + Ergänzung eines bestehenden Integrationstests in
  `Pugling.Api.Tests` (Position-Play-Pfad, `PracticeCard`/`TestItem`-Assertions).
- Frontend: neuer Vitest/RTL-Test `LetterBoxes.test.tsx` (Tippen über eine Trennstelle vorwärts und
  Backspace rückwärts).
- Manuell: `/smoke-test` deckt den Play-Pfad ohnehin ab (Auth/Ownership/Plan→Test→Submit); die Maske selbst
  braucht keinen eigenen E2E-Lauf, weil sie reines Rendering ist und der bestehende Sohn-Durchstich
  (`frontend/e2e/`) den Übungsfluss schon prüft.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #13 (Punkte 1 und 3; Punkt 2 zeigt auf
  [B-65](B-65-vokabel-mehrere-uebersetzungen.md)); Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#d--buchstabeneingabe-13-punkt-1-und-3).
- **2026-08-04** — gegrillt: Ist-Stand-Belege gegen den Code nachgeprüft (mehrere Zeilennummern
  korrigiert, u. a. `ExercisePreviewModal.tsx` um 59 Zeilen verschoben), zusätzlich `PreviewItem`/
  `ExercisePreviewService` als vierte hängende Stelle gefunden; alle fünf offenen Punkte in nummerierte
  Entscheidungen überführt (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — geschätzt: `groesse: M`, `wo: beides`, `migration: nein`, `vertragsbruch: nein`,
  Risiken, Angriffsplan (Backend zuerst) und Testweg ergänzt (autonom getroffen, Nutzerauftrag).
