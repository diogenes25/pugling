---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/student, rolle/supervisor]
aliases: [Taktung bis erledigt, UntilDone, Abschlussbonus]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
---

# B-636 · Eine Pflicht „täglich, bis erledigt“ mit Abschlussbonus

Teil D aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md). Eine Nacharbeit ist eine tägliche
Pflicht, die endet, sobald alle Aufgaben erledigt sind. Am Ende gibt es einmalig einen Bonus.

## User Story

Als *Vater* möchte ich, dass die Nacharbeit jeden Tag gespielt werden muss, bis sie erledigt ist, und danach
verschwindet. Für den Abschluss bekommt mein Sohn einen Bonus.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`), aus B-632:

- `GoalCadence` kennt `None`, `Daily` und `Weekly` (`backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:7-11`).
  `PeriodRange` behandelt jeden anderen Wert als Tag
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:35-39`).
- Eine Pflicht ist erfüllt durch einen bestandenen Test oder, bei Typen ohne Prüfung, durch eine gespielte Lernrunde
  (`IsGoalMetAsync` `:144-153`, `PlayedRoundMeetsGoalAsync` `:165-187`). Ein Lückentext ist `StudyPlanTest`, also
  bisher an den Test gebunden.
- Der Malus wird lazy über `SettleClosedPeriodsAsync` abgerechnet (`:558`).
- Belohnungen werden idempotent über `PositionGoalReward` gebucht
  (`backend/Pugling.Api/Models/PlanPositionEntities.cs:160`). `PointKind` steht in
  `backend/Pugling.Contracts/Common/AdminBaseTypes.cs:38`.

## Die echte Lücke

Es gibt keine Taktung, die endet. Bei einer Nacharbeit gilt die Tagespflicht durch eine gespielte Runde als erfüllt,
nicht durch einen Test. Einen Abschlussbonus gibt es nicht.

## Entscheidungen

Aus B-632, Entscheidung 5: Jeden Tag eine Runde durch alle offenen Aufgaben. Eine ganz gespielte Runde erfüllt die
Tagespflicht und bringt die eingestellten Münzen. Ein Tag ohne Runde kostet den eingestellten Malus. Mit der letzten
frei richtigen Aufgabe endet die Pflicht: kein Malus mehr, die Position verschwindet aus der Tagesmission, und
einmalig gibt es den Abschlussbonus.

## Akzeptanzkriterien

1. `GoalCadence.UntilDone` lässt sich am Positionsformular wählen, wenn die Einstellung „Nacharbeit“ (B-635)
   gesetzt ist.
2. Eine ganz gespielte Nacharbeitsrunde erfüllt die Tagespflicht, auch wenn danach noch Aufgaben offen sind. Die
   Pflicht-Regel aller anderen Positionen bleibt unverändert.
3. Ein Tag ohne Runde kostet den eingestellten Malus. Ein Tag nach dem Ende kostet nichts, auch wenn die Abrechnung
   ihn erst später nachholt.
4. Wenn die letzte Aufgabe erledigt ist, wird der Abschlussbonus genau einmal gebucht, mit eigenem `PointKind`. Die
   Position erscheint danach nicht mehr in der Tagesmission.
5. Der Vater stellt die Höhe des Abschlussbonus an der Position ein.

## Schätzung

**M.** Eine neue Taktung zieht durch die Pflicht-Regel, die Tagesmission, die Malus-Abrechnung und die Buchung.

- **`migration: ja`:** Die Höhe des Bonus ist eine Spalte an `PlanPosition`. Die idempotente Bonus-Zeile kommt
  nach dem Vorbild von `PositionGoalReward`, je nach Bau als eigene Tabelle. Die neuen Enum-Werte liegen als
  String in der DB und ändern das Schema nicht.
- **`vertragsbruch: nein`:** Die Enum-Werte und das Bonus-Feld sind additiv. `frontend/src/vater/PlanPositions.tsx`
  muss den neuen Wert kennen.
- **Abhängigkeit:** Ob eine Aufgabe erledigt ist, kommt aus B-635. Ohne B-635 kann diese Story nie enden.
- **Risiken:**
  - Ein Malus, der nach dem Ende rückwirkend anfällt.
  - Ein doppelt gebuchter Bonus.
  - Ein Zweig in `IsGoalMetAsync`, der die Regel anderer Positionen aufweicht.
- **Testweg:** in `RemediationPositionTests` (aus B-635) und `PositionGoalOverviewTests.cs`:
  - Eine Runde erfüllt die Pflicht.
  - Ein Tag ohne Runde kostet Malus, ein Tag nach dem Ende nicht.
  - Der Bonus wird einmal gebucht, auch wenn die Abrechnung zweimal läuft.
  - Die Position verschwindet aus der Tagesmission.
  - Dazu die Migrationsprobe gegen eine Kopie der echten DB.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil D), auf `geschaetzt` übernommen: Ist-Stand, Entscheidung und Schätzung
  stammen aus B-632.
