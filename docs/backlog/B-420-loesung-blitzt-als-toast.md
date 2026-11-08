---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/training, rolle/student]
aliases: [Lösung als Toast, Falsche Antwort ohne Pause]
status: abgenommen
nachgeschaut: "2026-11-08"
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-sohn
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-420 · Nach einer falschen Antwort blitzt die Lösung 1,6 s als Toast unter der nächsten Karte auf

Im Rollengang vom 2026-11-08 tippte der Sohn bei „la rue" etwas Falsches. Die Karte sprang sofort auf
„le magasin", und darunter stand für 1,6 Sekunden „Lösung: die Straße" – unter der **nächsten** Frage, ohne die
eigene Antwort daneben. Ein Elfjähriger, der das Wort nie gesehen hat, liest die richtige Lösung in dieser
Zeit nicht; der Lernmoment, den ein Fehler bietet, verpufft.

## User Story

Als *Sohn* möchte ich nach einer falschen Antwort die richtige Lösung **neben meiner eigenen Antwort und der
Frage** sehen, solange ich will, und selbst „Weiter" drücken, damit ich aus dem Fehler lernen kann statt ihn nur
gemeldet zu bekommen.

## Ist-Stand am Code

- `SohnPractice.tsx:165-170`: im Zweig `feedback.kind === "wrong"` wird `setToast(t("practice.solution", …))`
  gesetzt und nach `1600` ms gelöscht; der Cursor rückt im selben Handler weiter (die nächste Karte wird
  gerendert, bevor der Toast verschwindet).
- Richtige Antworten: `:160-162`, Toast „+n 🪙" für 1100 ms, ebenfalls mit sofortigem Weiterrücken – das ist
  gewollt (Tempo) und bleibt.
- Der Server liefert die Lösung im Review-Outcome (`expected`, `reviewFeedback` `:34-41`); im Set-Modus ist sie
  `null`, dann heißt die Meldung „Leider nicht." (`de.ts:264`).
- Gemessen im Rollengang: Karte 2 → falsche Antwort → Seite zeigt sofort „Karte 3/5 · le magasin" und darunter
  „Lösung: die Straße"; nach einer Sekunde ist die Zeile weg.

## Die echte Lücke

Ein Fehler hat keinen eigenen Zustand. Richtig und falsch laufen denselben Weg (Toast + Weiterrücken), obwohl
nur der eine Fall etwas zu lesen hinterlässt.

## Entscheidungen

Autonom gegrillt (`art: Defekt`): der Code verengt die Antwort, „richtig herstellen" ist keine Produktfrage.

1. **Eine falsche Antwort hält die Karte an.** Die Karte bleibt stehen und zeigt die Frage, die eigene Antwort
   (durchgestrichen oder rot) und „Lösung: …"; erst „Weiter" (Knopf **und** Enter) rückt vor. Kosten: ein Klick je
   Fehler – das ist der Lernmoment, nicht Reibung.
