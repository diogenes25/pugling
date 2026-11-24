---
tags: [typ/story, status/abgenommen, bereich/punkte, bereich/auswertung, rolle/supervisor]
aliases: [Punkte heute zeigt nur den Zielbonus, Münzen heute]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: ja
quelle: docs/pm-sitzung-2026-11-24.md#runde-140--vorstellung--feedback
grund: ""
ersetzt_durch: []
entgangen_bei: [B-662, B-770]
nachgeschaut: 2026-11-24
---

# B-954 · „Punkte heute“ auf der Vater-Startseite zeigt nur den Tagesziel-Bonus

Gesehen in Runde 140 an der Kopie der echten DB (2026-11-24, 17:45): Die Vater-Startseite zeigt bei Friedrich „Geübt heute
223 Antworten · Tagesziel 2 von 10 · **Punkte heute 60**“. Sein Konto hat an dem Tag aber 457 Münzen verdient (397 aus
Antworten, 60 aus zwei Tageszielen) und 140 Gems. Die 60 sind allein die zwei Tagesziel-Boni. Ein Kind, das zweihundert
Karten spielt und kein Ziel schließt, steht mit „Punkte heute 0“ da.

## User Story

Als Vater möchte ich auf der Startseite und in der Zeitleiste sehen, wie viele Münzen ein Kind an einem Tag verdient hat,
und zwar dieselbe Zahl, die das Kind in seiner App liest. Daran entscheide ich, ob die Arbeit des Tages eine Einlösung trägt.

## Ist-Stand am Code

- `ChildrenDashboardService.BuildAsync` (`backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:31`) füllt
  `ChildDay.PointsToday` mit `day.Plans.Sum(p => p.Overview.PointsAwarded)`.
- `PointsAwarded` ist `PositionProgressService.PointsAwardedAsync` (`:359`): die Summe der `PositionGoalReward.Points` eines
  Plans an einem Buchungstag, also nur die Zielboni. Die Münzen je Antwort (`PointKind.Base`) stehen nur im Ledger.
- Dieselbe Zahl geht als `ChildPeriod.Points` in die Zeitleiste (`:104`, `ChildrenTimeline.tsx:37` „…, N Punkte“) und als
  `pointsAwarded` in den Tagesverlauf eines Plans (`VaterPlanDetail.tsx:105`, Spalte „Punkte“).
- Die Sohn-App hat schon eine Definition von „verdient“: `WalletService.EarnedCoinKinds` (`WalletService.cs:47`, B-253) –
  positive Münz-Buchungen ohne Geschenk (`Manual`), Shop-Erstattung und Malus-Korrektur; Gems bleiben draußen.
- B-662 (Entscheidung 3) und B-770 haben „Punkte heute“ als vorhandene Zahl übernommen, ohne zu prüfen, was sie zählt.

## Die echte Lücke

Die Beschriftung verspricht die Tagesleistung in Punkten, die Zahl ist der Zielbonus. Vater und Sohn lesen über denselben
Tag zwei verschiedene Zahlen, und die des Vaters ist die kleinere.

## Entscheidungen

Im Dialog mit dem Nutzer am 2026-11-24 (Runde 140).

1. **„Punkte heute“ wird „Münzen heute“ und zeigt die an dem Tag verdienten Münzen nach der Definition der Sohn-App**
   (`EarnedCoinKinds`, nur positive Buchungen). Malus, Käufe, Geschenke und Gems zählen nicht hinein. *Kosten:* Die Zahl
   zeigt den Stock nicht; ein nachgebuchter Malus steht nur im Konto. Abgelehnt: Münzen und Gems (eine Zahl mehr), Netto
   (an Tagen mit nachgebuchtem Malus kleiner als die Leistung), nur umbenennen (der Vater sähe die Tagesleistung weiter nicht).
