---
tags: [typ/story, status/idee, bereich/auth, bereich/tests]
aliases: [PinHasher-Zeittest flackert, Timing-Test unter Last]
status: idee
prio: P3
art: Defekt
groesse: ""
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

## Verlauf

- **2026-11-08** — angelegt im Lauf AE; ein Fehlschlag gemessen, zwei Einzelläufe grün.
