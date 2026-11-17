---
tags: [typ/story, status/in-arbeit, bereich/lehrplan, rolle/supervisor, rolle/student]
aliases: [Ein Plan je Fach, Fach Pflicht am Plan, Unit am Plan]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: ja
quelle: B-537 (Teil b, Entscheidungen 1–2)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-539 · Ein aktiver Plan je Fach statt je Kind

Teil b von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Begründungen der Nutzer-Entscheidungen stehen
dort. **Nach [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md) (abgenommen) und
[B-538](B-538-bonus-muenzregeln-form-und-degression.md) bauen** – alle drei ändern Tagesbox und Bonus-Sperre.
**[B-580](B-580-plan-aus-unit.md) braucht diese Story** (Fach und Unit am Plan).

> **Geteilt am 2026-11-17 (Entscheidung 9):** Teil B (AK 5, 6) ist [B-606](B-606-kind-tagessicht-ueber-alle-plaene.md)
> und wird **vor** dieser Story gebaut. B-539 behält AK 1–4 (L). AK 5 und 6 stehen unten nur noch als Verweis.

## User Story

Als *Vater* möchte ich je Fach einen eigenen Lehrplan aktiv haben, damit mein Kind in jedem Fach täglich etwas tut
und jedes Fach in seinem eigenen Takt zum nächsten Kapitel wechselt. Als *Sohn* möchte ich alle Pflichten des
Tages an einer Stelle sehen, egal aus welchem Plan.

## Ist-Stand am Code

Gemessen am 2026-11-15 (Stand `d41df00a`), Pfade relativ zu `backend/Pugling.Api/`, sofern nicht anders genannt.
B-537 Punkte 1 und 3 sind nachgeprüft; die Zeilen haben sich seither verschoben.

### Plan-Entität und Vertrag

- `Models/StudyPlanEntities.cs:73-75`: `SubjectId int?` („for classification/filtering only“), **keine** Unit-Spalte.
  FK zum Fach mit `SetNull` (`Data/PuglingDbContext.cs:635-637`) – ein gelöschtes Fach hinterlässt einen Plan ohne Fach.
- `../Pugling.Contracts/Supervisor/StudyPlanDtos.cs:33-34` `CreatePlanDto(int ChildId, string Title, int? SubjectId, …)`,
  `:37-38` `UpdatePlanDto(…, int? SubjectId, …)` (PATCH: `null` = bleibt), `:7-17` `PlanResponse` mit
  `IsPlayable`-Doku „the one currently playable plan“.
- `Controllers/Supervisor/StudyPlansController.cs:93` prüft ein mitgegebenes Fach nur auf Existenz, `:102` übernimmt es
  ungeprüft optional; PATCH `:145-149` ebenso.

#### Die Ein-Plan-Sperre – die einzige Stelle, die „ein aktiver Plan je Kind“ erzwingt

- `StudyPlansController.cs:112` (Anlegen) und `:155` (PATCH) rufen `DeactivateSiblingPlansAsync` (`:121-123`), das
  **alle** anderen aktiven Pläne des Kindes abschaltet.
- `Services/Shared/PositionPlayService.cs:66-67` (`PlanPlayableForChild`) urteilt **je Plan** (aktiv + Laufzeit) – es
  gibt keine zweite Sperre; mehrere aktive Pläne wären heute schon einzeln spielbar.
- Kommentar/Doku, die die Regel nennen: `StudyPlansController.cs:55-58` (Schüler-Liste „its one playable plan“),
  `frontend/src/lib/fieldHelp.ts:148-149`, `frontend/src/vater/VaterPlaene.tsx:24`, `:91`,
  `frontend/src/vater/VaterPlanDetail.tsx:67`.

##### Was schon über alle aktiven Pläne rechnet (bleibt)

- Bonus-Sperre: `Services/Student/ExtraTrainingService.cs:243-260` (`GateAsync`) – alle aktiven, laufenden Pläne,
  offen erst, wenn jeder seine Belohnungs-Ziele erfüllt hat.
