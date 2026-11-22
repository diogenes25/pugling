---
tags: [typ/story, status/idee, bereich/frontend]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: offen
vertragsbruch: offen
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-660)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-845 · „Noch einmal“ verspricht Münzen, auch wenn der Vater den Bonus-Deckel auf 0 gesetzt hat

## Ist-Stand am Code

`frontend/src/sohn/NochEinmal.tsx:37-45` (Texte `againLocked*` in `de.ts`/`en.ts`): Der gesperrte Zweig sagt „danach kannst du Übungen im Bonus-Training für Münzen wiederholen“ auch bei `Child.BonusDailyCoinCap = 0`; es gibt dann nie Münzen. Der freigeschaltete Zweig behandelt den Fall richtig (`againNoCoins`). Vorschlag: im gesperrten Zweig `dailyCoinCap === 0` abfragen und einen Satz ohne „für Münzen“ zeigen (wie `capZero` in `SohnBonus.tsx`).

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