2. **Der Tag ist der Lerntag** (deutsche Zeit, B-730), an dem die Buchung entstand, wie bei „diese Woche“ in der Sohn-App.
3. **Die Zeitleiste bekommt dieselbe Zahl** (PM, gleiche Senke und gleicher Satz „N Punkte“): je Periode die Summe der
   verdienten Münzen ihrer Tage, unabhängig davon, ob ein Plan seither abgeschaltet wurde. *Kosten:* Vertragsbruch an zwei
   Feldern (`pointsToday` → `coinsToday`, `points` → `coins`); in der Testphase erlaubt.
4. **Der Tagesverlauf eines Plans behält seine Zahl und heißt „Zielbonus“** (PM). Das Ledger kennt keinen Plan; eine
   Münzzahl je Plan gibt es nicht. *Kosten:* Dort steht weiter nur der Bonus, jetzt ehrlich beschriftet.
5. **Die Bedingungen, die an der alten Zahl hingen, bleiben an ihr:** „inaktiver Plan zählt nur an einem Tag, an dem er
   benutzt wurde“ (`:107`) liest weiter den Zielbonus des Plans. **`Practiced` liest die Münzen nicht** (Review): eine
   morgens geöffnete Truhe (`DailyBoxCoins`) oder ein beim Login abgerechnetes Objective bucht verdiente Münzen ohne
   Lernen; „geübt“ heißt eine Antwort oder ein erfülltes Ziel. *Kosten:* keine, die alte Zahl war praktisch deckungsgleich
   mit „Ziel erfüllt“.
6. **Der Tag ist der Buchungstag, auch bei einem Nachtrag** (Folge aus 2): Trägt der Vater einen Test für gestern nach, steht
   der Zielbonus am Tag des Nachtrags; die Zeitleiste zeigt für gestern „1 von 1 Zielen, 0 Münzen“. *Kosten:* an
   nachgetragenen Tagen passen Ziel und Münzen nicht zusammen. Eine Rückdatierung der Ledger-Zeile wäre eine andere Story.

## Akzeptanzkriterien

1. Die Tagesübersicht (`supervisor/children/daily-overview`) liefert je Kind `coinsToday`: die Summe der positiven
   Buchungen der verdienten Münz-Arten an diesem Lerntag. Münzen aus Antworten ohne erfülltes Ziel zählen mit.
2. Gems, ein Malus, ein Kauf und ein Geschenk des Vaters ändern die Zahl nicht.
3. Buchungen des Vortags und des Folgetags zählen nicht; die Grenze ist Mitternacht deutscher Zeit, nicht UTC.
4. Die Zeitleiste (`supervisor/children/overview`) liefert je Periode `coins` als Summe derselben Tageszahlen, auch für
   einen seither abgeschalteten Plan und für Tage ganz ohne Plan-Ziel.
5. Die Startseite beschriftet die Zahl mit „Münzen heute“, die Zeitleiste sagt „N Münzen“ (auch an einer Periode ohne
   Ziel, wenn es Münzen gab), der Tagesverlauf eines Plans sagt „Zielbonus“ und seine Summe „Zielbonus gesamt“.
6. Die Sohn-App-Zahl „diese Woche“ bleibt unverändert (dieselbe Abfrage, ein Ort).
7. Münzen allein machen ein Kind nicht „dran“: verdiente Münzen ohne Antwort und ohne erfülltes Ziel lassen `practiced` falsch.

## Schätzung

**Größe: S** – eine Abfrage im `WalletService`, zwei Aufrufer, zwei Vertragsfelder, drei Beschriftungen.
**Risiken:** Tagesgrenze (UTC gegen deutsche Zeit); die Zeitleiste über 92 Tage lädt die Buchungen des Bereichs einmal je Kind.
**Angriffsplan:** `WalletService.EarnedCoinsByDayAsync(childId, from, to)` über dieselbe Basisabfrage wie `WeekCoinsAsync`;
`ChildrenDashboardService` liest sie in `BuildAsync` und `TimelineAsync`; Vertrag umbenennen; Frontend über `contract.ts`.
**Testweg:** Integration in `ChildrenDashboardTests`/neue Fälle mit direkt gebuchten Ledger-Zeilen (Art, Vorzeichen, Tag),
Vitest für die Beschriftungen, Rollengang an einer frischen Kopie der echten DB: Friedrichs Zahl gegen sein Ledger.

