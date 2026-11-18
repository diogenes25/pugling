---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Nacharbeit 2/3 obwohl fertig]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-637]
---

# B-667 · Der Nacharbeits-Report zeigt Aufgaben außerhalb des Pools als „offen“ und zählt gegen alle Inhalte

## Beobachtung

Gefunden in der Nachschau von [B-637](B-637-nacharbeit-vater-sicht-je-aufgabe.md), am Code bestätigt (`fe13531c`): Seit
dem Review-Fund G2 liefert der Server für Zeilen jenseits des Pools `remediation: null`. `NacharbeitReport`
(`frontend/src/vater/PlanPositions.tsx` ~:1068–1090) zeigt trotzdem jede Zeile – ohne `doneAt` als „offen“ – und rechnet
im Kopf `{erledigt}/{report.totalItems}`.

**Szenario:** Position mit `itemCount = 2` bei 3 Inhalten, beide Aufgaben erledigt. Der Vater liest „Nacharbeit: 2/3
erledigt · geschafft am …“ und eine Zeile „offen“, die nie abgefragt wird. Widerspricht B-637 AK 1.

## Naheliegend

Nur Zeilen mit `remediation` zeigen und gegen deren Anzahl zählen; ein Vitest-Fall mit einer `remediation: null`-Zeile.
