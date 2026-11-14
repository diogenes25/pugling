---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Planbau verwirft Auswahl beim Umsortieren]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-408, Nebenbefund beim Schätzen (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-540 · Im Planbau verwirft ein Sortierwechsel die gewählte Übung

Im Planbau (`frontend/src/vater/PlanPositions.tsx`) steckt „beste zuerst“ im Auswahl-Schlüssel `filterKey`
(`PlanPositions.tsx:498`, gebaut aus `planbauKriterien(filter, besteZuerst)` `:480`, das `besteZuerst` bei `:863`
mitnimmt; die Auswahl leert `auswahlNachFilterwechsel` im Effekt `:502-523`). Wer eine Übung gewählt hat und dann nur die Sortierung umstellt, verliert die Auswahl,
obwohl die Treffermenge dieselbe bleibt. Abgetrennt aus [B-408](B-408-assistent-zeigt-keine-bewertung.md): dort
entsteht die reine Funktion `auswahlNachUmsortieren` in `wizardSearch.ts` für den Assistenten; diese Story nimmt sie
für den Zwilling im Planbau (Angriffsplan-Schritt 3 von B-408).

Abgetrennt, weil eine parallele Sitzung `PlanPositions.tsx` gerade für B-534 umbaut und ihre Arbeit nicht committet
ist – ein Eingriff jetzt erzeugte einen Merge-Konflikt in fremder Arbeit.

## User Story

Als *Vater* möchte ich im Planbau die Trefferliste auf „beste Bewertung zuerst“ umstellen können, ohne dass die Übung,
die ich gerade gewählt und deren Einstellungen ich schon angepasst habe, stumm aus der Auswahl fällt – damit Sortieren
ein Hilfsmittel beim Vergleichen ist und keine Falle.

## Ist-Stand am Code (2026-11-14)

- Zustand der Reihenfolge: `besteZuerst` (`frontend/src/vater/PlanPositions.tsx:466`), Auswahlfeld „Trefferliste
  sortieren“ `:583-588`.
- Eine Liste für zwei Zwecke: `kriterien = planbauKriterien(filter, besteZuerst)` (`:480`) ist zugleich Abhängigkeit der
  Abfrage (`useAsync`, `:481-482`) und – als `JSON.stringify` – der Auswahl-Schlüssel `filterKey` (`:498`).
  `planbauKriterien` nimmt `besteZuerst` ausdrücklich auf (`:857-866`, Kommentar `:862`: „eine andere Sortierung zeigt
  andere Zeilen“).
- Der Effekt `:502-523` ruft `auswahlNachFilterwechsel` (`wizardSearch.ts`) bei jedem Wechsel von `filterKey` – also auch
  beim reinen Umsortieren – und leert `exerciseId` mit Hinweis „… weil sich die Suche geändert hat“. Die Meldung ist
  dabei sachlich falsch: die Suche ist dieselbe.
- Der Zwilling ist schon richtig: `wizardFilterKey` (`wizardSearch.ts:67-70`) schneidet `sort`/`dir` heraus,
  `VaterWizard.tsx:152-153` lädt bei `besteZuerst` neu, ohne den Schlüssel zu ändern.
- Die Regel für den Planbau liegt fertig und getestet bereit: `auswahlNachUmsortieren` (`wizardSearch.ts:117-128`,
  Tests `wizardSearch.test.ts:191-202`) – im Planbau nirgends aufgerufen (`PlanPositions.tsx:14` importiert nur
  `auswahlNachFilterwechsel`).
- Vitest `PlanPositions.test.ts:291` pinnt, dass `planbauKriterien` sich mit der Sortierung ändert – das ist für die
  **Abfrage** richtig und bleibt; es ist nur die falsche Quelle für den Auswahl-Schlüssel.
- Komponententest-Gerüst mit gemockter API existiert: `PlanPositionsAdd.test.tsx`. E2E der Sortierung:
  `e2e/planbau-bewertung.spec.ts:81` (stellt um, prüft aber keine Auswahl).

## Die echte Lücke

Die Abfrage und die Auswahl hängen an **einem** Schlüssel, obwohl sie Verschiedenes meinen: die Abfrage muss bei einer
anderen Sortierung neu laufen, die Auswahl nur bei einer anderen Treffermenge fallen. Im Planbau ist die Auswahl eine
Einzelauswahl auf der sichtbaren Seite – nach dem Umsortieren darf sie bleiben, wenn die Übung noch auf der Seite steht,
und muss fallen (mit Hinweis), wenn nicht (B-167: die Auswahl zeigt nie auf eine unsichtbare Übung).

## Offene Punkte

1. ~~Wie bekommt der Planbau einen Schlüssel ohne Sortierung?~~ → Entscheidung 1.
2. ~~Wann greift `auswahlNachUmsortieren` – nur nach einem Sortierwechsel oder nach jeder frisch geladenen Seite?~~ →
   Entscheidung 2.
3. ~~Was ist mit den Einstellungen, die die Übung beim Anklicken ins Formular geschrieben hat?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidungen im Auftrag des Nutzers (Backlog-Lauf 2026-11-14); die Richtung gab B-408 Entscheidung 3 vor.

1. **Zwei Schlüssel aus einer Quelle.** `planbauKriterien` bleibt die Abhängigkeitsliste der Abfrage (mit Sortierung);
   neu ist `planbauAuswahlSchluessel(filter)` in `PlanPositions.tsx` = `JSON.stringify(planbauKriterien(filter, false))`,
   der Schlüssel für `filterKey` und `useGemerkterFehler`. So kann kein Feld des Filters an nur einer der beiden Stellen
   fehlen (der Grund, warum es `planbauKriterien` gibt). *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung:
   dieselbe Trennung wie im Zwilling (`wizardFilterKey`). Kosten: eine weitere exportierte Funktion; der gemerkte
   Suchfehler (`useGemerkterFehler`, `:561`) überlebt künftig einen Sortierwechsel – gewollt, die Suche ist dieselbe.
2. **`auswahlNachUmsortieren` greift nach jeder frisch geladenen Seite derselben Suche**, nicht nur nach einem
   Sortierwechsel: ein Effekt auf `exercises.data`, der nur läuft, wenn die Daten zum eingestellten Stand gehören
   (`!exercises.stale`), und `exerciseId` gegen die geladenen Ids prüft. *Stakeholder-Entscheidung im Auftrag des
   Nutzers.* Begründung: auch „Erneut suchen“ (`reload`) kann eine Seite liefern, aus der die gewählte Übung
   verschwunden ist (gelöscht, zurückgezogen) – dieselbe Regel, kein Sonderfall „war es ein Sortierwechsel?“. Nach einem
   Filterwechsel ist die Auswahl schon leer, der Effekt tut dort nichts. Kosten: keine sichtbaren; der Hinweistext spricht
   vom Umsortieren, auch wenn ein Reload der Anlass war (selten, und die Aussage „steht nicht mehr in der Liste“ stimmt).
3. **Bleibt die Übung, bleiben die Formularwerte; fällt sie, bleiben sie auch.** Heute bleiben beim Leeren nach
   Filterwechsel die aus der Übung übernommenen Werte im Formular stehen; das gilt unverändert. *Stakeholder-Entscheidung
   im Auftrag des Nutzers.* Begründung: der Absende-Knopf ist ohne Auswahl ohnehin gesperrt (`:703`), und die nächste
   gewählte Übung überschreibt ihre Werte selbst. Kosten: keine.

## Akzeptanzkriterien

1. `planbauAuswahlSchluessel(filter)` ist für `besteZuerst` an und aus gleich und ändert sich mit jedem Feld von
   `ExerciseFilter` (Vitest in `PlanPositions.test.ts`). **Rot**, wenn `filterKey` wieder aus `kriterien` (mit Sortierung)
   gebaut wird bzw. ein Filterfeld im Schlüssel fehlt.
2. Komponente: Übung A gewählt, Sortierung umgestellt, die neue Seite (Mock) enthält A ⇒ A bleibt gewählt, kein Hinweis,
   „Übung hinzufügen“ bedienbar (Vitest in `PlanPositionsAdd.test.tsx`). **Rot**, wenn der Schlüssel die Sortierung
   wieder enthält (heutiger Defekt).
3. Komponente: Übung A gewählt, Sortierung umgestellt, die neue Seite enthält A **nicht** ⇒ Auswahl leer, Hinweis
   „Auswahl zurückgesetzt (1 Übung), weil sie nach dem Umsortieren nicht mehr in der Liste steht.“ sichtbar. **Rot**, wenn
   der Effekt aus Entscheidung 2 fehlt (dann zeigte die Auswahl auf eine unsichtbare Übung, B-167).
4. Filterwechsel leert die Auswahl weiter mit dem bisherigen Hinweis (bestehende Tests bleiben grün; Regression).
5. E2E `planbau-bewertung.spec.ts`: eine Übung wählen, auf „beste Bewertung zuerst“ umstellen, das Radio der Übung ist
   danach weiter `checked` (die Seed-Übungen passen alle auf eine Seite). **Rot** wie AK 2, aber an der laufenden App.

## Schätzung

**Größe: S** – zwei kleine Eingriffe in einer Datei (Schlüssel trennen, ein Effekt), die Regel selbst existiert samt
Tests. Größer als XS, weil zwei Komponententests mit zwei Mock-Seiten und eine E2E-Erweiterung dazukommen.

- **`migration: nein`**, **`vertragsbruch: nein`** – reines Frontend, keine API berührt.

**Risiken:**

- Reihenfolge der Effekte: der Filterwechsel-Effekt (`:502-523`) und der neue Effekt auf `exercises.data` dürfen sich nicht
  gegenseitig den Hinweis überschreiben – der neue setzt `auswahlHinweis` nur, wenn `auswahlNachUmsortieren` einen liefert.
- `exercises.stale` ist während des Nachladens wahr; der neue Effekt muss es abwarten, sonst prüfte er gegen die alte Seite.
- **Konflikt mit B-16 und B-140**: alle drei ändern `PlanPositions.tsx` (B-140 die Radio-Zeile `:620-660`, B-16 den
  `onChange` derselben Zeile). Nacheinander bauen, nicht parallel.

**Angriffsplan (kein Backend-Schritt):**

1. `planbauAuswahlSchluessel` + Vitest (AK 1) – rote Probe: `filterKey` testweise wieder aus `kriterien`.
2. `filterKey` darauf umstellen; Effekt auf `exercises.data` mit `auswahlNachUmsortieren` (AK 2, 3).
3. E2E-Schritt in `planbau-bewertung.spec.ts` (AK 5).

**Testweg:**

- Vitest: `PlanPositions.test.ts` (AK 1), `PlanPositionsAdd.test.tsx` (AK 2–4, je eine rote Probe am richtigen Ort).
- Playwright: `frontend/e2e/planbau-bewertung.spec.ts` erweitert (AK 5); zählt als Rollengang.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus B-408 (Nebenbefund), `wartet_auf` B-534.
- **2026-11-14** — Abgleich Backlog-Lauf: Kern gilt (Sortierung steckt weiter im Schlüssel, `PlanPositions.tsx:498`, `:863`; `auswahlNachUmsortieren` steht in `wizardSearch.ts:117`, im Planbau unbenutzt). Ist-Stand-Zeilen berichtigt; `wartet_auf` geleert, weil B-534 den Umbau von `PlanPositions.tsx` committet hat (`a3e5c1ba`).
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert (User Story, Ist-Stand mit Zeilen, Lücke), gegrillt (drei Stakeholder-Entscheidungen im Auftrag des Nutzers: eigener Auswahl-Schlüssel aus `planbauKriterien`, `auswahlNachUmsortieren` nach jeder frischen Seite, Formularwerte bleiben), geschätzt: S, nur Frontend, Konflikt mit B-16/B-140 in `PlanPositions.tsx`.
- **2026-11-14** — Sprint 74, Frontend gebaut: `planbauAuswahlSchluessel(filter)` (ohne Sortierung) speist `filterKey`; neuer Effekt auf `exercises.data` ruft `auswahlNachUmsortieren`, sobald die Seite zum Stand gehört (`frontend/src/vater/PlanPositions.tsx`). Tests: `PlanPositions.test.ts` „Planbau – Auswahl-Schlüssel“ (AK 1, zwei Fälle); `PlanPositionsAdd.test.tsx` „A bleibt gewählt, wenn die umsortierte Seite A enthält“ (AK 2), „die Auswahl fällt mit Hinweis, wenn die umsortierte Seite A nicht enthält“ (AK 3), „ein Filterwechsel leert die Auswahl weiter mit dem bisherigen Hinweis“ (AK 4); E2E `e2e/planbau-bewertung.spec.ts` um Auswahl vor dem Umsortieren und `toBeChecked` danach erweitert (AK 5). Rote Proben für AK 2/3 gemessen.
- **2026-11-14** — **abgenommen** (Sprint 74). volle Suite 1621/1621 (Release), Vitest 1023/1023 (drei volle Läufe in Folge), `tsc -b` grün; Rollengang Vater als Playwright gegen frisch gestartete Server nach dem letzten Edit: 19 passed (`auswahl-nicht-zuweisbar` 3 Fälle, `lernkontrolle-sichtbar`, `planbau-bewertung`, `vater-von-null`, `assistent`, `uebung-teilspeichern`, `interessen`, `bonus-deckel`, `kind-lehrbuch-fach`, `responsive-vater`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `defa3ea0`. Mutante N8b (Schlüssel mit Sortierung) → rot an „A bleibt gewählt …“
  (meine erste Fassung N8 war äquivalent – sie fixierte die Sortierung – und ist kein Beleg). Review-Fund behoben: der
  Hinweis sagt „nach dem Neuladen“ statt „nach dem Umsortieren“, weil der Effekt auch nach „Erneut suchen“ greift.
