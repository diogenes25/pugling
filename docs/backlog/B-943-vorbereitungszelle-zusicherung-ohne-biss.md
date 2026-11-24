---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: "Review von B-757 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, frontend-reviewer 🟡)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-943 · Die Zusicherung „keine Zahl bei geschriebener oder abgesagter Arbeit“ kann nicht fallen

## User Story

Als Entwickler möchte ich, dass der Test „keine Zahl bei geschriebener oder abgesagter Arbeit" fällt, wenn die Statusprüfung fehlt.

## Ist-Stand am Code

`frontend/src/vater/VaterClassTests.test.tsx:115-118` gibt `prepCardsPerDay: null` mit; ohne Statusprüfung in
`VorbereitungsZelle` (`VaterClassTests.tsx:182`) bleibt „Karten" trotzdem fern.

## Die echte Lücke

Der Ausgangszustand der Zusicherung ist schon „keine Karten".

## Entscheidungen

1. `prepCardsPerDay: 4` und `toBe("–")` (Vorschlag des Reviewers).

## Akzeptanzkriterien

1. Entfernt man `if (k.status !== "Planned")`, fällt der Fall.

## Schätzung

- **Testweg:** Mutante in `VaterClassTests.tsx:182`.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
- **2026-11-24** — ausformuliert, mit dem Nutzer entschieden (Dialog der PM-Sitzung), geschätzt; Protokoll [pm-sitzung-2026-11-24.md](../pm-sitzung-2026-11-24.md).
- **2026-11-24** — `geschaetzt` → `in-arbeit`, gebaut (Sprint 135). Belege je Fall:
  - AK 1, ohne Statusprüfung fällt der Fall → `frontend/src/vater/VaterClassTests.test.tsx:97`, Mutante B943-status → rot
  - Mutanten gemessen mit `.claude/scripts/mutant-probe.py` nach der letzten Code-Änderung der Logik (13 + 4 Mutanten, alle rot; Grundlinie danach grün; Tabelle im Protokoll pm-sitzung-2026-11-24.md).
- **2026-11-24** — `in-arbeit` → `abgenommen` (Sprint 135): Commit `1b92cd02`; Backend-Suite 2309/2309 grün (Release, nach der letzten Code-Änderung), Frontend `vitest` 1426/1426 und `tsc -b` grün, Mutanten-Probe 17/17 rot; `pugling-reviewer` (ein 🟡 behoben, siehe B-941) und `frontend-reviewer` (keine 🔴/🟡) gelaufen. Rollengang maschinell: E2E `klassenarbeit-stoff-dicht`, `klassenarbeit-vorbereitung`, `klassenarbeit-countdown`, `pfad` 4 passed (Ports 5231/5204, Server von Playwright nach der letzten Änderung gestartet).