- **2026-11-18** — gebaut (Sprint 86, [PM-Sitzung](../pm-sitzung-2026-11-18.md)). Migration `UntilDoneCompletionBonus`
  (Kette verlängert): `PlanPosition.CompletionBonus`, `PlanPosition.CompletedOn`, Tabelle `PositionCompletionBonuses`
  (eindeutig je Position). Senke `PositionProgressService.HasDutyOn`/`IsDailyDuty`; die Entscheidung je Aufrufer steht
  im Protokoll. Probe gegen eine Kopie der echten DB: 155 Positionen erhalten, 11 Nacharbeits-Positionen, keine
  `UntilDone`. **Fund der Probe:** eine schon fertige Nacharbeit ohne Endstempel endete nach dem Umstellen nie →
  `StampFinishedRemediationAsync` stempelt beim Umstellen, ohne Bonus.
  - AK 1, wählbar nur mit Nacharbeit (PATCH, POST) → `Bis_erledigt_nur_mit_Nacharbeit_und_Bonus_einstellbar`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:399), U15 → rot; im Formular `cadencesFor`/`withRemediation` (PlanPositions.test.ts:491/497), F5/F6.
  - AK 2, ganze Runde erfüllt trotz offener Aufgaben / abgebrochene nicht, auch bei Schwelle 50 % →
    `Ganz_gespielte_Runde_erfuellt_die_Tagespflicht_auch_mit_offenen_Aufgaben` (backend/Pugling.Api.Tests/RemediationPositionTests.cs:447), U3/U5 → rot; Balken 2/3 statt 0
    bzw. voll, U6/U7 → rot.
  - AK 3, Tag ohne Runde kostet Malus / Tage nach dem Ende nicht, auch nachgeholt →
    `Malus_fuer_den_Tag_ohne_Runde_nicht_nach_dem_Ende` (backend/Pugling.Api.Tests/RemediationPositionTests.cs:477), U4 → rot.
  - AK 4, Bonus genau einmal mit eigenem `PointKind`, nur durch die letzte Antwort → `Letzte_Aufgabe_bucht_den_Bonus_einmal_und_beendet_die_Pflicht`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:518), U12/U14 → rot; zweite Buchung nur vom eindeutigen Index gestoppt (Schnellweg im Test abgeschaltet).
  - AK 4, verschwindet aus der Tagesmission ab dem Folgetag, am Endtag „erfüllt“ → derselbe Test, U1/U8 → rot.
  - Aufrufer `EvaluateAndAwardAsync` (keine Ziel-Punkte nach dem Ende) → derselbe Test, U9 → rot.
  - Aufrufer `RewardObligations` (Truhe, Bonus-Training) → `Nacharbeit_bis_erledigt_sperrt_die_Belohnungen_wie_ein_Tagesziel`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:430), U2 → rot; Sohn-App `belohnungsZiele` (SohnHome.test.tsx), F3.
  - Aufrufer `DutyStateAsync` (Serie) → `Beendete_Nacharbeit_traegt_die_Serie_nicht_weiter` (backend/Pugling.Api.Tests/RemediationPositionTests.cs:646), U10 → rot.
  - Bonus nur bei „bis erledigt“, Umstellen zahlt nichts, stempelt aber → `Schon_fertige_Nacharbeit_endet_beim_Umstellen_ohne_Bonus`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:620), U11/U13/U19 → rot (U13 war im ersten Lauf grün: der Test stellte den Altbestand nicht her – nachgezogen).
  - AK 5, Bonushöhe einstellbar (POST, PATCH, Bereich) → backend/Pugling.Api.Tests/RemediationPositionTests.cs:399, U16/U17/U18 → rot; Formular hin und zurück
    (PlanPositions.test.ts:505), F7/F8.
  - Aufrufer `SettleClosedPeriodsAsync`: eine eigene `HasDutyOn`-Zeile dort wurde **entfernt** – keine Probe konnte sie
    rot machen (Doppelsicherung zu U4).

  Gemessen an `0f8f0328 + Arbeitsbaum geändert`, 2026-11-18 09:03 (U13/U19 nach der Nachbesserung an `7c642bb9 +
  Arbeitsbaum geändert`, 09:07); Befehl: `dotnet test backend/Pugling.Api.Tests -c Debug --no-build --filter
  "…RemediationPositionTests|…PositionGoalOverviewTests|…PflichtMalusTests"`.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | U1 | PositionProgressService.cs:56 | red | red |
  | U2 | PositionProgressService.cs:59 | red | red |
  | U3 | PositionProgressService.cs:158 | red | red |
  | U4 | PositionProgressService.cs:175 | red | red |
  | U5 | PositionProgressService.cs:180 | red | red |
  | U6 | PositionProgressService.cs:226 | red | red |
  | U7 | PositionProgressService.cs:233 | red | red |
  | U8 | PositionProgressService.cs:280 | red | red |
  | U9 | PositionProgressService.cs:487 | red | red |
  | U10 | PositionProgressService.cs:408 | red | red |
  | U11 | PositionProgressService.cs:545 | red | red |
  | U12 | PositionProgressService.cs:591 | red | red |
  | U13 | PositionProgressService.cs:602 | red | red (2. Lauf) |
  | U14 | PositionPracticeController.cs:786 | red | red |
  | U15 | PlanPositionsController.cs:141 | red | red |
  | U16 | PlanPositionsController.cs:143 | red | red |
  | U17 | PlanPositionsController.cs:230 | red | red |
  | U18 | PlanPositionsController.cs:356 | red | red |
  | U19 | PlanPositionsController.cs:375 | red | red (2. Lauf, kompilierbare Form) |

  Grundlinie nach dem Zurückspielen: green (beide Läufe).
