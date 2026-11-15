---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/auswertung, rolle/supervisor]
aliases: [Supervisor-Dashboard, Eltern-Dashboard, Fortschritts-Dashboard, Tag Woche Monat]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: Nutzer, Sitzung 2026-07-31
unverifiziert: false
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
  HTML-Tabelle.** `frontend/src/vater/VaterDashboard.tsx:53-75` rendert eine `<table>` mit Kind/Status/
  Ziele/Punkte, gespeist von `api.childrenDaily()`. Serverseitig liefert
  `backend/Pugling.Api/Controllers/Supervisor/ChildrenDashboardController.cs:13` (Route), `:20-25` (Action)
  (`GET supervisor/children/daily-overview`, optional `?date=`, sonst heute UTC) einen
  `Dashboard`-Record; die Aggregation läuft in
  `backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:17-50`, die pro Kind über die
  aktiven Pläne den Tag per `PositionProgressService.ComputeDayAsync` (`Services/Shared/PositionProgressService.cs:211`) aufsummiert
  (`goalsTotal`/`goalsMet`/`points`/`dutyDone`/`practiced`). **Es gibt keinen `?date=`-Bereich, keine
  Woche, keinen Monat** — nur genau ein Tag je Aufruf.
- **Der Drill-down existiert schon — aber entlang des Katalogs, nicht der Zeit.**
  `frontend/src/vater/VaterLernstand.tsx:34-55` bietet pro Kind zwei Reiter: „Schlecht gelernte Wörter"
  (Rollup über `api.childWordMastery`, kind-zentrisch flach) und „Nach Katalog" (Fach → Kapitel → Übung →
  Wort). Serverseitig sind das
  `backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:19`
  (`.../children/{childId}/vocabulary-progress`) und
  `backend/Pugling.Api/Controllers/Student/ChildLearnProgressController.cs:19`
  (`.../children/{childId}/learn`). Beide drillen entlang der **Katalog-Hierarchie**; eine Achse
  Monat → Woche → Tag → Sitzung gibt es an keiner Stelle im Frontend.
- **„Monatlich" existiert nirgends im Domänenmodell — bestätigt.**
  `backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4-12` (`enum GoalCadence`) kennt nur
  `None`/`Daily`/`Weekly`. Der Malus (`PositionProgressService.SettleClosedPeriodsAsync`, siehe
  `backend/Pugling.Api/CLAUDE.md` → „Services") rechnet ausschließlich in diesen zwei Perioden. Eine
  Monatsansicht kann darum nur eine **Aggregation über bereits existierende Tageswerte** sein — keine
  neue Zielperiode, keine Migration.
- **Übung ↔ Klausur ist modelliert, aber ungenutzt für eine Dashboard-Sicht.**
  `backend/Pugling.Api/Models/KlassenarbeitEntities.cs:35` (`ExerciseTag`: Tag ↔ Exercise) und
  `:106` (`KlassenarbeitTag`: Klassenarbeit ↔ Tag) ergeben die Kette
  `Klassenarbeit --< KlassenarbeitTag >-- Tag --< ExerciseTag >-- Exercise`; zusätzlich existiert die
  direkte `KlassenarbeitExercise` (`:92`). Eine Oberfläche, die diese Kette **zeigt**, existiert nicht.
- **Ein Paging/Sortier-Muster für Zeitreihen ist bereits etabliert und dient als Vorlage.**
  `backend/Pugling.Api/Controllers/Student/PlanOverviewController.cs:58-75`
  (`GET study-plans/{planId}/overview/progress`) nimmt `from`/`to`/`sort`/`skip`/`take` und liefert
  `X-Total-Count` — exakt das Muster, das die neue Zeitachse für mehrere Kinder braucht, nur bisher
  **innerhalb eines Plans**, nicht kind-übergreifend.
- **Das `?childId=`-Filtermuster für kind-übergreifende Aggregate ist etabliert.**
  `backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:48-49`
  (`List([FromQuery] int? childId = null, …)`) ist der Präzedenzfall für „top-level Aggregat, das nur
  nach Kind filtert" (siehe `CLAUDE.md` → Konventionen, Abschnitt „Eigentum").
- **Es gibt keine Chart-Bibliothek im Frontend.** `frontend/package.json` enthält weder `d3`, `recharts`,
  `victory`, `visx` noch `nivo` — bestätigt.
