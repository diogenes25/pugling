---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [createChild Restkanten, Kind per Index]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf N, Teil 2 in Lauf O (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-357 · Restkanten des E2E-Helfers `createChild` und eine Kind-Auswahl per Index

Drei 🟢 der Nachschau über `cc19020` (B-255), keiner eine Entgleitung:

1. **Fehlt `id` in der Antwort**, wird aus `String(undefined)` die Id `"undefined"`, und der Fehler erscheint als
   Zeilen-Wartezeit statt mit Ursache (`frontend/e2e/helpers.ts:47`). Weg: `expect(typeof body.id).toBe("number")`.
2. **Das POST-Warten ist auf 10 s begrenzt** (`helpers.ts:42-43`), ohne Rand über dem Expect-Timeout; auf einem kalten
   CI-Läufer fiele ein langsames Anlegen als `TimeoutError` ohne Status. Erst ändern, wenn es auftritt.
3. **`responsive-vater.spec.ts:94-97`** nimmt die Kind-Id als `kids[0].id`, wenn Vater 1 keinen Plan hat – Auswahl per
   Index, gegen die Repo-Regel. Der Zweig wird mit dem Seed vermutlich nie erreicht.

## User Story

Als **Entwickler** möchte ich, dass der E2E-Helfer und die Tablet-Spec mit ihrer Ursache scheitern, damit ein roter Lauf
sagt, was fehlt, statt an einer Zeile zu warten, die nie kommt.

## Ist-Stand am Code (Lauf P, Stand `299d216`)

1. `frontend/e2e/helpers.ts:47` macht aus `(await response.json()).id` ohne Prüfung einen String.
2. `helpers.ts:42-43` wartet höchstens 10 s auf die POST-Antwort; `playwright.config.ts:22` setzt das Expect-Timeout auf 10 s.
3. `responsive-vater.spec.ts:94-97` nimmt `plans[0]` bzw. `kids[0]`.

## Die echte Lücke

Punkt 1 wie beschrieben. Punkt 3 ist **keine** Auswahl eines bestimmten Elements: die Spec braucht irgendeinen eigenen Plan,
und jedes eigene Kind trägt ihn. Die Lücke ist dort nur, dass eine leere Liste als `TypeError` statt mit Grund scheitert.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`expect(typeof body.id).toBe("number")`** mit dem Antworttext in der Meldung.
2. **Die 10-s-Grenze bleibt**, bis sie auf einem Läufer auftritt (Vorschlag der Nachschau selbst). *Kosten:* ein kalter
   CI-Läufer kann einmal mit `TimeoutError` ohne Status fallen.
3. **Der Index bleibt, mit Begründung**, und ein Vater ohne Kind scheitert mit Meldung. *Kosten:* die Repo-Regel „nie per
   Index" hat damit eine benannte Ausnahme.

## Akzeptanzkriterien

1. Eine Anlege-Antwort ohne numerische `id` scheitert im Helfer mit dem Antworttext.
2. Die Tablet-Spec nennt ihren Grund, wenn Vater 1 weder Plan noch Kind hat.

## Schätzung

`XS`, nur E2E. Testweg: `tsc` über die Specs, E2E-Gesamtlauf. Beide Zweige laufen mit dem Seed nicht; eine rote Probe
bräuchte einen gebrochenen Server (pm-loop Step 6: solche Fehlerzustände sind keine E2E-Aufgabe).

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf N, Teil 2 (`frontend-reviewer`, 🟢 ×3). Aus Code gelesen.
- **2026-11-03** — Lauf P, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Keine rote Probe: beide
  Zweige sind mit dem Seed unerreichbar (Schätzung). Geprüft per `tsc --strict` über `helpers.ts` und die Spec (sauber).
- **2026-11-03** — Review `frontend-reviewer`: sauber für B-357 (Antworttext in der Meldung, Index mit Begründung, Grund bei leerem Vater); Kommentar in `responsive-vater.spec.ts` auf Deutsch wie die Umgebung.
- **2026-11-03** — Abnahme Lauf P, Teil 1: Vitest **619/619**, `tsc -b` sauber, E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Der Entwickler ist die Rolle – die Specs über `createChild` (elf) und die Tablet-Spec liefen grün; Creator, Vater, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
