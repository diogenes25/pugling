---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Mutantenprobe mit Umgebung und unittest]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: ".claude/scripts"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Sprint 59, Sprint 64)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
nachgeschaut: ""
---

# B-523 · `mutant-probe.py` misst weder E2E auf Ausweichports noch Pythons unittest

Zweimal im Tageslauf von Hand gemessen, weil das Skript abbrach: R2 (Sprint 59, `unittest` meldet `OK` statt `passed` –
die Grundlinie galt als „unklar") und U4 (Sprint 64, `PUGLING_E2E_API_PORT=5201 … playwright` – unter Windows startet
`shell=True` die `cmd.exe`, die `NAME=wert befehl` nicht kennt; die Grundlinie galt als „nicht grün"). Eine Hand-Messung
ist genau die Fehlerquelle, gegen die das Skript gebaut wurde.

## Akzeptanzkriterien

1. Eine Vorgabe- oder Mutantenzeile darf `"env": {…}` tragen; build und test laufen mit dieser Umgebung, die Grundlinie
   wird je (build, test, env) gemessen.
2. `unittest`-Ausgaben werden gelesen: `Ran N tests … OK` → grün, `FAILED (failures=N)`/`(errors=N)` → rot.

## Verlauf

- **2026-11-13** — gebaut und abgenommen (Tageslauf, Sprint 66). Beleg am echten Einsatz statt eigener Testdatei (das
  Skript hat keine): dieselben beiden Probes, die vorher nur von Hand gingen, laufen jetzt durch das Skript –
  U4 (E2E mit `env`, Route `/sohn/test/`) → rot, R2 (`laden.py` ohne FEFF-Entfernung, unittest) → rot, Grundlinie nach
  dem Zurückspielen grün. Rot ohne die Änderung: ohne `env` bricht die Grundlinie ab (gemessen in Sprint 64), ohne die
  unittest-Muster gilt sie als „unklar" (gemessen in Sprint 59).