- Malus: `Services/Shared/PositionProgressService.cs:417-437` (`SettleClosedPeriodsAsync`) lädt alle Positionen des
  Kindes über alle Pläne; `:341-342` (`PlanDueForPeriod`) verschont inaktive Pläne – je Position, also mehrplanfähig.
- Vater-Dashboard: `Services/Supervisor/ChildrenDashboardService.cs:29-44` – Pflicht erfüllt, wenn **jeder** Plan mit
  Tagesziel erfüllt ist.
- Schüler-Planliste: `Controllers/Student/StudentPlansController.cs:42` und `StudyPlansController.cs:58` liefern alle
  aktiven, laufenden Pläne.

###### Was je Plan rechnet und mit zwei aktiven Plänen falsch würde (die eigentliche Arbeit)

- **Tagesbox:** `Services/Shared/DailyBoxService.cs:34-39` verdient die Box aus der `DayOverview` **eines** Plans und
  dessen Serie (`progress.StreakBoundedAsync(plan, …)`). Aufgerufen nach jeder Übung/jedem Test mit dem gespielten
  Plan (`Controllers/Student/PositionPracticeController.cs:610-613`, `Controllers/Student/PositionTestsController.cs:442-446`)
  – mit zwei Plänen öffnet das erste erfüllte Fach die Box, während das zweite offen ist.
- **Serie (Box-Multiplikator):** `PositionProgressService.cs:569-578` (`StreakBoundedAsync`) zählt die Tage **eines**
  Plans.
- **Serie/Tage für Missionen und Auszeichnungen:** `Services/Shared/MetricsService.cs:60-69` (`CompleteDaysAsync`)
  nimmt die **Vereinigung** über alle Pläne, auch inaktive: ein Tag zählt, sobald *irgendein* Plan seine Pflicht
  erfüllt hat.
- **Tagesmission der Arcade:** `Controllers/Student/PlanOverviewController.cs:29-41` liefert Pflicht und Serie **eines**
  Plans; die Tagesrechnung selbst ist `PositionProgressService.cs:211-253` (`ComputeDayAsync`, je Plan),
  `RewardObligations` `:262-267`.
- **Arcade:** `frontend/src/sohn/useChosenPlan.ts:13-25` wählt **einen** Plan (gemerkt → spielbar → erster);
  `frontend/src/sohn/SohnHome.tsx:40`, `:59`, `:70` zeigt nur dessen Übersicht, `:97-100` einen Plan-Umschalter bei mehr
  als einem Plan.

**Wer Pläne anlegt** (alle ohne Pflicht-Fach): Vater-Wizard `frontend/src/vater/VaterWizard.tsx:81` (Fach optional,
`:397` `subjectId === "" ? null`), KI-Agent `../Pugling.Agent.Creator/PlanPlanner.cs:211-214` (Fach aus der Basis,
kann `null` sein), Tests: 27 direkte `POST supervisor/study-plans` in `Pugling.Api.Tests`, keiner mit `subjectId`; der
Helfer `../Pugling.Api.Tests/TestApi.cs:93`; `AssignAgentTests.cs:38`, `:47` mit `null`; 17 E2E-Dateien posten Pläne.

**Messung an einer Kopie der echten `pugling.db`** (2026-11-15; Kopie im Scratchpad, Original nicht geöffnet):

| Plan | Kind | Fach am Plan | Fächer der Positionen | aktiv |
| --- | --- | --- | --- | --- |
| 5 „Unit 1 This is London“ | Theodor (1) | Englisch | 4× Englisch | ja |
| 6 „Französisch: Au début bis Unité 2“ | Friedrich (3) | Französisch | 15× Französisch | ja |
| 1 Demo „Alle Lernarten“ | Demo-Kind (2) | Englisch | 11 Englisch, 3 Erdkunde, 2 Mathe | ja |
| 7 „Englisch – Regelmäßig üben“ | Testjunge (4) | Englisch | 4× Englisch | ja |

