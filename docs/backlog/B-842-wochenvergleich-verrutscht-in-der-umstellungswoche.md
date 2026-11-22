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

# B-842 · Wochenvergleich der Münzen verrutscht in einer Umstellungswoche um eine Stunde

## Ist-Stand am Code

`backend/Pugling.Api/Services/Shared/WalletService.cs:63`: `lastWeekNow = lastWeekStart + (now - weekStart)` rechnet die verstrichene Zeit seit Montag, nicht dieselbe Wanduhrzeit der Vorwoche, wie der Kommentar verspricht. Beispiel: `now` = So 2027-03-28 12:00 MESZ → Vergleichspunkt So 2027-03-21 11:00 MEZ statt 12:00; Münzen zwischen 11 und 12 Uhr fehlen in `lastWeekSoFar` (im Oktober eine Stunde zu viel). Nur Anzeige (`/student/me/points/week`), etwa 21 Stunden zweimal im Jahr. Vorschlag: auf der Wanduhr rechnen (lokal −7 Tage, dann `ConvertTimeToUtc`), Test mit `now` an einem Umstellungssonntag nach 03:00.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
