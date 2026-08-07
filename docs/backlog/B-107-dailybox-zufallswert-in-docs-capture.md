---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/qualitaet]
aliases: [DailyBox nicht deterministisch, coinsAwarded schwankt in docs/api-examples]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer-Befunde zu B-104 und B-60 (2026-08-05) — `docs/api-examples/study-plans.md` und
  `backend/Pugling.Api/OpenApi/openapi-examples.generated.json` ändern bei jedem `dotnet test`-Lauf den
  Wert `dailyBox.coinsAwarded` (beobachtet: 30, 22, 12, 21 in verschiedenen Läufen), obwohl kein
  fachlicher Code sich geändert hat
nachgeschaut: "2026-08-07"
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

## User Story

Als Entwickler möchte ich, dass zwei Testläufe ohne Codeänderung byte-identische Dateien unter
`docs/api-examples/` erzeugen, damit das CI-Tor D4 nur bei einer echten Vertragsänderung rot wird und
ein Diff dort wieder etwas bedeutet.

## Ist-Stand am Code

Belegt am 2026-08-06 am `HEAD`, nicht aus der Notiz übernommen:

- **`backend/Pugling.Api/Services/Shared/DailyBoxService.cs:44-45`** — `Random.Shared.Next(…)` für Münzen
  **und** Gems; die Story nannte nur `coinsAwarded`, betroffen sind beide Felder.
- **`backend/Pugling.Api/appsettings.json:24-29`** — Spannen `MinCoins 10 / MaxCoins 30`,
  `MinGems 0 / MaxGems 2`.
- **`backend/Pugling.Api.Tests/PuglingWebAppFactory.cs`** — neutralisiert bisher genau eine Quelle
  (`Scoring:TimeSlotsEnabled`), mit ausführlich begründetem Kommentar. Die Ziehung war dort nicht erfasst.
- **Gemessen:** zwei aufeinanderfolgende `DocsCaptureTests`-Läufe gegen den committeten Stand ergaben
  `coinsAwarded` 10 → 27 und `gemsAwarded` 2 → 0. Der CI-Lauf `31076750899` fiel dadurch an einer
  völlig unbeteiligten Änderung.

## Die echte Lücke

Nicht „ein Wert schwankt", sondern: **das Tor D4 misst etwas, das sich ohne Zutun ändert.** Ein Tor, das
zufällig rot wird, erzieht dazu, sein Rot zu ignorieren — und genau dann ist es wertlos, wenn eine echte
Vertragsänderung durchrutscht. Der Schaden lag nicht in der Doku, sondern in der Glaubwürdigkeit des Tors.

## Offene Punkte

1. ~~Deterministischen `Random` injizieren **oder** in `Redact` maskieren?~~ → beides verworfen, siehe
   Entscheidung 1.
2. ~~Nur `coinsAwarded` betroffen?~~ → nein, `gemsAwarded` ebenso.

## Entscheidungen

1. **Die Spannen kollabieren in der Test-Factory auf je einen Wert** (`Gamification:DailyBox:Min/Max`),
   statt einen `Random` zu injizieren oder das Feld zu maskieren. Begründung: es ist derselbe Griff, mit
   dem die Zeitfenster schon neutralisiert sind — eine Einstellung **vor** dem Start, kein neuer
   Konstruktor-Parameter im Produktionscode und keine zweite Redigier-Regel, die man beim nächsten Feld
   wieder vergisst. Die Doku zeigt weiter eine plausible Zahl statt `<random>`. Kosten: die
   Ziehungs*spanne* wird in Tests nicht mehr durchlaufen (siehe Entscheidung 2).
2. **Die verlorene Abdeckung wird durch eine schärfere Zusicherung ersetzt:** `DailyBoxTests`
   prüft die Streak-Stufe jetzt als exakten Wert (Basis 20 × 1,5 = 30) statt als Spanne `10…45`, die auch
   eine nicht eskalierte Ziehung erfüllt hätte. Kosten: der Test kennt damit den Pin der Factory — der
   Zusammenhang steht als Kommentar an beiden Stellen.

## Akzeptanzkriterien

- Zwei aufeinanderfolgende Läufe erzeugen byte-identische Dateien unter `docs/api-examples/` und in
  `openapi-examples.generated.json`. ✅
