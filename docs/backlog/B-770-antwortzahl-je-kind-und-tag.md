---
tags: [typ/story, status/in-arbeit, bereich/dashboard, rolle/supervisor]
aliases: [Antwortzahl heute, ChildDay Antworten]
status: in-arbeit
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-662
grund: ""
ersetzt_durch: []
---

# B-770 · Die Zahl der heute gewerteten Antworten je Kind

`ChildDay` bekommt ein additives Feld `AnswersToday`: die Zahl der Antworten, die das Kind am Tag serverseitig gewertet bekam. Sie
beantwortet „wie viel" auf der Vater-Startseite, ohne Punkte als Ersatz zu nehmen. Abgespalten aus
[B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (Entscheidung 3); die Anzeige folgt in
[B-749](B-749-vater-startseite-entruempeln-und-einloesen.md).

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-662.*

## User Story

Als *Vater* möchte ich je Kind sehen, wie viele Aufgaben heute beantwortet wurden, damit ich den Fleiß vom Zufall der Punkte (Bonus,
Zeitfenster) unterscheide.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- `ChildDay(ChildId, Name, ActivePlans, GoalsTotal, GoalsMet, PointsToday, DutyDone, Practiced)` ist ein positionaler Record
  (`backend/Pugling.Contracts/Supervisor/GoalDtos.cs:36-37`), einmal gebaut in `Services/Supervisor/ChildrenDashboardService.cs:32`.
- `PointsToday` ist die Summe der Punkte der aktiven Pläne des Tages (`:30`); eine Antwortzahl: nicht gefunden.
- Antworten liegen als `ItemReviewEvent` (Üben, `Models/ItemProgressEntities.cs:61`) und als `TestItemResult` (Test,
  `Models/StudyPlanEntities.cs:209`, mit `ItemIndex is not null` für gewertete Zeilen, die Zeilen „falsche Nennung" tragen
  `ItemIndex == null`, `:213-220`) vor.
- Ein Tageskalender: `ChildrenDashboardService.BuildAsync` rechnet je Tag und Kind über `ComputeChildDayAsync`; die Zählung der
  Antworten muss denselben UTC-Tag nehmen.

## Entscheidungen

1. **„Wie gut" heißt Punkte heute und die Zahl der heute beantworteten Aufgaben je Kind; die Trefferquote kommt nur, wenn sie ohne
   neue Auswertung aus `PositionReportService` oder `MetricsService` zu holen ist** (B-662 Entscheidung 3). Kosten: ein neues Feld
   und eine Zählung über `ItemReviewEvent` und `TestItemResult`. Verworfen: Trefferquote als Pflichtzahl. Schließt aus: eine
   Qualitätsaussage auf der Startseite im ersten Bau.
2. **Die Antwortzahl zählt gewertete Antworten über alle Pläne des Kindes am UTC-Tag, nicht aus Punkten abgeleitet.** Kosten: ein
   Kind mit Bonus-Punkten und null Antworten zeigt 0 (gewollt). Folge: Zeilen mit `ItemIndex == null` zählen als Antwort des Kindes,
   weil es getippt hat, nicht als Treffer.

## Akzeptanzkriterien

1. Die Zahl ist die der heute gewerteten Antworten aus `ItemReviewEvent` und `TestItemResult`; ein Kind mit Bonus-Punkten und null
   Antworten zeigt 0. *Rot, wenn* die Zahl aus `PointsToday` entsteht.
2. Eine Antwort von gestern zählt nicht. *Rot, wenn* der Tagesfilter fehlt (ein gestern geübtes Kind zeigt heute > 0).
3. Eine Übungsantwort und eine Testantwort am selben Tag zählen beide. *Rot, wenn* nur eine der beiden Quellen gezählt wird.
4. Die Antwort eines anderen Kindes zählt nicht. *Rot, wenn* der Kindfilter fehlt.
5. Das Feld steht im Vertrag additiv; ein bestehender Aufrufer ohne das Feld verhält sich wie vorher. *Rot, wenn* ein bestehendes
   `ChildDay`-Feld seinen Namen oder Typ ändert (Absichtserklärung).

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01) mit einer Zählabfrage: ein Feld, zwei Zählungen, ein Aufrufer.

