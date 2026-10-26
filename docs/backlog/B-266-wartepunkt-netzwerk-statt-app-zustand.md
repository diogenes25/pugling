---
tags: [typ/story, status/idee, bereich/frontend, bereich/test]
aliases: [waitForResponse statt App-Zustand, AK 3 von B-169 hängt weiter am Timing]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau Sprint 11 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-180]
wartet_auf: ""
---

# B-266 · Der Wartepunkt aus B-180 wartet aufs Netzwerk, nicht auf die App

B-180 sollte eine Zusicherung vom Scheduling lösen. `frontend/e2e/assistent.spec.ts:183-186` wartet jetzt mit
`page.waitForResponse` – das löst aus, sobald die Antwort ankommt, nicht wenn die App sie verarbeitet hat (Body
lesen, `setState`, Commit liegen dazwischen). Die erste Auswertung von `toBeDisabled()` (`:191`) kann darum weiter
in das Fenster fallen, in dem `laedt` den Knopf allein sperrt (`VaterWizard.tsx:723`:
`disabled={selectAllBusy || laedt || zeilenVeraltet}`); ohne `zeilenVeraltet` bliebe der Fall dann grün. Nach dem
Fix lief keine rote Probe, nur grüne Läufe.

`unverifiziert: true`: nur strukturell belegt, der Nachschau-Prüfer durfte kein Playwright fahren. Beweisweg:
`zeilenVeraltet` aus `VaterWizard.tsx:723` entfernen, den Fall mit `--repeat-each 20` fahren – jeder grüne Lauf
belegt den Defekt. Vorschlag: vorher auf einen App-Zustand warten (z. B. „Suche gescheitert“, `VaterWizard.tsx:714`),
gegen `useAsync` gegenprüfen. **Nicht in diesem Lauf behoben:** außerhalb des Sprint-11-Diffs.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-180 (sechster Nachtlauf, Sprint 11); strukturell belegt.