**Pläne ohne Fach: 0. Ohne ableitbares Fach: 0.** Bei keinem Plan weicht das gesetzte Fach von der Mehrheit seiner
Positionen ab. Je Kind genau ein aktiver Plan. Die Söhne haben je **ein** Fach im Plan – „in jedem Fach täglich“
scheitert heute tatsächlich an der Ein-Plan-Regel (B-537, Lücke 5, damit beantwortet).

## Die echte Lücke

1. Das Fach ist am Plan optional, und es gibt keine Unit am Plan.
2. Die Aktivierung schaltet alle anderen Pläne des Kindes ab statt nur die desselben Fachs.
3. Tagesbox, ihre Serie, die Metrik-Serie und die Arcade-Tagesmission rechnen je Plan; Bonus-Sperre, Malus und
   Vater-Dashboard tun es schon über alle aktiven Pläne. Ohne 3 macht 2 die Box zur Belohnung für das *leichteste*
   Fach – genau die Lücke, gegen die die Ein-Plan-Regel gebaut war.

## Offene Punkte

1. ~~Fach wird Pflicht – was ist mit Bestandsplänen ohne Fach?~~ → Entscheidung 3 (Nutzer-Entscheidung 2 umgesetzt).
2. ~~Wie wird die Unit gespeichert, und wie hängt sie am Fach?~~ → Entscheidung 4.
3. ~~Darf ein Plan Positionen aus anderen Fächern tragen?~~ → Entscheidung 5.
4. ~~Welche Pläne zählen für eine **vergangene** Serie, wenn das Modell keinen Aktivierungsverlauf kennt?~~ →
   Entscheidung 6.
5. ~~Wie zeigt die Arcade mehrere Pläne – ein neuer Tages-Endpunkt oder N Übersichten im Client?~~ → Entscheidung 7.
6. ~~Eigener Fehlercode für „Fach fehlt“?~~ → Entscheidung 8.
7. ~~Passt das in eine Story?~~ → Entscheidung 9.

## Entscheidungen

Nutzer-Entscheidungen aus B-537 (im Dialog, nicht umzustoßen): **1** ein aktiver Plan je Fach; **2** Fach Pflicht am
Plan, Unit optional, Fach bestehender Pläne aus der Mehrheit ihrer Positionen. Die folgenden setzen sie um.

<!-- markdownlint-disable MD029 -->

3. **Fach Pflicht im Vertrag, Spalte bleibt nullable.** `CreatePlanDto.SubjectId` wird `int` (Pflicht) bzw. aus der
   Unit abgeleitet; ein PATCH auf einen Plan **ohne** Fach muss eines mitbringen – Ausnahme: ein PATCH, der nur
   `active: false` setzt (Abschalten darf nie an einem Pflichtfeld scheitern). Die DB-Spalte bleibt `NULL`-fähig, weil
   die Migration nicht jeden Bestand ableiten kann und das Löschen eines Fachs (`SetNull`) neue leere Pläne erzeugt.
   *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: Nutzer-Entscheidung 2 und AK 3 verlangen „leer, bis
   der Plan bearbeitet wird“; eine `NOT NULL`-Spalte hieße, die Migration müsste ein Fach erfinden. Kosten:
   `vertragsbruch: ja` (bisher gültige Requests ohne Fach liefern `400`); Client, Agent, Wizard und rund 45 Test-Aufrufe
   ziehen nach.
