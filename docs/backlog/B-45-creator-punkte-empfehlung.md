---
tags: [typ/story, status/in-arbeit, bereich/katalog, bereich/training, rolle/creator, rolle/supervisor]
aliases: [Punkte-Empfehlung, RewardPoints]
status: in-arbeit
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
wartet_auf: pugling-reviewer + frontend-reviewer
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
---

# B-45 · Die Punkte-Empfehlung des Creators soll der Supervisor übernehmen können

`Exercise.RewardPoints` ([Models/LearnEntities.cs](../../backend/Pugling.Api/Models/LearnEntities.cs))
wird über die Creator-API geschrieben und gelesen, ist im Create/Update-DTO **Pflicht** — und **kein
einziger Bewertungspfad liest es**. Gepunktet wird ausschließlich über die `PlanPosition`
(`PointsGoalMet`, `NewContentPoints`, Combo/Speed, `PenaltyCoins`). Ein Creator, der dort 15 einträgt,
bewirkt heute nichts und bekommt darüber auch keine Rückmeldung.

## User Story

Als **Supervisor** möchte ich beim Anlegen einer Position die Punkte-Empfehlung des Creators als
Vorschlag sehen und übernehmen können, damit das Feld, das der Creator sorgfältig befüllt hat, nicht
folgenlos verpufft und ich beim Anlegen nicht jedes Mal neu raten muss, wie viele Punkte angemessen sind.

## Ist-Stand am Code

