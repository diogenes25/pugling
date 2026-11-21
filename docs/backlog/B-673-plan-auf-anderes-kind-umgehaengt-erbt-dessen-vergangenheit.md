---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Plan umhängen erbt Malus]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer, Hinweis 2) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-673 · Ein Plan, den der Vater einem anderen Kind gibt, bringt die Vergangenheit des ersten Kindes mit

Der Vater kann in der Plan-Bearbeitung das Kind wechseln. Der Plan wechselt den Besitzer, nimmt aber Spielstand, Serie
und Abrechnungs-Anfang des ersten Kindes mit – das zweite Kind erbt dessen Tage und womöglich dessen Malus.

## User Story

Als Schüler möchte ich einen Plan, den Papa mir zuweist, mit einer leeren Vergangenheit beginnen, damit weder die
Tage meines Bruders meine Serie verfälschen noch seine offenen Tage mir Münzen kosten.

## Ist-Stand am Code

Gelesen am 2026-11-19, nicht per Test nachgestellt.

1. **Umhängen ist ein ausgebauter Weg, kein Versehen.** `UpdatePlanDto.ChildId` ist Vertrag
   (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:66`, `:72-73`: „Moves the plan to another of the caller's own
   children"); die Plan-Bearbeitung im Frontend bietet die Kind-Auswahl an
   (`frontend/src/vater/VaterPlanDetail.tsx:161`, `:179-181`); ein Test sichert die Fach-Regel beim Wechsel
   (`PlanSubjectTests.cs:262-278`).
2. **Der Wechsel ändert nur `ChildId`.** `StudyPlansController.Update`
   (`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:183-187`) setzt `plan.ChildId`; `ActivatedAt`
   ändert sich nur beim Einschalten eines ausgeschalteten Plans (Zeile 220) oder beim Verschieben des Starts (Zeilen
   225-229). Positionen, `PositionItemProgress`, `PracticeSessions`, `TestAttempts`, `PositionGoalRewards` und
   `PositionGoalPenalties` bleiben am Plan, also beim neuen Kind. (Das Fach des Zielkinds wird danach über
   `DeactivateSameSubjectPlansAsync`, Zeile 232, bereinigt – das ist der einzige Nachlauf.)
3. **Der Spielstand hängt am Plan, nicht am Kind.** `TestAttempt` und `PracticeSession` tragen keine `ChildId`; sie werden
   über `StudyPlan.ChildId` zugeordnet (`MetricsService.cs:31-41`, `PositionProgressService.cs:856-859`). Die
   Zuordnung läuft nach dem Wechsel also automatisch auf das neue Kind.
4. **Serie und Tage:** `DutyStateAsync` (`PositionProgressService.cs:461-471`) prüft `IsGoalMetAsync` gegen die Sitzungen
   und Versuche des Plans – die des ersten Kindes. `IsCompleteDay` (Zeilen 439-443) zählt `Done` ohne `Active`. Die
   erfüllten Tage des ersten Kindes werden damit zu **erfüllten Tagen des zweiten** (zu seinen Gunsten); die offenen
   Tage eines aktiven Plans (`ActiveOn`: `day >= ActivatedAt`, Zeile 82, bei einem Plan, der seit dem `StartDate`
   läuft) zu **offenen Tagen** – die Serie des neuen Kindes reißt an Tagen, die es nie hätte spielen können.
5. **Malus:** `SettleClosedPeriodsAsync` (Zeilen 754-840) holt alle Positionen der Pläne des Kindes, nimmt als Beginn
   den ersten Spieltag des Plans aus `FirstPlayDaysAsync` (Zeilen 849-866) – die Einführungstage und Versuche des
   **ersten** Kindes – und rechnet nicht abgerechnete Perioden der letzten 14 Tage (`MaxSettleLookbackDays`, Zeile 25)
   ab. `PlanDueForPeriod` (Zeilen 677-679) lässt sie wegen `ActivatedAt <= StartDate` fällig. Folge bei einem gespielten Plan: das neue Kind
   zahlt `PenaltyCoins` für Perioden, in denen es nie Zugang hatte. Bereits gebuchte Belohnungen und Strafen des ersten
   Kindes liegen als `PositionGoalReward`/`PositionGoalPenalty` an der Position; die Idempotenz-Prüfung (Zeilen 791-794)
   überspringt diese Perioden – für das zweite Kind heißt das: keine Belohnung dort, keine Strafe dort.
6. **Reine Vermutung der Idee, hier nicht belegt:** „ActivatedAt = heute" als Lösung. Nach dem Code reicht das nicht
   (siehe Lücke).
7. Ein Test für „Plan mit Spielstand umhängen": **nicht gefunden** (`PlanSubjectTests.cs` prüft nur den Fach-Wechsel
   eines leeren Plans).

## Die echte Lücke

- Der Vorschlag der Nachschau, beim Kindwechsel eines aktiven Plans `ActivatedAt = heute` zu setzen, repariert beim
  **gespielten** Plan den **Malus** (`PlanDueForPeriod` verlangt dann `from >= ActivatedAt`) und die **offenen** Tage
  der Serie (`ActiveOn` ist davor falsch). Er repariert **nicht**, dass der erledigte Spielstand des ersten Kindes dem zweiten gutgeschrieben
  wird (`Done` hängt an den Sitzungen des Plans, nicht an `ActivatedAt`), noch dass dessen Leitner-Stufen und
  Einführungsstände (`PositionItemProgress`) am Plan hängen bleiben.
- Das Umhängen eines **ungespielten** Plans ist harmlos – bis auf `ActivatedAt`: ein seit seinem Start aktiver,
  ungespielter Plan lässt die Serie des neuen Kindes über seine vergangenen Tage reißen (Beleg 4).
- Das Umhängen eines **gespielten** Plans hat keinen sauberen Sinn: der Spielstand gehört dem ersten Kind. Gemeint ist
  fast immer „gib meinem Zweiten dasselbe Programm" – das ist ein Kopieren, kein Umhängen.

## Offene Punkte

1. ~~**Darf man einen gespielten Plan überhaupt umhängen?**~~ → Entscheidung 1.
2. ~~**Ungespielte Pläne:**~~ → Entscheidung 2.
3. ~~**Berührt das die Malus-Regeln?**~~ → Entscheidung 3.
4. ~~**Gesperrter Fall im UI:**~~ → Entscheidung 4.
5. ~~**Was heißt „gespielt“?**~~ → Entscheidung 1 (neu beim Grillen).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Ein gespielter Plan lässt sich nicht umhängen: `PATCH` mit einem anderen `childId` antwortet `409` mit dem neuen,
   additiven Code `plan_has_data`. „Gespielt“ heißt: an irgendeiner Position des Plans liegt mindestens eine
   `PracticeSession`, ein `TestAttempt` oder eine `PositionItemProgress`-Zeile.** Begründung: Der Spielstand hängt am Plan
   und wird über `StudyPlan.ChildId` dem Kind zugeordnet (Ist-Stand 3); ein Umhängen kann ihn dem zweiten Kind nur
   mitgeben oder zerstören, beides falsch. Die drei Tabellen sind absichtlich mehr als die Positions-Sperre prüft
   (`PlanPositionsController.cs:409-411`: nur `TestAttempts` und `PracticeSessions`): Die Abrechnung des Malus beginnt am
   ersten Tag, an dem das Kind ein Wort **eingeführt** hat (`FirstPlayDaysAsync`, `PositionProgressService.cs:849-866`,
   liest `PositionItemProgress.IntroducedAt`), und genau das übernähme das zweite Kind. Belegt am Code: die Sperre ist
   ein **Vorbild**, keine Repo-weite Konvention. Fundstelle: `position_has_data` an der Position
   (`PlanPositionsController.cs:409-411`, Fehlercode `ApiErrors.cs:205`, deutscher Satz `frontend/src/lib/api.ts:188`).
   Gegenfundstelle: `StudyPlansController.Delete` löscht einen ganzen Plan samt Spielstand ohne jede Sperre
   (`StudyPlansController.cs`, Methode `Delete`, Kommentar „Removes its positions via cascade incl. progress, practice
   sessions, test attempts and goal rewards“). Die Regel hier gilt also für das **Umhängen**, nicht für jede
   Plan-Änderung, und stützt sich auf den Besitz des Spielstands, nicht auf eine Konvention. Verworfen: den Spielstand
   beim Umhängen löschen (still zerstörend, der erste Sohn verlöre Tage) und ihn mitnehmen (das heutige Verhalten: das
   zweite Kind erbt Serie und Malus-Beginn). Kosten: Der Vater kann einen gespielten Plan nicht mehr dem Bruder geben; er
   legt für ihn einen neuen an. Ein bloß einmal geöffneter Plan ist nur gesperrt, wenn eine `PositionItemProgress`-Zeile
   entstand – nach dem Code erzeugt sie nur `ProgressForAsync` auf Spielwegen (`PositionPlayService.cs:633-646`). Ausgeschlossener Fall:
   „gib meinem Zweiten dasselbe Programm“ aus einem schon gespielten Plan. Gegen die User Story („mit einer leeren
   Vergangenheit beginnen“) ist das die richtige Lücke: Ein Umhängen kann nicht leer beginnen lassen, ohne dem ersten Kind
   etwas zu nehmen; das Kopieren eines Plans ist ein Wunsch für eine eigene Story, nicht dieser Defekt.
2. **Ein ungespielter Plan darf umgehängt werden; war er aktiv und bleibt er es, setzt der Wechsel `ActivatedAt` auf
   heute.** Begründung: Die bestehende Regel (B-613, `StudyPlanEntities.cs:90-97`, `ActiveOn` `PositionProgressService.cs:82`):
   Tage vor dem Einschalten zählen zugunsten des Kindes. Das Zielkind schaltet den Plan mit dem Wechsel faktisch ein;
   `StudyPlansController.Update` setzt `ActivatedAt = today` an genau dieser Stelle beim Einschalten (Zeile 220). Es wird
   schlicht `today` gesetzt, ohne `max` mit dem alten Wert: bei einem Plan mit künftigem Start steht `ActivatedAt = today`
   schon beim normalen Einschalten vor dem Start, und `ActiveOn` zusammen mit dem Start ergibt dasselbe. Ein inaktiver Plan
   bleibt unberührt (das Einschalten setzt es später); schaltet derselbe `PATCH` ihn gleichzeitig ein, gilt die
   bestehende Zeile 220. Der Wechsel sitzt **vor** dem Block für verschobene Starts (Zeilen 225-229), damit dieser auf dem neuen
   Stand rechnet. Verworfen: `ActivatedAt` bedingungslos beim Kindwechsel überschreiben (ein inaktiver Plan würde ein
   Datum tragen, das niemand je eingeschaltet hat). Kosten: ein weiterer Schreibweg für `ActivatedAt` neben Einschalten und
   Start-Verschieben. Ausgeschlossener Fall: ein Plan, den das Zielkind **rückwirkend** bespielen soll (Nachholen) –
   dafür gibt es Nachtrag durch den Vater, nicht den Wechsel; gegen die User Story („weder die Tage meines Bruders meine
   Serie verfälschen noch …“) trägt es, weil das Kind erst ab dem Zuweisungstag Pflichten hat.
3. **Keine neue Malus-Regel.** Entscheidung 2 wendet die vorhandene Fairness-Regel („was vor dem Einschalten lag, kann nicht
   verpasst worden sein“, `PlanDueForPeriod`, `PositionProgressService.cs:673-679`) auf einen weiteren Schreibweg an,
   Entscheidung 1 hält einen Plan dort, wo sein Spielstand liegt. Keine Münzhöhe, kein Takt, keine Abrechnung ändert
   sich; wer heute einen gespielten Plan umhängt, bekommt künftig `409` statt eines stillen Erbes. Begründet, weil der Lauf
   Malus- und Münz-Regeln nicht neu setzen darf. Zur Kenntnis für die Bestätigung: Die Sperre verbietet einen heute
   möglichen Vater-Weg.
4. **Das Vater-Web zeigt den Grund, es sperrt die Kind-Auswahl nicht vorab.** `plan_has_data` bekommt in
   `frontend/src/lib/api.ts` (neben Zeile 188) einen deutschen Satz, den die Plan-Bearbeitung über ihr
   `StatusBanner` (`VaterPlanDetail.tsx:30`, `:86`) zeigt – nach dem Muster der Positions-Sperre. Das Feld `PlanResponse`
   bekommt **kein** Kennzeichen „gespielt“. Verworfen: ein Server-Kennzeichen wie `IsPlayable` (`StudyPlanDtos.cs:25-31`
   ist der Vorläufer: der Server trägt die Regel, der Client rechnet sie nicht nach) mit abgeschalteter Auswahl. Kosten:
   Der Vater erfährt es erst beim Speichern, und das Formular hat keinen Weg, die Auswahl vorher zu erklären.
   Ausgeschlossener Fall: ein Vater, der die Kind-Auswahl schon vorher gesperrt sehen wollte; das Umhängen kommt selten vor,
   und ein Vertragsfeld für einen Fehlerfall wäre mehr als der Defekt braucht. Wird es zum Ärger, ist das Kennzeichen eine
   additive Folgestory.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. Ein Plan mit einer `PracticeSession` – in zwei weiteren Fällen mit einem `TestAttempt`, mit einer
   `PositionItemProgress`-Zeile –: `PATCH` mit anderem `childId` liefert `409` `plan_has_data`; der Plan bleibt beim ersten
   Kind, Serie und Konto beider Kinder bleiben unverändert. Rot heute (200, der Besitzer wechselt); rot, wenn die Sperre nur
   eine der drei Tabellen prüft (jeder Fall fällt einzeln).
2. Der `409` ändert nichts: ein `PATCH` mit neuem Titel **und** anderem `childId` auf einen gespielten Plan lässt den Titel
   unverändert. Rot, wenn die Sperre erst nach `SaveChangesAsync` greift.
3. Aktiver, ungespielter Plan, der seit vor drei Tagen läuft; das Zielkind hat diese Tage mit einem anderen Plan (anderes
   Fach) erfüllt: nach dem Umhängen reißt seine Serie nicht (`ChildStreakAsync` ohne Lücke an den drei Vortagen),
   `ActivatedAt` steht auf dem Tag des Wechsels. Rot heute (der Plan hat dort offene Pflichten); rot, wenn
   `ActivatedAt` nicht gesetzt wird.
4. Ein inaktiver, ungespielter Plan wird umgehängt: `ActivatedAt` bleibt (der Test liest den Wert aus der Datenbank, die
   API zeigt ihn nicht). Rot, wenn der Kindwechsel `ActivatedAt` bedingungslos überschreibt.
5. Ein `PATCH` ohne `childId`, oder mit dem gleichen `childId`, auf einen **gespielten** Plan (nur Titel) liefert `200` und
   ändert `ActivatedAt` nicht. Rot, wenn die Prüfung vor dem `newChildId != plan.ChildId`-Zweig (`StudyPlansController.cs:183`)
   greift.
6. `Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab` (`PlanSubjectTests.cs:262`) bleibt grün. Rot, wenn die Sperre die
   Reihenfolge von Speichern und `DeactivateSameSubjectPlansAsync` verändert.
7. Das Vater-Web übersetzt `plan_has_data` in einen deutschen Satz (Muster `errorMessage.test.ts:72-80`, `exercise_in_use`).
   Rot, wenn der Code kein Mapping hat: der Vater sähe den englischen Server-Text.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet), am Code nachgesehen.*

**Größe: M** — wie der Batch-Pfad im `MediaSelector` (B-03): ein Eingriff im `StudyPlansController.Update`, ein neuer
Fehlercode samt Nachziehern und ein gutes Dutzend Testfälle über zwei Klassen. Nicht S, weil die Sperre drei Tabellen
prüft und `ActivatedAt` mit der B-613-Logik zusammenspielen muss.

- **`wo: beides`** — Backend zuerst (`StudyPlansController`, `ApiErrors`); das Frontend ist nur der deutsche Satz in
  `frontend/src/lib/api.ts` (`GERMAN_PROBLEM_TEXT`) samt Test in `errorMessage.test.ts`.
- **`migration: nein`** — nachgesehen: `ActivatedAt` und `ChildId` sind vorhandene Spalten, die Sperre liest drei vorhandene
  Tabellen (`PracticeSessions`, `TestAttempts`, `PositionItemProgress`). Kein Modell-Drift, keine Migration.
- **`vertragsbruch: nein`** — nachgesehen: `UpdatePlanDto` und `PlanResponse` in `Pugling.Contracts` bleiben unberührt
  (Entscheidung 4: kein Kennzeichen „gespielt"). Der Fehlercode lebt in `Pugling.Api/Errors/ApiErrors.cs`, nicht im Vertrag.
  Der Aufruf `PATCH` bekommt eine neue `409`-Antwort; das ändert das eingecheckte `docs/openapi/v1.json` (`[ProducesResponseType(409)]`
  an `Update`, neuer Wert in der Code-Aufzählung – `ErrorCodeTests` prüft sie gegen das Register). Das gehört in denselben
  Commit, `ContractDocumentTests` schreibt die Datei bei jedem Lauf.

### Risiken

1. **Das Tor `ProblemTextGuardTests` koppelt Backend und Frontend:** jeder 409-Code braucht einen deutschen Text in
   `frontend/src/lib/api.ts` (`GERMAN_PROBLEM_TEXT`) oder eine Ausnahme
   (`backend/Pugling.Api.Tests/ProblemTextGuardTests.cs:39`). „Backend zuerst" heißt hier: Code, Text und Mapping-Test in
   **einem** Schritt, sonst steht die Suite zwischendurch rot.
2. **Reihenfolge im Controller:** die Sperre muss im Zweig `dto.ChildId is { } newChildId && newChildId != plan.ChildId`
   greifen (`StudyPlansController.cs:183`), **vor** `plan.ChildId = newChildId` und vor jedem `SaveChangesAsync`; sonst
   würde ein halbes `PATCH` gespeichert (AK 2). Die Prüfung `SupervisorOwnsChildAsync` (404) kommt davor, damit
   ein fremdes Kind keinen Hinweis auf den Spielstand bekommt.
3. **`ActivatedAt` bei gleichzeitigem Ausschalten:** `PATCH {childId, active:false}` auf einen aktiven Plan – „war er aktiv und
   bleibt er es" (Entscheidung 2) verlangt, `ActivatedAt` nur zu setzen, wenn der Plan nach dem `PATCH` aktiv ist; sonst setzt der
   Wechsel ein Datum, das niemand je eingeschaltet hat. Der Kombinationsfall (`active:true` zusammen mit `childId` auf einen
   inaktiven Plan) läuft über die bestehende Zeile 220 und braucht einen eigenen Fall.
4. **`DeactivateSameSubjectPlansAsync` bleibt der Nachlauf** (`:232`): die Reihenfolge von Speichern und Abschalten darf sich nicht
   ändern; `Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab` (`PlanSubjectTests.cs:262`) ist das Netz (AK 6).
5. **Das Seeden eines „gespielten" Plans:** Üben und Test laufen über HTTP, `PositionItemProgress` allein (AK 1, dritter Fall)
   nur über die Datenbank. `TestApi.SeedLeitnerPosition` legt solche Zeilen an (`TestApi.cs:291-295`); ob das für „ohne
   Sitzung" reicht, ist beim Bau zu prüfen.
6. **Verbotener, heute möglicher Vater-Weg:** wer einen gespielten Plan umhing, bekommt `409` statt eines stillen Erbes
   (Entscheidung 3, zur Bestätigung vorgemerkt). E2E-Specs, die den Kindwechsel fahren
   (`e2e/plan-kinderliste-*.spec.ts` lesen nur die Auswahl), sind nicht betroffen; ein Aufrufer in `docs/tutorial-supervisor.md`
   oder `docs/REST/` ist nicht nachgesehen.

### Angriffsplan

1. Backend, Tests zuerst: AK 1, 2, 5 und AK 3, 4 (rot), AK 6 (grün) schreiben.
2. `ApiErrors.PlanHasData` (`plan_has_data`, 409, nach dem Muster von `PositionHasData`, `ApiErrors.cs:205`);
   `StudyPlansController.Update`: Sperre im Kindwechsel-Zweig, `ActivatedAt = heute` bei aktivem Plan nach dem Zweig und vor
   dem Block für verschobene Starts; `[ProducesResponseType(409)]`.
3. Frontend im selben Schritt: Eintrag `plan_has_data` in `GERMAN_PROBLEM_TEXT` (`api.ts`, neben `position_has_data`) und der
   Fall in `errorMessage.test.ts` (AK 7).
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`; der Lauf schreibt `docs/openapi/v1.json` und die
   `docs/api-examples` neu, beides in den Commit.

### Testweg

- **AK 1, 2, 5 (Regressionstest, vorher rot):** `backend/Pugling.Api.Tests/PlanSubjectTests.cs` (Hilfen `SetupAsync`,
  `PlanAsync`, Zweitkind wie in `:262`) oder eine neue Klasse `PlanUmhaengenTests` für die Sperre; je Tabelle ein Fall
  (`PracticeSession`, `TestAttempt`, `PositionItemProgress`). Rot macht sie: heute `200` und neuer Besitzer; die Sperre
  nur auf eine Tabelle; die Prüfung außerhalb des `newChildId != plan.ChildId`-Zweigs.
- **AK 3, 4:** `backend/Pugling.Api.Tests/RueckwirkendePflichtTests.cs` – dort liegen `CreateBackdatedPlanAsync`,
  `Read(db => …)` für `ActivatedAt`, `StreakAsync` und `factory.Clock`; Vorbild `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`
  (`:459`). Rot macht sie: `ActivatedAt` bleibt der alte Wert (AK 3) bzw. wird bedingungslos überschrieben (AK 4). Hinweis:
  die Streak-Methode heißt im Code `ChildStreakAsync`/`StreakBoundedAsync` (`PositionProgressService.cs:479`), nicht nur
  `ChildStreakAsync`.
- **AK 6:** `PlanSubjectTests.Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab` bleibt grün.
- **AK 7:** `frontend/src/lib/errorMessage.test.ts` (Muster `exercise_in_use`); das Backend-Tor `ProblemTextGuardTests`
  ist der zweite Beleg.
- Vor der Abnahme: `pugling-reviewer`; Rollengang als Vater: gespielten Plan einem Bruder geben – deutscher Satz im
  `StatusBanner` (`VaterPlanDetail.tsx:86`).

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 87 (Hinweis 2), unverifiziert.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): am Code belegt; die Produktfrage ist beantwortet, das Umhängen ist
  ein ausgebauter Vater-Weg (Vertrag und Formular). Der Vorschlag `ActivatedAt = heute` allein genügt nicht; Empfehlung
  Sperre für gespielte Pläne plus `ActivatedAt` bei ungespielten.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 4. Gespielter Plan: Umhängen gesperrt (`409 plan_has_data`, drei Tabellen, mehr als die Positions-Sperre prüft); ungespielter aktiver Plan: `ActivatedAt = heute` beim Wechsel (B-613 auf einem weiteren Schreibweg); keine neue Malus-Regel. Die Sperre ist ein Vorbild (`position_has_data`), keine Repo-Konvention (`StudyPlansController.Delete` löscht ohne Sperre). Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, beides (Backend zuerst), keine Migration, kein Vertragsbruch (`docs/openapi/v1.json` ändert sich um eine 409-Antwort); `ProblemTextGuardTests` koppelt Backend und Frontend-Satz in einen Schritt.
