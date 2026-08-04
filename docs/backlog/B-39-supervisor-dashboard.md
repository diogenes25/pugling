---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/auswertung, rolle/supervisor]
aliases: [Supervisor-Dashboard, Eltern-Dashboard, Fortschritts-Dashboard, Tag Woche Monat]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: Nutzer, Sitzung 2026-07-31
---

# B-39 · Supervisor-Dashboard über die Kinder

Ein Dashboard, das dem Supervisor Fortschritte, Klausuren und Tätigkeiten seiner Kinder **anschaulich,
übersichtlich und informativ** zeigt — statt als Tabellen und Listen, wie das Vater-Web es heute tut. Mit
drei Zeitachsen-Ansichten (**täglich, wöchentlich, monatlich**), den **Zusammenhängen von Übungen zu einer
Klausur**, und durchgehend vom Groben ins Genaue: jede verdichtete Ansicht lässt sich aufklappen, bis man
beim einzelnen Wort steht.

## User Story

Als **Supervisor** möchte ich auf einen Blick sehen, wie meine Kinder heute, diese Woche und diesen Monat
lernen — visuell statt als Tabelle, mit der Möglichkeit, von einer verdichteten Zeitperiode bis zum
einzelnen Kind und dessen Positionen aufzuklappen —, damit ich Lernstand und Pflichterfüllung erfasse, ohne
jedes Kind einzeln in einer eigenen Ansicht nachzuschlagen.

## Ist-Stand am Code

- **Eine kind-übergreifende Tagesübersicht existiert bereits, aber nur für den heutigen Tag und als reine
  HTML-Tabelle.** `frontend/src/vater/VaterDashboard.tsx:44-69` rendert eine `<table>` mit Kind/Status/
  Ziele/Punkte, gespeist von `api.childrenDaily()`. Serverseitig liefert
  `backend/Pugling.Api/Controllers/Supervisor/ChildrenDashboardController.cs:11-26`
  (`GET supervisor/children/daily-overview`, optional `?date=`, sonst heute UTC) einen
  `Dashboard`-Record; die Aggregation läuft in
  `backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:12-51`, die pro Kind über die
  aktiven Pläne den Tag per `PositionProgressService.ComputeDayAsync` aufsummiert
  (`goalsTotal`/`goalsMet`/`points`/`dutyDone`/`practiced`). **Es gibt keinen `?date=`-Bereich, keine
  Woche, keinen Monat** — nur genau ein Tag je Aufruf.
- **Der Drill-down existiert schon — aber entlang des Katalogs, nicht der Zeit.**
  `frontend/src/vater/VaterLernstand.tsx:28-51` bietet pro Kind zwei Reiter: „Schlecht gelernte Wörter"
  (Rollup über `api.childWordMastery`, kind-zentrisch flach) und „Nach Katalog" (Fach → Kapitel → Übung →
  Wort). Serverseitig sind das
  `backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:17-24`
  (`.../children/{childId}/vocabulary-progress`) und
  `backend/Pugling.Api/Controllers/Student/ChildLearnProgressController.cs:16-23`
  (`.../children/{childId}/learn`). Beide drillen entlang der **Katalog-Hierarchie**; eine Achse
  Monat → Woche → Tag → Sitzung gibt es an keiner Stelle im Frontend.
