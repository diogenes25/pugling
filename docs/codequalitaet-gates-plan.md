---
tags: [typ/plan, bereich/doku, bereich/api]
aliases: [Codequalität-Gates, Test-Gate, CI-Plan, Integrationstests bewerten]
---

# Codequalität: von Disziplin zu mechanischen Toren

Status: **Etappen A, B und C umgesetzt (2026-07-29), D offen.** Alle Zahlen im Abschnitt „Ist-Zustand" sind
am 2026-07-29 am damaligen Arbeitsstand gemessen (Etappe 2 der `Father`→`Adult`-Umbenennung im Baum, noch
nicht committet – sie ist seither als `1ee1538` committet); der Stand **nach C** steht unter „Was Etappe C
gebracht hat". Was A gebracht hat, steht unter „Etappe A"; der **erste Fund des neuen Tors** unter „Was das
Tor sofort gefunden hat".

## Einstieg für eine frische Sitzung

Diese Seite ist die vollständige Übergabe – **nicht neu messen, nicht neu erheben.** Der Ausgangsbefund steht
unter „Ist-Zustand" (Momentaufnahme 2026-07-29), der erreichte Stand unter „Was Etappe C gebracht hat", die
Arbeit unter „Der Plan".

**Arbeitsstand nach Etappe C (2026-07-29):**

- `HEAD` = `1ee1538` „Vertrag heißt `adult` – Etappe 2 der Father→Adult-Umbenennung".
- **A, B und C liegen vollständig im Baum, aber nicht committet**: 96 Dateien geändert, 17 unversioniert
  (davon 12 `.cs`). Der Baum ist **grün**: 587 Tests, `dotnet build Pugling.sln` 0 Warnungen,
  `dotnet format --verify-no-changes` sauber, Testlauf-Exit-Code 0.
- Die Tore stehen und greifen: CI-Workflow, `workflow_run`-Kopplung des Deploys, Stop-Hook,
  `Directory.Build.props`, `.editorconfig`, `UnmappedMemberHandling.Disallow` und **sieben** reflexive
  Wächter (vier aus B4, dazu Ownership-Matrix, PATCH-Semantik und Endpunkt-Abdeckung aus C).

**Empfehlung: erst committen, dann Etappe D.** Ein 113-Dateien-Stand aus drei Etappen und die nächsten
Tore im selben roten CI-Lauf sind nicht auseinanderzuhalten. Sinnvoller Schnitt in drei Commits:

1. **Die Tore (A+B)** – `ci.yml`, `deploy-azure.yml`, `test-gate.sh`, `Directory.Build.props`,
   `.editorconfig`, `UnmappedMemberHandling` + `unknown_field`, `ConventionGuardTests`,
   `UnknownFieldTests`, dazu die Folgeänderungen (66 unbenutzte `using`, 5 CA-Fundstellen, die drei
   still verworfenen Payload-Felder in der Testsuite, `TimetableController` an den Ownership-Filter).
2. **Die Wächter (C1/C2/C4/C5)** – `ApiSurface`, `SampleJson`, `OwnershipMatrixTests`,
   `PatchSemanticsTests`, `EndpointCoverage`(+`Guard`), die Coverage-Schritte in `ci.yml`, der
   Bericht-Zweig im Stop-Hook.
3. **Die Defektfixes samt Tests (C3)** – `AdultsController` (Kaskade + `duplicate_email`),
   `CreatorProfilesController` (`duplicate_profile_name`), `TagsController` (EF-`Include`-Fehler),
   die beiden neuen `ApiErrors`, `AdultLifecycleTests`, `CatalogReadDeleteTests`,
   `ExerciseCheckEndpointTests`, `MediaLinkTeardownTests` und die Ergänzungen in den bestehenden
   Testklassen.

**Der erste inhaltliche Schritt ist D1** (siehe Etappe D) – er berührt nur `.github/workflows/ci.yml`,
keinen Produktivcode. Parallel dazu ist die **Nacharbeit aus B** offen (CS1591 in `Pugling.Api`, die 188
`CancellationToken`-Altlasten, das Frontend gegen `unknown_field`); die drei sind voneinander unabhängig.

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
| A2 | `deploy-azure.yml` hängt an A1 – nicht per `needs:` (getrennter Workflow), sondern per `workflow_run` auf `[CI]`/`main` mit `if: conclusion == 'success'`. Der Checkout nimmt ausdrücklich `workflow_run.head_sha`, sonst deployte der Default-Branch-HEAD und damit womöglich ein **ungeprüfter** Folge-Commit. | **fertig** |
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
   **189 von 337** async Actions nehmen keinen. Der Wächter ist darum eine **Zuwachs-Sperre** mit
   Baseline – sie darf nur sinken, und sinkt sie, meldet der Test das und verlangt die Absenkung.

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