4. **Unit als `StudyPlan.SeriesUnitId int?`, FK auf `SeriesUnit` mit `OnDelete(SetNull)`.** Mit Unit wird das Fach
   aus der Reihe übernommen; gibt der Request zusätzlich ein **anderes** Fach an, ist das `400 validation_error`. Hat
   die Reihe keins (laut B-106 kann sie dann keine Übungen tragen), muss es mitgegeben werden. PATCH kann die Unit
   setzen und über einen Schalter `ClearSeriesUnit` (Muster `ClearStage`) entfernen. *Stakeholder-Entscheidung im
   Auftrag des Nutzers.* Begründung: `SetNull` wie beim Fach – ein Plan überlebt den Katalog; die Konsistenzprüfung
   verhindert einen Englisch-Plan mit Französisch-Unit. Kosten: neue Spalte + Index (Tor G-Regeln: Löschverhalten
   explizit), Schalter im PATCH (`PatchSemanticsTests`).
5. **Positionen aus anderen Fächern bleiben erlaubt.** Das Plan-Fach ist das Gruppen-Merkmal der Aktivierungsregel,
   keine Sperre für Positionen. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: der Demo-Plan trägt drei
   Fächer; eine Sperre bräche Bestand und Klassenarbeitspläne über Fächergrenzen. Kosten: ein Vater kann die Regel
   unterlaufen, indem er Mathe in den Englisch-Plan legt – das schadet nur seiner eigenen Steuerung, nicht dem Kind.
6. **Eine Kind-Tagessicht über alle aktiven, laufenden Pläne ist die eine Quelle für Pflicht, Belohnungs-Sperre, Box
   und heutige Serie.** Für **vergangene** Tage (Serie, Missionen, Auszeichnungen) gilt: ein Tag ist vollständig, wenn
   mindestens ein Plan mit Pflicht an dem Tag sie erfüllt hat **und** kein heute aktiver Plan mit Pflicht an dem Tag
   offen ist. Inaktive Pläne zählen nur zu Gunsten des Kindes. *Stakeholder-Entscheidung im Auftrag des Nutzers.*
   Begründung: das Modell kennt keinen Aktivierungsverlauf; dieselbe Fairness-Regel trägt schon der Malus
   (`PlanDueForPeriod`: ein inaktiver Plan straft nicht). Ein Kapitelwechsel (alter Plan aus, neuer an) darf die Serie
   des Sohns nicht rückwirkend brechen. Kosten: schaltet der Vater einen Plan ab, kann ein früher offener Tag
   nachträglich vollständig werden – die Serie wächst dann; nur zu Gunsten des Kindes, hingenommen.
7. **Neuer Endpunkt `GET student/today`** (Kind-Tagessicht: Positionen aller aktiven, laufenden Pläne mit `PlanId`,
   Plan-Titel und Fachname, Pflichtstand, Serie, Box-Status). Die Arcade-Startseite zeigt ihn, nach Fach gruppiert;
   eine Position führt wie bisher in den Weg ihres Plans. `GET …/study-plans/{id}/overview` bleibt, seine Serie wird die
   Kind-Serie. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: API-First – die Aggregation ist eine
   Geschäftsregel und gehört nicht in N Client-Aufrufe; dieselbe Rechnung braucht die Box ohnehin serverseitig.
   Kosten: ein neuer Endpunkt (Tor `EndpointCoverageGuard` + UI), additive Felder an `PositionStatus`.
8. **Kein eigener Fehlercode für ein fehlendes Fach: `validation_error`.** *Stakeholder-Entscheidung im Auftrag des
   Nutzers.* Begründung: ein Pflichtfeld ist der Normalfall dieses Codes; das Formular sperrt Speichern vorher
   (`<Pflicht />`, B-233). Kosten: der Client unterscheidet die Ursache nur am `detail`.
9. **Teilung vorgeschlagen, nicht vollzogen:** Teil B „Kind-Tagessicht“ (AK 5, 6; Entscheidungen 6, 7) als eigene
   Story **vor** Teil A (AK 1–4; Entscheidungen 3–5, 8) bauen. *Stakeholder-Entscheidung im Auftrag des Nutzers.*
   Begründung: Teil B ist mit einem aktiven Plan je Kind verhaltensneutral und kann allein ausgeliefert werden;
   Teil A ohne Teil B öffnete die Box-Lücke (Lücke 3). Kosten: zwei Abnahmen, ein Rollengang mehr; die neue Story legt
   der Lauf an (diese Bearbeitung durfte keine Datei anlegen).

