---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: [Abschlusstest zeigt den Tipp, Hinweis im Test]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: ja
quelle: "B-223 beim Ausformulieren (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-544 · Der Abschlusstest liefert und zeigt den Tipp

## User Story

Als **Vater** möchte ich, dass der Abschlusstest ohne Hilfe misst, **damit** ein bestandener Test heißt, dass mein
Sohn das Wort kann – nicht, dass er den Tipp lesen konnte.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, nach Sprint 69):

- Der Server legt den Tipp in jede Testkarte: `new TestItem(…, f.Hint, …)`
  (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:72`), Vertrag
  `TestItem(…, string? Hint, …)` (`backend/Pugling.Contracts/Student/TestDtos.cs:34`).
- Die Arcade zeigt ihn auf getippten Stufen an: `hinweis={item.hint && typed && … 💡 {item.hint}}`
  (`frontend/src/sohn/SohnTest.tsx:215`).
- Entschieden ist das Gegenteil: **„im Abschlusstest gibt es keine Hinweise – er misst“** (Rest-Entscheidung im
  Auftrag des Nutzers vom 2026-11-05, festgehalten in [B-223](B-223-gestufte-hinweise-vor-der-antwort.md), Verlauf und
  Offener Punkt 5). Das Üben behält seinen Tipp (`PracticeCard`), davon handelt diese Story nicht.

## Die echte Lücke

Die Entscheidung vom 2026-11-05 ist nie gebaut worden. Ein reines Ausblenden in der Arcade reichte nicht: der Tipp
reiste weiter in der Antwort und stünde jedem offen, der die Anfrage liest – Anti-Cheat gehört an den Server.

## Entscheidungen

Die Produktfrage ist am 2026-11-05 entschieden; hier nur, wie (Stakeholder-Entscheidung im Auftrag des Nutzers,
Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)):

1. **Das Feld `Hint` fällt aus `TestItem`** (Vertragsbruch, in der Testphase erlaubt). *Begründung:* ein Feld, das
   immer `null` ist, lädt den nächsten Client ein, es doch zu füllen; der Vertrag soll sagen, was der Test ist.
   *Kosten:* `contract.ts` neu erzeugen, `SohnTest.tsx` und betroffene Tests anpassen.
2. **Die Arcade zeigt im Test keinen Tipp mehr**, auch nicht aus dem Üben zwischengespeichert. *Kosten:* keine.

## Akzeptanzkriterien

1. Eine Testkarte (`POST …/positions/{id}/tests`, Fragen) trägt kein Feld `hint`, auch wenn die Übung einen Tipp hat.
   *Rot macht ihn:* `f.Hint` zurück in den Konstruktor (Feld wieder im Vertrag).
2. Die Übungskarte (`PracticeCard`) trägt den Tipp weiterhin. *Rot macht ihn:* den Tipp auch im Üben entfernen.
3. `SohnTest` rendert auf einer getippten Stufe keinen 💡-Hinweis. *Rot macht ihn:* die Zeile `:215` zurück.

## Risiken

- `UnmappedMemberHandling.Disallow` gilt für Anfragen, nicht Antworten – ein alter Client bricht nicht.
- `DocsCaptureTests` schreibt `docs/api-examples` und `v1.json` neu; die Änderung ist gewollt.

## Angriffsplan

1. Backend: `Hint` aus `TestItem` und dem Konstruktor; Integrationstest (AK 1, AK 2) in einer bestehenden
   Testklasse zum Abschlusstest (`grep -l "tests" backend/Pugling.Api.Tests` – z. B. neben `SchulVokabeltestTests.cs`).
2. Frontend: `npm run gen:contract`, `SohnTest.tsx:215` entfernen, RTL-Fall in `SohnTest.test.tsx` (AK 3).

## Testweg

Integrationstest in `backend/Pugling.Api.Tests` (AK 1/2), RTL in `frontend/src/sohn/SohnTest.test.tsx` (AK 3),
Mutanten-Probe je AK, volle Suite; Rollengang über die E2E-Specs des Tests (`test-durchgefallen-ueben`, `full-flow`).

## Verlauf

- **2026-11-13** — angelegt aus dem Ausformulieren von B-223 und im selben Zug bis `geschaetzt` gebracht: die
  Produktentscheidung liegt seit 2026-11-05 vor, die Lücke ist am Code belegt (`PositionTestsController.cs:72`,
  `SohnTest.tsx:215`).
- **2026-11-13** — `in-arbeit` (Sprint 72): `Hint` aus `TestItem` und `PositionTestsController.ToItem` entfernt (Vertragsbruch, gewollt), `contract.ts` neu erzeugt; `SohnTest` rendert keinen 💡-Tipp mehr. Tests: `PositionTestFlowTests.Abschlusstest_Liefert_Keinen_Tipp_Das_Ueben_Schon` (AK 1, AK 2), `SohnTest.test.tsx` „zeigt auf einer getippten Stufe keinen Tipp" (AK 3). Rote Probe:

  Gemessen an `36ebffe5 + Arbeitsbaum geändert`, 2026-11-13 23:22.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Contracts/Student/TestDtos.cs:41 | red | red | Feld `Hint` zurück im Vertrag (AK 1) |
  | M2 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:328 | red | red | Tipp auch im Üben weg (AK 2) |
  | M3 | frontend/src/sohn/SohnTest.tsx:240 | red | red | 💡-Zeile zurück (AK 3) |

  Grundlinie nach dem Zurückspielen: green.
- **2026-11-13** — **abgenommen** mit Commit `ff6a08fb + fbd78e04` (Sprint 72, Backlog-Lauf 2026-11-13). Verifikation: Backend 1599/1599 grün (dotnet test Pugling.sln -c Release), Vitest 998/998, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: Code-Review Ã¼ber den ganzen Lauf (`pugling-reviewer`, `frontend-reviewer`) ohne Befund an dieser Story. Rollengang: volle Playwright-Suite gegen frisch gestartete Server auf Ausweichports (API 5216, Web 5189) nach dem letzten Edit: **145 passed** (alle Specs). Den Tab „Erfolge“ (Woche, Frist) fährt keine Spec eigens; er ist über RTL-Tests belegt – ein Handblick am Gerät bleibt für den Menschen.