- **`wo: backend`**: die Anzeige folgt in B-749.
- **`migration: nein`**: nur Lesen vorhandener Spalten.
- **`vertragsbruch: nein`**: additives Feld am Ende des Records. Auf dem Draht kein Bruch; der einzige Konstruktoraufruf
  (`ChildrenDashboardService.cs:32`) zieht mit, `contract.ts` wird neu erzeugt.

### Risiken

- **Zwei Datumsbezüge:** `ItemReviewEvent` und `TestAttempt` tragen ihre Zeit nicht identisch benannt (Spaltennamen vor dem Bau
  lesen); beide müssen auf denselben UTC-Tag fallen wie `ComputeChildDayAsync`. Nahe Mitternacht wirkt der Fallstrick „Zeit/UTC".
- **Zählkosten je Kind und Tag:** `BuildAsync` läuft über alle Kinder in einer Schleife; zwei Abfragen je Kind wachsen mit der
  Kinderzahl. Bei drei Kindern unkritisch; der Zeitachsen-Pfad (`TimelineAsync`, höchstens 92 Tage) bekommt das Feld nicht.
- **Trefferquote:** bewusst nicht enthalten (Entscheidung 1); wer sie später will, baut sie aus `PositionReportService`.

### Angriffsplan

1. Feld `AnswersToday` an `ChildDay`; Zählung in `ChildrenDashboardService.BuildAsync`.
2. `contract.ts` neu erzeugen; `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- `ChildrenDashboardTests` (vorhanden, `Dashboard_SpiegeltTagessoll_VorUndNachTestabschluss`) um Fälle für AK 1 bis 4 ergänzen:
  Übungsantworten und eine Testantwort erzeugen, dann zählen; gestern über `TestClock` (`PuglingWebAppFactory.cs:68`).
- `ContractDocumentTests` läuft mit (AK 5).
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` mit einer geübten Karte. Ein Rollengang folgt mit B-749.

## Verlauf

- **2026-11-20** — angelegt aus [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) (geteilt); `geschaetzt`: S, backend, keine
  Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-20** — von B-748 auf B-770 umnummeriert: eine parallele Sitzung des Nutzers legte zeitgleich `B-748-vater-morgenuebersicht-tagesmission-lernstand-klausurchance.md` an (Id-Kollision; die Datei der anderen Sitzung behält B-748, weil sie vom Nutzer selbst stammt, und diese Story wich auf eine freie Id aus).
- **2026-11-20** — gebaut in Sprint 97. **Abweichung von Entscheidung 2, am Code begründet:** gezählt werden `ReviewEvent`
  (Üben, über `PracticeSession.Day`) und **beantwortete** `TestItemResult` (`GivenAnswer != null || WasCorrect`, über `TestAttempt.Day`,
  ohne `BySupervisor`), nicht `ItemReviewEvent` – ein Vokabeltest schreibt beides je Antwort (doppelt), Nicht-Vokabel-Üben schreibt kein
  `ItemReviewEvent` (fehlte). Der Start legt je Frage eine Zeile vorab an; ohne den Filter zählte ein abgebrochener Test voll
  (Review-Fund `pugling-reviewer`). Bewusst nicht gezählt, im `<summary>` benannt: Übungssitzung des Vaters (keine Akteur-Spalte),
  selbst eingeschätztes „nicht gewusst" im Test, Bonus-Training (planunabhängig). AK 1–4 → `ChildrenDashboardAnswersTests` (Quellen mit
  verschiedenen Zahlen 2/3/7/11/13/17/19, damit jeder fehlende Filter eine andere Summe ergibt; Punkte 0 neben 5 Antworten). AK 5 ist
  Absichtserklärung (additiv am Ende des Records). Mutanten:

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| B770-M1 ohne Uebung | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:59 | red | red |  |
| B770-M2 ohne Test | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:59 | red | red |  |
| B770-M4 Test ohne Tag | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:57 | red | red |  |
| B770-M5 Uebung ohne Kind | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:55 | red | red |  |
| B770-M6 Test ohne Kind | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:57 | red | red |  |
| B770-M7 aus Punkten | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:34 | red | red |  |
| B770-M8 Vater-Versuch zaehlt | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:57 | red | red |  |
| B770-M9 unbeantwortet zaehlt | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:58 | red | red |  |
| B770-M10 Selbsteinschaetzung fehlt | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:58 | red | red |  |
| B770-M3 Uebung ohne Tag | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:55 | red | red | Nachmessung kompilierbar (`e.PracticeSession!.StudyPlan!`), erste Fassung `ungemessen` |
