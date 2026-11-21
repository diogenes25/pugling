---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Fach der Übung statt des Plans]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-21"
entgangen_bei: [B-606]
---

# B-614 · Die Startseite gruppiert nach dem Fach der Übung, nicht nach dem Fach des Plans

`PositionStatus.SubjectName` kommt aus `SubjectNameOf` (`Series.Subject?.Name ?? Series.SubjectName`, B-606), die
Startseite gruppiert danach (`tagessicht.ts`). Seit B-539 trägt jeder Plan ein Pflichtfach – die Gruppierung ignoriert
es: eine fachfremde Übung landet unter einer anderen Überschrift, ein Freitext „English" neben dem Katalognamen
„Englisch" ergibt zwei Gruppen, Positionen zweier Pläne teilen sich eine. Naheliegend: den Fachnamen des **Plans**
liefern.

## User Story

Als Schüler möchte ich auf der Startseite alle Aufgaben eines Plans unter dem Fach sehen, dem der Plan gehört, damit
meine Tagesliste so gegliedert ist, wie der Vater die Pläne angelegt hat – und nicht je nachdem, wie ein Lehrwerk
sein Fach geschrieben hat.

## Ist-Stand am Code

- **Quelle des Fachnamens:** `SubjectNameOf(pos)` liest das Fach der **Reihe** der Übung – Katalogname, sonst der
  Freitext der Reihe, sonst leer (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:391-392`). Es geht in
  `PositionStatus.SubjectName` (`PositionProgressService.cs:344`; Vertrag `backend/Pugling.Contracts/Shared/ProgressDtos.cs:49`).
- **Der Plan trägt ein Fach:** `StudyPlan.SubjectId`/`Subject` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:83-84`),
  im Vertrag Pflicht, in der Spalte nullable (Altbestand, gelöschtes Fach → `SetNull`, Kommentar davor).
  `StudyPlansController` leitet es beim Anlegen ab und verlangt es (`StudyPlansController.cs:102-104`).
- **Der Plan kommt ohne Fach in die Berechnung:** `ComputeChildDayAsync` lädt die Pläne `AsNoTracking` ohne `Include`
  (`PositionProgressService.cs:409-412`); `ComputeDayAsync` (`:305`) bekommt den `StudyPlan` übergeben und hat dessen
  `Subject` damit nur, wenn der Aufrufer es lädt. Die Positionen laden dagegen Reihe und Reihen-Fach
  (`LoadPositionsAsync`, `:285-286`).
- **Nichts erzwingt, dass die Übung zum Fach des Plans passt:** in `PlanPositionsController` kommt „Subject" nicht vor
  (`grep` ohne Treffer). Nur der Wiederholungs-Pfad prüft es (`WeakWordReviewService.cs:87`,
  `ReviewSubjectMismatch`). Eine fachfremde Übung im Plan ist also erreichbar.
- **Gruppierung im Frontend:** `nachFach` gruppiert nach `pos.subjectName ?? ""` (`frontend/src/sohn/tagessicht.ts:25`),
  einziger Aufrufer `frontend/src/sohn/SohnHome.tsx:84`; die Überschrift ist `f.fach || t("home.noSubject")` (`:134`).
  Andere Leser von `PositionStatus.subjectName` im Sohn-Frontend: nicht gefunden.
- **Tests:** `tagessicht.test.ts` und `tagessichtWeg.test.tsx` setzen `subjectName` von Hand. Im Backend prüft
  `KindTagessichtTests.cs:143` den Namen einer Position (`StartsWith("Englisch-Pos")`, das Fach der **Reihe**) – die
  Ausformulierung hatte das übersehen und „nicht gefunden“ geschrieben; die Zusicherung hält den alten Sinn fest
  (Akzeptanzkriterium 5). `FachnameFolgtDerFachIdTests` prüft Reihe, Profil und Lehrbuch, nicht den Status.

## Die echte Lücke

