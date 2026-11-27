---
tags: [typ/story, status/in-arbeit, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Pflichtquelle Tagesaufgabe, scharfer Tag, Quellenumschaltung]
status: in-arbeit
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-736
grund: ""
ersetzt_durch: []
---

# B-1036 · Im scharfen Modus kommt die Pflicht des Tages aus den Tagesaufgaben (Quellenumschaltung)

Erster Teil aus der Teilung von [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) (Nachtlauf P, 2026-11-27): die **Lesequelle** der Pflicht.
Die Abrechnung je Aufgabe ist [B-1037](B-1037-malus-je-tagesaufgabe-und-rueckstand.md), die Dosis aus der Aufgabe
[B-1038](B-1038-dosis-aus-der-tagesaufgabe.md). Kriterium 6 von B-736 (gestrichene Aufgabe) liegt bei
[B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md), das das Streichen erst baut (dort Kriterium 5).

## User Story

Als *Vater* möchte ich, dass an einem Tag mit scharfem Tagesplan nur die Aufgaben dieses Plans Pflicht sind – Serie, Tagessicht, Truhe
und Zielpunkte lesen sie –, damit kein Parallelbetrieb mit der alten Positions-Pflicht entsteht. Als *Sohn* möchte ich, dass ein Tag
ohne Plan wie eine Pause zählt.

## Ist-Stand am Code

Gemessen am 2026-11-27 auf `main` (`8c4395de`). Pfade relativ zu `backend/Pugling.Api/`.

- `PlannerMode` kennt nur `Off` und `Shadow` (`Pugling.Contracts/Supervisor/DailyPlanDtos.cs:9`); `DailyPlan.Mode` ist eine Momentaufnahme.
- Jede Frage „ist das heute Pflicht?“ läuft über `DutyCadenceOn`/`IsObligationOn` an der Position (`PositionProgressService.cs:89`, `:111`):
  Tagessicht `ComputeDayAsync` (`:405`), Pflichtzustand `DutyStateAsync` (`:571`) für Serie (`:590`), erfüllte Tage (`:625`) und Zeitachse
  (`ChildrenDashboardService.cs:107`), Zielpunkte `AddGoalRewardIfDueAsync` (`:685`), alter Malus `SettleClosedPeriodsAsync` (`:897`).
- Welche Tage zählen, sagt `ChildDutyDays` (`DutyCalendar.cs:29`) – Sonntag und Pause. Ein Tag ohne Plan kennt es nicht.
- Der Planer nimmt Kandidaten aus jeder Position eines aktiven Plans, auch ohne Pflicht (`DailyPlanner.cs:157`, Filter nur für
  „bis erledigt“).
- Das Vater-Web schaltet nur `Off`/`Shadow` (`frontend/src/vater/Tagesplaene.tsx:60`); seine zwei `switch` über den Modus sind
  erschöpfend, damit ein neuer Modus `tsc` rot macht (`:28`, `:37`).

## Entscheidungen (geerbt)

Aus B-736 (dort mit Herkunft): 1 (Serie reißt an offener Pflichtaufgabe), 2 (Tag ohne erzeugten Plan zählt wie Pause), 4 (Münzen hängen an
der erfüllten Pflicht; B-680 Entscheidung 6: „Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht“), 5 (kein
Parallelbetrieb), 5 der Nachträge (Kür wird nie Tagesaufgabe, Antwort auf B-1033).

### Schätz-Annahmen (stellvertretend, unbestätigt — Nachtlauf P)

1. **Ein Tag ist „scharf“**, wenn sein gespeicherter Plan den Modus scharf trägt, oder wenn das Kind jetzt scharf ist und der Tag nicht vor
   `PlannerSince` liegt. Die Momentaufnahme hält die Vergangenheit fest, der Kindzustand deckt Tage, an denen kein Plan entstand.
   Folgerung, kein neuer Entscheid: ein scharfer Tag ohne Plan oder mit einem Plan ohne Aufgaben schuldet nichts (Entscheidung 2).
