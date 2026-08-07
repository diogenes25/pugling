---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/qualitaet]
aliases: [DailyBox-Spanne ungeprüft, Min/Max ohne Test]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer-Befund zur Abnahme von
  [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) (2026-08-06) — nicht dort mitgenommen, weil
  B-107s Ziel (byte-stabile Capture-Dateien) ohne diesen Punkt erfüllt ist
unverifiziert: false
---

# B-118 · Keine Zusicherung sieht die Ziehungsspanne der Tagesbox mehr

Seit [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) pinnt `PuglingWebAppFactory` die Münz- und
Gem-Ziehung der täglichen Box auf je einen festen Wert. Damit durchläuft **kein** Test mehr eine echte
Ziehung über `[Min,Max]`: die dokumentierte Inklusivität der Obergrenze (`opts.MaxCoins + 1` in
`DailyBoxService.cs:44`) und die Bindung an die Spanne aus `appsettings.json` sind unbelegt. Der Reviewer
hält ausdrücklich fest, dass B-107 dabei **nichts kaputt gemacht** hat — die vorherige
`Assert.InRange(10, 30)` hätte weder eine vertauschte noch eine um eins verfehlte Grenze gefangen; der Pin
macht eine schon vorhandene Lücke nur sichtbar. Vorschlag aus dem Review: eine eigene kleine Factory mit
einer engen, von der Produktionsspanne verschiedenen Spanne (etwa `Min 7 / Max 9`) und einer
`InRange(7, 9)`-Zusicherung — dasselbe Muster, mit dem `TimeSlotsOnFactory` die stillgelegten Zeitfenster
für den einen Test wieder anschaltet, der sie sehen muss.

## User Story

Als **Entwickler**, der sich beim Ändern von `DailyBoxService` auf die Testsuite als Regressionsnetz
verlässt, möchte ich, dass ein Test tatsächlich beide Enden der Ziehungsspanne erreicht — damit eine
vertauschte oder um eins verfehlte Grenze auffällt, statt lautlos grün durchzulaufen.

## Ist-Stand am Code

Seit [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) pinnt `PuglingWebAppFactory` die Münz- und
Gem-Ziehung der Tagesbox auf je einen festen Wert. Damit durchläuft kein Test mehr eine echte Ziehung
über `[Min,Max]`: die dokumentierte Inklusivität der Obergrenze (`opts.MaxCoins + 1` in
`DailyBoxService.cs:44`) und die Bindung an die Spanne aus `appsettings.json` sind unbelegt. Die
vorherige `Assert.InRange(10, 30)` hätte weder eine vertauschte noch eine um eins verfehlte Grenze
gefangen — der Pin macht eine schon vorhandene Lücke nur sichtbar (Befund des `pugling-reviewer` zur
B-107-Abnahme).

## Entscheidungen

1. **Eigener Factory-Zuschnitt statt neuer Lösung.** Der Reviewer-Vorschlag (eigene Factory mit enger,
   von Produktion und Test-Pin verschiedener Spanne, Vorbild `TimeSlotsOnFactory`) ließ keine offene
   Frage übrig — direkt übernommen. Kosten: eine neue Factory-Klasse (`DailyBoxRangeFactory`).
2. **Direkter Aufruf statt HTTP-Umweg.** Der Test ruft `DailyBoxService.EvaluateAndAwardAsync` direkt
   über die DI-Scope auf, statt über einen echten Positions-Testlauf je Ziehungsversuch — das wäre
   teuer und unnötig, da nur die Ziehungslogik selbst geprüft wird. Kosten: ein manuell konstruiertes
   `DayOverview(day, true, …)` als Award-Gate; ein Plan ohne `PlanPosition`s hält den
   Streak-Multiplikator dabei bei jedem Versuch auf 1.0 (`ComputeDayAsync`:
   `obligations.Count == 0` ⇒ `dutyDone = false` ⇒ `StreakBoundedAsync` bricht sofort bei Streak 0 ab).

