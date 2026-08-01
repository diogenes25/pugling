---
tags: [typ/story, status/gegrillt, bereich/qualitaet, bereich/tests]
aliases: [Testabdeckung-Paket, Sammel-Story Testabdeckung]
status: gegrillt
prio: P2
art: Aufräumen
quelle: docs/testplan.md
---

# B-52 · Sammel-Story: das Testabdeckungs-Paket

Sieben Stories zur Testabdeckung ([B-26](B-26-e2e-in-ci.md), [B-27](B-27-testsuite-grenzfaelle.md),
[B-40](B-40-client-routen-waechter.md), [B-41](B-41-produktions-startup-smoke.md),
[B-42](B-42-openapi-typen-generieren.md), [B-43](B-43-frontend-komponententests.md),
[B-47](B-47-deploy-artefakt-smoke.md)) wurden einzeln ausformuliert und kollidieren einzeln gebaut. Der Plan
steht in **[testabdeckung-plan.md](../testabdeckung-plan.md)** – Etappen, Nähte und die offenen Punkte dort,
**nicht hier** (Backlog-Regel „kein zweiter Ablageort", [README.md](README.md)).

Diese Story führt **keine** Etappen-Zustände; die sieben behalten ihre Stufe und bleiben die Arbeitseinheiten.
Sie wird erst `abgenommen`, wenn im Plandokument keine offene Etappe mehr steht.

## User Story

Als **Entwickler**, der die sieben Test-Stories nacheinander baut, möchte ich **eine** verbindliche
Reihenfolge und die drei geteilten Nähte an einem Ort haben, damit ich nicht in Etappe 4 merke, dass Etappe 2
dieselbe Konstante, dasselbe Artefakt oder dasselbe Lockfile anfasst.

## Ist-Stand am Code

Nur die drei Berührungspunkte – alles Weitere steht je Etappe in ihrer eigenen Story und im Plandokument:

- **Naht 1, eine geteilte Konstante:** `EndpointCoverageGuard.cs:30` pinnt `FullRunTouchedActions = 263`, und
  `:75` macht daraus eine **obere** Schranke („Erfreulich: … bitte auf diesen Wert setzen"). E1 und E2 fahren
  beide neue Hosts und wollen sie ziehen.
- **Naht 2, ein Artefakt, das nicht byte-stabil ist:** `Program.cs:279` lädt beim Hoststart
  `OpenApiExampleCatalog.Load(ContentRootPath)` (`OpenApiExampleCatalog.cs:20` liest die eingecheckte
  `OpenApi/openapi-examples.generated.json` aus dem Quellbaum), und `DocsCaptureTests.cs:1105-1113` schreibt
  genau diese Datei im selben Lauf neu. xUnit parallelisiert über Collections – die Reihenfolge ist nicht
  bestimmbar.
- **Naht 3, ein Lockfile:** E5 braucht `@testing-library/react` (Peer `@testing-library/dom@^10`, den
  `npm ci --legacy-peer-deps` **nicht** mitinstalliert), E6 braucht `openapi-typescript` (Peer nur
  `typescript ^5.x`, unkritisch). Dazu setzt `frontend/vitest.config.ts:10-15` kein `globals: true` – ohne
  globales `afterEach` registriert RTL sein `cleanup()` nicht.
- **Der Fund, den keine Story kannte:** `gh run list --workflow=e2e.yml` – Lauf `30608976657` vom 2026-07-31
  ist **rot**, weil `vater-von-null.spec.ts:265-272` die gelöschte Lernziel-Ebene fährt (0 Treffer für
  „Lernziel anlegen" in `frontend/src`).

## Die echte Lücke

Nicht „die sieben Stories sind unsortiert" – jede trägt eine eigene Reihenfolge-Entscheidung. Die Lücke ist,
dass **keine** von ihnen die drei Berührungspunkte sieht: `EndpointCoverageGuard.FullRunTouchedActions` (eine
obere Schranke, die zwei Etappen ziehen wollen), das eingecheckte `/openapi/v1.json` (heute nicht byte-stabil,
weil der Beispielkatalog beim Hoststart aus dem Quellbaum gelesen und im selben Lauf neu geschrieben wird)
und ein gemeinsames Lockfile mit zwei devDependencies. Dazu kam beim Bündeln ein **offener Defekt** ans Licht,
den keine der Stories kannte: der E2E-Nachtlauf ist seit dem 2026-07-31 rot.

Belege und Herleitung stehen im Plandokument.

## Akzeptanzkriterien

1. Alle Etappen des Plandokuments sind `abgenommen` oder ausdrücklich verworfen – mit Grund.
2. Kein Punkt des Plans ist nur hier abgehakt: der Zustand einer Etappe steht in **ihrer** Story.

## Entscheidungen

Acht, getroffen am 2026-08-01 – sie stehen mit Begründung und Kosten im Plandokument unter
„[Entscheidungen](../testabdeckung-plan.md)". Zwei davon haben den Zuschnitt verändert: E0 **schreibt den
toten E2E-Abschnitt um statt ihn zu streichen** (die KeyResult-Ebene ist heute gar nicht abgedeckt), und die
Sperre aus E5 bekommt keinen Schlüssel-Parameter, dafür verlässt die Bibliothekssuche das Schreib-Primitiv.

## Verlauf

- **2026-08-01** — angelegt: sieben Stories zu einem Paket gebündelt, Plandokument geschrieben, Backend- und
  Frontend-Dev-Sicht eingeholt. Zwei Stories aus dem Paket geschoben (B-27, B-47), eine Etappe neu erfunden
  (Werkzeugkette Frontend), ein roter E2E-Nachtlauf gefunden.
- **2026-08-01** — gegrillt: acht Entscheidungen im Plandokument. Neu abgespalten
  [B-53](B-53-wizard-doppelklick.md); [B-26](B-26-e2e-in-ci.md) und [B-27](B-27-testsuite-grenzfaelle.md)
  neu zugeschnitten, [B-47](B-47-deploy-artefakt-smoke.md) mit Eintrittsbedingung versehen.
