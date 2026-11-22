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

# B-846 · Der Kopf von `nochmal-nach-pflicht.spec.ts` verspricht ein Rot, das zwei Mutanten nicht liefern

## Ist-Stand am Code

`frontend/e2e/nochmal-nach-pflicht.spec.ts:4-8` sagt „rot, wenn der Knopf in eine andere Übung führt“; gegen `:85-88` bleiben zwei Mutanten grün: `positions[0]` statt der Zuordnung (der Plan hat nur eine Position) und „erste Übung im Pool“ (der Server sortiert nach `OrderIndex`, die Vokabeln haben 1, die Rechen-Übung 2). Getragen wird die Zuordnung von `NochEinmal.test.tsx:31/51-58`. Vorschlag: Rechen-Übung mit `orderIndex: 0` und zweite Position, oder den Satz auf „Block fehlt“ beschränken.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