- **`prefersReducedMotion` existiert bereits als geteilte Regel.** `frontend/src/lib/ui.ts:22-25`.
- **Die Woche beginnt am Montag** – so rechnet schon die Wochen-Pflicht (`PositionProgressService.cs:46`,
  `PeriodStart`); die Zeitachse übernimmt diese Grenze, statt eine zweite zu erfinden.
- **Ein Tagesverlauf über eine Laufzeit existiert schon als Schleife** (`PositionProgressService.ProgressAsync`,
  `:529-539`: je Tag ein `ComputeDayAsync`) – die neue Aggregation ist dieselbe Schleife über mehrere Pläne/Kinder.
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

8. **Schnitt auf einen Sprint: der Kind-Hub-Reiter „Zeitachse" (alte AK 4) entfällt; das Aufklappen führt aus
   der Haushaltszeile per Link in den vorhandenen Lernstand `/vater/kind/:id/lernstand`.** Stakeholder-Entscheidung im Auftrag des Nutzers
   (Backlog-Lauf 2026-11-15). Begründung: die Rolle des Vaters braucht den Überblick über
   die Zeit; der Weg „Haushalt → Kind → Wort" bleibt vollständig, weil die Wort-Ebene schon existiert. Ein dritter
   Reiter wäre dieselbe Darstellung an zweiter Stelle mit eigenem Lade-/Fehlerzustand – das war der Posten, der die
   Story auf L hob. Die vereinbarte Teilung (a) Endpunkt + Tests, (b) Frontend bleibt die **Bau-Reihenfolge**
   innerhalb dieser einen Story, keine zweite Id. Kosten: ein Kind über die Zeit sieht der Vater nur in der
   Haushaltsansicht (seine Zeile, oder `?childId=`), nicht im Kind-Hub. Die Nutzer-Entscheidung G-7 (Blättern über
   Kinder) bleibt unberührt; Entscheidung 3 (autonom getroffen 2026-08-03) wird damit für die zweite Ansicht
   zurückgestellt, nicht umgestoßen – der Reiter kann als eigene Idee nachkommen.
9. **Der Zeitraum ist gedeckelt: höchstens 92 Tage je Aufruf (`400 validation_error` darüber).**
   Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-15). Begründung: die Kosten wachsen mit
   Tagen × Plänen × Kindern (`ComputeDayAsync` je Tag; nicht gemessen, gerechnet: ein Quartal bei zwei Kindern mit
   je einem Plan sind ~184 Aufrufe); ein Quartal deckt Tag/Woche/Monat ab, eine Jahresansicht ist nicht verlangt.
   Kosten: wer weiter zurück will, verschiebt den Zeitraum.

## Akzeptanzkriterien

1. `GET api/v1/supervisor/children/overview` liefert für `granularity=day|week|month` je Kind eine
   Periodenreihe (`goalsTotal`/`goalsMet`/`points`/`dutyDone`) im Bereich `from`..`to`; `skip`/`take` blättern
   **über die Kinder** (Nutzer-Entscheidung G-7), `X-Total-Count` = Zahl der Kinder, optional gefiltert über
   `?childId=` (ein fremdes Kind erscheint nie); die bestehende `daily-overview` bleibt unverändert erreichbar.
   *Rot, wenn* `X-Total-Count` die Perioden zählt oder ein nicht betreutes Kind in der Antwort steht.
   Zusatz: Zeitraum über 92 Tage oder `from > to` → `400 validation_error` (Entscheidung 9). *Rot, wenn* die Grenze
   fehlt.
2. Woche = Montag bis Sonntag (`PeriodStart`), Monat = Kalendermonat, beides in UTC; eine Periode ist die Summe
   ihrer Tage im Bereich, `dutyDone` einer Periode heißt „an jedem Tag mit Tagesziel erfüllt". Keine neue
   `GoalCadence`, kein Schema-Wechsel. *Rot, wenn* ein Sonntag in die Folgewoche fällt (Grenztag-Test So/Mo und
   31./1.) oder der Monat als 4 × 7 Tage gerechnet wird.