## Schätzung

`groesse: XS`, `wo: backend`, `migration: nein`, `vertragsbruch: nein` (reiner Test-Code, kein
Produktivpfad geändert). Angriffsplan: `DailyBoxRangeTests.cs` + `DailyBoxRangeFactory` (Min/Max 7/9
Coins, 2/4 Gems), 60 Ziehungsversuche über 60 verschiedene Tage desselben Kindes, je ein eigener
Wegwerf-Plan. Testweg: rote Probe durch gezielte Fehler-Injektion (exklusive statt inklusive
Obergrenze) — siehe „Verlauf" für die tatsächliche Umsetzung und die Messzahlen.

## Akzeptanzkriterien

1. Eine eigene Factory setzt Coins/Gems auf eine enge Spanne, verschieden von Produktions-Default
   (10-30/0-2) **und** vom Test-Pin (20/2) — sonst prüfte der Test nur den Pin.
2. Ein Test zieht mehrfach und belegt **beide** Grenzen als tatsächlich erreicht (nicht nur „innerhalb"),
   sonst fängt er eine exklusive statt inklusive Obergrenze nicht.
3. Der Test läuft ohne echten Endpunkt-Umweg schnell genug, um kein spürbares Suite-Gewicht zu werden.

## Verlauf

- **2026-08-07** — ausformuliert, gegrillt, geschätzt (**XS**, `wo: backend`) in einem Zug: der
  Reviewer-Vorschlag (eigene Factory, `TimeSlotsOnFactory`-Vorbild) ließ keine offene Entscheidung übrig.
  Einzige Ausformulierung: statt über HTTP zu ziehen (teuer, bräuchte einen echten Positions-Testlauf je
  Trial), ruft der Test `DailyBoxService.EvaluateAndAwardAsync` direkt über die DI-Scope auf, mit einem
  manuell konstruierten `DayOverview(day, true, …)` als Award-Gate — ein Plan ohne `PlanPosition`s hält
  den Streak-Multiplikator dabei bei jedem Versuch auf 1.0 (`ComputeDayAsync`: `obligations.Count == 0` ⇒
  `dutyDone = false` ⇒ `StreakBoundedAsync` bricht sofort bei Streak 0 ab, weit unter der ersten Eskalation
  bei 7).
- **2026-08-07** — umgesetzt: `DailyBoxRangeTests.cs` + `DailyBoxRangeFactory` (Min/Max 7/9 Coins, 2/4
  Gems). 60 Versuche über 60 verschiedene Tage desselben Kindes (id 1, aus dem Development-Seed), je ein
  eigener Wegwerf-Plan. **Rote Probe** (Fehler injiziert: `Random.Shared.Next(opts.MinCoins, opts.MaxCoins)`
  statt `+ 1`, also exklusive Obergrenze): `Assert.Contains() Failure: ... Not found: 9`. Zurückgesetzt:
  grün. Volle Suite: **760/760 grün** (758 vor diesem Sprint + 2 neue Tests, gemeinsam mit B-120).
- **2026-08-07** — `pugling-reviewer` gefahren (zusammen mit B-120): **kein Blocker.** Die Kernannahme
  (Streak bleibt bei einem positionslosen Plan immer 0, Multiplikator 1.0) unabhängig gegen
  `PositionProgressService.ComputeDayAsync`/`StreakBoundedAsync` nachgerechnet und bestätigt; 60 Versuche
  als statistisch großzügige, aber nicht übertriebene Marge bewertet ((2/3)^60 ≈ 3×10⁻¹¹); Aufräum-
  Verhalten der neuen Factory korrekt (keine eigene Dispose-Logik nötig, Basisklasse reicht).
- **2026-08-07** — Rollengang-Ersatz: kein UI-Kandidat (reiner Test-Code, kein Produktivverhalten
  geändert). Ersatz: volle Suite plus Reviewer plus der gezielte Fehler-Injektions-Beleg oben.
- **2026-08-07** — `abgenommen`.
