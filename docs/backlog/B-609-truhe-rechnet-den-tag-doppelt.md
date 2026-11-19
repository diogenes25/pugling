---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/student]
aliases: [Truhen-Prüfung doppelt, Kind-Tag auf dem Schreibweg]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Backend-Review von B-606 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-609 · Bis die Truhe vergeben ist, rechnet jede Abgabe den Tag zweimal

Jede Abgabe (Runde beenden, Test abschließen) rechnet den Tagesstand des gespielten Plans zweimal voll durch, und die
Verlaufsschleifen laden die Positionen samt Fach-Kette jeden Tag neu, obwohl sie das Fach nie anzeigen. Das Ergebnis
ist richtig, nur nicht sparsam.

## User Story

Als Entwickler möchte ich, dass der Schreibweg nach einer Abgabe den Tag nur so oft rechnet, wie er gebraucht wird,
damit die Abgabe auch bei einem Plan mit vielen Positionen schnell antwortet, ohne dass sich eine Regel ändert.

## Ist-Stand am Code

Gelesen am 2026-11-19; **Zeilen gegenüber der Idee verschoben** (dort „~736" und „~450").

1. **Die Aufrufkette nach einer Abgabe.** `PositionPracticeController.cs:957-961` (Runde beenden) und
   `PositionTestsController.cs:466-471` (Test abschließen) rufen nacheinander
   `progress.EvaluateAndAwardAsync(plan, day)`, `gamification.EvaluateAndAwardAsync(childId, day)` und
   `dailyBox.EvaluateAndAwardAsync(childId, day)`. Beide Controller verwerfen den Rückgabewert der ersten.