3. Die Haushalts-Übersicht (`/vater`) zeigt neben der heutigen Tabelle einen Umschalter Tag/Woche/Monat
   mit einer handgebauten Balken-Darstellung je Kind (Ziele erfüllt / gesamt je Periode). *Rot (Vitest), wenn* der
   Umschalter „Woche" nicht so viele Balken rendert, wie die Antwort Perioden trägt.
4. ~~Der Kind-Hub bekommt einen dritten Reiter „Zeitachse".~~ (entfallen, Entscheidung 8) Jede Kindzeile der
   Haushalts-Übersicht verlinkt auf `/vater/kind/:id/lernstand` – dort liegt die Wort-Ebene. *Rot (Vitest), wenn* der
   Link fehlt oder auf ein anderes Kind zeigt.
5. Die Darstellung respektiert `prefersReducedMotion` und trägt bei jedem Status zusätzlich zur Farbe
   Text oder Symbol. *Rot (Vitest), wenn* ein Balken seinen Status nur über die Farbklasse zeigt (Prüfung auf
   zugänglichen Namen/Text je Balken).
6. Kein neues npm-Paket für Diagramme wird installiert.
7. Tätigkeits-Zeitleiste (Sitzungen/Käufe/Missionen als chronologisches Log) und eine dedizierte
   Klausur↔Übung-Ansicht sind **nicht** Teil dieser Story (Entscheidung 6).

## Schätzung

**Größe: M** (bis 2026-11-15: L) — nach Entscheidung 8 bleibt **ein** neuer Endpunkt mit Gruppierung und
**eine** Frontend-Einbaustelle mit handgebauten Balken. Vergleich: in der Breite wie B-538 (M: Regeln in einem
Dienst, ein additives Vertragsstück, eine Oberfläche); mehr als B-01 (S), weil Backend und Frontend nachziehen;
kein L mehr, weil die zweite Einbaustelle mit eigenem Lade-/Fehlerzustand entfällt und keine Migration entsteht.

- **`wo`: beides** — Backend zuerst (API-First): neuer Endpunkt und Gruppierungslogik in
  `ChildrenDashboardService`, danach die eine Frontend-Integration (`VaterDashboard.tsx`).
- **`migration`: nein** — keine Schema-Änderung; die Aggregation liest ausschließlich bereits
  vorhandene, tagesgenaue Werte neu gruppiert.
- **`vertragsbruch`: nein** — ein neuer, additiver Endpunkt mit neuem DTO; `daily-overview` und alle
  bestehenden Routen bleiben unverändert.
