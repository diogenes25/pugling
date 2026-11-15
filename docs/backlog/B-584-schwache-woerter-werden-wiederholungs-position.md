---
tags: [typ/story, status/abgenommen, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Lücke wird Pflicht, Schwache Wörter zuweisen]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/rockettutor-vergleich-plan.md#wo-rockettutor-weiter-ist
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-584 · Die schwachen Wörter eines Kindes werden mit einem Griff zur Wiederholungs-Position

RocketTutors Lehrer-Sicht zeigt die größten Wissenslücken und ist zugleich der Startpunkt einer
Zuweisung. Pugling erkennt die Lücke schon: der Lernstand filtert schwache Wörter (`onlyWeak`, Schwelle
50 %, `backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:47` und `:92`), das
Vater-Web zeigt sie (`frontend/src/lib/api.ts:591`), und Positionen üben per Default die schwächsten
zuerst (`PracticeOrder.WeakestFirst`). Was fehlt, ist der Schritt von der Liste zur **Pflicht**: der Vater
sieht „diese 15 Wörter sitzen nicht" und kann daraus nicht direkt eine Position mit eigenem Pflichtziel
machen. Berührt [B-39](B-39-supervisor-dashboard.md) und [B-539](B-539-ein-aktiver-plan-je-fach.md).

## User Story

Als **Vater** möchte ich die schwachen Wörter, die mir der Lernstand meines Kindes zeigt, mit einem Klick als
tägliche Pflicht-Wiederholung in seinen laufenden Plan legen, damit aus „das sitzt nicht" sofort „das wird
geübt" wird – ohne Übung anlegen, Wörter abtippen und zuweisen in drei Bereichen.
Als **Sohn** möchte ich dabei nur Wörter bekommen, die ich wirklich schon hatte, damit die Pflicht fair bleibt.

## Ist-Stand am Code

- **Die Lücke ist erkannt und sichtbar.** `ChildVocabularyProgressController.cs:92` (`by-word`) rollt den Lernstand
  je Store-Vokabel über alle Übungen auf; `onlyWeak` filtert auf Durchschnitt `< ItemProgress.WeakBelowPercent`
  (= 50, `Models/ItemProgressEntities.cs:39`), optional je Sprachpaar (`sourceLang`/`targetLang`, B-21).
- **Das Vater-Web zeigt die Arbeitsliste, aber ohne Handlung.** `frontend/src/vater/VaterLernstand.tsx:70-75`
  (`WordList`, Seite à `PAGE_SIZE` = 25, `components/ListControls.tsx:12`), Filter „nur schwache (unter 50 %)"
  `:102`, und der Text darunter nennt die Wörter schon „Kandidaten für eine gezielte Wiederholungs-Übung" (`:107`)
  – einen Knopf dafür gibt es nicht.
- **Die Konsole kann es schon, das Web nicht.** Der KI-Creator nimmt `create --child X --use-weak`
  (`backend/Pugling.Agent.Creator/AgentCommands.cs:509-511`, `:575`) und `assign` legt Positionen in den
  spielbaren Plan (`AgentCommands.cs:51` → `PlanAssigner.cs:160`, Plan-Wahl über `IsPlayable` `:223`,
  Dubletten-Sprung `:173`). Beides braucht Ollama bzw. eine Konsole – nicht der Weg des Vaters am Küchentisch.
  Der offene `--mode weakness` deckt dasselbe Ziel also nur in der Konsole ab.
- **Eine Übung hängt zwingend an einer Unit.** `Exercise.SeriesUnitId` ist Pflicht (`Models/LearnEntities.cs:80`);
  Autorschaft über `AuthorAdultId` plus Owner-Grant entsteht heute nur im Creator-Pfad
  (`Controllers/Creator/ExerciseControllerBase.cs:371`, `:422`, Grant `:435`); `ExecutePublic` default
  `true` (`LearnEntities.cs`, Feld `ExecutePublic`).