- Auch nach einem vollen Suite-Lauf unverändert. ✅
- Die Zusicherung über die Streak-Stufe wird nicht schwächer. ✅

## Schätzung

`XS` — vier `UseSetting`-Zeilen plus eine geschärfte Zusicherung, vergleichbar dem Anker B-02.
`migration: nein` (kein Schema), `vertragsbruch: nein` (kein `Contracts`-Typ berührt).

**Testweg:** `DocsCaptureTests` zweimal hintereinander mit Hash-Vergleich über die 13 Beispieldateien und
den Beispielkatalog, dazu `DailyBoxTests` und die volle Backend-Suite.

## Verlauf

- **2026-08-05** — angelegt aus zwei `pugling-reviewer`-Nebenbefunden (zu B-104 und B-60), die den
  wandernden Wert je als „nicht Teil dieser Story" markiert hatten.
- **2026-08-06** — `idee` → **`abgenommen`**, auf ausdrückliche Entscheidung des Nutzers **unter
  Auslassung der Zwischenstufen**. Der Grund gehört zur Ehrlichkeit dieser Zeile: die Arbeit war bereits
  gebaut, als die Story gesichtet wurde — der Einstieg war das rote CI-Tor D4 (Lauf `31076750899`), nicht
  der Backlog. `ausformuliert`/`gegrillt`/`geschaetzt` sind darum **nachgetragen statt erarbeitet**.
  Belege: zwei aufeinanderfolgende `DocsCaptureTests`-Läufe erzeugen byte-identische Dateien
  (md5 über alle 13 Beispieldateien + `openapi-examples.generated.json`), auch nach einem vollen
  Suite-Lauf; **746/746 grün**. Commit `2d91047`.
- **2026-08-06** — `pugling-reviewer`: **tragfähig, keine Auflage**. Er hat die Behauptung selbst
  nachgemessen (zwei volle Release-Läufe, danach `git status` für `docs/api-examples/` leer) und die
  Frage nach einer *zweiten* Zufallsquelle im Capture-Pfad beantwortet: `Random.Shared` steht in
  `Pugling.Api` an drei Stellen, die beiden anderen (`PositionPlayService.cs:204,228` — nur bei
  `PracticeOrder.Random`/`NewestWeighted`, Vorgabe ist `WeakestFirst`; `BuiltInExerciseTypes.cs:328` —
  nur bei `generate` ohne Seed) werden von keinem Beispiel erreicht. Zwei seiner drei Notizen sind
  eingearbeitet: die Spannen-Zusicherungen des ersten Tests waren unter dem Pin bedeutungslos geworden
  (jetzt `Assert.Equal(20/2)`), und die Zusammenfassung von `SchemaOnlyWebAppFactory` behauptete
  „dasselbe wie", ohne den Pin zu tragen — sie sagt jetzt, dass und **warum** sie ihn auslässt. Danach
  erneut **746/746 grün**.
- **2026-08-06** — die dritte Notiz ist **nicht** eingearbeitet, sondern als
  [B-118](B-118-dailybox-spanne-ohne-zusicherung.md) abgelegt: seit dem Pin sieht kein Test mehr eine
  echte Ziehung über `[Min,Max]`. Das Ziel dieser Story (byte-stabile Capture-Dateien) ist ohne sie
  erfüllt, und der Reviewer hält fest, dass der Pin nichts kaputt gemacht, sondern eine vorbestehende
  Lücke sichtbar gemacht hat.
- **2026-08-06** — **Rollengang: ausgefallen.** Die Änderung ist an keiner Stelle für Creator, Vater oder
  Sohn sichtbar — sie betrifft ausschließlich die Test-Factory und ein eingechecktes Doku-Artefakt. Ein
  Gang durch die laufende App hätte hier nichts prüfen können, was die Hash-Gleichheit nicht schon zeigt.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `DailyBoxService` weiterhin über `Random.Shared`
  würfelt und `PuglingWebAppFactory` die Spanne weiterhin auf feste Werte pinnt — hält
  (`DailyBoxService.cs:44-45`, `PuglingWebAppFactory.cs:157-160`). Die dritte Notiz (Spanne ungeprüft) ist
  erwartungsgemäß als B-118 ausgelagert und dort inzwischen abgenommen — kein Widerspruch. Kein Fund.
