---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/supervisor]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: "backend"
migration: "nein"
vertragsbruch: "nein"
quelle: "B-944 Punkt 1, am Code bestätigt in Sprint 137 (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-757]
wartet_auf: ""
nachgeschaut: "2026-11-24"
---

# B-946 · Nach dem Start zeigt die Vater-Liste den Deckel statt der Karten einer kleinen Übung ohne Tagesdosis

Aus B-944 Punkt 1 herausgelöst, weil er am Code bestätigt ist und den Vater direkt trifft.

## User Story

Als Vater möchte ich nach dem Start der Vorbereitung dieselbe Zahl „etwa N Karten je Tag“ sehen wie vorher, damit ich der
Last-Angabe trauen kann.

## Ist-Stand am Code (2026-11-24, `cfebfe60`)

- Der Start legt eine Position ohne Tagesdosis mit `ItemCount = MaxCardsPerDay` (10) an
  (`backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:239`).
- Vor dem Start rechnet die Liste `min(Karten, Deckel)` (`DailyCards`, `:98`), nach dem Start nahm sie `ItemCount` ungeprüft
  (`CardsPerDayAsync`, `:110-112`). Eine Rechenübung mit drei Aufgaben sprang so von „etwa 3“ auf „etwa 10“.
- Das Kind spielt die Runde über `PositionPlayService.PoolSize` (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:463`):
  `min(ItemCount, vorhandene Karten der gespielten Übung)`, also 3.

## Die echte Lücke

Die Liste rechnete die Rundengröße selbst, statt die eine Regel zu lesen, nach der das Kind spielt.

## Akzeptanzkriterien

1. Eine Übung ohne Dosis mit 3 Karten zeigt vor und nach dem Start „etwa 3“.
2. Eine Übung ohne Dosis mit 12 Karten zeigt vor und nach dem Start den Deckel 10.

## Entscheidungen

1. Die Liste liest dieselbe Regel wie die Runde des Kindes (`PositionPlayService.PoolSize`), keine eigene Rechnung.

## Schätzung

- **Größe:** XS – eine Zeile in `CardsPerDayAsync`, keine Migration, kein Vertragsbruch.

## Angriffsplan und Testweg

`CardsPerDayAsync` liest für gestartete Positionen ohne Dosis `play.PoolSize(p, heute, ItemsOfAsync(p, heute))`. Dazu kommen zwei
Integrationstests in `VorbereitungsPflichtTests` und der Rollengang über `e2e/klassenarbeit-vorbereitung.spec.ts`.

Risiko: Die Liste kostet je gestarteter Arbeit eine Abfrage plus `ItemsOfAsync` je Position ohne Dosis. Das ist dieselbe
Größenordnung wie der Zweig vor dem Start.

## Verlauf

- **2026-11-24** — angelegt und gebaut in Sprint 137.
- Nach dem Review auf `PositionPlayService.PoolSize` umgestellt (eine Regel für Liste und Runde). Dabei Zeile 119 neu gemessen,
  nach dem letzten Edit:
- AK 1, kleine Übung → `VorbereitungsPflichtTests.Karten_Je_Tag_Bleiben_Nach_Dem_Start_Bei_Einer_Kleinen…`, Mutante B946-1
  (`ItemCount` statt `PoolSize`) → rot.
- AK 2, große Übung am Deckel → `…_Am_Deckel_Einer_Grossen…`, Mutante B946-2 (Karten ohne Deckel) → rot.

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| B946-1 Deckel statt Karten | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:119 | red | red |
| B946-2 Karten ohne Deckel | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:119 | red | red |

Der erste Versuch von B946-1 (`p.ItemCount ?? 0`) kompilierte nicht. Er ist als kompilierbare Form neu gemessen, die das
`await` behält.

- **2026-11-24** — abgenommen in Sprint 137, Commit `262bd5ae`. Volle Suite 2315/2315 grün (Release), Build ohne Warnung.
  `pugling-reviewer`: für B-945 kein Fund, für B-946 ein 🟡 (eigene Rechnung statt `PoolSize`, Deckel-Fall ungetestet), beide
  eingearbeitet. Rollengang: `klassenarbeit-countdown.spec.ts` und `klassenarbeit-vorbereitung.spec.ts` gegen einen frisch
  gestarteten Server auf Ausweichports (5201/5174, Release), 2 passed. Die Specs fahren die Anzeige, nicht den Überlappungs- bzw.
  Deckelfall; den tragen die Integrationstests.
- **2026-11-24** — nachgeschaut in der Retro von Sprint 138 (`262bd5ae`): ohne Befund.
