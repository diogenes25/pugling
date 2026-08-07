---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Testabdeckung-Paket, Sammel-Story Testabdeckung]
status: abgenommen
prio: P2
art: Aufräumen
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/testplan.md
nachgeschaut: "2026-08-07"
---

# B-52 · Sammel-Story: das Testabdeckungs-Paket

Sieben Stories zur Testabdeckung ([B-26](B-26-e2e-in-ci.md), [B-27](B-27-testsuite-grenzfaelle.md),
[B-40](B-40-client-routen-waechter.md), [B-41](B-41-produktions-startup-smoke.md),
[B-42](B-42-openapi-typen-generieren.md), [B-43](B-43-frontend-komponententests.md),
[B-47](B-47-deploy-artefakt-smoke.md)) wurden einzeln ausformuliert und kollidieren einzeln gebaut. **Zwei
davon hat der Plan noch am Tag des Grillens herausgenommen** – B-27 (Testtiefe, keine Abdeckung) und B-47
(bewacht einen stillgelegten Weg); im Paket liefen die übrigen **fünf**. Der Plan
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

## Schätzung

**Nachgetragen am 2026-08-01 bei der Abnahme** – gemessen, nicht vorhergesagt. Die Sammel-Story hat die Stufe
`geschaetzt` übersprungen, weil sie selbst **nichts baut**: die Schätzungen leben in den fünf Paket-Stories,
und genau das verlangt ihr eigenes Akzeptanzkriterium 2 („kein Punkt ist nur hier abgehakt").

**L** · `wo: beides` · keine Migration · kein Vertragsbruch. Das L ist die **Summe** der Etappen
(XS + S + S + M + M plus das nicht aus einer Story entstandene E4), nicht eigener Umfang – **XL** gibt es
nicht, „dann wird geteilt", und geteilt *war* das Paket von Anfang an. `wo: beides` fällt aus der Mischung:
E1–E3 Backend, E4–E6 Frontend.

**Testweg** – der einzige Punkt, an dem diese Story selbst etwas zu prüfen hatte, sind die drei Nähte. Alle
drei haben gehalten und sind je in ihrer Etappe belegt: die geteilte Konstante `FullRunTouchedActions` (E1/E2),
das nicht byte-stabile `openapi/v1.json` (E3) und das gemeinsame Lockfile (E4, einmal statt zweimal angefasst).

## Verlauf

- **2026-08-01** — angelegt: sieben Stories zu einem Paket gebündelt, Plandokument geschrieben, Backend- und
  Frontend-Dev-Sicht eingeholt. Zwei Stories aus dem Paket geschoben (B-27, B-47), eine Etappe neu erfunden
  (Werkzeugkette Frontend), ein roter E2E-Nachtlauf gefunden.
- **2026-08-01** — gegrillt: acht Entscheidungen im Plandokument. Neu abgespalten
  [B-53](B-53-wizard-doppelklick.md); [B-26](B-26-e2e-in-ci.md) und [B-27](B-27-testsuite-grenzfaelle.md)
  neu zugeschnitten, [B-47](B-47-deploy-artefakt-smoke.md) mit Eintrittsbedingung versehen.
- **2026-08-01** — **abgenommen.** Beide Akzeptanzkriterien erfüllt. AK 1: alle Etappen des Plandokuments sind
  abgenommen und mit Commit belegt – E0 `99a3720`+`86570aa`, E1 `8081362`, E2 `b229b1c`+`a4be490`,
  E3 `9aac8b1`+`7306f05`, E4 `30f2067`, E5/E5' `7891485`+`1c90710`, E6 `9f9c185`+`34f82b1`. AK 2: der Zustand
  steht je in der eigenen Story; B-26/B-40/B-41/B-42/B-43 tragen ihn selbst.
  Was das Paket über seinen Auftrag hinaus gebracht hat, in der Reihenfolge der Schwere: **E3s Tor war für
  Antwort-Formen halb blind** (167 von 323 Operationen ohne Erfolgsantwort – gefunden erst, als E6 Typen daraus
  erzeugte), die **Doppelklick-Lücke** im Schreib-Primitiv hinter 23 Bildschirmen, und **zwei Stellen, an denen
  ein `reload` alle Listenzeilen aushängte** (B-54). Dreimal war eine *eigene* Zahl oder Zusicherung falsch und
  fiel nur durch eine Gegenprobe auf – fünf statt sechzehn Knöpfe, „34 von 86" statt 40, und eine AK, die vor
  der Reparatur ebenso grün war. Das ist der Ertrag des Pakets, nicht sein Nebenschaden.
  Offen und **nie Etappe**: [B-27](B-27-testsuite-grenzfaelle.md) und [B-47](B-47-deploy-artefakt-smoke.md),
  beide mit Grund unter „Nicht im Paket". Erzeugt: B-53 und B-54 (abgenommen), B-55…B-61, entblockt B-24.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob alle Etappen E0–E6 im referenzierten
  `testabdeckung-plan.md` weiterhin als abgenommen markiert sind und die Deckelkonstante unverändert
  ist — hält (`docs/testabdeckung-plan.md` Zeilen 117,134,151,156,209,320 alle „abgenommen",
  `EndpointCoverageGuard.FullRunTouchedActions = 263` unverändert). Kein Fund.
