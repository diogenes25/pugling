---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Testmodus-Fehler ohne alert]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-444 · Der Testmodus meldet einen Fehler stumm, und der Text zu `exercise_empty` nennt einen Ausweg

Gefunden im Review zu B-424 (Lauf AF, Sprint 26, `frontend-reviewer`, 🟢). Seit B-424 erreicht der Vater den Testmodus auch aus
der Zuweisen-Perspektive; zwei ältere Mängel werden damit sichtbarer:

1. Der Fehlerkasten in `frontend/src/vater/ExercisePreviewModal.tsx` trägt kein `role="alert"`; der Fokus sitzt beim Öffnen auf
   „×", ein nachgeladener Fehler wird nicht angesagt.
2. Der deutsche Text zu `exercise_empty` (`frontend/src/lib/api.ts:87`) nennt einen Ausweg („Füge erst Wörter hinzu …") – gegen die
   Meldungsregel vom 2026-11-07 – und sagt „Wörter" auch bei Lückentext oder Liste.

## Akzeptanzkriterien

1. **Ein Ladefehler des Testmodus wird angesagt:** er steht in einer Live-Region, die schon beim Öffnen da ist.
2. **`exercise_empty` nennt nur die Sperre** – keinen Ausweg und keine „Wörter" (Lückentext, Liste).

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **`StatusBanner` statt eines eigenen Kastens** – das Primitiv hält die Region im Leerfall bereit;
   `role="alert"` (Vorschlag der Story) wäre die zweite Art, dasselbe zu tun.
2. **„Diese Übung hat noch keine Inhalte."** – gleichlautend in `GERMAN_PROBLEM_TEXT` und Arcade (`problem.*`); das
   Englische sagte es schon so.

## Verlauf

- **2026-11-09** — angelegt aus dem Review zu B-424; außerhalb des Sprint-Diffs, darum nicht gezählt und nicht in diesem Sprint gebaut.
- **2026-11-12** — Nachtlauf, Sprint 41: autonom gegrillt (`art: Defekt`), geschätzt XS, umgesetzt.
- **2026-11-12** — Rote Probe (`ExercisePreviewModal.test.tsx`, neu; `vi.spyOn(api, "previewExercise")` nach Vorbild
  `TextbookForm.test.tsx`): erwartet 2 rot, gemessen 2 rot (keine Region `status`; Satz mit „Füge … Wörter").
- **2026-11-12** — Belege: AK 1 → `steht in einer Live-Region, die schon vor ihm da war` (F5); AK 2 → `sagt bei einer
  leeren Übung nur, was sperrt` (F3).
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
