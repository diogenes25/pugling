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

# B-843 · Das Tor gegen UTC-Tagesbildung fängt weniger Formen, als B-730 AK 2 verspricht

## Ist-Stand am Code

`backend/Pugling.Api.Tests/ConventionGuardTests.cs:686-694` (`Tage_Entstehen_Nur_Im_Lerntag`): durch das Muster gehen `.Date` auf einer Variablen mit UTC-Zeitpunkt (Beleg, dass die Form vorkommt: `ShopRefillSchedule.cs:60/86`, dort zufällig Wanduhr), `new DateOnly(t.Year, t.Month, t.Day)`, `DateOnly.FromDayNumber`, `.DayOfWeek` auf Zeitpunkten, Datumsformat in interpolierten Strings (siehe B-844), `.ToDateTime(default)` und alles außerhalb von `Controllers/` und `Services/` (z. B. `Exercises/`). Heute versteckt sich dahinter kein Verstoß; das Tor schützt nur weniger, als es behauptet.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
