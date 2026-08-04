---
tags: [typ/story, status/verworfen, bereich/doku]
aliases: [Nicht-transaktionale Altmigration]
status: verworfen
prio: P3
art: Frage
quelle: memory/offene-maengel-backlog.md
grund: "gegenstandslos seit der Neufaltung auf InitialCreate — die Migrationskette besteht (SchemaGuardTests, Tor G1b) aus genau einer Migration (Data/Migrations/20260803223259_InitialCreate.cs), keine Altmigration ist mehr vorhanden, die nicht-transaktional laufen könnte. Program.cs (Zeilen 471–484) behandelt eine DB der alten Kette ohnehin nicht als Migrationsfall, sondern wirft eine handlungsfähige Fehlermeldung — ein Upgrade-Pfad existiert bewusst nicht."
---

# B-29 · Prüfauftrag: nicht-transaktionale Altmigration

Aus dem Wartbarkeits-Review blieb eine Migration übrig, die nicht in einer Transaktion lief — bricht sie
mittendrin, bleibt ein halber Zustand.

**Vermutlich gegenstandslos:** Der DB-Umbau hat die Kette auf **eine** Migration gefaltet
(`InitialCreate`), Altdaten gibt es nicht mehr. Erst prüfen, dann wahrscheinlich `verworfen` — das ist
der Erfolg dieser Story.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — geprüft und verworfen: gegenstandslos seit der Neufaltung auf `InitialCreate` — nur
  noch eine Migration (`Data/Migrations/20260803223259_InitialCreate.cs`), keine Altmigration mehr
  vorhanden; Kettenlänge 1 hält `SchemaGuardTests` mechanisch (Tor G1b), und `Program.cs` weist eine DB
  der alten Kette explizit ab statt sie zu migrieren (autonom geprüft, Nutzerauftrag 2026-08-04).