- **„Monatlich" existiert nirgends im Domänenmodell — bestätigt.**
  `backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4-12` (`enum GoalCadence`) kennt nur
  `None`/`Daily`/`Weekly`. Der Malus (`PositionProgressService.SettleClosedPeriodsAsync`, siehe
  `backend/Pugling.Api/CLAUDE.md` → „Services") rechnet ausschließlich in diesen zwei Perioden. Eine
  Monatsansicht kann darum nur eine **Aggregation über bereits existierende Tageswerte** sein — keine
  neue Zielperiode, keine Migration.
- **Übung ↔ Klausur ist modelliert, aber ungenutzt für eine Dashboard-Sicht.**
  `backend/Pugling.Api/Models/KlassenarbeitEntities.cs:35-41` (`ExerciseTag`: Tag ↔ Exercise) und
  `:106-113` (`KlassenarbeitTag`: Klassenarbeit ↔ Tag) ergeben die Kette
  `Klassenarbeit --< KlassenarbeitTag >-- Tag --< ExerciseTag >-- Exercise`; zusätzlich existiert die
  direkte `KlassenarbeitExercise` (`:92-100`). Eine Oberfläche, die diese Kette **zeigt**, existiert nicht.
- **Ein Paging/Sortier-Muster für Zeitreihen ist bereits etabliert und dient als Vorlage.**
  `backend/Pugling.Api/Controllers/Student/PlanOverviewController.cs:57-74`
  (`GET study-plans/{planId}/overview/progress`) nimmt `from`/`to`/`sort`/`skip`/`take` und liefert
  `X-Total-Count` — exakt das Muster, das die neue Zeitachse für mehrere Kinder braucht, nur bisher
  **innerhalb eines Plans**, nicht kind-übergreifend.
- **Das `?childId=`-Filtermuster für kind-übergreifende Aggregate ist etabliert.**
  `backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:44`
  (`List([FromQuery] int? childId = null, …)`) ist der Präzedenzfall für „top-level Aggregat, das nur
  nach Kind filtert" (siehe `CLAUDE.md` → Konventionen, Abschnitt „Eigentum").
- **Es gibt keine Chart-Bibliothek im Frontend.** `frontend/package.json` enthält weder `d3`, `recharts`,
  `victory`, `visx` noch `nivo` — bestätigt.
- **`prefersReducedMotion` existiert bereits als geteilte Regel.** `frontend/src/lib/ui.ts:22-25`.
- **`Objective`/`KeyResult` liefern schon einen live berechneten Zielfortschritt** (siehe
  `docs/obsidian.md` → Lernziele/Bonus-System) — eine eigene Berechnung im Dashboard wäre eine zweite
  Fassung derselben Zahl.

## Die echte Lücke

Die Notiz vermutete ein fehlendes Dashboard; die Recherche zeigt: **die Bausteine existieren, die
Zeitachse fehlt.** Ein kind-übergreifender Tagesüberblick ist da (nur ein Tag, nur Tabelle), ein
Kind-Detail-Drilldown ist da (nur Katalog-Achse, kein Zeitbezug). Die eigentliche Lücke ist schmaler als
„ein neues Dashboard bauen": **ein neuer, ranged/gepagter Aggregations-Endpunkt** nach dem Muster von
`PlanOverviewController.Progress`, der die bereits vorhandene Tages-Berechnung
(`PositionProgressService.ComputeDayAsync`) über einen Zeitraum und mehrere Kinder gruppiert liefert —
plus eine handgebaute Visualisierung, die diese Zeitachse an zwei Stellen zeigt (Haushalts-Übersicht,
Kind-Detail). Tätigkeits-Zeitleiste und Klausur-Sicht sind eigenständige, **nicht** von der Zeitachse
abhängige Ergänzungen (siehe Entscheidung 6).

## Offene Punkte

~~„Monatlich" gibt es nirgends. Ist die Monatsansicht eine reine Aggregation über Wochen (dann rein
lesend, keine Schemafrage), oder soll sie eine eigene Zielperiode werden?~~ → siehe Entscheidung 1.

~~Was heißt „Tätigkeiten"?~~ → siehe Entscheidung 2.

~~Ein Kind oder alle? Sind das zwei Ansichten (Haushalts-Überblick vs. Kind-Detail), und ist die
Haushaltssicht die gröbste Stufe des Drill-downs?~~ → siehe Entscheidung 3.

~~Wie „anschaulich"? Handgebautes SVG (kein neues Paket) oder eine Chart-Bibliothek
(`--legacy-peer-deps`-Fußangel, vgl. B-25)?~~ → siehe Entscheidung 4.

~~Wie viele neue Endpunkte braucht es wirklich?~~ → siehe Entscheidung 5.

~~Ist das eine Story oder ein Programm? Zeitachsen-Aggregat, Tätigkeits-Zeitleiste, Klausur-Sicht und
Visualisierung sind vier trennbare Brocken.~~ → siehe Entscheidung 6.

~~Abgrenzung zu den großen Zielen (`Objective`/`KeyResult`).~~ → siehe Entscheidung 7.

## Entscheidungen

