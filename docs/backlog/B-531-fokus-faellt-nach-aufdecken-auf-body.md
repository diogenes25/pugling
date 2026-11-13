---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Fokus nach Aufdecken]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Review zu B-526, Etappe 2 (frontend-reviewer, 2026-11-13) – bestand schon vor B-526"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-531 · Im Üben fällt der Fokus nach „Aufdecken“ und nach einer Selbsteinschätzung auf `body`

Der Fokus-Effekt in `SohnPractice.tsx` setzt den Fokus nur, wenn die Karte aus dem Blatt oder aus dem Laden kommt.
Drückt das Kind „Aufdecken“, ersetzt die App den fokussierten Knopf durch „Nochmal“ / „Gewusst!“; nach dem Urteil
kommt die nächste Karte. In beiden Fällen verschwindet der fokussierte Knopf, und der Fokus fällt auf `body` – ein
Kind an der Tastatur oder mit Screenreader verliert die Stelle. Dieselbe Fehlerklasse wie B-439, dort für das
Rundenende behoben. Nicht in B-526 mitgenommen, weil es vorher schon so war und das Ziel jener Story ohne es erfüllt ist.

## User Story

Als **Sohn**, der an der Tastatur (oder mit Screenreader) übt, möchte ich nach „Aufdecken“, nach „Nochmal“/„Gewusst!“
und nach dem „Weiter“ einer Kennenlernkarte mit dem Fokus dort stehen, wo ich als Nächstes handle, **damit** ich die
Runde ohne Maus durchspielen kann und die Stelle nicht verliere.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- Der Fokus-Effekt `frontend/src/sohn/SohnPractice.tsx:165-180` hängt nur an `[phase]` und fokussiert die Antwortzone
  ausschließlich beim Wechsel `blatt → front` und `loading → front` (mit `focusAfterLoad`), dazu Rundenende, Leer und
  Fehler. `:153` fokussiert „Weiter“ auf dem Blatt.
- **Aufdecken:** `onReveal` setzt `phase = "back"` (`SohnPractice.tsx:466`). `SelfAssessAnswer`
  (`frontend/src/sohn/SohnTest.tsx:264-294`) rendert vor dem Aufdecken nur „Aufdecken“ (`:278-281`), danach
  „Nochmal“/„Gewusst!“ (`:293-294`) – der fokussierte Knopf wird ersetzt. Für `front → back` hat der Effekt keinen Zweig.
- **Selbsteinschätzung:** `judge()` hält dort nicht an (`SohnPractice.tsx:209-211`, `halted` bleibt `false`) und ruft
  `next()` (`:261`), das `phase = "front"` und `idx + 1` setzt (`:283-284`). Für `back → front` gibt es keinen Zweig.
- **Kennenlernkarte „Weiter“** (`SohnPractice.tsx:459`): `next()` wechselt nur `idx`, `phase` bleibt `front` – der Effekt
  läuft gar nicht. Ist die nächste Karte getippt, ersetzt `Antwortflaeche` den Knopf, und der Fokus fällt ebenfalls weg.
  Dieser dritte Weg steht nicht in der Idee, ist aber dieselbe Lücke.
- Bestehende Fokus-Fälle in `frontend/src/sohn/SohnPractice.test.tsx`: `:285-306` und `:311-329` (Blatt → nächste
  Karte), `:394-417` (Rundenende), `:472-536` (nach „Jetzt abfragen“). Für Aufdecken, Selbsteinschätzung und
  Kennenlernkarte gibt es keinen.

## Die echte Lücke

Der Effekt kennt die Übergänge, die frühere Stories behoben haben, nicht die Regel dahinter: **verschwindet das
fokussierte Element durch eine Handlung des Kindes, geht der Fokus an das erste Bedienbare der Antwortzone.** Drei Wege
(Aufdecken, Urteil, Kennenlern-„Weiter“) verletzen sie.

## Offene Punkte

1. ~~Wohin nach „Aufdecken“: auf „Nochmal“ (erster Knopf) oder auf „Gewusst!“?~~ → Entscheidung 1.
2. ~~Die Kennenlernkarte (`front → front`) mitnehmen?~~ → Entscheidung 2.
3. ~~Dieselbe Lücke im Test (`SohnTest.tsx`, gleicher `SelfAssessAnswer`) mitnehmen?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Nach „Aufdecken“ steht der Fokus auf dem ersten freigegebenen Knopf der Antwortzone („Nochmal“).** Dieselbe Regel
   wie überall im Effekt (`input, button:not([disabled])`), keine Sonderregel. *Begründung:* „Gewusst!“ vorzuwählen hieße,
   Enter zum bequemen Weg zur Selbstbelohnung zu machen – das widerspricht dem Kern (der Vater erzwingt Lernen).
   *Kosten:* wer „Gewusst!“ meint, braucht einmal Tab.
2. **Jeder Kartenwechsel durch eine Handlung des Kindes fokussiert die neue Karte**, auch `front → front` (der Effekt
   hört zusätzlich auf `idx`). Ausgenommen bleibt der erste Aufbau nach dem Laden ohne `focusAfterLoad` (B-443: keine
   ungefragt aufspringende Tastatur). *Begründung:* dieselbe Fehlerklasse, eine Regel statt drei Zweige. *Kosten:* ein
   weiterer Testfall.
3. **Der Test (`SohnTest.tsx`) bleibt außerhalb.** Eigener Bildschirm mit eigenem Fokus-Weg; beim Bau prüfen und, wenn
   dort dieselbe Lücke steht, als eigene Story anlegen (README → „Ein Fund beim Bauen wird eine eigene Story“).
   *Kosten:* eine mögliche Lücke im Test bleibt bis dahin offen.

