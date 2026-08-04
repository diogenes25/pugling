---
tags: [typ/story, status/geschaetzt, bereich/katalog, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Lehrwerk-Hierarchie, Verlag-Reihe-Band-Unit]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: ja
quelle: remark #2, #3, #4, #5, #6, #7 (+ #10 zweite Hälfte)
---

# B-63 · Das Lehrwerk ist eine Ebene aus Freitext, gebraucht wird eine Hierarchie mit Listen

Sechs Anmerkungen aus **einer** Testsitzung an derselben Seite (`/vater/lehrwerke`) sagen dasselbe aus
verschiedenen Richtungen. Die Häufung ist das Signal: das Lehrwerk trägt heute eine flache Ebene mit
Freitextfeldern, erwartet wird eine Hierarchie mit kontrollierten Listen.

## User Story

Als Vater möchte ich ein Lehrwerk als **Verlag → Fach → Reihe → Band → Units** anlegen und dabei aus
gepflegten Listen wählen statt zu tippen, damit dieselbe Reihe wiederverwendbar bleibt, statt in fünf
Schreibweisen zu zerfallen.

## Ist-Stand am Code

**Struktur** — Band und Unit liegen **bewusst** auf einer Ebene:

> „Band und Unit liegen bewusst auf **einer** Ebene (`Grade` = Band): ‚Access 8, Unit 3' ist eine Zeile,
> kein zweistufiger Baum."
> — [CurriculumEntities.cs:44-47](../../backend/Pugling.Api/Models/CurriculumEntities.cs), dieselbe Aussage
> in [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md) → „Unterrichtsmaterial &
> Creator-Profile".

- `TextbookSeries.Publisher` ist ein `string?` (`CurriculumEntities.cs:22`) — der Verlag ist **keine Ebene**.
- `TextbookSeries.SubjectId` ist nullable (`:26`) — Fach → Reihe ist eine optionale Verknüpfung, keine
  Hierarchie.
- `SeriesUnit.Grade` trägt den Band (`:57`), `SeriesUnit.OrderIndex` die Reihenfolge darin (`:59`).

**Freitext, wo Listen erwartet werden:**

- `SeriesUnit.Topics`, `.Grammar`, `.VocabularyNotes` sind je ein `string?`
  (`CurriculumEntities.cs:63,65,67`); im UI je ein einzeiliges Feld bzw. `textarea`
  ([VaterLehrwerke.tsx:273-292](../../frontend/src/vater/VaterLehrwerke.tsx)).
- Eine **Grammatik-Entität existiert nicht** — „Grammar" im Backend ist ausschließlich der Übungstyp
  ([BuiltInExerciseTypes.cs:52](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs),
  [ExerciseConfigs.cs:112](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs),
  [ExerciseControllers.cs:343](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)).
- `TextbookSeries.SourceLanguage`/`.TargetLanguage` sind `string?` (`CurriculumEntities.cs:31,33`), im UI
  Freitext ([VaterLehrwerke.tsx:375,379](../../frontend/src/vater/VaterLehrwerke.tsx)).

**Formular und Liste:**

- Reihenfolge im Anlege-Formular: Reihe, Verlag, Fach, Schulart, Lernsprache, Muttersprache, Notiz
  ([VaterLehrwerke.tsx:352-384](../../frontend/src/vater/VaterLehrwerke.tsx)). Fach (`:361`) und Schulart
  (`:368`) sind bereits Pulldowns.
- Übersichtstabelle: Reihe · Fach · Schulart · Units (`:56`) — **kein Band**; der steht nur in der
  aufgeklappten Unit-Tabelle (`:152` Kopf, `:158` Wert).
- Suche kennt `search` (Name/Verlag), `subjectId` und `mineOnly`
  ([TextbookSeriesController.cs:48-50](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)) —
  weder Schulart noch Verlag noch Band sind filterbar.

**Was mit hängt:**

- `Textbook.SeriesId`/`CurrentUnitId` am Kind zeigt auf **Reihe + Unit**
  ([AdminEntities.cs:168-175](../../backend/Pugling.Api/Models/AdminEntities.cs)).
- `CreatorProfile.SeriesId` (`CurriculumEntities.cs:98`) und das Matching-Gewicht „Reihe 8"
  (`CreatorProfileService`, siehe [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md)).