2. **Richtige Antworten bleiben, wie sie sind.** Der kurze Toast und das Weiterrücken tragen das Tempo-Gefühl
   („Match"). Kosten: zwei Wege im Code, am Zustand benannt.
3. **Set-Modus ohne Lösung** („Leider nicht."): ebenfalls anhalten, nur mit der Absage – sonst hätte ein Fehler in
   Listen weiter keinen Halt. Kosten: keine.
4. **Live-Region**: die Lösung wird angesagt (bestehende `role="status"`-Zeile), Fokus geht auf „Weiter".
   Kosten: keine über die Hausregel hinaus (B-134).

## Akzeptanzkriterien

1. Falsche Antwort → die aktuelle Karte bleibt sichtbar, zeigt eigene Antwort und „Lösung: …"; die nächste Karte
   wird **nicht** gerendert, bis „Weiter" gedrückt oder Enter getippt wurde.
2. Richtige Antwort → unverändertes Verhalten (Toast, sofortiges Weiterrücken).
3. Set-Modus ohne `expected` → Karte hält mit „Leider nicht." an.
4. Das Rundenende nach der letzten Karte ist davon unberührt (letzte Karte falsch → „Weiter" führt zum Ende).
5. Vitest: je ein Fall für 1–4; die Mutante „Weiterrücken im wrong-Zweig lassen" macht Fall 1 rot.

## Schätzung

**Größe: S** — ein neuer Zustand `phase: "wrong"` in `SohnPractice.tsx`, ein Knopf, i18n de/en, vier
Vitest-Fälle in `SohnPractice.test.tsx`. Kein Vertrag.

**Risiken:** Der Tempo-Modus (⚡) und die Heartbeat-Logik dürfen beim Anhalten nicht aussetzen (Heartbeat läuft
weiter). Bestehende Specs, die auf sofortiges Weiterrücken nach Fehlern bauen (`full-flow.spec.ts`,
`buchstaben-ordnen.spec.ts`), vorher mit `grep -n "Lösung"` prüfen; `.claude/scripts/old-phrases.py` nach dem
Umbau.

**Angriffsplan:** (1) Zustand + Render-Zweig in `SohnPractice.tsx`; (2) Enter-Handler; (3) i18n; (4) Vitest;
(5) betroffene E2E anpassen; (6) Rollengang als E2E-Fall in `erstbegegnung.spec.ts` (B-419) oder eigener
Fall in `karten-umdrehen.spec.ts`.

**Testweg:** `SohnPractice.test.tsx` (Vitest), Playwright `full-flow.spec.ts` (Regression) plus ein Fall
„falsch → Weiter"; `mutant-probe.py`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  autonom gegrillt (Defekt), geschätzt. Nicht in B-419 aufgenommen: der Fehler trifft auch längst eingeführte
  Karten.
- **2026-11-08** — Lauf AE, Sprint 22 ([Protokoll](../pm-sitzung-2026-11-08-nacht.md)), Commit `652e5d3`. Neuer Zustand
  `wrong` in `SohnPractice.tsx`: die Karte bleibt stehen, zeigt die eigene Antwort durchgestrichen und „Lösung: …" (bzw.
  „Leider nicht." in einer Menge), Fokus auf „Weiter →", nach „Weiter" zurück ins Antwortfeld der nächsten Karte (Fund des
  `frontend-reviewer`, R5). **Präzisierung von Entscheidung 1:** angehalten wird nur nach einer **getippten** Antwort
  (Freitext, Kästchen, Kacheln, Auswahl); die Selbsteinschätzung („Nochmal") nicht – dort stand die Lösung schon auf der
  Rückseite, bevor das Kind urteilte. Die Live-Region trägt die Lösung im Haltezustand `sr-only` (sichtbar steht sie in
  der Karte); „anderes Bild" ist dort gesperrt.
- **2026-11-08** — Verifikation: Vitest 683/683, Playwright 120 passed. Fälle → Tests (`SohnPractice.test.tsx`):
  AK 1 „hält die Karte mit eigener Antwort und Lösung an, bis Weiter gedrückt wird" (inkl. Fokus auf Weiter und zurück
  ins Feld); AK 2 „rückt nach einer richtigen Antwort sofort weiter"; AK 3 „hält in einer Menge ohne Lösung mit der
  Absage an"; AK 4 „führt nach der letzten Karte über Weiter ins Rundenende"; Präzisierung „hält bei der
  Selbsteinschätzung nicht an". E2E: `erstbegegnung.spec.ts` (falsch → Karte bleibt nach 2 s stehen → Enter → nächste
  Karte, Fokus im Feld). Regression `full-flow`, `buchstaben-ordnen` grün; `old-phrases.py` nicht nötig (kein
  Meldungstext umformuliert).
- **2026-11-08** — Mutanten (`mutant-probe.py`, alle rot am benannten Test, Grundlinie grün): F1 wrong-Zweig rückt
  weiter (`SohnPractice.tsx:201`) rot; F2 auch Selbsteinschätzung hält (`:200`) rot; F3 Fokus nicht auf Weiter (`:144`)
  rot; R5 Fokus nicht zurück ins Feld (`:154`) rot.
- **2026-11-08** — Review `frontend-reviewer`: 🟡 Fokus nach „Weiter" fiel auf `body` → behoben (R5); 🟡 Schnellbonus
  misst die Lesezeit der Lösung mit (Server) → [B-434](B-434-schnellbonus-misst-die-lesezeit-der-loesung-mit.md); 🟢
  gedrücktes Enter (Autorepeat) kann die Lösung überspringen → hingenommen (selten, kein Doppel-Weiterrücken).
  Rollengang Sohn als E2E (siehe B-419). → `abgenommen`.
- **2026-11-08** — Nachschau (Lauf AE, frischer `frontend-reviewer`, nur gelesen, auf `652e5d3`): Halt nur bei getippter Antwort, Tempo-Leiste nur auf `front`, Bild im Halt gesperrt, Fokus hin und zurück, `next()` räumt `mistake`/`toast`. **Entgangen (🟡):** die sichtbare Lösung in der Karte prüft kein Test – Vitest und E2E treffen auch die `sr-only`-Region → [B-439](B-439-loesung-in-angehaltener-karte-ungetestet.md). 🟢: Fokus fällt nach der letzten Karte auf `body` (dort mitgeführt).
