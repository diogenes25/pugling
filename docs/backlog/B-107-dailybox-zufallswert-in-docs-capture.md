---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [DailyBox nicht deterministisch, coinsAwarded schwankt in docs/api-examples]
status: idee
prio: P3
art: Aufräumen
quelle: pugling-reviewer-Befunde zu B-104 und B-60 (2026-08-05) — `docs/api-examples/study-plans.md` und
  `backend/Pugling.Api/OpenApi/openapi-examples.generated.json` ändern bei jedem `dotnet test`-Lauf den
  Wert `dailyBox.coinsAwarded` (beobachtet: 30, 22, 12, 21 in verschiedenen Läufen), obwohl kein
  fachlicher Code sich geändert hat
unverifiziert: true
---

# B-107 · `DailyBoxService` würfelt ohne Seed – der Doku-Capture-Snapshot ist dadurch nicht byte-stabil

`Services/Shared/DailyBoxService.cs:44-45` zieht die Belohnungshöhe der täglichen Box über
`Random.Shared.Next(...)` ohne festen/injizierbaren Seed. `DocsCaptureTests` schreibt
`docs/api-examples/study-plans.md` (und das zugehörige `openapi-examples.generated.json`) bei jedem
Testlauf neu und friert dabei den zufälligen Wert ein – jeder erneute `dotnet test`-Lauf erzeugt daher
einen Diff, der nichts mit der eigentlichen Codeänderung zu tun hat (mehrfach in Reviews als
„Nebenbefund, nicht Teil dieser Story" markiert, u. a. bei B-104 und B-60).

Das widerspricht der in [docs/db-struktur-umbau-plan.md](../db-struktur-umbau-plan.md)/Memory
„Doku-Capture byte-stabil" festgehaltenen Erwartung: zeitabhängige Werte werden maskiert
(`Redact`), aber dieser Zufallswert ist bislang nicht erfasst.

Noch nicht ausformuliert: ob die Test-Factory `DailyBoxService` einen deterministischen `Random`
injizieren sollte (analog zu `TimeProvider` an anderer Stelle), oder ob `DocsCaptureTests` den Wert
wie andere volatile Felder redigieren soll.
