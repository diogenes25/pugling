---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/training, rolle/student]
aliases: [Lösung als Toast, Falsche Antwort ohne Pause]
status: geschaetzt
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
