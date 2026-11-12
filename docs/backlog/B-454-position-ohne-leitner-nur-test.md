---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, rolle/supervisor]
aliases: [Nur Test ohne Leitner]
status: abgenommen
nachgeschaut: 2026-11-12
prio: P1
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
grund: ""
ersetzt_durch: []
entgangen_bei: [B-419]
---

# B-454 · Eine Vokabel-Position ohne Leitner-Kasten bot dem Kind nur „TEST" – Abfrage ohne je gezeigte Wörter

## User Story

Als Kind möchte ich jede Übung erst ansehen und üben können, bevor ich getestet werde – egal, welches Häkchen
der Vater gesetzt hat.

## Ist-Stand am Code

Gefunden im Rollengang `e2e/ziel-vier-wochen.spec.ts` am 2026-11-10: der Vater legt die Position über die
Plan-Seite an; eine im Formular angelegte Vokabelübung trägt `defaultUseLeitner = false`, das Häkchen
„Leitner-Kasten" bleibt also aus. Die Kind-Startseite zeigte den Üben-Knopf nur bei
`pos.useLeitner || !pos.testable` (`frontend/src/sohn/SohnHome.tsx`, `PositionCard`) – für eine prüfbare
Position ohne Kasten blieb allein „TEST". Der Server spielt die Runde auch ohne Kasten aus
(`PositionPlayService.DueItemIndicesAsync`, `!pos.UseLeitner || IsDue`), samt Kennenlernkarten.

## Die echte Lücke

B-419 („erst zeigen, dann fragen") galt nur für Positionen mit Leitner-Kasten. Über den zweiten Anlegeweg
(Plan-Seite statt Assistent) wurde das Kind weiter kalt geprüft, und der Malus lief.

## Entscheidungen

1. **Der Üben-Knopf steht, sobald die Position Inhalte hat** (`poolSize > 0`), mit oder ohne Kasten. Kosten:
   auch andere prüfbare Typen ohne Kasten zeigen jetzt zwei Knöpfe; ohne Kasten bringt die Runde keine Punkte.
2. **Eine Tagesdosis verlangt den Kasten** (Server: `validation_error`; Formular: das Häkchen kommt mit) – das
   gehört zu B-04 und steht dort.

## Akzeptanzkriterien

1. Eine prüfbare Position ohne Leitner-Kasten zeigt dem Kind „ÜBEN" und „TEST".
2. `e2e/ueben-ohne-leitner.spec.ts`: Position mit `useLeitner: false` → das Kind sieht „ÜBEN“ neben „TEST“, und die
   Runde zeigt das Wort mit Lösung.

## Verlauf

- **2026-11-10** — gefunden und behoben in Sprint 29 des Fertigstellungs-Laufs (vor der Abnahme von B-04, aber in
  abgenommener Arbeit von B-419 – darum `entgangen_bei`).
- **2026-11-10** — `frontend-reviewer`: der Rollengang deckte den Fall nicht (die Dosis schaltet den Kasten ein); eigene Spec `ueben-ohne-leitner.spec.ts` ergänzt, AK 2 berichtigt.
- **2026-11-10** — abgenommen. E2E 134 passed, `ueben-ohne-leitner.spec.ts` ist der Rollengang des Kindes; `frontend-reviewer` gelaufen. Commit `595b429`.
- **2026-11-12** — Nachschau 2026-11-12 (Sprint 31): `canPractice` mit `poolSize > 0`; ohne Leitner zahlt der Server keine Punkte (`leitnerScored`), Spec legt `useLeitner: false` an – ohne Befund.
