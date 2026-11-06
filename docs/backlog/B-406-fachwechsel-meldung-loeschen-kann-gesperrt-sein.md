---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Ausweg Löschen kann gesperrt sein]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-aa
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-403, B-405]
---

# B-406 · Der Ausweg „Übung löschen" in der Fachwechsel-Meldung kann selbst gesperrt sein

Der deutsche Satz zu `series_subject_in_use` (B-403, `frontend/src/lib/api.ts`) sagt „eine Übung mit einer Art dieses Fachs kannst du nur
löschen" und „Ganz entfernen lässt sich das Fach nur, solange die Reihe keine Übungen trägt". Eine Übung, die in einem Lehrplan steckt,
lässt sich aber nicht löschen (`409 exercise_in_use`, `ExerciseControllerBase.cs` um `:507`) – der Ausweg führt in die zweite Sperre.

Dazu zwei Beleg-Mängel aus derselben Nachschau:
- Die AK-Tabelle von [B-405](B-405-fach-leeren-meldung-verspricht-wechsel.md) zitiert die Zusicherung `once no exercise or unit carries`;
  seit dem Delta-Review heißt sie `once no unit carries grammar topics and no exercise`.
- `ReiheFachwechselTests` sichert Anzahlen mit `Assert.Contains("1 exercise(s) …")` zu – das passt auch auf „11 exercise(s)". Eine Mutante,
  die über alle Reihen zählt, wird darum nur rot, solange die geteilte Test-DB nicht zufällig eine passende Zahl liefert.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf AA, Sprint 16 (Retro Sprint 17; frischer `frontend-reviewer`, `git show 9f395d2`,
  🟡/🟡/🟢). Increment **dieses** Laufs: **Limit 2**, Lauf AA endet nach Sprint 17. **Gegengerechnet und verworfen:** der Fund, die rote
  Probe in B-403 („expected … to contain 'Grammatik-Thema'") könne nicht stimmen, weil der alte Satz „Grammatik-Themen" enthielt –
  „Grammatik-Them**en**" enthält „Grammatik-Them**a**" nicht; die Ausgabe ist gemessen und steht so im Lauf.
