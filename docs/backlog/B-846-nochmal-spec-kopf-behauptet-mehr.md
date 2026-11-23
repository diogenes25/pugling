---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-660)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-846 · Der Kopf von `nochmal-nach-pflicht.spec.ts` verspricht ein Rot, das zwei Mutanten nicht liefern

## Ist-Stand am Code

`frontend/e2e/nochmal-nach-pflicht.spec.ts:4-8` sagt „rot, wenn der Knopf in eine andere Übung führt“; gegen `:85-88` bleiben zwei Mutanten grün: `positions[0]` statt der Zuordnung (der Plan hat nur eine Position) und „erste Übung im Pool“ (der Server sortiert nach `OrderIndex`, die Vokabeln haben 1, die Rechen-Übung 2). Getragen wird die Zuordnung von `NochEinmal.test.tsx:31/51-58`. Vorschlag: Rechen-Übung mit `orderIndex: 0` und zweite Position, oder den Satz auf „Block fehlt“ beschränken.

## User Story

Als Entwickler möchte ich, dass der Kopf einer Spec nur das Rot verspricht, das sie liefert, damit ich mich bei einer
Änderung an der Zuordnung Position → Übung auf sie verlassen kann.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Die Spec liefert das Rot, statt den Satz zu kürzen:** eine zweite **Vokabel**-Position (`Farben`) wird vor der
   geprüften angelegt (erste Position), und die Rechen-Übung steht mit `orderIndex: 0` vorn im Bonus-Vorrat, bleibt aber
   ohne Position. Damit landen „erste Position“ und „erste Übung im Vorrat“ beide außerhalb der Tiere. Kosten: eine
   Position mehr; der Test erfüllt beide Pflichten über die API. Abweichung vom ersten Entwurf (Rechen-Übung als
   Position): eine Nicht-Vokabel-Pflicht liegt nicht im Vorrat (B-533 Entscheidung 6), der Block bliebe dann leer, und
   „erste Übung im Vorrat“ wäre nicht mehr die Rechen-Übung. Alternative „Satz kürzen“ verworfen: die Unit-Ebene trägt
   die Zuordnung, aber der Kopf sagt sie zu, und der Rollengang ist genau der Ort, wo die Verdrahtung zählt.

## Akzeptanzkriterien

1. Mutante `positions[0]` statt der Zuordnung in `NochEinmal.tsx` → die Spec fällt (URL zeigt `Farben`).
2. Mutante „erste Übung im Pool“ → die Spec fällt.
3. Grundlinie grün.

## Schätzung

XS, frontend (nur E2E), keine Migration, kein Vertragsbruch. Testweg: Mutanten-Probe gegen die Spec auf Ausweichports.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 118). Grundlinie: `nochmal-nach-pflicht` 1 passed (Ausweichports 5211/5184).
  - AK 1 → `nochmal-nach-pflicht.spec.ts`, E1 (`o.today.positions[0]` in `NochEinmal.tsx:31`) → rot.
  - AK 2 → dieselbe Spec, E2 (erste Übung des Vorrats, `NochEinmal.tsx:34`) → rot.
  - AK 3 → Grundlinie vor und nach den Mutanten grün.
  Review (`frontend-reviewer`): 🟡 Entscheidung 1 und AK 1 beschrieben die Rechen-Übung als Position, gebaut ist
  `Farben` → Story nachgezogen (nur Text, ohne Zählung); 🟢 zwei Hinweise ohne Handlungsbedarf.
- **2026-11-23** — abgenommen (Sprint 118, `08a421cb`). Verifikation nach
  der letzten Änderung: Backend 2130/2130 (Release), Vitest 1361/1361, `tsc -b` sauber; Mutanten F1–F3, E1–E2, K1–K10 rot
  (K10 erst nach plan-weiten Lernstand-Zeilen im Aufbau, vorher grün = ohne Beleg). Rollengang Sohn per E2E gegen frisch
  gestartete Server (Ausweichports 5211/5184): `nochmal-nach-pflicht`, `bonus`, `bonus-deckel`, `bonus-deckel-voll`,
  `bonus-satz-je-karte`, `nacharbeit`, `nacharbeit-bis-erledigt` – 7 passed. Vater und Creator Regressionszeugen (Suite
  grün, `bonus-deckel` fährt die Vater-Seite des Deckels).
