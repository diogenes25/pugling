---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT mit Id 0 ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-386]
---

# B-393 · B-386 AK 1 „Ändern mit `0`" hat keinen Test

B-386 AK 1 verlangt `400 validation_error` für eine `vocabularyId` von `0` beim Anlegen **und** Ändern. Belegt ist `0` nur beim
`POST` (`backend/Pugling.Api.Tests/UnknownReferenceCodeTests.cs`, Fall `Unbekannte_Vokabel_an_einem_Uebersetzungspaar_…`); der `PUT`
ist nur mit einer unbekannten Id geprüft. Das Risiko ist klein (derselbe Validator), der Fix eine Zeile: `PUT … Translation(0)` →
`validation_error`. Dieselbe Klasse wie B-388.


## Akzeptanzkriterien

1. `PUT` einer Übersetzung mit `vocabularyId = 0` → `400 validation_error`, getestet.

## Schätzung

`XS`, `wo: backend`, eine Zusicherung im bestehenden Fall.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu B-386 (Lauf W, Retro Sprint 8; frischer `pugling-reviewer`, 🟢). Entgangen bei B-386,
  dem Increment von Lauf V – darum kein Limit 2 für Lauf W.
- **2026-11-05** — gebaut in Lauf W, Sprint 9 (mit B-392). AK 1 → `UnknownReferenceCodeTests.cs:138`. Rote Probe: nur der `PUT`
  meldet `0` als `invalid_reference` → rot in `:137` (seither `:138`), 1 von 19, kompiliert.
- **2026-11-05** — Review `pugling-reviewer` (mit B-392): 🟡 die Zusicherung fängt allein keine Regression – Anlegen und Ersetzen teilen einen Validator, die `POST`-Fälle fangen jede Lockerung schon; die Probe brauchte einen künstlichen eigenen `PUT`-Weg. Darum jetzt im Test als **Absichtserklärung** beschriftet (`CLAUDE.md`: „Eine Zusicherung, die den Ausgangszustand prüft …"), nur Text.
- **2026-11-05** — **abgenommen.** Volle Suite 1090/1090 nach dem letzten Edit; Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein frisch gestartetes Backend mit der Änderung (danach nur die Umbenennung eines Parameters). **Grenze:** keine Spec ändert den Text eines Übersetzungspaars im Browser – der Weg „Paar bearbeiten → Speichern → neu verlinkt“ ist nur durch den Integrationsfall belegt, nicht geklickt. Vater und Sohn unberührt (nur der Creator-`PUT`). Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `c429792`.
- **2026-11-05** — **Nachschau** (Lauf X, Retro Sprint 10): nichts entgangen; die Zusicherung ist als Absichtserklärung beschriftet und
  wird nicht als Absicherung gezählt.
