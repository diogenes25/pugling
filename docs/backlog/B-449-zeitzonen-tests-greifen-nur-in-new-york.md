---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/tests, rolle/supervisor]
aliases: [Zeitzonen-Tests greifen nicht]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-427]
nachgeschaut: 2026-11-13
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

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Die Zeitzone wird Parameter, nicht Umgebung:** `formatDateIn(value, timeZone?)` trägt die Logik, `formatDate` ruft es
   ohne Zone (Gerät). Der DateOnly-Test fragt unter `America/New_York` – die Mutante „Textzerlegung weg" ist dann auf jeder
   Maschine rot, nicht nur westlich von Greenwich.
2. **`augenblick` wird exportiert** und zonenunabhängig über `toISOString()` zugesichert: ohne Offset, mit Offset und mit
   sieben Nachkommastellen (die häufigste Form vom Server). Die `Intl`-Zeile, die `datum.ts` gar nicht aufrief, entfällt.

## Akzeptanzkriterien

1. „Textzerlegung weg" macht den DateOnly-Test in UTC und in Berlin rot.
2. „kein `Z` anhängen" macht den Zeitstempel-Test in UTC rot.
3. Ein Muster, das den Bruchteil nicht als „ohne Offset" erkennt, macht einen Test rot.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 27 (Retro Sprint 28, Lauf AG). Increment **dieses** Laufs → **Limit 2**:
  Lauf AG endet nach Sprint 28.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 54).
- **2026-11-13** — gebaut (Sprint 54). `formatDateIn` (Zone als Parameter), `augenblick` exportiert, eine gemeinsame
  Lese-Hälfte `lies` für Vater-Web und Arcade (nach Review: 🟡 zwei Tests waren weiter nur in bestimmten Zonen rot –
  behoben durch die gemeinsame Lese-Hälfte und den Mittag UTC; Zähler 1).
  - AK 1 → „zeigt einen DateOnly als deutschen Kalendertag …" (New York); D1 (Mitternacht statt Mittag) → rot.
  - AK 2/3 → „liest einen Zeitstempel ohne Offset als UTC" (`toISOString`, sieben Nachkommastellen); D2, D3 → rot.
  - Verifikation: Vitest 820/820, `tsc -b` sauber; Mutanten D1–D6 und E1 nach der letzten Änderung (Tabelle im Protokoll,
    Sprint 54): D1, D2, D3, D5, D6, E1 rot; **D1b und D4 nicht rot – begründet**: ein `DateOnly` wird als Mittag UTC gelesen
    *und* in der Arcade in UTC formatiert; fällt eine Hälfte weg, trägt die andere für alle Zonen bis UTC+11 (D4 wirkt nur
    jenseits davon, ohne Zonenparameter für DateOnly nicht messbar). Playwright 140/140 auf 5201/5174.
  - Rollengang Sohn/Vater: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 56/57, frischer Reviewer, gelesen; Ratschläge bzw. Datumswerte je Zweig/Zone nachgerechnet). Befund: 🔴 Zeitstempel-Tests in UTC blind; 🟡 Folgetag östlich UTC+11 → [B-517](B-517-datum-tests-in-utc-und-folgetag.md).