- **Vokabel-Items referenzieren den Store.** `ExerciseItem.VocabularyId` (`Models/VocabEntities.cs:85`),
  materialisiert ID-erhaltend über `ExerciseItemService.ReconcileAsync` (`Services/*/ExerciseItemService.cs:107`).
  Eine Übung aus vorhandenen Store-Wörtern braucht also keine neuen Vokabeln.
- **Position anlegen** prüft Execute-Recht, leere Übung, Stufe, Dosis
  (`Controllers/Supervisor/PlanPositionsController.cs:148-229`); `Cadence` default `None` (`:195`),
  `PenaltyCoins` default 0 (`:214`, Malus ist Opt-in je Position), Zielpunkte über `GoalPointsPolicy.Effective`.
- **Ein aktiver Plan je Kind.** `StudyPlansController.cs:109-123` (`DeactivateSiblingPlansAsync`), spielbar
  heißt `Active && Start <= heute <= Ende` (`:30`). B-539 (gegrillt) macht daraus „einer je Fach"; ein Plan trägt
  dafür schon `SubjectId` (`Models/StudyPlanEntities.cs:74`).

## Die echte Lücke

Kein neues Datenmodell, sondern **ein Supervisor-Endpunkt, der drei vorhandene Schritte in einer Transaktion
ausführt** – Vokabel-Übung aus Store-Ids anlegen, Items referenzieren, Position mit Pflicht in einen Plan legen –
und **ein Knopf** in `WordList`.

## Offene Punkte

~~Neue Katalog-Übung oder bestehende Position auf eine Item-Auswahl einschränken?~~ → Entscheidung 1.
~~Wer ist Autor, wer darf sie zuweisen (`AuthorAdultId`, Grants, `ExecutePublic`)?~~ → Entscheidung 2.
~~An welcher Unit hängt die neue Übung?~~ → Entscheidung 3.
~~Welcher Plan – heute einer je Kind, nach B-539 einer je Fach?~~ → Entscheidung 4.
~~Taktung, Ziel, Punkte, Malus?~~ → Entscheidung 5.
~~Grenzen (wie viele Wörter, welche Wörter überhaupt)?~~ → Entscheidung 6.
~~Dubletten (Doppelklick, zweimal dieselbe Liste)?~~ → Entscheidung 7.
~~Deckt `--mode weakness` des KI-Creators das schon ab?~~ → Entscheidung 8.

## Entscheidungen

Alle **Stakeholder-Entscheidung im Auftrag des Nutzers** (Backlog-Lauf 2026-11-15, Delegation der
Stakeholder-Rolle inkl. `Wunsch`). Keine frühere Nutzer-Entscheidung liegt in dieser Story vor.

1. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Eine neue Vokabel-Übung je Wiederholung, keine Item-Auswahl an einer bestehenden Position.** Begründung: die
   schwachen Wörter stammen aus mehreren Übungen; eine Position kennt genau eine. Eine Item-Auswahl an der Position
   wäre ein neues Feld (Migration) und eine zweite Bedeutung von `Scope`. Die neue Übung referenziert die
   vorhandenen Store-Vokabeln (`ExerciseItem.VocabularyId`), der Lernstand je Wort läuft darum plan-übergreifend
   weiter. Kosten: der Katalog wächst um je eine kleine Übung pro Klick.
2. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Autor ist der handelnde Supervisor (`AuthorAdultId` = `User.AdultId()`, Owner-Grant wie im Creator-Pfad),
   `ExecutePublic = false`.** Gegated wird auf `Roles.Supervisor` – auch ein Supervisor ohne Creator-Rolle (Mutter)
   darf es, denn er schreibt keinen Stoff, er bündelt vorhandenen. Begründung: die Übung ist auf ein Kind
   zugeschnitten; im globalen Katalog bleibt sie lesbar, aber kein fremder Creator soll sie in fremde Pläne legen.
   Kosten: im Katalog erscheinen private Übungen „Wiederholung …"; der Vater kann sie über die vorhandene
   Bearbeiten-Seite ändern oder zurückziehen.
3. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Die Unit ist die häufigste Unit der Herkunftsübungen** dieser Wörter beim Kind (`ItemProgress.ExerciseId` →
   `Exercise.SeriesUnitId`), Gleichstand → kleinste Id. Begründung: deterministisch, ohne Rückfrage, und das Fach
   der Übung stimmt dann mit dem Fach der Wörter. Kosten: bei gemischten Units hängt die Übung an einer davon –
   nur Katalog-Ablage, ohne Wirkung aufs Spielen.
4. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Der Plan ist explizit in der Route** (`study-plans/{planId}/…`, `PlanOwnershipFilter`); das Frontend wählt den
   spielbaren Plan des Kindes, bei keinem ist der Knopf gesperrt und nennt den Grund, bei mehreren (nach B-539)
   erscheint eine Auswahl. Der Server prüft: der Plan gehört dem Kind der Wörter, und trägt er ein Fach, muss es
   das Fach der gewählten Unit sein (sonst `400 validation_error`). Begründung: so funktioniert es mit einem
   aktiven Plan je Kind wie mit einem je Fach, ohne dass diese Story B-539 vorwegnimmt. Kosten: eine Auswahl im
   Frontend, die heute nie erscheint (Vitest hält sie).
5. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Taktung `Daily`, Rest wie beim normalen Anlegen**: `GoalThreshold` Standard (80 %), Zielpunkte über
   `GoalPointsPolicy.Effective` (20), `PenaltyCoins` 0, Leitner an (`DefaultUseLeitner = true` an der neuen
   Übung), `OrderStrategy` `WeakestFirst`. Begründung: „Lücke wird Pflicht" ist der Kern; der Malus bleibt Opt-in je
   Position wie überall (`PlanPositionsController.cs:214`) – der Vater schärft ihn über den vorhandenen
   Positions-Editor nach, die Erfolgsmeldung verlinkt dorthin. Kosten: ein zweiter Schritt für den Vater, der
   sofort Malus will.
6. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **1 bis 25 Wörter, nur solche mit Lernstand beim Kind, alle aus einem Sprachpaar.** 25 = eine Seite der Liste
   (`PAGE_SIZE`), also genau, was der Knopf zeigt. Eine Vokabel ohne `ItemProgress`-Zeile des Kindes →
   `400 invalid_reference`; gemischte Sprachpaare → `400 validation_error` (Muster B-21). Begründung: Fairness für
   den Sohn – Pflicht nur auf Wörter, die er schon hatte; und der Endpunkt wird kein Hintereingang, um beliebige
   Übungen am Creator-Pfad vorbei zu bauen. Kosten: wer mehr als 25 Wörter will, klickt zweimal (zweite Seite).
7. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **Dubletten: doppelte Ids im Rumpf werden zusammengelegt; liegt im Plan schon eine Position auf einer
   Vokabel-Übung mit genau derselben Wortmenge, antwortet der Server `409 review_already_in_plan`** (neuer Code,
   additiv) mit der Position. Doppelklick fängt zusätzlich `useAction` + `disabled={busy}`. Begründung: zweimal
   dieselbe Pflicht wäre doppelte Arbeit fürs Kind ohne Mehrwert; eine *überlappende* neue Liste ist dagegen eine
   neue Entscheidung des Vaters und erlaubt. Kosten: Mengenvergleich über die Items der Plan-Positionen – ohne
   Markierungsspalte, also ohne Migration.
8. *Stakeholder-Entscheidung im Auftrag des Nutzers:* **`--mode weakness` des KI-Creators bleibt eigenständig.** Er erzeugt *neuen* Stoff um Schwächen herum (LLM),
   diese Story bündelt *vorhandene* Wörter deterministisch. Kosten: keine.

## Akzeptanzkriterien

1. `POST api/v1/supervisor/study-plans/{planId}/positions/from-words` mit `{ vocabularyIds: [...] }` legt eine
   Vokabel-Übung (Autor = Aufrufer, Owner-Grant, `ExecutePublic = false`, Items mit genau diesen `VocabularyId`s in
   Reihenfolge der Anfrage) und eine Position (`Cadence.Daily`, `PenaltyCoins` 0) an und liefert `201` mit
   `PositionResponse`. *Rot, wenn* der Service die Position mit `Cadence.None` anlegt oder die Items über
   Inline-Wörter statt Store-Referenzen erzeugt (Test prüft `VocabularyId` je Item).