<!-- markdownlint-enable MD029 -->

## Akzeptanzkriterien

Final. Je Fall die Änderung, die seinen Test rot macht.

1. `POST supervisor/study-plans` ohne `subjectId` und ohne `seriesUnitId` → `400 validation_error`; mit Unit wird das
   Fach aus der Reihe übernommen (Antwort trägt beide); Unit + abweichendes Fach → `400`. Ein PATCH auf einen Plan ohne
   Fach ohne `subjectId` → `400`, nur `active: false` → `200`. *Rot ohne die Pflichtprüfung (heute `201`) bzw. wenn
   das Abschalten an ihr scheitert.*
2. Ein Plan trägt optional eine Unit: `PlanResponse.SeriesUnitId` (+ Label), setzbar beim Anlegen und per PATCH,
   entfernbar per `clearSeriesUnit`; `Pugling.Client` und Vater-Web (Plan-Formular, Wizard: Fach Pflicht, Unit
   optional) zeigen und setzen sie. *Rot, wenn der PATCH-Schalter fehlt (`PatchSemanticsTests`).*
3. Migration (an die Kette angehängt): neue Spalte `SeriesUnitId`; Pläne ohne Fach bekommen das Fach der Mehrheit
   ihrer Positionen (Gleichstand oder keine Position ⇒ bleibt leer). Migrationstest nach Vorbild
   `DailyBoxOpenedAtMigrationTests.cs`: Plan ohne Fach mit 2× Englisch, 1× Mathe ⇒ Englisch; Gleichstand ⇒ leer;
   gesetztes Fach bleibt. Die echte `pugling.db` (Kopie) startet danach, die Pläne 5 und 6 sind unverändert spielbar.
   *Rot, wenn die Migration ein gesetztes Fach überschreibt oder bei Gleichstand rät.*
4. Aktivieren (Anlegen aktiv, PATCH `active: true`, PATCH Fachwechsel, PATCH Kindwechsel) schaltet nur die aktiven
   Pläne **desselben Fachs** desselben Kindes ab; Pläne ohne Fach bilden eine Gruppe. Regressionstest: Englisch aktiv,
   Französisch anlegen ⇒ beide aktiv; zweiter Englisch-Plan ⇒ der erste aus. *Rot mit der heutigen
   `DeactivateSiblingPlansAsync` (beim ersten Fall) bzw. ohne jede Abschaltung (beim zweiten).*
5. Tagespflicht, Tagesbox, Bonus-Sperre, Malus und Serie rechnen über **alle** aktiven, laufenden Pläne des Kindes.
   Regressionstest: zwei Fächer, Pflicht des einen erfüllt, des anderen offen ⇒ `student/today` meldet Pflicht offen,
   **keine** Box verdient, Bonus gesperrt; beide erfüllt ⇒ genau eine Box. Serie: an Tag 1 beide erfüllt, an Tag 2 nur
   einer ⇒ Serie bricht; wird der offene Plan danach abgeschaltet, zählt Tag 2 (Entscheidung 6). *Rot mit der heutigen
   Box-Vergabe je Plan bzw. der Vereinigung in `MetricsService`.*
6. Die Arcade-Startseite zeigt die Pflichten aller aktiven Pläne nach Fach gruppiert (Fachname sichtbar); eine
   Position öffnet den Weg ihres Plans. *Rot, wenn die Startseite weiter `useChosenPlan` folgt* (Vitest am
   Gruppier-Baustein, E2E am Weg).

## Schätzung

