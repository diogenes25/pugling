---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [createChild Restkanten, Kind per Index]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf N, Teil 2 in Lauf O (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-357 · Restkanten des E2E-Helfers `createChild` und eine Kind-Auswahl per Index

Drei 🟢 der Nachschau über `cc19020` (B-255), keiner eine Entgleitung:

1. **Fehlt `id` in der Antwort**, wird aus `String(undefined)` die Id `"undefined"`, und der Fehler erscheint als
   Zeilen-Wartezeit statt mit Ursache (`frontend/e2e/helpers.ts:47`). Weg: `expect(typeof body.id).toBe("number")`.
2. **Das POST-Warten ist auf 10 s begrenzt** (`helpers.ts:42-43`), ohne Rand über dem Expect-Timeout; auf einem kalten
   CI-Läufer fiele ein langsames Anlegen als `TimeoutError` ohne Status. Erst ändern, wenn es auftritt.
3. **`responsive-vater.spec.ts:94-97`** nimmt die Kind-Id als `kids[0].id`, wenn Vater 1 keinen Plan hat – Auswahl per
   Index, gegen die Repo-Regel. Der Zweig wird mit dem Seed vermutlich nie erreicht.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf N, Teil 2 (`frontend-reviewer`, 🟢 ×3). Aus Code gelesen.
