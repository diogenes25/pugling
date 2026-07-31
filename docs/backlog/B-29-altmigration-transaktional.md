---
tags: [typ/story, status/idee, bereich/doku]
aliases: [Nicht-transaktionale Altmigration]
status: idee
prio: P3
art: Frage
quelle: memory/offene-maengel-backlog.md
unverifiziert: true
---

# B-29 · Prüfauftrag: nicht-transaktionale Altmigration

Aus dem Wartbarkeits-Review blieb eine Migration übrig, die nicht in einer Transaktion lief — bricht sie
mittendrin, bleibt ein halber Zustand.

**Vermutlich gegenstandslos:** Der DB-Umbau hat die Kette auf **eine** Migration gefaltet
(`InitialCreate`), Altdaten gibt es nicht mehr. Erst prüfen, dann wahrscheinlich `verworfen` — das ist
der Erfolg dieser Story.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
