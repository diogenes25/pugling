---
tags: [typ/story, status/idee, bereich/frontend, bereich/tests, rolle/supervisor]
aliases: [Zeitzonen-Tests greifen nicht]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-427]
---

# B-449 · Die Zeitzonen-Tests von `lib/datum.ts` greifen weder lokal noch in der CI

Gefunden in der Nachschau zu Sprint 27 (Commit `a14961d`, Lauf AG, frischer `frontend-reviewer`), 🟡. Die CI läuft in UTC
(`.github/workflows/ci.yml:45`), lokal läuft Vitest in Europe/Berlin; keine Konfiguration setzt eine Zeitzone.

1. **DateOnly (AK 3 von B-427):** `new Date("2026-11-08")` ist Mitternacht UTC und ergibt in UTC wie in Berlin den 08.11. Die
   Mutante „Textzerlegung weg" (`datum.ts:38`) ist nur unter einer Zone westlich von Greenwich rot – gemessen wurde sie einmal unter
   `TZ=America/New_York`, im Alltag hält AK 3 kein Test.
2. **Zeitstempel ohne Offset:** In UTC ist `formatDate("…T23:30:00") === formatDate("…Z")` auch ohne angehängtes `Z` wahr – die
   Mutante MF5 (`datum.ts:28`) ist in der CI blind. Die zweite Zeile des Tests (`berlin.format(...)`) ruft `datum.ts` gar nicht auf.
3. **Nachkommastellen:** gelesene Zeitstempel kommen mit bis zu 7 Stellen („…T23:30:00.1234567"); kein Fall testet diese häufigste Form.
   Eine Mutante, die den Bruchteil aus `OHNE_OFFSET` nimmt, bliebe grün.

Reparaturvorschlag: `augenblick` exportieren und zonenunabhängig zusichern (`toISOString()` ergibt `…Z`, mit und ohne Bruchteil); für
DateOnly „ruft `new Date` nicht auf" (Spy) oder eine Zeitzone als Parameter der Formatierer; die `Intl`-Zeile streichen.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 27 (Retro Sprint 28, Lauf AG). Increment **dieses** Laufs → **Limit 2**:
  Lauf AG endet nach Sprint 28.
