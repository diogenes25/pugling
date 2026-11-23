---
tags: [typ/story, status/abgenommen, bereich/api, bereich/training, rolle/student]
aliases: [Überholte Übungsrunde, Runde nach Stufenwechsel]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: ja
vertragsbruch: nein
quelle: "Nachschau von B-848 im Nachtlauf 2026-11-23-e (docs/pm-sitzung-2026-11-23-e.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-848]
wartet_auf: ""
---

# B-881 · Eine offene Übungsrunde von gestern spielt nach einem Stufenwechsel die falsche Übung

Schwester von [B-880](B-880-ueberholter-testversuch-nach-stufenwechsel.md): Eine Übungsrunde (`PracticeSession`) friert
ihre Reihenfolge als Item-Indizes ein und löst ihre Übung über `session.Day` auf. Wechselt der Vater heute die Stufe, sperrt
`InPlayTodayAsync` nur Runden von heute; eine offene Runde von gestern läuft über ihre Id weiter und wertet ihre Karten gegen
die Übung der neuen Stufe (Punkte, Kasten, Lernstand der falschen Übung).

## Ist-Stand am Code

- `PositionPracticeController` – `Cards` (`:386`), `ReshuffleImage` (`:428`), `Next` (`:477`), `Easier` (`:515`),
  `Review` (`:564`) laden die Runde über `GetSession` (`:80`) und lösen die Übung mit
  `PositionPlayService.PlayedExercise(pos, session.Day)` auf; keiner prüft, ob die Runde noch zur Übung dieses Tages gehört.
- `PracticeSession` trägt keine Stufe (`Models/StudyPlanEntities.cs`), der Testversuch seit B-849 schon
  (`TestAttempt.BuildLevel`), und B-880 weist überholte Versuche ab (`PositionTestsController.IsStale`).
- `PlanPositionsController.InPlayTodayAsync` sperrt den Stufenwechsel nur gegen Runden von **heute**.
- Die Sohn-App setzt keine Runde über ihre Id fort (kein `sessionId` in Storage); erreichbar nur über eine von Hand gebaute
  Anfrage – darum P3.

## Entscheidungen

Autonom gegrillt (Defekt, Freigabe 1, Nachtlauf E).

1. **Die Runde stempelt ihre Stufe wie der Versuch, und Runde und Versuch fragen über ein gemeinsames Prädikat, ob sie
   überholt sind.** `PracticeSession.BuildLevel` = `ExaminedBuildLevel(pos, day)` beim Start; überholt ist eine Runde,
   deren Stempel nicht mehr die Stufe ist, auf die ihr Tag auflöst. `Cards`, `ReshuffleImage`, `Next`, `Easier` und
   `Review` antworten dann 404 wie B-880. Begründung: dieselbe Frage wie beim Versuch, dieselbe Antwort – die Retro von
   Sprint 122 hat gerade zwei Lesarten einer Frage als Fehlerquelle gemessen. Kosten: eine nullable Spalte (Migration,
   Bestand aus `CurrentBuildLevel` gefüllt wie bei B-849), fünf Prüfungen. Verworfen: Runden beim Stufenwechsel des Vaters
   beenden (`EndedAt`) – eine beendete Runde bleibt heute beantwortbar, das bräuchte eine zweite Regel; Runden früherer
   Tage für das Kind abweisen – bricht die Regel aus B-849, dass eine Runde über Mitternacht auf ihrer Übung bleibt.
   Schließt aus: Punkte, Kasten und Lernstand einer Übung aus Karten einer anderen.
2. **`End` und `Get` bleiben offen.** Eine überholte Runde darf beendet und gelesen werden: `End` wertet nur das Ziel des
   Tages (das liest keine Karte), `Get` gibt nichts von der Übung heraus. Kosten: keine. Schließt aus: eine Runde, die sich
   nach dem Wechsel nicht mehr schließen lässt.

## Akzeptanzkriterien

1. Eine Runde von gestern auf Stufe 2; der Vater setzt heute Stufe 1: `Next` und `Review` antworten 404, der Lernstand
   der Stufe-1-Übung bleibt unberührt. Rot: einer der beiden Pfade ohne Prüfung.
2. `Cards`, `Easier` und `ReshuffleImage` antworten auf dieselbe Runde 404. Rot: je ein Pfad ohne Prüfung.
3. Eine Runde über Mitternacht ohne Stufenwechsel (B-849) spielt weiter. Rot: eine Prüfung, die den heutigen Tag statt des
   Runden-Tags liest (bestehender Test `Runde_Ueber_Mitternacht_Bleibt_Auf_Ihrer_Stufe`).
4. Die Migration nimmt Runden mit; Runden gestufter Positionen bekommen deren Stufe, andere `null`.

## Schätzung

**Größe: S** – wie B-880 (dieselbe Prüfung an fünf statt drei Stellen), plus eine nullable Spalte.
`migration: ja` (`PracticeSessions.BuildLevel`), `vertragsbruch: nein`, `wo: backend`.

### Angriffsplan

