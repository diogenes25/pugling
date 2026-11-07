---
tags: [typ/story, status/abgenommen, bereich/beides, rolle/creator]
aliases: [Verwendung ohne Ziel-Etappen]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
grund: ""
ersetzt_durch: []
entgangen_bei: [B-406]
---

# B-409 · „Verwendung" an der Übung zeigt keine Ziel-Etappen – der Verweis in der Fachwechsel-Meldung ist darum nur halb wahr

Der deutsche Satz zu `series_subject_in_use` (B-406) verweist für „wo sie bei deinen Kindern steckt" auf den Knopf „Verwendung".
`ExerciseCatalogController.Usage` (um `:248-274`) liefert aber nur Pläne und Klassenarbeiten plus `OtherLearnersCount` – keine
Ziel-Etappen (`KeyResult`), die das Löschen ebenso sperren (`ExerciseUsageQueries.CountBlockingAsync`). Hängt eine Übung nur an einem Ziel,
zeigt „Verwendung" nichts, und das Löschen antwortet trotzdem 409 – derselbe Widerspruch, den Anmerkung 14 beheben sollte.

Dazu zwei Beleg-Mängel aus derselben Nachschau: die ersten beiden AK-Zeilen von [B-406](B-406-fachwechsel-meldung-loeschen-kann-gesperrt-sein.md)
zitieren überholte Zusicherungen (`in einem Lehrplan`, `study plan` statt `nirgends mehr benutzt`, `only while nothing uses it`), und der
Summary von `Ausweg_Loeschen_Geht_Bei_Einer_Verplanten_Uebung…` spricht noch vom „take it out of the plan first".

Zu entscheiden beim Ausformulieren: Ziel-Etappen in `UsageResponse` aufnehmen (die vollständige Antwort) oder „Ziel" aus dem Verweis nehmen.


## User Story

Als Vater möchte ich unter „Verwendung" jede Stelle bei meinen Kindern sehen, die das Löschen einer Übung sperrt – auch eine
Ziel-Etappe –, damit die Anzeige und die Löschsperre dieselbe Antwort geben.

## Ist-Stand am Code

- `ExerciseCatalogController.Usage` (`backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs:248-275`) füllt nur
  `Plans` und `ClassTests`; `UsageResponse` (`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:79`) hat kein Feld für Ziele.
- Die Löschsperre zählt Ziel-Etappen mit: `ExerciseUsageQueries.CountBlockingAsync` (`Services/Shared/ExerciseUsageQueries.cs`,
  `goalTotal`/`goalMine`), `Explain` nennt sie als „objective milestone". Fremde Ziel-Etappen stecken schon in `HiddenLearners`.
- `UsagePanel` (`frontend/src/vater/VaterExercises.tsx:211-245`) entscheidet `hasOwn` nur über Pläne und Klassenarbeiten. Hängt eine
  Übung **nur** an einer eigenen Ziel-Etappe, steht dort wörtlich „Wird derzeit nirgends eingesetzt … Löschen ist möglich" – und
  `DELETE` antwortet `409 exercise_in_use`. Schärfer als die Notiz: nicht nur ein halb wahrer Verweis, sondern eine falsche Zusage.
- Ziel-Etappen auf eine **Unit** (`KeyResult.SeriesUnitId` ohne `ExerciseId`) sperren das Löschen einer Übung nicht
  (`CountBlockingAsync` filtert auf `ExerciseId`) und gehören darum nicht in die Anzeige.

## Die echte Lücke

Die Anzeige, auf die zwei deutsche Meldungen (`series_subject_in_use`, `exercise_in_use`) verweisen, kennt eine der drei Sperren
nicht. Nach der Meldungsregel vom 2026-11-07 (`pm-loop` Step 5: eine Fläche, auf die eine Meldung zeigt, muss die Sperre zeigen) ist
das die vollständige Antwort, nicht das Streichen von „Ziel" aus dem Verweis.

## Entscheidungen

1. **Ziel-Etappen kommen in `UsageResponse`** als Liste `Goals` (`GoalUsage`: Ziel-Id, Ziel-Titel, Etappen-Titel, Kind) – nur eigene
   Kinder, wie Pläne und Klassenarbeiten; fremde bleiben Zahl. *Entscheidung im Lauf (Defekt), getroffen von Claude.* Begründung:
   die Alternative ließe die falsche Zusage „Löschen ist möglich" stehen. Kosten: ein additives Antwortfeld (kein Bruch: Clients
   lesen, keiner schreibt es), eine Zeile im Panel.
2. **Die Beleg-Mängel aus B-406 werden mit berichtigt** (AK-Zeilen 1/2, Summary des Gang-Tests) – reine Doku, derselbe Satz.

## Akzeptanzkriterien

Fälle Weg × Wert (Regel aus `pm-loop` Step 5):

| Fall | Weg | Wert | erwartet |
|---|---|---|---|
| 1 | `GET …/usage` | eigene Ziel-Etappe auf der Übung | `goals` nennt Ziel, Etappe, Kind; `otherLearnersCount` 0 |
| 2 | `GET …/usage` | Ziel-Etappe eines fremden Kindes | `goals` leer, `otherLearnersCount` 1 |
| 3 | `GET …/usage` | eigene Ziel-Etappe auf einer **anderen** Übung | `goals` leer |
| 3b | `GET …/usage` + `DELETE` | Ziel-Etappe auf der **ganzen Unit** der Übung | `goals` leer, Löschen `204` (aus dem Review) |
| 4 | Panel | nur eine eigene Ziel-Etappe | Zeile „Ziele: …", **nicht** „nirgends eingesetzt" |
| 5 | Panel | gar nichts | weiterhin „nirgends eingesetzt … Löschen ist möglich" (Gegenprobe) |
| 6 | Klickweg (E2E) | Übung an eigener Ziel-Etappe, „Verwendung" klicken | Ziel sichtbar |
| 7 | Doku | B-406 AK 1/2, Summary | zitieren die heutigen Zusicherungen |

