---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: B-822
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-851 · Vokabel-Türme: Spiel-Run mit Bewertung je Zuweisung, Münzdeckel und Minutenlimit

Teil 2 von 4 aus B-822. Neue Student-Endpunkte `tower-game/…`: Run aus einer Position (Türme und Tokens
ohne Zuordnung), `assign` mit serverseitiger Bewertung und Lernstand-Quelle `Game`, zurückgelegte
Tokens, Heartbeat, Run-Ende mit eigenem Münzdeckel (30/Tag) und Minutenlimit je Kind (Vorgabe 20).
Entscheidungen und Akzeptanzkriterien: [B-822](B-822-vokabel-tuerme-natives-spiel.md); Spielidee und
Technik: [turm-spiel-plan.md](../turm-spiel-plan.md).

## Gebaut (2026-11-23)

Entscheidungen stellvertretend nach eigener Empfehlung getroffen (Auftrag des Nutzers), zum Umstoßen:

- **Run-Tabellen** `TowerGameRun` + `TowerGameRunItem` (Migration `TowerGameRuns`, nur angehängt; nimmt keine
  Bestandszeilen mit, weil nur neue Tabellen und eine nullable Spalte entstehen). Der Run friert seine Wörter ein und
  zählt selbst, welche Türme gelöst sind – gezahlt wird nach **dieser** Zählung, nie nach der des Clients. Turm- und
  Token-Ids bleiben verschlüsselte Referenzen, jetzt an den Run statt an die Position gebunden.
  *Bewusst offen:* ein App-Neustart setzt das Level nicht fort (es gibt keinen Resume-Endpunkt); der Stoff ist
  eingefroren, aber nur für Zählung und Bewertung.
- Endpunkte unter `student/children/{id}/tower-game`: `GET` (Positionen, Spielzeit heute/Limit, Münzen heute/Deckel),
  `POST runs`, `POST assign {runId,…}`, `POST runs/{runId}/heartbeat`, `POST runs/{runId}/end`
  ([TowerGameController.cs](../../backend/Pugling.Api/Controllers/Student/TowerGameController.cs)).
- **Münzen:** eigener `PointKind.TowerGameCoins`, 10 je gewonnenem Level, Deckel 30/Tag
  (`Gamification:TowerGame`, [TowerGameOptions.cs](../../backend/Pugling.Api/Services/Student/TowerGameOptions.cs)).
  Gewonnen = so viele **verschiedene** Türme gelöst wie das Ziel (10, bei kleineren Listen alle). `end` ist
  idempotent und bumpt `ConcurrencyStamp`. Der Deckel ist für alle Kinder gleich – je Kind übersteuerbar ist nur
  das Minutenlimit.
- **Heartbeat:** die App meldet alle 15 s; jeder Schlag zählt höchstens 20 s, eine Hintergrund-Pause also nicht.
- **Minutenlimit:** `Child.TowerGameMinutesPerDay` (null = Vorgabe 20, 0 = Spiel aus, 0–240), Vater-Web unter
  Kind → Einstellungen „Vokabel-Türme (Handyspiel)" mit Tagesstand. Ist es erreicht, verweigert der Start mit
  `409 tower_game_time_limit`; ein laufendes Level darf 5 Minuten weiter, danach `409 tower_game_run_closed`
  und der Heartbeat meldet `timeUp`.
- Tests: [TowerGameTests.cs](../../backend/Pugling.Api.Tests/TowerGameTests.cs) (9 Fälle: Ids, Bewertung, Fremd-Run,
  Box-Dämpfung, Münzen bis Deckel + doppeltes Ende, derselbe Treffer zählt einmal, Zuweisung nach Ende, Klammer,
  Limit mit Nachspielzeit). Nicht eigens getestet: zwei *parallele* `end`-Aufrufe (Muster aus dem Bonus-Training).
- Geprüft im Emulator gegen eine DB-Kopie: Startseite zeigt Restzeit und Münzen, der Heartbeat bucht Spielzeit, das
  Level-Ende zeigt das Server-Ergebnis. Ein **gewonnenes** Level mit Münzen ist nur per Test belegt, nicht im Emulator
  durchgespielt.

**Offen bis zur Abnahme:** Friedrich spielt am Handy gegen die echte Instanz (Heimnetz-Neustart spielt die
Migration `TowerGameRuns` ein).

## Verlauf

- 2026-11-22 · `idee` angelegt beim Teilen von B-822 (Entscheidung 8).
- 2026-11-23 · Kern gebaut (zustandsloser Run, `assign`, Quelle `Game`) für den Prototyp B-853; Münzen, Heartbeat, Minutenlimit offen. Status bewusst unverändert.
- 2026-11-23 · Rest gebaut: Run-Tabellen (Migration `TowerGameRuns`), Heartbeat, `end` mit Münzen (10/Level, 30/Tag), Minutenlimit je Kind mit Vater-Web; App verdrahtet. Status unverändert bis zur Abnahme am Handy.