2. **Eine Aufgabe wird am Tag gemessen**, mit der Ziel-Regel ihrer Position: die Taktung der Position gilt an scharfen Tagen nicht
   (Entscheidung 5), also ist die Periode der Tag – auch für eine Position mit Wochenziel. Eine Nacharbeit „bis erledigt“ behält ihre
   Tagesregel (eine Runde durch die offenen Aufgaben).
3. **Zielpunkte** bucht an einem scharfen Tag nur die Position mit Aufgabe, als Tagesziel (`PointsGoalMet` der Position, eine Zeile je
   Tag); eine Position ohne Aufgabe bucht keine (B-680 Entscheidung 6).
4. **Den Modus „scharf“ setzt die API noch nicht** – der Schalter kommt mit dem Reset ([B-737](B-737-reset-zum-stichtag.md)). Bis dahin
   antwortet `PATCH children/{id}` mit `400 validation_error`.
5. **Der angezeigte Malus einer Aufgabe ist 0**, bis [B-1037](B-1037-malus-je-tagesaufgabe-und-rueckstand.md) ihn bucht: kein Preis, der
   nie berechnet wird (B-1022).

## Akzeptanzkriterien

1. An einem scharfen Tag ist genau die Position mit Tagesaufgabe Pflicht: Tagessicht (`GoalsTotal`, `DutyDone`, `Outstanding`) und
   Pflichtzustand lesen die Aufgaben. *Rot, wenn* eine Position mit Taktung `Daily` ohne Aufgabe als Pflicht erscheint oder eine Aufgabe
   einer freien Position fehlt.
2. Im Schattenmodus und ohne Planer bleibt alles wie vorher. *Rot, wenn* ein Schattenplan Pflicht erzeugt (Regressionsnetz unverändert grün).
3. Ein scharfer Tag ohne Plan, ein scharfer Plan ohne Aufgaben, ein Sonntag und ein Pausentag lassen die Serie stehen und zeigen in der
   Zeitachse keine Pflicht. *Rot, wenn* die Serie dort bricht.
4. Die Serie zählt einen scharfen Tag, wenn alle Aufgaben erfüllt sind, und bricht an der ersten offenen. *Rot, wenn* `ChildStreakAsync`
   an einem scharfen Tag aus Positionen rechnet.
5. Truhe und Bonus-Training (`RewardsUnlocked`) öffnen an einem scharfen Tag erst, wenn alle Aufgaben erfüllt sind. *Rot, wenn* sie bei
   offener Aufgabe öffnen.
6. Eine Aufgabe einer Position mit Wochenziel ist am Tag erfüllt, nicht durch eine Runde vom Vortag derselben Woche. *Rot, wenn* die
   Wochenperiode gilt.
7. Zielpunkte bucht an einem scharfen Tag nur eine Position mit Aufgabe, einmal je Tag. *Rot, wenn* eine Position ohne Aufgabe Zielpunkte
   bekommt oder eine Aufgabe zweimal.
8. Der alte Malus an der Position bucht für scharfe Tage nichts. *Rot, wenn* `SettleClosedPeriodsAsync` einen scharfen Tag abrechnet.
9. Der Planer macht aus einer Kür-Position (Taktung „keine“, Pflicht nach `DutyUntil` beendet) nie eine Aufgabe, in beiden Modi. *Rot,
   wenn* eine Kür-Position im Plan steht.
10. `PATCH children/{id}` mit Modus scharf antwortet `400 validation_error`. *Rot, wenn* der Modus über die API setzbar ist.
11. Das Vater-Web beschriftet einen scharfen Plan und Modus (`planWirkung`, `planerStatus`). *Rot, wenn* `tsc` fällt.

## Schätzung