- **2026-11-18** — Review (pugling-reviewer, frontend-reviewer, beide lesend): keine 🔴; eingearbeitet Y1 (Pool schrumpft
  → Rundenstart stempelt das Ende, ohne Bonus; der Vorschlag „Tag gilt als erfüllt“ wurde **verworfen**, weil eine nie
  endende erfüllte Pflicht täglich Ziel-Punkte ohne Spielen gezahlt hätte), Y2 (`paysCoins` an `HasDutyOn`, Ende
  endgültig), Y3 (Münzpfad der Runde), Bonus dauerhaft sichtbar statt nur im Jubel. Reste in
  [B-652](B-652-abschlussbonus-feld-nachschliff.md). Fälle dazu:
  - Pool geschrumpft, Start stempelt, ohne Bonus → `Geschrumpfter_Pool_beendet_die_Nacharbeit_beim_Start`, U20 → rot.
  - Keine Münzen nach dem Ende → `Nach_dem_Ende_zahlt_die_Position_keine_Muenzen`, U21 → rot.
  - Ganze Runde bucht Ziel-Punkte als Periode `UntilDone` → backend/Pugling.Api.Tests/RemediationPositionTests.cs `Ganz_gespielte_Runde…`, U22 → rot.

  **Endmessung nach der letzten Bearbeitung** (ersetzt die Tabelle oben): Backend an `7c642bb9 + Arbeitsbaum geändert`,
  2026-11-18 09:41, gleicher Befehl – U1–U22 und R1–R5 alle **red**, Grundlinie green.

  Frontend, gemessen an `7c642bb9 + Arbeitsbaum geändert`, 2026-11-18 09:52 (F5 an `10ef6195 + geändert`, 09:55);
  Vitest `src/vater/PlanPositions.test.ts src/sohn/pfad.test.ts src/sohn/SohnHome.test.tsx`, E2E
  `e2e/nacharbeit-bis-erledigt.spec.ts` auf 5211/5181:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | F1 | sohn/pfad.ts:36 (`istTagespflicht`) | red | red |
  | F2 | sohn/pfad.ts:49 (nächster Knoten) | red | red |
  | F3 | sohn/SohnHome.tsx:363 (`belohnungsZiele`) | red | red |
  | F4 | sohn/SohnHome.tsx:350 (`zielZaehler`) | red | red |
  | F5 | vater/PlanPositions.tsx:43 (`cadencesFor`) | red | red (3. Form) |
  | F6 | vater/PlanPositions.tsx:45 (`withRemediation`) | red | red |
  | F7 | vater/PlanPositions.tsx:308 (Bonus laden) | red | red |
  | F8 | vater/PlanPositions.tsx:363 (Bonus senden) | red | red |
  | F9 | vater/PlanPositions.tsx:1069 (`stufenZaehler`) | red | red |
  | F10 | sohn/SohnHome.tsx:32 (`cadenceKey`) | red | red |
  | E1 | vater/PlanPositions.tsx:482 (Bonusfeld) | red | red |
  | E3 | vater/PlanPositions.tsx:1037 (Nacharbeits-Report) | red | red |
  | E4 | sohn/SohnPractice.tsx:297 (Bonus in der Ansage) | red | red |

  Grundlinie nach dem Zurückspielen: green. Der 2-s-Jubel selbst ist kein Beleg (aria-hidden, sensorisch) – der Bonus
  steht seit dem Review dauerhaft in Ansage, Blatt und Rundensumme (E4).

  Verifikation: `dotnet test Pugling.sln -c Release` **1843/1843**, `tsc` sauber, Vitest **1211/1211**, E2E
  `nacharbeit-bis-erledigt.spec.ts` + `nacharbeit.spec.ts` **2 passed** (Ausweichports, Wegwerf-DB, Server von Playwright
  nach der letzten Bearbeitung gestartet).
- **2026-11-18** — abgenommen (`177027d9`): Rollengang Vater maschinell (`nacharbeit-bis-erledigt.spec.ts`, Server nach der letzten Bearbeitung gestartet), Sohn und Creator als Regressionszeugen; Protokoll [PM-Sitzung 2026-11-18](../pm-sitzung-2026-11-18.md). Offen als Gerätecheck (kein Abnahmehindernis, AK 1–5 halten): ob der große Jubel „Nacharbeit geschafft!“ am Handy wie ein TADAA wirkt.
