---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/student]
aliases: [Lösungsmenge auswählen, Distraktoren Gleichung]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-640
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-711 · „Gleichung lösen“: Stufe „Lösungsmenge auswählen“ mit berechneten falschen Mengen

Zweiter Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md): die Auswahlstufe des Typs aus
[B-710](B-710-gleichung-loesen-typ-und-editor.md). Das Kind wählt die richtige Lösungsmenge aus bis zu vier Optionen; die
falschen bildet C# aus den Koeffizienten. Die Stufe fragt dasselbe Soll wie die getippte Stufe (die Menge) in anderer Form –
genau das Muster der Zuordnung (`MatchStage.Distractors` gegen `Direct`).

**Reihenfolge:** nach B-710 (der Typ muss stehen). Unabhängig von [B-712](B-712-sohn-eingabe-loesungsmenge.md).

## User Story

Als **Schüler** möchte ich zuerst aus Lösungsmengen auswählen, bevor ich selbst tippe, damit ich das Ergebnis erkenne, bevor
ich es aus dem Kopf schreiben muss.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`.

- **Das Muster steht:** `MatchingExerciseType.Choices` liefert die Optionen der Auswahlstufe
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:449`–`:450`: `MatchStage.Distractors` über
  `StageMechanics.DistractorPool`, `MatchStage.Direct` bleibt `null`); `StageOptions` (`:404`) und `DefaultStage` (`:428`)
  nennen die Stufen; `IExerciseType.Choices(configJson, items, item, stage)` (`IExerciseType.cs:134`).
- **Die Stufenliste ist die Stufenvalidierung:** `StageValidation` prüft eine gesetzte Stufe gegen `StageOptions`; eine leere
  Liste heißt „jede Stufe erlaubt“ (`backend/Pugling.Api/Exercises/StageValidation.cs:24`–`:37`). B-710 lässt sie leer, B-711
  füllt sie.
- **Die Karte zeigt Optionen generisch:** `karte.choices` rendert Auswahlknöpfe (`frontend/src/sohn/Lektion.tsx:166`–`:174`);
  der gewählte Text wird als Antwort geschickt – eine Option `2;4` trifft also das Mengen-Atom aus B-710, wenn die Optionen in
  **derselben Schreibweise** (aufsteigend, `;`) stehen wie die Antwort. Ein Frontend-Eingriff ist nicht nötig.
- **Falsche Mengen aus Koeffizienten:** nicht gefunden (kein Löser, kein Distraktor-Erzeuger für Zahlen; der Vokabel-Pool
  `StageMechanics.DistractorPool` nimmt Antworten der anderen Atome).
- Die Option mit der richtigen Antwort wird in anderen Typen über `GradedAnswers()` aus den falschen herausgehalten
  (`ExerciseContentProvider.cs:56`); für Mengen gilt dieselbe Sorge.

## Die echte Lücke

Der Typ braucht eine **Stufenliste** (`Direct`, `Distractors`) und einen **Erzeuger für falsche Mengen**. Anders als in der
Zuordnung kommen die falschen Optionen nicht aus den anderen Atomen (das wären andere Gleichungen), sondern aus **typischen
Fehlern** derselben Gleichung.

## Offene Punkte

Keine; die Fachfrage nach den Fehlermodellen ist in Entscheidung 2 entschieden, ob die Liste didaktisch reicht, prüft der
Rollengang (Entscheidung 2, Kosten).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Zwei Stufen mit demselben Soll: „Lösungsmenge auswählen“ (`Distractors`) und „Lösungsmenge tippen“ (`Direct`)** (B-640
   Nr. 6). Die Auswahlstufe liefert die richtige Menge und bis zu drei falsche, deterministisch je Aufgabe. Begründung: genau das
   Muster der Zuordnung. **Verworfen:** die vier Stufen der Idee in einem Zug – zwei davon (Anzahl, Verfahren) fragen ein anderes
   Soll (offene Punkte 1 und 2 in [B-710](B-710-gleichung-loesen-typ-und-editor.md)). **Ausgeschlossen – gegen die User Story
   gerechnet:** der Schüler *bestimmt* die Anzahl der Lösungen oder das Verfahren nicht als eigene Frage. **Kosten:** die
   Stufenleiter der Idee ist nur teilweise erfüllt.
