---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Fokus im Abschlusstest nach Aufdecken]
status: idee
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-531, Entscheidung 3 und Bau-Befund (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-541 · Im Abschlusstest fällt der Fokus nach dem Aufdecken auf `body`

[B-531](B-531-fokus-faellt-nach-aufdecken-auf-body.md) hat den Fokus beim Kartenwechsel im Üben
(`SohnPractice.tsx`) repariert und den Test-Bildschirm bewusst ausgenommen (Entscheidung 3). Beim Bauen fiel auf:
`SohnTest.tsx` hat gar keinen Fokus-Effekt, `onReveal` setzt dort nur `revealed` (`SohnTest.tsx:241`). Nach
„Aufdecken“ und nach dem Urteil verschwindet der fokussierte Knopf also sehr wahrscheinlich und der Fokus fällt auf
`body` – wer mit Tastatur oder Screenreader prüft, verliert den Ort. Gleiche Regel wie B-531: jeder Kartenwechsel
durch eine Handlung des Kindes fokussiert das erste Bedienbare der neuen Antwortzone.

**Zweiter Fall derselben Klasse** (frontend-reviewer zu B-531/B-532): Beendet `plan_inactive` die Runde mitten im
Üben, greift `landingRef` nicht (`SohnPractice.tsx:180`, nur bei `focusAfterLoad`), und der Fokus fällt ebenfalls
auf `body`. Die `role="alert"`-Meldung wird angesagt, der Ort aber geht verloren. Der Fall gehört hierher, nicht in
eine eigene Story: dieselbe Regel, derselbe Testweg.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus dem Bau von B-531.
- **2026-11-13** — um den `plan_inactive`-Fall aus dem Review von B-531/B-532 erweitert.
