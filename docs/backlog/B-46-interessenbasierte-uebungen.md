---
tags: [typ/story, status/geschaetzt, bereich/katalog, bereich/medien, rolle/creator, rolle/student]
aliases: [Interessenbasierte Übungen, Zielgruppe statt Kind]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
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