**Ganz: über L** (XL gibt es nicht) – sechs Backend-Stellen, eine Migration mit Rückfüllung, ein Vertragsbruch mit
rund 45 Test-Aufrufern, ein neuer Endpunkt und die Arcade-Startseite. Vergleich: eine DB-Umbau-Etappe wie E6 (L) *plus*
ein Umbau der Pflicht-Rechnung in der Breite von B-535 (M). **Vorgeschlagene Teilung (Entscheidung 9):**

- **Teil B – Kind-Tagessicht (neue Story, M, zuerst):** AK 5, 6. `migration: nein`, `vertragsbruch: nein` (neuer
  Endpunkt, additive Felder).
- **Teil A – bleibt B-539 (L):** AK 1–4. Die Frontmatter (`groesse: L`, `migration: ja`, `vertragsbruch: ja`, `wo:
  beides`) gilt für Teil A **und** für das Ganze, solange nicht geteilt ist.

### Risiken

- **Test-Welle:** 27 direkte Plan-POSTs, der Helfer `TestApi.cs:93`, `AssignAgentTests.cs`, 17 E2E-Dateien und der
  Seed (`Data/Seed.cs`, `new StudyPlan`) brauchen ein Fach. Zuerst der Helfer mit Vorgabe-Fach, dann die Einzelstellen;
  Tests, die heute „Anlegen deaktiviert den anderen Plan“ prüfen (`AntiCheatTests.cs:233`), brauchen dasselbe Fach.
- **Box doppelt:** die Box-Vergabe läuft auf dem Schreibweg nach jeder Abgabe. Mit Kind-Sicht rechnet sie N Pläne statt
  einen – die Idempotenz-Vorprüfung (`DailyBoxService.cs:37`) muss vor der Aggregation bleiben, sonst kostet jede
  Antwort N Tagesrechnungen.
- **Serie über inaktive Pläne:** `CompleteDaysAsync` liest heute **alle** Pläne des Kindes über ihre ganze Laufzeit;
  die neue Regel (Entscheidung 6) braucht für jeden Tag die Menge „heute aktive Pläne mit Pflicht“ – auf Laufzeit und
  Pflicht-Positionen achten (B-460-Lehre: Tagesrechnung je Tag ist teuer; `MaxStreakLookbackDays` begrenzt nur die Box).
- **Migration gegen die echte DB:** Rückfüllung als SQL in `Up` (Mehrheit über `PlanPositions → Exercises →
  SeriesUnits → TextbookSeries`), keine Code-Logik; an der Kopie heute 0 betroffene Zeilen – der Test muss die Fälle
  selbst herstellen. `heimnetz.ps1` sichert vor dem Start (`.bak-auto-…`).
- **Doku-Capture und Tutorials:** `docs/tutorial-supervisor.md`/`docs/api-examples` zeigen Pläne ohne Fach.
- **Hinweistexte:** vier Stellen im Vater-Web nennen die alte Regel (Ist-Stand) – alle auf „je Fach“ umstellen.

#### Angriffsplan (Backend zuerst; Teil B vor Teil A)

1. *Teil B:* `PositionProgressService` bekommt `ComputeChildDayAsync(childId, day)` (Vereinigung der Tages-Übersichten
   aller aktiven, laufenden Pläne; Pflicht/Belohnung = alle) und eine Kind-Serie nach Entscheidung 6;
   `DailyBoxService.EvaluateAndAwardAsync(childId, day)` nutzt beides; `ExtraTrainingService.GateAsync` und
   `MetricsService.CompleteDaysAsync` ziehen auf dieselbe Quelle; `GET student/today` + `PositionStatus` (additiv:
   `PlanId`, `PlanTitle`, `SubjectName`); Overview-Serie = Kind-Serie. Tests AK 5.
