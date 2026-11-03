---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test]
aliases: [vater-main Querachse]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: [B-276]
wartet_auf: ""
---

# B-363 · B-276 begründet `min-width: 0` falsch, und eine Zusicherung der neuen Probe trägt nichts

`.app-vater` ist eine Flex-**Spalte** (`frontend/src/index.css:293`, selbst nachgelesen). Die Breite von `.vater-main` liegt
damit auf der Querachse, wo es keine automatische Mindestbreite gibt – darum trug `min-width: 0` nichts, **nicht** weil der
Überlauf nicht `visible` ist (so steht es im Kommentar an `.vater-main` und in B-276 Entscheidung 2). Ohne `overflow-x: auto`
behält `.vater-main` `width: 100%` und wächst **nicht** mit der Tabelle (anders als der Kommentar in
`e2e/responsive-vater.spec.ts:153` sagt); die Tabelle läuft über, `html` schneidet ab. Die Zusicherung `mainBreite <= WIDTH`
fällt unter der Mutante darum nicht – getragen hat `verschoben` (so auch gemessen: „1 failed an ‚scrollt seitlich'"). Die
Review-Zeile in B-276 „fallen `mainBreite`, `verschoben` und `dokument`" ist an `mainBreite` falsch.

## User Story

Als **Entwickler** möchte ich, dass ein Kommentar die wirkliche Ursache eines gemessenen Effekts nennt und jede Zusicherung
einer Probe etwas trägt, damit ein späterer Umbau sich auf beides verlassen kann.

## Ist-Stand am Code (Stand `a25a1a6`)

- `frontend/src/index.css:293` `.app-vater { display: flex; flex-direction: column; }`; `:320-321` begründet `min-width: 0`
  mit dem Überlauf.
- `frontend/e2e/responsive-vater.spec.ts:153` „dann wächst `.vater-main` mit der Tabelle"; `mainBreite <= WIDTH` als Beleg.

## Die echte Lücke

Wie beschrieben.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Kommentare berichtigen:** an `.vater-main` die Querachse als Grund, in der Spec „behält seine Breite, die Tabelle läuft
   heraus" statt „wächst mit".
2. **`mainInhalt > mainBreite` und `mainBreite <= WIDTH` als Vorbedingungen beschriften**, nicht streichen: sie halten den Fall
   gegen eine Probe, die nicht breit genug wäre. Getragen wird AK 1 von `verschoben`.

## Akzeptanzkriterien

1. Die Mutante „ohne `overflow-x`" fällt an „scrollt seitlich", nicht an einer Vorbedingung.
2. Kein Kommentar begründet `min-width: 0` mit dem Überlauf.

## Schätzung

`XS`, frontend (Kommentare + Beschriftung). Testweg: Mutante gegen `responsive-vater.spec.ts`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (frischer `frontend-reviewer` über `a25a1a6`, 🟡). Flex-Richtung
  selbst nachgelesen.
- **2026-11-03** — Lauf Q, Teil 2: autonom gegrillt (Freigabe 1), gebaut. AK 1 → Mutante „ohne `overflow-x`" **1 failed** an
  „der Seiten-Container scrollt seitlich" (Meldung gelesen); AK 2 → `index.css` und Spec-Kommentar berichtigt. B-276 behält
  seinen Text; die Berichtigung steht hier.
- **2026-11-03** — Review `frontend-reviewer` und `pugling-reviewer`: kein 🔴; ein 🟡 (InfoHint noch nicht gestaged – AK 1 von B-362 gilt erst nach dem Commit, dort per `git ls-files --eol` geprüft, siehe Protokoll; kein Code geändert, ohne Zählung); drei 🟢 (Umbruch, zwei Kommentar-Formulierungen) ohne Zählung behoben. Abnahme Lauf Q, Teil 2: volle Suite **1059/1059**, Vitest **621/621**, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: Vater maschinell über `responsive-vater.spec.ts` (breite Tabelle bei 768 px), die Mutante fällt am tragenden Satz; Creator, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf R, Teil 1 (frischer `frontend-reviewer`, `git show 617df82`, read-only): **sauber.** Geprüft: beide Vorbedingungen stehen vor `verschoben` und sind beschriftet; `scrollWidth` zählt den Überlauf auch bei `visible`; `.app-vater` ist `flex-direction: column` (`index.css:293`).
