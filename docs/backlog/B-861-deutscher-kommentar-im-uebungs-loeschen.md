---
tags: [typ/story, status/abgenommen, bereich/katalog]
aliases: []
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau von B-700 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, pugling-reviewer 🟢)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: []
wartet_auf: ""
---

# B-861 · Ein deutscher Block-Kommentar im Löschen einer Übung

`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:602-607` (`Delete`) trägt noch einen deutschen
`/* … */`-Block, älter als B-700, direkt neben dessen englischem Kommentar. Die Regel „Code-Doku auf Englisch“ gilt für
jeden Kommentar; die Tore prüfen offenbar nur `//` und `///`, nicht `/* */` – das wäre beim Ausformulieren zu prüfen.

## Ist-Stand am Code

Gemessen am 2026-11-23 (`grep` über alle fünf Backend-Projekte nach `/* */`-Zeilen mit Umlauten oder zwei deutschen
Funktionswörtern): **sechs** deutsche Blöcke, nicht einer – `ExerciseControllerBase.cs:602`, `PlanPositionsController.cs:66`,
`TextbooksController.cs:104`, `PuglingDbContext.cs:578`, `PositionTestFlowTests.cs:131`, `PlanPositionCrudTests.cs:231`.
Kein Tor prüft die Sprache von Kommentaren; die Übersetzung von 2026-08-01 hat `//` und `///` erfasst, `/* */` nicht.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Aufräumen, Freigabe 1), unbestätigt.*

1. **Alle sechs übersetzen**, Umbau-Erzählung („stand einmal im Seed“, „beim Umbau Father→Adult“) dabei weglassen –
   die Konvention will das Warum, nicht die Geschichte. Kosten: keine Verhaltensänderung.
2. **Kein Tor in diesem Sprint** – ein neues Tor hält den Lauf an; als Retro-Vorschlag notiert.

## Akzeptanzkriterien

1. Dieselbe Suche findet danach keinen deutschen Block-Kommentar mehr (die Treffer mit deutschen Beispielwörtern in
   englischem Text bleiben, `PositionPracticeFlowTests.cs:57`, `PositionTimeSlotTests.cs:271`).
2. Suite grün.

## Schätzung

XS, backend, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-11-23** — angelegt aus der Nachschau von B-700 (Nachtlauf 2026-11-23-c).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Aufräumen), geschätzt und gebaut im Nachtlauf 2026-11-23-c
  (Sprint 120). AK 1 gemessen: vorher 6 Blöcke, danach 0 (zwei englische Treffer mit deutschen Beispielwörtern). Kein
  Mutant – es ändert sich kein Verhalten; der Beleg ist die Suche selbst.
  Review (`pugling-reviewer`): alle sechs Übersetzungen behalten Warum und Regel; 🟡 deutsche `//`/`///`-Absätze in
  `Pugling.Api.Tests` außerhalb des Diffs → [B-863](B-863-deutsche-kommentare-in-den-tests.md) (ohne Zählung). AK 1 gilt nur
  für `/* */`.
- **2026-11-23** — abgenommen (Sprint 120, Commit HASH). Verifikation nach der letzten Änderung: Backend 2134/2134
  (Release), Vitest 1371/1371, `tsc -b` sauber; Rollengang Sohn per E2E gegen frisch gestartete Server (5211/5184):
  `bonus`, `bonus-deckel`, `bonus-deckel-voll`, `bonus-satz-je-karte`, `nochmal-nach-pflicht`, `full-flow` – 6 passed.
  Der Fall „Deckel 0 auf der Startseite“ ist nur auf Komponentenebene gegangen (keine Spec setzt den Deckel auf 0 und
  öffnet danach die Startseite). Vater Regressionszeuge (`bonus-deckel`), Creator Regressionszeuge (Suite grün).