2. Ein fremder Plan → `404`/`403` wie jeder Plan-Endpunkt; ein Kind-Login → `403`. *Rot, wenn*
   `PlanOwnershipFilter` bzw. `[Authorize(Roles = Roles.Supervisor)]` fehlt.
3. Eine Vokabel ohne Lernstand des Plan-Kindes → `400 invalid_reference`, nichts angelegt. *Rot, wenn* die Prüfung
   gegen `ItemProgress` des Kindes fehlt (Test nimmt ein Wort, das nur ein anderes Kind hatte).
4. 0 oder 26 Wörter → `400 validation_error`; zwei Sprachpaare → `400 validation_error`. *Rot, wenn* die
   Grenze bzw. die Sprachpaar-Prüfung fehlt.
5. Trägt der Plan ein Fach und die ermittelte Unit ein anderes → `400 validation_error`. *Rot, wenn* der
   Fach-Abgleich fehlt (Test: Plan mit `SubjectId` Mathe, Wörter aus Englisch).
6. Zweiter Aufruf mit derselben Wortmenge (auch umsortiert, mit Dublette) → `409 review_already_in_plan`, kein
   zweiter Eintrag; eine um ein Wort andere Menge → `201`. *Rot, wenn* die Mengenprüfung fehlt (erster Fall) oder
   auf Überlappung statt Gleichheit prüft (zweiter Fall).
7. Die Unit ist die häufigste Herkunfts-Unit, Gleichstand kleinste Id. *Rot, wenn* stattdessen die Unit der
   ersten Vokabel genommen wird (Test: erstes Wort aus Unit A, zwei weitere aus Unit B ⇒ B).
8. Vater-Web: unter „nur schwache" mit mindestens einem Wort zeigt `WordList` den Knopf „Diese N Wörter als
   tägliche Wiederholung zuweisen"; ohne spielbaren Plan ist er gesperrt und nennt den Grund; nach Erfolg steht
   ein Banner mit Link zum Plan. *Rot (Vitest), wenn* der Knopf ohne spielbaren Plan freigegeben ist oder ohne
   `onlyWeak` erscheint.
9. Der Sohn sieht die neue Position in der Arcade als Tagespflicht und kann sie spielen (E2E). *Rot, wenn* die
   Übung leer oder nicht spielbar entsteht.

## Schätzung

**Größe: M** – ein Service mit vier Prüfungen und drei vorhandenen Schreibschritten, ein Endpunkt, ein Knopf mit
Planwahl. Vergleich: wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03, M); mehr als B-01 (S), weil
Backend **und** Frontend nachziehen; keine Migration, darum kein L.

- **`migration: nein`** – Übung, Items, Grant und Position sind vorhandene Tabellen; die Dubletten-Prüfung
  vergleicht Wortmengen statt eine Markierung zu speichern.
- **`vertragsbruch: nein`** – neues DTO `CreateReviewFromWordsDto` (Contracts/Supervisor), neuer Fehlercode
  additiv, neue Client-Methode; nichts Bestehendes ändert sich.

**Risiken**

- **Zwei Schreib-Pfade für Übungen.** Der Creator-Pfad (`ExerciseControllerBase.Create`) prüft Config, Grammatik,
  Interessen; der neue Pfad darf diese Prüfungen nicht unterlaufen. Gegenmittel: nur die Vokabel-Config
  (Sprachpaar aus den Wörtern, `Direction` Standard) und `ExerciseItemService.ReconcileAsync` verwenden – keine
  freie Config vom Client annehmen.
- **Transaktion.** Übung, Grant, Items und Position in **einer** `SaveChanges`-Folge mit Transaktion; sonst bleibt
  bei einem `409`/`400` nach dem ersten Schritt eine leere Übung im Katalog. Alle Prüfungen vor dem ersten Schreiben.
- **`EndpointCoverageGuard`** (`Pugling.Api.Tests/EndpointCoverageGuard.cs:30`, 282) zählt die neue Action: Zahl
  anheben, sobald der Integrationstest sie trifft.
