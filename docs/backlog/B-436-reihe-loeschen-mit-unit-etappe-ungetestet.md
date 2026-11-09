---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Reihe löschen mit Unit-Etappe, unit_content_in_use Reihen-Pfad]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-nacht.md
grund: ""
ersetzt_durch: []
entgangen_bei: [B-413]
---

# B-436 · Eine Reihe zu löschen, die nur eine Ziel-Etappe auf einer ihrer Units sperrt, prüft kein Test

Gefunden in der Nachschau zu Sprint 21 (Commit `4c25504`, Lauf AE). B-413 hat für das Löschen einer Unit **oder Reihe**
den Code `unit_content_in_use` eingeführt; die Meldung (`TextbookSeriesController.cs:326-327`), die Swagger-Summary
(`:281-282`), der deutsche Text in `frontend/src/lib/api.ts` und `docs/tutorial-supervisor.md:341` sagen alle, dass auch
eine Ziel-Etappe auf einer Unit der Reihe sperrt. Getestet ist nur die Unit (`CatalogManagementTests.cs:132`,
`Delete_Unit_MitZielEtappeAufDerUnit_Liefert409`); die Reihe nur über eine Lehrplan-Position.

Folge: der Unit-Umfang im Reihen-Pfad (`TextbookSeriesController.cs:301`) ließe sich stilllegen, und das Löschen endete am
Restrict-FK `KeyResult→SeriesUnit` als nackter 500 – kein Test würde rot.

Reparaturvorschlag des Reviewers: Fall 3 als `[Theory]` über `wholeSeries` wie der Nachbartest, dazu eine Mutante auf
Zeile 301.

## User Story

Als Creator möchte ich, dass das Löschen einer Reihe genau dann mit `unit_content_in_use` abgelehnt wird, wenn ein Ziel auf
eine ihrer Units zeigt – und nicht wegen einer Etappe auf fremdem Inhalt –, damit Meldung und Verhalten übereinstimmen.

## Ist-Stand am Code

- Reihen-Pfad: `TextbookSeriesController.Delete` (`backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:296-298`)
  übergibt `db.SeriesUnits.Where(u => u.SeriesId == seriesId)` an `ExerciseUsageQueries.AnyBlockingAsync`
  (`Services/Shared/ExerciseUsageQueries.cs:132-134`).
- Unit-Pfad: `SeriesUnitsController.Delete` (`:154-156`) mit `u.Id == unitId`.
- Getestet war nur der Unit-Pfad mit Unit-Etappe (`CatalogManagementTests.Delete_Unit_MitZielEtappeAufDerUnit_Liefert409`).

## Entscheidungen

1. Der Unit-Etappen-Fall wird `[Theory]` über `wholeSeries` wie der Nachbartest. *Entscheidung im Lauf (Defekt), getroffen von Claude.*
2. Aus dem Review: auch die Gegenrichtung (zu breit sperren) bekommt einen Fall – eine Etappe auf der Nachbar-Unit bzw. einer
   fremden Reihe sperrt **nicht**. Die Klassen-DB seedet keine Ziel-Etappen; ohne diesen Fall fiele eine Verbreiterung nur
   je nach Testreihenfolge auf. Kosten: ein Test.

## Akzeptanzkriterien

| Fall | Weg | Wert | erwartet |
|---|---|---|---|
| 1 | `DELETE` Unit | Etappe auf dieser Unit | `409 unit_content_in_use` (bestand) |
| 2 | `DELETE` Reihe | Etappe auf einer ihrer Units | `409 unit_content_in_use` |
| 3 | `DELETE` Unit | Etappe auf der Nachbar-Unit derselben Reihe | `204` |
| 4 | `DELETE` Reihe | Etappe auf einer Unit einer anderen Reihe | `204` |

## Schätzung

**Größe: XS** — nur Tests. Testweg: `CatalogManagementTests`.

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 21 (Lauf AE, frischer `pugling-reviewer`), 🟡.
- **2026-11-09** — ausformuliert, im Lauf entschieden (Defekt, Freigabe 1) und geschätzt; gebaut in Lauf AF, Sprint 25.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Test-Edit an `0c0462f + Arbeitsbaum`, 2026-11-09 14:13:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | 2 | `Delete_UnitOderReihe_MitZielEtappeAufDerUnit_Liefert409(wholeSeries: True)` | MB1 Unit-Umfang der Reihe leer (`TextbookSeriesController.cs:298`) | red, am Fall |
  | 4 | `Delete_UnitOderReihe_ZielEtappeAufEinerAnderenUnit_SperrtNicht(wholeSeries: True)` | MB2 Reihenfilter weg (`:298`) | red, am Fall |
  | 3 | `…_ZielEtappeAufEinerAnderenUnit_SperrtNicht(wholeSeries: False)` | MB3 Unit-Pfad auf die ganze Reihe verbreitert (`SeriesUnitsController.cs:156`) | red, am Fall |

  Fall 1 bestand vorher (Mutante aus B-413). Grundlinie nach dem Zurückspielen grün. **Rote Probe mit Zahl (Auflage 5):** MB1
  gegen den Ausgangsstand wäre grün gewesen – der Reihen-Fall existierte nicht (0 von 0 Fällen); nach dem Fix 1 rot von 1.
- **2026-11-09** — Review `pugling-reviewer`: keine 🔴; 🟡 zu breites Sperren in beiden Pfaden nur zufällig gedeckt →
  Fälle 3/4 ergänzt (**Zähler Sprint 25: 1**); 🟢 Übungs-Zweig der Etappenprüfung über die API nicht trennbar (eine Übungs-Etappe
  trägt immer ihre Unit) → Kommentar in `ExerciseUsageQueries.cs` statt Test (ohne Zählung); 🟢 Fortbestand nach 409 ungeprüft →
  bewusst nicht (der Löschaufruf folgt erst nach dem `return`).
- **2026-11-09** — abgenommen. Backend 1172/1172 (nach dem letzten Edit), Playwright 121 passed. **Rollengang** Creator ersetzt durch
  die Integrationstests gegen die echte API (`DELETE` Reihe/Unit mit und ohne fremde Etappe) – die Fläche hat kein eigenes UI über
  das Löschen hinaus, das `e2e/lehrwerke.spec.ts` fährt; Vater und Sohn als Regressionszeugen (Suite grün, kein Pfad berührt).
  Commit: siehe `git log -- docs/backlog/B-436-reihe-loeschen-mit-unit-etappe-ungetestet.md` (Lauf AF, Sprint 25).
