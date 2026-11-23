---
tags: [typ/story, status/geschaetzt, bereich/backend]
aliases: []
status: geschaetzt
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-730)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-730]
wartet_auf: ""
---

# B-844 · Der Titel der Schwachwort-Wiederholung trägt das UTC-Datum

## Ist-Stand am Code

`backend/Pugling.Api/Services/Supervisor/WeakWordReviewService.cs:111`: `Title = $"Wiederholung: schwache Wörter ({childName}, {now:dd.MM.yyyy})"` mit `now` in UTC. Legt der Vater die Wiederholung am 23.11. um 00:30 an (22.11. 23:30Z), steht „22.11.2026“ im Titel, während `DutySince` (Zeile 136, `learningDay.DayOf(now)`) richtig der 23.11. ist. Älter als B-730, aber dessen Versprechen „jede Tagesbildung“ hätte sie erfasst; das Tor sieht sie nicht (B-843). Vorschlag: `{learningDay.DayOf(now):dd.MM.yyyy}`.

## User Story

Als Vater möchte ich, dass die Schwachwort-Wiederholung kurz nach Mitternacht das Datum des neuen Tages im Titel trägt,
damit Titel und Pflichtbeginn nicht verschiedene Tage nennen.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **`{learningDay.DayOf(now):dd.MM.yyyy}`** wie vorgeschlagen; bestehende Titel bleiben (Produktinhalt, kein Nachzug).

## Akzeptanzkriterien

1. Vater legt die Wiederholung am 2027-01-15 23:30Z (00:30 MEZ am 16.) an → Titel endet auf „16.01.2027)“. Rot mit `now`.

## Schätzung

XS, backend, keine Migration, kein Vertragsbruch. Testweg: Integrationstest in `TagesbegriffTests` (Berlin-Fabrik).

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 117). Rote Probe: `Schwachwort_Wiederholung_Traegt_Den_Lerntag_Im_Titel` erwartet rot,
  gemessen rot (1/1, Titel endete auf „15.01.2027)“).
  - AK 1 → `TagesbegriffTests.Schwachwort_Wiederholung_Traegt_Den_Lerntag_Im_Titel`, M3 (`{now:dd.MM.yyyy}`) → rot.