Die Behauptung der Idee stimmt am Code, mit einer Schärfung: die Gruppe hängt am **Lehrwerk**, nicht am Plan. Der Fehler
tritt auf, wenn (a) die Reihe eines Plans ein anderes Fach hat als der Plan oder (b) zwei Reihen desselben Fachs es
verschieden schreiben (Katalogname gegen Freitext) – oder wenn ein Plan nur Reihen ohne Fach enthält (Überschrift
„ohne Fach", obwohl der Plan ein Fach hat). Fall (a) ist nur möglich, weil die Anlage der Positionen es nicht prüft; wie
oft er vorkommt, ist nicht gemessen. Die Söhne haben je Fach einen Plan, darum fällt es heute kaum auf (P3 bleibt).

## Offene Punkte

1. ~~**Wo wird umgestellt – am Server oder im Frontend?**~~ → Entscheidung 1.
2. ~~**Plan ohne Fach (Altbestand, `SetNull`)?**~~ → Entscheidung 2.
3. ~~**Soll eine fachfremde Übung im Plan sichtbar markiert werden?**~~ → Entscheidung 4 (zurückgestellt, eigene Story).
4. ~~**Der Plantitel im Feld `SubjectName`?**~~ → Entscheidung 1 (nein, das Fach ist `Subject.Name`).
5. ~~**Wie kommt der Fachname des Plans in die Berechnung?**~~ → Entscheidung 3 (neu beim Grillen: die Aufrufer laden den
   Plan verschieden).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Der Server liefert das Fach des Plans: `PositionStatus.SubjectName` bedeutet künftig `StudyPlan.Subject.Name`.** Begründung:
   Die Gruppierung hat genau eine Fundstelle (`frontend/src/sohn/tagessicht.ts:25`, einziger Aufrufer
   `SohnHome.tsx:84`); wer den Namen am Server richtig macht, ändert das Frontend nicht. Die Vertragsform (`string SubjectName`,
   `ProgressDtos.cs:49`) bleibt, nur der XML-Doc-Satz darüber (`ProgressDtos.cs:28-30`, „the subject of its content, taken
   from the exercise's textbook series“) wird nachgezogen. Andere Leser des Felds: im Sohn-Frontend nicht gefunden
   (`grep subjectName` ergibt nur `tagessicht.ts`), im Vater-Frontend nur andere DTOs (`ChildCarePanels.tsx:160`,
   `ChildMaterialSection.tsx:86`), in `Pugling.Client`/`Agent.Creator` keine `PositionStatus`-Leser; im Backend liest nur
   `KindTagessichtTests.cs:143` das Feld (siehe Akzeptanzkriterium 5). Verworfen: nach `planId` gruppieren (bindet die Reiter
   an den Plantitel, den der Vater frei benennt; zwei Pläne desselben Fachs sollen weiter eine Gruppe sein, wie
   `tagessicht.test.ts:17` es festhält) und ein zusätzliches Vertragsfeld `PlanSubjectName` (additiv, aber ein zweites Feld
   für dieselbe Aussage, und das alte bliebe als Falle stehen). Kosten: ein Feld ändert seine Bedeutung ohne Typänderung;
   `docs/api-examples/` und `contract.ts` ziehen über den Doku-Mitschnitt bzw. die Generierung nach, kein Handgriff. Ausgeschlossener
   Fall: ein Client, der das Fach der **Übung** wollte – gegen die User Story („so gegliedert, wie der Vater die Pläne
   angelegt hat“) ist das der Fall, der verschwinden soll.
2. **Ein Plan ohne `SubjectId` behält den heutigen Namen der Reihe als Rückfall** (`Subject.Name ?? SubjectName ?? ""`, die
   bisherige `SubjectNameOf`). Begründung: Die Spalte ist nullable (Altbestand, gelöschtes Fach → `SetNull`,
   `StudyPlanEntities.cs:77-84`); ohne Rückfall fiele die Gruppe eines Altplans von ihrem Namen auf „ohne Fach“
   (`home.noSubject`), eine Verschlechterung für genau die Pläne, die der Vater noch nicht nachgepflegt hat. Verworfen: leerer
   Name (verliert die Überschrift, die es vorher gab). Kosten: der Rückfallzweig bleibt, und damit die Kette
   `Series → Subject` in `LoadPositionsAsync` (`PositionProgressService.cs:284-289`); [B-609](B-609-truhe-rechnet-den-tag-doppelt.md)
   fasst sie in Entscheidung 3 ausdrücklich nicht an, die beiden Stories stören sich also nicht. Ausgeschlossener Fall: ein Altplan ohne Fach mit einem
   Lehrwerk ohne Fach bleibt „ohne Fach“; gegen die User Story trägt das, weil der Vater dort kein Fach angelegt hat, das
   die Liste zeigen könnte.
3. **`ComputeChildDayAsync` lädt die Pläne mit `Subject`; `ComputeDayAsync` holt den Namen selbst nach, wenn `SubjectId`
   gesetzt, die Navigation aber leer ist.** Begründung (am Code geprüft): Die Aufrufer laden den Plan verschieden –
   `ComputeChildDayAsync` ohne `Include` (`PositionProgressService.cs:409-412`), `PlanOverviewController.GetPlan`
   `AsNoTracking` ohne `Include` (`PlanOverviewController.cs:25-26`, Aufruf `:40`), die übrigen Aufrufer über
   `ChildrenDashboardService.cs:77`, `ProgressAsync` (`:875`) und `EvaluateAndAwardAsync` (`:587`). Ein Vertrag „der Aufrufer
   lädt `Subject`“ liefe bei einem vergessenen `Include` still in den Rückfall (Entscheidung 2), also in den heutigen Fehler.
   Verworfen: `Include` bei jedem Aufrufer (fragil, lautlos) und der Name über `PlanPosition.StudyPlan.Subject` in
   `LoadPositionsAsync` (hängt an einer Navigation, die zur übergebenen `plan`-Instanz parallel lebt). Kosten: eine
   zusätzliche Abfrage nach der Primärschlüssel-Zeile des Fachs je `ComputeDayAsync`-Aufruf ohne vorgeladenes `Subject`;
   auf der Startseite (`ComputeChildDayAsync` lädt es mit) fällt sie weg, in den Verlaufsschleifen (`ProgressAsync`,
   Dashboard-Zeitachse) fällt sie je Tag an – ein Befehl von mehr als 60 je Verlaufstag bei 15 Positionen, geschätzt, nicht
   gemessen (Rechnung in B-609, Entscheidung 3). Ausgeschlossener Fall: ein Aufrufer, der `Subject` vorlädt, zahlt nichts. Gegen die User Story gerechnet: jeder Weg, der `SubjectName` an ein Kind
   ausgibt, liefert dasselbe Fach – sonst zeigte die Plan-Übersicht eine andere Überschrift als die Startseite.
4. **Die fachfremde Übung im Plan bleibt hier unberührt; ob die Anlage sie zulassen soll, ist eine eigene Frage
   (zurückgestellt).** Begründung: In `PlanPositionsController` kommt „Subject“ nicht vor; nur der Wiederholungs-Pfad prüft das
   Fach (`WeakWordReviewService.cs:87`, `ReviewSubjectMismatch`). Eine Sperre ändert, was der Vater anlegen darf, und
   berührt Pläne im Bestand; das ist nicht „den Namen richtig herstellen“. Verworfen: die Sperre in dieser Story (ein
   Verhaltenswechsel an einem Schreibweg, den kein Akzeptanzkriterium des Defekts braucht). Kosten: Positionen mit fremdem
   Fach erscheinen künftig unter dem Fach des Plans, ohne dass jemand es merkt. Ausgeschlossener Fall: eine Übung, deren Fach
   das Kind an der Überschrift erkennen wollte – gegen die User Story trägt das, weil die Gliederung ausdrücklich die des
   Vaters sein soll. Zurückgestellt bis zu einer Messung am echten Bestand (wie viele Positionen haben ein anderes Fach
   als ihr Plan); eine Story dafür entsteht erst, wenn die Zahl über null liegt.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. Eine Position, deren Übung in einer Reihe mit anderem Fach liegt als der Plan, meldet in
   `student/children/{id}/today` das Fach des Plans (Reihe „Mathe-x“, Plan „Englisch“ → `Englisch`). Rot, wenn
   `SubjectNameOf` weiter nur die Reihe liest.
2. Zwei Positionen desselben Plans, deren Reihen „Englisch“ (Katalog) bzw. „English“ (Freitext) heißen, melden beide den
   Namen des Plan-Fachs. Rot, wenn der Name aus der Reihe kommt.
3. Ein Plan ohne `SubjectId` meldet den Namen der Reihe wie bisher (Katalogname, sonst Freitext, sonst leer). Rot, wenn
   der Rückfall fehlt (dann ist der Name leer).
4. `student/study-plans/{planId}/overview` – dort wird der Plan ohne `Include` geladen (`PlanOverviewController.cs:25-26`) –
   liefert dasselbe Plan-Fach wie die Startseite. Rot, wenn `ComputeDayAsync` den Namen nur aus `plan.Subject` liest und
   bei leerer Navigation auf die Reihe zurückfällt.
5. Die bestehende Zusicherung `Assert.StartsWith("Englisch-Pos", …)` in `KindTagessichtTests.cs:143` wird auf das Fach des
   Testplans umgeschrieben (`TestApi.SeedLeitnerPosition` setzt `SubjectId` auf „Englisch“, `TestApi.cs:318-324`).
   Rot, sobald die Umstellung steht und die Zusicherung unverändert bleibt (Plan-Fach „Englisch“ ≠ Reihen-Fach
   „Englisch-Pos…“): sie hält den alten Sinn („Fach der Reihe“) fest. Umgeschrieben ist sie rot, wenn der Name wieder aus der
   Reihe kommt.
6. Das Frontend bleibt unverändert: `tagessicht.test.ts:17` (zwei Pläne eines Fachs, eine Gruppe) bleibt grün. Rot, wenn die
   Gruppierung auf `planId` umgestellt wird.
7. Der XML-Doc-Satz zu `SubjectName` in `ProgressDtos.cs` nennt das Fach des Plans, nicht der Übung (Durchsicht bei der
   Abnahme, kein Tor: ein Kommentar ist nicht mechanisch prüfbar).

## Schätzung

**Größe: M** — der Code ist klein (`SubjectNameOf` wird planbewusst, zwei Ladestellen), aber die Änderung wirkt über fünf
Aufrufer von `ComputeDayAsync` und trägt eine ungemessene Abfrage-Last in den Verlaufsschleifen; dazu sieben
Kriterien mit eigenen Fixtures. Anker: wie der Batch-Pfad im `MediaSelector` (M), nicht wie B-01 (S).

- **`migration: nein`** — `StudyPlan.SubjectId` und `Subject` gibt es (`StudyPlanEntities.cs:83-84`); nichts am Schema.
- **`vertragsbruch: nein`** — `PositionStatus.SubjectName` bleibt `string` (`ProgressDtos.cs:49`); die Form ändert sich
  nicht, nur der XML-Doc-Satz (`ProgressDtos.cs:28-30`) und der Wert. Das ist eine Bedeutungsänderung ohne Typbruch –
  `Pugling.Client`/`Agent.Creator` lesen das Feld nicht (Entscheidung 1), die Beispiele und `docs/openapi/v1.json`
  ziehen über `DocsCaptureTests` bzw. `ContractDocumentTests` nach.
- **`wo: backend`** — das Frontend bleibt unverändert (AK 6); `tagessicht.ts:25` gruppiert weiter nach `subjectName`.
  Die Quelle trug `bereich/frontend`, weil der Defekt dort sichtbar wird; die Arbeit liegt im Server.

### Risiken

- **Abfrage-Last:** Entscheidung 3 nennt einen Befehl je `ComputeDayAsync`-Aufruf ohne vorgeladenes `Subject`; in den
  Verlaufsschleifen (`PositionProgressService.cs:875`, `ChildrenDashboardService.cs:77`) fällt er je Tag an. Gemessen ist
  das nicht; der Bau misst es (B-609 hat eine Rechnung dazu, Entscheidung 3 dort).
- **Fünf Aufrufer mit verschiedenem Ladeverhalten:** `PositionProgressService.cs:416` (lädt mit), `PlanOverviewController.cs:40`
  (ohne `Include`, AK 4), `ChildrenDashboardService.cs:77`, `PositionProgressService.cs:587` und `:875`. Ein vergessener
  Pfad fällt still auf den Reihen-Namen zurück – genau der Fehler; AK 4 deckt nur die Plan-Übersicht ab.
- **Doku-Capture byte-stabil:** `docs/api-examples` ändert sich vermutlich an Stellen mit `subjectName`; `DocsCaptureTests`
  schreibt sie neu, der Diff ist zu lesen, nicht blind zu committen.
- **Fachfremde Positionen** bleiben ungeprüft (Entscheidung 4, zurückgestellt); kein Teil dieser Größe.

### Angriffsplan

1. `PositionProgressService`: `SubjectNameOf` bekommt den Plan; Name = `plan.Subject?.Name`, bei leerer Navigation und
   gesetzter `SubjectId` nachladen, sonst Rückfall auf die Reihe (Entscheidungen 2 und 3).
2. `ComputeChildDayAsync` (`:409-412`) lädt die Pläne mit `Include(Subject)`.
3. Tests (siehe Testweg), je Kriterium vorher rot gemessen; `KindTagessichtTests.cs:143` umschreiben.
4. `ProgressDtos.cs:28-30` Doc-Satz nachziehen; Capture- und Vertragsdokument neu erzeugen lassen.
5. `dotnet build Pugling.sln` und `dotnet format Pugling.sln`.

### Testweg

- `KindTagessichtTests` (`backend/Pugling.Api.Tests/KindTagessichtTests.cs`): AK 1, 2, 3, 5 über `student/children/{id}/today`
  mit Reihe eines anderen Fachs bzw. zwei Schreibweisen; Aufbau über `TestApi.SeedLeitnerPosition` (Plan-Fach „Englisch“
  über `DefaultSubjectIdAsync`, `TestApi.cs:318-324`). Rot heute: AK 1, 2 und das umgeschriebene AK 5; AK 3 (Rückfall)
  ist im Ausgangszustand grün und trägt nur gegen eine Fassung ohne Rückfall.
- AK 4: `student/study-plans/{planId}/overview` – Testklasse für `PlanOverviewController` nicht benannt; im Bau per Grep
  unter `backend/Pugling.Api.Tests` zu finden, sonst in `KindTagessichtTests`.
- AK 6: `frontend/src/sohn/tagessicht.test.ts:17` bleibt grün (keine Frontend-Änderung); `tagessichtWeg.test.tsx` ebenso.
- Vor der Abnahme: `pugling-reviewer`; Rollengang als Sohn an der Startseite eines Kindes mit zwei Fächern.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟢, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): am Code bestätigt; Lücke geschärft (Gruppe hängt am
  Lehrwerk; Plan wird ohne `Subject` geladen), fachfremde Positionen sind nicht ausgeschlossen. `unverifiziert` entfällt.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 4. Server liefert das Fach des Plans (Bedeutung von `SubjectName` ändert sich, Vertragsform bleibt), Rückfall auf die Reihe bei Plan ohne Fach, Plan-Fach wird in `ComputeDayAsync` nachgeladen, wo die Aufrufer es nicht mitbringen (`PlanOverviewController` lädt ohne `Include`). Gegen den Code korrigiert: `KindTagessichtTests.cs:143` liest `subjectName` und hält den alten Sinn fest. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, backend (Frontend bleibt), keine Migration, kein Vertragsbruch
  (Feldform unverändert, nur Bedeutung und Doc-Satz); Aufrufer von `ComputeDayAsync` am Code gezählt (fünf), Last der
  Nachladeabfrage ungemessen.
- **2026-11-21** — in-arbeit (Nachtlauf, Sprint 107). Rote Probe vor dem Fix: `PlanFachTagessichtTests` 2 von 3 rot – AK 1 erwartet `Englisch`, gemessen `Mathe-x 16`; AK 4 erwartet `Englisch`, gemessen `Mathe-x 24`; AK 3 grün wie angekündigt (Rückfall = Ausgangszustand).
- **2026-11-21** — Fälle → Tests: AK 1 (Reihe mit fremdem Katalogfach) → `PlanFachTagessichtTests.Startseite_Meldet_Das_Fach_Des_Plans`, Mutante Q1 → rot; AK 2 (nur Freitext „English“) → derselbe Fall, Zeile `FreeTextPos`, Q1 → rot; AK 3 (Plan ohne Fach: Katalogname / Freitext / leer) → `Plan_Ohne_Fach_Faellt_Auf_Die_Reihe_Zurueck`, Q2 (Rückfall leer) und Q4 (Freitext fehlt) → rot; AK 4 (Plan-Übersicht) → `Planuebersicht_Meldet_Dasselbe_Fach_Wie_Die_Startseite`, Q1 → rot; Entscheidung 3 (Aufrufer ohne geladenes Fach) → `Plan_Ohne_Geladenes_Fach_Meldet_Trotzdem_Das_Plan_Fach` (direkter Service-Aufruf), Q3 → rot; AK 5 → `KindTagessichtTests.cs:143-144` auf `Equal("Englisch")` umgeschrieben, Q1 → rot; AK 6 → Vitest `src/sohn/tagessicht*` 12/12 grün, kein Frontend-Diff; AK 7 → Doc-Satz `ProgressDtos.cs:28-31` gelesen (nennt Plan-Fach, Rückfall, "" = keins).
- **2026-11-21** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Debug, Filter `PlanFachTagessichtTests|KindTagessichtTests`):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Q1 Name nur aus der Reihe | `PositionProgressService.cs:345` | red | red |
  | Q2 Rückfall leer | `PositionProgressService.cs:345` | red | red |
  | Q3 Nachladen aus | `PositionProgressService.cs:396` | red | red |
  | Q4 Freitext der Reihe fehlt | `PositionProgressService.cs:403` | red | red |
  | Q5 `Include` in `ComputeChildDayAsync` weg | `PositionProgressService.cs:420` | green | green |
  | Q6 `Include` in der Plan-Übersicht weg | `PlanOverviewController.cs:27` | green | green |

  Q5/Q6 grün ist gewollt: die `Include`s sparen nur die Nachlade-Abfrage, das Ergebnis trägt die Senke. E2E-Probe E1 (Q1 gegen `startseite-plan-fach.spec.ts`, Ausweichports) → rot.