- **`Exercise.RewardPoints` existiert und ist Pflicht.** Modell:
  `backend/Pugling.Api/Models/LearnEntities.cs:66` (`public int RewardPoints { get; set; }`, Kommentar
  „Points the child receives for completing it" — das stimmt für keinen heutigen Pfad). Vertrag:
  `backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:12` (`ExercisePayload<TConfig>` trägt
  `int RewardPoints` **ohne** Default-Wert, also nicht-nullable/Pflicht) und `:25` (im Response-DTO
  ebenfalls `int RewardPoints`, nicht nullable). Controller übernimmt es unverändert, ohne Prüfung auf
  einen sinnvollen Mindestwert: `backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:246`
  (Create) und `:314` (Update).
- **Kein Bewertungspfad liest `Exercise.RewardPoints`.** `ScoringService.ScoreConfig`
  (`backend/Pugling.Api/Services/Shared/ScoringService.cs:48-49`) nimmt `NewContentPoints`,
  `ComboThreshold`, `ComboBonusPoints`, `SpeedThresholdSeconds`, `SpeedBonusPoints` — laut
  Klassenkommentar (`:40-41`) ausdrücklich **„comes from the `PlanPosition`"**, nicht von der `Exercise`.
  Eine gezielte Suche nach `RewardPoints` in `backend/Pugling.Api/Services/` findet nur
  `Mission.RewardPoints`/`Achievement.RewardPoints` in `GamificationService.cs` (Zeilen 28, 36, 42, 47, 56,
  57, 62, 67, 97, 125) — das sind andere Entities (`GamificationEntities.cs:26` und `:73`), keine Vokabel
  für dieselbe Sache.
- **Beim Anlegen einer Position wird `Exercise.SuggestedBonus`, aber nicht `Exercise.RewardPoints`
  gelesen.** `PlanPositionsController.Create`
  (`backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:130-161`) holt `sb =
  exercise.SuggestedBonus` und vererbt daraus `NewContentPoints`/`ComboThreshold`/`ComboBonusPoints`/
  `SpeedThresholdSeconds`/`SpeedBonusPoints` (`:153-157`, Muster „position override → exercise
  suggestion → model default"). `PointsGoalMet` dagegen fällt bei fehlender Angabe hart auf `20`
  (`:150`, `dto.PointsGoalMet ?? 20`) — **ohne** einen Blick auf `exercise.RewardPoints`.
- **Dasselbe Muster fehlt im Vater-Web.** `frontend/src/vater/PlanPositions.tsx:98-104`
  (`defaultSettings`) liest `ex?.defaultItemCount`, `ex?.defaultUseLeitner`,
  `ex?.defaultRequireTypedTest` aus der Übung, um das Anlegeformular vorzubelegen — aber `pointsGoalMet`
  bekommt an derselben Stelle (`:101`) den hartcodierten Literal-Wert `20`, nicht `ex?.rewardPoints`. Der
  Bruch ist also serverseitig **und** clientseitig an derselben Stelle: dem hartcodierten `20` statt einem
  Blick auf die Übung.
- **Das Hybrid-Muster („Position erbt, solange sie nicht selbst überschreibt") ist im Code bereits
  etabliert** — nur eben für andere Felder: `DefaultStage`/`DefaultItemCount`/`DefaultUseLeitner`/
  `DefaultRequireTypedTest` (`LearnEntities.cs:75-81`) und `SuggestedBonus`
  (`backend/Pugling.Contracts/Common/LearnBaseTypes.cs:34-39`, Kommentar `:28-32`: „copied ONCE… later
  changes to the exercise therefore do NOT retroactively affect existing child plans"). `RewardPoints`
  ist der einzige Punkte-nahe Wert an der `Exercise`, der diesem Muster **nicht** folgt.

## Die echte Lücke

Die Notiz vermutete ein fehlendes zweites Ende des Feldes; der Beleg zeigt genau das, nur schmaler als
gedacht: Es fehlt **eine einzige Verdrahtung** — `exercise.RewardPoints` als Fallback für
`PlanPosition.PointsGoalMet` an exakt der Stelle, an der `SuggestedBonus` schon als Fallback für die
Combo-/Speed-/NewContent-Felder dient (Backend) und an exakt der Stelle, an der `ex?.defaultItemCount`
schon das Formular vorbelegt (Frontend). Kein neues Feld, kein neuer Endpunkt, keine Ebenen-Verschiebung —
das Muster existiert zweimal im Code, nur an `RewardPoints` vorbei.

## Offene Punkte

~~Welche Positions-Punktgröße meint die Empfehlung (Zielbelohnung, Basispunkte je neuem Inhalt oder
beides)?~~ → siehe Entscheidung 1.

~~Wird sie still vererbt (wie `DefaultStage`) oder erscheint sie im Vater-Web als sichtbarer Vorschlag mit
„übernehmen"-Knopf?~~ → siehe Entscheidung 2.

~~Wechselt `RewardPoints` dabei von Pflicht auf optional (Vertragsänderung)?~~ → siehe Entscheidung 3.

~~Was passiert, wenn `RewardPoints` 0 ist (heute unvalidiert möglich) — übernimmt die Position dann einen
0-Punkte-Default statt der bisherigen 20?~~ → siehe Entscheidung 4.

## Entscheidungen

1. **Die Empfehlung meint `PlanPosition.PointsGoalMet` (Zielbelohnung), nicht `NewContentPoints`.**
   Begründung: Der Modellkommentar an `Exercise.RewardPoints` sagt wörtlich „points the child receives
   for completing it" — das ist die Semantik von `PointsGoalMet` (Belohnung fürs erreichte Pflichtziel
   der Position), nicht die von `NewContentPoints` (Punkte je Review-Ereignis, schon über
   `SuggestedBonus` abgedeckt). Kosten: keine neue Empfehlungsquelle für Combo/Speed/NewContent nötig —
   die bleiben unverändert bei `SuggestedBonus`; es entsteht kein Konflikt zwischen zwei
   Empfehlungsfeldern für dasselbe Ziel.
2. **Stille Vererbung nach dem etablierten Hybrid-Muster, kein eigener „übernehmen"-Knopf.**
   Begründung: Backend und Frontend haben das Muster für `DefaultStage`/`DefaultItemCount`/
   `SuggestedBonus` bereits gebaut (`PlanPositionsController.cs:144-157`,
   `PlanPositions.tsx:98-104`) — ein Sonder-UI nur für `RewardPoints` wäre eine zweite Fassung derselben
   Regel. Backend: `dto.PointsGoalMet ?? (exercise.RewardPoints > 0 ? exercise.RewardPoints : 20)` in
   `PlanPositionsController.Create`. Frontend: `pointsGoalMet: ex?.rewardPoints && ex.rewardPoints > 0 ?
   ex.rewardPoints : 20` in `defaultSettings()` — das Formularfeld bleibt danach normal überschreibbar,
   wie bei `itemCount`/`useLeitner` schon heute. Kosten: ein Frontend-Zeile-Change plus eine
   Backend-Zeile-Change; kein neues DTO-Feld, keine Vertragsänderung.
3. **`RewardPoints` bleibt Pflichtfeld am Exercise-DTO.** Begründung: Es dient jetzt als
   Empfehlungsquelle und soll darum immer einen Wert tragen; Nullable machen bräche den Vertrag
   (`Pugling.Client`, Frontend, `unknown_field`-Guards ziehen nach) ohne Nutzen — Seed und Creator-UI
   verlangen den Wert ohnehin schon heute. Kosten: keine (Status quo bleibt).
4. **Ein `RewardPoints` von 0 überschreibt den Modell-Default `20` nicht.** Begründung: `RewardPoints`
   trägt heute keine serverseitige Untergrenze (`ExerciseControllerBase.cs:246`/`:314` validieren nur
   `Title`, nicht `RewardPoints`); eine unbedacht auf `0` gelassene alte Übung darf nicht rückwirkend
   jede neu angelegte Position auf 0 Zielpunkte ziehen — das wäre eine stille Verschlechterung
   gegenüber dem heutigen Verhalten (hartes `20`). Kosten: eine zusätzliche `> 0`-Prüfung an der
   Vererbungsstelle (Backend und Frontend je eine Zeile, siehe Entscheidung 2); keine Migration, da
   `RewardPoints` nicht angefasst wird.
5. **Wie `SuggestedBonus` wird nur beim Anlegen der Position kopiert, nicht live nachgezogen.**
   Begründung: Konsistent mit dem dokumentierten Verhalten von `SuggestedBonus`
   (`LearnBaseTypes.cs:28-32`, „copied ONCE… later changes to the exercise therefore do NOT
   retroactively affect existing child plans") — ein Creator, der `RewardPoints` nachträglich ändert,
   darf keine bereits laufenden Kind-Pläne verschieben. Kosten: keine zusätzliche Logik, das Verhalten
   ergibt sich automatisch aus „nur bei `dto.PointsGoalMet is null` greift der Fallback, sonst nie".

## Akzeptanzkriterien

1. `POST .../study-plans/{planId}/positions` ohne `pointsGoalMet` übernimmt `exercise.RewardPoints` als
   `PointsGoalMet`, sofern `RewardPoints > 0`; bei `RewardPoints == 0` bleibt der Modell-Default `20`.
2. Wird `pointsGoalMet` in der Anfrage explizit gesetzt, gewinnt dieser Wert unverändert (kein Bruch der
   bestehenden „Position override zuerst"-Reihenfolge).
3. Eine spätere Änderung von `Exercise.RewardPoints` verändert `PointsGoalMet` bereits angelegter
   Positionen **nicht** rückwirkend.
4. Das Vater-Web belegt das Punkte-Feld beim Anlegen einer neuen Position mit dem `RewardPoints`-Wert der
   gewählten Übung vor (bzw. `20`, wenn die Übung `0` trägt) und lässt es weiterhin frei überschreiben.
5. `Exercise.RewardPoints` bleibt im Create/Update-DTO ein Pflichtfeld; kein Contracts-Schema ändert
   sich.

## Schätzung

**Größe: S** — Anker B-01 (`childId` aus dem Test-Pfad ziehen): eine Fallback-Zeile im Controller, eine
Fallback-Zeile im Frontend-Default, ein Integrationstest. Kein neues DTO, kein neuer Endpunkt.

- **`wo`: beides** — Backend zuerst (`PlanPositionsController.Create`), danach die Frontend-Vorbelegung
  in `PlanPositions.tsx:defaultSettings` (API-First: das Frontend hängt an der bereits vorhandenen
  `rewardPoints`-Angabe im `ExerciseSummary`, kein neuer Contracts-Roundtrip nötig).
- **`migration`: nein** — keine Schema-Änderung, `RewardPoints` existiert bereits genau wie gebraucht.
- **`vertragsbruch`: nein** — kein DTO ändert Form oder Pflichtstatus; nur interne Vererbungslogik.
- **Risiken**: der fehlende serverseitige Mindestwert an `RewardPoints` (Entscheidung 4) bleibt bestehen
  — ein Creator kann weiterhin `0` oder theoretisch negative Werte eintragen. Das wird hier bewusst
  **nicht** mitgefangen (wäre ein eigener, unabhängiger Defekt/Frage-Kandidat, kein Teil dieser Story);
  die `> 0`-Prüfung an der Vererbungsstelle reicht, um die stille Verschlechterung zu verhindern.
- **Angriffsplan**: (1) `PlanPositionsController.Create` um den `RewardPoints`-Fallback für
  `PointsGoalMet` ergänzen (Backend zuerst, API-First); (2) `PlanPositions.tsx:defaultSettings` denselben
  Fallback fürs Anlegeformular nachziehen; (3) Testweg unten.
- **Testweg**: zwei neue Fälle in `backend/Pugling.Api.Tests/PlanPositionCrudTests.cs` (die bestehende
  Integrationstest-Klasse für `PlanPositionsController.Create`/`Update`): Übung mit `RewardPoints = 15`
  anlegen, Position ohne `pointsGoalMet` erzeugen, `PointsGoalMet == 15` erwarten; zweiter Fall mit
  `RewardPoints = 0` erwartet weiterhin `20`. Frontend-seitig per `/smoke-test`-Sichtprüfung des
  Anlegeformulars (Punkte-Feld zeigt den Übungswert vorbelegt).

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung; das tote Feld ist belegt, die
  Ausgestaltung nicht).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`RewardPoints` Pflicht ohne Leser;
  `PlanPositionsController`/`ScoringService` lesen für Punkte nur `PlanPosition`-Felder bzw.
  `SuggestedBonus`, nie `Exercise.RewardPoints`; dieselbe Lücke besteht identisch im Vater-Web-Formular).
  Vier offene Punkte formuliert.
- **2026-08-03** — gegrillt: alle vier offenen Punkte in nummerierte Entscheidungen überführt (Ziel =
  `PointsGoalMet`, stille Vererbung nach dem etablierten Hybrid-Muster, `RewardPoints` bleibt Pflicht,
  `0` überschreibt den Default nicht, Kopie nur bei Anlage wie `SuggestedBonus`) — autonom getroffen,
  Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe S, `wo: beides` (Backend zuerst), `migration: nein`,
  `vertragsbruch: nein`, Testweg über einen Integrationstest an `PlanPositionsController.Create`
  benannt — autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-09-01** — `geschaetzt → in-arbeit`, **gebaut**. Rote Probe zuerst: die zwei Vererbungs-Fälle in
  `PlanPositionCrudTests` waren rot (`Expected 15, Actual 20`), danach grün. Der 0-Fall war vorher grün und
  ist darum per Mutation belegt: ohne den `> 0`-Wächter fällt er (`Expected 20, Actual 0`) — dasselbe
  gefahren für die Frontend-Hälfte.

  **Vier Funde, die die Story korrigieren:**

  1. **Die Schätzung war an einem Punkt falsch.** Sie behauptete, das Frontend hänge „an der bereits
     vorhandenen `rewardPoints`-Angabe im `ExerciseSummary`" — das Feld stand dort **nicht**
     (`ExerciseCatalogDtos.cs:13-16`, nur `ExerciseDetail` trug es). Additiv ergänzt, `vertragsbruch: nein`
     gilt also weiter; aber `Pugling.Contracts` änderte sich, also liefen `dotnet build Pugling.sln`, die
     Neu-Erzeugung von `docs/openapi/v1.json` durch den Testlauf und `npm run gen:contract` mit. Damit
     liegt die Größe faktisch an der **oberen Kante von S**; das Feld bleibt bei `S`, weil die Schätzung
     in gutem Glauben entstand und ihre Korrektur hier steht.
  2. **AK 4 hing nicht dort, wo die Story es vermutete.** `defaultSettings()` wird **ohne** Argument
     aufgerufen (`PlanPositions.tsx:387`) — die Übung ist erst nach dem Radio-Klick bekannt. Die
     Vorbelegung musste also in den `onChange` der Trefferliste, und dessen Kommentar verlangte
     ausdrücklich, Punkte beim Übungswechsel **stehen zu lassen**. Entscheidung 2 nennt aber `itemCount`
     als Vorbild, und das wird dort überschrieben — also der neueren, ausdrücklichen Entscheidung gefolgt
     und der Kommentar nachgezogen. Der Malus bleibt außen vor: für ihn gibt es keine Empfehlung. Das ist
     eine **Verhaltensänderung**, die die Story nicht ausbuchstabiert hatte.
  3. **Der Hilfetext des Feldes war falsch — und zwar schon vorher.** `fieldHelp.ts:110-114` sagte
     „Grundwert einer richtigen Antwort in dieser Übung … Boni (Combo, schnelle Antwort)". Das ist die
     Semantik von `newContentPoints`, die aus `SuggestedBonus` kommt; dieses Feld hat es nie getan. Neu
     formuliert als das, was es jetzt ist.
  4. **Der Testhelfer bekam eine Überladung, keine vierte Fassung.** `CreateVocabRefExerciseAsync` nagelte
     `rewardPoints = 10` fest; `params string[]` verbietet einen optionalen Parameter dahinter. Statt einer
     Kopie der Reihen/Unit-Vorbereitung (die Lehre aus B-179) delegiert die alte Signatur an eine neue mit
     explizitem Wert.

  **Eine Abweichung vom Testweg, nach oben:** Die Schätzung sah frontendseitig eine
  `/smoke-test`-Sichtprüfung vor. Stattdessen `e2e/punkte-empfehlung.spec.ts` — wie bei B-172 wird der
  Rollengang damit dauerhaft statt einmalig. Sie ist belegt **diskriminierend**: entfernt man die
  Vorbelegung im `onChange`, bleiben die 18 Vitest-Fälle grün und **nur** die E2E fällt (`Received: "20"`).
  Beim Schreiben fiel dabei ein Strict-Mode-Verstoß auf (der Kindname steht auch in der „Heute"-Tabelle) —
  gelöst über die Kinder-Sektion, nicht über einen Index.

  **Sichtbarer Nebeneffekt:** `docs/api-examples/study-plans.md` zeigt jetzt `pointsGoalMet: 10` statt
  `20` — die Übung der Test-Fixture trägt 10, die Position erbt es. Die Wirkung steht damit im
  mitgeschriebenen Beispiel.

  **Belegt:** Backend **834/834** (`-c Release`, +3), Vitest **307/307** (+3), Playwright **38/38** (+1),
  `npm run build` grün, `dotnet format Pugling.sln --verify-no-changes` sauber.

  **Bleibt auf `in-arbeit`:** `wo: beides` verlangt **beide** Reviewer, und diese Sitzung startet keine
  Agenten unaufgefordert. Mein eigener Blick auf den Diff ist kein Review.