- **Herkunfts-Unit eines gelöschten Stoffs.** `ItemProgress.ExerciseId` kann auf eine inzwischen gelöschte Übung
  zeigen (Lernstand überlebt). Dann zählt diese Zeile nicht für die Unit-Wahl; bleibt keine → `400 validation_error`.
- **Doku-Capture** (`DocsCaptureTests`): neuer Endpunkt braucht ggf. ein Beispiel; zeitabhängige Titel (Datum)
  maskieren.

**Angriffsplan (Backend zuerst, API-First)**

1. Contracts: `CreateReviewFromWordsDto(List<int> VocabularyIds)`; `ApiErrors.ReviewAlreadyInPlan` (409).
2. `Services/Supervisor/WeakWordReviewService.cs`: Prüfungen (Anzahl, Lernstand beim Kind, Sprachpaar, Unit-Wahl,
   Fach-Abgleich, Mengen-Dublette), dann Übung + Owner-Grant + Items (`ReconcileAsync`) + Position in einer
   Transaktion. Titel (Produkttext, deutsch): „Wiederholung: schwache Wörter (<Kind>, <Datum>)".
3. Action in `PlanPositionsController` (`[HttpPost("from-words")]`, Plan-Ownership wie die Nachbarn).
4. `Pugling.Client/SupervisorApi.cs`: eine Zeile; OpenAPI neu, `contract.ts` generieren, Alias in `types.ts`.
5. Frontend: `api.ts` + Knopf in `WordList` (`useAction`, `disabled={busy}`, gesperrt mit Grund ohne spielbaren
   Plan, Auswahl bei mehreren), Erfolgsbanner mit Link `/vater/plaene/{planId}`.
6. Doku: `docs/tutorial-supervisor.md` und `docs/endpunkt-beziehungen.md` um den Weg ergänzen.

**Testweg**

- Integration: neue Klasse `backend/Pugling.Api.Tests/WeakWordReviewTests.cs` (Vorbild `PositionVocabRefTests.cs`,
  `ExerciseItemsAndProgressTests.cs`) – AK 1–7, je Fall die oben genannte rote Probe.
- Vitest: `frontend/src/vater/VaterLernstand.test.tsx` erweitern (AK 8: Knopf nur unter `onlyWeak`, gesperrt ohne
  spielbaren Plan).
- E2E: neue Spec `frontend/e2e/schwache-woerter-zuweisen.spec.ts` (eine Fläche je Spec): Kind beantwortet Wörter
  falsch → Vater öffnet Lernstand → Knopf → Kind sieht die Tagespflicht und spielt sie (AK 9). Zählt als Rollengang.
- Reviewer: `pugling-reviewer` und `frontend-reviewer` (`wo: beides`).

## Verlauf

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 5 des Übernahmeplans; Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)).
- **2026-11-15** — Backlog-Lauf 2026-11-15: `ausformuliert` – Ist-Stand am Code belegt (Liste und Filter da, Konsole
  kann `--use-weak` + `assign`, Web hat keinen Knopf; Übung braucht Unit; ein aktiver Plan je Kind, B-539 im Blick).
- **2026-11-15** — Backlog-Lauf 2026-11-15: `gegrillt` – acht Stakeholder-Entscheidungen im Auftrag des Nutzers
  (neue Übung aus Store-Ids, Autor = Supervisor mit `ExecutePublic = false`, Unit = häufigste Herkunft, Plan
  explizit in der Route mit Fach-Abgleich, `Daily` ohne Malus, 1–25 Wörter mit Lernstand, Mengen-Dublette 409,
  `--mode weakness` getrennt).
- **2026-11-15** — Backlog-Lauf 2026-11-15: `geschaetzt` – M, beides, keine Migration, kein Vertragsbruch; Risiken
  zweiter Übungs-Schreibpfad und Transaktion.
