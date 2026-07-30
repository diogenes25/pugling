---
tags: [typ/plan, bereich/doku, bereich/api]
aliases: [Codequalität-Gates, Test-Gate, CI-Plan, Integrationstests bewerten]
---

# Codequalität: von Disziplin zu mechanischen Toren

Status: **Etappen A–D4 umgesetzt und committet (2026-07-29).** Alle Zahlen im Abschnitt „Ist-Zustand" sind
am 2026-07-29 am damaligen Arbeitsstand gemessen (Etappe 2 der `Father`→`Adult`-Umbenennung im Baum, noch
nicht committet – sie ist seither als `1ee1538` committet); der Stand **nach C** steht unter „Was Etappe C
gebracht hat". Was A gebracht hat, steht unter „Etappe A"; der **erste Fund des neuen Tors** unter „Was das
Tor sofort gefunden hat".

## Einstieg für eine frische Sitzung

Diese Seite ist die vollständige Übergabe – **nicht neu messen, nicht neu erheben.** Der Ausgangsbefund steht
unter „Ist-Zustand" (Momentaufnahme 2026-07-29), der erreichte Stand unter „Was Etappe C gebracht hat", die
Arbeit unter „Der Plan".

> **CI wurde jetzt angesehen (2026-07-29, `gh` ist inzwischen installiert und authentifiziert).** Alle fünf
> CI-Läufe seit D0 waren rot – **aber nicht wegen der Tests.** Jeder Lauf scheiterte exakt am selben Schritt
> „Zweigabdeckung ausgeben" mit `grep: write error: Broken pipe` (`grep … | head -1 | grep …` unter
> `set -e -o pipefail`: `head` schließt die Pipe nach der ersten Zeile, der schreibende `grep` bekommt
> SIGPIPE). Der `Test`-Schritt selbst war in jedem Lauf grün (zuletzt 587/587) – das aus „Was das Tor sofort
> gefunden hat" bekannte `CreatorAgentTests`-Isolationsproblem ist also längst gefixt. Weil der Coverage-
> Schritt aber crashte, lief `Format prüfen` nie, und weil CI insgesamt „failure" meldete, blieb
> `Deploy to Azure App Service` durchgehend `skipped` (`workflow_run` mit `if: conclusion == 'success'`) –
> das Deploy war also weiterhin blockiert, nur jetzt aus einem anderen Grund als dem `npm ci`-Problem, das D1
> gefunden hat. **Gefixt**: `grep -m1` statt `| head -1` (kein Broken Pipe mehr, lokal gegen eine
> Beispiel-Cobertura-Datei unter `pipefail` verifiziert).
>
> **E2E hatte zum Zeitpunkt der Prüfung null Läufe** (`gh api .../workflows/e2e.yml/runs` → `total_count: 0`).
> Der Workflow triggert nur auf `pull_request`, nightly (03:00 UTC) und Handbetrieb; bisher ging alles per
> direktem Push auf `main`. Die „25/25 grün"-Aussage aus der vorherigen Übergabe ist ein **lokaler** Beleg
> (`CI=1 npx playwright test`), kein CI-Beleg – der fehlt weiterhin, bis ein PR oder die erste Nightly läuft.
>
> **D4 ist jetzt ein echtes CI-Tor** (`git diff --exit-code -- docs/api-examples` direkt nach `Test` in
> `ci.yml`) – und der erste echte Lauf schlug sofort an, **kein Umgebungsunterschied wie vermutet, sondern ein
> echter Bug**: `Truncate()` (feste 1500-Zeichen-Grenze für lange Responses in der Doku) schneidet am rohen,
> von `JsonSerializer(WriteIndented: true)` erzeugten String, und dessen Zeilenumbruch hing ohne explizite
> Angabe an `Environment.NewLine` – `\r\n` unter Windows, `\n` unter Linux. Bei identischem Inhalt zählt
> Windows also mehr Bytes pro Zeile, der Schnitt landet auf beiden Plattformen an unterschiedlicher Stelle
> mitten im JSON (betraf nur `shop.md`/`remarks.md`, die einzigen zwei Antworten, die über die Grenze kommen –
> git normalisiert Zeilenenden beim Commit ohnehin, sonst wäre der Effekt überall sichtbar gewesen). Gefixt:
> `Indented`-Optionen bekommen `NewLine = "\n"` explizit; der neu erzeugte Diff war byteidentisch mit dem, den
> CI gemeldet hatte. Zweiter CI-Lauf danach **komplett grün** (alle drei Jobs).
>
> **D3 ist umgesetzt** (siehe „Was D3 vorfindet" unten für die Vorher-Zahlen und die getroffenen
> Entscheidungen) – `markdownlint-cli2` läuft jetzt als eigener Job in `ci.yml`, 0 Treffer.
>
> **Nach dem Pipefail-Fix lief das Deploy zum ersten Mal seit dem 2026-07-05er `vite`-Bruch überhaupt an**
> (`workflow_run` griff endlich, weil CI grün war) – scheiterte dann aber an `azure/webapps-deploy@v3` mit
> „No credentials found". `gh secret list` zeigt **kein einziges** Repo-Secret, `AZURE_WEBAPP_PUBLISH_PROFILE`
> fehlt also komplett. **Kein neuer Befund – Azure ist schlicht noch nicht konfiguriert.** Frontend-Build und
> `dotnet publish` liefen davor beide grün durch; sobald das Secret gesetzt ist, sollte der Rest laufen.

**Arbeitsstand nach Etappe C (2026-07-29): A, B und C sind committet – D0 ist erledigt.**

