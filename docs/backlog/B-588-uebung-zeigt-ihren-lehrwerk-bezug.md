---
tags: [typ/story, status/in-arbeit, bereich/training, bereich/katalog, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Lehrwerk-Bezug, Buchseite zur Übung, schon durchgenommen]
status: in-arbeit
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: ja
vertragsbruch: nein
quelle: Nutzer im Gespräch zum sofatutor-Vergleich (2026-11-15)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-588 · Eine Übung zeigt ihren Bezug zum Lehrwerk (Reihe, Unit, Seite)

Zu jeder Übung soll ein Bezug zum Lehrwerk angezeigt werden können: welche Unit, welche Seite. Damit
kann das Kind (oder der Vater) bei einer Übung **im Buch nachsehen**, und das Kind erkennt, dass es die
Übung **eigentlich können müsste**, weil sie sich auf eine Seite oder ein Kapitel bezieht, das in der
Schule schon durchgenommen wurde. Anlass ist sofatutors Video-Hilfe, die von der Übung an die passende
Stelle des Erklärvideos springt; Puglings Gegenstück ist das eigene Schulbuch.

Zuschnitt seit dem Grillen (2026-11-16): diese Story liefert **Seitenfeld + Anzeige beim Kind**. Das Merkmal
„schon durchgenommen", die Anzeige in Vater-Plan/Lernstand und das Mitsetzen beim Import sind eigene
Folge-Stories (Entscheidungen 7–9).

## User Story