- **2026-11-19** — Entscheidung 1 (Sperre `409 plan_has_data` für gespielte Pläne) vom Nutzer bestätigt.
- **2026-11-21** — in-arbeit (Nachtlauf 2026-11-21, Sprint 105). Gebaut nach Entscheidungen 1–4:
  `StudyPlansController.Update` prüft im Zweig „anderes Kind“ nach dem 404 und vor jeder Zuweisung
  `PlanHasPlayDataAsync` (Übungssitzung, Testversuch, Fortschrittszeile am Plan) → `409 plan_has_data` (neu in
  `ApiErrors`); sonst `ActivatedAt = heute`, wenn der Plan nach dem PATCH aktiv ist. Deutscher Satz in
  `frontend/src/lib/api.ts`, nennt die Sperre und keinen Ausweg (es gibt keinen, der dem ersten Kind nichts nimmt).
  **Rote Proben** (vor dem Fix): AK 1 drei Fälle und AK 2 erwartet 409, gemessen 200; AK 3 erwartet Serie 4, gemessen 1.
  Je AK-Fall Test und Mutante (`RueckwirkendePflichtTests`):
  - AK 1 → `Gespielter_Plan_Laesst_Sich_Nicht_Umhaengen` („practice“/„test“/„introduced“); Q1/Q2/Q3 (je eine Tabelle
    weg) → je genau ihr Fall rot. Das „Konto beider Kinder“ des AK prüft der Test nicht: ohne Abrechnung entstünde dort
    kein Malus; die Serie des Zielkinds (4) trägt die Zusicherung.
  - AK 2 → `Abgewiesenes_Umhaengen_Speichert_Nichts`. **Ungemessen:** die Reihenfolge „Sperre vor dem Speichern“ ist
    keine Einzeilen-Mutante; eine Sperre nach dem Titel, aber vor dem Speichern, wäre harmlos (Kommentar am Test).
  - AK 3 → `Umgehaengter_Plan_Bricht_Die_Serie_Des_Neuen_Kindes_Nicht`; Q5 (`ActivatedAt` weg) → rot.
  - AK 4 und Risiko 3 → `Umgehaengter_Inaktiver_Plan_Behaelt_Sein_Einschaltdatum` (Datenzusicherung); Q6 (ohne
    `plan.Active`) → rot.
  - AK 5 → `Gleiches_Kind_Ist_Kein_Umhaengen`; Q7 (Sperre auch bei gleichem Kind) → rot.
  - AK 6 → `PlanSubjectTests.Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab` grün.
  - AK 7 → `errorMessage.test.ts` „übersetzt die Sperre beim Umhängen eines gespielten Plans“; Q8 (Eintrag fehlt) → rot.
  - Kombinationsfall `{childId, active:true}` auf einen inaktiven Plan: ohne eigenen Fall – beide Zeilen setzen heute,
    ein Test fiele nur, wenn beide fehlten (Review-Hinweis 🟢5).

  Gemessen nach der letzten Änderung an `72103ab9 + Arbeitsbaum geändert` mit `mutant-probe.py` (Debug), Grundlinie
  danach grün:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Q1 | `StudyPlansController.cs:176` ohne `PracticeSessions` | red | red |
  | Q2 | `:177` ohne `TestAttempts` | red | red |
  | Q3 | `:178` ohne `PositionItemProgress` | red | red |
  | Q5 | `:242` `ActivatedAt` nicht gesetzt | red | red |
  | Q6 | `:242` ohne `plan.Active` | red | red |
  | Q7 | `:195` Sperre auch bei gleichem Kind | red | red |
  | Q8 | `frontend/src/lib/api.ts:190` Eintrag fehlt | red | red |
  | Q0e | `:200` Sperre ganz weg, E2E `plan-umhaengen-gesperrt` | red | red |

  Eine erste Gang-Probe Q2e (nur `TestAttempts` weg) war **kein Beleg**: von Hand gefahren blieb der Gang grün, weil
  der Test-Submit auch Fortschrittszeilen schreibt und Q3 weiter sperrt. Ersetzt durch Q0e.

  Review (`pugling-reviewer`): kein 🔴. 🟡 1: der neue Helfer stand zwischen der Doku von
  `DeactivateSameSubjectPlansAsync` und deren Rumpf → verschoben, eigene Doku (Code, **Zähler 1 von 5**, vorsichtig
  gezählt). 🟡 2: der AK-2-Kommentar behauptete eine rote Probe „nach dem Titel“, die keine ist → berichtigt (ohne
  Zählung). 🟢 3/4: englischer `detail` ohne „by its child“ (auch ein Vater-Versuch sperrt, und das ist richtig: ein
  bestandener zählt fürs Kind); deutscher Satz „gehört dem Kind, das damit gelernt hat“ statt mehrdeutig „diesem Kind“.
  Bestätigt: „gespielt“ ist vollständig – alle anderen kindgebundenen Zeilen am Plan setzen eine der drei Tabellen
  voraus.
- **2026-11-21** — abgenommen in Commit `1ac42f4a` (Sprint 105). Backend 1985/1985 (Release), Vitest 1302/1302.
  **Rollengang Vater:** neue Spec `plan-umhaengen-gesperrt` gegen die frisch gestartete App (5201/5174): der Vater wählt
  auf der Plan-Seite des Bruders das jüngere Kind, speichert und liest den deutschen Satz; der Plan bleibt beim Bruder.
  Mit Q0e rot. Dazu `full-flow` und `plan-kinderliste-laedt-noch` grün – 3 passed.
- **2026-11-21** — vom Nutzer bestätigt (Chat nach dem Nachtlauf, „Ja, so lassen“): die stellvertretenden Entscheidungen 1–4, insbesondere Entscheidung 1 (ein gespielter Plan lässt sich nicht mehr umhängen).