2. **Erste Durchrechnung, bedingungslos:** `PositionProgressService.EvaluateAndAwardAsync`
   (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:540-588`) lädt die Positionen, bucht die
   Ziel-Belohnungen und endet mit `return await ComputeDayAsync(plan, day, ct)` (Zeile 587). Diesen Rückgabewert liest
   nur ein Test (`PositionGoalOverviewTests.cs:266-268`); der zweite Aufrufer in Tests
   (`RemediationPositionTests.cs:595`) verwirft ihn ebenfalls. Der Schreibweg bezahlt die volle Tagessicht also für
   nichts. Das ist **mehr**, als die Idee beschreibt (dort: nur die Wiederholung in der Truhe).
3. **Zweite Durchrechnung, bis die Truhe vergeben ist:** `DailyBoxService.EvaluateAndAwardAsync`
   (`DailyBoxService.cs:35-40`) prüft zuerst `AlreadyClaimedAsync` (Zeile 37) und kehrt dann zurück – danach entfällt
   sie. Davor ruft sie `ComputeChildDayAsync` (`PositionProgressService.cs:407-431`), das je aktivem Plan wieder
   `ComputeDayAsync` rechnet (Zeile 416).
4. **Was `ComputeDayAsync` je Position kostet** (Zeilen 305-360): `play.ItemsOfAsync` (326), bei Leitner
   `play.DueItemIndicesAsync` (328), `IsGoalMetAsync` und, wenn offen, `OpenGoalProgressAsync` (329-330), dazu eine
   Abfrage der eingeführten Items je Plan (312-316). Die Truhe braucht davon nur `GoalMet` und `Cadence` je Position
   (`RewardObligations`, Zeilen 369-374, `RewardsUnlocked` Zeile 428).
5. **Das Fach wird überall mitgeladen:** `LoadPositionsAsync` (Zeilen 284-289) lädt
   `Exercise → SeriesUnit → Series → Subject`. Gelesen wird das Fach nur in `SubjectNameOf`
   (Zeilen 391-392), das nur `ComputeDayAsync` aufruft (Zeile 344). Die Aufrufer ohne Anzeige:
   `ProgressAsync` (Zeilen 869-879, nutzt von `ComputeDayAsync` nur `DutyDone`, `GoalsTotal`, `GoalsMet`,
   `PointsAwarded`), die Zeitachse `ChildrenDashboardService.cs:77-83` (nur Zähler) und
   `EvaluateAndAwardAsync` selbst (Zeile 542, liest nur Übung und Titel für den Ledger-Text, Zeile 568, nie Reihe oder Fach).
6. **Die Schleifen laden je Tag neu:** `ComputeDayAsync` ruft `LoadPositionsAsync` bei **jedem** Aufruf (Zeile 307);
   `ProgressAsync` ruft es je Tag der Plandauer, die Zeitachse je Kind, Tag und Plan. Der Kommentar an der Funktion
   (Zeilen 282-283) benennt das („runs once per day of a history"), löst es aber nicht. Die Streak-Schleifen
   sind anders gebaut und laden einmal je Plan (Wörterbuch in `ChildStreakAsync`, Zeilen 487-496; `CompleteChildDaysAsync`
   Zeile 521).
7. **Messung:** keine gefunden (Suche nach `Stopwatch`/`BenchmarkDotNet` im Backend ohne einschlägigen Treffer; ein
   Messwert zur Abgabe-Latenz bei 4-15 Positionen steht nirgends). Die Idee vermutet „vermutlich nicht spürbar".

## Die echte Lücke

- **Echt, klein:** Die erste Durchrechnung auf dem Schreibweg (Beleg 2) hat keinen Leser. Ein Rückgabewert, den nur ein
  Test liest, treibt die volle Tagessicht bei jeder Abgabe. Das ist die billigste Einsparung und die einzige, die auch
  **nach** der Truhe weiterläuft.
- **Echt, mittel:** Die Verlaufsschleifen laden Positionen und Fach je Tag neu (Beleg 5-6) – ein Vielfaches der nötigen
  Abfragen, an der Planübersicht des Vaters (`…/overview/progress`) und der Zeitachse.
- **Fraglich:** Die zweite Durchrechnung (Truhe) entfällt, sobald die Truhe vergeben ist; bis dahin ist sie einmal je
  Tag und Kind „zu viel". Eine eigene schlanke Truhen-Prüfung (Vorschlag der Idee) wäre eine zweite Wahrheit für
  `RewardsUnlocked` und widerspricht B-611/B-612 („alle Zahlen kommen aus derselben Liste").

## Offene Punkte

1. **Erst messen?** *Empfehlung: ja, kurz.* Vor jedem Umbau die Abfragen einer Abgabe zählen (EF-Befehlszähler in
   einem Test, Vorbild `QueryPlanSmokeTests`) für einen Plan mit 5 und mit 15 Positionen. Zeigt sich kein Unterschied
   von Belang, wird die Story als Verlustposten geschlossen und die Messung ins Protokoll geschrieben.
2. **Rückgabewert von `EvaluateAndAwardAsync`:** *Empfehlung:* auf `Task` ohne `DayOverview` umstellen und die zwei
   Test-Aufrufe auf `ComputeDayAsync` verlegen. Kein Vertrag, kein HTTP-Verhalten betroffen.
3. **Fach nur in der Tagessicht laden?** *Empfehlung:* ja – `LoadPositionsAsync` bekommt einen Schalter oder die Tagessicht
   lädt Reihe und Fach selbst; die Verlaufsschleifen laden die Positionen **einmal** je Aufruf und reichen sie an die
   Tagesrechnung weiter. Verworfen: die Einsparung per Cache über Requests (Gefahr stale Daten nach einer Vater-Änderung).
4. **Truhe:** *Empfehlung:* nicht anfassen (siehe Lücke 3). Wer es dennoch will, rechnet `ComputeChildDayAsync` mit
   den bereits geladenen Positionen des Plans – nie eine zweite Regel für `RewardsUnlocked`.
5. **Berührt das Regeln oder Münzen?** Nein: die Umbauten ändern keine Ausgabe (Aufräumen). Das muss ein Test belegen
   (AK 1).

## Akzeptanzkriterien (Entwurf)

Je Kriterium: die Änderung, die es rot macht.

1. **Verhalten unverändert:** alle bestehenden Tests zu Tageskiste, Ziel-Belohnung, Serie, Planübersicht und Zeitachse
   bleiben unverändert grün (`DailyBoxTests`, `PositionGoalOverviewTests`, `ChildrenOverviewTests`,
   `KindTagessichtTests`). *Rot, wenn der Umbau die Reihenfolge Ziel-Belohnung → Truhe verschiebt oder `RewardsUnlocked`
   anders rechnet.*
2. **Eine Tagessicht je Abgabe nach vergebener Truhe:** nach dem Spielen, wenn die Truhe des Tages schon vergeben ist,
   löst eine Abgabe keinen `ItemsOfAsync`/`DueItemIndicesAsync`-Aufruf je Position mehr aus (Zählung an der
   Befehlsschnittstelle oder an einem Testdoppel). *Rot heute: die Zählung ist größer als null; rot, wenn der Umbau die
   Schluss-Rechnung in `EvaluateAndAwardAsync` stehen lässt.*
3. **Verlauf lädt Positionen einmal:** `ProgressAsync` über 30 Tage löst genau **eine** Positions-Abfrage aus (nicht
   dreißig). *Rot heute (eine je Tag).*
4. **Tagessicht zeigt weiter das Fach:** `…/overview` und die Kindsicht liefern `SubjectName` je Position unverändert
   (`KindTagessichtTests`). *Rot, wenn die Fach-Kette beim Umbau aus der Tagessicht verschwindet.*
5. **Keine zweite Wahrheit:** `RewardsUnlocked` wird weiter nur in `ComputeChildDayAsync` berechnet (ein Treffer im
   Quelltext). *Rot, wenn eine eigene Truhen-Prüfung dieselbe Regel nachbaut.*

## Verlauf

- **2026-11-17** — angelegt aus dem Backend-Review von B-606 (🟢, ohne Zählung).
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Aufrufkette neu belegt (Zeilen gegenüber der Idee verschoben);
  größerer Posten gefunden – die erste Durchrechnung in `EvaluateAndAwardAsync` (Zeile 587) hat keinen Leser auf dem
  Schreibweg; die schlanke Truhen-Prüfung der Idee würde eine zweite Wahrheit schaffen; Messung als erster Schritt
  empfohlen.
