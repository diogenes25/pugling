---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/supervisor]
aliases: [Assistent Dosis Liste, wizardFinish Dosis]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 29/30)
unverifiziert: true
entgangen_bei: [B-04]
grund: ""
ersetzt_durch: []
---

# B-477 · Der Assistent schickt die Tagesdosis auch an ungeordnete Listen – das Anlegen bricht mittendrin ab

Gefunden in der Nachschau von Sprint 29 (B-04), gelesen, nicht im Browser nachgestellt: Der Plan-Assistent setzt
`useLeitner = true` und `newItemsPerDay = 10` als Vorgabe und schickt beides an **jede** gewählte Übung
(`VaterWizard.tsx:373`, Schleife in `wizardFinish.ts`). Für eine Listen-Übung mit `ordered: false` lehnt der Server
die Dosis seit B-04 (Entscheidung 9) mit 400 ab (`DoseOnSetProblem`). `runWizardFinish` bricht dann mit „… Der Plan
ist angelegt – nimm die Übung ab und versuche es erneut" ab: der Plan steht halb, und die Meldung nennt nicht den
eigentlichen Ausweg (Dosis 0). Der Test `Dosis_AufUngeordneterListe` prüft nur die nackte API.

Naheliegend: im Assistenten für Übungen, die als Menge gewertet werden, `newItemsPerDay: 0` schicken (Typ- und
Config-Prüfung wie die vorhandene `typeOf`-Prüfung für `stage`).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 29/30 im Backlog-Lauf, `pugling-reviewer`).
