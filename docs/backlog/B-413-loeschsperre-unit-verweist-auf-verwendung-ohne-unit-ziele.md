---
tags: [typ/story, status/abgenommen, bereich/beides, rolle/creator]
aliases: [Unit-Löschsperre zeigt auf „Verwendung"]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: ja (Code beim Löschen einer Unit/Reihe: exercise_in_use → unit_content_in_use; vor der Publikation frei, Verbraucher nur Frontend und Tests)
quelle: docs/pm-sitzung-2026-11-07.md#runde-2--review
grund: ""
ersetzt_durch: []
---

# B-413 · Unit/Reihe löschen meldet `exercise_in_use` mit Verweis auf „Verwendung" – Ziel-Etappen auf der Unit stehen dort nie

Gefunden im Review zu B-409 (`frontend-reviewer`, 🟡), bewusst nicht in den Sprint genommen (Regel: ein Fund während der Verifikation
wird eine eigene Story). Der deutsche Text zu `exercise_in_use` (`frontend/src/lib/api.ts`, Eintrag `exercise_in_use`) sagt, nur fremde
Verwendungen stünden nicht unter „Verwendung". Derselbe Code kommt aber auch beim Löschen einer **Unit** bzw. **Reihe**
(`SeriesUnitsController`, `TextbookSeriesController` über `ExerciseUsageQueries.AnyBlockingAsync`), und dort sperrt zusätzlich eine
Ziel-Etappe auf der **ganzen Unit** (`KeyResult.SeriesUnitId` ohne `ExerciseId`). Die zeigt keine „Verwendung" einer Übung – zu Recht,
sie sperrt das Löschen der Übung nicht (B-409, Fall 3b). Der Vater bekommt 409 und findet an der genannten Stelle nichts.

Nach der Meldungsregel vom 2026-11-07 (`pm-loop` Step 5) muss eine Fläche, auf die eine Meldung zeigt, die Sperre zeigen. Zu entscheiden
beim Ausformulieren: eigener Code bzw. eigener Text für das Löschen einer Unit/Reihe (nennt Ziel-Etappen auf der Unit, ohne „Verwendung"),
oder eine Verwendungs-Sicht auf Unit-Ebene. Hängt mit [B-414](B-414-meldungstexte-nennen-auswege.md) zusammen (dieselben Sätze).

## User Story

Als Vater möchte ich beim Löschen einer Unit oder Reihe eine Meldung, die nennt, was sperrt – ohne mich an eine Stelle zu
schicken, die diese Sperre gar nicht zeigt.

## Ist-Stand am Code

- `SeriesUnitsController.Delete` (`:154-159`) und `TextbookSeriesController.Delete` (`:298-303`) prüfen über
  `ExerciseUsageQueries.AnyBlockingAsync` und antworten `409 exercise_in_use` – denselben Code wie das Löschen einer Übung
  (`ExerciseControllerBase`).
- `AnyBlockingAsync` sperrt zusätzlich bei einer Ziel-Etappe auf der **Unit** (`KeyResult.SeriesUnitId`, ohne `ExerciseId`).
- Der deutsche Text zu `exercise_in_use` (`frontend/src/lib/api.ts`) verweist auf „Verwendung"; die zeigt nur Verwendungen einer
  **Übung** (seit B-409 inkl. Ziel-Etappen auf der Übung), nie Unit-Etappen. Die Frontend-Übersetzung schlüsselt nur über den Code –
  sie kann die beiden Fälle nicht trennen.
- Verbraucher von `exercise_in_use` beim Unit-/Reihen-Löschen: `CatalogManagementTests.Delete_UnitOderReihe_MitVerwendeterUebung_Liefert409`,
  `e2e/creator-lehrwerk-weg.spec.ts` (Schritt 6a, Reihe löschen). Kein Agent, kein Client.

## Die echte Lücke

Zwei verschiedene Sperren teilen einen Code, und ihr deutscher Text kann darum nur für eine wahr sein.

## Entscheidungen

1. **Eigener Code `unit_content_in_use` (409)** für das Löschen einer Unit und einer Reihe. *Entscheidung im Lauf (Defekt),
   getroffen von Claude.* Begründung: nur ein eigener Code erlaubt einen eigenen Text; vor der Publikation ändert v1 frei
   (`backend/CLAUDE.md`). Kosten: ein Vertragsbruch an zwei Endpunkten (Frontend und zwei Tests ziehen mit).
2. **Text nach der Meldungsregel** (BZ): er nennt, was sperrt – Übungen der Unit/Reihe in Lehrplan, Klassenarbeit oder Ziel, oder
   ein Ziel mit einer Etappe auf der Unit selbst – und verweist auf **keine** Fläche, weil keine alle diese Stellen zeigt. Kein
   „remove it there first" (das Herausnehmen einer Position ist eine zweite Sperre, `position_has_data`).

## Akzeptanzkriterien

| Fall | Weg | Wert | erwartet |
|---|---|---|---|
| 1 | `DELETE` Unit | Übung der Unit in einem Lehrplan | `409 unit_content_in_use` |
| 2 | `DELETE` Reihe | Übung der Reihe in einem Lehrplan | `409 unit_content_in_use` |
| 3 | `DELETE` Unit | nur eine Ziel-Etappe auf der Unit | `409 unit_content_in_use` |
| 4 | `DELETE` Übung (Vergleichsweg) | Übung in einem Lehrplan | weiterhin `409 exercise_in_use` |
| 5 | deutscher Text | `unit_content_in_use` | nennt Ziel-Etappe auf der Unit, **kein** „Verwendung", kein Ausweg |
| 6 | Klickweg | Reihe löschen in `creator-lehrwerk-weg.spec.ts` | zeigt den neuen deutschen Satz |

## Schätzung

**Größe: S** — ein Code, zwei Aufrufe, ein Text, zwei Tests umgestellt, ein neuer Fall (3). Angriffsplan: Tests auf den neuen Code
zuerst (rot), `ApiErrors` additiv, Controller, Text, E2E-Erwartung. Testweg: `CatalogManagementTests`, `errorMessage.test.ts`,
`creator-lehrwerk-weg.spec.ts`. Zusammen mit B-414 und B-411 in Sprint 21.

## Verlauf

- **2026-11-07** — angelegt aus dem Review zu B-409 (Lauf AC, Sprint 20).
- **2026-11-08** — ausformuliert, gegrillt (Defekt, im Lauf entschieden, beschriftet) und geschätzt; Sprint 21 mit B-411/B-414.
- **2026-11-08** — gebaut. Tests zuerst gegen den Ausgangszustand: die Unit-/Reihen-Fälle rot am Code (`exercise_in_use` statt `unit_content_in_use`), Fall 3 (`Delete_Unit_MitZielEtappeAufDerUnit_Liefert409`) ebenso. `ApiErrors.UnitContentInUse` additiv, `SeriesUnitsController`/`TextbookSeriesController` liefern ihn mit einem Satz ohne Ausweg, Swagger-Summaries nennen die Unit-Etappe. Deutscher Text ohne Verweis auf „Verwendung“. Belege: Fall 1/2 ← Mutanten B413-1/-2, Fall 3 ← B413-1, Fall 4 ← `ExerciseUsageScopeTests` (Vergleichsweg, `exercise_in_use` unverändert), Fall 5 ← B413-DE, Fall 6 ← B413-E2E (`creator-lehrwerk-weg.spec.ts`).
- **2026-11-08** — abgenommen. Backend 1126/1126, Vitest 675/675, `tsc -b` sauber, Playwright **119 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet; der erste Lauf war 117/2 – siehe Sprint-Protokoll). Mutanten mit `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Test-Edit an `c910cf8 + Arbeitsbaum`, 2026-11-08: 34 von 34 rot am eigenen Fall, Grundlinie danach grün (Tabelle im Protokoll `docs/pm-sitzung-2026-11-08.md`). **Zähler 4 von 5** (Sprint 21, beide Reviewer je zwei 🟡). Commit: siehe `git log -- docs/backlog/B-413-loeschsperre-unit-verweist-auf-verwendung-ohne-unit-ziele.md` (Lauf AD, Sprint 21). Review: Doku `docs/endpunkt-beziehungen.md`, `docs/tutorial-supervisor.md` nannten noch `exercise_in_use` → berichtigt. **Rollengang** Vater = `creator-lehrwerk-weg.spec.ts` Schritt 6a (Reihe löschen zeigt den neuen Satz).
