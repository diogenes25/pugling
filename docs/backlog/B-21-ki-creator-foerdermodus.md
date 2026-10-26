---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/auswertung, rolle/creator]
aliases: [Fördermodus, weakness, --use-weak]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/ki-creator-agent.md
wartet_auf: ""
nachgeschaut: "2026-10-26"
---

# B-21 · KI-Creator: Fördermodus (`--use-weak`) sprachrein und mit Tests absichern

Schwache Wörter eines Kindes gruppieren und daraus gezielt Übungen bauen — das war die Idee. Die
Recherche zeigt: **der Mechanismus existiert bereits**, seit dem allerersten Commit des Agenten, als
Option `--use-weak` statt als eigener `--mode`. Was fehlt, ist schmaler: der Wort-Rollup kennt kein
Sprachpaar-Filter (ein Kind mit zwei Sprachen könnte fachfremde Wörter in den Pflicht-Wortschatz gezogen
bekommen), und kein einziger Test führt die Pipeline mit gesetztem `--use-weak` je aus.

## User Story

Als Vater, der den KI-Creator-Agenten für ein Kind mit **mehreren** gelernten Sprachen einsetzt, möchte
ich, dass `--use-weak` nur Wörter der **Zielsprache der Übung** heranzieht und dass dieser Weg durch einen
Test abgesichert ist, damit eine Förder-Übung nicht versehentlich fachfremden Wortschatz als
„unveränderlich" vorschreibt.

## Ist-Stand am Code

**Der Fördermodus ist bereits vollständig verdrahtet** — keine Idee, sondern gebauter Code seit Commit
`72dc543` (git log -S "UseWeakWords" -- backend/Pugling.Agent.Creator):

- CLI: `--use-weak` (`backend/Pugling.Agent.Creator/AgentCommands.cs:244,360`,
  `backend/Pugling.Agent.Creator/README.md:150`) setzt `GenerationRequest.UseWeakWords`
  (`backend/Pugling.Agent.Creator/GenerationRequest.cs:34,51`). Es gibt **kein** `--mode` (repo-weite Suche
  ohne Treffer außer in dieser Story).
- `BriefingBuilder.LoadChildAsync` lädt bei gesetztem Flag die schwächsten Wörter über
  `student.ListWordMasteryAsync(childId, onlyWeak: true, take: MaxWeakWords, ct: ct)`
  (`backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:151-153`) von
  `GET api/v1/student/children/{childId}/vocabulary-progress/by-word`
  (`backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:66-100`) — Rollup von
  `ItemProgress` je `VocabularyId`, mit `AvgMasteryPercent`/`CorrectPercent`
  (`backend/Pugling.Contracts/Student/ProgressDtos.cs:23-25`).
- Die Wörter werden **doppelt** verwendet: als Fallback für den unveränderlichen `RequiredWords`-Pflicht-
  Wortschatz, wenn `--words` fehlt (`BriefingBuilder.cs:38-40`), und zusätzlich als eigener Prompt-Abschnitt
  „Schwach beherrschte Wörter" (`backend/Pugling.Agent.Creator/Briefing/CreatorBriefing.cs:116-121`).