1. **„Monatlich" ist eine reine Aggregation über bereits berechnete Tageswerte, kein neues
   `GoalCadence`-Mitglied.** Begründung: `GoalCadence` (`PlanPositionBaseTypes.cs:4-12`) und der darauf
   aufbauende Malus kennen nur Tag/Woche; eine dritte Zielperiode würde Pflicht und Malus-Abrechnung
   anfassen, was diese Story nicht braucht — hier geht es ums **Anzeigen**, nicht ums Definieren neuer
   Pflichten. Kosten: reine Serverlogik, die Tageswerte im Kalendermonat/-woche gruppiert und summiert
   (keine Migration, kein neues Feld).
2. **„Tätigkeiten" bleiben in dieser Story auf das beschränkt, was die Zeitachse ohnehin zeigt
   (gelernt/nicht gelernt, Ziele erreicht, Punkte) — keine eigene Ereignis-Zeitleiste.** Begründung:
   Sitzungen, Shop-Käufe, Missionen und Klausur-Termine liegen in vier getrennten Tabellen ohne
   gemeinsamen Endpunkt; sie zu einer chronologischen Zeitleiste zu verschmelzen ist ein eigenständiges
   Vorhaben mit eigenem Datenmodell-Bedarf (welche Ereignistypen, welche Filter), keine Erweiterung der
   Zeitachsen-Aggregation. Kosten: „Tätigkeiten" im Sinn der ursprünglichen Notiz (Sitzungen, Käufe,
   Missionen als Log) ist **nicht** Teil dieser Story — eigene Folge-Idee wert.
