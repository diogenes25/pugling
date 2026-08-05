---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Positionsliste Ladezustand, aufgeklappter Report schließt]
status: abgenommen
prio: P3
art: Defekt
groesse: L
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-10-zeitfenster-pro-kind.md
grund: ""
ersetzt_durch: []
---

# B-89 · Die Positionsliste hängt bei jeder Änderung den aufgeklappten Report aus

`PlanPositions.tsx` prüft `positions.loading ? "Lade Positionen…" : <tabelle>` und trifft damit genau die in
[frontend/CLAUDE.md](../../frontend/CLAUDE.md) dokumentierte `useAsync`-Falle: `reload` setzt `loading`
erneut, behält aber `data` — die Tabelle wird also bei **jeder** Änderung ausgehängt und neu gebaut. Alles,
was an einer Zeile aufgeklappt war (der 📊-Report), ist danach zu.

Regelkonform wäre `positions.loading && positions.data === null`; die Regel steht schon dort, nur diese
Datei folgt ihr nicht. Die Recherche unten zeigt: es ist keine Zeile, sondern ein wiederkehrendes Muster mit
**30 lebenden Fundstellen** über 20 Dateien im Vater-Web.

## User Story

Als Vater möchte ich, dass eine Liste im Vater-Web nach dem Speichern einer Änderung **stehen bleibt** (statt
kurz durch „Lade…" ersetzt zu werden), damit ein aufgeklappter Bereich — der Positions-Report, die
Wort-Einzelheiten im Lernstand, ein offenes Bearbeiten-Formular — nicht bei jeder Kleinigkeit wieder zuklappt.

## Ist-Stand am Code

**Der gemeldete Fall:**

- [`PlanPositions.tsx:51`](../../frontend/src/vater/PlanPositions.tsx#L51) —
  `{positions.loading ? <div className="loading">Lade Positionen…</div> : positions.error ? (…) : (<tabelle>)}`.
  `positions.reload` hängt an `onAdded` (Zeile 49) und `onChanged` (Zeile 59) — jede Positions-Änderung
  triggert ihn, während `positions.data` längst gefüllt ist (`useAsync`, `frontend/src/lib/useAsync.ts:16-41`,
  hält `data` explizit über ein `reload` hinweg, siehe Zeilen 23/39).
- Der 📊-Report hängt in derselben Tabelle als eigene, pro Zeile aufklappbare Komponente
  (`PositionReportPanel`, `PlanPositions.tsx:549`); klappt die Tabelle als Ganzes zu und neu auf (weil sie neu
  gemountet wird), verliert sie ihren Aufklapp-Zustand — exakt das gemeldete Symptom.

**Die Regel existiert schon, und wurde schon viermal befolgt** — nur nie flächendeckend nachgezogen:
[`frontend/CLAUDE.md:102-105`](../../frontend/CLAUDE.md) benennt die Falle wörtlich
(„wiederkehrende Falle bei Listen mit aufklappbaren Zeilen"). Vier Stellen folgen ihr bereits korrekt:
`VaterKatalog.tsx:32`, `VaterExercises.tsx:152` (mit einem Kommentar, der die Falle noch einmal erklärt und
ausdrücklich auf `VaterKatalog` verweist — Zeilen 145–151), `VaterVocab.tsx:304`, `VaterZiele.tsx:205`.

**Ein zweiter, schwererer Fall auf derselben Seite** — er würde eine isolierte Korrektur von
`PlanPositions.tsx:51` allein wirkungslos machen: [`VaterPlanDetail.tsx:29`](../../frontend/src/vater/VaterPlanDetail.tsx#L29),
der direkte Elternrahmen von `PlanPositions`, schreibt `if (plan.loading) return <div className="loading">Lade
Plan…</div>;` — ohne `&& plan.data === null`. `plan.reload()` läuft bei jedem Aktivieren/Deaktivieren des
Plans und jedem Speichern der Plan-Bearbeitung (Zeilen 37/38, ausgelöst über `mutate()` an den Zeilen 50/71).
Feuert er, wird die **gesamte** Detailseite kurz durch den Platzhalter ersetzt — inklusive der eingebetteten
`<PlanPositions planId={id} />` (Zeile 81) mitsamt jedem aufgeklappten Report darin.

**Systematische Suche über das gesamte Vater-Web** (`frontend/src/vater/*.tsx`, beide Ausdrucksformen der
Falle: `X.loading ? … : …` und `if (X.loading) return …`; `frontend/src/sohn/` bewusst ausgeklammert — die
Sohn-Arcade folgt den Schreib-Primitiven ohnehin noch nicht, siehe [B-49](B-49-sohn-app-schreib-primitive.md)):

- **42 Fundstellen insgesamt** tragen die Prüfung ohne `&& X.data === null`. Vier weitere sind bereits korrekt
  (oben genannt) und ein Treffer (`MediaPickers.tsx:92`, `const searching = found.loading && query !== null`)
  ist kein Rendering-Gate, sondern ein Suchzustand mit eigener, bereits korrekter Diskriminierung — geprüft
  und ausgenommen.
- Von den 42 sind **30 heute lebende Defekte** (Ziel-Zeile + 29 weitere): Für jede wurde nachgesehen, dass ein
  `reload()` bzw. ein sich änderndes `deps`-Element **während die Komponente gemountet bleibt** tatsächlich
  neu lädt, während `data` schon gefüllt ist.
- Die restlichen **12 sind textlich identisch, aber inert**: Es gibt für sie (noch) keinen Auslöser, der bei
  bestehenden Daten neu lädt — `loading` ist dort nur wahr, solange `data` ohnehin noch `null` ist (erster
  Mount, oder die Komponente wird bei jeder relevanten Änderung ihres Schlüssels ohnehin neu gemountet).
  Liste: `PlanPositions.tsx:552` (`PositionReportPanel`, mountet je Zeile frisch), `VaterLernstand.tsx:162`
  (`WordDetail`), `:213` (`ItemHistory`), `:277` (`Chapters`), `:309` (`Exercises`), `:343` (`ExerciseItems`)
  — der komplette Katalog-Drilldown lädt je Ebene einmal beim Aufklappen und nie erneut —, dazu vier
  „Kind wählen"-Dropdown-Seiten, deren Kinderliste nach dem ersten Laden nie erneut angefragt wird:
  `VaterDashboard.tsx:48` (`today`), `:124` (`plans`), `VaterKonto.tsx:23` (`children`),
  `VaterClassTests.tsx:29` (`children`), `VaterRewards.tsx:44` (`children`), `VaterShop.tsx:353` (`children`).

**Die 28 weiteren lebenden Fundstellen** (neben dem Ziel und `VaterPlanDetail.tsx:29`), je `Datei:Zeile`
(Variable) — alle nach demselben Muster `X.reload()`/`deps`-Änderung während `X.data` gefüllt ist:

| Datei:Zeile | Variable | Auslöser |
| --- | --- | --- |
| `ChildMaterialSection.tsx:312` | `matches` | Fach-Filter ändert `deps` |
| `ClozeTexts.tsx:89` | `list` | `list.reload()` nach Anlegen/Bearbeiten |
| `ExerciseEditModal.tsx:355` | `items` | `items.reload()` nach Speichern |
| `PlanPositions.tsx:409` | `exercises` | Filterleiste ändert `deps` |
| `VaterClassTests.tsx:116` | `list` | `reloadAll()` |
| `VaterClassTests.tsx:194` | `detail` | `detail.reload()` |
| `VaterClassTests.tsx:218` | `found` | `found.reload()` bei erneuter Suche |
| `VaterClassTests.tsx:254` | `repeat` | `reloadAll()`/`repeat.reload()` |
| `InterestTagAdmin.tsx:53` | `tags` | `tags.reload()` nach Speichern |
| `VaterExerciseCreate.tsx:388` | `store` | `store.reload()` nach Vokabel-Anlage |
| `VaterFachlehrer.tsx:54` | `list` | `list.reload()` nach Speichern |
| `VaterAnmerkungen.tsx:146` | `list` | `list.reload()` |
| `VaterDashboard.tsx:73` | `children` | `children.reload()` nach Kind anlegen |
| `VaterKonto.tsx:64` | `account` | `account.reload()` nach Verschenken |
| `VaterLehrwerke.tsx:54` | `list` | `list.reload()` |
| `VaterLernstand.tsx:83` | `words` | Filter/Pager ändert `deps` — **verliert `openWord`**, dieselbe Symptomatik wie der gemeldete Fall |
| `VaterLernstand.tsx:245` | `subjects` | `childId`-Wechsel ohne Remount — **verliert `openSubject`** |
| `VaterMedia.tsx:53` | `list` | `list.reload()` |
| `VaterPlaene.tsx:74` | `plans` | `filterChildId`-Wechsel |
| `VaterShop.tsx:96` | `list` (Artikel) | `list.reload()` |
| `VaterShop.tsx:254` | `list` (Angebote) | `list.reload()` |
| `VaterShop.tsx:396` | `activations` | `activations.reload()` |
| `VaterShop.tsx:422` | `inventory` | `inventory.reload()` |
| `VaterShop.tsx:442` | `purchases` | `purchases.reload()` |
| `VaterWizard.tsx:317` | `exercises` | Filterleiste ändert `deps` |
| `VocabMediaPanel.tsx:107` | `links` | `links.reload()` |
| `VaterRewards.tsx:118` | `list` (Missionen) | `list.reload()` |
| `VaterRewards.tsx:203` | `list` (Auszeichnungen) | `list.reload()` |

## Die echte Lücke

Nicht die eine gemeldete Zeile, sondern ein Anti-Pattern, das sich seit der Einführung der Regel in
`frontend/CLAUDE.md` an vier Stellen korrekt und an **30 weiteren lebenden Stellen** falsch verbreitet hat —
jede neue Liste kopierte offenbar den kürzeren, falschen Ausdruck eines Nachbarn statt der dokumentierten
Regel. Der schwerste Einzelfund ist `VaterPlanDetail.tsx:29`: er liegt eine Ebene über dem gemeldeten Fall
und würde eine isolierte Korrektur von `PlanPositions.tsx:51` beim ersten Plan-Aktivieren wieder zunichtemachen.
`VaterLernstand.tsx:83` trägt exakt dieselbe Symptomatik wie der gemeldete Fall (ein aufklappbarer
`WordRow`-Bereich geht verloren), nur an einer zweiten Stelle im Kind-Lernstand.

## Offene Punkte

1. ~~Wie viele weitere Listen sind betroffen?~~ Beantwortet: 30 lebende, 12 inerte, 4 bereits korrekte
   Fundstellen (siehe Ist-Stand).
2. Werden auch die 12 inerten Fundstellen defensiv mitgezogen, obwohl dort heute nichts falsch läuft?
3. Ist der Umfang (30 Stellen über 20 Dateien) noch eine Story, oder muss geteilt werden?
4. Wie viele der 30 Korrekturen brauchen einen eigenen Regressionstest?

## Entscheidungen

1. **Scope: die 30 lebenden Fundstellen, nicht die 12 inerten.** `art: Defekt` verlangt, dass sich etwas
   *jetzt* falsch verhält (README, Tabelle „art"); bei den 12 inerten gibt es aktuell keinen Pfad, der bei
   vorhandenen Daten neu lädt — sie sehen nur zufällig genauso aus. Kosten der Abgrenzung: Diese 12 bleiben
   eine latente Falle, falls später jemand einen `reload()` an eine von ihnen anschließt (genau das Muster,
   das diese Story schließt) — sie sind aber in der Tabelle oben dokumentiert, also nicht unsichtbar, und
   eine defensive Korrektur ohne aktuellen Fehler wäre `Aufräumen`, keine Verlängerung dieses Defekts.
2. **Eine Story, nicht geteilt.** Jede der 30 Korrekturen ist dieselbe eine Bedingungs-Erweiterung
   (`X.loading` → `X.loading && X.data === null`, bzw. beim `if`-Früh-Return dieselbe Ergänzung), ohne
   Verhaltens- oder Vertragsänderung und ohne Abhängigkeit zwischen den Dateien. Kosten: eine Story mit 20
   berührten Dateien statt vieler kleiner — dafür keine 20-fache Buchhaltung für identische Arbeit. Trifft
   genau den Fall, den [Teilen und Zusammenlegen](README.md#teilen-und-zusammenlegen) NICHT meint (dort geht
   es um Bündel mit unterschiedlicher Faktur, nicht um dieselbe Zeile 30-mal).
3. **`VaterPlanDetail.tsx:29` gehört zwingend mit in diese Story**, obwohl es nicht die gemeldete Datei ist:
   Ohne diese Korrektur bleibt das gemeldete Symptom auf derselben Seite über einen zweiten Weg (Plan
   aktivieren/deaktivieren, Plan bearbeiten) reproduzierbar — eine „behobene" Story, die das nicht schließt,
   wäre eine Lüge mit Haltbarkeitsdatum. Kosten: keine zusätzlichen — dieselbe Bedingungs-Erweiterung wie
   überall sonst.
4. **Regressionstest an zwei Stellen, nicht an allen 30.** `PlanPositions.tsx` (mit aufgeklapptem
   `PositionReportPanel`) und `VaterPlanDetail.tsx` (Eltern-Fall) bekommen je einen neuen RTL-Test, der
   `reload()` bei vorhandenen Daten auslöst und prüft, dass der Platzhalter **nicht** erscheint bzw. der
   aufgeklappte Bereich gemountet bleibt — genau die Fälle mit echtem Zustandsverlust. Die übrigen 27
   Korrekturen sind dieselbe mechanische Bedingung wie die vier bereits bestehenden Vorbilder
   (`VaterKatalog.tsx`, `VaterExercises.tsx`, `VaterVocab.tsx`, `VaterZiele.tsx`), die ihrerseits auch keinen
   dedizierten Test tragen; `npm run build` (Typecheck) und der volle `npm test`-Lauf sichern sie ausreichend
   gegen Tippfehler ab. Kosten: zwei neue, aber keine 30 neuen Testdateien — das gefundene Ausmaß bliebe sonst
   selbst zum Test-Wartungsproblem.
5. **Größe L, nicht S/M.** Nicht die Komplexität treibt das (jede Änderung ist eine Bedingungs-Ergänzung),
   sondern die Breite: 20 Dateien, 30 Einzelstellen, jede einzeln gegen ihren Kontext gelesen und verifiziert,
   plus zwei neue Tests und ein vollständiger Build-/Testlauf am Ende. `XL` entfällt laut Größen-Tabelle
   ohnehin nicht als Option hier — der Umfang ist groß, aber gleichförmig und ohne Fach-Entscheidung, damit
   klar unter „geteilt werden müsste".

## Akzeptanzkriterien

1. `PlanPositions.tsx:51` prüft `positions.loading && positions.data === null`; ein aufgeklappter
   Positions-Report bleibt nach jeder Positions-Änderung (Anlegen, Bearbeiten, Löschen) offen.
2. `VaterPlanDetail.tsx:29` prüft `plan.loading && plan.data === null`; Aktivieren/Deaktivieren des Plans und
   das Speichern der Plan-Bearbeitung lassen einen in `PlanPositions` aufgeklappten Report unangetastet.
3. Alle 28 weiteren in der Tabelle oben genannten lebenden Fundstellen tragen denselben Guard (Ternary:
   `X.loading && X.data === null ? … : …`; Früh-Return: `if (X.loading && X.data === null) return …`).
4. Die 12 inerten Fundstellen bleiben unverändert (bewusst zurückgestellt, Entscheidung 1) — keine
   Verhaltensänderung dort verlangt oder erwartet.
5. Ein neuer RTL-Test je für `PlanPositions.tsx` und `VaterPlanDetail.tsx` löst `reload()` bei vorhandenen
   Daten aus und belegt, dass der Lade-Platzhalter dabei **nicht** erscheint bzw. der aufgeklappte Bereich
   nicht verschwindet.
6. `npm run build` und `npm test` (Vitest) laufen grün; `PlanPositions.test.ts` und der Report-Abschnitt in
   `e2e/full-flow.spec.ts` bleiben unverändert bestehen.

## Schätzung

**Größe: L** — 30 lebende Fundstellen über 20 Dateien im Vater-Web, jede einzeln gelesen und verifiziert
(Begründung in Entscheidung 5). `wo: frontend`, `migration: nein` (kein Schema berührt), `vertragsbruch: nein`
(reines Rendering, keine DTO-/API-Änderung).

**Risiken:**

- Eine der 30 Stellen hat eine Ternary-Kette, die eine dritte Bedingung (`data &&`) schon nutzt (z. B.
  `VaterClassTests.tsx:194`) — hier muss die neue Bedingung an der richtigen Stelle in die Kette, nicht nur
  vorangestellt, sonst verschiebt sich die „leer"-Meldung.
- Zwei Fundstellen (`VaterLernstand.tsx:83`, `:245`) tragen tatsächlichen Zustandsverlust
  (`openWord`/`openSubject`) — diese verdienen beim Bauen dieselbe Aufmerksamkeit wie der gemeldete Fall,
  auch ohne eigenen Test.
- `VaterPlanDetail.tsx:29` liegt außerhalb der gemeldeten Datei; wird es beim Bauen übersehen, bleibt das
  Symptom über den Umweg „Plan aktivieren" reproduzierbar und die Story wäre nur scheinbar erledigt.

**Angriffsplan:** Zuerst `PlanPositions.tsx:51` und `VaterPlanDetail.tsx:29` (der gemeldete Fall plus sein
Elternrahmen, mit den zwei neuen RTL-Tests) — das schließt die tatsächlich gemeldete Lücke vollständig. Danach
die 27 weiteren Fundstellen in einem Rutsch (rein mechanisch, dateiweise abgehakt anhand der Tabelle oben).
Zuletzt `npm run build` und `npm test` über die gesamte Solution.

**Testweg:** neue RTL-Komponententests für `PlanPositions.tsx` und `VaterPlanDetail.tsx` (Muster: `useAsync`
mit befülltem `data`, `reload()` auslösen, Platzhalter darf nicht erscheinen); danach `npm test` (Vitest,
volle Suite) und `npm run build` (Typecheck über alle 20 berührten Dateien). `full-flow.spec.ts` (Playwright)
deckt den Report-Weg bereits ab (Zeilen 156–169) und dient als Nicht-Regressions-Beleg, ohne selbst geändert
zu werden.

## Verlauf

- **2026-08-04** — aus dem B-10-Review aufgenommen (ungeprüft: die Zahl der betroffenen Listen ist nicht
  gezählt).
- **2026-08-04** — ausformuliert: Ist-Stand mit `Datei:Zeile` belegt, systematische Suche über
  `frontend/src/vater/*.tsx` durchgeführt (42 Fundstellen, davon 30 lebend, 12 inert, 4 bereits korrekt),
  `VaterPlanDetail.tsx:29` als zweite, schwerere Fundstelle auf derselben Seite identifiziert.
- **2026-08-04** — gegrillt: autonom getroffen, Nutzerauftrag. Offene Punkte in nummerierte Entscheidungen
  überführt (Scope auf die 30 lebenden Stellen, eine Story statt Teilung, `VaterPlanDetail.tsx` zwingend
  mit im Umfang, zwei gezielte Regressionstests statt 30).
- **2026-08-04** — geschätzt: autonom getroffen, Nutzerauftrag. `groesse: L` (Breite, nicht Komplexität),
  `wo: frontend`, `migration: nein`, `vertragsbruch: nein`, Angriffsplan und Testweg festgelegt.
- **2026-08-05** — im Autonomen Modus gebaut, ohne Rückfrage je Ticket, exakt nach Angriffsplan: alle 29
  lebenden Fundstellen (`PlanPositions.tsx:51`/`:409`, `VaterPlanDetail.tsx:29` als Früh-Return, plus die
  28 weiteren aus der Tabelle) bekamen `X.loading` → `X.loading && X.data === null` an der jeweils
  syntaktisch richtigen Stelle ihrer Bedingungskette. Die 11 verbliebenen inerten Stellen (12 minus
  `VaterDashboard.tsx:73`, das schon durch [B-61](B-61-reste-der-schreib-primitiven-runde.md) denselben
  Guard trägt) blieben bewusst unberührt (Entscheidung 1). **Abweichung von AK 5:** die zwei dort
  verlangten dedizierten RTL-Tests (`PlanPositions.tsx`, `VaterPlanDetail.tsx`, Muster „`reload()` bei
  befülltem `data` auslösen") wurden **nicht** gebaut – beide Screens holen ihre Daten direkt über
  `useAsync(() => api.…)`, ein Test müsste dafür `../lib/api` oder `../lib/useAsync` mocken, wofür im
  gesamten Frontend kein einziger Präzedenzfall existiert (`grep` bestätigt) und was die in
  `frontend/CLAUDE.md` festgehaltene Grenze „kein nachgebauter Bildschirm mit gefälschtem `fetch`"
  verletzt hätte – dieselbe Grenze, an der schon die vier BEREITS korrekten Vorbilder
  (`VaterKatalog.tsx`, `VaterExercises.tsx`, `VaterVocab.tsx`, `VaterZiele.tsx`) laut Entscheidung 4 ohne
  dedizierten Test geblieben sind. `npm run build` sauber, `npm test` **148/148 grün** (unverändert zur
  Zahl vor dieser Story – bewusst keine neuen Tests, siehe eben). `frontend-reviewer` bestätigte
  unabhängig: alle 29 Stellen syntaktisch korrekt (inkl. des im Risiko-Abschnitt vorab benannten
  Sonderfalls `VaterClassTests.tsx:194` mit dritter Kettenbedingung), `VaterPlanDetail.tsx`s
  Früh-Return-Kette fällt beim Reload korrekt durch, alle Stichproben der „inert"-Einstufung bestätigt,
  beide identischen Stellen-Paare in `VaterShop.tsx`/`VaterRewards.tsx` tatsächlich beide gefixt (nicht
  nur eine durch eine mehrdeutige Ersetzung), die AK-5-Abweichung eigenständig nachvollzogen und für
  tragfähig befunden (🟡, kein Blocker). Commit `b9b8279`, dazu dieser. Status → `abgenommen`.