- **Risiken**: (a) SQLite-Aggregation über Kalenderwochen/-monate ist fehleranfällig bei Zeitzonen nahe
  Mitternacht (`CLAUDE.md` → „Zeit/UTC") — Wochen-/Monatsgrenzen strikt in UTC ziehen, wie die übrige
  Tageslogik. (b) Ohne Chart-Bibliothek kostet die Balkendarstellung mehr Handarbeit als ein
  Library-Aufruf — akzeptiert (Entscheidung 4). (d) **Laufzeit:** `ComputeDayAsync` lädt je Aufruf die Positionen neu; bei 92 Tagen × Plänen × Kindern
  zuerst messen (Integrationstest mit Zeitausgabe genügt), erst bei Bedarf die Positionen je Plan einmal laden –
  nicht vorbeugend umbauen. (e) `EndpointCoverageGuard` (`Pugling.Api.Tests/EndpointCoverageGuard.cs:30`) zählt
  die neue Action. (c) Die in Entscheidung 6 zurückgestellten Brocken
  (Tätigkeits-Zeitleiste, Klausur-Sicht) bleiben offene, potenziell wertvolle Folgeideen — bewusst nicht
  Teil dieser Schätzung.
- **Angriffsplan** (Teilung a/b als Reihenfolge): (a1) Contracts-DTO für die Periodenreihe (additiv);
  (a2) `ChildrenDashboardService`: die `ComputeDayAsync`-Schleife über den Bereich laufen lassen und nach
  `granularity` über `PeriodStart` bzw. Monatsanfang gruppieren; (a3) Action unter `supervisor/children/overview`
  (eigene Route neben `daily-overview`, `Roles.Supervisor`, Paging über Kinder mit `X-Total-Count`); (a4)
  Client-Zeile, OpenAPI, `contract.ts`; (b1) Frontend: Umschalter + Balkenkomponente in `VaterDashboard.tsx`, Link
  je Kindzeile zum Lernstand.
- **Testweg**: neue Integrationstestklasse in `Pugling.Api.Tests` (mirrored an `ChildrenDashboardTests`/
  `PositionGoalOverviewTests`) für die Gruppierungslogik — je ein Fall für `day`/`week`/`month`, Grenztag
  an einer Kalenderwochen-/Monatsgrenze, `?childId=`-Filter und Paging/`X-Total-Count`. Frontend:
  Vitest-Komponententest für die neue Zeitachsen-Komponente (Umschalter rendert die richtige Periodenzahl
  an Balken, Link je Kind, Text neben der Farbe). E2E: neue Spec `frontend/e2e/vater-zeitachse.spec.ts` (eine
  Fläche je Spec): Kind erfüllt heute die Pflicht → Vater schaltet auf „Woche" → der Balken der laufenden Woche
  nennt „1 von 1"; zählt als Rollengang. Reviewer `pugling-reviewer` und `frontend-reviewer`.

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
- **2026-10-17** — Prioritäts-Beleg von außen: Khan Academy bietet Eltern drei fertige Reports (Aktivität, Einzelbericht, Zuweisungs-Scores) — Vergleich in `docs/khan-academy-vergleich-plan.md`, Etappe 7. Keine inhaltliche Änderung an Entscheidungen oder Schätzung.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt, kein Teil geliefert; Zeilen verschoben (`VaterDashboard.tsx:46-68`, `ChildrenDashboardService.cs:16-50`). Zwei Punkte vor dem Bau: (1) **AK 1 sagt nicht, was `skip`/`take`/`sort` blättert** – Kinder oder Perioden; das ist eine Entscheidung, die die Story nicht hergibt. (2) `ComputeDayAsync` je Kind × Plan × Tag – eine Monatsansicht wären ~31 × Pläne × Kinder Aufrufe (nicht gemessen). Teilungsvorschlag: (a) Endpunkt mit Integrationstests, (b) die zwei Einbaustellen im Frontend mit Vitest.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-7 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): `skip`/`take`/`sort` in AK 1 blättern **über die Kinder** – eine Zeile je Kind, der Zeitraum (Woche/Monat) ist ein Filter, kein Blätter-Achse. Damit gibt es keine offene Produktfrage mehr; die Story ist nachtlauffähig, Teilen bleibt Schritt 0 (a: Endpunkt mit Integrationstests, b: die zwei Einbaustellen im Frontend). Paket NP-13.
- **2026-11-05** — Prioritäts-Beleg von außen: Baobab zeigt Eltern in Echtzeit, was sitzt und was noch nicht – eine Ansicht „sitzt / sitzt nicht" ist die einfachste Form dieser Story. Vergleich in `docs/baobab-vergleich-plan.md` (Sammel-Story [B-374](B-374-baobab-vergleich-sammel-story.md)). Keine Änderung an Entscheidungen oder Schätzung.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt, kein Teil geliefert – einziger Supervisor-Überblick bleibt `daily-overview` (`ChildrenDashboardController.cs:13`, Aggregation `ChildrenDashboardService.cs:17-36`), Haushaltssicht weiter als Tabelle (`VaterDashboard.tsx:18,54`), Kind-Hub mit zwei Reitern (`VaterLernstand.tsx:34-55`), keine Chart-Bibliothek in `frontend/package.json`, kein Endpunkt `children/overview`. Die neue Wochen-Münzsicht `me/points/week` (`MeController.cs:42`, B-253) ist Student-seitig und deckt die Story nicht ab.
- **2026-11-15** — Backlog-Lauf 2026-11-15: Abgleich am Code, bleibt `geschaetzt`. Zeilen aufgefrischt
  (`ChildrenDashboardController.cs:13`, `ChildrenDashboardService.cs:17-50`, `VaterDashboard.tsx:53-75`,
  `VaterLernstand.tsx:34-55`, `StudyPlansController.cs:48-49`, `PlanOverviewController.cs:58-75`); weiter kein
  `children/overview`, keine Chart-Bibliothek in `frontend/package.json`. **Neu geschätzt L → M** durch zwei
  Stakeholder-Entscheidungen im Auftrag des Nutzers: 8 (Kind-Hub-Reiter entfällt, Link in den Lernstand; Teilung a/b
  bleibt Bau-Reihenfolge in einer Story) und 9 (Zeitraum ≤ 92 Tage). AKs mit roter Probe je Fall, E2E-Spec benannt.