2. *Teil B Frontend:* Arcade-Startseite auf `student/today`, Gruppierung nach Fach; `useChosenPlan` bleibt für den Weg.
3. *Teil A:* Migration (Spalte, Index, FK `SetNull`, Rückfüllung) + Migrationstest; Entität/`PuglingDbContext`.
4. *Teil A:* Vertrag (`SubjectId` Pflicht, `SeriesUnitId`, `ClearSeriesUnit`), `StudyPlansController` (Ableitung aus
   Unit, Konsistenz, Pflicht, Abschaltung je Fach), Client, `TestApi`-Helfer, Test-Welle, Agent (`PlanPlanner` muss
   ein Fach liefern – aus der Unit der erzeugten Übungen).
5. *Teil A Frontend:* Plan-Formular und Wizard (Fach Pflicht, Unit optional), Hinweistexte, `contract.ts`.

##### Testweg

- `Pugling.Api.Tests/AntiCheatTests.cs`: AK 4 (neben `:233`).
- Neue Datei `Pugling.Api.Tests/KindTagessichtTests.cs` (Teil B): AK 5 inkl. Box, Bonus-Sperre, Serie, Metrik.
- `Pugling.Api.Tests/DailyBoxTests.cs`, `ExtraTrainingTests.cs`, `ChildrenDashboardTests.cs`, `PflichtMalusTests.cs`:
  bleiben grün (Malus, Dashboard und Bonus-Sperre rechneten schon über alle Pläne – Gegenprobe).
- Neuer Migrationstest nach `DailyBoxOpenedAtMigrationTests.cs`: AK 3.
- `PatchSemanticsTests` (Schalter `clearSeriesUnit`), Plan-CRUD-Tests für AK 1, 2.
- Vitest: Gruppierung der Startseite; Plan-Formular/Wizard Pflichtfeld (`disabled` bei leerem Fach).
- E2E: neue Spec `frontend/e2e/zwei-faecher.spec.ts` – Vater legt Englisch- und Französisch-Plan an, Sohn sieht beide
  Pflichten, erfüllt eine, Box bleibt zu, erfüllt die zweite, Box kommt. Das ist der Rollengang.
