---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/tests, rolle/student]
aliases: [Lösung in der angehaltenen Karte ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-nacht.md
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-09"
entgangen_bei: [B-420]
---

# B-439 · Die sichtbare Lösung in der angehaltenen Karte prüft kein Test

Gefunden in der Nachschau zu Sprint 22 (Commit `652e5d3`, Lauf AE, frischer `frontend-reviewer`, nur gelesen). Im
Haltezustand von B-420 steht die Lösung zweimal: sichtbar in der Karte (`SohnPractice.tsx`, Block `.card.mistake`, Zeile
`.rev`) und in der Live-Region, die dann nur `sr-only` wird. Die Zusicherungen treffen beide:

- Vitest `SohnPractice.test.tsx` (AK 1 und AK 3) prüfen `getAllByText(…).length > 0` – grün auch ohne die Zeile in der Karte.
- E2E `erstbegegnung.spec.ts` prüft `getByText("Lösung: die Straße").first()` mit `toBeVisible()` – ohne die Kartenzeile
  landet `.first()` auf der 1×1-px-Region, die Playwright als sichtbar zählt.

Damit ist der Kern der Story („die Lösung **neben meiner eigenen Antwort**") durch keinen Test gehalten; die gemessenen
Mutanten F1–F3/R5 fassen die Zeile nicht an. Reparatur des Reviewers: die Zusicherungen auf die Karte begrenzen
(`within(…closest(".card"))` bzw. `page.locator(".card.mistake")`), dazu prüfen, dass die Region im Haltezustand `sr-only`
trägt; eine Mutante „`.rev`-Zeile entfernt".

Kleinere Funde derselben Nachschau, hier mitgeführt (🟢):

1. `e2e/bilder.spec.ts:199-204` sucht „la ville" mit „Umdrehen"/„Gewusst!" – auf einer frischen Position gibt es diese Knöpfe
   nicht mehr (Kennenlernkarte); die Schleife läuft nur zufällig nie (Wort an Index 0). Die Spec prüft „anderes Bild" jetzt
   auf einer Kennenlernkarte statt auf einer Selbsteinschätzung – unbenannt.
2. `ErstbegegnungTests.GemischteRunde_…_OhneDieComboZuBrechen` misst die Reihenfolge, nicht die Combo – umbenennen oder die
   Runde spielen und `combo` prüfen.
3. Nach einer falschen Antwort auf der **letzten** Karte fällt der Fokus nach „Weiter" auf `body` (das Rundenende nimmt
   ihn nicht).

## User Story

Als Sohn möchte ich nach einem Tippfehler die Lösung **in der Karte neben meiner eigenen Antwort** sehen – und ein Test soll
dafür sorgen, dass das so bleibt.

## Ist-Stand am Code

- Haltezustand: `frontend/src/sohn/SohnPractice.tsx:415-418` (`.card.mistake`, Zeile `.rev`), Live-Region `:468` wird im
  Haltezustand `sr-only`.
- Vitest AK 1/AK 3 (`SohnPractice.test.tsx`, vorher `getAllByText(…).length > 0`) und E2E `erstbegegnung.spec.ts:80`
  (`.first()`) treffen auch die Live-Region. **Gegenprobe gemessen:** mit entfernter Lösungszeile (Mutante MF1) laufen die
  Tests von `HEAD` **15/15 grün** – der Defekt besteht.
- Rundenende: `phase === "done"` rendert die Überschrift, niemand nimmt den Fokus (`:155-160` vor dem Fix).

## Entscheidungen

1. Zusicherungen auf `.card.mistake` begrenzen, dazu `sr-only` der Region im Haltezustand prüfen (Vorschlag des Reviewers).
   *Entscheidung im Lauf (Defekt), getroffen von Claude.*
2. Die drei 🟢 werden mitgenommen: `bilder.spec.ts` sucht über „Weiter →" (Kennenlernkarten) und benennt das; der Testname
   wird ehrlich (`GemischteRunde_ZeigtErstDieNeuen_DannDasBekannte`); beim **Übergang** ins Rundenende geht der Fokus an die
   Überschrift (`tabIndex={-1}`, Muster `SohnInterests`) – aus jeder Phase, weil auch ein Treffer auf der letzten Karte das
   Feld wegnimmt. Kosten: drei Zeilen Code, ein zusätzlicher Vitest.

## Akzeptanzkriterien

| Fall | Weg | Wert | erwartet |
|---|---|---|---|
| 1 | Vitest | falsch getippt, Lösung geliefert | Lösung **in** `.card.mistake`, Region `sr-only` |
| 2 | Vitest | falsch getippt, Menge ohne Lösung | Absage **in** `.card.mistake`, Region `sr-only` |
| 3 | E2E | „die Strasse" | `.card.mistake` zeigt „Lösung: die Straße" |
| 4 | Vitest | Halt auf der letzten Karte → Weiter | Fokus auf der Rundenend-Überschrift |
| 5 | Vitest | Treffer auf der letzten Karte | Fokus auf der Rundenend-Überschrift |

## Schätzung

**Größe: XS** — Testzeilen, ein Effekt-Zweig. Testweg: `SohnPractice.test.tsx`, `erstbegegnung.spec.ts`, `bilder.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 22 (Lauf AE), 🟡. Löst Limit 2 aus: der Lauf endet nach Sprint 23.
- **2026-11-09** — ausformuliert, im Lauf entschieden (Defekt, Freigabe 1) und geschätzt; gebaut in Lauf AF, Sprint 24.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Code-Edit an `0c0462f + Arbeitsbaum`, 2026-11-09 14:04:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | 1 | `SohnPractice.test.tsx` „hält die Karte mit eigener Antwort und Lösung an …" | MF1 Lösung aus der Karte (`SohnPractice.tsx:417`) | red, am Fall |
  | 1, 2 | dieselben + „hält in einer Menge ohne Lösung …" | MF3 Region im Halt nicht `sr-only` (`:468`) | red, an beiden |
  | 2 | „hält in einer Menge ohne Lösung mit der Absage an" | MF2 Absage aus der Karte (`:417`) | red, am Fall |
  | 3 | `e2e/erstbegegnung.spec.ts` | ME1 = MF1 | red, am Fall |
  | 4, 5 | „führt nach der letzten Karte …", „fokussiert das Rundenende auch nach einem Treffer …" | MF4 Fokus-Zweig aus (`:158`) | red, an beiden |
  | 5 | „fokussiert das Rundenende auch nach einem Treffer …" | MF5 nur nach dem Halt (`:158`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün. **Rote Probe mit Zahl (Auflage 5):** MF1 gegen die Tests von `HEAD` → 15 von 15
  grün (erwartet: grün, weil das der Defekt ist); gegen die neuen Tests → rot.
- **2026-11-09** — Review `frontend-reviewer`: keine 🔴, keine 🟡; drei 🟢, alle behoben **ohne Zählung**: Wettlauf in der
  `bilder.spec`-Suchschleife (wartet jetzt auf den Kartenwechsel), Rundenende als `role="heading" aria-level={1}`,
  Fokusring `.vtitle[tabindex]:focus-visible` (`index.css`). Zähler Sprint 24: 0.
- **2026-11-09** — Vitest-Mutanten MF1–MF5 nach den 🟢-Fixes neu gemessen (14:23): alle red, Grundlinie grün.
- **2026-11-09** — abgenommen. Backend 1172/1172, Vitest 687/687, `tsc -b` sauber, Playwright **121 passed** (ganze Suite, Server
  vom Lauf nach dem letzten Code-Edit gestartet). **Rollengang** Sohn = `e2e/erstbegegnung.spec.ts` (frische Position: Kennenlernen,
  Abfrage, Tippfehler hält die Karte mit der Lösung **in der Karte** an) und `e2e/bilder.spec.ts` (Bildwechsel auf der
  Kennenlernkarte). Vater und Creator als Regressionszeugen: volle Suite grün, keine ihrer Flächen im Diff. Kein Browser-Rollengang
  von Hand (Chrome in diesem Lauf nicht genutzt). Commit: siehe `git log -- docs/backlog/B-439-loesung-in-angehaltener-karte-ungetestet.md` (Lauf AF, Sprint 24).
- **2026-11-09** — Nachschau (Retro Sprint 25, Lauf AF, frischer `frontend-reviewer`, `git show 62bb35d`): kein 🔴, kein 🟡.
  Prüfpunkte: alle sechs Mutanten-Zeilen gegen `SohnPractice.tsx:158/417/468` und `erstbegegnung.spec.ts:83` nachgerechnet (MF5 rot
  nur am Treffer-Fall, AK 4 bleibt grün – stimmt); doppelte h1 gesucht (nur `SohnLogin.tsx:48`, anderer Bildschirm); `toastRegion()`/
  `mistakeCard()` eindeutig. Fünf 🟢 → [B-443](B-443-fokus-wege-der-sohn-runde.md).