**Größe: M** — eine Quelle (`ChildDutyDays` trägt die Aufgaben der scharfen Tage) und ein Helfer für die Taktung eines Tages, den die
fünf Leser statt `DutyCadenceOn` fragen; dazu der Kür-Filter, der API-Riegel und zwei Zeilen im Vater-Web. Keine Migration (`PlannerMode`
liegt als Text), kein Vertragsbruch (ein Enum-Wert dazu).

### Risiken

- Ein Leser, der weiter `DutyCadenceOn` fragt, zeigt dem Kind ein anderes Bild als die Serie. Vorab die Leserliste per `grep`.
- `DailyPlanner` und `DailyEvaluationService` brauchen den **Kalender** ohne Aufgaben (Lerntage), sonst plant der Planer nie.

### Angriffsplan

1. Enum-Wert `Enforced`, API-Riegel, Vater-Web-Zeilen.
2. `ChildDutyDays`: Aufgaben je scharfem Tag, `OwesDutyOn(day)` (Lerntag und, an scharfen Tagen, mindestens eine Aufgabe).
3. `PositionProgressService.CadenceOn(pos, day, dutyDays)`; Tagessicht, Pflichtzustand, Serie, erfüllte Tage, Zeitachse, Zielpunkte und alter
   Malus darauf umstellen.
4. Kür-Filter im Planer.

### Testweg

- Neue Klasse `TagespflichtScharfTests` (Integration, `TestClock`, scharfer Plan direkt in der DB angelegt): je Kriterium ein Fall.
- Regressionsnetz: `PflichtMalusTests`, `BelohnungenTagespflichtTests`, `BelohnungUeberPlaeneTests`, `DailyBoxTests`, `KindTagessichtTests`,
  `TagesplanerTests` unverändert grün.
- Mutanten je Leser.

## Verlauf

- **2026-11-27** — angelegt durch Teilung von B-736 (Nachtlauf P), `geschaetzt` mit den geerbten Entscheidungen und den fünf
  Schätz-Annahmen oben.
- **2026-11-27** — **gebaut** (Nachtlauf P, Sprint 198). Senke `PositionProgressService.CadenceOn` über `ChildDutyDays.TaskPositionsOn`;
  Fälle in `backend/Pugling.Api.Tests/TagespflichtScharfTests.cs`:
  - AK 1, Aufgabe Pflicht / Position ohne Aufgabe frei / freie Position mit Aufgabe Pflicht / Preis 0 / `owesDuty` → `Nur_Die_Aufgaben_Sind_Die_Pflicht_Des_Tages` (:159), Mutanten S6, S10.
  - AK 1, Nacharbeit als Aufgabe behält ihre Tagesregel → `Nacharbeit_Als_Aufgabe_Behaelt_Ihre_Tagesregel` (:323), S7.
  - AK 2, Schatten-Kind mit `PlannerSince` und Schattenplan → `Schattenplan_Aendert_Die_Pflicht_Nicht` (:184), S1, S2.
  - AK 3, Tag ohne Plan / Plan ohne Aufgaben, Serie und Missions-Serie → `Tag_Ohne_Plan_Und_Plan_Ohne_Aufgaben_Halten_Die_Serie` (:211), S3, S5, S13, S16; Zeitachse → `Zeitachse_Zeigt_Tage_Ohne_Aufgabe_Ohne_Pflicht` (:233), S20.
  - AK 4, Serie aus Aufgaben → `Serie_Zaehlt_Die_Aufgaben_Und_Bricht_An_Der_Offenen` (:281), S11; am Tag gemessen → `Serie_Misst_Die_Aufgabe_Am_Tag` (:302), S12.
  - AK 5, Truhe beide Richtungen → `Truhe_Oeffnet_Nach_Den_Aufgaben` (:362, zwei Fälle). Keine eigene Mutante: die Sperre hat keinen eigenen Pfad, sie liest die Status aus `ComputeDayAsync` (S6/S9 treffen sie mit).
  - AK 6 → `Aufgabe_Einer_Wochenposition_Gilt_Am_Tag` (:379), S8, S9. AK 7 → `Zielpunkte_Nur_Fuer_Die_Aufgabe_Einmal_Je_Tag` (:398), S14.
  - AK 8 → `Alter_Malus_Bucht_Keinen_Scharfen_Tag` (:427), S4, S15; Momentaufnahme nach dem Zurückschalten → `Scharfe_Tage_Bleiben_Scharf_Nach_Dem_Zurueckschalten` (:258), S19.
  - AK 9 → `Kuer_Wird_Nie_Tagesaufgabe` (:448), S17. AK 10 → `Scharfer_Modus_Ist_Ueber_Die_Api_Nicht_Setzbar` (:475), S18.
  - AK 11 → `tsc -b` mit gestrichener `Enforced`-Zeile in `planWirkung`: 1 Fehler (erwartet ≥ 1), mit Zeile 0.
  - Ungedeckt, benannt: `OpenGoalProgressAsync` mit Aufgaben-Taktung ist nur bei Typen ohne Prüfmodus (Lesen, Aufsatz) sichtbar; kein Fall.
