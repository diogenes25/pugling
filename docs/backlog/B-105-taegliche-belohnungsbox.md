---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [Loot-Box, Daily Box, Tagesbox]
status: abgenommen
prio: P4
art: Wunsch
groesse: S
wo: backend
migration: ja
vertragsbruch: nein
quelle: Gespräch mit dem Nutzer, 2026-08-04 — süchtig machende Mobile-Game-Mechaniken (Brawl Stars: Boxen, tägliches Anmelden) als bewusstes Vorbild
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-105 · Tägliche Belohnungsbox: Loot-Box + Streak als positives Gegenstück zum Stick

Pugling erzwingt Lernerfolg heute nur über den „Stick" (`PenaltyCoins` — verpasste Pflicht kostet
Münzen). Eine positive, zufallsbasierte Belohnung nach erfüllter Pflicht — wie sie Mobile-Games über
Loot-Boxen und Login-Streaks einsetzen — gibt es noch nicht. Die Idee: eine tägliche Box mit zufälligem
Inhalt (Münzen/Gems-Spanne, ggf. kleine Skin-Drop-Chance), ausgelöst durch das erreichte Tagesziel, plus
ein Streak-Zähler.

## User Story

Als Student möchte ich nach Erreichen meines Tagesziels eine zufällige Belohnungsbox (Münzen/Gems, ggf.
eine kleine Chance auf ein seltenes Skin) samt sichtbarem Streak bekommen, damit das Erfüllen der Pflicht
Vorfreude statt reiner Pflichtabarbeitung erzeugt.

## Ist-Stand am Code

- **Punkte/Währung**: `PointKind`-Enum (`backend/Pugling.Contracts/Common/AdminBaseTypes.cs:38-60`,
  u. a. `Base, Manual, Combo, Speed, Mission, Achievement, SkinPurchase, Goal, ShopCoins, ShopGems,
  ManualGems, GoalPenalty, ObjectiveCoins, ObjectiveGems`). Zuordnung zur Währung (Coins/Gems) ist eine
  **exhaustive** reine Funktion: `PointKindCurrency`
  (`backend/Pugling.Api/Services/Shared/PointKindCurrency.cs:19-27`) — wirft bei unbekanntem `PointKind`,
  ein neuer Wert muss also zwingend dort eingetragen werden. Buchungen laufen als flaches Journal
  `ChildPointsEntry` (`backend/Pugling.Api/Models/AdminEntities.cs:209-219`); der Saldo ist kein eigenes
  Feld, sondern eine Summe über `WalletService` (`backend/Pugling.Api/Services/Shared/WalletService.cs`,
  `BalancesAsync` Z.22-33). `ScoringService`
  (`backend/Pugling.Api/Services/Shared/ScoringService.cs`) ist das Vorbild für zustandslose, gut
  testbare Punktelogik.