- **2026-11-15** — `geschaetzt` → **`abgenommen`** in Sprint 79 des Backlog-Laufs 2026-11-15, Code-Commit `c2353a57`.
  Gebaut nach dem Angriffsplan: `WeakWordReviewService` (alle Prüfungen vor dem ersten Schreiben, Übung + Owner-Grant +
  Items + Position in einer Transaktion), `POST …/positions/from-words`, neue Codes `review_already_in_plan` (409) und
  `review_subject_mismatch` (400, Review-Fund: eigener Code, damit das Vater-Web den Fall deutsch nennen kann),
  Client-Methode, Knopf im Lernstand.
  **Am laufenden Produkt gefunden (E2E):** sind alle Wörter einer einzigen Übung schwach, ist die Wiederholung genau
  diese Übung – der Server lehnt sie richtig als doppelte Pflicht ab, der deutsche Satz sagte aber „schon als
  Wiederholung“. Satz und Doku nennen jetzt jede Position; ein eigener Integrationstest hält den Fall.
  **Review-Funde behoben:** Blocker – ein Kind mit zwei Sprachen (Friedrich) bekam einen Knopf über gemischte Wörter, den
  der Server ablehnte: der Lernstand nennt jetzt das Sprachpaar je Wort (`WordMasteryResponse.SourceLanguage/
  TargetLanguage`, additiv), die Seite bietet einen Knopf je Paar; die Unit-Wahl war nicht deterministisch (jetzt die
  früheste Herkunftsübung je Wort); Reihe ohne Fach wird abgelehnt (der Creator-Pfad verbietet sie); Knopf wird beim
  Blättern gesperrt statt ausgeblendet; Sperrgrund nennt nur, was sperrt; Einzahl; zwei leere Test-Zusicherungen ersetzt,
  Planwahl getestet. Bewusst offen: zwei gleichzeitige identische Anfragen können beide durchkommen (der Doppelklick
  fängt der Client; die Doku sagt es so).
  **Stakeholder-Entscheidung im Auftrag des Nutzers beim Bau:** bei der Fach-Abweichung bleibt der Knopf eines Paars
  klickbar und der Server antwortet mit dem deutschen Satz – die Seite kennt das Fach der Wörter nicht; mit B-539 (ein
  Plan je Fach) wählt der Vater dann den passenden Plan.
  - AK 1 → `WeakWordReviewTests.Legt_Eine_Taegliche_Wiederholung_…`; Mutanten „Tagespflicht“, „nicht öffentlich“ → rot.
  - AK 2 → `…Das_Kind_Darf_Es_Nicht`, `…Ein_Fremder_Plan_Ist_Unbekannt` (zweiter, echter Vater).
  - AK 3 → `…Ein_Wort_Ohne_Lernstand_…`; Mutante „Lernstand dieses Kindes“ → rot.
  - AK 4 → `…Anzahl_Und_Sprachpaar_…`; Mutanten „Obergrenze“, „ein Sprachpaar“ → rot.
  - AK 5 → `…Ein_Plan_Eines_Anderen_Fachs_…`; Mutante „Fach des Plans“ → rot.
  - AK 6 → `…Dieselbe_Wortmenge_…`, `…Die_Herkunftsuebung_…`; Mutanten „Mengengleichheit“, „Herkunftsübung zählt“ → rot.
  - AK 7 → `…Die_Uebung_Haengt_An_Der_Haeufigsten_Unit`; Mutante „häufigste Unit“ → rot.
  - AK 8 → Vitest `VaterLernstand.test.tsx` (gesperrt ohne spielbaren Plan, nur unter „nur schwache“, Planwahl, je
    Sprachpaar); Mutanten „nur spielbarer Plan“, „nur unter schwache“, „Planwahl“, „je Sprachpaar“, „Sprachpaar je Wort“
    → rot.
  - AK 9 → E2E `schwache-woerter-zuweisen.spec.ts` (Test falsch → Lernstand → Knopf → 409 beim zweiten Klick → Kind sieht
    die Tagespflicht).
  - Belege: volle Suite **1693/1693** (Exit 0, `EndpointCoverageGuard` 284), Vitest **1113/1113**, `tsc -b` grün;
    Rollengang Vater **13 passed** (Ports 5253/5193). Reviewer: `pugling-reviewer`, `frontend-reviewer`.
- **2026-11-15** — umnummeriert von B-544 auf B-584: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
