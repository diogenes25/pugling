---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Lösch-Summary ohne Ziele]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08.md#retrospektive
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-409]
nachgeschaut: 2026-11-13
---

# B-416 · Das Swagger-Summary von „Übung löschen" nennt keine Ziel-Etappen; `GoalUsage.ObjectiveId` prüft kein Test

Nachschau zu Sprint 20 (frischer `pugling-reviewer`, `git show c910cf8`, ein 🟡, dazu 🟢). Am Code nachgeprüft (2026-11-08):

- 🟡 `ExerciseControllerBase.Delete` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:489`): „Not possible while it is
  embedded in a study plan or a class test." – das fließt in `docs/openapi/v1.json` für jedes `DELETE …/{type}/{exerciseId}`. Seit dem
  Scope-FK sperrt auch eine Ziel-Etappe, und B-409 hat genau das sichtbar gemacht, aber nur das Summary von `Usage` nachgezogen.
  Dieselbe Drift in `docs/uebergabe-vokabeln.md:52-55` (alte Route `learn/exercises/{id}/usage`, „Lehrpläne + Klassenarbeiten").
- 🟢 `ExerciseUsageScopeTests.EigeneZielEtappe_WirdUnterVerwendungGenannt` sichert `objectiveId` nicht zu: eine Projektion von `k.Id`
  statt `k.ObjectiveId` bliebe grün.
- 🟢 kein Fall mit einem Kind, das zwei Betreuer hat (beide sollen die Etappe namentlich sehen).

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Summary von `Delete` nennt Lehrplan, Klassenarbeit **und Ziel-Etappe**; `docs/uebergabe-vokabeln.md` ebenso (die Route
   war inzwischen schon richtig).
2. `EigeneZielEtappe_WirdUnterVerwendungGenannt` sichert `objectiveId` gegen die Id des angelegten Ziels zu. Weil Ziele und
   Etappen getrennt zählen und in der geteilten Test-DB gleich sein können (gemessen: beim ersten Lauf gleich), schiebt ein
   Wegwerf-Ziel mit zwei Etappen die Zähler vorher auseinander, und eine Vorbedingung hält das fest.
3. 🟢 „Kind mit zwei Betreuern" bewusst nicht gebaut: die Sichtregel trägt `ExerciseUsageScopeTests` schon über die
   Betreuung; ein zweiter Betreuer ist derselbe Pfad.

## Akzeptanzkriterien

1. Eine Projektion `k.Id` statt `k.ObjectiveId` macht den Test rot.

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 20 (Retro Sprint 21, Lauf AD). Increment eines **früheren** Laufs (AC) – kein Limit 2 für Lauf AD.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 51).
- **2026-11-13** — gebaut (Sprint 51). AK 1 → `EigeneZielEtappe_WirdUnterVerwendungGenannt`; R1 (`k.Id` statt
  `k.ObjectiveId`) → rot. Erster Lauf fiel an der eigenen Vorbedingung (beide Ids gleich) – darum das Wegwerf-Ziel.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1434/1434; Mutanten-Probe nach der letzten Änderung (Debug,
    gefiltert) 8/8 rot je am genannten Test, Grundlinie grün (Tabelle im Protokoll, Sprint 50/51).
  - Rollengang Creator: Integrationstest gegen den echten In-Process-Server; `docs/openapi/v1.json` trägt das neue
    Summary. Vater, Sohn: Regressionszeugen. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 52/53, frischer `pugling-reviewer` an `2917449` + `82fdc4c`, gelesen).
  Geprüft: Probe-Ziel-Logik gegen parallele Testklassen (eigene SQLite-Datei je Klasse, Tests einer Klasse seriell);
  `concurrency_conflict`-Quellen nachgezählt; Zeittest ohne Lockvogel rot an allen drei Pfaden; AK-Tests je gegen die
  benannte Änderung. Befund: sauber.