## Akzeptanzkriterien

1. Nach „Aufdecken“ auf einer Selbsteinschätzungs-Karte ist `document.activeElement` der Knopf „Nochmal“.
   *Rot, wenn* der Effekt den Wechsel `front → back` wieder ignoriert.
2. Nach „Gewusst!“ steht der Fokus im ersten Bedienbaren der nächsten Karte (Feld oder Knopf).
   *Rot, wenn* der Zweig `back → front` fehlt.
3. Nach „Weiter“ auf einer Kennenlernkarte, der eine getippte Karte folgt, steht der Fokus im Textfeld.
   *Rot, wenn* der Effekt wieder nur an `phase` hängt (dann läuft er bei `front → front` nicht).
4. Beim ersten Laden der Seite springt weiterhin kein Fokus in die Karte. *Rot, wenn* der neue `idx`-Zweig auch den
   ersten Aufbau trifft (bestehende Fälle zu B-443 decken nur den Weg nach „Jetzt abfragen“; ein Fall für den ersten
   Aufbau kommt dazu).

## Schätzung

**Größe: XS** – ein Effekt in `SohnPractice.tsx` (Abhängigkeit `idx`, Zweige `front → back`, `back → front`,
Kartenwechsel bei `front`) und vier Fälle in `SohnPractice.test.tsx`. `wo: frontend`, `migration: nein`,
`vertragsbruch: nein`.

**Risiken:** Die Ruhezeit nach dem Blatt (`NACH_BLATT_MS`, `SohnPractice.tsx:18`, `:270-274`) darf nicht umgangen werden – der
Fokus ist kein Klick, darum unkritisch. Auf dem Handy kann der Fokus ins Feld die Bildschirmtastatur öffnen; das tut der
Weg `blatt → front` heute schon, also kein neues Verhalten.

**Angriffsplan:** (1) Vier Fälle in `SohnPractice.test.tsx` schreiben, AK 1–3 gegen den Ist-Code rot sehen; (2) Effekt
umbauen (`prevIdx`-Ref neben `prevPhase`); (3) grün, je Fall die rote Probe am benannten Zweig; (4) E2E
`lektion.spec.ts` und `erstbegegnung.spec.ts` als Regressionszeugen.

**Testweg:** Vitest `frontend/src/sohn/SohnPractice.test.tsx` (AK 1–4); E2E `frontend/e2e/lektion.spec.ts` als
Rollengang-Zeuge des Sohns.

## Verlauf

- 2026-11-13 · `idee` – Fund beim Review von B-526; Prio P3 ist ein Vorschlag.
- 2026-11-13 · `ausformuliert` – am Code belegt; dritter Weg (Kennenlern-„Weiter“, `front → front`) gefunden.
- 2026-11-13 · `gegrillt` – Entscheidungen 1–3; Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13).
- 2026-11-13 · `geschaetzt` – XS, frontend, ohne Migration und Vertragsbruch.
- 2026-11-13 · `in-arbeit` – Bau im Backlog-Lauf (Frontend), Worktree `backlog-lauf`.
- 2026-11-13 · `in-arbeit` – gebaut: der Fokus-Effekt in `SohnPractice.tsx` hängt an `[phase, idx]` (`prevIdx` neben
  `prevPhase`) und fokussiert das erste Bedienbare der Antwortzone bei jedem Wechsel von einer Karte aus (`front → back`,
  `back → front`, `blatt → front`, `front → front` mit neuem `idx`); der erste Aufbau ohne `focusAfterLoad` bleibt
  ausgenommen. Vier Fälle in `SohnPractice.test.tsx` („Fokus beim Kartenwechsel“), AK 1–3 gegen den Ist-Code rot gesehen,
  AK 4 dort grün (Absicherung des neuen Zweigs). Rote Probe (`mutant-probe.py`, Build `echo ok`, Test Vitest der Datei):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | AK1: `toCard` nur bei `front` (front → back ignoriert) | SohnPractice.tsx:172 | red | red |
  | AK2: `back` fehlt in `fromCard` | SohnPractice.tsx:170 | red | red |
  | AK3: Effekt nur an `[phase]` | SohnPractice.tsx:187 | red | red |
  | AK3: `changed` ohne `idx` | SohnPractice.tsx:171 | red | red |
  | AK4: `fromCard = true` (erster Aufbau fokussiert) | SohnPractice.tsx:170 | red | red |

  Gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13; Grundlinie danach grün. Entscheidung 3 geprüft:
  `SohnTest.tsx` hat **keinen** Fokus-Effekt, `onReveal` setzt nur `revealed` (`:241`) – dieselbe Lücke im Test ist
  wahrscheinlich und gehört als eigene Story angelegt. E2E-Zeugen: `lektion.spec.ts`, `erstbegegnung.spec.ts`,
  `karten-umdrehen.spec.ts`.
- **2026-11-13** — **abgenommen** mit Commit `699b02a7` (Sprint 69, Backlog-Lauf 2026-11-13). Verifikation: Backend 1555/1555 grün (dotnet test Pugling.sln -c Release), Vitest 937/937, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `frontend-reviewer` ohne blockierenden Befund; zwei Sollte-Funde zu B-532 im selben Sprint behoben. Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184) nach dem letzten Edit – lektion, erstbegegnung, karten-umdrehen, full-flow, test-durchgefallen-ueben, uebung-abbruch, pfad, bonus, artikel-pflicht: **10 passed**. Vater und Creator sind Regressionszeugen (Suite grün, kein Pfad ihrer Ebene im Diff außer der Vater-Wand von B-433).