- **2026-11-15** — `geschaetzt` → **`abgenommen`** in Sprint 79 des Backlog-Laufs 2026-11-15, Code-Commit `c2353a57`.
  Gebaut nach den Entscheidungen 1–9: `GET supervisor/children/overview` (`ChildrenDashboardService.TimelineAsync` über
  dieselbe `ComputeDayAsync`-Schleife, Woche über das jetzt öffentliche `PositionProgressService.WeekMonday`),
  `ChildrenTimeline` im Dashboard mit Umschalter Tag/Woche/Monat, handgebauten Balken (kein npm-Paket, AK 6) und Link je
  Kind in den Lernstand.
  **Review-Funde behoben:** `EndpointCoverageGuard` stand auf 282 (der Zähler schlägt nur im Dispose an, „Passed!“
  täuschte – jetzt 284, Exit-Code geprüft); ein `to` in der Zukunft las kommende Tage als verpasste Pflicht (endet jetzt
  heute); unbekanntes Raster → 400; `ChildPeriod`-Doku versprach „Tagesziele“, gezählt werden Ziele jeder Taktung je
  Tag wie in der Tagesübersicht – Doku berichtigt; Zeitachse lud nach „Kind anlegen“ nicht neu; leere Fläche beim
  Rasterwechsel; erster Wochenbalken eine halbe Woche (Raster beginnt jetzt am Montag); Status nur als `aria-label` an
  einem nicht bedienbaren Listeneintrag (jetzt Text im Element) und ohne sichtbare Achse; Monat als Datumsspanne.
  **Rest als Story:** [B-558](B-558-zeitachse-zeigt-abgeschaltete-plaene-nicht.md) – abgeschaltete Pläne fallen aus der
  Vergangenheit (dieselbe Regel wie Tagesübersicht und Malus; ehrlich dokumentiert).
  - AK 1 → `ChildrenOverviewTests.Blaettert_Ueber_Die_Kinder_…`, `…Zeitraum_Ist_Begrenzt`; Mutanten „childId-Filter“,
    „nur eigene Kinder“, „Zählung Kinder“, „92 Tage“ → rot.
  - AK 2 → `…Gruppiert_Nach_Tag_Woche_Und_Kalendermonat` (So 29.03./Mo 30.03., 31.03./01.04.),
    `…Pflicht_Der_Periode_Folgt_Den_Tagen`, `…Zukunft_Endet_Heute_…`; Mutanten „Woche ab Montag“, „Kalendermonat“,
    „Pflicht je Periode“, „Zukunft endet heute“ → rot.
  - AK 3 → Vitest `ChildrenTimeline.test.tsx` „zeichnet je Periode …“; Mutante „Raster fragt neu“ → rot.
  - AK 4 → „führt aus jeder Kindzeile in deren Lernstand“; Mutante „Link je Kind“ → rot.
  - AK 5 → „nennt je Balken den Status in Worten“; Mutante „Text je Balken“ → rot. Keine Animation, darum keine
    `prefersReducedMotion`-Weiche nötig (Kommentar an der Komponente).
  - AK 6 → kein neues npm-Paket (`package.json` unverändert).
  - Review-Fälle → „beginnt das Wochen-Raster an einem Montag“, „nennt einen Monat beim Namen“; Mutanten → rot.
  - Belege: volle Suite **1693/1693** (Exit 0), Vitest **1113/1113**, `tsc -b` grün; Rollengang Vater: E2E
    `vater-zeitachse.spec.ts` (Kind erfüllt heute → Tag-Raster 14 Balken, heutiger „1 von 1 Zielen – Pflicht geschafft“,
    Monat ebenso, Link) im Lauf mit **13 passed**. Reviewer: `pugling-reviewer`, `frontend-reviewer`.
- **2026-11-15** — Prioritäts-Beleg von außen (sofatutor-Vergleich): In unabhängigen Bewertungen loben Eltern bei sofatutor vor allem, dass sie den Fortschritt je Kind sehen (Aktivitäten und Merklisten je Geschwisterprofil). Vergleich in `docs/sofatutor-vergleich-plan.md` (Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)). Keine Änderung an Entscheidungen oder Schätzung.