2. **Die falschen Mengen entstehen aus drei Fehlermodellen: Vorzeichen vertauscht, nur eine der beiden Lösungen, Lösungen der mit
   `p` statt `p/2` gerechneten Formel; paarweise verschieden und nie gleich der richtigen.** Fehlt ein Modell (Gleichung mit nur
   einer oder ohne Lösung), füllen Abwandlungen auf (eine Lösung ±1, die Menge `keine Lösung`); gibt es weniger als drei
   verschiedene, werden es weniger Optionen. Begründung: das sind die Fehler, die ein Kind bei der pq-Formel tatsächlich macht –
   eine zufällige Zahl wäre erkennbar falsch. **Verworfen:** Zufallszahlen und die Mengen anderer Gleichungen derselben Übung.
   **Ausgeschlossen:** dass `keine Lösung` bei einer Gleichung mit Lösungen als Option fehlt – sie ist ein gültiger falscher
   Wert. **Kosten:** die Wahl ist eine Fachentscheidung; der Rollengang muss prüfen, ob die falschen Mengen plausibel sind.
3. **Die Optionen stehen in der Antwort-Schreibweise `x1;x2` (aufsteigend, `;`, Dezimalpunkt) und sind deterministisch** –
   gleiche Reihenfolge bei jedem Aufruf, gemischt über einen Wert aus den Koeffizienten, nie über `Random`. Begründung: das
   Kind schickt den Optionstext als Antwort, er muss das Mengen-Atom aus B-710 treffen; ein Wechsel zwischen zwei Aufrufen
   ließe die Karte beim Neuladen springen. **Verworfen:** Anzeige mit Komma (der Text ginge nicht durch den Mengenvergleich).
   **Kosten:** das Kind sieht `2;4`, nicht „{2, 4}“.
4. **`DefaultStage` bleibt `Direct` (getippt).** Begründung: ein Plan ohne Stufenangabe soll abfragen, nicht anbieten; die
   Auswahl ist die leichtere Vorstufe, die der Vater wählt. **Verworfen:** `Distractors` als Vorgabe.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. `StageOptions` des Typs nennt genau `Direct` und `Distractors`; `Choices` liefert für `Distractors` die richtige Menge und
   bis zu drei falsche, paarweise verschieden und nie gleich der richtigen, für `Direct` `null`. *Rot, wenn:* die Optionen die
   richtige Menge doppeln oder `Direct` Optionen liefert.
2. Zwei Aufrufe mit denselben Koeffizienten liefern dieselbe Optionsliste in derselben Reihenfolge. *Rot, wenn:* `Random`
   oder ein Zeitwert in die Reihenfolge eingeht.
3. Für `x² − 6x + 8 = 0` enthalten die falschen Optionen die vertauschten Vorzeichen (`-4;-2`) und eine einzelne Lösung (`2`
   oder `4`); für `x² + 1 = 0` ist `keine Lösung` die richtige Option und alle anderen sind Mengen. *Rot, wenn:* eines der
   Modelle fehlt (ein Fall je Modell).
4. Eine Option, die das Kind wählt, wird als Antwort richtig bzw. falsch gewertet wie getippt. *Rot, wenn:* die Option
   `2;4` für `1, −6, 8` als falsch gewertet wird (Schreibweise weicht vom Mengen-Atom ab).
5. Eine Stufe außerhalb der Liste wird beim Zuweisen abgelehnt (`StageValidation`). *Rot, wenn:* der Typ `StageOptions` leer lässt.
6. `DefaultStage` ist `Direct`. *Rot, wenn:* eine Position ohne Stufenangabe die Auswahlstufe spielt – **Absichtserklärung**,
   soweit die Vorgabe schon so steht; sie sichert nur, dass sie beim Umbau nicht kippt.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: ein klar umrissener Eingriff an einer Typklasse mit eigener Logik
(Fehlermodelle, Eindeutigkeit, Determinismus), ohne Schema und ohne Frontend-Arbeit, weil die Karte Optionen schon rendert.

- **`migration: nein`** — nachgesehen: Stufen sind `int`-Werte in bestehenden Spalten; die Enum liegt im Code.
- **`vertragsbruch: nein`** — nachgesehen: `Choices` und `StageOptions` sind intern im Typ-Vertrag (`IExerciseType`); das Manifest
  führt die Stufen schon heute (`ExerciseTypeRegistry.cs:32`–`:44`), kein neues Feld in `Pugling.Contracts`.
- **`wo: backend`** — das Frontend rendert `karte.choices` generisch (`Lektion.tsx:166`); der Stufenwähler des Editors liest die
  Stufen aus dem Server-Manifest (`DefaultStageSelect`).

### Risiken

- **Zu wenige verschiedene falsche Mengen** bei Doppellösung und bei Lösungen wie 0 (die Vorzeichen-Vertauschung gleicht der
  richtigen): der Erzeuger muss deduplizieren und mit weniger als drei Optionen leben; ein Fall je Sonderfall.
- **Sortierung und Dezimalschreibweise:** die Optionen laufen durch dieselbe Formatierung wie die Antwort in B-710; weicht eine
  Stelle ab (etwa `2.0` statt `2`), trifft die Option das Mengen-Atom nicht. AK 4 prüft das Ende zu Ende.
