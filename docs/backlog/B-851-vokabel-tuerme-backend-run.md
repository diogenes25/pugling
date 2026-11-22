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

## Kern gebaut (2026-11-23, für den Prototyp B-853)

Gebaut ist der **schlanke Kern**, den der Prototyp braucht – Entscheidungen stellvertretend nach eigener
Empfehlung getroffen (Auftrag des Nutzers), zum Umstoßen:

- **Zustandsloser Run statt `TowerGameRun`/`TowerGameRunItem`:** Turm- und Token-Ids sind per ASP.NET Data
  Protection verschlüsselte Referenzen (Kind, Position, Item, Rolle, Nonce). Der Payload verrät keine Zuordnung,
  eine Id gilt nur für dieses Kind und diese Position. *Grund:* keine Migration an der echten DB für einen
  Prototyp; *Preis:* kein eingefrorener Stoff über einen App-Neustart, keine serverseitige Run-Zählung.
- Endpunkte `GET tower-game`, `POST tower-game/runs {planPositionId}`, `POST tower-game/assign`
  ([TowerGameController.cs](../../backend/Pugling.Api/Controllers/Student/TowerGameController.cs)); Run = bis zu
  12 Wörter einer Position, schon gesehene zuerst (schwächste vorn), Ziel 10, 5 Turm-Slots.
- Lernstand mit neuer Quelle `ItemReviewSource.Game` (String-Enum, keine Migration); Box steigt nur beim ersten
  Treffer je Wort und Tag (E3), ein Fehler setzt zurück. Gleiches Wort oder gleiche Bedeutung zählt als richtig.
- Tests: [TowerGameTests.cs](../../backend/Pugling.Api.Tests/TowerGameTests.cs) (4 Fälle).

**Noch offen** aus dieser Story: `end` mit Münzen (10/Level, Deckel 30/Tag, eigener `PointKind`), Heartbeat,
Minutenlimit je Kind (Feld + Migration + Vater-Web), echte Run-Tabellen mit eingefrorenem Stoff.

## Verlauf

- 2026-11-22 · `idee` angelegt beim Teilen von B-822 (Entscheidung 8).
- 2026-11-23 · Kern gebaut (zustandsloser Run, `assign`, Quelle `Game`) für den Prototyp B-853; Münzen, Heartbeat, Minutenlimit offen. Status bewusst unverändert.