Als **Sohn** möchte ich an einer Übung sehen, aus welchem Buch, welcher Unit und von welcher Seite sie stammt
(„Access 3 · Unit 2 · S. 42"), damit ich beim Üben im eigenen Schulbuch nachschlagen kann und merke, dass es
Stoff aus dem Unterricht ist. Als **Creator** möchte ich dafür die Buchseite(n) an der Übung pflegen können.

## Ist-Stand am Code

Nachgemessen am 2026-11-16 (Worktree `backlog-lauf-1116`):

- **Unit-Bezug ist Pflicht.** `Exercise.SeriesUnitId` (`backend/Pugling.Api/Models/LearnEntities.cs:80`); die
  Unit trägt `Label`, `OrderIndex`, `Grade` und `BookType` (`Models/CurriculumEntities.cs:73`–`79`), die Reihe
  `Name` (`Models/CurriculumEntities.cs:33`).
- **Eine Seite gibt es nirgends.** Kein Seitenfeld an `Exercise` (`Models/LearnEntities.cs:77`–`175`). Es gibt
  aber ein **freies `Source`** („Green Line 3, Unit 4", `Models/LearnEntities.cs:126`–`127`), im Vertrag in
  `ExercisePayload` (`backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:29`), `ExerciseResponse`
  (`:46`) und `ExerciseDetail` (`Creator/ExerciseCatalogDtos.cs:63`). Es ist Katalog-Such-/Sortierfeld
  (`Controllers/Creator/ExerciseCatalogController.cs:106`–`107`, `:182`–`183`) und erreicht das Kind nicht.
- **Schreiben/Lesen am Creator:** Anlegen `Controllers/Creator/ExerciseControllerBase.cs:428`, Ändern per
  **PUT = volle Darstellung** `:517` (fehlendes `source` löscht), Projektion `Map` `:255`, Detail
  `ExerciseCatalogController.cs:224`. Validierungen sitzen vor dem Schreiben (`ExerciseControllerBase.cs:401`–`405`
  bzw. `:494`) mit `ProblemWithCode(ApiErrors.ValidationError, …)`.
- **Beim Kind:** Die Tagesübersicht liefert je Position `PositionStatus` mit `SeriesUnitId`/`SeriesUnitLabel`
  (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:25`–`29`), gebaut in
  `Services/Shared/PositionProgressService.cs:236`–`245`; geladen wird `Exercise → SeriesUnit`, **nicht** die
  Reihe (`:192`). Im Frontend gruppiert `frontend/src/sohn/pfad.ts:27` die Pfad-Abschnitte nach dem Unit-Label
  (die Notiz vom 2026-11-15 „kein Treffer" ist überholt); das Positions-Blatt zeigt Titel und Pillen
  (`frontend/src/sohn/SohnHome.tsx:293`–`303`). Frontend-Typen kommen generiert aus dem Vertrag
  (`frontend/src/lib/types.ts:335`, `:506`; `npm run gen:contract`).
- **Editor:** `frontend/src/vater/ExerciseEditModal.tsx` schreibt `source` (`:250`), hält es im Formular (`:285`),
  liest es zurück (`:311`) und zeigt das Feld „Quelle (Lehrbuch)" (`:382`); Feldhilfe `exerciseSource`
  (`frontend/src/lib/fieldHelp.ts:207`).
- **Import kennt die Seite, gibt sie aber nicht weiter.** Die Seiten-Markdowns tragen `seiten: "24-25"`
  (`.claude/skills/lehrwerk-aufbereiten/MARKDOWN.md:36`), `laden.py` setzt je Übung nur
  `"source": f"{book['quelle']}, Unit {e['unit']}"` (`laden.py:452`) und schreibt bei `--ersetzen` per **PUT**
  (`:458`); weder `buch.json`/`uebungen.json` noch die Vokabel-TSV (`laden.py:63`–`79`) haben ein Seitenfeld.
  Der KI-Creator (`backend/Pugling.Agent.Creator`) legt Übungen an, ohne eine Seite zu kennen (Treffer nur für
  `--source-file`, `AgentCommands.cs:59`–`71`).
- Letzte Migration der Kette: `20261115215256_ExerciseItemExplanation`.

## Die echte Lücke

Es gibt kein Feld, das die Buchseite einer Übung trägt, und keinen Weg, auf dem Reihe und Seite das Kind
erreichen. Unit-Label allein schickt das Kind „irgendwo in Unit 2" suchen.

## Offene Punkte

1. ~~Anti-Cheat: wo wird die Seite ausgeliefert?~~ → Entscheidung 6.
2. ~~Granularität: Seite an der Übung oder am Item?~~ → Entscheidung 2.
3. ~~Was heißt „durchgenommen"?~~ → zurückgestellt, Entscheidung 7.
4. ~~Wirkung auf Pflicht und Münzen?~~ → zurückgestellt mit Punkt 3, Entscheidung 7.
5. ~~Mehrere Bücher je Reihe (`BookType`)?~~ → Entscheidung 5.
6. ~~Form des Feldes (Zahl, Bereich, Text)?~~ → Entscheidung 1.
7. ~~Verhältnis zum vorhandenen `Source`?~~ → Entscheidung 3.
8. ~~Vertrag, PUT-Semantik, Feldname?~~ → Entscheidung 4.
9. ~~Anzeige beim Vater in Plan und Lernstand?~~ → zurückgestellt, Entscheidung 8.
10. ~~Setzen KI-Creator / `lehrwerk-aufbereiten` die Seite mit?~~ → zurückgestellt, Entscheidung 9.

## Entscheidungen

Alle: **Stakeholder-Entscheidung im Auftrag des Nutzers (2026-11-16).** Frühere Entscheidungen des Nutzers
lagen zu dieser Story nicht vor; die Empfehlungen der Idee sind Vorschläge und wurden geprüft.

1. **Freitext `BookPages` (≤ 20 Zeichen), keine Zahlen-Felder.** Getrimmt, leer/Whitespace → `null`, länger →
   `400 validation_error`. Gespeichert wird nur die Seitenangabe wie gedruckt („42", „42–43", „WB 12"), das „S."
   setzt die Anzeige davor. Begründung: entspricht `seiten:` der Seiten-Markdowns, deckt Doppelseiten und
   Workbook ab, kein Vorrat. Kosten: kein Sortieren/Filtern nach Seite.
2. **An der Übung, nicht am Item** (Empfehlung der Idee übernommen). Begründung: eine Übung entsteht aus einer
   Seite bzw. Doppelseite; Item-Seiten erst, wenn B-583 auf die Seite verweisen will. Kosten: eine Wortliste über
   zwei Seiten heißt „42–43" statt je Wort genau.
3. **`Source` bleibt unverändert und getrennt.** Keine Ableitung, kein Umschreiben der Bestandszeilen.
   Begründung: `Source` ist Katalog-Such-/Sortierfeld und Freitext ohne feste Form; die Reihe kennt der Server
   ohnehin über die Unit. Kosten: zwei Felder, die nach „Quelle" klingen – die Feldhilfe grenzt sie ab.
4. **Vertrag additiv, gleiche Semantik wie `Source`:** `string? BookPages = null` als **letzter Parameter mit
   Vorgabe** in `ExercisePayload`, `ExerciseResponse`, `ExerciseDetail`; PUT ist volle Darstellung (fehlt → `null`).
   Feldname `bookPages` fällt nicht unter die Lösungsfeld-Regel (`Answer`/`Solution`/`CorrectAnswer`/`Translation`);
   Clients ohne das Feld bleiben gültig, ein Tippfehler (`bookPage`) wird wie jedes fremde Feld `unknown_field`.
   Begründung: ein drittes PATCH-Muster für ein Feld wäre Inkonsistenz. Kosten: ein PUT ohne `bookPages`
   (heute `laden.py --ersetzen`) löscht die Seite – trägt die Folge-Story aus Entscheidung 9.
5. **Mehrere Bücher je Reihe: kein eigenes Feld.** Das Buch ergibt sich aus Reihe + Unit; ein Workbook-Bezug
   steht im Freitext („WB 12"). Begründung: klein halten, `BookType` hängt an der Unit, nicht an der Übung.
   Kosten: keine maschinelle Unterscheidung Schülerbuch/Workbook.
6. **Ausgeliefert wird die Seite an das Kind nur über die Tagesübersicht (`PositionStatus`), nirgends in Test-
   oder Klausur-DTOs.** Abweichend von der Empfehlung der Idee ist sie damit auch vor einem Positionstest
   sichtbar. Begründung: `PositionStatus` ist nicht test-skopiert, ein Verstecken wäre nur clientseitig; die Unit
   steht dort heute schon, das Buch liegt ohnehin neben dem Kind, Pugling beaufsichtigt nicht. Der Hebel des
   Vaters bleibt die Klausur (strikt one-at-a-time, ohne Seite) und der getippte Leitner über Tage, der Behalten
   misst und nicht Nachschlagen belohnt. Nachschlagen beim **Üben** ist der eigentliche Nutzen. Kosten: im
   Positionstest kann das Kind die Seite gezielter aufschlagen als mit dem Unit-Label allein.
7. **„Schon durchgenommen" (aus `CurrentUnitId`) und eine Wirkung auf Pflicht/Münzen: zurückgestellt** →
   Folge-Story. Begründung: braucht Regeln für mehrere Textbooks, Klassenstufen-Ordnung und Kinder ohne aktuelle
   Unit – sprengt ½–1 Tag. Kosten: der Titel der Idee ist nur zur Hälfte geliefert.
8. **Anzeige in Vater-Plan und Lernstand: zurückgestellt** → mit Entscheidung 7 in dieselbe Folge-Story. Der
   Vater sieht das Feld im Übungs-Editor; `PositionStatus` ist derselbe Vertrag für seine Tagessicht des Kindes.
   Kosten: auf eine Frage aus B-586 zeigt er die Seite erst über den Editor.
9. **Import und KI-Creator setzen die Seite noch nicht: zurückgestellt** → Folge-Story. `laden.py` bräuchte ein
   `seiten`-Feld in `uebungen.json` und je Abschnitt der Vokabel-TSV (bzw. `buch.json`) plus `bookPages` im
   Payload, sonst löscht `--ersetzen` (Entscheidung 4); der Agent bräuchte die Seite im Briefing. Begründung:
   andere Dateien, eigenes Format, eigener Test (`LadenFieldListTests`). Kosten: bis dahin pflegt der Creator die
   Seite von Hand.

## Akzeptanzkriterien

Jedes Kriterium nennt, welche Änderung es rot macht.

1. **Anlegen mit Wert:** `POST …/units/{uid}/vocabulary` mit `bookPages: "42–43"` → `201`, Antwort
   `bookPages: "42–43"`; `GET` Katalog-Detail derselben Übung liefert `"42–43"`. **Rot**, wenn `Create`, `Map`
   oder das Detail (`ExerciseCatalogController`) das Feld nicht übernimmt.
2. **Anlegen trimmt/leert:** `bookPages: " 42 "` → `"42"`; `bookPages: "   "` → `null`. **Rot** ohne Trim bzw.
   wenn Whitespace als `""` gespeichert wird.
3. **Länge:** 20 Zeichen → `201`; 21 Zeichen → `400` mit `code: validation_error`, bei POST **und** PUT. **Rot**
   ohne Längenprüfung oder wenn sie nur an einem der beiden Pfade sitzt.
4. **Ändern (PUT, volle Darstellung):** Übung mit `"42"`, PUT mit `bookPages: "7"` → `"7"`; danach PUT ohne das
   Feld → `null`. **Rot**, wenn `Update` das Feld ignoriert oder `null` als „bleibt" liest.
5. **Kind sieht Reihe und Seite:** Position auf einer Übung mit `bookPages: "42"` → in der Tagesübersicht der
   Student-Route `positions[i].bookPages == "42"` und `seriesName ==` Name der Reihe; eine zweite Position ohne
   Seite im selben Plan → `bookPages == null`, `seriesName` gesetzt. **Rot**, wenn `PositionProgressService` das
   Feld nicht projiziert oder das `Include` der Reihe fehlt (`seriesName == ""`).
6. **Bestand bleibt:** neue Migration hängt an `20261116065113_ForgivingDemotion` (B-219; bei der Schätzung war es noch `20261115215256_ExerciseItemExplanation`), Spalte nullable,
   Bestandsübungen lesen `bookPages: null`; `SchemaGuardTests` grün. **Rot** (G-Tore) bei Modell ohne Migration
   oder neu gefalteter Kette.
7. **Editor beidseitig:** `ExerciseEditModal` liest `bookPages` aus dem Detail ins Formular und schickt es im
   Payload zurück, leer → `null` (Vitest `ExerciseEditModal.test.ts`). **Rot**, wenn der Read-back fehlt – dann
   löschte jedes Speichern im Editor die Seite (PUT-Semantik).
8. **Anzeige beim Kind:** Positions-Blatt zeigt bei `bookPages: "42"` die Pille „Access 3 · Unit 2 · S. 42"
   (Reihe · Unit · S. Seiten), ohne Seite **keine** Pille (Vitest `SohnHome.test.tsx`). **Rot**, wenn die Pille
   immer oder nie erscheint.
9. *Absichtserklärung, kein Test:* Test-, Klausur- und Practice-DTOs (`Contracts/Student/TestDtos.cs`,
   `PracticeDtos.cs`) bekommen **kein** `bookPages` (Entscheidung 6). Eine Zusicherung darauf prüfte nur den
   Ausgangszustand und könnte nicht fehlschlagen.

## Schätzung

**Größe S** – reines Durchreichen ohne Fachlogik: ein Feld, eine Migration, drei Vertrags-Records + ein
`PositionStatus`, ein Editor-Feld, eine Pille. Kleiner als B-219 (M, dort neue Regel in `ApplyReview`), breiter
als der S-Anker B-01. `wo: beides`, `migration: ja` (eine nullable Spalte `BookPages`, angehängt; Bestand bleibt
`NULL`), `vertragsbruch: nein` (nur additive Felder mit Vorgabe am Ende).

### Risiken

| Risiko | Gegenmittel |
| --- | --- |
| Migration auf der echten `pugling.db` | nur `AddColumn` nullable; vor dem Commit gegen eine **Kopie** starten (Heimnetz-Regel). |
| PUT ohne `bookPages` löscht die Seite (Editor, `laden.py --ersetzen`) | AK 7 hält den Editor; `laden.py` → Folge-Story (Entscheidung 9), bis dahin im Skill-Text erwähnen ist **nicht** Teil dieser Story. |
| `DocsCaptureTests` überschreibt `docs/api-examples` mit dem neuen Feld | nachziehen und committen (D4-Tor). |
| `ExerciseDetail` hat bereits `CanAssign = false` als letzten Vorgabe-Parameter | `BookPages` dahinter anhängen, nicht davor – sonst verschieben sich positionale Aufrufer. |

### Angriffsplan

1. Entity: `Exercise.BookPages` (`string?`) hinter `Source` (`Models/LearnEntities.cs:127`), englische `///`;
   Migration anhängen: `dotnet dotnet-ef migrations add ExerciseBookPages --project backend/Pugling.Api --output-dir Data/Migrations`.
2. Vertrag: `ExercisePayload` (`Contracts/Creator/ExerciseAuthoringDtos.cs:26`–`32`), `ExerciseResponse` (`:44`–`49`),
   `ExerciseDetail` (`Contracts/Creator/ExerciseCatalogDtos.cs:61`–`68`), `PositionStatus`
   (`Contracts/Shared/ProgressDtos.cs:25`–`29`: `string SeriesName = "", string? BookPages = null`); `///` je Feld.
3. Controller: Längenprüfung neben `ExerciseControllerBase.cs:401`–`405` und `:494`; schreiben `:428`, `:517`;
   `Map` `:255`; Detail `ExerciseCatalogController.cs:224`.
4. Kind: `PositionProgressService.cs:192` um `.ThenInclude(u => u!.Series)` erweitern, `:245` projizieren.
5. Integrationstests AK 1–4 in `ExerciseMetadataTests`, AK 5 in `PositionGoalOverviewTests`; `docs/openapi/v1.json`
   und `docs/api-examples` neu erzeugen.
6. Frontend: `npm run gen:contract`; `ExerciseEditModal.tsx` `:250`/`:285`/`:311`/`:382` (Feld „Buchseite(n)"),
   Feldhilfe `exerciseBookPages` neben `fieldHelp.ts:207`; Pille in `SohnHome.tsx:300`–`303`, i18n-Schlüssel in
   `frontend/src/i18n/de.ts`/`en.ts` (`pfad`); Vitest AK 7–8.

### Testweg

`dotnet test Pugling.sln -c Release` (Stop-Hook) mit den neuen Fällen; Vitest `ExerciseEditModal.test.ts`,
`SohnHome.test.tsx`; `/smoke-test` mit einer Übung mit Seite. Rollengang: als Creator Seite setzen und nach dem
Speichern wieder sehen, als Sohn das Positions-Blatt öffnen und „… · S. 42" lesen.

## Verwandt

[B-375](B-375-heftseite-wird-uebungsentwurf.md) (Buchseite wird Übungsentwurf – liefert die Seite gleich
mit) · [B-580](B-580-plan-aus-unit.md) (Plan aus Unit) · [B-583](B-583-erklaerung-nach-falscher-antwort.md)
(Erklärung nach der Antwort, Sprung zum Stoff) · [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md)
(Frage an den Betreuer) · Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md).

**Folge-Stories (noch anzulegen):** (a) „Schon durchgenommen"-Merkmal aus `CurrentUnitId` plus Lehrwerk-Bezug in
Vater-Plan und Lernstand (Entscheidungen 7, 8); (b) `lehrwerk-aufbereiten`/`laden.py` und KI-Creator setzen
`bookPages` aus den Seiten-Markdowns mit (Entscheidung 9).

## Verlauf

- **2026-11-15** — angelegt (Quelle: Wunsch des Nutzers im Gespräch zum sofatutor-Vergleich; Anlass sofatutors Video-Hilfe, die von der Übung zur passenden Stelle springt).
- **2026-11-16** — Backlog-Lauf 2026-11-16: ausformuliert (Ist-Stand nachgemessen; Fund: freies `Source` existiert, `laden.py:452` setzt nur Reihe+Unit, PUT löscht fehlende Felder), gegrillt (neun Stakeholder-Entscheidungen im Auftrag des Nutzers; „schon durchgenommen", Vater-Anzeige und Import zurückgestellt, Titel auf den gelieferten Teil verengt) und geschätzt: **S**, `beides`, Migration ja (eine nullable Spalte), kein Vertragsbruch.
- **2026-11-16** — in-arbeit (Backlog-Lauf 2026-11-16). Gebaut: `Exercise.BookPages` (nullable, Länge 20 im Controller geprüft, Spalte per Konvention 200), Migration `20261116071258_ExerciseBookPages` hängt an `20261116065113_ForgivingDemotion` (B-219, nach der Schätzung entstanden; AK 6 entsprechend korrigiert); Vertrag additiv (`ExercisePayload`/`ExerciseResponse`/`ExerciseDetail` je `BookPages = null` am Ende, `PositionStatus` mit `SeriesName = ""`, `BookPages = null`); Editor-Feld „Buchseite(n)" mit Feldhilfe `exerciseBookPages`; Pille im Positions-Blatt (`pfad.bookRef`, de/en). Volle Suite 1713/1713 grün, Vitest 1121/1121. Die Tests AK 1–4 gehen über die Vokabel-Route (`…/vocabulary`), Validierung und Schreiben sitzen typunabhängig in `ExerciseControllerBase`.
- AK 1, Anlegen mit Wert in Antwort und Katalog-Detail → `Buchseite_Beim_Anlegen_Steht_In_Antwort_Und_Detail` (`backend/Pugling.Api.Tests/ExerciseMetadataTests.cs:351`), M1 Detail ohne Feld (`ExerciseCatalogController.cs:238` → `null`) → rot.
- AK 2, Trim → `Buchseite_Wird_Getrimmt_Und_Leer_Heisst_Keine` (`ExerciseMetadataTests.cs:370`), M2 ohne `Trim()` (`ExerciseControllerBase.cs:248`) → rot; Fall Whitespace → `null`: dieselbe Datei:Zeile, M3 `IsNullOrEmpty` statt `IsNullOrWhiteSpace` (speichert `""`) → rot.
- AK 3, Länge bei POST → `Buchseite_Hoechstens_Zwanzig_Zeichen_Bei_Post_Und_Put` (`ExerciseMetadataTests.cs:387`), M4 Prüfung in Create aus (`ExerciseControllerBase.cs:420`) → rot; Fall PUT: dieselbe Datei:Zeile, M5 Prüfung in Update aus (`:513`) → rot.
- AK 4, PUT ersetzt → `Buchseite_Put_Ersetzt_Und_Ohne_Feld_Leert` (`ExerciseMetadataTests.cs:415`), M6 `??=` in Update (`ExerciseControllerBase.cs:537`) → rot; Fall PUT ohne Feld leert: dieselbe Datei:Zeile, M7 `?? exercise.BookPages` („null = bleibt") → rot.
- AK 5, Kind sieht Seite → `Tagesuebersicht_NenntReiheUndBuchseite_DerPosition` (`backend/Pugling.Api.Tests/PositionGoalOverviewTests.cs:131`), M8 Projektion `null` (`PositionProgressService.cs:244`) → rot; Fall Reihe geladen: dieselbe Datei:Zeile, M9 ohne `.ThenInclude(u => u!.Series)` (`:192`) → rot.
- AK 6, Modell und Kette stimmen → `SchemaGuardTests.Modell_Und_Migrationen_Stimmen_Ueberein` (`backend/Pugling.Api.Tests/SchemaGuardTests.cs:33`), M10 Snapshot-Spalte umbenannt (`PuglingDbContextModelSnapshot.cs:632`) → rot; Bestandsübung liest `null`: abgedeckt durch die zweite Position in AK 5.
- AK 7, Read-back ins Formular → „liest die Seite ins Formular und schickt eine Änderung zurück" (`frontend/src/vater/ExerciseEditModal.test.ts:153`), M11 `bookPages: ""` in `initialForm` (`ExerciseEditModal.tsx:316`) → rot (fällt mit der Voll-Nutzlast-Zusicherung `:54` zusammen); Fall Änderung wird geschickt: dieselbe Datei:Zeile, M12 `detail.bookPages` statt Formular (`:252`) → rot; Fall leer → `null`: `ExerciseEditModal.test.ts:160`, M13 ohne `|| null` (`:252`) → rot.
- AK 8, Pille mit Seite → „zeigt Reihe, Unit und Seite als Pille – und ohne Seite keine" (`frontend/src/sohn/SohnHome.test.tsx:143`), M14 `{false && (` (`SohnHome.tsx:304`) → rot; Fall ohne Seite keine Pille: dieselbe Datei:Zeile, M15 `{true && (` → rot.
- AK 9 bleibt Absichtserklärung ohne Test: `TestDtos.cs`/`PracticeDtos.cs` sind unverändert.
- Mutanten-Probe (`.claude/scripts/mutant-probe.py`, Debug), gemessen an `58fec04b + Arbeitsbaum geändert`, 2026-11-16 08:29: M1–M15 erwartet rot, gemessen rot, jeweils am benannten Test; Grundlinie nach dem Zurückspielen grün.