## Schätzung

**Größe: S** — ein Feld, eine Query, eine Panel-Zeile, ein Integrationstest, ein Vitest, eine E2E. Risiken: der Vergleichsweg
`CountBlockingAsync` und die neue Query müssen dasselbe Prädikat tragen (`ExerciseId == id`, Kind betreut) – je Bedingung eine
Mutante. `UsagePanel` ist nicht exportiert; für den Vitest wird er exportiert. Angriffsplan: Vertrag → Controller → Test →
OpenAPI neu → Panel → Vitest → E2E. Testweg: `ExerciseUsageScopeTests`, `VaterExercises`-Vitest, neue Spec `verwendung-ziele.spec.ts`.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf AB, Sprint 18 (Retro Sprint 19; frischer `pugling-reviewer`, `git show 98e684d`, 🟡).
  Increment **dieses** Laufs: **Limit 2**, Lauf AB endet nach Sprint 19.
- **2026-11-07** — ausformuliert, gegrillt (Defekt, im Lauf entschieden, Entscheidung 1 beschriftet) und geschätzt am Code; Lauf mit B-409, Sprint 20.
- **2026-11-07** — gebaut (Lauf AC, Sprint 20). Tests zuerst gegen den Ausgangszustand: die drei Integrationsfälle rot an
  `KeyNotFoundException` (kein Feld `goals`), nicht am Aufbau. Backend: `GoalUsage` + `UsageResponse.Goals`
  (`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs`), Abfrage mit dem Prädikat von `CountBlockingAsync`
  (`ExerciseCatalogController.Usage`). Frontend: `UsagePanel` zählt Ziele in `hasOwn`, Zeile „Ziele: …".
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Test-Edit an `5143e75 + Arbeitsbaum`, 2026-11-07 23:09:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | 1 | `ExerciseUsageScopeTests.EigeneZielEtappe_WirdUnterVerwendungGenannt` | MB3 Ziel-Liste leer (`ExerciseCatalogController.cs:281`) | red, am Fall |
  | 2 | `…FremdeZielEtappe_BleibtEineZahl` | MB2 Betreuungsfilter weg (`:273`) | red, am Fall |
  | 3 | `…ZielEtappe_EinerAnderenUebung_ZaehltNicht` | MB1 Übungsfilter weg (`:273`) | red, am Fall |
  | 3b | `…ZielEtappe_AufDieGanzeUnit_IstKeineVerwendungDerUebung` | MB4 Unit-Etappen mitgenommen (`:273`) | red, am Fall |
  | 4 | `UsagePanel.test.tsx` „nennt eine eigene Ziel-Etappe …" | MF1 `hasOwn` ohne Ziele (`VaterExercises.tsx:222`) | red, am Fall |
  | 4b | `UsagePanel.test.tsx` „lässt den Etappen-Titel weg …" | MF2 Titel ungeprüft (`:235`) | red, am Fall |
  | 5 | `UsagePanel.test.tsx` „sagt bei gar keiner Verwendung …" | – | *Gegenprobe*, auch vor dem Fix grün |
  | 6 | `e2e/verwendung-ziele.spec.ts` | ME1 `hasOwn` ohne Ziele (`:222`) | red, am Fall |
  | 7 | Doku | – | B-406 AK-Zeilen 1/2 und Summary `Ausweg_Loeschen_…` berichtigt (Text) |

  Grundlinie nach dem Zurückspielen grün. Fall 2: `otherLearnersCount == 1` hielt schon vorher (im Summary vermerkt).
- **2026-11-07** — Review: `pugling-reviewer` keine 🔴; 🟡 Unit-Etappe ungetestet → Fall 3b ergänzt, 🟡 Action-Summary nannte die Ziele
  nicht → berichtigt, OpenAPI neu, `docs/uebergabe-vokabeln.md` nachgezogen. `frontend-reviewer` keine 🔴; 🟡 Unit-/Reihen-Löschen
  verweist auf „Verwendung" ohne Unit-Ziele → [B-413](B-413-loeschsperre-unit-verweist-auf-verwendung-ohne-unit-ziele.md), 🟡 alte
  Meldungstexte nennen Auswege → [B-414](B-414-meldungstexte-nennen-auswege.md) (beide bewusst nicht in diesen Sprint: das Sprint-Ziel
  betrifft das Löschen einer **Übung**, und dafür sind beide Verweise jetzt wahr); 🟢 Kommentar „Zwei —-Zeilen" berichtigt. **Zähler 4 von 5.**
- **2026-11-07** — abgenommen. Backend 1123/1123, Vitest 656/656, `tsc -b` sauber, Playwright **119 passed** (ganze Suite, Server
  vom Lauf selbst nach dem letzten Edit gestartet). **Rollengang** Vater = `e2e/verwendung-ziele.spec.ts` (Übung an eigenem Ziel →
  „Verwendung" nennt das Ziel, kein „nirgends"). Creator und Sohn als Regressionszeugen: Suite und ihre Specs grün, kein Spielweg
  berührt. Commit: siehe `git log -- docs/backlog/B-409-verwendung-zeigt-keine-ziel-etappen.md` (Lauf AC, Sprint 20).
