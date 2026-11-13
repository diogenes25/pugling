---
tags: [typ/story, status/ausformuliert, bereich/backend]
aliases: [Tor null-Element je Body-Liste]
status: ausformuliert
prio: P2
art: Aufräumen
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-y
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-410 · Reflexiver Wächter: jede Liste in jedem `[FromBody]`-Graphen meldet ein `null`-Element als 400

Retro-Vorschlag (BR), entschieden am 2026-11-07 als Tor-Story. `NullElementGuardFilter`
(`backend/Pugling.Api/Errors/NullElementGuardFilter.cs`) soll seit B-397 jedes `null`-Element einer nicht-nullbaren Liste
mit `400 validation_error` beantworten. Die Regressionsfälle in `UnknownReferenceCodeTests.cs` decken aber nur die Formen ab,
die beim Bauen eingefallen sind. Die Listen im Body selbst kamen erst durch das Review dazu (Lauf Y, Sprint 12). „Jede Liste"
hängt also an einer Vorstellung, nicht an einer Aufzählung.

Ein reflexiver Test soll alle Actions mit `[FromBody]` aufzählen, im Typgraphen jede Liste finden (auch den Body selbst),
dort ein `null`-Element einsetzen und `400` mit dem Pfad in `errors` erwarten. Der Filter dokumentiert eine Ausnahme
(`List<List<T>>` wird nicht durchsucht, „no contract has one"). Der Wächter soll genau das ebenfalls festhalten.

**Erst messen** (`CLAUDE.md` → „Neue Regel scharf stellen? Erst messen"): Beim Ausformulieren zuerst zählen, wie viele
Listen der Wächter heute findet und ob eine davon rot wäre. Erst danach entscheiden, ob er scharf gestellt wird oder eine
Ausnahmeliste mit Grund bekommt. Ein neues Tor beendet den Lauf (`docs/backlog/README.md`, Limit 2).

## Ist-Stand am Code (grob gezählt 2026-11-13, keine Messung im Sinne von „erst messen")

- `NullElementGuardFilter.cs:19` dokumentiert die eine Ausnahme: `List<List<T>>` wird nicht durchsucht („no contract has one").
- `UnknownReferenceCodeTests.cs` trägt 12 Fälle (`[Fact]`/`[Theory]`) – die Formen, die beim Bauen und im Review auffielen.
- Nur **5** Actions tragen ein ausdrückliches `[FromBody]` (`grep` über `Controllers/`). Das ist **nicht** die Zahl der
  Body-Actions: `[ApiController]` leitet komplexe Parameter ohne Attribut als Body ab. Der Wächter muss darum über den
  `ApiExplorer` (`BindingSource.Body`) aufzählen, nicht über das Attribut – sonst sähe er einen Bruchteil.
- Grob ~61 Listen-Eigenschaften in `*Dtos.cs`/`*Configs.cs` des Vertrags (Regex über Typnamen, zählt auch Response-DTOs mit).

## Offene Punkte

1. Messen (Ausformulieren → Grillen): ein Wegwerf-Test zählt über `IApiDescriptionGroupCollectionProvider` alle
   Body-Parameter, läuft ihren Typgraphen ab und schickt je Liste ein `null`-Element – wie viele sind heute rot?
2. Erst danach: scharf stellen oder Ausnahmeliste mit Grund. Ein neues Tor beendet einen autonomen Lauf (Limit 2).

## Verlauf

- **2026-11-07** — angelegt aus Retro-Vorschlag (BR); Entscheidung des Nutzers: „Ja, wie empfohlen" (Tore als Stories, nicht direkt landen).
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand grob belegt; Falle „`[FromBody]` zählt nur 5" festgehalten; Messung als erster Schritt des Grillens.
