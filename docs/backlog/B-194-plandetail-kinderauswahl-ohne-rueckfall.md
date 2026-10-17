---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [achter Fall ohne ReferenceSelect, children.error im Plan-Detail]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-01 zu B-172
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-17: Sonderoption, string/number-Konvertierung und dto.childId-Bedingung nachgerechnet; Ladefenster zeigt (nicht geladen) auch im Erfolgsfall → B-229"
---

# B-194 · Die Kind-Auswahl im Plan-Detail hat die Regel von B-172 nicht geerbt

Der **achte** Fall derselben Falle — B-172 hat sieben umgestellt und ihren Umfang auf Fach/Verlag/Reihe/
Unit ausgezählt. Dieser hier lag außerhalb.

## User Story

Als **Vater** möchte ich beim Bearbeiten eines Plans sehen, dass die Kinderliste nicht geladen werden
konnte, statt ein leeres Feld für einen Plan zu sehen, der ein Kind hat.

## Ist-Stand am Code

`VaterPlanDetail.tsx:137-139` rendert `children.data?.map(...)` **roh**: keine Leer-Option, keine
Rückfall-Option für den geladenen Wert, und `children.error` wird in der ganzen Datei nirgends gerendert.
Fällt der Kinder-Abruf aus, steht das Feld leer — und der Ausfall ist unsichtbar. Genau die Klasse
„ungerenderter Fehlerzustand", die B-172s Akzeptanzkriterium 5 an fünf anderen Stellen geschlossen hat.

**Was es NICHT tut, und das ist nachgerechnet statt angenommen:** Es zerstört nichts. Ohne Optionen kann
kein `change` feuern, `form.childId` bleibt auf `plan.childId`, und `dto.childId` wird bei `:126` nie
gesetzt. `api.children()` ist ungepaged (`ChildrenController.cs:36` liefert `IEnumerable` ohne Paging) —
eine *teilweise* Liste kann es also gar nicht geben. Der Schaden ist damit „unsichtbarer Ausfall", nicht
„stiller Datenverlust" wie bei B-172.

**Nachgesehen am 2026-10-17, was das Bauteil heute kann und was die Seite schon hat:**

- `ReferenceSelect` (`frontend/src/components/ReferenceSelect.tsx:71-82`) rendert **immer** eine Leer-Option
  `<option value="">{emptyLabel}</option>`; `emptyLabel` ist Pflicht. Ein Plan hat aber immer ein Kind
  (`PlanResponse.childId: number`, nicht nullable, `contract.ts:24842`) — eine Leer-Option wäre hier ein
  wählbarer Zustand, den der Vertrag nicht kennt.
- Die Seite lädt das Kind bereits für Name und Link: `VaterPlanDetail.tsx:18-19` (`child = useAsync(() =>
  api.child(plan.data.childId))`). Der Name für die Rückfall-Option ist also da, ohne Vertragsänderung
  (`PlanResponse` trägt keinen `childName`, `contract.ts:24838-24860`).
- `PlanEditForm` (`:112`) bekommt den Plan, nicht das Kind — der Name muss als Prop mitreisen.

**`entgangen_bei` bleibt leer, und das ist keine Nachlässigkeit:** Die Zeile liegt außerhalb dessen, was
B-172 angefasst und ausgezählt hat. Sie ist keiner Abnahme entgangen, sondern war nie im Umfang.

## Die echte Lücke

Ein roher `<select>` an einer Stelle, an der die Regel acht von acht Mal gelten soll — plus ein nie
gerenderter `children.error`. Das Bauteil passt heute nicht ganz, weil es eine Leer-Option erzwingt, die
ein Pflichtfeld nicht haben darf.

## Offene Punkte

1. ~~`ReferenceSelect` einsetzen oder nur `children.error` rendern?~~ → Entscheidungen 1 und 2.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **`ReferenceSelect` einsetzen — und `emptyLabel` wird optional:** ohne Beschriftung keine Leer-Option.
   *Begründung:* B-172, Entscheidung 2 — der Wert des Bauteils liegt darin, dass die Regel nur noch an einer
   Stelle steht; ein achter roher `<select>` mit handgeschriebener Rückfall-Option wäre die neunte Kopie.
   Ein Pflichtfeld ist der einzige Grund, die Leer-Option wegzulassen, und er ist am Vertrag ablesbar.
   *Kosten:* eine optionale Prop (additiv, sieben Aufrufstellen unverändert), ein Testfall im
   `ReferenceSelect.test.tsx`, der die Abwesenheit der Leer-Option prüft.
2. **`children.error` wird als Banner über dem Formular gerendert — ohne Wiederhol-Knopf.** *Begründung:*
   B-172, Entscheidung 5: von 68 Lesefehler-Bannern bietet keines einen erneuten Versuch; der Weg ist als
   [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md) abgelegt, und hier einen einzelnen zu bauen
   hieße die Zufalls-Inkonsistenz fortzusetzen, die jene Entscheidung gerade vermeidet. *Kosten:* bis B-183
   hilft F5 — aber jetzt weiß der Vater, warum.
