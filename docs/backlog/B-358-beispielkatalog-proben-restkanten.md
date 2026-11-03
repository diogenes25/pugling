---
tags: [typ/story, status/idee, bereich/backend, bereich/test]
aliases: [Beispielkatalog Restkanten, TempRoot UnauthorizedAccess]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf O in Lauf P, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-358 · Restkanten der Beispielkatalog-Proben (Nachschau über `299d216`)

Drei 🟢 der Nachschau über B-261/B-265/B-274, keiner eine Entgleitung:

1. **`TempRoot.Dispose` fängt nur `IOException`** (`backend/Pugling.Api.Tests/OpenApiExampleCatalogRetryTests.cs:125`). Ein
   Virenscanner kann unter Windows beim `Directory.Delete` `UnauthorizedAccessException` werfen; der Test fiele dann im
   Aufräumen. Weg: `catch (Exception e) when (e is IOException or UnauthorizedAccessException)`.
2. **Unter Linux nennt die rote Meldung nur das aktuelle Fenster** (`OpenApiExampleCatalogConcurrencyTests.cs:282`, die
   Zerrissen-Regel je Fenster). B-265 AK 2 („eine rote Probe nennt alle zehn Fenster") ist damit weiter gefasst, als der Code
   hält – die AK auf die Summen-Zusicherung einengen oder die Meldung um die bisherigen Fenster ergänzen.
3. **Der Vorgabewert der Pause trägt keine Zusicherung** (Mutante „`pause` ignoriert, immer 50 ms" bliebe grün; B-261
   Entscheidung 1 nimmt das an). Dazu eine Zeile von ~127 Zeichen (`ConcurrencyTests.cs:293`).

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf O (`pugling-reviewer` über `299d216`, 🟢 ×3). Aus Code gelesen.