- **2026-11-27** — Mutantenprobe (`.claude/scripts/mutant-probe.py`, Debug, Filter `TagespflichtScharfTests`), 20 von 20 rot am benannten
  Test. Erster Lauf an `8c4395de + Arbeitsbaum` 21:44: S1 grün (der Schatten-Fall setzte kein `PlannerSince`), S4 kompilierte nicht (CS0162);
  nach Schärfung des Falls bzw. der Mutante 21:48 beide rot. S19/S20 (zu den Review-Funden) 22:20 rot. Grundlinie nach jedem Lauf grün.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | S1 | DutyCalendar.cs:23 (Kind-Modus) | red | red (2. Lauf) |
  | S2 | DutyCalendar.cs:27 (Plan-Modus) | red | red |
  | S3 | DutyCalendar.cs:65 (`Since`) | red | red |
  | S4 | DutyCalendar.cs:72 (`JudgedByTasks`) | red | red (2. Lauf) |
  | S5 | DutyCalendar.cs:81 (`DayOwesDuty`) | red | red |
  | S6–S15 | PositionProgressService.cs:121, 131, 132, 439, 452, 606, 610, 637, 727, 980 | red | red |
  | S16 | MetricsService.cs:103 | red | red |
  | S17 | DailyPlanner.cs:158 | red | red |
  | S18 | ChildrenController.cs:192 | red | red |
  | S19 | DutyCalendar.cs:27 (nur solange scharf) | red | red |
  | S20 | ChildrenDashboardService.cs:119 | red | red |

- **2026-11-27** — Verifikation: Suite Release 2715/2715 grün (vor den zwei Review-Fällen; danach Klasse 15/15 grün, seither nur ein
  Kommentar im Code). Vitest `Tagesplaene` 6/6, `tsc -b` grün. E2E (Ausweichports 5211/5184) `tagesplan-schatten`, `belohnung-ueber-faecher`,
  `belohnung-trotz-wochenziel`, `nacharbeit-bis-erledigt`, `nacharbeit`: 5 passed.
- **2026-11-27** — Review `pugling-reviewer`: kein 🔴. Behoben (zählen, Freigabe 11): Testlücke Zeitachse (AK 3), Testlücke Momentaufnahme;
  dazu der eigene Fund S1 (Schatten-Fall ohne `PlannerSince`) – Zähler 3 von 5. Nicht gebaut, weil erst mit B-737 erreichbar und eine
  Entscheidung: Umschalttag und angebrochene Woche → [B-1039](B-1039-umschalten-scharf-umschalttag-und-wochenziel.md) (Frage). 🟢 ohne
  Zählung: Kommentar zur Woche, die einen scharfen Tag berührt; `ChildrenDashboardService:119` bleibt bei `IsDutyDay` (gleiches Ergebnis,
  S20 hält es).