- Eine `TextbookSeries → SeriesUnit`-Kette ist genau zweistufig (`CurriculumEntities.cs:41,51`) — weitere
  Bücher derselben Reihe (Lernbuch, Übungsbuch) kennt das Modell nicht.

## Die echte Lücke

Nicht „eine Ebene fehlt", sondern: **der geteilte Katalog ist auf Wiederverwendung ausgelegt, seine Felder
sind es nicht.** Der Slug macht die Reihe idempotent (`CurriculumEntities.cs:19-20`), aber Verlag, Sprachen,
Themen und Grammatik sind Freitext — zwei Väter, die dieselbe Reihe beschreiben, erzeugen zwei
Beschreibungen, und nichts daran ist verknüpfbar.

Bemerkenswert: **den Band gibt es schon** — auf der Kind-Seite (`Textbook.Grade`,
[AdminEntities.cs:161](../../backend/Pugling.Api/Models/AdminEntities.cs)), nur nicht im geteilten Katalog.

Die Reihenfolge ergibt sich damit von selbst: **#7 ist der Träger.** #6 wird erst danach eindeutig (heute
kann eine Reihe mehrere Bände tragen, eine Spalte müsste aggregieren), #2 braucht die Verlag-Ebene, und
die Anmerkungen #3, #4 und #5 sind derselbe Schnitt eine Ebene tiefer. Einzeln gebaut wären das drei
Migrationen an derselben Tabelle.

## Offene Punkte

1. ~~Wird die dokumentierte Entscheidung wirklich umgekehrt?~~ → siehe Entscheidung 1 (differenziert
   beantwortet: teilweise).
2. ~~Verlag als eigene Tabelle oder als kontrollierte Liste?~~ → siehe Entscheidung 2.
3. ~~Grammatik-Themen: geteilte Taxonomie oder pro Reihe?~~ → siehe Entscheidung 3 (Antwort steht fest,
   Umsetzung zurückgestellt).
4. ~~Themen der Unit: Array von Freitext oder eigene Entität?~~ → siehe Entscheidung 4.
5. ~~Sprachen: feste Liste oder ISO-Codes?~~ → siehe Entscheidung 5.
6. ~~Weitere Buchtypen derselben Reihe~~ → siehe Entscheidung 6.
7. ~~Zweite Hälfte von Anmerkung #10~~ → siehe Entscheidung 7 (zurückgestellt, eigener Umbau).
8. ~~Ist das eine Karte statt einer Grill-Runde?~~ → siehe Entscheidung 9: eine Grill-Runde genügte, keine
   Karte nötig.

## Entscheidungen

1. **Ist „Verlag → Fach → Reihe → Band → Unit" wirklich ein Baum?** Nein, korrigiert: Verlag und Fach sind
   zwei **unabhängige, optionale Dimensionen** einer Reihe, kein verschachtelter Pfad — ein Verlag bedient
   mehrere Fächer, ein Fach hat Reihen mehrerer Verlage (das steht im Code bereits so:
   `TextbookSeries.SubjectId` ist ein einzelner nullable FK neben `Publisher`, nicht `Publisher.Subjects`).
   Echte Baum-Tiefe beginnt erst bei Reihe → Unit. Die dokumentierte Ein-Ebenen-Entscheidung für **Band +
   Unit** (`CurriculumEntities.cs:44-47`) bleibt inhaltlich richtig und wird **nicht** aufgehoben: keine der
   sechs Anmerkungen nennt einen Bedarf an bandeigenen Metadaten (eigene ISBN, eigenes Cover je Band) — der
   Schmerz war Sichtbarkeit/Filterbarkeit (Entscheidung 7), nicht fehlende Struktur. Aufgehoben wird die
   Ein-Ebenen-Entscheidung nur dort, wo sie **Wiederverwendung** verhindert: Verlag (Entscheidung 2) und
   Grammatik (Entscheidung 3). Begründung: ein echtes `Volume`-Entity wäre eine weitere Migration, ein
   weiterer Contract-Typ und eine weitere UI-Ebene für einen Bedarf, den niemand belegt hat — YAGNI gegen
   ein bewusstes, dokumentiertes Nein. Kosten: keine zusätzliche Tabelle; die „Hierarchie" der User Story
   wird über zwei neue Filter-Dimensionen (Verlag, Grammatik-Konzept) plus eine Ebene bereits vorhandener
   Struktur (Band, sichtbar gemacht) eingelöst, nicht über einen tieferen Baum.