3. **Der Kindname reist als Prop `childName` in `PlanEditForm`**, aus dem bereits geladenen `child`.
   *Begründung:* kein zweiter Abruf, keine Vertragsänderung. *Kosten:* Ist auch dieser Abruf gescheitert,
   beschriftet das Bauteil mit `#<id>` (sein Verhalten seit B-172) — wahr, nur weniger hübsch.

## Akzeptanzkriterien

1. Scheitert `GET supervisor/children`, zeigt das Bearbeiten-Formular des Plans das geladene Kind als
   gewählte Option „<Name> (nicht geladen)" und darüber ein Banner mit dem Fehler. *Beleg:* E2E
   `plan-kinderliste-faellt-aus.spec.ts` (`page.route("**/api/v1/supervisor/children", abort)`, Plan über
   `/vater/plan/new` anlegen wie `planbau-ladefenster.spec.ts`).
2. Speichert der Vater in diesem Zustand nur den Titel, trägt der PATCH keine `childId`. *Beleg:* dieselbe
   E2E prüft den Request-Rumpf über `page.waitForRequest`.
3. Das Feld hat **keine** Leer-Option (ein Plan ohne Kind ist nicht wählbar). *Beleg:*
   `ReferenceSelect.test.tsx` („bietet ohne `emptyLabel` keine leere Wahl") und die E2E zählt die Optionen.
4. Im Normalfall (Liste geladen) ist das Feld unverändert: alle Kinder wählbar, das geladene vorausgewählt.
   *Beleg:* bestehende E2E, die den Plan bearbeiten (`full-flow.spec.ts`), bleiben grün.

## Schätzung

**Größe: S** — eine optionale Prop im Bauteil (+1 Test), ein `ReferenceSelect` statt eines rohen `<select>`,
ein Banner, eine Prop, eine E2E. Anker: B-01.

**Risiken:** `ReferenceSelect` arbeitet mit `string`-Werten, das Formular hält `childId: number` —
Konvertierung an der Aufrufstelle (`String(form.childId)` / `Number(v)`), wie bei den sieben anderen.

**Angriffsplan:** (1) `emptyLabel?` im Bauteil + Test, (2) `VaterPlanDetail`: Prop, Bauteil, Banner,
(3) E2E.

**Testweg:** `vitest run src/components/ReferenceSelect.test.tsx`, `playwright test
e2e/plan-kinderliste-faellt-aus.spec.ts`, dann die ganze Suite.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-172 (Nachtlauf). Der Nachschau-Agent hat alle 103
  `<select>` des Frontends gesichtet; dies ist der einzige verbliebene **bearbeitende** Fall ohne die
  Regel. Die übrigen sind Anlege-, Filter- oder Auswahlfelder ohne geladenen Wert bzw. laufen auf
  Konstantenlisten — namentlich geprüft und im Protokoll benannt.
- **2026-10-17** — **ausformuliert → gegrillt** im Nachtlauf (Sprint 1), autonom unter Freigabe 1: der
  offene Punkt war eine Frage an den Code (Pflicht-Leer-Option des Bauteils, `childName` nicht im
  Vertrag) und ist dort beantwortet. Akzeptanzkriterien mit Belegen ergänzt.
- **2026-10-17** — **gegrillt → geschaetzt**: S, frontend, keine Migration, kein Vertragsbruch.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 1. Rollengang: Vater auf `/vater/plan/2` (Kinderliste abgeschossen → „Bearbeiten" → Feld zeigt „Sohn (nicht geladen)" als einzige Option, Banner „Kinder: …" darüber). Rote Probe: ReferenceSelect.test 1/1 (drei statt zwei Optionen). E2E `plan-kinderliste-faellt-aus.spec.ts` grün; ihre PATCH-Zusicherung ist als Absichtserklärung beschriftet (galt auch vorher – Reviewer-Fund). Belege: tsc -b sauber · vitest run 380/380 (47 Dateien) · playwright test 69/70 im vollen Lauf – der eine rote Fall (`lehrwerke.spec.ts`) war eine Nebenwirkung der neuen `fachlehrer-faecher-fallen-aus.spec.ts` in der geteilten DB (aktives Englisch-Profil ohne Grenzen passt zu jedem Kind), behoben (Profil wird stillgelegt angelegt), danach die betroffenen Specs gemeinsam grün · `frontend-reviewer` gelaufen, **kein Blocker**, ein Fund mit Codewirkung (Leer-Satz und Zählzeile des Trefferbereichs hingen noch an `fehler` statt am Gedächtnis – behoben, Vitest ergänzt) · Skill `web-design-guidelines` (Freigabe 7): ein Fund (fokussierter Kasten ohne projektweiten Fokusring – `.banner[tabindex]:focus-visible` ergänzt) · **Browser-Rollengang in Chrome (Freigabe 6)** gegen Backend mit Wegwerf-DB und `npm run dev`, Abrufe über einen Fetch-Wrapper in der Seite abgeschossen bzw. verzögert, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`. Commit `d834a8e`.
- **2026-10-17** — Nachschau (Nachtlauf): Sonderoption, Konvertierung und `dto.childId` nur bei Änderung – halten. **Ein Fund:** solange die Kinderliste erst lädt, ist „(nicht geladen)" die einzige Option, auch im Erfolgsfall ([B-229](B-229-nicht-geladen-waehrend-des-ladens.md)).
