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

# B-942 · Die Erfolgsmeldung des Fenster-Editors verschwindet sofort nach dem Speichern

## User Story

Als Vater möchte ich nach „Fenster speichern" sehen, dass gespeichert wurde.

## Ist-Stand am Code

`frontend/src/vater/VaterClassTests.tsx:271` hängt `FensterEditor` an den Schlüssel `${id}-${prepWindowDays}`; nach dem Speichern
lädt `detail.reload()` neu, der Schlüssel wechselt, der Editor wird neu montiert und verliert die Meldung von `useAction`.

## Die echte Lücke

Der Schlüssel enthält den gespeicherten Wert.

## Entscheidungen

1. Schlüssel nur `id`; das Feld behält danach die Eingabe des Vaters, die ohnehin der gespeicherte Wert ist. (Variante „Erfolg
   stumm" verworfen: ein leeres Feld zeigt sonst nicht, dass es den Standard bedeutet.)

## Akzeptanzkriterien

1. Nach erfolgreichem Speichern steht die Meldung („Vorbereitung: 3 Tage.") auch dann, wenn die Detailkarte neu lädt.

## Schätzung

- **Angriffsplan:** Schlüssel ändern; Test, der den Editor so einhängt wie die Detailkarte.
- **Testweg:** Vitest `VaterClassTests.test.tsx`.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
- **2026-11-24** — ausformuliert, mit dem Nutzer entschieden (Dialog der PM-Sitzung), geschätzt; Protokoll [pm-sitzung-2026-11-24.md](../pm-sitzung-2026-11-24.md).
- **2026-11-24** — `geschaetzt` → `in-arbeit`, gebaut (Sprint 135). Belege je Fall:
  - AK 1, Meldung übersteht das Neuladen der Detailkarte → `frontend/src/vater/VaterClassTests.test.tsx:160`, Mutante B942-key (alter Schlüssel mit Fensterwert) → rot
  - Mutanten gemessen mit `.claude/scripts/mutant-probe.py` nach der letzten Code-Änderung der Logik (13 + 4 Mutanten, alle rot; Grundlinie danach grün; Tabelle im Protokoll pm-sitzung-2026-11-24.md).
- **2026-11-24** — `in-arbeit` → `abgenommen` (Sprint 135): Commit `1b92cd02`; Backend-Suite 2309/2309 grün (Release, nach der letzten Code-Änderung), Frontend `vitest` 1426/1426 und `tsc -b` grün, Mutanten-Probe 17/17 rot; `pugling-reviewer` (ein 🟡 behoben, siehe B-941) und `frontend-reviewer` (keine 🔴/🟡) gelaufen. Rollengang maschinell: E2E `klassenarbeit-stoff-dicht`, `klassenarbeit-vorbereitung`, `klassenarbeit-countdown`, `pfad` 4 passed (Ports 5231/5204, Server von Playwright nach der letzten Änderung gestartet).