- **2026-11-21** — Review (`pugling-reviewer`): kein 🔴/🟡. Geprüft: `PositionStatus` entsteht nur in `ComputeDayAsync`; alle fünf Aufrufer gelesen, die Verlaufsschleifen lesen den Namen nicht. Zwei 🟢 übernommen (ohne Zählung): `Include(Subject)` in `PlanOverviewController.GetPlan` und der Dashboard-Zeitachse (spart die Abfrage je Verlaufstag); „father“ → „supervisor“ im Swagger-Satz. Weil die Plan-Übersicht danach nicht mehr nachlädt, trägt AK 4 das Nachladen nicht mehr – dafür der direkte Service-Fall (Q3). Volle Suite (Release) 1994/1994. **Zähler 0 von 5.**
- **2026-11-21** — abgenommen (Sprint 107). Rollengang Sohn als E2E (`frontend/e2e/startseite-plan-fach.spec.ts`, Server frisch von Playwright auf 5201/5174 nach der letzten Änderung): Englisch-Plan mit einer Übung aus einer Mathe-Reihe zeigt genau eine Fach-Karte „Englisch …“ mit beiden Aufgaben, keine Mathe-Karte. Vater und Creator: Regressionszeugen (`full-flow`, `belohnung-ueber-faecher` grün; keine Vater-/Creator-Fläche im Diff außer der Ladezeile der Dashboard-Zeitachse). Entscheidung 4 bleibt zurückgestellt (fachfremde Positionen ungeprüft). Commit `86d74423`.
- **2026-11-21** — nachgeschaut (Nachtlauf, Retro Sprint 108), kein Befund. Geprüft: (1) `subjectName` hat im Sohn-Frontend genau einen Leser, `frontend/src/sohn/tagessicht.ts:25` (Grep über `frontend/src/sohn` ohne Tests); (2) `new PositionStatus(` steht nur in `PositionProgressService.cs:336`, der Name dort ist `planSubject ?? SeriesSubjectNameOf(pos)` – kein zweiter Weg liefert das Fach der Reihe an ein Kind.
