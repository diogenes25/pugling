---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/student]
aliases: [Deaktivierter Plan zeigt englische Serverzeile]
status: in-arbeit
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf I, Teil 2 in Lauf J (docs/pm-sitzung-2026-10-28-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-326]
wartet_auf: ""
---

# B-330 · Ein deaktivierter Plan zeigt dem Kind nach einem Reload die englische Serverzeile

[B-326](B-326-ueben-und-klausur-nehmen-ungepruefte-plan-id.md) nennt in seiner Problembeschreibung „Löschen **oder
Deaktivieren**“ und in der User Story „wenn mein Plan inzwischen weg ist“, behandelt aber nur `404`. Ein deaktivierter
Plan antwortet `403 plan_inactive` (`PositionPracticeController.cs:71` u. a., `PositionTestsController.cs:138` u. a.).
Der Code steht weder in `GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts`) noch in `PROBLEM_KEYS`
(`frontend/src/lib/chromeText.tsx`) – außer in `contract.ts` kommt er im Frontend nirgends vor (`grep`, Lauf J).

**Szenario** (aus Code gelesen): der Vater deaktiviert den Plan, das Kind lädt `/sohn/practice/<id>` neu → die deutsche
Arcade zeigt „This study plan is not currently active. Ask your parent. (Ref: …)“ – genau das Bild, das B-326 beheben
sollte.

Zu klären beim Ausformulieren: deutsche/englische Fassung für `plan_inactive` (Vater-Web und Arcade) **oder** wie bei
404 zur Basis umleiten; vermutlich beides nötig, weil `plan_inactive` auch mitten im Spiel kommen kann.

Mitgenommene 🟢-Hinweise derselben Nachschau (ohne Zählung, beim Ausformulieren mit prüfen):
`queryByText(/Study plan not found/)` in `SohnTest.test.tsx`/`SohnPractice.test.tsx:110` kann allein nie fallen
(Absichtserklärung beschriften oder streichen); der 404-Fang in `SohnTest.tsx:55-60` umfasst auch `submitTest`/`overview`.

## User Story

Als Kind möchte ich, wenn mein Plan gerade nicht gespielt werden darf, einen Satz in meiner Sprache lesen, der mir sagt,
was los ist und wen ich fragen kann – nicht eine englische Serverzeile mit Referenznummer.

## Ist-Stand (Lauf J, am Code belegt)

- `plan_inactive` (`403`) meldet der Server nur einem **Kind** und aus drei Gründen: Plan deaktiviert, abgelaufen/noch
  nicht gestartet, oder nicht der eine aktive (`PositionPlayService.PlanPlayableForChild`, Aufrufe
  `PositionPracticeController.cs:70`, `PositionTestsController.cs:138` u. a.).
- Arcade-Fehler laufen über `useErrorMessage` (`chromeText.tsx`): eigener Satz nur für Codes in `PROBLEM_KEYS`, sonst auf
  Deutsch `errorMessage` (`GERMAN_PROBLEM_TEXT` in `api.ts`) – für `plan_inactive` gab es weder das eine noch das andere.
- `SohnPractice`/`SohnTest` zeigen den Start-Fehler mit einem Knopf zur Basis (`progress.toHome`).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Eigener Satz je Sprache, keine Umleitung zur Basis.** Anders als bei `404` (B-326) gibt es den Plan noch, und der Code
   kommt auch mitten im Spiel – dort wäre ein stilles Umleiten ein Rauswurf ohne Grund. Der Knopf zur Basis steht schon
   neben der Meldung. *Kosten:* nach einem Reload sieht das Kind erst die Meldung, dann einen Klick zur Basis.
2. **Der Satz nennt keinen der drei Gründe** („gerade nicht dran … frag deine Eltern, welcher Plan jetzt gilt“): das Kind
   kann keinen davon selbst ändern, und alle drei löst der Vater. *Kosten:* weniger genau als der Server weiß.
3. Deutsch wortgleich in `GERMAN_PROBLEM_TEXT` und `problem.plan_inactive` (die bestehende Regel, gehalten von
   `arcadeSpielweg.test.tsx`).
4. Von den mitgenommenen 🟢: leere Zusicherung beschriftet (Absichtserklärung); der 404-Fang um `submitTest` in
   `SohnTest.tsx` bleibt zurückgestellt (selten, ändert Verhalten, eigene Story wert, falls er je auftritt).

5. (aus dem Review) **Mitten in einer Übungsrunde endet die Runde** mit dem Satz, statt die Ablehnung zu verschlucken und
   weiterblättern zu lassen (`SohnPractice.judge`); in der Klausur verschwindet „Weiter versuchen“, weil es nur dieselbe
   `403` holte – auch nach einem Neustart aus dem Ergebnis (`start()` vergisst den abgegebenen Versuch).
   *Kosten:* eine Runde, die bis eben zählte, bricht ab; das ist ehrlich, denn ab hier zählt nichts mehr.

## Akzeptanzkriterien

1. `plan_inactive` liest das Kind auf Deutsch und auf Englisch als eigenen Satz, ohne Ref-Nummer.
2. Der deutsche Satz ist im Vater-Web und in der Arcade derselbe.
3. Eine Übungsrunde, in der der Plan nicht mehr dran ist, endet mit dem Satz; die Klausur bietet dann kein „Weiter
   versuchen“ an (Antwort, Fortsetzen, Neustart).

## Schätzung

`XS`, Frontend. Testweg: Komponententest in `arcadeSpielweg.test.tsx` (rote Probe, Mutante), Vitest, E2E-Regression.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show b64006c`) in Lauf J; Fehlen von
  `plan_inactive` im Frontend per `grep` bestätigt, Szenario nicht gemessen.
- **2026-10-28** — Lauf J, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe **1 failed**
  (erwartet 1; gemessen „This study plan is not currently active. Ask your parent. (Ref: trace-4)“ statt des deutschen
  Satzes). Mutante „Arcade-Schlüssel gestrichen“ **1 failed** an der englischen Zeile. Vitest **570/570**, `tsc -b` sauber.
- **2026-10-28** — Review (`frontend-reviewer`): 🟡 `judge` verschluckte `plan_inactive` mitten in der Runde (Entscheidung 1
  behauptete das Gegenteil) → Runde endet; rote Probe **1 failed**, Mutante „läuft trotzdem weiter“ **1 failed**. 🟢
  „Weiter versuchen“ nach `plan_inactive` → ausgeblendet; rote Probe **1 failed** (Z. 75, Knopf da). 🟢 Englischer Satz
  natürlicher, Plural wie im Deutschen; Testkommentar präzisiert. Delta-Review: 🟡 Neustart aus dem Ergebnis trug den alten
  Versuch → `start()` setzt ihn zurück; rote Probe **1 failed** (Z. 85, Knopf da); 🟢 `resume()`-Zweig ohne Fall → Fall,
  Mutante **1 failed** (Z. 101). Zurückgestellt (🟢, ohne Zählung): `reshuffleImage` beendet die Runde nicht (Toast, die
  nächste Bewertung beendet sie); doppelte Prüfung `isPlanInactive`/Inline. Vitest **574/574**, `tsc -b` sauber.
