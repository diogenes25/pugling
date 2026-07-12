# EXPLAIN Query Plan Check (2026-07-12)

## Ziel
Nachweis, dass die neu eingefuehrten Hotpath-Indizes vom SQLite-Optimizer fuer zentrale EF-Query-Muster verwendet werden.

## Methode
- Ausfuehrung per automatisiertem Smoke-Test in [backend/Pugling.Api.Tests/QueryPlanSmokeTests.cs](backend/Pugling.Api.Tests/QueryPlanSmokeTests.cs).
- Frische temporaere SQLite-DB, Migrationen werden vollstaendig angewendet.
- Pruefung via `EXPLAIN QUERY PLAN` auf den erwarteten Indexnamen.

## Gepruefte Query-Muster
1. Wallet-Ledger Paging (`ChildPoints` nach `CreatedAt DESC, Id DESC`)
- Erwarteter Index: `IX_ChildPoints_ChildId_CreatedAt_Id`

2. Wallet-Summen nach Punkteart (`ChildPoints` mit `Kind IN (...)`)
- Erwarteter Index: `IX_ChildPoints_ChildId_Kind`

3. Zielpruefung Uebungssession (`PracticeSessions` auf Position + Day-Range + Mode)
- Erwarteter Index: `IX_PracticeSessions_PlanPositionId_Day_Mode`

4. Zielpruefung Test (`TestAttempts` auf Position + Day-Range + CompletedAt + Passed)
- Erwarteter Index: `IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed`

5. Positionen-Liste im Plan (`PlanPositions` nach StudyPlan + Order + Id)
- Erwarteter Index: `IX_PlanPositions_StudyPlanId_Order_Id`

6. Plan-Rollup Uebungssessions (`PracticeSessions` auf StudyPlan + Day-Range)
- Erwarteter Index: `IX_PracticeSessions_StudyPlanId_Day`

7. Plan-Rollup Tests (`TestAttempts` auf StudyPlan + Day-Range)
- Erwarteter Index: `IX_TestAttempts_StudyPlanId_Day`

8. Lernstand je Uebung (`ItemProgress` auf Child + Exercise)
- Erwarteter Index: `IX_ItemProgress_ChildId_ExerciseId`

## Ergebnis
- Alle geprueften Muster nutzen den erwarteten Index.
- Teststatus: 1/1 erfolgreich.
- Ausfuehrung: `dotnet test backend/Pugling.Api.Tests/Pugling.Api.Tests.csproj --filter QueryPlanSmokeTests`

## Hinweis
Der Nachweis ist als Regression-Guard im Test enthalten und kann bei kuenftigen Modell-/Migrationsaenderungen direkt wiederverwendet werden.