3. **Zwei Ansichten, beide Teil dieser Story: Haushalts-Übersicht (alle Kinder, gröbste Stufe) und
   Kind-Detail (ein Kind, mit Drill-down zur bestehenden Wort-/Katalogsicht).** Begründung: Die
   Formulierung „seine Kinder" *und* „des Kindes" beschreibt keinen Widerspruch, sondern die gewünschte
   Grob-zu-Genau-Kette: Haushalt (neu) → Kind über die Zeit (neu) → Position/Wort (existiert bereits in
   `VaterLernstand.tsx`). Kosten: zwei Frontend-Integrationsstellen (`VaterDashboard.tsx` erweitert,
   `VaterLernstand.tsx` bekommt einen dritten Reiter „Zeitachse"), aber ein gemeinsamer Endpunkt
   (Entscheidung 5) bedient beide.
4. **Handgebaute SVG-/CSS-Balken, keine neue Abhängigkeit.** Begründung: Es gibt keine Chart-Bibliothek
   im Frontend, und jede neue Abhängigkeit läuft in den dokumentierten `--legacy-peer-deps`-Konflikt
   hinein (`frontend/CLAUDE.md`, B-25); eine Balken-/Sparkline-Darstellung für Ziele/Punkte je Periode
   braucht kein Diagramm-Framework. Kosten: etwas mehr Handarbeit als ein Library-Aufruf, dafür kein
   neues Peer-Risiko. Pflicht dabei: `prefersReducedMotion` respektieren (`lib/ui.ts:22-25`) und Farbe
   nie als einzige Information (Text/Symbol zusätzlich, wie bei `MasteryPill`).
5. **Genau ein neuer Endpunkt, kein Fünffach-Verrechnen im Frontend.** `GET
   api/v1/supervisor/children/overview` (top-level, analog zum Präzedenzfall
   `StudyPlansController.List` mit optionalem `?childId=`, `CLAUDE.md` → „Eigentum") nimmt `from`/`to`/
   `granularity` (`day`|`week`|`month`) sowie `skip`/`take`/`sort` nach dem Muster von
   `PlanOverviewController.Progress` (`X-Total-Count`) und liefert je Kind eine Periodenreihe, berechnet
   aus der bereits vorhandenen `PositionProgressService.ComputeDayAsync`-Schleife
   (`ChildrenDashboardService.cs:33-47`). Die bestehende `daily-overview` bleibt unverändert (kein
   Vertragsbruch) und bedient weiter die schlanke Ein-Tages-Tabelle. Begründung: API-First — die
   Verdichtung gehört in den Server, sonst rechnet Web und später eine PWA dieselbe Aggregation doppelt.
   Kosten: ein neuer Service-Pfad (Gruppierung von Tageswerten zu Woche/Monat), ein neues Contracts-DTO
   (additiv, kein Bruch), Frontend nutzt denselben Endpunkt für beide Ansichten aus Entscheidung 3.
6. **Diese Story liefert Zeitachsen-Aggregat + Visualisierung; Tätigkeits-Zeitleiste und eine dedizierte
   Klausur↔Übung-Sicht sind explizit draußen, kein Split der Story-Id nötig.** Begründung: Die vier
   Brocken sind trennbar, aber nicht gleich groß — die Zeitachse allein braucht einen neuen Endpunkt plus
   zwei Frontend-Integrationen (Größe L, siehe Schätzung), während Tätigkeits-Zeitleiste und Klausur-Sicht
   eigene Datenmodell- bzw. Interaktionsfragen aufwerfen, die diese Story nicht beantwortet. Ein
   Split auf Story-Ebene (→ `verworfen: geteilt`) ist darum **nicht** nötig: B-39 bleibt die Zeitachse,
   die zwei übrigen Brocken sind Kandidaten für eigene künftige Ideen (nicht Teil dieser Datei). Kosten:
   „Klausur-Zusammenhang zeigen" bleibt vorerst unsichtbar, obwohl modelliert — akzeptiert, weil es ohne
   Zeitachsen-Abhängigkeit jederzeit nachgeliefert werden kann.
   **Größenkontrolle**: Damit bleibt die Story bei Größe **L**, nicht **XL** — der Anker „eine
   DB-Umbau-Etappe" trifft nicht zu, weil keine Migration entsteht; ein neuer Endpunkt plus zwei
   Frontend-Integrationsstellen ohne Bibliotheksabhängigkeit ist die obere Grenze von „ohne Migration
   baubar in einer Sitzungsfolge".
7. **Das Dashboard zeigt `Objective`/`KeyResult`-Fortschritt an (Link/Kachel), berechnet ihn nicht neu.**
   Begründung: Der Zielfortschritt wird bereits live berechnet; eine zweite Berechnung im Dashboard wäre
   eine zweite Fassung derselben Zahl und liefe garantiert irgendwann auseinander. Kosten: keine —
   Wiederverwendung des bestehenden Wertes über die vorhandene Route.

## Akzeptanzkriterien

1. `GET api/v1/supervisor/children/overview` liefert für `granularity=day|week|month` je Kind eine
   Periodenreihe (`goalsTotal`/`goalsMet`/`points`/`dutyDone`) im Bereich `from`..`to`, paged über
   `skip`/`take` mit `X-Total-Count`, optional gefiltert über `?childId=`; die bestehende
   `daily-overview` bleibt unverändert erreichbar.
2. Die Monatsansicht ist eine serverseitige Summe über die zugehörigen Kalenderwochen/-tage — keine neue
   `GoalCadence`, kein Schema-Wechsel.
3. Die Haushalts-Übersicht (`/vater`) zeigt neben der heutigen Tabelle einen Umschalter Tag/Woche/Monat
   mit einer handgebauten Balken-/Sparkline-Darstellung je Kind.
4. Der Kind-Hub (`/vater/kind/:id/lernstand`) bekommt einen dritten Reiter „Zeitachse" mit derselben
   Darstellung für ein Kind, aufklappbar bis zur bestehenden Wort-/Katalog-Sicht (kein neuer
   Drill-down-Endpunkt nötig — die bestehenden Reiter „Schlecht gelernte Wörter"/„Nach Katalog" bleiben
   das Ziel des Aufklappens).
5. Die Darstellung respektiert `prefersReducedMotion` und trägt bei jedem Status zusätzlich zur Farbe
   Text oder Symbol.
6. Kein neues npm-Paket für Diagramme wird installiert.
7. Tätigkeits-Zeitleiste (Sitzungen/Käufe/Missionen als chronologisches Log) und eine dedizierte
   Klausur↔Übung-Ansicht sind **nicht** Teil dieser Story (Entscheidung 6).

## Schätzung

**Größe: L** — Anker „eine DB-Umbau-Etappe wie E6" trifft von der Tragweite her nicht zu (keine
Migration), aber die Kombination aus neuem Backend-Endpunkt (Gruppierungslogik Tag→Woche→Monat über
mehrere Kinder), neuem additivem Contracts-DTO und **zwei** Frontend-Integrationsstellen mit
handgebauter Visualisierung ohne Bibliothek übersteigt die M-Anker (ein vokabel-basierter Batch-Pfad,
B-03) deutlich — passt aber noch in eine baubare Story ohne Split.

- **`wo`: beides** — Backend zuerst (API-First): neuer Endpunkt und Gruppierungslogik in
  `ChildrenDashboardService`/einem neuen Service, danach die zwei Frontend-Integrationen
  (`VaterDashboard.tsx`, `VaterLernstand.tsx`).
- **`migration`: nein** — keine Schema-Änderung; die Aggregation liest ausschließlich bereits
  vorhandene, tagesgenaue Werte neu gruppiert.
- **`vertragsbruch`: nein** — ein neuer, additiver Endpunkt mit neuem DTO; `daily-overview` und alle
  bestehenden Routen bleiben unverändert.
- **Risiken**: (a) SQLite-Aggregation über Kalenderwochen/-monate ist fehleranfällig bei Zeitzonen nahe
  Mitternacht (`CLAUDE.md` → „Zeit/UTC") — Wochen-/Monatsgrenzen strikt in UTC ziehen, wie die übrige
  Tageslogik. (b) Ohne Chart-Bibliothek kostet die Balkendarstellung mehr Handarbeit als ein
  Library-Aufruf — akzeptiert (Entscheidung 4). (c) Die in Entscheidung 6 zurückgestellten Brocken
  (Tätigkeits-Zeitleiste, Klausur-Sicht) bleiben offene, potenziell wertvolle Folgeideen — bewusst nicht
  Teil dieser Schätzung.
- **Angriffsplan**: (1) Contracts-DTO für die Periodenreihe (additiv); (2) Service-Erweiterung: bestehende
  `ComputeDayAsync`-Schleife über einen Datumsbereich laufen lassen und je nach `granularity` zu Woche/
  Monat gruppieren; (3) neuer Controller-Endpunkt `supervisor/children/overview` nach dem
  Paging-/Sortier-Muster von `PlanOverviewController.Progress`; (4) Frontend: Umschalter + Balkenkomponente
  einmal bauen, an zwei Stellen (`VaterDashboard.tsx`, `VaterLernstand.tsx`) einhängen.
- **Testweg**: neue Integrationstestklasse in `Pugling.Api.Tests` (mirrored an `ChildrenDashboardTests`/
  `PositionGoalOverviewTests`) für die Gruppierungslogik — je ein Fall für `day`/`week`/`month`, Grenztag
  an einer Kalenderwochen-/Monatsgrenze, `?childId=`-Filter und Paging/`X-Total-Count`. Frontend:
  Vitest-Komponententest für die neue Zeitachsen-Komponente (Umschalter rendert die richtige Periodenzahl
  an Balken), zusätzlich Sichtprüfung über `/smoke-test` auf `/vater` und `/vater/kind/:id/lernstand`.

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche). Vorab nur geklärt, dass es
  keine Duplikat-Story gibt und dass Übung↔Klausur schon modelliert ist.
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (kind-übergreifende Tagesübersicht und
  Katalog-Drilldown existieren bereits, aber ohne Zeitachse; `GoalCadence` kennt nur Tag/Woche;
  Übung↔Klausur ist modelliert; keine Chart-Bibliothek im Frontend; Paging-/Filter-Präzedenzfälle
  identifiziert). Sieben offene Punkte formuliert.
- **2026-08-03** — gegrillt: alle sieben offenen Punkte in nummerierte Entscheidungen überführt (Monat als
  reine Aggregation, Tätigkeits-Zeitleiste draußen, Haushalt **und** Kind-Detail als zwei Ansichten,
  handgebaute Visualisierung ohne neue Abhängigkeit, genau ein neuer Endpunkt nach etabliertem Muster,
  bewusster Schnitt ohne Story-Split, Ziele werden angezeigt statt neu berechnet) — autonom getroffen,
  Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe L (kein Split nötig), `wo: beides` (Backend zuerst), `migration: nein`,
  `vertragsbruch: nein`, Testweg über eine neue Integrationstestklasse plus Vitest-Komponententest
  benannt — autonom getroffen, Nutzerauftrag 2026-08-04.