## Verlauf

- **2026-11-24** — angelegt, ausformuliert, gegrillt (Entscheidungen 1–2 Nutzer, 3–5 PM) und geschätzt (S, beides);
  Quelle: Rollengang Runde 140 an der Kopie der echten DB.
- **2026-11-24** — in Arbeit.
- AK 1, Münzen aus Antworten ohne Plan und Ziel, Zielbonus zählt mit → `MuenzenHeuteTests.Muenzen_Heute_Sind_Die_Verdienten_Muenzen_Des_Tages`
  (Kind ohne Plan, Summe 3 aus 1 + 2), Mutante B954-8 → rot.
- AK 2, je Art: negative Buchung einer verdienten Art → B954-1; Geschenk und Shop-Erstattung → B954-2; Gems → B954-3; Malus
  und Kauf (negativ, ausgeschlossene Art) stehen im selben Fall, kein eigener Mutant (zwei Bedingungen decken sie doppelt).
- AK 3, Vortag und Folgetag → derselbe Fall (128 am 12., 256 am 14.), obere Grenze B954-6, Mitternacht B954-7; deutsche Zeit →
  `MuenzenHeuteBerlinTests.Der_Tag_Einer_Buchung_Ist_Der_Lerntag_In_Deutscher_Zeit`, Gruppierung B954-4, untere Grenze B954-5.
  Nur Winterzeit gemessen; ein Sommerzeit-Tag ist nicht gemessen.
- AK 4, Zeitleiste je Tag und je Woche, Kind ohne Plan → `Zeitleiste_Summiert_Die_Verdienten_Muenzen_Je_Periode`, B954-9 und
  B954-10; abgeschalteter Plan → `ChildrenOverviewTests.Abgeschalteter_Plan_Bleibt_In_Der_Zeitachse` (20 aus dem Ledger).
- AK 5, „Münzen heute“ → `VaterDashboard.test.tsx` (B954-11 Beschriftung, B954-12 Feldquelle); „N Münzen“/„1 Münze“ und
  „kein Ziel, N Münzen“ → `ChildrenTimeline.test.tsx` (B954-13, B954-15); „Zielbonus“/„Zielbonus gesamt“ →
  `e2e/full-flow.spec.ts` (Spaltenkopf und Kachel), **ohne Mutant**: belegt nur durch den grünen Lauf nach der Umbenennung.
- AK 6, „diese Woche“ unverändert → `WochenvergleichTests` grün in der Suite; die Mutanten B954-1 bis -3 sitzen in der
  geteilten Basisabfrage. Rollengang: Vater-Woche 544 = Sohn-App „diese Woche“ 544.
- AK 7, Münzen allein sind kein Üben → `Muenzen_Aus_Der_Truhe_Sind_Kein_Ueben`, B954-14 → rot.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B954-1 negative Buchung zaehlt | WalletService.cs:99 | red | red |  |
  | B954-2 Geschenk und Erstattung zaehlen | WalletService.cs:99 | red | red |  |
  | B954-3 Gems zaehlen | WalletService.cs:99 | red | red |  |
  | B954-4 Tag nach UTC statt Lerntag | WalletService.cs:93 | red | red |  |
  | B954-5 untere Grenze UTC-Mitternacht | WalletService.cs:86 | red | red |  |
  | B954-6 obere Grenze einen Tag zu frueh | WalletService.cs:87 | red | red |  |
  | B954-7 Mitternacht gehoert nicht zum Tag | WalletService.cs:90 | red | red |  |
  | B954-8 Tagesuebersicht wieder Zielbonus | ChildrenDashboardService.cs:32 | red | red |  |
  | B954-9 Zeitleiste ohne Muenzen | ChildrenDashboardService.cs:115 | red | red |  |
  | B954-10 Periode nimmt nur den ersten Tag | ChildrenDashboardService.cs:121 | red | red |  |
  | B954-14 Muenzen allein gelten als geuebt | ChildrenDashboardService.cs:37 | red | red |  |
  | B954-11 Beschriftung wieder Punkte heute | VaterDashboard.tsx:142 | red | red |  |
  | B954-12 Karte zeigt die Antwortzahl als Muenzen | VaterDashboard.tsx:142 | red | red |  |
  | B954-13 Einzahl fehlt | ChildrenTimeline.tsx:36 | red | red |  |
  | B954-15 Tag ohne Ziel verschweigt Muenzen | ChildrenTimeline.tsx:38 | red | red |  |

  Gemessen mit `mutant-probe.py` an `c7842636 + Arbeitsbaum`, 2026-11-24 20:17, nach dem letzten Edit. Grundlinie danach grün.
