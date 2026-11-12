---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Lernstand leer, frei mit Häkchen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-467 · Ohne eine einzige Antwort meldet der Lernstand „es sitzt alles 👍“, und freie Übungen tragen ein Häkchen

Zwei Stellen lesen „nichts gemessen“ als „alles gut“:

- **Vater, `/vater/kind/{id}/lernstand`:** Bei einem Kind ohne jede Antwort steht „Wörter (0)“ und darunter
  „Kein Wort unter 50 % – es sitzt alles. 👍“. Der Vater, der den Lernfortschritt prüfen will, liest Erfolg, wo
  noch nicht gelernt wurde. Auch nach acht eingeführten Wörtern bleibt die Liste leer.
- **Sohn, `/sohn/progress` („Meine Übungen“):** Positionen ohne Pflicht („frei“) tragen ✅, obwohl sie nie
  gespielt wurden; die Pflicht-Positionen darüber tragen ⬜. Die API liefert für `cadence: None` `goalMet: true`.

Erwartet: Der Lernstand unterscheidet „noch nichts gelernt“ von „nichts Schwaches“; eine freie Position zeigt
ihr Häkchen erst nach einer gespielten Runde oder gar keins.

## Akzeptanzkriterien

1. **Der Lernstand des Vaters** meldet unter „nur schwache" Erfolg nur, wenn das Kind Wörter hat und keins schwach ist;
   ohne ein einziges Wort sagt er „Noch kein Lernstand".
2. **Eine freie Position** (`cadence: None`) trägt in der Arcade weder Häkchen noch grünen Rahmen – auf der Startseite
   wie im Weg („Meine Übungen"); eine erfüllte Pflicht behält beides.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Eine zweite, kleine Abfrage** (`take: 1`, ohne Filter) beantwortet „gibt es überhaupt ein Wort?". Solange sie lädt
   oder fehlschlägt, steht der neutrale Satz „Kein Wort unter 50 %." – kein Erfolg ohne Messung.
2. **Freie Position: gar kein Häkchen** (statt „erst nach einer gespielten Runde"): sie hat kein Ziel, also nichts
   abzuhaken; der Punkt „•" im Weg bleibt. Der Server bleibt unverändert (`goalMet: true` für `None` trägt
   Tageswertung und Serie).
3. **Nicht angefasst:** dass acht eingeführte, aber nie beantwortete Wörter im Lernstand fehlen – der Lernstand zählt
   Antworten (`ItemProgress`); „eingeführt" ist eine andere Größe.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-12** — Nachtlauf, Sprint 41: autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt (`WeakWords` mit
  zweiter Abfrage, `SohnProgress`, `SohnHome.PositionCard`).
- **2026-11-12** — Rote Probe: `VaterLernstand.test.tsx` (neu) – zuerst am falschen Ort (fehlender `AuthProvider`,
  beide Fälle rot), korrigiert: erwartet 1 rot, gemessen 1 rot; der Gegenfall „es sitzt alles" ist vorher grün und trägt
  nur F7. `SohnProgress.test.tsx`: erwartet 1, gemessen 1 (`'✅Frei geübtfrei'`). `SohnHome.test.tsx` (neu, gegen den
  Stand vor dem Fix per `git stash`): erwartet 1, gemessen 1.
- **2026-11-12** — Belege: AK 1 → `meldet bei einem Kind ohne Lernstand keinen Erfolg` (F6), `meldet den Erfolg, wenn es
  Wörter gibt…` (F7); AK 2 Weg → `trägt kein Häkchen, eine erfüllte Pflicht schon` (F8); AK 2 Start → `…kein Häkchen und
  keinen grünen Rahmen` (F9 – erst nach Ergänzung der Rahmen-Zusicherung rot; vorher grün, weil die Pill-Bedingung
  doppelt schützt) und `…ihr Häkchen und den grünen Rahmen` (F10).
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