- **Pflicht-Erreichung**: `PositionProgressService.IsGoalMetAsync`
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:91-120`) prüft je `PlanPosition`, ob
  das Ziel in der Periode erreicht ist; `ComputeDayAsync` (Z.144-174) rollt das über alle Positionen eines
  Plans zu `DayOverview.DutyDone` auf — das wäre der natürliche Trigger-Punkt für die Box.
  `PositionProgressService.Streak` (Z.359-367) zählt **bereits aufeinanderfolgende Tage mit erfüllter
  Pflicht** und fließt in `ProgressViewAsync` (Z.376-397) als `CurrentStreak` ein — ein Lern-Streak
  existiert also schon, nur ohne Belohnungs-Anbindung. Ausgewertet wird nach jedem Review/Testabschluss in
  `PositionTestsController.cs:420-422` und `PositionPracticeController.cs:425,461-462`.
- **PenaltyCoins/„Stick"**: `PlanPosition.PenaltyCoins`
  (`backend/Pugling.Api/Models/PlanPositionEntities.cs:85`, Doku Z.79-84). Verrechnung läuft **lazy**
  (kein Scheduler) bei Login (`AuthController.cs:78`) und Shop-Kauf (`MeController.cs:275`) über
  `PositionProgressService.SettleClosedPeriodsAsync` (Z.273-343); Idempotenz über die Entity
  `PositionGoalPenalty` mit Unique Index auf `(PlanPositionId, Cadence, PeriodStart)`
  (`PlanPositionEntities.cs:171-185`). Genau dieses Schablonen-Muster (lazy Settlement an denselben
  POST-Seams, eigene Idempotenz-Entity mit Unique Index, `PointKind`-getriebene Buchung) sollte die neue
  Box als positives Gegenstück übernehmen.
- **Missionen/Achievements** (Vorlage „Ereignis prüfen → idempotent belohnen"):
  `GamificationService.EvaluateAndAwardAsync`
  (`backend/Pugling.Api/Services/Shared/GamificationService.cs:17-69`) — Metrik holen, gegen
  Target/Threshold prüfen, idempotent über `MissionAward`/`AchievementAward` gegenprüfen,
  `ChildPointsEntry` buchen, `SaveIgnoringDuplicateAsync` (Z.184-202) fängt die Unique-Index-Race ab statt
  500 zu werfen. Dasselbe Muster steckt auch in `PositionProgressService` und `ObjectiveRewardService`.
  `ProgressMetric.StreakDays` (`backend/Pugling.Contracts/Common/GamificationBaseTypes.cs:20`) nutzt den
  Lern-Streak bereits für Auszeichnungen.
- **Shop/Skins**: `ShopService` (Concurrency-Bumps beim Kauf, u. a. Z.175,214,221,240,275,350,387).
  `SkinCatalog` (`backend/Pugling.Api/Models/AdminEntities.cs:189-206`) ist eine **feste**
  `Dictionary<string,int>` ID→Gem-Preis-Tabelle — **kein Rarity-/Gewichtungs-Konzept** vorhanden.
  Kauf/Equip-Referenzimplementierung: `MeController.cs:165-227` (`PurchaseSkin`, `EquipSkin`).
- **Wallet-Concurrency**: durchgängiges Pattern — betroffene Liste **reassignen** (nie in-place
  mutieren), danach `child.ConcurrencyStamp = Guid.NewGuid()`
  (`backend/Pugling.Api/Models/AdminEntities.cs:129`, EF-Konfiguration
  `Data/PuglingDbContext.cs:185`), Save in try/catch gegen `DbUpdateConcurrencyException` — u. a.
  `MeController.cs:197/222`, `PositionProgressService.cs:330`, `ShopService.cs:221`. Der Stamp ist
  bewusst kein ETag/keine Client-Ressourcenversion (siehe B-103).
- **Login-Streak**: existiert **nicht** — Grep nach `Streak|LastLogin|ConsecutiveDays` findet nur den
  oben genannten Lern-Streak. Kein `Child.LastLoginAt`, kein separates Login-Streak-Feld im Schema.
- **JSON-Spalten-Konvention**: `Data/JsonValueComparer.cs:13-22` (generischer
  `ValueComparer<T>.For<T>()`), Registrierung zentral in `PuglingDbContext.cs` an ~15 Stellen — Pflicht
  für ein neues JSON-Feld (z. B. eine `List<BoxRewardLine>` für den gezogenen Box-Inhalt).

**Einschätzung**: ca. 60–70 % der Mechanik (Trigger, Idempotenz, Ledger, Wallet-Concurrency) ist
Schema-Kopie aus dreifach vorhandenem Code (Missionen, Achievements, Goal-Reward/-Penalty). Echter Neubau
ist die RNG-Ziehung selbst (kein Vorbild im Code) und — falls Skin-Drops gewünscht — ein
Rarity-/Gewichtungs-Konzept für `SkinCatalog`, das es noch nicht gibt.

## Die echte Lücke

Es gibt aktuell keine positive Zufallsbelohnung und keinen Login-/Tages-Streak-Reward — nur die negative
Verstärkung über `PenaltyCoins`. Die Infrastruktur für „Ereignis prüfen → idempotent belohnen" ist aber
bereits dreifach vorexerziert, sodass die neue Mechanik strukturell kein Neuland ist, sondern eine vierte
Instanz desselben Musters plus einer neuen RNG-Komponente und (optional) eines neuen Rarity-Konzepts.

## Offene Punkte

Alle sieben Punkte sind unten als Entscheidungen gefallen — der Nutzer hat ausdrücklich angewiesen,
die Empfehlungen zu übernehmen und bis zur Umsetzung durchzuarbeiten, statt eine separate Grill-Sitzung
abzuwarten.

~~1. Trigger pro `PlanPosition` oder plan-weit?~~ → Entscheidung 1.
~~2. Streak-Basis neu oder wiederverwenden?~~ → Entscheidung 2.
~~3. Box-Ökonomie fest oder konfigurierbar?~~ → Entscheidung 3.
~~4. Skin-Drops im ersten Schnitt?~~ → Entscheidung 4.
~~5. Streak-Bruch: harter Reset oder Freeze?~~ → Entscheidung 5.
~~6. Eskalationsstufen mit konkreten Zahlen?~~ → Entscheidung 6.
~~7. Ethische Leitplanke gegen Dark Patterns?~~ → Entscheidung 7.

## Entscheidungen

1. **Trigger ist plan-weit** (`DayOverview.DutyDone` des Kindes einzigem aktiven Plan), nicht pro
   Position. Begründung: die Anti-Cheat-Invariante erzwingt ohnehin genau einen aktiven+laufenden Plan
   je Kind (siehe Memory „Ein aktiver Plan"), plan-weit und „das eine aktive Plan" fallen also zusammen.
   Kosten: ein Plan mit mehreren Pflicht-Positionen erzeugt trotzdem nur eine Box pro Tag statt einer je
   Position — das ist beabsichtigt, keine Einschränkung.
2. **Streak wird über `PositionProgressService.Streak` wiederverwendet**, kein eigener Login-Zähler.
   Begründung: ein reines Einloggen-ohne-Lernen-Streak widerspräche der Kernidee des erzwungenen
   Lernerfolgs. Kosten: die Box-Streak ist deckungsgleich mit dem in `overview`/`overview/progress`
   bereits sichtbaren `CurrentStreak` — keine zweite Quelle der Wahrheit, aber auch keine
   Box-eigene Nuance möglich.
3. **Box-Ökonomie liegt fest in `appsettings.json`** (`Gamification:DailyBox`, analog
   `Scoring:TimeSlots`), nicht Supervisor-konfigurierbar. Begründung: kleinerer erster Schnitt.
   Kosten: der Vater kann Wertspannen nicht individuell je Kind anpassen — eigene spätere Story, falls
   gewünscht.
4. **Erster Schnitt: nur Münzen/Gems, keine Skin-Drops.** Begründung: `SkinCatalog` hat kein
   Rarity-/Gewichtungs-Konzept, das wäre ein eigener Neubau. Kosten: der Sammel-Reiz (die eigentliche
   Loot-Box-Spannung aus dem Vorbild) fehlt im ersten Schnitt; Skin-Drops bleiben ein klar benannter
   Ausbauschritt.
5. **Streak-Bruch ist ein harter Reset**, kein „Streak Freeze". Begründung: konsistent mit dem
   bestehenden `PenaltyCoins`-Verhalten, das ebenfalls hart abrechnet, statt einen Fehltag zu
   verzeihen. Kosten: keine Schonfrist bei einem einzelnen verpassten Tag (z. B. Krankheit) — akzeptiert
   für den ersten Schnitt.
6. **Eskalation über zwei feste Stufen**: Streak ≥ 7 Tage → ×1,5, Streak ≥ 30 Tage → ×2,0 auf die
   gezogene Coins-/Gems-Menge (höchste zutreffende Stufe gewinnt, keine Kumulierung). Begründung: einfache,
   nachvollziehbare Zahlen ohne eigene Recherche zur Spielbalance; als Konfigurationswerte hinterlegt,
   nicht hartkodiert, damit eine Nachjustierung keine Codeänderung braucht.
7. **Ethische Leitplanke bestätigt**: kein Verfalls-Timer auf die Box, kein Push-Reminder. Die Box
   wartet, bis das Kind sie durch erfüllte Pflicht tatsächlich auslöst, und verfällt nie. Begründung:
   das Ziel ist erzwungener Lernerfolg, nicht Zeitdruck um seiner selbst willen — Letzteres wäre ein
   Dark Pattern gegen ein Kind. Kosten: keine (die Mechanik ist ohnehin schon geduldig, weil sie
   lazy an den Pflicht-Abschluss hängt statt an einen Kalendertimer).

## Akzeptanzkriterien

1. Bei `DutyDone = true` im `DayOverview` erhält das Kind einmalig pro Kalendertag eine Box mit
   zufälliger Coins-/Gems-Menge aus einer konfigurierten Spanne, skaliert nach Streak-Stufe
   (Entscheidung 6).
2. Ein zweiter Testabschluss/Sitzungsende am selben Tag löst keine zweite Box aus (Idempotenz über
   einen Unique Index `(ChildId, Day)` auf der neuen `DailyBoxClaim`-Entity, analog `PositionGoalReward`
   — **nicht** über den `ConcurrencyStamp`, der reine Credit-Pfad braucht keinen Debit-Schutz).
3. Der Streak-Zähler (wiederverwendet aus `PositionProgressService.Streak`) erhöht sich nur an Tagen mit
   `DutyDone = true` und setzt beim ersten ausgelassenen Pflicht-Tag zurück.
4. Box-Inhalt (falls heute schon geklaimt) und aktueller Streak sind über den bestehenden
   Student-Endpunkt `GET study-plans/{planId}/overview` lesbar (erweitert um ein `dailyBox`-Feld) — kein
   neuer Endpunkt nötig.
5. Die Auswertung läuft ausschließlich an den bestehenden POST-Seams (Testabschluss, Sitzungsende),
   nie bei einem GET (kein Belohnen als Nebenwirkung eines Lesezugriffs, wie bei Missionen/Auszeichnungen).
6. Ein Integrationstest deckt: Box genau einmal pro Tag, Idempotenz bei doppeltem Abschluss, Anzeige im
   Overview, sowie die Streak-Eskalationsstufe.

## Schätzung

**Größe: S** — Anker B-01 (`childId` aus dem Test-Pfad ziehen): ähnlich lokal, obwohl mehr Dateien
berührt sind, weil jede einzelne Änderung eine Kopie eines bereits vorhandenen Musters ist (Reward-Entity,
Idempotenz-Index, PointKind-Paar, Options-Klasse), kein neues Konzept außer der RNG-Ziehung selbst.

- **Migration**: ja — neue Entity `DailyBoxClaim`, Kette wird neu gefaltet (`InitialCreate`).
- **Vertragsbruch**: nein — `OverviewResponse` bekommt ein zusätzliches, additives Feld (`dailyBox`);
  kein bestehendes Feld ändert Typ oder verschwindet.
- **Risiken**: keine besonderen — der Trigger hängt an bereits vorhandenen, gut getesteten Seams
  (`PositionTestsController.Submit`, `PositionPracticeController.End`); die Streak-Berechnung ist die
  bereits produktiv genutzte `PositionProgressService.Streak`.
- **Angriffsplan**: PointKind/Currency-Mapping → Entity + DbContext → Migration neu falten → Options
  + Service → Contracts-Erweiterung → Controller-Verdrahtung (Trigger in beiden Controllern, Anzeige in
  `PlanOverviewController`) → Konfiguration in `appsettings.json` → Tests.
- **Testweg**: neue Integrationstests (`DailyBoxTests.cs`, Muster `PositionGoalOverviewTests.cs`) gegen
  die reale Test-Factory; `/smoke-test` zur End-to-End-Bestätigung; `pugling-reviewer` vor dem Commit.

## Verlauf

- **2026-08-04** — angelegt (Quelle: Gespräch mit dem Nutzer über Mobile-Game-Mechaniken als Vorbild),
  direkt als `ausformuliert` mit Code-Recherche über einen Explore-Agenten.
- **2026-08-04** — auf Anweisung des Nutzers ohne separate Grill-Sitzung direkt auf `gegrillt` (eigene
  Empfehlungen als Entscheidungen übernommen) und `geschaetzt` gehoben, Umsetzung beginnt sofort
  (`in-arbeit`).
- **2026-08-04** — umgesetzt und abgenommen: `dotnet test Pugling.sln -c Release` 712/712 grün
  (inkl. der beiden neuen Tests in `DailyBoxTests.cs`); `/smoke-test` gegen eine Wegwerf-DB grün
  (Auth, Ownership-Filter, kompletter Plan→Test→Submit-Flow inkl. Punktevergabe, Anmerkungen);
  `pugling-reviewer` lief gegenlesend über den vollen Diff, keine Blocker — ein „Sollte"-Befund
  (`DailyBoxService.EvaluateAndAwardAsync` scannte über `PositionProgressService.ProgressAsync` die
  gesamte Plan-Laufzeit für die Streak-Berechnung, statt wie `SettleClosedPeriodsAsync` einen
  begrenzten Rückblick zu nutzen) wurde behoben: neue `PositionProgressService.StreakBoundedAsync`
  (kurzschließend, 45 Tage Deckel – die Eskalationsstufen enden ohnehin bei 30) ersetzt den
  Vollständig-Scan auf dem Schreibpfad; die Suite bleibt danach grün. Commit `2d7f900`, dazu dieser.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `DailyBoxService`, `DailyBoxClaim` und die
  plan-weiten Trigger weiterhin existieren und ob die Streak-Eskalation dasselbe Muster nutzt — hält
  (`Services/Shared/DailyBoxService.cs`, `Models/GamificationEntities.cs`, Trigger in
  `PositionPracticeController`/`PositionTestsController`). Kein Fund.
