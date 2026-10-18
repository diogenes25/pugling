---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Fokus bei gesperrtem Knopf ohne Meldung, Mehr laden ohne Fehler]
status: idee
prio: P3
art: Frage
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: B-228, Entscheidung 2 (Nachtlauf 2026-10-17, zweiter Lauf)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-231 · Wohin gehört der Fokus, wenn ein Knopf ohne Fehlerkasten gesperrt wird?

Ein Knopf, der nach dem Klick `disabled` wird, verliert in Chromium den Fokus auf `<body>` (B-216, im E2E
gemessen). B-216 und B-228 legen ihn dort, wo eine Meldung steht, auf den Fehlerkasten. Für den Normalfall
gibt es keinen Kasten: „Mehr laden" ohne Fehler (`frontend/src/sohn/SohnShop.tsx`, gefüllte Verlaufskarte)
lässt den Fokus weiter fallen – und dasselbe gilt für jeden `disabled={busy}`-Knopf der Hausregel
(`frontend/CLAUDE.md`), sobald er per Tastatur gedrückt wird. Nach dem Laden steht der Fokus am
Dokumentanfang, und der Sohn verliert in einer langen Liste seinen Platz.

Kandidaten: der umgebende Container (`.list`, `tabIndex={-1}`, mit Fokusring), oder `aria-disabled` statt
`disabled` am Knopf (behält den Fokus, bricht aber die Hausregel und Playwrights Actionability-Wartepunkt).
Beides ist eine Gestaltungsentscheidung über alle Knöpfe der Arcade, keine Reparatur eines einzelnen – darum
eine `Frage`, nicht im Nachtlauf entschieden (Freigabe 1).

## Verlauf

- **2026-10-17** — angelegt aus B-228, Entscheidung 2: der Fehlerfall ist dort behoben, der Fall ohne Meldung
  bewusst ausgespart.
