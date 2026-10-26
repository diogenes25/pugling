---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/medien, rolle/creator, rolle/student]
aliases: [Interessenbasierte Übungen, Zielgruppe statt Kind]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
nachgeschaut: 2026-10-26
wartet_auf: ""
---

# B-46 · Übungen entstehen für ein Interessenprofil, nicht für ein bestimmtes Kind

Die fachliche Regel: Ein Kind gibt seine **Interessen** an; der Creator baut Übungen **für ein Kind mit
diesen Interessen** — nicht für dieses eine Kind. Kommt ein zweites Kind mit demselben Profil, bekommt es
die vorhandenen Übungen. Ein Mädchen, das Einhörner mag, bekommt Übungen mit Einhörnern; ein Junge mit
Vorliebe für Roboter bekommt beim Verb „jump" den springenden Roboter. **Interessenbasiert, nicht
kindbasiert.**

## User Story

Als *Creator* möchte ich eine Übung **für ein Interessenprofil** (statt für ein bestimmtes Kind) erzeugen
und am Katalog erkennen können, welche Interessen sie einkleiden — damit ein zweites Kind mit demselben
Profil dieselbe Übung wiederverwenden kann, statt dass sie beim ersten Kind verborgen bleibt.

## Ist-Stand am Code

Bei den **Bildern** ist die Zielgruppen-Zuordnung schon gebaut: die geteilte Taxonomie `InterestTag`
(`backend/Pugling.Api/Models/InterestEntities.cs:16-46`) wird sowohl von Kindern (`ChildInterest`,
`InterestEntities.cs:53-76`, gewichtet `-3…+3`) als auch von Bildern referenziert
(`MediaTagLink`, `backend/Pugling.Api/Models/MediaEntities.cs:92-101`, reines n:m ohne Gewicht). Der
`MediaSelector` nutzt genau diese doppelte Referenz, um aus vielen Darstellungen eines Motivs die zum Kind
passende zu wählen.

Bei **Übungen** fehlt die Entsprechung an zwei Stellen, beide jetzt belegt:

1. **`Exercise` trägt keine Interessen-Referenz.** Die Klasse
   (`backend/Pugling.Api/Models/LearnEntities.cs:51-119`) hat `GradeMin`/`GradeMax`/`SchoolTypes`/`Source`/
   `CategoryId` als strukturierte Vorfilter-Merkmale (Zeilen 86-96) — aber kein Feld, das auf `InterestTag`
   verweist. Eine Suche nach `InterestTag` in dieser Datei findet **keinen** Treffer. Der generische Vertrag
   `ExercisePayload<TConfig>`/`ExerciseResponse<TConfig>`
   (`backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:12-26`) spiegelt das: er trägt dieselben
   Vorfilter-Felder, aber keine Interessen. Auch das `CreatorProfile`
   (`backend/Pugling.Api/Models/CurriculumEntities.cs:78-119`, der „Fachlehrer") kennt Fach, Schulart,
   Klassenstufe, Reihe — keine Interessen. Eine für Einhörner geschriebene Übung ist im Katalog von einer
   für Roboter geschriebenen also **nicht unterscheidbar**.
2. **Der KI-Creator brieft Interessen nur über ein konkretes Kind, nie unabhängig davon.**
   `BriefingBuilder.LoadChildAsync` lädt die gewichteten Interessen ausschließlich für eine `childId`
   (`backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:133-170`, insb. Zeilen 149 und 162-166).
   `CreatorBriefing.Interests` ist **bewusst leer**, sobald kein Kind gesetzt ist
   (`backend/Pugling.Agent.Creator/Briefing/CreatorBriefing.cs:36-37`: „deliberately empty in general
   mode"), und `--general` löscht das Kind sogar bei gesetzter `ChildId` wieder
   (`BriefingBuilder.cs:44-46`: `Child: request.General ? null : child`). `GenerationRequest`
   (`backend/Pugling.Agent.Creator/GenerationRequest.cs:40-56`) kennt kein Feld, mit dem sich Interessen
   **ohne** `ChildId` vorgeben ließen — bestätigt durch die Grep-Suche nach `--interests`, die außer der
   Doku-Erwähnung von `--general` nichts findet. Damit erzeugt jeder Lauf ohne Kind eine Übung **ohne jede**
   Interessen-Information, und jeder Lauf mit Kind bindet sie an genau dieses eine Kind, ohne eine Spur zu
   hinterlassen, welche Interessen tatsächlich eingekleidet wurden.

## Die echte Lücke

Enger als die Notiz vermutete: Es geht **nicht** darum, wie die Passung dem Supervisor angezeigt wird (das
ist bereits als eigene Frage in [B-18](B-18-auto-lehrplan-generator.md) verortet) und **nicht** um eine
Vater-Web-Oberfläche. Die eigentliche Lücke ist backend-seitig und zweiteilig:

- Der Katalog kann nicht **speichern**, für welche Interessen eine Übung eingekleidet wurde.
- Der KI-Creator kann nicht **ohne ein konkretes Kind** eine interessenbasierte Übung erzeugen — obwohl er
  mit `--general`/`--profile` bereits einen kindlosen Pfad kennt, der bloß bei Interessen leerläuft.

Ist das geschlossen, ist die Wiederverwendung für ein zweites, gleich interessiertes Kind eine reine
Katalog-Abfrage — kein neues Konzept mehr.

## Offene Punkte

1. ~~Welcher Interessen-Träger gilt (Freitext `Child.Interests` oder taxonomische `ChildInterest`)?~~ →
   Entscheidung 1.
2. ~~Gehört das Geschlecht zur Zielgruppe oder nur zur Einkleidung?~~ → Entscheidung 2.
3. ~~Wie kommt die Passung an den Supervisor (Filter/Sortierung/Auto-Generator)?~~ → Entscheidung 3.
4. ~~Wie eng darf getroffen werden (Bewertung wie `CreatorProfileService` statt Gleichheit)?~~ →
   Entscheidung 3 (folgt derselben Zurückstellung, weil sie erst dort gebraucht wird).
5. ~~Wie bekommt eine Übung ohne Kind überhaupt Interessen zugewiesen?~~ → Entscheidung 4.

## Entscheidungen

1. **Träger ist die taxonomische Seite (`InterestTag`/`ChildInterest`), nicht der Freitext
   `Child.Interests`.** Begründung: Wiederverwendbarkeit braucht einen stabilen Schlüssel zum Vergleichen
   zweier Kinder; Freitext ("mag Einhörner") ist dafür so ungeeignet wie er es für die Bildwahl war — genau
   darum referenziert `MediaTagLink` schon dieselbe Taxonomie, nicht das Freitextfeld. Kosten: keine
   zusätzlichen — die Taxonomie existiert bereits und wird nur um eine weitere Referenzstelle ergänzt.
2. **Geschlecht bleibt reine Einkleidungs-Information, kein Filtermerkmal am Katalog.** Begründung:
   `ChildFacts.Gender` fließt heute schon in den Prompt (`ChildFacts.cs:40`) und steuert dort die Ansprache
   — ein zusätzliches harte Filterfeld an `Exercise` würde eine zweite, mit den Interessen konkurrierende
   Zielgruppen-Achse aufmachen, obwohl „springt gern" und „ist ein Junge" beide letztlich Dressing sind.
   Kosten: Ein Grenzfall („Übung nur für Mädchen") bliebe ungefiltert auffindbar; das ist hinnehmbar, weil
   dafür bislang kein belegter Bedarf vorliegt (die Notiz nannte ihn als Beispiel, nicht als Anforderung).
3. **Die Passung an den Supervisor (Filter/Sortierung/Bewertung wie `CreatorProfileService`) ist
   ausdrücklich außerhalb dieser Story.** Begründung: Sie gehört zum immer schon vorgesehenen
   Auto-Generator [B-18](B-18-auto-lehrplan-generator.md) („Lehrplan automatisch aus gefilterten Übungen
   bauen") — dort entsteht ohnehin die erste Stelle, die Übungen nach Kind-Passung filtert, und eine zweite
   Bewertungslogik parallel dazu wäre doppelte Arbeit. Kosten: B-46 liefert nur das **Datenfeld**
   (`Exercise` ↔ `InterestTag`), keine Konsument:innen-Sicht — bis B-18 gebaut ist, ist die Zuordnung nur
   über die API sichtbar, nicht im Vater-Web.
4. **Interessen-Tags kommen auf zwei Wegen an die Übung.** Mit `--child`: automatisch aus den positiv
   gewichteten `ChildInterest`-Zeilen, die tatsächlich in den Prompt eingeflossen sind (Analogie zu
   `WeightedInterests` in `ChildFacts.cs:19,165`) — keine zusätzliche Entscheidung des Nutzers nötig. Ohne
   Kind (`--general`/`--profile`): ein neuer, optionaler `--interests <slug,slug,…>`-Schalter am
   `GenerationRequest`, aufgelöst gegen die bestehende `InterestTagsController`-Taxonomie. Begründung: Das
   schließt exakt die zweite Lücke (kindlose, aber interessenbasierte Übung) ohne den bestehenden
   `--general`-Pfad zu verändern. Kosten: ein neuer Request-Parameter plus Validierung
   (unbekannter Slug → `AgentUsageException`, Muster wie bei `--unit`).
5. **Größe: aus „voraussichtlich XL" wird `L`, kein Split nötig.** Begründung: Die Notiz vermutete drei
   unabhängige Pakete (Katalog-Merkmal, Agenten-Umbau, Vater-Web-Sicht). Entscheidung 3 schneidet das dritte
   ganz ab (gehört zu B-18), und Recherche zeigt, dass die verbleibenden zwei Pakete **eine** zusammen-
   hängende Änderung sind, die zentral greift: Der generische Vertrag `ExercisePayload`/`ExerciseResponse`
   deckt bereits **alle** Übungstypen ab (`ExerciseAuthoringDtos.cs:12,24`), und die CRUD-Logik läuft durch
   die **eine** Basis `ExerciseControllerBase` (`Controllers/Creator/ExerciseControllerBase.cs`) — eine
   Änderung dort wirkt für jeden Typ, ohne zehn Controller einzeln anzufassen. Kosten: keine zusätzlichen;
   diese Entscheidung schärft nur die Einschätzung.

## Akzeptanzkriterien

1. `Exercise` trägt eine n:m-Referenz auf `InterestTag` (neue Verknüpfungstabelle nach dem Muster von
   `MediaTagLink`, ohne Gewicht — Dressing ist Ja/Nein, keine Rangfolge).
2. `ExercisePayload<TConfig>`/`ExerciseResponse<TConfig>` tragen ein additives Feld für die Interessen-
   Tag-Ids (leer = Vorgabe, kein Bruch für bestehende Aufrufer). Ein unbekannter Tag-Id beim Anlegen/Ändern
   liefert `validation_error`, keine stille Übernahme.
3. Der KI-Creator setzt beim Erzeugen mit `--child` automatisch die positiv gewichteten, tatsächlich in den
   Prompt geflossenen Interessen als Tags auf die neue Übung.
4. Der KI-Creator erzeugt mit dem neuen `--interests`-Schalter (ohne `--child`) eine Übung, die dieselben
   Tags trägt, ohne dass ein Kind genannt wurde. Ein unbekannter Slug bricht mit einer sprechenden
   `AgentUsageException` ab statt mit einem rohen 400.
5. Eine bestehende, ohne Interessen erzeugte Übung bleibt unverändert lesbar (leere Tag-Liste), kein
   Migrationsschritt verlangt Altdaten nachzupflegen.
6. Zwei Übungen mit überlappenden Tag-Mengen sind über die Creator-Suche/-API danach unterscheidbar
   (Tag-Ids stehen in der `ExerciseResponse`) — die Voraussetzung, die B-18 künftig für die Passung braucht.

## Schätzung

**Größe: L** — Schema-Erweiterung (neue Verknüpfungstabelle, einzige neue Migration) **plus** Vertrags-
und Agenten-Umbau über vier Projekte (`Pugling.Api`, `Pugling.Contracts`, `Pugling.Client`,
`Pugling.Agent.Creator`), aber ohne Frontend und ohne die Bewertungslogik von B-18 — vergleichbar mit einer
DB-Umbau-Etappe wie E6, nur ohne deren Lösch-/Rettungs-Sonderfälle.

**Risiken:**

- Die neue Verknüpfungstabelle braucht eine eigene Zeile in den `SchemaGuardTests`-Listen (neue Beziehung
  = bewusste Zeile, siehe CLAUDE.md „Schema-Änderungen laufen gegen gepinnte Listen") — vergessen heißt
  rotes Tor, nicht stilles Risiko.
- `--interests` muss dieselbe Slug-Normalisierung wie `InterestSlug.cs` durchlaufen, sonst entstehen
  Karteileichen-Tags statt Treffer auf die bestehende Taxonomie.
- Der automatische Tag-Vorschlag aus `--child` darf **keine** Abneigungen (negative `ChildInterest.Weight`)
  aufnehmen — sonst würde eine Übung fälschlich mit einem Merkmal getaggt, das sie laut Prompt gerade
  vermeidet.

**Angriffsplan** (Backend zuerst):

1. `ExerciseInterestLink`-Entity + Migration (Kette neu falten), `SchemaGuardTests` nachziehen.
2. `ExercisePayload`/`ExerciseResponse` um die Tag-Ids erweitern, `ExerciseControllerBase` validiert und
   mappt sie zentral für alle Typen.
3. `Pugling.Client`: eine Zeile je betroffener Methode (Tag-Ids durchreichen), kein neues HTTP-Plumbing.
4. `Pugling.Agent.Creator`: `GenerationRequest.Interests`/`--interests`-Schalter, `BriefingBuilder` löst
   Slugs auf, `AgentCommands.CreateAsync` hängt die aufgelösten Tag-Ids an den Create-Aufruf.
5. Automatischer Tag-Vorschlag aus `ChildFacts.WeightedInterests` (nur `Weight > 0`) beim `--child`-Pfad.

**Testweg:** `ExerciseMetadataTests.cs` um die neuen Tag-Ids erweitern (Anlegen/Ändern, unbekannter Tag →
`validation_error`); `CreatorAgentTests.cs` (nutzt `FakeChatClient`, kein Ollama nötig) um die beiden neuen
Pfade (`--child` autotaggt, `--interests` ohne Kind); `SchemaGuardTests.cs` um die neue Beziehung. Kein
Frontend betroffen, daher kein E2E-Zusatz; `/smoke-test` bleibt der manuelle Gegencheck vor dem Commit.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung; die fachliche Regel kommt vom Nutzer,
  der Ist-Stand ist nur angelesen und beim Ausformulieren zu belegen).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den echten Code belegt (`Datei:Zeile` für
  `InterestEntities.cs`, `MediaEntities.cs`, `LearnEntities.cs`, `ExerciseAuthoringDtos.cs`,
  `CurriculumEntities.cs`, `BriefingBuilder.cs`, `CreatorBriefing.cs`, `GenerationRequest.cs`); „echte
  Lücke" auf den backend-seitigen Kern verengt, Supervisor-Passung als bereits vorhandene Frage von B-18
  erkannt — autonom getroffen, Nutzerauftrag 2026-08-03.
- **2026-08-03** — gegrillt: alle vier offenen Punkte in nummerierte Entscheidungen überführt
  (Interessen-Träger, Geschlecht, Abgrenzung zu B-18, Zuweisungsweg ohne Kind); Größe von der
  vermuteten XL auf L geschärft, kein Split nötig — autonom getroffen, Nutzerauftrag 2026-08-03.
- **2026-08-03** — geschätzt: `groesse: L`, `wo: backend`, `migration: ja`, `vertragsbruch: nein`,
  Risiken, Angriffsplan und Testweg festgelegt — autonom getroffen, Nutzerauftrag 2026-08-03.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
- **2026-09-02** — **gebaut** (Nachtlauf, Sprint 1; `geschaetzt` → `in-arbeit`). Alle sechs
  Akzeptanzkriterien erfüllt, danach **beide Reviewer** gelaufen und ihre Funde eingearbeitet.

  **Die fünf Schichten, je an genau einer Stelle** — das ist, was den `L`-Umfang getragen hat:
  `ExerciseInterestLink` (n:m ohne Gewicht, Muster `MediaTagLink`) · `InterestTagIds` am generischen
  Vertrag, der **alle** Übungstypen deckt · `ExerciseControllerBase` mit zwei Helfern
  (`InterestTagsProblemAsync`, `SetInterestLinks`) · `Pugling.Client` **unverändert**, weil die Methoden
  das Payload-`record` durchreichen · `ExerciseStrategy.Payload(…)`, den alle vier Agenten-Strategien
  benutzen. Kette neu gefaltet, Snapshot-Diff **45 Zeilen rein additiv**.

  **Rote Proben, jede mit Zahl (Auflage 5 des Nachtlaufs):**

  | Probe | erwartet | gemessen |
  |---|---|---|
  | `SetInterestLinks` aus `Create` | fällt breit | **4** Fälle (Metadaten + 3 Agenten-Fälle) |
  | `SetInterestLinks` aus `Update` | genau 1 | **1** (`Uebung_TraegtIhreEinkleidung_UndPutErsetztSie`) |
  | Sortierung absteigend statt aufsteigend | genau 1 | **1** (derselbe Fall, an der Vergleichbarkeit) |
  | Tag-Validierung im `Create` aus | genau 1 | **1** (`UnbekannterTag_Wird_Abgelehnt_…`) |
  | Tag-Validierung im `Update` aus | genau 1 | **1** (`Uebung_TraegtIhreEinkleidung_…`) |
  | Abneigungen mit-taggen | genau 1 | **1** (`Kinderpfad_TaggtDiePositivenInteressen_…`) |
  | Slug-Vergleich groß/klein-empfindlich | genau 1 | **1** (`KindloserPfad_TaggtDieAngegebenenSlugs`) |
  | `unused`-Filter ohne den dritten Referenzierer | genau 1 | **1** (`EineUebungAlsEinzigerNutzer_…`) |
  | `ExerciseCount` fest auf 0 | genau 1 | **1** (derselbe Fall) |
  | „erster Suchtreffer" statt exaktem Slug | genau 1 | **erst 0, dann 1** — siehe unten |

  **Drei Dinge, die die Schätzung falsch hatte, und alle drei nach oben:**
  1. **`wo: backend` war falsch, das Feld ist `wo: beides`.** Der Editor des Vater-Webs speichert per
     **Vollersatz**-PUT: ohne Durchreichung räumt ein Klick auf „Speichern" die Einkleidung weg, die der
     Agent gerade eingetragen hat. Die Falle steht in `uiTypes.ts` **zweimal** dokumentiert
     (`suggestedBonus`, `executePublic`) und hatte trotzdem keine Zusicherung — jetzt hat sie eine
     (`ExerciseEditModal.test.ts`, `payloadFrom` dafür exportiert). Das Frontmatter ist nachgezogen.
  2. **`ExerciseDetail` verspricht in seiner eigenen Doku „all metadata".** Die Einkleidung dort
     weglassen hätte diesen Satz falsch gemacht; also trägt sie auch diese Sicht. `ExerciseSummary`
     bleibt bewusst die schmale Zeile — mit der Begründung, die diese Zeile über sich selbst gibt.
  3. **Ein Fall, den die `## Entscheidungen` nicht hergeben:** `--child` *und* `--interests` zugleich.
     Entscheidung 4 nennt zwei Pfade, keine Vorrangregel. Nicht geraten, sondern zum **Bedienfehler**
     gemacht, der den Ausweg nennt — und der Test pinnt, dass `--general` dieselbe Kombination erlaubt,
     weil der Ausschluss an dem hängt, was das Briefing **trägt**, nicht an den getippten Schaltern.

  **Was die Reviewer gefunden haben (4 Funde mit Code-/Test-Wirkung, Zähler der Freigabe 3: 4 von 5):**
  - **Die Taxonomie hat jetzt drei Referenzierer, die Aufräum-Sicht kannte zwei.** Ein Stichwort, das nur
    eine Übung einkleidet, sah unter `?unused=true` unbenutzt aus — und das Löschen ist bewusst ohne
    Riegel. `ExerciseCount` ist additiv am `InterestTagResponse`, der Filter kennt den dritten Zweig, und
    die Begründung am `DELETE` sagt jetzt, dass ihr alter Satz („kostet nur Auswahlqualität") seit dieser
    Story nicht mehr stimmt. Die **Entscheidung**, ob ein benutztes Stichwort löschbar bleibt, ist
    ausdrücklich nicht getroffen → [B-206](B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md).
  - **Die Tag-Validierung im Änderungspfad erreichte keine rote Probe.** Gemessen bestätigt: ohne sie
    liefert ein PUT mit unbekannter Id **500** (Fremdschlüssel-Verstoß) statt `validation_error`, und der
    ganze PUT ist verloren. Der Fall trägt sie jetzt, inklusive der tragenden zweiten Hälfte — dass der
    abgelehnte PUT **nichts** geschrieben hat.
  - **`--interests` löste gegen eine auf 100 Einträge abgeschnittene Liste auf.** Ab dem 101. Stichwort
    hätte der Agent einen vorhandenen Slug als Tippfehler gemeldet und eine selbst gekürzte Liste als
    Beweis angeboten. Aufgelöst wird jetzt **je Slug** mit `search`. Die 100er-Grenze selbst hat bewusst
    keinen Fall (er bräuchte 101 Stichworte in der geteilten Klassen-DB); stattdessen ist der Weg so
    gebaut, dass die Grenze nicht mehr erreicht wird.
  - **Mein Kommentar über den `as`-Cast in der Frontend-Attrappe war falsch.** Der Reviewer hat
    nachgerechnet: das Literal ist vollständig und übersetzt **ohne** Cast. Der Cast hätte genau das
    Signal unterdrückt, für das die Datei da ist (ein 31. Feld am `ExerciseDetail`). Jetzt ein annotiertes
    `BASE` plus Spread — und eine Zusicherung über die **ganze** Nutzlast (`toEqual`), die auch ein
    versehentlich zusätzlich mitgeschicktes Feld fängt.

  **Und ein Fund an mir selbst, der wichtiger ist als die vier:** Die erste Fassung des Falls
  „Slug wird exakt getroffen" war **wirkungslos** — der Lockvogel sortierte hinter dem Ziel, „nimm den
  ersten Suchtreffer" traf also zufällig richtig. Gemessen: **47/47 grün unter der Probe.** Umgebaut
  (Ziel `zzz-…`, Lockvogel `aaa-…`, der den Slug enthält), danach grün in der Grundlinie und rot unter
  der Probe. Das ist die Regel „rote Probe am richtigen Ort" in ihrer teuren Form: der Fall *sah*
  tragfähig aus.

  **Verifikation:** **852/852** Backend (+8 Fälle), **328/328** vitest (+2), `dotnet build` 0 Warnungen,
  `dotnet format --verify-no-changes` sauber, `tsc -b` sauber.
- **2026-09-02** — **abgenommen.** Commit `644cff7`. Belege: 852/852 Backend, 328/328 vitest, 46/46 E2E,
  `dotnet build` 0 Warnungen, `dotnet format --verify-no-changes` und `markdownlint` sauber, **beide
  Reviewer** gelaufen (der Sprint hat Backend *und* Frontend angefasst), zehn rote Proben je mit Zahl.
  **Rollengang:** als Live-Probe gegen die laufende Instanz erbracht und im Protokoll
  ([docs/pm-sitzung-2026-09-02.md](../pm-sitzung-2026-09-02.md), „Runde 1") Schritt für Schritt
  aufgeschrieben — kein Browser gebunden, also Freigabe 6 in ihrer Ersatz-Fassung. Vater und Sohn sind
  ausdrücklich nur **Regressions-Zeugen**: keine ihrer Flächen ist von dieser Story berührt.
- **2026-10-26** — Nachschau (sechster Nachtlauf, Sprint 11, [Protokoll](../pm-sitzung-2026-10-26-nacht.md)): AK 4 (kindloser Pfad `--interests`) gegen den Prompt nachgerechnet, nicht nur gegen die Tag-Ids; AK 2 (`InterestTagsProblemAsync` vor jeder Zuweisung in Create/Update, `ExerciseControllerBase.cs:300,384`) hält. Befund → eigene Defekt-Story [B-264](B-264-interessen-ohne-kind-kleiden-nicht-ein.md): `--interests` taggt die Übung, kleidet sie aber nicht ein. `CreatorBriefing.Interests` ist ohne Kind leer (`Briefing/CreatorBriefing.cs:38`), die aufgelösten Slugs leben nur als Ids in `InterestTagIds` (`BriefingBuilder.cs:72,107-130`) und erreichen den Prompt nie; der Prompt sagt stattdessen „Kein bestimmtes Kind … keine erfundenen Vorlieben“ (`CreatorBriefing.cs:110-112`), `DraftPrompts.cs:61` überspringt die Einkleidungszeile, und die Beschreibung lautet „für den gemeinsamen Katalog erzeugt“ (`Drafting/ExerciseStrategy.cs:85`). Der Katalog behauptet damit eine Einkleidung, die der Inhalt nicht hat – die eine Bedingung `Child is null` zieht „kein Kind“ und „keine Interessen“ zusammen. `CreatorAgentTests.KindloserPfad_TaggtDieAngegebenenSlugs` (`:122-140`) prüft nur `detail.InterestTagIds`, nie `chat.LastMessages`.
