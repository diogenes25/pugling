---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Position entfernen kündigt Verlust an]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08.md#runde-2--review
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-418 · „Position entfernen" kündigt den Verlust des Fortschritts an – den es nie gibt, weil der Server dann ablehnt

Gefunden im Frontend-Review zu Sprint 21 (`frontend-reviewer`, Nebenbefund zu B-411), bewusst nicht im Sprint (älterer Code). Die
Bestätigungsfrage in `frontend/src/vater/PlanPositions.tsx:688` lautet „Fortschritt und Auswertung dieser Position gehen verloren". Hat die
Position aber Übungs- oder Testdaten, lehnt der Server mit `409 position_has_data` ab (`PlanPositionsController.cs:272`). Der Vater bestätigt
also einen Verlust und liest danach, dass das Entfernen nicht geht. Ohne Daten gibt es nichts zu verlieren. Die Ankündigung ist in keinem der
beiden Fälle wahr. Am Code geprüft am 2026-11-08.

## Akzeptanzkriterien

1. **Die Frage vor „Entfernen" kündigt keinen Verlust an** und sagt, dass die Übung im Katalog bleibt.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Nur der Text ändert sich.** Entfernen geht nur ohne Übungs- und Testdaten; mit Daten erklärt der 409-Text
   (`position_has_data`), warum nicht. Ein Feld „hat Daten" im `PositionResponse`, um den Knopf vorher zu sperren, wäre
   ein Wunsch (Vertrag, Server) und nicht nötig, damit die Frage stimmt.
2. Die Frage steht als `REMOVE_POSITION_QUESTION` an einer Stelle, damit ein Test sie lesen kann.

## Verlauf

- **2026-11-08** — angelegt aus dem Frontend-Review zu Sprint 21 (Lauf AD).
- **2026-11-12** — Nachtlauf, Sprint 41: autonom gegrillt (`art: Defekt`), geschätzt XS, umgesetzt.
- **2026-11-12** — Rote Probe: zuerst am falschen Ort (die Konstante gab es noch nicht, `undefined`) – dann die alte
  Frage als Konstante herausgezogen und neu gemessen: erwartet 1 rot, gemessen 1 rot (`not to match /verloren/`).
- **2026-11-12** — Beleg: AK 1 → `PlanPositions.test.ts`, `kündigt keinen Verlust an und nennt, was bleibt` (F4). Ein
  dünner Test über einen Satz – er hält nur fest, dass die Ankündigung nicht zurückkehrt.
- **2026-11-12** — `frontend-reviewer`: kein 🔴; **ein 🟡** in B-467 – „das Kind hat noch nichts geübt" stimmte nicht,
  wenn Wörter eingeführt, aber nie beantwortet sind; jetzt „…hat noch keine Frage beantwortet" (Test prüft den ganzen
  Satz). Dazu ein **roter E2E-Fall** (`vater-von-null.spec.ts:430`), der den alten Erfolgs-Satz für ein Kind ohne
  Lernstand festschrieb – angepasst. **Review-Zähler Sprint 41: 2.** 🟢 ohne Zählung behoben: Sonderfall
  `media_no_alternative` in `SohnPractice` entfällt samt `practice.noMoreImages` (eine Kopie weniger), zwei englische
  Sätze, JSDoc an `REMOVE_POSITION_QUESTION`. 🟢 außerhalb des Diffs → [B-496](B-496-reste-review-sprint-41.md). 🟢
  bewusst offen: der B-418-Test liest die Konstante, nicht den Aufruf.
- **2026-11-12** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Vitest über Node): F9 war zuerst **grün** (die
  Pill-Bedingung schützt doppelt) – Rahmen-Zusicherung ergänzt, dann rot; F2 fiel zuerst „am falschen Ort" (die
  Wortgleichheits-Schleife statt des neuen Falls – dort gehört es hin, Erwartung angepasst).

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | F1 Arcade-Schlüssel fehlen | chromeText.tsx:46 | red | red |
  | F2 deutscher Satz fehlt | api.ts:178 | red | red (Wortgleichheits-Schleife) |
  | F3 alte exercise_empty-Zeile | api.ts:87 | red | red |
  | F4 alte Entfernen-Frage | PlanPositions.tsx:39 | red | red |
  | F5 Fehlerkasten ohne Live-Region | ExercisePreviewModal.tsx:140 | red | red |
  | F6 leere Liste immer Erfolg | VaterLernstand.tsx:105 | red | red |
  | F7 Erfolg fällt weg | VaterLernstand.tsx:108 | red | red |
  | F8 Häkchen an freier Position (Weg) | SohnProgress.tsx:52 | red | red |
  | F9 Häkchen an freier Position (Start) | SohnHome.tsx:126 | red | red |
  | F10 Pflicht ohne Häkchen (Start) | SohnHome.tsx:126 | red | red |

- **2026-11-12** — **abgenommen.** `tsc -b` sauber, Vitest 774/774, Backend 1396/1396 (Debug). **Rollengang als
  Maschinengang:** Playwright gegen echten Server und Wegwerf-DB auf Ausweichports (5201/5174, neben dem Heimnetz-Server):
  `full-flow.spec.ts` (Vater→Sohn), `vater-von-null.spec.ts` (Lernstand eines Kindes ohne Antwort), `responsive-vater.spec.ts`
  – 4/4. Nicht angesehen: die Sätze in der englischen Arcade und der Rahmen am Handy.