1. `PositionPlayService.IsOvertaken(pos, stamped, day)`; `PositionTestsController.IsStale` darauf umstellen.
2. Spalte + Migration `PracticeSessionBuildLevel`, Stempel im `Start`.
3. Prüfung in den fünf Pfaden.

### Testweg

`AufbauStufeAufstiegTests` um die Runde erweitern (AK 1–3), Migrationstest nach dem Muster
`AufbauStufeAufstiegMigrationTests` (AK 4). Mutanten je Pfad.

## Verlauf

- **2026-11-23** — Bei der Nachschau von B-848 gefunden (Nachtlauf E).
- **2026-11-23** — ausformuliert, gegrillt, geschätzt (Nachtlauf E, Sprint 123, autonom als Defekt): am Code belegt (Ist-Stand
  oben), zwei Entscheidungen, S.
- **2026-11-23** — in-arbeit, gebaut nach Angriffsplan: `PositionPlayService.IsOvertaken` (B-880 umgestellt),
  `PracticeSession.BuildLevel` im `Start` gestempelt, Prüfung in `Cards`/`ReshuffleImage`/`Next`/`Easier`/`Review`,
  Migration `PracticeSessionBuildLevel`. Eigener Fund bei der Probe (zählt, Freigabe 11): die erste Fassung von
  `Ueberholte_Runde_Wird_Nicht_Mehr_Gespielt` prüfte nur „404“ – `ReshuffleImage` antwortet bei einer Rechenkarte auch ohne
  Prüfung 404 (keine Vokabel), Mutante R2 blieb grün (gemessen: erwartet rot, gemessen grün). Jetzt verlangt der Test je
  Pfad den Text der überholten Runde; R2 rot.
- **2026-11-23** — Review `pugling-reviewer` (nur gelesen): keine 🔴, ein 🟡 behoben (zählt): die Fortsetzung der offenen
  Nacharbeitsrunde im `Start` (B-635) gab eine überholte Runde zurück – eine vom Kind beendete Runde sperrt den Vater
  nicht (`InPlayTodayAsync` liest `EndedAt`), jeder weitere Start lieferte dieselbe 404-Runde, das Kind war für den Tag
  ausgesperrt. Jetzt setzt `OpenRemediationRoundAsync` keine überholte Runde fort; Test
  `Nacharbeit_Setzt_Keine_Ueberholte_Runde_Fort`. 🟢 ohne Zählung: die Füllung der Migration stempelt eine Runde vom Tag
  eines wartenden Aufstiegs mit der neuen Stufe – im Migrations-Kommentar als hingenommen benannt (nur DBs, die
  `BuildLevelPromotion` schon allein hatten; eine neue Runde lässt sich starten). Fehlerzähler Sprint 123: 2.
- **2026-11-23** — AK → Test (`AufbauStufeAufstiegTests`), Mutante → rot (gemessen nach der letzten Änderung, 10/10):
  AK 1+2 → `Ueberholte_Runde_Wird_Nicht_Mehr_Gespielt` (R1–R5 je Pfad, R9 Prädikat); AK 3 →
  `Runde_Ueber_Mitternacht_Bleibt_Auf_Ihrer_Stufe` (R6 kein Stempel, R7 Prüfung mit „heute“); AK 4 →
  `AufbauStufeAufstiegMigrationTests.Runde_Bekommt_Die_Stufe_Ihrer_Position` (R8); Nacharbeit →
  `Nacharbeit_Setzt_Keine_Ueberholte_Runde_Fort` (R10). Suite 2173/2173 (Release); E2E `nacharbeit`,
  `nacharbeit-bis-erledigt`, `ueben-ohne-leitner`, `uebung-abbruch`, `aufbau-weg-editor`, `test-durchgefallen-ueben`
  6 passed (Ausweichports); Migration an einer Kopie der echten DB: 226 Runden, 118 Versuche, alle ohne Stempel.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| R1 Cards ohne Pruefung | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:403 | red | red |  |
| R2 ReshuffleImage ohne Pruefung | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:442 | red | red |  |
| R3 Next ohne Pruefung | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:494 | red | red |  |
| R4 Easier ohne Pruefung | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:534 | red | red |  |
| R5 Review ohne Pruefung | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:585 | red | red |  |
| R6 kein Stempel | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:119 | red | red |  |
| R7 Pruefung mit heute | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:494 | red | red |  |
| R8 Migration ohne Fuellung | backend/Pugling.Api/Data/Migrations/20261123165222_PracticeSessionBuildLevel.cs:28 | red | red |  |
| R9 Test-Pruefung immer frisch | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:86 | red | red |  |
| R10 Nacharbeit setzt ueberholte Runde fort | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:154 | red | red |  |

Grundlinie nach dem Zurückspielen: green.

- **2026-11-23** — abgenommen (Nachtlauf E, Sprint 123), Commit `e5c2430c`. Rollengang maschinell: Sohn über die
  HTTP-Pipeline (überholte Runde, Nacharbeit startet neu), Vater als Auslöser über die Supervisor-API (Stufe setzen),
  Creator Regressionszeuge (E2E). Mensch offen: Migration `PracticeSessionBuildLevel` beim nächsten Heimnetz-Start.
