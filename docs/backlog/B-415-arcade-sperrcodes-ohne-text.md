---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Arcade-Sperrcodes ohne Text]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08.md
unverifiziert: true
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-415 · Sechs 409-Codes, die nur das Kind erreicht, haben in der Arcade keinen Text

Gefunden beim Ausformulieren von [B-411](B-411-tor-409-codes-haben-deutschen-text.md) (2026-11-08). Die Arcade übersetzt Fehler über
`problem.*` in i18n (`frontend/src/lib/chromeText.tsx`, `PROBLEM_KEYS`: heute vier Codes), nicht über `GERMAN_PROBLEM_TEXT`. Diese
409-Codes erreicht nur das Kind, und keiner steht in `PROBLEM_KEYS`: `skin_already_unlocked`, `shop_insufficient_stock`,
`media_no_alternative`, `media_not_on_card`, `interest_wish_limit_reached`, `concurrency_conflict`. Auf Deutsch fällt `useErrorMessage`
auf `errorMessage` zurück (englische Server-Zeile mit Trace-Id), auf Englisch zeigt es die Server-Zeile direkt.

Beim Ausformulieren zuerst je Code prüfen, ob die Arcade ihn überhaupt erreichen kann (manche sperrt die Oberfläche vorher, etwa ein
schon freigeschalteter Skin) – ein unerreichbarer Code braucht keinen Text, aber einen Grund auf der Ausnahmeliste von B-411s Tor.

## Akzeptanzkriterien

1. **Jede der sechs Sperren** (`skin_already_unlocked`, `shop_insufficient_stock`, `media_no_alternative`,
   `media_not_on_card`, `interest_wish_limit_reached`, `concurrency_conflict`) spricht in der Arcade einen eigenen Satz –
   auf Deutsch und auf Englisch, nie die Server-Zeile mit Ref.
2. **Deutsch heißt dieselbe Sperre überall gleich** (Arcade `problem.*` wortgleich mit `GERMAN_PROBLEM_TEXT`).

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Alle sechs bekommen einen Text, auch die schwer erreichbaren** (`media_not_on_card`, `skin_already_unlocked` nach
   einem Doppeltipp). Die Story erlaubte, unerreichbare mit Grund auszunehmen; ein Satz kostet weniger als eine
   belastbare Begründung, dass ein Code nie ankommt, und ein Rennen macht jeden erreichbar.
2. **Die deutschen Sätze stehen auch in `GERMAN_PROBLEM_TEXT`** – die bestehende Wortgleichheits-Regel der Arcade
   (`arcadeSpielweg.test.tsx`) verlangt es. Folge: die fünf Ausnahmen in `ProblemTextGuardTests` entfallen (das Tor
   meldet eine überflüssige Ausnahme selbst als rot).
3. **Kein Ausweg in den Sätzen** (Meldungsregel 2026-11-07): „Deine Liste mit eigenen Interessen ist voll." nennt die
   Sperre; der Server-Satz „ask a grown-up to remove one" bleibt dem Server.
4. `media_no_alternative` übernimmt den vorhandenen Satz `practice.noMoreImages` wörtlich.

## Verlauf

- **2026-11-08** — angelegt beim Ausformulieren von B-411 (Lauf AD, Sprint 21); bewusst nicht im Sprint (anderer Übersetzungsweg, andere Rolle).
- **2026-11-12** — Nachtlauf, Sprint 41: autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt (`PROBLEM_KEYS`,
  `problem.*` de/en, `GERMAN_PROBLEM_TEXT`, Ausnahmeliste im Tor geleert).
- **2026-11-12** — Rote Probe vor dem Fix (`arcadeSpielweg.test.tsx`, je Code ein Fall): erwartet 6 rot, gemessen 6 rot
  (`concurrency_conflict` nur auf Englisch: der deutsche Satz stand schon in `GERMAN_PROBLEM_TEXT`).
- **2026-11-12** — Belege je AK: AK 1 → `sagen dem Kind %s in beiden Sprachen mit einem eigenen Satz` (F1); AK 2 →
  Wortgleichheits-Schleife `sind auf Deutsch genau errorMessage` (F2); Tor `ProblemTextGuardTests` 2/2.
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
- **2026-11-12** — Nachschau (Retro Sprint 42, Nachtlauf): frischer `frontend-reviewer` an `1c7b567` (Vitest des Commits
  55/55). Geprüft: alle Leser von `goalMet`/`cadence` (Summen zählen freie Positionen serverseitig nicht,
  `PositionProgressService.cs:209-211`), alle 31 `confirmAction`-Texte gegen das Serververhalten, Erfolgssätze auf leeren
  Daten, de/en-Schlüsselgleichheit (0 Abweichungen, `practice.noMoreImages` nirgends mehr, `bilder.spec.ts:225` trifft den
  Satz über `problem.media_no_alternative`), Doku nach alten Sätzen. **Gefunden:** der B-467-Leerfall misst „gewertete
  Antwort", nicht „beantwortet"; „Übung löschen" kündigt wie B-418 eine Wirkung an, die der Server ablehnt →
  [B-499](B-499-nachschau-reste-sprint-41.md) (P2).