- `/smoke-test` und Start gegen eine **Kopie** der echten DB (Migration läuft, Pläne 5/6 spielbar).
- Reviewer: `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
- 2026-11-15 · Backlog-Lauf 2026-11-15: Ist-Stand am Code nachgetragen (Ein-Plan-Sperre nur in
  `DeactivateSiblingPlansAsync`; Bonus-Sperre, Malus und Dashboard schon mehrplanfähig; Box, Box-Serie, Metrik-Serie und
  Arcade je Plan), an der DB-Kopie 4 Pläne gemessen – 0 ohne Fach, 0 ohne ableitbares Fach. Entscheidungen 3–9 als
  Stakeholder im Auftrag des Nutzers ergänzt; AKs präzisiert, Nutzer-Entscheidungen 1–2 unverändert.
- 2026-11-15 · `geschaetzt` im Backlog-Lauf 2026-11-15: L (für Teil A), beides, Migration ja, Vertragsbruch ja; als Ganzes
  über L, Teilung in Teil B (Kind-Tagessicht, zuerst) und Teil A vorgeschlagen.
- 2026-11-17 · Nachtlauf 2026-11-17: Teilung nach Entscheidung 9 vollzogen – Teil B als B-606 (`geschaetzt`, M) angelegt;
  B-539 behält AK 1–4. Abweichend von README „Teilen" bleibt B-539 bestehen statt `verworfen`, weil Entscheidung 9 das
  so festlegt und B-580 auf diese Id verweist.
- 2026-11-17 · Aus dem Frontend-Review von B-606: die Startseite hat keinen Plan-Umschalter mehr (sie zeigt alle Pläne).
  Die Fortschrittsseite der Arcade (`SohnProgress.tsx`, `useChosenPlan`) zeigt den gemerkten Plan, den „Los" setzt.
  Solange je Kind ein Plan aktiv ist, betrifft das niemanden; **mit Teil A braucht die Fortschrittsseite eine eigene
  Planwahl** – beim Bau von AK 4 mitnehmen.
- 2026-11-17 · `in-arbeit` – Sprint 84 im Nachtlauf 2026-11-17.
- 2026-11-17 · Gebaut (Subagent, Sprint 84). Migration `20261117061523_StudyPlanSeriesUnit` angehängt (Spalte, Index,
  FK `SetNull`, Rückfüllung als SQL nur `WHERE SubjectId IS NULL`, Gleichstand bleibt leer); SchemaGuard-Zeile
  `StudyPlan.SeriesUnitId → SetNull`. Probe gegen eine Kopie der echten DB (`heimnetz.ps1 -Database <kopie> -Port 5205`):
  Migration läuft, Pläne 1/5/6/7/8 mit unverändertem Fach, 55 Positionen / 72 Sitzungen / 37 Versuche wie im Original,
  `PRAGMA foreign_key_check` leer, Übersichten 5/6/8 antworten 200. Planwahl auf der Arcade-Fortschrittsseite mitgebaut
  (Vermerk vom 2026-11-17).
- 2026-11-17 · AK-Fälle → Test, Mutante (`mutant-probe.py`, Debug, nach der letzten Änderung, 09:15):
  - AK 1 ohne Fach → `PlanSubjectTests.cs:71`; A3 (Pflichtprüfung weg) → rot. Unit + abweichendes Fach → `:101`;
    A4 (Konsistenzzeile beim Anlegen weg) → **grün, äquivalent**: `ResolveSubject` liefert dann `(null, …)`, die
    Pflichtzeile antwortet mit demselben `400 validation_error`, nur der `detail`-Text unterscheidet sich (Entscheidung 8:
    kein eigener Code) – kein Beleg für die Zeile, die Regel selbst hält. PATCH nur `active:false` → `:139`; A5 → rot.
  - AK 2 Unit setzen/entfernen → `:166`, `PatchSemanticsTests.cs:277`; Formular: `planSubject.test.tsx` (U2 B-170-Sperre,
    U3 Pflichtfeld) → rot.
  - AK 3 Migration (Mehrheit, Gleichstand, gesetztes Fach bleibt, abhängige Zeilen überleben den Tabellen-Neubau) →
    `StudyPlanSubjectMigrationTests.cs:26`; A7 (Gleichstand rät), A8 (überschreibt gesetztes Fach) → rot.
  - AK 4 → `:194` (A1: Abschaltung ohne Fach-Bedingung → rot), `:208` (A2: keine Abschaltung beim Anlegen → rot),
    `:225` (A6: keine Abschaltung beim PATCH → rot), `:245`, `:262`, `:285`.
  - Review-Fund „Aktivieren ohne Fach" → `planSubject.test.tsx:173`; U1 → rot.
- 2026-11-17 · Reviews Sprint 84 (`frontend-reviewer`, `pugling-reviewer`): gezählt 🟡 Aktivieren ohne Fach, 🟡 zwei
  ungetestete Stellen (B-170-Sperre, Unit-Reset), 🟡 `assign` wählt den ersten spielbaren Plan (→ B-610, im Sprint
  behoben), 🟡 Migrationstest prüft die abhängigen Zeilen nicht. Nicht gezählt: fehlende `zwei-faecher.spec.ts` (ist der
  Rollengang laut Testweg); 🟢 behoben ohne Zählung: Wizard-Feldhilfe, `responsive-vater` per Index, veraltete Kommentare
  und Swagger-Satz; 🟢 hingenommen: ein erneut gesendeter `seriesUnitId` übernimmt ein inzwischen geändertes Reihen-Fach
  (Entscheidung 4), ein gelöschtes Fach lässt einen aktiven Plan ohne Fach zurück (Entscheidung 3).
- 2026-11-17 · Verifikation: `dotnet test -c Release` 1795/1795, Vitest 1160/1160 in drei von vier vollen Läufen (ein
  Lauf 1 failed, nicht zuzuordnen – Muster B-603), `tsc -b` und `dotnet format` sauber.