- **Zugriff/Rollentrennung ist bereits korrekt geklärt** (die „ungeprüfte" Frage der Idee-Stufe):
  `AuthAccess.OwnsChildAsync` (`backend/Pugling.Api/Auth/AuthAccess.cs:122-126`) lässt nur `IsStudent()`
  (eigenes Kind) oder `IsSupervisor()` (verlinktes Kind) zu — eine reine Creator-Rolle scheitert mit
  403/404. Der Agent kommt bei `--child` nur an die Daten, weil dasselbe Adult-Konto laut Produktregel
  (root-`CLAUDE.md`: „ein Vater ist zugleich Creator+Supervisor") ohnehin beide Rollen trägt
  (`AccountService.EnsureForAdultAsync`) — kein Sonderweg, dieselbe Doppelrolle wie im Vater-Web. Getestet
  in `backend/Pugling.Api.Tests/PuglingClientTests.cs:252-276` (Supervisor liest, fremdes Konto nicht).
  Ein reines Lehrer-Konto (nur Creator) kann laut `backend/Pugling.Agent.Creator/README.md:74-77` folgerichtig
  nur `--profile` ohne `--child` nutzen.
- **Lücke 1 — kein Test:** `backend/Pugling.Api.Tests/CreatorAgentTests.cs:64` setzt ausschließlich
  `UseWeakWords: false`. Der gesamte Mechanismus (Laden, Pflicht-Wortschatz, Prompt-Abschnitt) läuft bis
  heute **ohne** eine einzige Pipeline-Assertion.
- **Lücke 2 — kein Sprachfilter:** `ByWord` (`ChildVocabularyProgressController.cs:66-100`) gruppiert über
  **alle** `ItemProgress`-Zeilen eines Kindes, ohne Fach- oder Sprachfilter. `Vocabulary.SourceLanguage`/
  `TargetLanguage` (`backend/Pugling.Api/Models/VocabEntities.cs:17-18`) können je Eintrag verschieden sein
  — ein Kind, das Englisch **und** Französisch lernt, bekäme bei `--use-weak` schwache Wörter aus **beiden**
  Sprachen zurück, und `BriefingBuilder.cs:38-40` würde sie unterschiedslos in den unveränderlichen
  Pflicht-Wortschatz einer einzelnen (z. B. französischen) Übung zwingen — ein Verstoß gegen genau die
  Kernregel, die der Agent sonst deterministisch erzwingt („Interessen kleiden den Stoff ein, sie ersetzen
  ihn nie", hier: der Wortschatz selbst wird verunreinigt).

## Die echte Lücke

Nicht „der Fördermodus fehlt", sondern: **der vorhandene Fördermodus ist ungetestet und nicht
sprachrein.** Das Fach als Filterdimension wäre eine falsche Fährte (ein Fach wie „Sprachen" kann mehrere
Sprachpaare bündeln, ein Sprachpaar aber nie zwei Fächer) — richtig ist ein Filter auf das **Sprachpaar**
der Vokabel selbst, dem Träger der Bedeutung.

## Offene Punkte

- ~~Ungeprüft: kommt der Agent auf dem Creator-Sitz ohne Rollenverletzung an kind-bezogene
  Auswertungs-Endpunkte?~~ → siehe Entscheidung 1.
- ~~Soll ein neuer `--mode weakness`-Verb/Schalter gebaut werden, wie der Titel der Idee nahelegt?~~ →
  siehe Entscheidung 2.
- ~~Wie wird der Mechanismus davor geschützt, sprachfremde Wörter in den Pflicht-Wortschatz zu ziehen?~~ →
  siehe Entscheidung 3.
- ~~Soll `--use-weak` ohne `--child` weiterhin still wirkungslos bleiben?~~ → siehe Entscheidung 4.
- ~~Braucht der bestehende Mechanismus Testabdeckung, bevor an ihm weitergebaut wird?~~ → siehe
  Entscheidung 5.

## Entscheidungen

1. **Zugriff ist bereits korrekt und geklärt — keine Änderung nötig.** Ein reiner Creator-Sitz kommt nicht
   an kind-bezogene Lernstand-Daten (siehe Ist-Stand). Der Agent funktioniert bei `--use-weak` nur, weil das
   Konto ohnehin Supervisor der Zielkinder ist — dieselbe Doppelrolle, die das gesamte Produkt trägt, kein
   Sonderweg des Agenten. Kosten: keine, reine Feststellung.
2. **Kein neuer `--mode`-Verb — `--use-weak` bleibt der eine Mechanismus.** Er existiert seit dem ersten
   Commit des Agenten; ein `--mode weakness` wäre ein zweiter, redundanter Weg zum selben Ziel und würde die
   schlanke „ein Verb, mehrere Optionen"-CLI (`backend/Pugling.Agent.Creator/CommandLine.cs:11-14`)
   aufblähen. Der Titel dieser Story wird auf die reale Lage angepasst; die Id B-21 bleibt stabil (README
   „Teilen und Zusammenlegen": die Id ist die Referenz, nicht der Slug). Kosten: keine Baukosten, nur eine
   Erwartungskorrektur.
3. **Sprachpaar-Filter statt Fach-Filter für den Wort-Rollup.** `GET .../vocabulary-progress/by-word`
   bekommt zwei optionale Query-Parameter `sourceLang`/`targetLang` (additiv); die Query filtert vor dem
   Gruppieren zusätzlich auf `Vocabulary.SourceLanguage`/`TargetLanguage`. `StudentApi.ListWordMasteryAsync`
   reicht beide optional durch. `BriefingBuilder` übergibt das **effektive** Sprachpaar der Anfrage
   (Request-Override, sonst Profil, sonst `en`/`de` — dieselbe Kaskade wie in `BriefingBuilder.cs:53`
   bereits für die Übung selbst verwendet). Dafür muss `ResolveProfileAsync` vor `LoadChildAsync` laufen
   (Tausch der Reihenfolge in `BriefingBuilder.BuildAsync:29-30` — `ResolveProfileAsync` braucht nur die
   rohe `childId`, keine `ChildFacts`). Kosten: additive Query-Parameter (kein Vertragsbruch), ein kleiner
   Reorder, ein neuer Testfall; Risiko einer veränderten Fehler-Reihenfolge bei kombinierten Fehlern
   (unbekanntes Profil **und** unsichtbares Kind) wird durch die bestehenden Tests in `PuglingClientTests`
   mit abgedeckt.
4. **`--use-weak` ohne `--child` wird ein Fehler, kein stiller No-Op.** Heute setzt `BuildRequestAsync`
   (`AgentCommands.cs:244`) `UseWeakWords` unabhängig von `--child`, aber `BriefingBuilder.BuildAsync`
   (`BriefingBuilder.cs:29`) lädt Kind-Fakten nur mit `ChildId` — die Option verpufft wortlos, ohne Meldung.
   Eine Guard Clause in `BuildRequestAsync` wirft `AgentUsageException`, analog zur bestehenden
   Pflicht-Prüfung (`AgentCommands.cs:226-228`). Kosten: eine Prüfung, kein Verhaltensbruch im Normalfall.
5. **Pipeline-Test für `UseWeakWords: true` ist Pflicht.** `CreatorAgentTests.cs:64` deckt bislang nur
   `UseWeakWords: false` ab — der Mechanismus läuft seit seiner Entstehung ungetestet durch Reparatur-Runde
   und Selbsttest. Vor der Sprachfilter-Erweiterung (Entscheidung 3) braucht es einen grünen
   Regressionsanker, sonst prüft der neue Test nur sich selbst. Kosten: ein neuer Testfall mit
   `FakeChatClient`, Fixture-Daten für zwei Sprachpaare.

## Akzeptanzkriterien

1. `GET api/v1/student/children/{childId}/vocabulary-progress/by-word` akzeptiert die optionalen
   Query-Parameter `sourceLang`/`targetLang`; gesetzt, enthält das Ergebnis nur Wörter mit genau diesem
   Sprachpaar. Ohne die Parameter bleibt das Verhalten unverändert (Rückwärtskompatibilität).
2. `StudentApi.ListWordMasteryAsync` nimmt `sourceLang`/`targetLang` optional entgegen und reicht sie an
   die Query weiter.
3. `BriefingBuilder` ruft bei `UseWeakWords: true` `ListWordMasteryAsync` mit dem effektiven Sprachpaar der
   Anfrage auf (Request-Override, sonst Profil, sonst `en`/`de`); ein schwaches Wort aus einem anderen
   Sprachpaar erscheint weder im Pflicht-Wortschatz noch im Prompt-Abschnitt „Schwach beherrschte Wörter".
4. `pugling-creator create --use-weak` (bzw. `exam`) **ohne** `--child` bricht mit einer verständlichen
   Fehlermeldung ab, statt die Option stillschweigend zu ignorieren.
5. Ein neuer Test in `CreatorAgentTests` erzeugt für ein Kind mit einem schwachen Wort im Sprachpaar A und
   einem (irrelevanten) schwachen Wort im Sprachpaar B eine Übung mit `UseWeakWords: true`, deren Entwurf
   ausschließlich das Wort aus Sprachpaar A enthält.
6. Alle bestehenden Tests (`PuglingClientTests`, `CreatorAgentTests`, `ExerciseItemsAndProgressTests`)
   bleiben grün; ohne `--use-weak`/`onlyWeak` ändert sich kein bestehendes Verhalten.

## Schätzung

**Größe: S** — additive Query-Parameter an einem bestehenden Endpunkt (kein neues Konzept), ein kleiner
Reorder in `BriefingBuilder`, eine Guard Clause in `AgentCommands`, ein neuer Testfall. Vergleichbar mit dem
S-Anker „`childId` aus dem Test-Pfad ziehen" (B-01) — lokal begrenzte Änderung an drei bis vier Dateien ohne
Schema-Anfassen.

- **wo:** backend (Controller, Contracts-Client-Erweiterung, Agent — kein Frontend betroffen).
- **migration:** nein — keine Schema-Änderung, reine Filterung auf den vorhandenen Spalten
  `Vocabulary.SourceLanguage`/`TargetLanguage`.
- **vertragsbruch:** nein — zwei neue optionale Query-Parameter sind additiv; `WordMasteryResponse` bleibt
  unverändert.
- **Risiken:** Der Reorder in `BriefingBuilder.BuildAsync` (Profil vor Kind) kann die Reihenfolge von
  Fehlermeldungen bei kombinierten Fehlern verschieben — durch bestehende Tests abgedeckt, aber beim Bauen
  gegenprüfen. Ein Kind mit mehreren Lehrwerken **im selben** Sprachpaar bleibt unkritisch (Sprachpaar ist
  die korrekte Dimension, nicht das Lehrwerk).
- **Angriffsplan (Backend zuerst):**
  1. `by-word`-Endpunkt + `WordMasteryResponse`-Query additiv um `sourceLang`/`targetLang` erweitern.
  2. `StudentApi.ListWordMasteryAsync` um die beiden optionalen Parameter ergänzen.
  3. `BriefingBuilder`: `ResolveProfileAsync` vor `LoadChildAsync` ziehen, effektives Sprachpaar
     durchreichen.
  4. Guard Clause `--use-weak` ohne `--child` in `AgentCommands.BuildRequestAsync`.
  5. Testfall in `CreatorAgentTests` (zwei Sprachpaare) + ergänzender Integrationstest für den neuen
     Query-Parameter in `Pugling.Api.Tests` (Muster `ExerciseItemsAndProgressTests`).
- **Testweg:** `dotnet test Pugling.sln -c Release` (Test-Tor); gezielt `CreatorAgentTests` (neuer Fall) und
  ein neuer Fall in `Pugling.Api.Tests` für `by-word?sourceLang=…&targetLang=…`. Kein E2E/`/smoke-test`
  nötig — der Agent läuft nicht im Frontend-Durchstich.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Recherche gegen den Code zeigt, dass der Fördermodus als `--use-weak`
  bereits seit dem ersten Agenten-Commit existiert (`GenerationRequest.UseWeakWords`, `BriefingBuilder`,
  `ChildVocabularyProgressController.ByWord`); die „ungeprüfte" Rollentrennungsfrage der Idee-Stufe ist
  über `AuthAccess.OwnsChildAsync` und `PuglingClientTests` belegt geklärt. Verbliebene echte Lücke:
  fehlender Sprachfilter im Wort-Rollup und fehlende Testabdeckung für `UseWeakWords: true`.
- **2026-08-03** — gegrillt: autonom getroffen, Nutzerauftrag 2026-08-04. Fünf Entscheidungen getroffen
  (Zugriff bereits korrekt, kein neuer `--mode`-Verb, Sprachpaar- statt Fach-Filter, Guard Clause gegen den
  stillen No-Op, Pipeline-Test als Vorbedingung); Titel der Story an die reale Lage angepasst.
- **2026-08-03** — geschätzt: autonom getroffen, Nutzerauftrag 2026-08-04. Größe S, `wo: backend`,
  `migration: nein`, `vertragsbruch: nein`; Angriffsplan und Testweg (`CreatorAgentTests` +
  `Pugling.Api.Tests`) festgelegt. Kein XL-Split nötig.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt, beide Lücken bestehen (kein Sprachfilter in `ChildVocabularyProgressController.cs:66-74`/`StudentApi.cs:34-37`; kein Test mit `UseWeakWords: true`, `CreatorAgentTests.cs:68`; kein Guard ohne `--child`, `AgentCommands.cs:334`). Neu: das Verb `plan` aus B-19 nutzt denselben `BuildRequestAsync` (`AgentCommands.cs:189`) – der Guard deckt es mit ab, ein Testfall dafür gehört dazu. Nachtlauffähig.
- **2026-10-23** — gebaut im Nachtlauf (dritter Lauf, Sprint 7 / NP-2), unter Freigabe 8. `by-word` filtert optional auf `sourceLang`/`targetLang` vor dem Gruppieren (EXISTS auf `Vocabularies`), `StudentApi.ListWordMasteryAsync` reicht beides durch, Guard `--use-weak` ohne `--child` in `BuildRequestAsync` (deckt `create`, `exam`, `plan`). **Abweichung von Entscheidung 3, mit Grund:** statt „Profil vor Kind" lädt `BriefingBuilder` erst das Kind, dann das Profil und holt danach die schwachen Wörter mit dem effektiven Sprachpaar (`child with { WeakWords = … }`) – `ResolveProfileAsync` ruft für ein unsichtbares Kind `MatchProfilesAsync` und würfe sonst eine rohe API-Ausnahme statt der lesbaren Sichtbarkeitsmeldung (vom Reviewer bestätigt). Tests: `WortRollup_FiltertAufDasSprachpaar`, `Foerdermodus_nimmt_nur_…`, `Foerdermodus_ohne_Kind_…` (create, plan). Rote Proben, je an der erwarteten Zeile: kein Filter → rot an `ExerciseItemsAndProgressTests.cs:221` (`DoesNotContain`); falsche Spalte → rot an `:220`; Briefing ohne Sprachpaar (= Ausgangszustand) → rot an `CreatorAgentTests.cs:1046`; kein Guard → beide Theorie-Fälle rot an `:1066`. Backend **885/885**.
- **2026-10-23** — `pugling-reviewer`: kein Blocker. **Offen, nicht behoben (Lauf an Freigabe 3 beendet):** (a) der Test fährt nur den Zweig „Anfrage setzt die Sprache"; ein kaputter Profil-Zweig der Kaskade (AK 3) bliebe grün – Fall mit `SourceLang = null` und fr/de-Profil fehlt; (b) beide Testwörter haben das Ziel `de`, das Entfernen des `targetLang`-Filters bliebe grün; (c) Nit: `is not null` statt `IsNullOrWhiteSpace` wie der Nachbar `VocabularyStoreController.cs:124`; (d) README `:159`/Hilfe `:486` nennen die neue Pflicht `--child` nicht; `exam` fehlt als `InlineData`. **Vorbestehend, eigene Story wert:** `--child X --general --use-weak` zieht die schwachen Wörter weiter in `RequiredWords` (`BriefingBuilder.cs:54`), obwohl `--general` das Kind fallen lässt. Kein Rollengang, keine Abnahme.
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Sprint 7 wird **behalten** (kein Revert von `93f227a`). Die offenen Review-Funde werden als Nacharbeit mit **neuem Zähler (0)** abgearbeitet, danach folgt NP-3. `wartet_auf` geleert.
- **2026-10-26** — Nacharbeit ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C): (a) neuer Fall für die ganze Kaskade (Profil la→fr ohne Sprachen der Anfrage; zweite Anfrage en/de schlägt das Profil), (b) drittes Wort en→fr mit Positivkontrolle im Rollup-Test, (c) `IsNullOrWhiteSpace`, (d) Hilfe und README nennen „braucht `--child`“, `exam` als `InlineData`. Proben: Profil ignoriert / Profil schlägt Anfrage / Ziel des Profils ignoriert / ohne `sourceLang`-Filter → je 1 failed; ohne `targetLang`-Filter → 1 failed; Wächter weg → 3 failed. Der vorbestehende Fund `--general --use-weak` ist jetzt [B-279](B-279-general-use-weak-zieht-woerter-trotzdem.md). Rollengang ohne Browser (Extension nicht verbunden) als E2E bzw. Integrationstest: **Creator** fährt `uebungstypen.spec.ts` (jede Übungsart anlegen und im Testmodus ausprobieren, der Aufsatz als Ansicht ohne Bewertung) und die Agent-Tests (`CreatorAgentTests.Foerdermodus_*`); **Vater** und **Sohn** als Regressionszeugen, volle Suiten grün. Verifikation: Solution-Build 0 Warnungen, Backend **920/920 `exit=0`**, Vitest **444/444**, Playwright voll nach dem letzten Code-Edit **95 passed**. Zähler der Nacharbeit am Ende **4**. **Abgenommen**; Commit: `14fa1f7`.
- **2026-10-26** — **Nachschau** (frischer Prüfer, `git archive` von `14fa1f7`, [Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C): AK 1–4 halten, alle Mutanten rot. **Doku-Befund:** Die Probenzahlen im Eintrag davor stimmen nicht mehr. Gemessen: Server ohne `sourceLang`-Filter → **3** failed, Client ohne `sourceLang` → 2, Server ohne `targetLang`-Filter → **2**, Client ohne `targetLang` → 1. Sie stammen von vor den Review-Behebungen 2 und 4 und wurden danach nicht wiederholt. Das ist dasselbe Muster wie bei B-280, hier ohne Folge für die Abdeckung. Anmerkung: Die zweite Anfrage im Kaskaden-Test (en/de) ist nur sicher, weil die DB je Klasse wenige schwache en/de-Wörter hat.