- **Manifest meldet jetzt Stufen:** die Berechnung `SelfAssessedTest` (`ExerciseTypeRegistry.cs:42`–`:44`) hängt an
  `IsTypedStage`; beide Stufen sind getippt, der Wert muss `false` bleiben – ein Fall im Manifest-Test.

### Angriffsplan

1. `QuadraticEquationStage { Distractors, Direct }` (Namen nach dem Muster `MatchStage`), `StageOptions`, `DefaultStage`,
   `IsTypedStage` (beide `true`).
2. Erzeuger für falsche Mengen (rein, ohne Zustand): drei Modelle, Auffüllen, Deduplizieren, deterministische Mischung.
3. `Choices(configJson, items, item, stage)` für `Distractors`; Formatierung über dieselbe Funktion wie `ItemsOf`.
4. `dotnet build Pugling.sln` und `dotnet format Pugling.sln`.

### Testweg

- `backend/Pugling.Api.Tests/QuadraticEquationChoicesTests.cs` (neu, nach dem Muster `PositionPlayChoicesTests`) – AK 1, 2, 3 mit
  je einem Fall je Fehlermodell und Sonderfall (Doppellösung, keine Lösung, Lösung 0). Rote Probe je Fall: das Modell aus dem
  Erzeuger streichen.
- `PositionPracticeFlowTests` (oder `PositionPlayChoicesTests`) – eine Position auf `Distractors`: Option wählen, richtig bzw.
  falsch (AK 4, 5).
- Manifest-Test der Registry (AK 5, 6) – vorhandene Tests um den Typ ergänzen.
- Kein E2E: die Auswahl rendert dieselbe Komponente wie jede andere Auswahlstufe; der Rollengang als Sohn (Karte mit
  Auswahlknöpfen an der Gleichung) belegt die Darstellung.
- Vor der Abnahme: `pugling-reviewer`; Rollengang als Sohn.

## Verlauf

- **2026-11-19** — angelegt als Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md) (geteilt, Vorbereitungslauf, Sonnet):
  Auswahlstufe (AK 7 der Ursprungsstory).
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, backend, keine Migration, kein Vertragsbruch; hängt an B-710.
  `wartet_auf` bleibt.
- **2026-11-19** — Entscheidungen bestätigt. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
- **2026-11-25** — in-arbeit, Sprint 171 (Nachtlauf M). Stufenwerte wie `MatchStage` (`Direct = 1`, `Distractors = 2`);
  Auffüller über die Fehlermodelle hinaus: Radikand mit falschem Vorzeichen, dessen Einzelwerte, Scheitel, Lösung ±1, leere
  Menge. Die Auswahlkarte trägt den Trenner-Hinweis nicht (`CardForStage`).
- AK 1, richtige einmal, falsche verschieden, `Direct` ohne Optionen → `backend/Pugling.Api.Tests/QuadraticEquationChoicesTests.cs:38` (6 Gleichungen),
  `:77` (zweiter richtiger Knopf in der Toleranz); M22 (Optionen auf `Direct`), M23 (nur Textvergleich) → rot.
- AK 2, feste Reihenfolge → `…ChoicesTests.cs:53` (probabilistische Probe, 1:24 bei `Random`).
- AK 3, je Fehlermodell → `…ChoicesTests.cs:65`, ohne Lösung `:88`, Doppellösung/Null `:102`; M9, M10, M11, M12 → rot.
- AK 4, Option gewertet wie getippt → `…ChoicesTests.cs:117`, API `backend/Pugling.Api.Tests/QuadraticEquationApiTests.cs:149`.
- AK 5, fremde Stufe abgelehnt → `…ApiTests.cs:128`; Manifest-Pinning `ExerciseTypeManifestTests` (Stufen-Enum).
- AK 6 (Absichtserklärung), Vorgabe `Direct` → `…ChoicesTests.cs:127`; M24 → rot; `SelfAssessedTest = false` im
  Manifest-Test.
- **2026-11-26** — abgenommen (Sprint 171, Nachtlauf M). Code `a961897b`. Backend 2542 passed (`dotnet test Pugling.sln
  -c Release`), Vitest 1488 passed, `tsc -b` sauber, Playwright 4 passed (`gleichung-loesen`, `uebungstypen`, `lektion`,
  `rechnen-mit-komma`); Mutanten 26/26 (Backend) und 16/16 (Frontend) rot. Review `pugling-reviewer` und `frontend-reviewer`
  eingearbeitet. Rollengang aller drei Rollen an einer Kopie der echten DB (:5241) plus UI-Spec; offen für den Menschen nur
  der Geräte-Check (Zahlentastatur am Handy) und die Formel-Darstellung (B-638).
  Protokoll: [pm-sitzung-2026-11-25-m](../pm-sitzung-2026-11-25-m.md).