- `main` trägt drei Commits über `1ee1538` („Vertrag heißt `adult`"). Der Endstand ist **grün**: 587 Tests,
  `dotnet build Pugling.sln` 0 Warnungen, `dotnet format --verify-no-changes` sauber.
- Die Tore stehen und greifen: CI-Workflow, Stop-Hook, `Directory.Build.props`, `.editorconfig`,
  `UnmappedMemberHandling.Disallow` und **sieben** reflexive Wächter (vier aus B4, dazu Ownership-Matrix,
  PATCH-Semantik und Endpunkt-Abdeckung aus C). Die `workflow_run`-Kopplung des Deploys stand hier
  ebenfalls, ist aber seit **2026-07-30 stillgelegt** – siehe A2.

| Commit | Inhalt | Beim Schnitt gemessen |
|---|---|---|
| `05f1a67` | **Die Tore (A+B)** – `ci.yml`, `deploy-azure.yml`, `test-gate.sh`, `Directory.Build.props`, `.editorconfig`, `UnmappedMemberHandling` + `unknown_field`, `UnknownFieldTests`, dazu die Folgeänderungen (66 unbenutzte `using`, 5 CA-Fundstellen, die still verworfenen Payload-Felder der Testsuite, `TimetableController` an den Ownership-Filter) | **grün**: 494 Tests, 0 Warnungen |
| `70a16f4` | **Die sieben Wächter (B4 + C1/C2/C4/C5)** – `ApiSurface`, `ConventionGuardTests`, `SampleJson`, `OwnershipMatrixTests`, `PatchSemanticsTests`, `EndpointCoverage`(+`Guard`), die Coverage-Schritte in `ci.yml`, der Bericht-Zweig im Stop-Hook | **absichtlich rot**: 550 grün, dazu die eine Meldung des Abdeckungs-Wächters mit 49 offenen Actions – er ist ja das Werkzeug, mit dem der nächste Commit sie schließt |
| `74f2606` | **Die Defektfixes samt Tests (C3)** – `AdultsController` (Kaskade + `duplicate_email`), `CreatorProfilesController` (`duplicate_profile_name`), `TagsController` (EF-`Include`-Fehler), die beiden neuen `ApiErrors`, `AdultLifecycleTests`, `CatalogReadDeleteTests`, `ExerciseCheckEndpointTests`, `MediaLinkTeardownTests` + Ergänzungen in bestehenden Testklassen | **grün**: 587 Tests, 0 Warnungen |

Der mittlere Commit ist damit der einzige Punkt, an dem `git bisect` über diese drei Commits stolpert – und
zwar sichtbar am Abdeckungs-Wächter, nicht an einem Fachtest. Zwei Dinge liefen beim Schnitt anders als in
der Empfehlung, die hier vorher stand; beide waren erzwungen, nicht Geschmack:

- **`ConventionGuardTests` liegt beim Wächter-Commit, nicht bei den Toren.** Es teilt die Routen-Auflösung
  mit `ApiSurface`, und dessen `<see cref="EndpointCoverage"/>` wäre ohne `EndpointCoverage.cs` ein CS1574 –
  unter der neuen Ratsche also ein Build-Fehler. Alle sieben Wächter in einem Commit ist ohnehin die
  ehrlichere Einheit.
- **Der Reihenfolge-Fix in `CreatorAgentTests` gehört zu den Toren, nicht zu C3.** Erst die Payload-Fixes
  legen an, was der Server vorher still verwarf; das verschiebt die Ids im klassenweit geteilten
  Vokabelspeicher und legt die schlummernde Ordnungsabhängigkeit der Dedupe-Prüfung offen. Mit dem Fix in
  C3 war der Tore-Commit rot – gemessen, nicht vermutet.

**D1 ist erledigt** (Frontend-Job in `ci.yml`) und hat beim Messen gefunden, dass das Azure-Deploy seit
2026-07-05 am `npm ci` scheiterte – siehe „Was D1 sofort gefunden hat". **D2 ist erledigt**
([e2e.yml](../.github/workflows/e2e.yml)) **und die Suite ist belegt**: `CI=1 npx playwright test` liefert
**25 von 25 grün in 1,5 min** (2026-07-29). `CI=1` ist der Punkt – damit greift `reuseExistingServer: false`
auch für Vite, es lief also genau der Pfad, den der Runner nimmt: beide Server frisch, Wegwerf-DB, kein
Rückgriff auf einen stehenden Entwicklungs-Server. Der Vorbau-Schritt ist mitgeprüft (`dotnet build
backend/Pugling.Api` in Debug, 2,5 s warm).

**D3 und D4 sind seither ebenfalls erledigt** (2026-07-29, siehe „Einstieg für eine frische Sitzung" oben) –
**A bis D4 sind damit vollständig**, CI ist grün (alle drei Jobs: Backend inkl. D4-Diff-Check, Frontend,
Markdown-Lint). Offen bleiben, unabhängig voneinander:

- **Nacharbeit aus B**: CS1591 ist erledigt (2026-07-29, 123 statt der geschätzten 117 Lücken) und die
  **188 `CancellationToken`-Altlasten sind es seit 2026-07-30** – der Wächter ist damit hart, siehe
  „Nacharbeit aus B" unten. Offen bleibt nur, das Frontend gegen `unknown_field` durchzuspielen.
- **Der Peer-Konflikt** `vite-plugin-pwa` ↔ `vite@8`, den D1 nur benannt, nicht behoben hat.
- **E2E hat immer noch keinen echten CI-Lauf** (Stand 2026-07-29) – der erste kommt über einen PR oder die
  nächste Nightly (03:00 UTC); erst dann ist D2 nicht nur lokal, sondern auch in CI belegt.
- **Azure-Secret** (`AZURE_WEBAPP_PUBLISH_PROFILE`) fehlt weiterhin – laut Rückmeldung bewusst, noch nicht
  konfiguriert, keine Aufgabe für eine Code-Sitzung.
  **Nachtrag 2026-07-30: A2 ist stillgelegt und das Thema damit vom Tisch, bis Azure existiert.** Nachdem CI
  erstmals dauerhaft grün lief, lief das Deploy zum ersten Mal wirklich an – und scheiterte ab da nach
  *jedem* grünen Lauf an „No credentials found" (`gh secret list` ist leer: es ist überhaupt kein Secret
  gesetzt, nicht nur dieses). Ein Tor, das immer rot ist, erzieht zum Wegsehen und entwertet das echte Rot
  daneben; es widerspricht damit dem Zweck dieses ganzen Plans. Entscheidung des Eigentümers: das
  Deployment wird später **komplett neu** gebaut. Umgesetzt als **Stilllegung, nicht Löschung** – der
  `workflow_run`-Block in [deploy-azure.yml](../.github/workflows/deploy-azure.yml) ist auskommentiert,
  `workflow_dispatch` und die `if:`-Bedingung bleiben. Grund: die beiden Fallstricke, die zusammen 24 Tage
  unbemerkten Deploy-Ausfall gekostet haben (`npm ci --legacy-peer-deps` und der
  `workflow_run.head_sha`-Checkout) – eine Neufassung soll dort anfangen, nicht bei null. Sie stehen samt
  Zielbild und Reaktivierungs-Checkliste in **[deployment-azure.md](deployment-azure.md)**; wieder scharf =
  Secret setzen + einen Block einkommentieren.

**Wo die Wächter ihre Befunde ablegen** – das muss man wissen, bevor man ein rotes Tor deutet:
`ConventionGuardTests` und `PatchSemanticsTests` melden wie gewohnt als Test. Der Endpunkt-Abdeckungs-Wächter
**nicht**: er urteilt im Aufräumen des Assembly-Fixtures, und die Konsole verschluckt seine Meldung
(„Passed!" trotz Exit-Code 1). Sein Befund steht in `TestResults/endpoint-coverage.txt`; Stop-Hook und CI
geben die Datei aus. Begründung unter „Warum kein `[Fact]`".

## Die Frage, kurz beantwortet

**Würden Integrationstests die Codequalität verbessern? Sie tun es bereits – und darum ist „mehr davon"
nicht die erste Aufgabe.**

Der Beleg steht im eigenen Repo. [docs/code-review.md](code-review.md) schloss die Achse Korrektheit noch
mit „*Keine automatisierten Tests (Verifikation manuell/API) – nächster Schritt: Integrationstests*". Genau
die dort gemeldeten Fehlerklassen sind heute je festgenagelt: die IDOR-Lücke in `AdultsController` durch
`OwnershipTests`, die Selbst-Einstufung des Kindes und der Heartbeat-Zeitcheat durch `AntiCheatTests`,
das PIN-Hashing durch `SecurityHardeningTests`. Es sind heute **467 Tests** in **14.082 Zeilen**, sie laufen
in-process gegen eine echte, frische SQLite je Testklasse ([PuglingWebAppFactory](../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs)),
und sie sind **grün**.

Die Testbasis ist also nicht das Problem. Das Problem ist der **Zeitpunkt**: zwischen „Code entsteht" und
„Code liegt auf `main` und ist deployt" gibt es kein einziges Tor, das diese 467 Tests aufmacht.
[deploy-azure.yml](../.github/workflows/deploy-azure.yml) baut Frontend und API auf jedem Push nach `main`
und deployt – ohne `dotnet test`. Der einzige automatische Prüfschritt im Arbeitsfluss ist der
PostToolUse-Hook, und der macht `dotnet format whitespace` + `dotnet build` der **einen** geänderten Datei.

Für „saubere und fehlerfreie Codegenerierung" heißt das: die Rückkopplung, die generierten Code korrigiert,
ist **Kompilierbarkeit**. Das fängt Syntax und Typen. Es fängt nicht Ownership, nicht Vertragsdrift,
nicht PATCH-Semantik – und genau das sind die Fehlerklassen dieses Projekts.

## Ist-Zustand (gemessen)

| Achse | Stand | Bewertung |
|---|---|---|
| Integrationstests | 467 Facts / 14.082 Zeilen / 71 Dateien, alle grün | **stark** |
| Testarchitektur | in-process `WebApplicationFactory`, Wegwerf-DB + Wegwerf-Medienordner je Klasse, Wanduhr neutralisiert | **stark** |
| Konventions-Guards | `TagConventionTests` (reflexiv, mit Selbstschutz gegen falsch-grün), `ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry`, `QueryPlanSmokeTests` (EXPLAIN QUERY PLAN), `DocsCaptureTests`, `OpenApiExampleTests` | **stark, aber punktuell** |
| Zeilenabdeckung `Pugling.Api` | 97,9 % (110.445/112.811) | irreführend hoch, siehe unten |
| Zweigabdeckung `Pugling.Api` | **65,7 %** | die aussagekräftige Zahl |
| Endpunkt-Abdeckung | **57 von 295** async Controller-Actions von **keinem** Test berührt (19 %) | **die konkrete Lücke** |
| Compiler-Strenge | `Nullable`+`GenerateDocumentationFile` an, Build **0 Warnungen** – aber **kein** `TreatWarningsAsErrors`; CS1591 war zudem per `NoWarn` **abgeschaltet** (in der Messung übersehen, siehe Etappe B) | Ratsche fehlt |
| `.editorconfig` / `Directory.Build.props` | **existieren nicht** | `dotnet format` erzwingt nur Defaults |
| Fehler-Konvention | 252× `ProblemWithCode`, **0×** rohes `Problem(`, **0×** `BadRequest(` | 100 % gehalten – **von nichts erzwungen** |
| CI | 2 Workflows: Deploy + Wiki-Sync. **Kein Test-Workflow.** | **kritische Lücke** |
| Frontend | 2 Vitest-Dateien, 10 Playwright-Specs – ebenfalls nicht in CI | Lücke |
| Laufzeit ganze Suite | **63 s** (`dotnet test Pugling.sln`, warm) | macht jedes Tor billig |
| Laufzeit `dotnet format --verify-no-changes` | **139 s**, Ergebnis **sauber** | langsamer als die Tests |

Die letzten zwei Zeilen entscheiden über den Zuschnitt des Plans. **Die vollständige Testsuite kostet
eine Minute.** Es gibt damit kein Laufzeit-Argument gegen ein Tor – weder in CI noch im Arbeitsfluss des
Agenten. Der teuerste Prüfschritt ist die Formatprüfung, und die ist heute sauber.

Zur Zeilenabdeckung: 97,9 % klingt nach „durchgetestet", ist aber ein Artefakt. Coverlet zählt die
async-State-Machines mit, und die vielen kleinen `Get`/`List`-Rümpfe verwässern das Bild. Die Wahrheit
steckt in den 65,7 % Zweigen – und noch schärfer in der Liste der nie berührten Actions.

### Die 57 nie aufgerufenen Actions (historische Momentaufnahme)

> **Erledigt und bewusst nicht gepflegt.** Die Liste ist der Befund vom 2026-07-29 und war beim Beginn von
> Etappe C bereits falsch (es waren 63, siehe „Was Etappe C gebracht hat"). Sie bleibt hier, weil das
> **Muster** die Aussage ist, nicht die Namen. Den aktuellen Stand erzeugt der Abdeckungs-Wächter bei jedem
> Lauf nach `TestResults/endpoint-coverage.txt`; heute ist er leer.

Aus der Cobertura-Auswertung (State-Machines mit 0 Treffern):

| Controller | nie berührte Actions |
|---|---|
| `AchievementsController` | Delete, List, Update |
| `AdultsController` | Delete, Get, Update |
| `ArithmeticDrillController` | Check, Generate |
| `BirkenbihlController` | DeleteSentence |
| `ChaptersController` | Get, Update |
| `ChildInterestsController` | Remove, SetWeight |
| `ChildVocabularyProgressController` | Get |
| `ChildrenController` | RemoveSupervisor, Supervisors |
| `ClozeTextsController` | Delete, Get, GetByKey, List |
| `ExerciseCategoriesController` | Delete, Get, List, Update |
| `ExerciseMediaController` | ListForExercise, UnlinkExercise, UnlinkItem |
| `InterestTagsController` | Delete, Update |
| `KlassenarbeitenController` | Delete, LinkTag, UnassignExercise, UnlinkTag |
| `MediaAssetsController` | AttachAsync, AttachTags, GetByKey, Update |
| `MediaVariantsController` | Update |
| `MyObjectivesController` | Get |
| `ObjectivesController` | Delete, List, Update |
| `PlanPositionsController` | Get |
| `PositionPracticeController` | Get |
| `PositionTestsController` | Get |
| `SeriesUnitsController` | Delete |
| `SubjectsController` | Update |
| `TagsController` | Delete, GetExercises, UntagExercise, Update |
| `TextbookSeriesController` | List |
| `TimetableController` | Delete |
| `VocabularyTagsController` | AttachTags, Create, DetachTag, Update |

**Das Muster ist die Aussage, nicht die Zahl.** Es ist fast immer `Update` und `Delete` – der CRUD-Schwanz.
Getestet ist, was ein Durchstich anfasst (anlegen → spielen → auswerten); ungetestet ist das Ändern und
Löschen. Und gerade dort sitzen die zwei Regeln, die dieses Projekt teuer bezahlt, wenn sie brechen:
die **PATCH-Semantik** (`null` = unverändert, Leeren nur über einen `Clear…`-Schalter) und die
**Eigentumsprüfung**. `PatchClearFieldTests` deckt fünf Fälle exemplarisch – `AdultsController.Update`,
`SubjectsController.Update`, `TagsController.Update`, `ObjectivesController.Update` und die übrigen
prüft niemand.

Ein zweites Muster steckt darin, kleiner aber lehrreich: `ArithmeticDrillController.Generate`/`Check`
sind unberührt, obwohl es `ArithmeticProblemGeneratorTests` gibt. Der **Algorithmus** ist geprüft, der
**Endpunkt**, der ihn nach außen gibt, nie aufgerufen. Genau diese Naht – korrekte Logik, unbelegte
Ausspielung – ist die, an der generierter Code typischerweise reißt.

## Die Lücken, einzeln

### L1 · Deploy ohne Test (kritisch)

`push` auf `main` → Frontend-Build → `dotnet publish` → Azure. Kein `dotnet test`, kein `tsc`-Gate außer
dem in `npm run build`. Ein Commit, der 200 Tests bricht, geht in Produktion. Dass das bisher nicht
passiert ist, liegt an manueller Disziplin.

### L2 · Kein Test-Tor im Generierungs-Loop

Der Hook baut das besitzende Projekt. Damit ist die engste Rückkopplung für generierten Code
„kompiliert" – eine Stufe zu schwach für ein Projekt, dessen Regeln Laufzeitregeln sind
(Ownership, Rollen, Idempotenz, Wallet-Serialisierung). CLAUDE.md verlangt darum ausdrücklich
„*Änderungen mit echtem Laufzeit-Effekt per `/smoke-test` oder gezieltem `curl` prüfen*" – als
Anweisung an den Menschen bzw. Agenten, nicht als Tor.

Das Gegenargument „Tests im Loop sind zu langsam" trägt hier nicht: **63 Sekunden** für alle 467.

### L3 · Der Server nimmt unbekannte Felder stillschweigend an

`Program.cs` konfiguriert an den JSON-Optionen nur einen Converter; `UnmappedMemberHandling` bleibt auf
dem Default `Skip`. Ein Client, der ein vertipptes oder veraltetes Feld schickt, bekommt **201 Created**
und glaubt, es sei angekommen.

**Der Beweis liegt im eigenen Testhelfer.** [TestApi.CreateVocabPlanAsync](../backend/Pugling.Api.Tests/TestApi.cs)
postet bis heute `method`, `contentKeys` und `dailyTestRequired`:

```csharp
var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
{
    childId, title = "Test-Plan",
    method = "Vocabulary",                                          // existiert nicht mehr
    durationDays = 5,
    contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },    // existiert nicht mehr
    dailyTestRequired,                                              // existiert nicht mehr
});
```

`CreatePlanDto` ist `(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
string? Description)`. Die drei Felder gehören zum plan-weiten `StudyPlanItem`/`Method`-Modell, das beim
Lehrplan-Umbau **vollständig entfernt** wurde. Der Server verwirft sie schweigend, der Aufrufer erfährt
nichts, der Test in `OwnershipTests` bleibt grün, weil er nur auf 403/404 prüft.

Für ein API-First-Produkt, dessen Konsumenten generierte Clients und KI-Agenten sind, ist das die
teuerste einzelne Einstellung im Projekt: sie verwandelt „falsches Feld" von einem sofortigen 400 in
einen stillen Datenverlust.

### L4 · Konventionen gelten per Disziplin, nicht per Tor

252 `ProblemWithCode` und **null** Ausnahmen; 154 `AsNoTracking`; 0 Build-Warnungen. Die Konventionen aus
CLAUDE.md werden praktisch lückenlos befolgt. Nur: **nichts hält das fest.** Ein generierter Controller mit
`return BadRequest("...")` kompiliert, läuft, liefert ein `ProblemDetails` ohne `code` – und kein Test
bemerkt es. Der Zustand ist ideal für eine Ratsche: weil heute nichts zu bereinigen ist, kostet das
Einziehen der Sperre **nichts** und verhindert ab sofort jede Drift.

### L5 · Keine Warnungssperre

`GenerateDocumentationFile` ist an, also meldet der Compiler fehlende `/// <summary>` auf öffentlichen
Membern als CS1591 – die mechanische Fassung von CLAUDE.mds Doku-Regel. Als Warnung. Bei 0 Warnungen
Ist-Stand ist `TreatWarningsAsErrors` ein Nulltarif-Gewinn.

### L6 · Kein `.editorconfig`

`dotnet format` läuft im Hook und laut CLAUDE.md vor dem Commit – aber ohne `.editorconfig` erzwingt es
nur die .NET-Defaults. Jede Projektkonvention (Guard Clauses zuerst, `record`-DTOs, `var`-Gebrauch,
Namensregeln) steht ausschließlich in Prosa und muss von Menschen bzw. dem Modell gelesen werden.
Ein `.editorconfig` verlagert den erzwingbaren Teil in den Compiler.

### L7 · Abdeckung wurde nie gemessen

`coverlet.collector` steckt seit Beginn im Testprojekt. Ausgewertet wurde es bis heute nicht – die
Zahlen in diesem Dokument sind der erste Lauf.

### L8 · Frontend ohne Netz

Zwei Vitest-Dateien (`lib/remarks.test.ts`, `vater/navigation.test.ts`) gegen ein Frontend, das laut
CLAUDE.md den vollständigen Vater→Sohn-Durchstich trägt. Die zehn Playwright-Specs sind das eigentliche
Netz, laufen aber nur von Hand.

### L9 · Toter Testhelfer

Symptom von L3, aber eigenständig zu erledigen: `CreateVocabPlanAsync` behauptet in seiner Doku
„*Legt einen Vokabel-Lehrplan mit zwei Seed-Vokabeln an*" und legt einen **leeren** Container an.
Der eine Aufrufer (`OwnershipTests`) merkt es nicht.

## Der Plan

Vier Etappen, nach Wirkung pro Aufwand geordnet. A wirkt sofort und ist an einem Nachmittag fertig;
C ist die eigentliche inhaltliche Arbeit.

### Etappe A · Das Tor (¼ Tag) – macht die 467 Tests wirksam · **umgesetzt 2026-07-29**

| # | Aufgabe | Stand |
|---|---|---|
| A1 | `.github/workflows/ci.yml`: auf `push` (main), `pull_request` und `workflow_dispatch` → `restore` → `build -c Release` → `test -c Release --no-build` → `format --verify-no-changes`. Ein Job, `ubuntu-latest`, `10.0.x`. Format **hinter** die Tests, damit der inhaltliche Befund zuerst kommt; `.trx` als Artefakt. | **fertig** – YAML validiert; der rote Lauf ist noch am echten Push zu sehen |
| A2 | `deploy-azure.yml` hängt an A1 – nicht per `needs:` (getrennter Workflow), sondern per `workflow_run` auf `[CI]`/`main` mit `if: conclusion == 'success'`. Der Checkout nimmt ausdrücklich `workflow_run.head_sha`, sonst deployte der Default-Branch-HEAD und damit womöglich ein **ungeprüfter** Folge-Commit. | **stillgelegt 2026-07-30** (war fertig, s. u.) |
| A3 | `actions/cache` auf `~/.nuget/packages`, Schlüssel über alle `*.csproj` + `dotnet-tools.json`. `--no-build` beim Test spart den zweiten Build. | **fertig** – Laufzeit erst am echten Lauf messbar |
| A4 | Test-Tor im Arbeitsfluss (L2) als **Stop-Hook** `.claude/hooks/test-gate.sh`. Drei Sparmaßnahmen: keine `.cs`-Abweichung gegenüber `HEAD` → nichts tun; gleicher Inhalt wie beim letzten grünen Lauf → Skip über Fingerprint (`.claude/.test-gate-state`, gitignored); `PUGLING_SKIP_TEST_GATE=1` schaltet ab. `stop_hook_active` verhindert die Schleife. Rot → `exit 2` mit den gefallenen Tests. | **fertig, beide Zweige verifiziert** (absichtlich gebrochener Test → `exit 2` + Namen; Fingerprint-Treffer → `exit 0` in 1 s) |

`-c Release` im Hook ist **Absicht**: läuft parallel ein Dev-Server (`dotnet run` gegen `localhost:5200` –
laut CLAUDE.md der Normalfall beim Prüfen), hält er `bin/Debug/…/Pugling.Contracts.dll` gesperrt und ein
Debug-Build der Solution scheitert mit `MSB3021`, **bevor ein Test läuft**. Release schreibt nach
`bin/Release` und ist unabhängig – und deckt sich zusätzlich mit CI. (Beim Bauen dieser Etappe genau so
passiert.)

Bewusst **nicht** in A: die Playwright-E2E. Sie brauchen Browser-Runner und Startzeit; sie kommen in D.
Ebenfalls nicht: `-warnaserror` an der CLI – das gehört als `Directory.Build.props` in B1, damit es
**lokal genauso** gilt und nicht nur in CI.

#### Was das Tor sofort gefunden hat

Der erste vollständige Lauf nach dem Einziehen ist **rot** – und zwar an einer Stelle, die die Messung vom
2026-07-29 noch grün sah (damals 467 Tests, jetzt 491):

```text
Failed CreatorAgentTests.Bekannte_Vokabeln_werden_verlinkt_statt_dupliziert
  Assert.Equal() Failure: Expected: 31  Actual: 28
```

**Kein Flackern, sondern ein Isolationsfehler.** Einzeln läuft der Test grün, mit seiner Klasse rot – die
Klasse teilt eine SQLite. Der Test legt „the horse"/„das Pferd" im Vokabelspeicher an und erwartet, dass die
Übung auf **seine** Zeile zeigt. Zu dem Zeitpunkt hat aber ein früherer Test derselben Klasse dieselbe
Vokabel längst angelegt (`VocabularyJson` enthält „the horse", die Anlage materialisiert Inline-Vokabeln im
Store, Id 28) – der Test erzeugt also eine **zweite** Zeile (Id 31) und die Dedupe-Logik verlinkt korrekt
die erste. **Das Produktverhalten ist richtig, die Annahme des Tests ist falsch.**

Zwei Wege, Entscheidung offen:

1. **Test isolieren** (klein, empfohlen): nicht auf `existing.Id` prüfen, sondern darauf, dass die Übung auf
   eine *vorhandene* Store-Zeile zeigt und der Lauf **keine neue** angelegt hat. Das ist die Aussage, die der
   Testname verspricht („verlinkt statt dupliziert").
2. **Store dedupliziert beim Anlegen**: `CreateVocabularyAsync` gäbe für gleiche Sprach-/Wortkombination die
   bestehende Zeile zurück. Größer und eine echte Vertragsfrage (zwei Einträge mit verschiedenem `Hint`/
   `PartOfSpeech` können gewollt sein) – nicht nebenbei entscheiden.

Bis das behoben ist, ist CI ab dem ersten Push rot und A2 blockt jedes Deploy. Das ist das Tor bei der
Arbeit, kein Fehler an A – aber es ist der Schritt, der vor B kommt.

### Etappe B · Die Ratsche (½ Tag) – friert den heutigen guten Stand ein · **umgesetzt 2026-07-29**

| # | Aufgabe | Stand |
|---|---|---|
| B1 | `Directory.Build.props` im Repo-Root mit `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`. | **fertig, aber anders zugeschnitten** – siehe „Was die Messung am Plan geändert hat" |
| B2 | `.editorconfig` mit den mechanisch prüfbaren Regeln aus CLAUDE.md. | **fertig**; `dotnet format --verify-no-changes` sauber, Build 0 Warnungen |
| B3 | **`UnmappedMemberHandling.Disallow`** + `ApiErrors.UnknownField` (`unknown_field`, additiv) + Test; L9 mitgezogen. | **fertig** – hat drei echte Vertragsfehler aufgedeckt |
| B4 | Vier reflexive Konventions-Wächter (`ConventionGuardTests`), je mit Selbstschutz gegen falsch-grün. | **fertig**, davon drei hart, einer als Zuwachs-Sperre |

#### Was die Messung am Plan geändert hat

Drei Annahmen des Plans hielten der Messung nicht stand. Alle drei sind ausgemessen und die Entscheidung
steht im Code neben der Einstellung:

1. **`AnalysisLevel = latest-recommended` ist nicht tragbar.** Gemessen: **1358 Warnungen**, davon
   **926× CA1707** („Bezeichner ohne Unterstriche") – das trifft die deutschsprachigen Testnamen, eine
   gewollte Konvention. Eine Ratsche mit 1358 Ausnahmen ist keine. `AnalysisLevel` bleibt darum auf dem
   SDK-Default; einzelne CA-Regeln mit fachlichem Gehalt stehen namentlich in der `.editorconfig`
   (CA2016 Token-Weitergabe, CA1001, CA1805 – zusammen 5 Fundstellen, alle behoben).
2. **L5 traf nicht zu: CS1591 war nicht „nur eine Warnung", sondern abgeschaltet.** Jedes Projekt mit
   `GenerateDocumentationFile` trug zugleich `<NoWarn>$(NoWarn);CS1591</NoWarn>`; die Doku-Regel aus
   CLAUDE.md war also gar nicht mechanisch wirksam. Aufgehoben ergibt sie **636 Fundstellen**, davon
   **428 in `Pugling.Api/Models` und `/Data`** – EF-Entities und der DbContext, die nur wegen EF `public`
   sind und in kein Swagger fließen. Entscheidung: **scharf in `Contracts`/`Client`/`Agent.Creator`**
   (die 94 Lücken im Vertrag sind geschlossen – überwiegend Enum-Werte, die im OpenAPI-Schema stehen),
   **noch unterdrückt in `Pugling.Api`**. Nacharbeit siehe unten.
3. **Eine `.editorconfig`-Zeile kann den halben Baum umschreiben.** `csharp_preserve_single_line_statements
   = false` (ein verbreiteter Default) lässt `dotnet format` jeden kompakten Wächter
   `if (!int.TryParse(parts[1], out var i)) return false;` auf zwei Zeilen umbrechen – beim Einziehen der
   Datei in **einem** Lauf **162 Dateien / ~900 Zeilen**, und zwar genau in der Schreibweise, die CLAUDE.md
   mit „Guard Clauses zuerst" meint. Steht jetzt auf `true`, mit dieser Begründung daneben. **Merksatz für
   jede weitere Regel: eine Ratsche friert den Bestand ein, sie restyled ihn nicht** – nach jeder neuen
   Zeile in der `.editorconfig` einmal `git diff --stat` ansehen, bevor man weitermacht.
4. **„Konventionen werden lückenlos befolgt" (L4) gilt für drei von vier Regeln.** `ProblemWithCode`,
   Vertragstypen und Namens-Eindeutigkeit: null Verstöße, sofort hart. `CancellationToken` an Actions:
   **189 von 337** async Actions nehmen keinen. Der Wächter war darum zunächst eine **Zuwachs-Sperre** mit
   Baseline – sie durfte nur sinken, und sank sie, verlangte der Test die Absenkung. Die Altlast ist am
   2026-07-30 abgearbeitet, der Wächter seither **hart** (siehe „Die CancellationToken-Altlast" unten).

#### Was Etappe B gefunden hat

- **Drei still verworfene Payload-Felder** in der eigenen Testsuite (B3). Neben dem bekannten
  `method`/`contentKeys`/`dailyTestRequired` aus L9: `MediaSelectionTests` schickte beim Anlegen eines
  Plans `endDate`/`active` (Felder, die es nur im Update-DTO gibt) und bei der Position
  `goalCadence`/`goalPoints` (Namen, die der Vertrag nicht kennt – richtig sind `cadence`/`pointsGoalMet`).
  **Vierzehn Tests bauten also auf einer Position ohne Pflichtrhythmus und ohne Punkte auf und hielten
  das für den geprüften Aufbau.** Genau die Fehlerklasse, für die B3 da ist.
- **`TimetableController` prüfte Eigentum inline** statt über den geteilten Filter (B4b) – kein IDOR,
  aber gegen die Konvention, und mit **403** statt des sonst üblichen **404**. Der Statuscode ist kein
  Detail: 403 verrät, dass die Kind-Id existiert. Jetzt am `ChildOwnershipFilter`, damit sich fremde
  Kind-Ids nicht durch Statuscodes enumerieren lassen.
- **Fünf CA-Fundstellen**: vier verschluckte `CancellationToken` (`ExerciseCatalogController.Usage`,
  `PlanPositionsController.Create`) und ein `SemaphoreSlim`, das nie freigegeben wurde
  (`PuglingTokenStore`, jetzt `IDisposable`). Dazu **66 unbenutzte `using`-Direktiven** (IDE0005).

#### Nacharbeit aus B

- **CS1591 in `Pugling.Api` scharf gestellt (2026-07-29).** Neu gemessen statt der alten ~117-Schätzung
  zu vertrauen: **123** echte Lücken (nicht 117) in `Controllers/`, `Auth/`, `Errors/`, `Services/`,
  `Exercises/` und den Nicht-EF-Helfern unter `Data/` (`Seed.cs`, die drei `*Backfill.cs`,
  `PuglingDbContextFactory.cs`). Die Doku-Arbeit lief parallel über drei Subagenten in Worktree-Isolation
  (Exercises-Modul 67 Member, Services/Controllers 33, Auth/Errors/Data-Helfer 23); die Ergebnisse wurden
  anschließend in einem Build/Test-Durchlauf zusammengeführt und verifiziert. Ausgenommen bleiben `Models/`
  (718 Stellen) und `PuglingDbContext.cs` (128 `DbSet<T>`-Properties) – beide sind nur wegen EF `public`,
  dafür jetzt **namentlich** in der `.editorconfig` begründet statt pauschal im csproj unterdrückt. Die
  `NoWarn`-Zeile im csproj ist weg. Endstand: `dotnet build Pugling.sln` 0 Warnungen, 587/587 Tests,
  `dotnet format --verify-no-changes` sauber.
- **Die `CancellationToken`-Altlast abgearbeitet (2026-07-30), der Wächter ist jetzt hart.** Siehe
  „Die CancellationToken-Altlast" unten – dort steht auch, was die Arbeit über die Grenzen von CA2016
  gelehrt hat.
- **Frontend gegen `unknown_field` prüfen.** Die Verschärfung aus B3 trifft jeden Aufrufer, und die
  Playwright-E2E laufen noch nicht in CI (D2). Schickt eine Maske ein Feld, das der Vertrag nicht kennt,
  bekommt sie jetzt 400 statt stillem Erfolg – das ist der Sinn der Sache, will aber einmal durchgespielt
  werden. Die Backend-Suite (499 Tests) deckt es nicht ab.

Zu B4 (c): die Namens-Eindeutigkeit ist bereits als Fallstrick dokumentiert (der OpenAPI-Generator schlüsselt
Schemas über den einfachen Typnamen, gleichnamige Records verschmelzen still). Der Reflexionstest darüber
kostet zwanzig Zeilen und macht aus einem stillen Vertragsfehler einen roten Test.

#### Die CancellationToken-Altlast (2026-07-30)

Der vierte B4-Wächter ist von der Zuwachs-Sperre auf eine **harte Regel** umgestellt: `Async_Actions_Nehmen_Einen_CancellationToken`
verlangt jetzt **0** Verstöße, die Baseline-Konstante ist weg. Erreicht in zwei Schichten, und die Reihenfolge
war nicht Geschmack, sondern erzwungen:

**Schicht 1 – die Kaskade** (Commit „Kaskadenschicht"): 26 Methoden in 10 Dateien, 78 Aufrufstellen. 20 public
async Service-Methoden (Cluster: `PositionProgressService` 6/6, `GamificationService` 5/5, `PositionPlayService`
3/3) plus `AuthAccess` (3) und `ExercisePermissionService` (3), dazu die privaten async Helfer dieser Klassen.

**Warum diese Schicht zuerst und in einem Stück:** CA2016 **koppelt Aufrufer an Aufgerufene**. In dem Moment,
in dem ein Callee einen Token akzeptiert, wird jeder schon tokenisierte Aufrufer, der ihn nicht weiterreicht,
zur CA2016-Warnung und damit zum Build-Fehler. Ein Schnitt „Services jetzt, Aufrufer später" hat deshalb
**keinen grünen Zwischenstand**; die 11 Forwards, die dieser Commit außerhalb seiner 10 Dateien setzt, sind
genau diese Kopplung. Die beiden Ownership-Filter können keinen Parameter bekommen (Signatur durch
`IAsyncActionFilter` festgelegt) und reichen `ctx.HttpContext.RequestAborted` weiter.

**Schicht 2 – die Actions**: 192 Actions in 31 Dateien, 495 Aufrufstellen, 38 stille Helfer. Sieben Gruppen
parallel, disjunkt pro Datei, in Worktree-Isolation (dasselbe Muster wie bei CS1591), zusammengeführt als ein
Octopus-Merge.

| Gruppe | Actions | Aufrufstellen | stille Helfer |
|---|---|---|---|
| Shop + Me | 31 | 53 | 3 |
| Tags + Vokabelspeicher | 29 | 77 | 7 |
| Missionen + Katalog-CRUD | 28 | 54 | 4 |
| Medien + Lückentexte + Interessen | 28 | 69 | 2 |
| Pläne + Auth + Rest | 27 | 62 | 3 |
| Klassenarbeiten + Kinder + Erwachsene | 25 | 74 | 7 |
| Übungs-Controller + Positions-Spiel | 24 | 106 | 12 |

192 statt 188, weil die vier Actions der abstrakten `ExerciseControllerBase` mitgezogen wurden – der Wächter
zählt sie per `DeclaredOnly` nicht.

**Die Konvention, mechanisch:** `CancellationToken ct = default` als **letzter** Parameter. Der Vorgabewert ist
kein Stil, sondern Zwang – C# verbietet einen erforderlichen Parameter nach den optionalen
`[FromQuery] int skip = 0`. Private Helfer tragen ihn ohne Vorgabewert (sie werden immer explizit gerufen).
EF-`FindAsync` nimmt ihn nur als `FindAsync([id], ct)`; die Annahme, man könne solche Stellen notfalls
auslassen, ist falsch – **CA2016 feuert auch dort** und bricht den Build.

##### Was die Arbeit über CA2016 gelehrt hat

Der Analyzer ist ein guter erster Durchgang, **aber kein Netz**. Er prüft die *Kante* zwischen zwei Methoden,
die beide schon einen Token haben – er findet kein fehlendes Glied einer Kette. Drei Blindstellen, alle
gemessen, nicht vermutet:

1. **Lambdas.** Drei Stellen in `RemarksController` und mehrere in `GamificationService` blieben ungemeldet.
2. **Jeder Helfer ohne Token-Parameter** verbirgt sämtliche Aufrufe in seinem Rumpf. Das gilt nicht nur für
   nicht-`async`, Task-rückgebende Ausdrucks-Bodies (die zusätzlich *beidseitig* unsichtbar sind), sondern
   auch für ganz normale `private async Task`-Helfer. In `MeController` hingen an drei solchen Helfern
   8 Aufrufstellen in Actions und 7 EF-/Service-Aufrufe in den Rümpfen: 15 Stellen, die ein grüner Build
   gedeckt hat. Suchkriterium ist der **Rückgabetyp `Task`/`Task<…>` ohne `async`, unabhängig vom
   Sichtbarkeitsmodifikator** – „such nach `private Task`" verpasst die implizit privaten.
3. **Der Wächter selbst misst Signaturen, nicht Ketten.** Die Zahl 188 hat das Problem darum *unterschätzt*.
   Beleg: `MediaAssetsController.Upload` trug den Token längst, der Helfer `UniqueKeyAsync` nicht – dessen
   beide `AnyAsync`-Abfragen liefen **unabbrechbar**, ein echter Abbruch-Leak hinter einer „fertigen" Action.
   Weder der Wächter noch der Scan über die 188 konnten ihn sehen; gefunden hat ihn nur das Lesen.

##### Fallstrick der Worktree-Isolation

**Alle sieben Worktrees zweigten von `main` ab, nicht vom Zweig mit Schicht 1.** Dort nahm `AuthAccess` also
noch keinen Token – und weil CA2016 schweigt, wenn der Callee keinen nimmt, war der Build in diesen Worktrees
**grün, obwohl Forwards fehlten**. Nach dem Rebase kamen nachweislich **28 Forwards** dazu (Übungs-/Positions-Gruppe
17, Shop+Me 5, Tags 5, Medien 1; zwei Gruppen hatten den Fehler selbst bemerkt und vorher korrigiert). Merksatz:
**bei paralleler Arbeit über eine gemeinsame Basisänderung die Basis der Worktrees prüfen, nicht annehmen** –
ein grüner Build im Worktree beweist nur Konsistenz mit *dessen* Basis.

**Endstand:** `dotnet build Pugling.sln` 0 Warnungen/0 Fehler, `dotnet format --verify-no-changes` sauber,
`dotnet test -c Release` 587/587, Endpunkt-Abdeckung 268/268, unabhängiger Quell-Scan 0 verbleibende Actions.
Kein Verhalten geändert. **Ein Befund blieb bewusst ungefixt** (gemeldet, nicht nebenbei behoben):
`ExerciseControllerBase.Update` ruft `NormalizeConfigAsync` nicht, `Create` schon – beim PATCH einer
Übersetzungsübung werden Paare ohne `vocabularyId` daher nicht im Vokabelspeicher angelegt. Sieht nach Lücke
aus, nicht nach Absicht; eigener Vorgang.

### Etappe C · Die Lücken schließen – die eigentliche Arbeit · **umgesetzt 2026-07-29**

| # | Aufgabe | Stand |
|---|---|---|
| C1 | **Ownership-Matrix-Test**: reflexiv alle Actions unter `{childId}`/`{planId}` einsammeln, jede mit einem fremden Vater aufrufen, auf 403/404 prüfen. Statt 6 exemplarischer Fälle die vollständige Fläche. | **fertig** ([OwnershipMatrixTests](../backend/Pugling.Api.Tests/OwnershipMatrixTests.cs)), **zwei** Angreifer statt einem – siehe unten |
| C2 | **PATCH-Semantik-Guard**: für jedes `Update…Dto` prüfen, dass „Feld setzen → PATCH mit `null` → unverändert" gilt; wo ein `Clear…`-Schalter existiert, zusätzlich „`Clear` gewinnt". | **fertig** ([PatchSemanticsTests](../backend/Pugling.Api.Tests/PatchSemanticsTests.cs)): 29 Ressourcen, 50 Tests, Vollständigkeit reflexiv geprüft |
| C3 | Die restlichen Actions je mit **einem** Test berühren – Happy Path + der eine fachlich interessante Fehlerfall. Reihenfolge nach Schadenshöhe: `AdultsController.Delete` (Kaskade!), `ChildrenController.RemoveSupervisor`, `KlassenarbeitenController.Delete`, `ObjectivesController.Delete`, dann der Tag-/Kategorie-/Medien-Schwanz. | **fertig** – es waren **63**, nicht 57 (die Liste unten war veraltet); **0 offen** |
| C4 | **Endpunkt-Abdeckungs-Guard** einziehen, sobald C3 durch ist, damit die Lücke nicht zurückkommt. | **fertig** ([EndpointCoverage](../backend/Pugling.Api.Tests/EndpointCoverage.cs) + [EndpointCoverageGuard](../backend/Pugling.Api.Tests/EndpointCoverageGuard.cs)), **anders gebaut** – siehe „Warum kein `[Fact]`" |
| C5 | `dotnet test --collect:"XPlat Code Coverage"` in A1 aufnehmen, Zweigabdeckung als Bericht ausgeben (zunächst **ohne** harte Schwelle). | **fertig** – Zweige/Zeilen ins Job-Summary, Cobertura + Abdeckungsbericht als Artefakt. Schwelle weiter **bewusst keine** |

#### Was Etappe C gebracht hat

| Achse | vor C | nach C |
|---|---|---|
| Tests | 499 | **587** |
| Endpunkt-Abdeckung | 205 von 268 Actions (63 unberührt) | **268 von 268, 0 offen** |
| Zweigabdeckung `Pugling.Api` | 65,7 % | **69,2 %** |
| Zeilenabdeckung | 97,9 % | 98,2 % (weiter irreführend, s. o.) |

Die Zahl **63** statt der im Plan genannten 57 ist kein Widerspruch, sondern der Grund, warum C mit einer
Messung begann statt mit der Liste: zwischen der Erhebung und dem Beginn von C waren Endpunkte dazugekommen
(`MatchingController.Check`, `VocabularyStoreController.Delete`, `ExerciseGrantsController.Remove`,
`LearnGoalsController.Get`, `TextbookSeriesController.Update`, `MediaVariantsController.Delete`,
`VocabularyController.GetItem`), andere weggefallen. **Eine Abdeckungsliste in Prosa ist nach zwei Tagen
falsch** – deshalb steht sie ab jetzt nicht mehr in diesem Dokument, sondern entsteht bei jedem Lauf neu
(C4).

#### Was Etappe C gefunden hat

Vier echte Defekte, alle in Code, den **kein Test je aufgerufen hatte** – genau die Vorhersage des Plans:

- **`AdultsController.Delete` ließ Waisen zurück.** Die Doku versprach „samt aller Kinder, Fächer, Kapitel",
  aber seit dem Multi-Supervisor-Umbau hängt ein `Child` **nicht** mehr per Fremdschlüssel am Erwachsenen,
  sondern über `SupervisorLink`. Die DB-Kaskade räumte darum nur die Verknüpfung ab: das Kind blieb liegen –
  für keinen Erwachsenen sichtbar oder löschbar, sein PIN-Login aber weiter gültig. Zusätzlich blieb das
  profillose `Account` zurück und hielt seine (eindeutige) E-Mail dauerhaft besetzt. Jetzt gehen die Kinder
  mit, die **ihren letzten** Supervisor verlieren (ein ko-betreutes bleibt bestehen), und das Konto mit.
- **`GET creator/tags/{id}/exercises` antwortete immer 500.** `Include` nach einer Projektion
  (`.Select(x => x.Exercise!).Include(…)`) ist in EF Core ungültig und wirft zur Laufzeit. Die Route war nie
  aufgerufen worden. Es war die einzige Fundstelle dieses Musters im Baum.
- **Doppelte E-Mail und doppelter Fachlehrer-Name lieferten 500 statt eines Fachfehlers.** `Account.Email`
  und `CreatorProfile(OwnerAdultId, Name)` tragen Unique-Indizes; die Controller schrieben blind. Bei der
  Registrierung lief es sogar auf halbem Weg auf – der `Adult` war gespeichert, das Konto scheiterte am
  Index, zurück blieb ein Erwachsener **ohne Login**. Neu: `duplicate_email` und `duplicate_profile_name`
  (beide 409, additiv in `ApiErrors`).
- **Der B4-Wächter prüfte teilweise Routen, die es nicht gibt.** Eine mit `~/` beginnende Action-Vorlage
  **ersetzt** das Controller-Präfix, sie hängt nicht daran (`ShopController` führt so kindgebundene Routen
  unter seinem Shop-Präfix). Das naive Verketten ergab
  `api/v1/supervisor/shop/~/api/v1/supervisor/children/3/…`. Aufgefallen, weil C1 diese URLs tatsächlich
  aufrief – ein reiner Attribut-Check merkt es nie. Die Routen-Auflösung liegt jetzt geteilt in
  [ApiSurface](../backend/Pugling.Api.Tests/ApiSurface.cs), damit die drei reflexiven Wächter dieselbe
  Fläche sehen.

Dazu ein Fund am Wächter selbst: **C2 hätte einen blinden Fleck gehabt.** Der Vertrag nennt die Ziel-Ebene
`UpdateObjectiveRequest`/`UpdateKeyResultRequest`/`UpdateLearnGoalRequest`, alles andere `Update…Dto`. Eine
Prüfung nur auf `…Dto` hätte diese drei stillschweigend ausgelassen und dabei Deckung behauptet – schlimmer
als kein Wächter. Er prüft jetzt beide Namensformen.

#### Warum C1 zwei Angreifer hat, und warum ein Rumpf gebaut wird

Zwei Dinge, die beim Bauen nicht optional waren:

1. **Ein fremder Supervisor allein genügt nicht.** Auf einer `student/…`-Route scheitert er womöglich schon
   an der Rolle, und ein Rollen-403 sieht genauso aus wie ein Eigentums-403. Der zweite Durchgang läuft
   darum mit einem **Kind**-Token: es trägt die Student-Rolle und kommt bis zur Eigentumsprüfung.
2. **Für schreibende Actions muss die Nutzlast bindbar sein.** `ChildOwnershipFilter`/`PlanOwnershipFilter`
   sind `IAsyncActionFilter` und laufen **nach** der Modellbindung – und nach dem `ModelStateInvalidFilter`
   (Order −2000). Ein `POST` mit leerem Rumpf bekäme **400**, bevor die Eigentumsprüfung dran ist; die
   Matrix könnte über Schreibpfade dann nichts sagen. [SampleJson](../backend/Pugling.Api.Tests/SampleJson.cs)
   baut darum je DTO eine gerade eben gültige Nutzlast (nur Pflichtfelder). Ein 400 wird getrennt als
   **„unentschieden"** gemeldet, nicht als Erfolg – sonst tarnt sich eine echte Lücke als Validierungsfehler.

Gegenprobe, dass die Matrix trägt: `[ServiceFilter(typeof(ChildOwnershipFilter))]` an `TimetableController`
entfernt → sie meldet `GET → 200`, `POST → 201` (**ein fremdes Kind bekam einen Stundenplan-Eintrag**) und
`DELETE → 204`.

#### Warum kein `[Fact]` – der Abdeckungs-Wächter sitzt im Assembly-Fixture

Der Plan schlug für C4 „Middleware im Testhost + ein `[Fact]`" vor. Das `[Fact]` geht **nicht**: xUnit
parallelisiert über Collections, eine Reihenfolge „zuletzt" gibt es nicht, und vor dem letzten Test weiß
niemand, was noch berührt wird. Der Wächter urteilt darum im Aufräumen eines **Assembly-Fixtures**
(`[assembly: AssemblyFixture(…)]`, xUnit v3) – nur dort steht fest, dass alle Tests durch sind.

Der Preis ist eine Eigenheit, die man kennen muss: **eine Ausnahme von dort lässt den Lauf scheitern
(Exit-Code 1, und die `.trx` trägt die vollständige Meldung), aber der Konsolen-Zusammenzug meldet trotzdem
„Passed!" und zeigt bloß `Xunit.Sdk.TestPipelineException` – ohne Grund.** Auch `Console.WriteLine` aus dem
Fixture kommt nicht durch (nachgemessen, beide Ströme). Ein rotes Tor ohne Befund ist unbrauchbar, deshalb:

- der Wächter schreibt `TestResults/endpoint-coverage.txt` (gitignored) mit der Liste der offenen Actions,
- der Stop-Hook gibt sie bei „Cleanup Failure" aus,
- CI gibt sie bei `failure()` aus und lädt sie als Artefakt hoch.

Gezählt wird nur ein Aufruf mit **Status < 400**. Sonst hätte C1 die Abdeckung vorgetäuscht: es ruft jede
kindes-/plan-gebundene Action auf und bekommt 403/404. Ein 2xx belegt dagegen, dass der Rumpf gelaufen ist.
Die Zählung schneidet außerdem mit dem Soll (`DeclaredOnly`, wie die übrigen Wächter) – die Middleware sieht
auch die aus `ExerciseControllerBase` **geerbten** CRUD-Actions der typisierten Übungs-Controller, und ohne
Schnitt käme „berührt" über „Soll" hinaus.

Gegenprobe: einen Test auf `[Fact(Skip = …)]` gesetzt → Exit 1, und der Bericht nennt genau
`MediaVariantsController.Delete`.

### Etappe D · Frontend und Rand (1–2 Tage) – **D1–D4 umgesetzt**

| # | Aufgabe | Worauf zu achten ist |
|---|---|---|
| D1 | **umgesetzt** – Job `frontend` in `ci.yml`: `npm ci --legacy-peer-deps` → `npm run build` (`tsc -b && vite build`) → `npm test` (Vitest) | Eigener Job mit `actions/setup-node` + `cache: npm` (`cache-dependency-path: frontend/package-lock.json`, das Lockfile liegt nicht im Root), **parallel** zum .NET-Job, kein `needs:`. `NODE_VERSION` deckt sich mit `deploy-azure.yml` – ein Tor, das eine andere Umgebung prüft als die, in der deployt wird, bewacht nichts. Gemessen aus frischem Checkout: Install, Build und 21 Vitest-Tests grün. |
| D2 | **umgesetzt** – [.github/workflows/e2e.yml](../.github/workflows/e2e.yml) auf `pull_request` + nightly (03:00 UTC) + Handbetrieb: nur Chromium (`install --with-deps chromium`), Backend **vorgebaut**, die 25 Tests in 10 Specs, Trace/Screenshot als Artefakt bei Rot | Lokal belegt mit `CI=1 npx playwright test`: **25/25 grün in 1,5 min**. **In echtem CI bisher null Läufe** (gemessen 2026-07-29 über `gh api .../workflows/e2e.yml/runs`) – der Workflow triggert nur auf `pull_request`/nightly/Handbetrieb, bisher ging alles per direktem Push auf `main`. Zwei Dinge sind Entscheidung, nicht Zufall: **kein `push: main`** (das Deploy hängt an `CI`, nicht hier – rote E2E sind Diagnose, kein Freigabe-Tor) und **kein Retry** (die Specs teilen eine DB je Lauf; ein Retry liefe auf der beschriebenen DB und könnte grün werden, ohne dass der Fehler weg ist). |
| D3 | **umgesetzt** – `markdownlint-cli2` als eigener Job `markdownlint` in `ci.yml` | Ausgangsbefund 2026-07-29: 536 Treffer in 27 Dateien (Details unten unter „Was D3 vorfindet"). Alle drei Entscheidungen mit Empfehlung vorgelegt und vom Nutzer bestätigt: Glob fest in `.markdownlint-cli2.jsonc` (`globs: ["**/*.md"]`), Generator (`DocsCaptureTests.WriteMarkdown`/`RenderGroup`) lint-konform gemacht statt Ausnahme, MD033+MD004 projektweit abgeschaltet. Endstand: **0 Treffer**, `dotnet test Pugling.sln` weiterhin 587/587, `dotnet format --verify-no-changes` sauber. |
| D4 | **umgesetzt** – `git diff --exit-code -- docs/api-examples` direkt nach `Test` in `ci.yml` | War zuerst nur eine Annahme ohne CI-Beleg (`ci.yml` diffte nie tatsächlich). Als echtes Tor eingezogen, **erster Lauf sofort rot** – kein Umgebungsunterschied, sondern ein echter Bug: `Truncate()` (1500-Zeichen-Grenze) schneidet am rohen `JsonSerializer(WriteIndented: true)`-String, dessen Zeilenumbruch ohne explizite Angabe an `Environment.NewLine` hing (`\r\n` Windows, `\n` Linux) – der Schnitt landete je Plattform an unterschiedlicher Stelle mitten im JSON (nur `shop.md`/`remarks.md` betroffen, die einzigen Antworten über der Grenze). Gefixt: `NewLine = "\n"` explizit in den `Indented`-Optionen (`DocsCaptureTests.cs`). Zweiter Lauf **komplett grün**, alle drei Jobs. |

**D0 (erledigt): die drei Etappen sind committet.** Der Schnitt und die je Commit gemessenen Zahlen stehen
unter „Einstieg für eine frische Sitzung". D beginnt damit auf einem Stand, auf dem ein roter CI-Lauf
eindeutig dem neuen Tor zuzuordnen ist.

#### Was D3 vorfindet (gemessen 2026-07-29, damit die nächste Sitzung nicht neu erhebt)

> **Erledigt.** Der Ausgangsbefund unten ist die Momentaufnahme, gegen die die drei Entscheidungen getroffen
> wurden; die Zahlen sind historisch (das Muster ist die Aussage, nicht die Zahl – wie bei den 57/63 nie
> aufgerufenen Actions oben). Endstand nach Umsetzung: **0 Treffer**, `markdownlint`-Job in `ci.yml`. Ein
> vierter, ungeplanter Fund unterwegs: `--fix` korrigiert nicht immer sicher – zwei echte Regressionen
> traten auf und wurden von Hand zurückgedreht (siehe „Was D3 zusätzlich gefunden hat" unten).

`npx markdownlint-cli2 "**/*.md"` meldet **536 Treffer in 27 Dateien**. Die Zahl schreckt nur, solange man
sie nicht aufteilt:

| Anteil | Treffer | Was damit zu tun ist |
|---|---|---|
| `docs/api-examples/**` | **380** | **Nicht von Hand anfassen.** Diese Dateien schreibt `DocsCaptureTests` bei *jedem* Testlauf neu (siehe D4) – eine Handkorrektur ist beim nächsten `dotnet test` weg. Entweder in `ignores` aufnehmen oder den Generator lint-konform ausgeben lassen (fast alles ist MD031/MD022: fehlende Leerzeile um Codeblöcke und Überschriften – im Generator eine Kleinigkeit, danach dauerhaft sauber). |
| Fremdes `node_modules` | **50** | `.opencode/node_modules/zod/README.md`. Die `ignores`-Liste hat `node_modules/**`, und das greift **nur im Root**. Fix ist eine Zeile: `**/node_modules/**`. Reine Lücke der Konfiguration, keine Doku-Frage. |
| Handgeschriebene Doku | **106** | Die eigentliche Arbeit, verteilt auf 13 Dateien. Spitzenreiter: `docs/perf-explain-2026-07-12.md` (31), `docs/pm-sitzung-2026-07-04.md` (24), `docs/anmerkungen/aktuell.md` (20 – ebenfalls ein **Export**, gehört eher zu Zeile 1). Nach Regel: 40× MD032 (Leerzeile um Listen), 12× MD022, 11× MD009 (Leerzeichen am Zeilenende), 9× MD031, 7× MD029, 6× MD028, 5× MD040 (Codeblock ohne Sprache), 4× MD004. |

Dazu **50× MD033/no-inline-html**, quer über die generierten Dateien: bewusstes HTML in der Doku. Das ist
eine Regel-Entscheidung (abschalten oder pfadweise erlauben), keine Fundstelle zum Reparieren.

Die zwei bekannten `CLAUDE.md`-Treffer stehen weiterhin auf **Zeile 118 und 124** (MD004/ul-style, `+` statt
`-`): das sind **keine Listen**, sondern Zeilenfortsetzungen. Umformulieren oder MD004 abschalten –
**nicht** den Umbruch „reparieren", der Satz wird dabei falsch.

Drei Entscheidungen also, bevor eine einzige Leerzeile gesetzt wird: **(1)** welchen Glob ruft CI auf (ein
bares `npx markdownlint-cli2` lintet **nichts**, die `.markdownlint-cli2.jsonc` nennt keine `globs`),
**(2)** generierte Dateien ignorieren oder den Generator sauber ausgeben lassen, **(3)** MD033 und MD004.
Erst danach ist der Rest mechanisch – und `--fix` erledigt MD009/MD012/MD031/MD022 größtenteils selbst.

#### Was D3 zusätzlich gefunden hat

Alle drei Entscheidungen fielen auf die empfohlene Option (Glob in der Config, Generator fixen, MD033+MD004
abschalten). Umgesetzt: `globs` in `.markdownlint-cli2.jsonc`, `**/node_modules/**` statt `node_modules/**`
(traf sonst nur den Root), `.agents/**` neu in `ignores` (ein lokal installiertes, `.gitignore`tes
Drittanbieter-Skill-Paket, keine Projekt-Doku – 8 Treffer, die dem Repo gar nicht gehören). `RenderGroup`
in `DocsCaptureTests.cs` bekam die fehlenden Leerzeilen um Überschriften/Codezäune (MD022/MD031: 380 → 13
Treffer), dazu eine `NormalizeTrailingNewline`-Hilfe gegen doppelte Leerzeilen am Dateiende (MD012/MD047).
Der Rest ging über `npx markdownlint-cli2 --fix` – **aber nicht blind**: `--fix` traf zweimal eine falsche
Entscheidung, die eine echte Bedeutungsänderung gewesen wäre, hätte niemand den Diff gelesen:

- **`docs/pm-sitzung-2026-07-04.md`**: Fließtext „`#2 (Sound/Feier) und #3 …`" (Verweis auf Wunsch Nr. 2,
  keine Überschrift) stand am Zeilenanfang. MD018 (kein Leerzeichen nach `#`) interpretierte das als
  ATX-Überschrift ohne Leerzeichen und „reparierte" es zu `# 2 (Sound/Feier) …` – eine **echte** H1-Überschrift,
  wo vorher Prosa stand. Von Hand zurückgedreht (Zeile mit dem Vorsatz verschmolzen, damit `#2` nicht mehr am
  Zeilenanfang steht).
- **`docs/perf-explain-2026-07-12.md`**: Eine 8er-Liste (`1.` … `8.`) hatte je Punkt eine direkt folgende,
  nicht eingerückte `-`-Zeile („Erwarteter Index: …") ohne Leerzeile dazwischen. Der MD032-Fix fügte die
  Leerzeile ein – wodurch CommonMark jeden nummerierten Punkt und seine Erklärung als **eigene, unabhängige
  Einzelpunkt-Liste** parst (jeder Markerwechsel `1.`→`-` beginnt ohnehin eine neue Liste). `--fix` erkannte
  das und normierte MD029 (ol-prefix) auf „alle `1.`" – rendert zufällig richtig (jede Einzelpunkt-Liste startet
  bei ihrer eigenen Zahl), liest sich im Quelltext aber falsch. Behoben, indem die Erklärzeile als
  **eingerücktes** Unterelement des jeweiligen Listenpunkts steht (drei Leerzeichen + `- Erwarteter Index: …`) –
  damit ist es strukturell eine durchgehende 8er-Liste, keine acht einzelne.
- **`docs/pm-sitzung-2026-07-04.md`**: Nach dem Umbau der zweiten `# PM-Sitzung 2 …`-Überschrift zu `##`
  (gegen MD025, zwei H1 im selben Dokument) wurden zwei „`## PM-Synthese & Priorisierung (→ Entwickler)`"
  zu echten Geschwistern (MD024, `siblings_only`). Je Sitzung benannt: „… Sitzung 1 …" / „… Sitzung 2 …".
- **`docs/anmerkungen-plan.md` + `docs/anmerkungen/aktuell.md`** (MD028, Leerzeile im Blockzitat): Der
  eigentliche Fund lag im **Export-Service**, nicht in der Doku – `RemarkExportService.AppendComments`
  (backend/Pugling.Api/Services/Shared/RemarkExportService.cs) trennte aufeinanderfolgende Kommentare eines
  Verlaufs mit einer **nackten** Leerzeile mitten im Zitat. Gefixt an der Quelle (zitierte Leerzeile `>`
  zwischen zwei Beiträgen, nackte Leerzeile nur nach dem letzten); die bereits committete `aktuell.md`
  (echter Datenexport, keine Testausgabe) von Hand auf densel­ben Stand nachgezogen, da sie nicht durch
  einen Testlauf neu entsteht.

**Lehre**: `--fix` ist ein guter erster Durchgang, aber kein Ersatz für `git diff` danach – bei
Regel-Umschreibungen, die auf Heuristiken beruhen (MD018, MD029), kann „lint-konform" und „bedeutungsgleich"
auseinanderfallen.

#### Was D1 sofort gefunden hat: das Deploy war 24 Tage kaputt

Bevor der Job geschrieben war, wurde gemessen, was er ausführen würde – und `npm ci` **scheitert aus dem
Leerstand** mit `ERESOLVE`: `vite-plugin-pwa@0.21` deklariert Peer `vite ^3…^6`, installiert ist `vite@8`.
Dass lokal alles läuft, liegt an einem `node_modules`, das einmal mit `--legacy-peer-deps` entstanden ist
(so steht es auch in [frontend/CLAUDE.md](../frontend/CLAUDE.md)); ein frischer Runner hat das nicht.

Damit war nicht nur der neue Job rot, sondern **`deploy-azure.yml` an derselben Zeile schon die ganze Zeit**:
seit dem Vite-8-Sprung (`2c4eb69`, 2026-07-05) konnte kein Deploy mehr den Frontend-Build erreichen. Nichts
hat es gemeldet, weil niemand das Ergebnis gelesen hat – genau die Lücke, die dieser Plan schließt, hier
einmal in Reinform: **ein Schritt, den kein Tor prüft, ist ein Schritt, dessen Zustand niemand kennt.**
Beide Workflows installieren jetzt mit `--legacy-peer-deps`.

Der Peer-Konflikt selbst bleibt bestehen und ist damit **nicht** behoben, nur benannt: `vite-plugin-pwa` auf
eine Vite-8-fähige Fassung zu heben (oder Vite zu pinnen) ist eine Dependency-Entscheidung mit Wirkung auf
das PWA-Artefakt und gehört nicht in ein CI-Tor. Offener Punkt, kein Nebenbei-Fix.

### Etappe E · Schema-Tore (G1–G9) – **umgesetzt 2026-07-31**

Die Tore A–D halten *Code*-Konventionen. Für die **Form des Datenbankschemas** gab es keine – und dort saßen
zwei echte Datenverlust-Defekte. Der DB-Struktur-Umbau
([db-struktur-umbau-plan.md](db-struktur-umbau-plan.md)) hat sie behoben und die Regeln danach mechanisch
festgenagelt: neun Tore in `backend/Pugling.Api.Tests/SchemaGuardTests.cs`, jedes **mit der Etappe eingezogen,
die es grün machte**, jedes mit Selbstschutz gegen falsch-grün und jedes mit einer gesehenen
Falsch-Grün-Probe.

| Tor | Regel | Mechanik |
|---|---|---|
| **G1** | Kein Modell-Drift | `HasPendingModelChanges() == false` |
| **G1b** | Migrationskette == **1** | `GetMigrations().Count() == 1`; **bewusst endlich** – endet mit der ersten Veröffentlichung, und dann wird das Tor *ausdrücklich* entfernt statt zu erodieren |
| **G2** | Jede FK hat ein abgenommenes `OnDelete` | literal gepinnte Tabelle über alle FKs, als sortierte Zeilen verglichen; zusätzlich **`ClientSetNull` verboten** (der Konventions-Default räumt nur im geladenen ChangeTracker auf) |
| **G3** | Jede String-Spalte hat eine Länge – eine unique-indizierte **muss** eine haben | `GetMaxLength()`, Ausnahmen aus `PuglingDbContext.UnlimitedByDesign` |
| **G4** | Jedes persistierte Enum liegt als String | ValueConverter auf `string`; Ausnahmen `IntEnumsByDesign` + `[Flags]` |
| **G6** | Kein Zeitpunkt/Zeitraum als Text | Namensregel (`…Key/Period/Day/Date/On/At/Time/Week/Month/Year`), **case-sensitiv**; Ausnahmen namentlich mit Grund |
| **G7** | Jede JSON-Spalte hat einen ValueComparer | Sammlungs-/Komplextyp mit String-Converter ⇒ Annotation `ValueComparer` gesetzt |
| **G8** | Die erwarteten Check-Constraints existieren | **Mengen**-Vergleich (eine verschwundene Invariante ist genauso ein Fund wie eine neue ohne Eintrag) |
| **G9** | Genau **ein** gewollter DB-Default | `IColumn.DefaultValue` über das relationale Modell |

**Zwei Fallstricke, die dabei Zeit gekostet haben** und für jedes weitere Schema-Tor gelten:

1. **`db.Model` ist nicht das ganze Modell.** Das laufzeit-optimierte Modell wirft weg, was zur Laufzeit
   niemand liest: Check-Constraints (es *wirft* dann sogar – „not stored in the read-optimized model") und
   **Annotationen**. G8 und G7 brauchen darum `db.GetService<IDesignTimeModel>().Model`
   (`Microsoft.EntityFrameworkCore.Metadata`), G9 das relationale Modell.
2. **„Gibt es einen" ist nicht „wurde einer gesetzt".** `GetValueComparer()` liefert immer etwas – im
   Zweifel den referenzvergleichenden Default, also genau den Fehlerfall. Gefragt ist die Annotation.

**Bewusst nicht mechanisiert:** „jeder Idempotenz-Log hat einen Unique-Index" – „Idempotenz-Log" ist keine
reflektierbare Eigenschaft, und ein Tor auf Tabellennamen-Heuristik ist ein Tor, das man umbenennt statt
erfüllt. Ebenso **G5** (FK-lose `…Id`-Spalten): die Liste wäre reine Buchhaltung ohne mechanischen Nutzen,
solange G2 jede echte FK schon erzwingt – die Begründungen stehen stattdessen an den Spalten selbst.

## Was bewusst nicht getan wird

- **Keine parallele Unit-Test-Schicht über den Services.** Die Fehlerklassen dieses Projekts sind
  Vertrag, Ownership, Zustand und Idempotenz – die zeigen sich am zusammengesetzten System, nicht an der
  isolierten Methode. Wo Algorithmik isoliert prüfbar ist, gibt es sie schon
  (`ArithmeticProblemGeneratorTests`, `BirkenbihlDecodingServiceTests`, `PointKindCurrencyTests`).
- **Kein Mocking-Framework.** Die Wegwerf-SQLite je Testklasse ist schneller aufgesetzt und näher an der
  Wahrheit als ein Mock des `DbContext`; die Migrationen werden dabei gratis mitgeprüft.
- **Keine Abdeckungsquote als Ziel.** Die 97,9 % Zeilen sind der Beweis, dass die Quote lügt. Ziel ist die
  **Endpunkt**-Vollständigkeit (C4), nicht eine Prozentzahl.
- **Kein `v2` und keine Abwärtskompatibilität** für die Vertragsverschärfung aus B3. Bis zur Publikation
  gilt laut CLAUDE.md `1.0` als frei änderbar; ein Client, der auf stillschweigend verworfenen Feldern
  aufbaut, ist bereits kaputt.

## Reihenfolge, wenn nur ein Tag da ist

A1 + A2 + A4 + B1 + B3. Damit laufen die vorhandenen 467 Tests bei jedem Push **und** im Arbeitsfluss,
ein Deploy braucht grün, Warnungen sind Fehler, und der Server sagt „nein" statt „201" zu einem Feld,
das er nicht kennt. Das ist der größte Teil der Wirkung für ein Achtel des Aufwands.

## Wie man das nachmisst

Die Zahlen dieses Dokuments sind reproduzierbar:

```bash
dotnet test Pugling.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
# Die 0-%-Klassen der Form  <Controller>/<Action>d__N  in coverage.cobertura.xml
# sind die nie berührten Actions – daraus entsteht die Tabelle oben.
dotnet build Pugling.sln --nologo                       # heute: 0 Warnungen
dotnet format Pugling.sln --verify-no-changes --no-restore   # heute: sauber (139 s)
```

## Verwandt

- [code-review.md](code-review.md) – der Review, der die Integrationstests angestoßen hat
- [architektur-entscheidung.md](architektur-entscheidung.md) – API-First; warum der Vertrag das Produkt ist
- [endpunkt-beziehungen.md](endpunkt-beziehungen.md) – die Wissenskarte für C1/C3
- [CLAUDE.md](../CLAUDE.md) – die Konventionen, die B2/B4 mechanisch machen sollen