2. **Verlag als eigene Entität.** Neue Tabelle `Publisher` (slug-idempotent, Muster `InterestTag`/
   `TextbookSeries`: `Id, Name, Slug, CreatedAt`, `OwnerAdultId` **nicht** nötig — ein Verlagsname ist keine
   Autorenschaft, jeder Creator darf ihn anlegen wie einen `InterestTag`). `TextbookSeries.PublisherId`
   (nullable FK, `SetNull` beim Löschen wie `SubjectId`) ersetzt `TextbookSeries.Publisher` (`string?`).
   Begründung: nur eine Entität macht „welche Reihen führt Cornelsen" und „welcher Verlag deckt Englisch
   ab" beantwortbar, ohne fünf Schreibweisen gegeneinander zu vergleichen — exakt das Argument, das die
   Anmerkungen für die Reihe selbst schon gewonnen haben. Kosten: neuer Controller
   `api/v1/creator/publishers` (CRUD-Kopie von `InterestTagsController`, ohne die Nutzungs-Facetten),
   neue Contracts (`PublisherResponse`, `CreatePublisherDto`), `CreateTextbookSeriesDto`/
   `UpdateTextbookSeriesDto` tauschen `Publisher`/`SubjectName`-Freitext-Symmetrie teilweise gegen
   `PublisherId` (Vertragsbruch), Frontend-Formular bekommt ein Kombo-Feld mit Inline-Anlage (Muster: die
   Reihe selbst, „gleicher Name = gleiche Zeile"), Seed (`Seed.cs:210`, `Publisher = "Klett"`) und
   `e2e/lehrwerke.spec.ts:13` (`publisher: "Cornelsen"` als Freitext) müssen auf die Auswahl umgestellt
   werden.
3. **Grammatik-Themen als geteilte Taxonomie — Antwort ja, Bau zurückgestellt.** Fachlich richtig wäre eine
   slug-idempotente `GrammarTopic`-Tabelle (Muster `VocabTag`) mit einer Join-Tabelle an `SeriesUnit`, weil
   „Present perfect" tatsächlich reihenübergreifend vorkommt. Das ist aber der teuerste Einzelposten dieser
   Story (zwei neue Tabellen, ein neuer Controller, ein Multi-Select im Formular, eine Migration der
   Bestandswerte) und macht aus einer **L**- eine **XL**-Story. *Ausdrücklich zurückgestellt*: `SeriesUnit.
   Grammar` bleibt vorerst `string?` (Freitext), die Taxonomie wird eine eigene Folge-Idee. Kosten: der
   sprachübergreifende Wiedererkennungswert bleibt bis dahin ungenutzt — kein Rückschritt gegenüber heute,
   nur kein Fortschritt an dieser Stelle.
4. **Themen der Unit werden eine Liste, keine eigene Entität.** `SeriesUnit.Topics` wechselt von `string?`
   auf `List<string>` (JSON-Spalte **mit** `ValueComparer`, siehe Fallstrick „JSON-Spalten" in der
   Root-`CLAUDE.md` — sonst gehen In-Place-Änderungen am Frontend-Array still verloren). Begründung: Themen
   sind buchspezifisch, eine geteilte Taxonomie wie bei Grammatik bringt hier keinen Wiederverwendungswert
   („Reading a Tube map" taucht nicht in einer zweiten Reihe auf). Kosten: Typwechsel im Contract
   (`SeriesUnitResponse.Topics` `string?` → `List<string>`) ist **nicht additiv** → Vertragsbruch; Frontend
   wechselt vom Einzelfeld zu einer Chip-/Tag-Eingabe (Muster existiert schon im Vokabel-Tag-Editor).
5. **Sprachen: dieselbe geschlossene Liste wie der Vokabel-Store, kein neues Schema.**
   `TextbookSeries.SourceLanguage`/`.TargetLanguage` bleiben `string`, aber das Formular
   (`VaterLehrwerke.tsx:374-379`) ersetzt die zwei freien `<input>` durch `<select>` aus
   `frontend/src/lib/languages.ts` (`LANGUAGES`: `de`/`en`/`fr`/`la`) — exakt das Muster, das
   [B-17](B-17-birkenbihl-sprachcodes.md) heute (2026-08-04) für das Birkenbihl-Formular derselben Datei
   beschließt. Beide Stories bleiben unabhängig **umsetzbar** (verschiedene Formulare, kein gemeinsamer
   Code-Pfad), sind aber **derselbe Fix an zwei Stellen** — beim Bauen in einem Aufwasch erledigen, falls
   beide anstehen. Begründung: keine neue Infrastruktur nötig, das Picklist-Muster existiert bereits.
   Kosten: keine Migration (Feldtyp bleibt `string`), kein Vertragsbruch (nur UI-Steuerung ändert sich) —
   die einzige „billige" Entscheidung dieser Story.
6. **Weitere Buchtypen als Feld, nicht als Ebene.** `SeriesUnit` bekommt `BookType`
   (Enum-als-String-Konvention, Werte `Textbook`/`Workbook`/…, Default `Textbook` — deckt den heutigen
   impliziten Fall verlustfrei ab). Begründung: eine eigene Ebene unter dem Band verdoppelt die gerade in
   Entscheidung 1 bewusst vermiedene Tiefe für denselben Bedarf, den ein Feld schon deckt. Kosten: additive
   Spalte mit DB-Default — Migration ja, aber kein zusätzlicher zweiter DB-Default (die Root-`CLAUDE.md`
   erlaubt bewusst nur einen, `Exercises.ExecutePublic`); dieser hier bräuchte einen zweiten und verletzt
   damit die Konvention der `SchemaGuardTests` — **Korrektur**: das Feld startet ohne DB-Default, C#
   `= BookType.Textbook` reicht (neue Zeilen bekommen ihn ohnehin serverseitig gesetzt, ein leerer
   Altbestand existiert wegen der Neufaltung nicht).
7. **Übersicht und Suche.** Die Lehrwerk-Tabelle zeigt zusätzlich die im Katalog vorhandenen Bände einer
   Reihe als aggregierten Chip/Bereich (z. B. „Klasse 7–9", berechnet über `Min`/`Max` von
   `SeriesUnit.Grade`, keine neue Spalte). `GET creator/textbook-series` bekommt zusätzliche Filter
   `publisherId`, `schoolTypes` und `grade` neben dem bestehenden `search`/`subjectId`/`mineOnly`
   ([TextbookSeriesController.cs:47-53](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)).
   Begründung: löst direkt die in den Anmerkungen benannte Lücke „weder Schulart noch Verlag noch Band sind
   filterbar". Kosten: additive Query-Parameter (kein Vertragsbruch für sich genommen), eine neue
   `IQueryable`-Aggregation in `Project(...)`.
8. **Zweite Hälfte von Anmerkung #10 (Klassenstufe/Schulart vom `Exercise` ans `Chapter`) bleibt
   zurückgestellt**, wie in der Ausformulierung empfohlen: `Chapter` trägt heute nur `Name`+`OrderIndex`
   ([LearnEntities.cs:35-44](../../backend/Pugling.Api/Models/LearnEntities.cs)), das ist ein Umbau am
   Übungs-Katalog, nicht am Lehrwerk-Modell dieser Story. Kosten: keine, solange niemand das Feld doppelt
   pflegt (heute nicht der Fall).
9. **Grill-Runde statt Karte.** Mit der Klärung aus Entscheidung 1 (keine tiefere Baum-Struktur, sondern
   zwei neue Wiederverwendungs-Entitäten plus Sichtbarkeit) lösten sich die ursprünglich vermuteten
   Abhängigkeiten in einer Sitzung auf — Punkt 3 (Grammatik) hing tatsächlich von Punkt 1 ab, ist aber durch
   Zurückstellen statt durch Verwerfen erledigt. Keine Karte nötig.
10. **Überlappung mit [B-64](B-64-textbook-vs-series.md) — kein Duplikat, aber eine echte Kopplung.** B-64
    fragt „Textbook (Kind, Freitext) vs. TextbookSeries (Katalog) — welcher Weg gewinnt beim Anlegen des
    Kind-Buchs?", diese Story hier baut die **innere Struktur** des Katalogs selbst (Verlag, Grammatik,
    Themen, Band-Sichtbarkeit) um. Unterschiedliche Befunde, keine Dopplung — B-64 sagt das selbst bereits
    korrekt voraus („Reihenfolge zu B-63: danach", B-64 Punkt 4). **Wichtig für den B-64-Bearbeiter:** durch
    Entscheidung 2 hier bekommt `TextbookSeries.Publisher` ein `PublisherId`-Gegenstück; `Textbook.Publisher`
    (Kind-Seite, `AdminEntities.cs:161`) bleibt unberührter Freitext. B-64s eigener Punkt 3 („Was passiert
    mit `Textbook.Grade`/`Publisher`, wenn eine Reihe gewählt ist? Doppelte Wahrheit") bekommt dadurch einen
    zweiten Kandidaten (`Publisher` **und** `Grade`) statt nur einem — B-64 sollte das bei seiner eigenen
    Grill-Runde mit diesem aktuellen Code-Stand neu ansehen, nicht mit dem vor dieser Story.

## Akzeptanzkriterien

1. Verlag ist eine eigene, geteilte, slug-idempotente Entität (`Publisher`, CRUD unter
   `api/v1/creator/publishers`); `TextbookSeries.PublisherId` ersetzt das Freitextfeld, das
   Anlege-/Änderformular bietet Auswahl **und** Inline-Anlage einer neuen Verlags-Zeile.
2. Verlag und Fach sind zwei unabhängige Auswahlfelder der Reihe (kein Pfad, der eines vom anderen
   abhängig macht).
3. `TextbookSeries.SourceLanguage`/`.TargetLanguage` sind `<select>` aus derselben `LANGUAGES`-Liste wie
   der Vokabel-Store, kein Freitext mehr.
4. `SeriesUnit.Topics` ist eine Liste; `SeriesUnit` trägt ein `BookType`-Feld (Default: Lehrbuch).
5. Die Lehrwerk-Übersicht zeigt die in einer Reihe vorhandenen Bände (aggregiert).
6. Die Suche (`GET creator/textbook-series`) filtert zusätzlich nach `publisherId`, `schoolTypes` und
   `grade`.
7. `Textbook` am Kind und `CreatorProfile` bleiben funktionsfähig; das Fachlehrer-Matching liefert für
   denselben Datenstand dieselbe Empfehlung (Reihen-Gewicht unverändert, `series_match` weiterhin über
   `SeriesId`).
8. `SeriesUnit.Grammar` bleibt bewusst Freitext (Entscheidung 3) — **kein** Akzeptanzkriterium dieser
   Story, damit keine falsche Erwartung entsteht.

## Schätzung

**Größe: L** — zwei neue geteilte Entitäten (`Publisher`, plus das bewusst zurückgestellte
`GrammarTopic`, das die Größe sonst nach XL getrieben hätte), ein Feldtypwechsel (`Topics`), ein neues Feld
(`BookType`), erweiterte Filter/Aggregation und ein spürbar größeres Formular auf `VaterLehrwerke.tsx` —
vergleichbar mit einer eigenständigen DB-Umbau-Etappe, aber (dank Entscheidung 3) noch nicht XL.

- **`wo: beides`** — Backend zuerst (neue Entität, Feldtypwechsel, Filter), danach das Formular.
- **`migration: ja`** — neue Tabelle `Publisher`, `TextbookSeries.PublisherId` statt `.Publisher`,
  `SeriesUnit.Topics` Typwechsel, `SeriesUnit.BookType` neu. Kette wird **neu gefaltet**
  (`rm -rf backend/Pugling.Api/Data/Migrations` + `migrations add InitialCreate`), nicht verlängert.
- **`vertragsbruch: ja`** — `TextbookSeriesResponse`/`CreateTextbookSeriesDto`/`UpdateTextbookSeriesDto`
  verlieren `Publisher`, `SeriesUnitResponse`/`CreateSeriesUnitDto`/`UpdateSeriesUnitDto` ändern `Topics`
  auf `List<string>` und bekommen `BookType` — Frontend, `Pugling.Client` und die `unknown_field`-Guards
  ziehen nach.
- **Risiken:**
  - Bestandsdaten: der Seed (`Seed.cs:210`, `Publisher = "Klett"`) muss auf `PublisherId` umgestellt
    werden — sonst bricht `SeedContractTests.cs`. Kein Altdaten-Migrationsproblem, weil die Kette ohnehin
    neu gefaltet wird (Fallstrick „EF-Migrationen" der Root-`CLAUDE.md`).
  - `e2e/lehrwerke.spec.ts:13` befüllt `publisher` heute als Freitext-Input — bricht sicher, muss auf die
    neue Auswahl umgestellt werden; das ist der einzige Vater→Sohn-Durchstich für diese Seite, ein Ausfall
    hier bliebe sonst unbemerkt.
  - `BookType`-Default darf **kein** zweiter DB-Default werden (Root-`CLAUDE.md` erlaubt bewusst nur einen,
    `Exercises.ExecutePublic`) — der C#-Property-Default reicht, siehe Entscheidung 6.
  - `CreatorProfileService`-Matching-Gewichte dürfen sich nicht verschieben (Akzeptanzkriterium 7) — reiner
    Strukturumbau an `TextbookSeries`/`SeriesUnit`, `CreatorProfile.SeriesId` selbst ändert sich nicht.
- **Angriffsplan** (Backend zuerst):
  1. `Publisher`-Entität + Migration (neu falten) + Controller + Contracts.
  2. `TextbookSeries.PublisherId` statt `.Publisher`; Seed anpassen.
  3. `SeriesUnit.Topics` → `List<string>` (+ `ValueComparer`), `SeriesUnit.BookType` neu.
  4. Filter/Aggregation in `TextbookSeriesController.List`/`Project`.
  5. Frontend: Verlags-Kombo mit Inline-Anlage, Sprach-`<select>`, Themen-Chips, `BookType`-Auswahl,
     Band-Anzeige + Filterleiste in `VaterLehrwerke.tsx`.
  6. `e2e/lehrwerke.spec.ts` auf die neuen Formularelemente umstellen.
- **Testweg:**
  - Backend: neue Tests analog `InterestTaxonomyTests.cs` für den `Publisher`-CRUD; bestehende
    `CreatorProfileTests.cs` (nutzt bereits `CreateSeriesAsync`) um `PublisherId`/`BookType`/`Topics`-Liste
    erweitern; `CatalogReadDeleteTests.cs` für den geänderten Lösch-/Lesepfad; `PatchSemanticsTests.cs`
    prüft die neuen `Update…Dto`-Felder reflexiv mit; `SchemaGuardTests` hält Kettenlänge 1 und den
    JSON-`ValueComparer` von `Topics` (Fallstrick „JSON-Spalten") automatisch nach.
  - Frontend/E2E: `e2e/lehrwerke.spec.ts` (bestehend, muss angepasst werden) bleibt der Durchstich
    Reihe→Unit→Profil→Kind; `/smoke-test` vor dem Commit für den End-to-End-Rundgang.

## Verlauf

- **2026-08-02** — angelegt aus den Anmerkungen #2, #3, #4, #5, #6 und #7 (#10 als Punkt 7); Ist-Stand am
  Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#a--lehrwerk-modell--der-größte-block-2-3-4-5-6-7).
- **2026-08-04** — gegrillt: alle acht offenen Punkte in zehn nummerierte Entscheidungen überführt
  (Verlag → eigene Entität, Grammatik-Taxonomie inhaltlich bejaht aber zurückgestellt, Band/Unit-Ebene
  bewusst nicht aufgehoben, Sprachen über die bestehende `LANGUAGES`-Liste, Buchtyp als Feld,
  Übersicht/Suche erweitert, Chapter/Subject-Frage zurückgestellt, keine Karte nötig, Überlappung mit
  B-64 als Kopplung ohne Dopplung geklärt); jeder `Datei:Zeile`-Beleg des Ist-Stands gegen den heutigen
  Code nachgeprüft (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — geschätzt: Größe L (Grammatik-Taxonomie bewusst zurückgestellt, sonst wäre die Story
  XL geworden), `wo: beides`, `migration: ja`, `vertragsbruch: ja`; Risiken, Angriffsplan (Backend zuerst)
  und Testweg ergänzt (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — Querverweis: [B-106](B-106-lehrwerkgetriebener-katalog.md) verschmilzt `Exercise` mit
  `SeriesUnit`; diese Story bleibt voraussichtlich unabhängig (innere Reihen-Struktur, nicht die
  Kapitel-Zuordnung), bei `SeriesUnit`-Feldtypwechseln gegenprüfen — kein Status-Wechsel hier.
