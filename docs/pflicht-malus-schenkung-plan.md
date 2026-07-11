# Umbauplan: Pflicht-Malus + Vater-Schenkung

Status: **umgesetzt**. Stand 2026-07-11. Backend + Frontend fertig; Build grün, 290 Tests grün
(inkl. `PflichtMalusTests`: Malus+Schuld+Idempotenz, Fairness bei inaktivem Plan, Gem-/Münz-Schenken),
Migration `PositionPenaltyAndPenaltyLedger`.

## Motivation / Zielbild

Das Kern-Konzept der App ist **Zwang zum Lernen**: Der Sohn *muss* wiederkehrende
Pflichtaufgaben (täglich/wöchentlich) erledigen bzw. Klausuren bestehen. Das heutige System
ist jedoch **rein positive Verstärkung** (Carrots): Lernen erzeugt Punkte → Münzen/Gems → Shop.
Es gibt keinen einzigen Mechanismus, der als Folge von *Nicht*-Lernen etwas entzieht (kein „Stick").

Dieser Plan ergänzt den fehlenden Stick – **und** ein Druckventil dagegen:

1. **Pflicht-Malus (der Stick):** Ein Pflichtziel einer Lehrplan-Position zahlt bei Erfüllung
   schon heute Münzen (`PointKind.Goal`) und **zieht bei „gerissen" Münzen ab** (neu).
   **Schulden sind erlaubt** – der Münz-Saldo darf negativ werden (Münzen = reale Privilegien,
   also der härteste Hebel). Der Malus wohnt bewusst am **Pflichtziel** (nicht am Missions-System,
   das Gems zahlt und der kosmetische Carrot bleibt).
2. **Vater-Schenkung (das Druckventil):** Zu hohe Schulden töten die Motivation. Der Vater kann
   jederzeit **Münzen und Gems verschenken** – als Schulden-Erlass *und* als Belohnung für
   Leistung außerhalb der App, ohne Bezug zu einem Shop-Artikel.

## Getroffene Entscheidungen

1. **Malus-Heimat = Positions-Pflichtziel, Währung = Münzen.** Es ist schon heute die
   tägliche/wöchentliche Pflicht mit Rhythmus + Schwelle + idempotenter Münz-Belohnung
   (`PositionGoalReward`). Münz-Malus trifft reale Privilegien = echter Stick.
2. **Schuld erlaubt (kein Clamp bei 0).** Negativer Münz-Saldo blockt den Shop-Kauf natürlich
   (`InsufficientCoins`), bis der Sohn wieder verdient *oder* der Vater schenkt.
3. **Kein Background-Job.** Die App hat keinen Scheduler. Der Malus fürs *Nicht*-Handeln wird
   per **Lazy Settlement** an POST-Nahtstellen abgerechnet (Login + Shop-Kauf), idempotent
   über einen Unique-Key gesichert – mehrfaches Auslösen ist gefahrlos.
4. **Fairness:** Kein Malus für Perioden, in denen der Plan nicht spielbar war
   (`PlanPlayableForChild`: inaktiv / außerhalb Datumsfenster).
5. **Schenken reuse statt neu:** `POST children/{id}/points` bucht schon `PointKind.Manual`
   (Münzen, positiv/negativ). Es fehlt nur der **Gem-Zwilling** `PointKind.ManualGems`.
   Der Endpunkt bekommt einen `Currency`-Parameter (Default Coins → abwärtskompatibel).
   Kein redundanter `GrantCoins`/`GrantGems`, kein neuer Endpunkt.

## Vorab-Defaults (kein Rückfragebedarf)

- Wochen-Periode wird erst **nach Sonntag** (voll abgeschlossen) abgerechnet.
- Rückblick-Deckel: max. **14 Tage** bzw. seit Positions-Anlage (was später ist).
- `PenaltyCoins` default **0** → Malus ist opt-in pro Position (reiner Carrot bleibt möglich).

## Etappen (API-First, jede lauffähig + testbar)

1. **Schenken:** `PointKind.ManualGems` (→ Gems) + Währungs-Mapping + Tests; `AddPoints`
   um `Currency`-Parameter erweitern (Coins→Manual, Gems→ManualGems). Keine Migration.
2. **PenaltyCoins:** Feld an `PlanPosition` + Migration + Vater-CRUD/DTO.
3. **Malus-Schema:** `PointKind.GoalPenalty` (→ Coins) + Tabelle `PositionGoalPenalty`
   (Unique `(PlanPositionId, PeriodKey)`) + Migration + Währungs-Test.
4. **Settlement:** `PositionProgressService.SettleClosedPeriodsAsync(childId, today)` –
   Fairness-Klausel, Rückblick-Deckel, `child.ConcurrencyStamp`-Bump, ohne Clamp.
5. **Auslöser:** Settlement an Kind-Login + Shop-Kauf (vor Saldo-Prüfung) einklinken +
   Integrationstest (gerissener Tag → −Malus genau einmal, negativer Saldo, Kauf blockiert).
6. **Frontend:** Malus-Feld im Positions-Editor, „verschenken"-Dialog (Münzen/Gems),
   negativer Saldo + Malus-/Geschenk-Buchungen im Ledger sichtbar.

## Wallet-Invariante (Pflicht)

Jeder **abbuchende** Pfad (Malus) muss `child.ConcurrencyStamp` bumpen – der geteilte
Saldo-Serialisierungspunkt gegen Doppelspend bei parallelen Käufen. **Gutschriften**
(Schenken) brauchen den Bump nicht (verletzen die Überzieh-Invariante nicht).
