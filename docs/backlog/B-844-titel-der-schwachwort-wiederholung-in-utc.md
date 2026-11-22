---
tags: [typ/story, status/idee, bereich/backend]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: offen
vertragsbruch: offen
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-730)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-730]
wartet_auf: ""
---

# B-844 · Der Titel der Schwachwort-Wiederholung trägt das UTC-Datum

## Ist-Stand am Code

`backend/Pugling.Api/Services/Supervisor/WeakWordReviewService.cs:111`: `Title = $"Wiederholung: schwache Wörter ({childName}, {now:dd.MM.yyyy})"` mit `now` in UTC. Legt der Vater die Wiederholung am 23.11. um 00:30 an (22.11. 23:30Z), steht „22.11.2026“ im Titel, während `DutySince` (Zeile 136, `learningDay.DayOf(now)`) richtig der 23.11. ist. Älter als B-730, aber dessen Versprechen „jede Tagesbildung“ hätte sie erfasst; das Tor sieht sie nicht (B-843). Vorschlag: `{learningDay.DayOf(now):dd.MM.yyyy}`.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
