---
tags: [typ/story, status/abgenommen, bereich/auth, bereich/tests]
aliases: [PinHasher-Zeittest flackert, Timing-Test unter Last]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-438 · `PinHasher_Verify_RechnetAuchOhneGespeichertenHash` fiel einmal unter Last

Im Lauf AE (Sprint 23) fiel in einem vollen `dotnet test Pugling.sln -c Release` genau dieser Test (1 von 1169); der Lauf
dauerte 3 min 9 s statt der üblichen ~1 min – der Rechner war ausgelastet. Einzeln zweimal sofort grün. Der Test vergleicht
die schnellste von fünf Laufzeiten je Pfad mit dem Faktor 0,3 (`SecurityHardeningTests.cs:33-57`, B-282); unter Last kann
der Lockvogel-Pfad offenbar schneller wirken als 30 % des Falsch-PIN-Pfads.

Nicht im Diff des Sprints und dort nicht behoben. Zu prüfen: ob die Messung robuster geht (mehr Läufe, Median statt
Minimum, oder Zählung der PBKDF2-Aufrufe statt Zeit) – ein Test, der in CI unter Last flackert, sieht wie eine
Sicherheitsregression aus.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Verschränkt messen statt hintereinander:** neun Runden, in jeder Runde jeder Pfad einmal, je Pfad die schnellste. Eine
   Lastspitze trifft dann alle Pfade gleich, statt den Block des falschen PINs allein. Faktor 0,3 bleibt. Zählen der
   PBKDF2-Aufrufe wäre schärfer, bräuchte aber einen Zähler im Produktionscode – verworfen.

## Akzeptanzkriterien

1. Ohne die Lockvogel-Ableitung in `PinHasher.Verify` ist der Test rot.

## Verlauf

- **2026-11-08** — angelegt im Lauf AE; ein Fehlschlag gemessen, zwei Einzelläufe grün.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 51).
- **2026-11-13** — gebaut (Sprint 51). AK 1 → `PinHasher_Verify_RechnetAuchOhneGespeichertenHash`; S1 (Lockvogel weg) → rot.
  Ein voller Lauf unter Last (3 min 2 s statt ~1,5 min) blieb grün – eine Messung, kein Beweis gegen das Flackern.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1434/1434; Mutanten-Probe nach der letzten Änderung (Debug,
    gefiltert) 8/8 rot je am genannten Test, Grundlinie grün (Tabelle im Protokoll, Sprint 50/51).
  - Rollengang: reiner Test-Umbau, Suite grün. Abgenommen.