- **CS1591 in `Pugling.Api` scharf stellen**: ~117 echte Lücken in `Controllers/`, `Auth/`, `Errors/`,
  `Services/`, `Exercises/` nachziehen, dann per `.editorconfig` pfadweise scharf stellen und
  `Models/`+`Data/` (EF-Entities, 428 Stellen) ausnehmen. Erst dann kann die `NoWarn`-Zeile im csproj weg.
- **Die Actions ohne `CancellationToken`** abarbeiten und die Baseline in `ConventionGuardTests`
  mitsenken. Der Test nennt bei jedem Lauf die vollständige Liste. Stand: **188** (Etappe C hat
  `AdultsController.Delete` mitgezogen – und die Ratsche hat prompt die Absenkung verlangt, genau wie
  vorgesehen).
- **Frontend gegen `unknown_field` prüfen.** Die Verschärfung aus B3 trifft jeden Aufrufer, und die
  Playwright-E2E laufen noch nicht in CI (D2). Schickt eine Maske ein Feld, das der Vertrag nicht kennt,
  bekommt sie jetzt 400 statt stillem Erfolg – das ist der Sinn der Sache, will aber einmal durchgespielt
  werden. Die Backend-Suite (499 Tests) deckt es nicht ab.

Zu B4 (c): die Namens-Eindeutigkeit ist bereits als Fallstrick dokumentiert (der OpenAPI-Generator schlüsselt
Schemas über den einfachen Typnamen, gleichnamige Records verschmelzen still). Der Reflexionstest darüber
kostet zwanzig Zeilen und macht aus einem stillen Vertragsfehler einen roten Test.

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

### Etappe D · Frontend und Rand (1–2 Tage) – **offen, der nächste Schritt**

| # | Aufgabe | Worauf zu achten ist |
|---|---|---|
| D1 | `npm run build` (also `tsc -b`) + `vitest run` in `ci.yml` aufnehmen – billig, fängt Typfehler im Frontend | Eigener Job mit `actions/setup-node` + `cache: npm`, **parallel** zum .NET-Job (keine Abhängigkeit). Das Deploy hängt per `workflow_run` am Gesamt-Ergebnis von `CI`, zieht also automatisch mit. |
| D2 | Playwright als **eigener** Workflow (`e2e.yml`) auf `pull_request` + nightly, nicht im Haupt-Tor: Backend hochfahren, `npx playwright install --with-deps`, die 10 Specs | Bewusst nicht im Haupt-Tor (Browser-Runner + Startzeit). Die E2E teilen sich laut [frontend/CLAUDE.md](../frontend/CLAUDE.md) eine DB – Auswahl **nie** per Index, siehe den Fallstrick in `sohntest-doppelter-versuch`. |
| D3 | `markdownlint-cli2` (Konfiguration liegt bereits im Root) in `ci.yml` – die Doku ist in diesem Projekt Produkt, nicht Beiwerk | Erst messen: `npx markdownlint-cli2` läuft heute **nicht** sauber. In `CLAUDE.md` stehen zwei MD004-Treffer (Zeilen 118/124), die keine Listen sind, sondern Zeilenfortsetzungen mit `+`. Also entweder umformulieren oder MD004 abschalten – **nicht** den Umbruch „reparieren". |
| D4 | Prüfen, ob `/smoke-test` und `DocsCaptureTests` in CI reproduzierbar byte-stabil laufen (die Wanduhr-Neutralisierung ist dafür gebaut; auf einem UTC-Runner erst zu verifizieren) | `ci.yml` setzt schon `TZ: UTC`. `DocsCaptureTests` **überschreibt** `docs/api-examples/` bei jedem Lauf – ein Diff dort nach einem CI-Lauf ist der Befund, kein Versehen. |

**D0 (empfohlen, vor D1): die drei Etappen committen.** Siehe „Einstieg für eine frische Sitzung" – dort
steht der Schnitt in drei Commits. Ohne das laufen A+B+C und D im ersten roten CI-Lauf zusammen.

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