- **2026-11-24** — `pugling-reviewer` und `frontend-reviewer`: zwei Korrektheitsfunde im Increment, beide behoben und
  gemessen. (1) `Practiced` las die Münzen und wäre durch eine geöffnete Truhe wahr geworden (AK 7). (2) Die Zeitleiste
  verschwieg Münzen an einer Periode ohne Ziel (AK 5). Dazu übernommen: „Zielbonus gesamt“ über der Zielbonus-Spalte,
  Token ohne Vorgabewert am neuen Helfer, „all in UTC“ im `ChildPeriod`-Summary. Nicht übernommen: ein Sommerzeit-Fall
  (Code laut Review richtig), die Namensnähe „Münzen heute“ im Kind-Hub und die 🏆-Zahl der Sohn-App → B-955.
- **2026-11-24** — Verifikation am endgültigen Stand: .NET 2343/2343 (Release), Vitest 1440/1440, `tsc --noEmit` sauber,
  E2E 5/5 (`full-flow`, `konto-waehrung-und-datum`, `heute-dran-nach-antworten`; Ausweichports 5290/5193, Release).
- **2026-11-24** — Rollengang an einer frischen Kopie der echten DB, Server nach dem letzten Edit gestartet (`:5201`):
  Friedrich `coinsToday` 457 = Ledger Base 397 + Goal 60 (Combo 140 sind Gems), Montag 87 = 57 + 30; Theodor 0 und
  `practiced` falsch; Zeitleiste Friedrich 16.–24.11. je Tag aus dem Ledger; Wochen 2838 und 544 = `lastWeekTotal` und
  `thisWeek` der Sohn-App. Die Oberfläche selbst ist an der Kopie nicht geklickt; sie ist durch Vitest und E2E belegt.
- **2026-11-24** — abgenommen, Commit `ddf6d1d6`. Vater an der Kopie der echten DB (Zahlen gegen das Ledger), Sohn und
  Creator als Regressionszeugen (Protokoll Runde 140).
- **2026-11-24** — Nachschau (Nachtlauf L, [Protokoll](../pm-sitzung-2026-11-24-l.md)): Behauptung „dieselbe Definition wie
  ‚diese Woche‘“ nachgerechnet – `WalletService.EarnedCoinsByDayAsync` (`WalletService.cs:83-94`) und `WeekCoinsAsync`
  (`:58-75`) filtern beide über das eine `EarnedCoins` (`:97-99`, positive Einträge der verdienten Arten) und schneiden am
  Lerntag (`learningDay.StartUtc`). Einziger Unterschied: die Woche begrenzt oben mit `<= now`, der Tag mit dem Ende des
  Lerntags – trifft nur Einträge mit Zeitstempel nach der Serveruhr, die es nicht gibt. Beide Aufrufer
  (`ChildrenDashboardService.cs:32`, `:98`) gelesen: Tag und Zeitleiste nehmen dieselbe Methode. Kein Befund.
- **2026-11-24** — nachgeschaut (Nachtlauf K, Sprint 151): `WalletService.cs:99` filtert `Amount > 0 && EarnedCoinKinds` –
  ein positiver `GoalPenalty` (die Malus-Rücknahme aus B-768) zählt darum nicht als „Münzen heute“, gemessen in
  `MeldungBuchtNachTests.Malus_wird_durch_eine_positive_Gegenbuchung_zurueckgenommen` (Woche wächst nur um die Belohnung). Kein Befund.
