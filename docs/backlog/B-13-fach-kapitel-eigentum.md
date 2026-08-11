---
tags: [typ/story, status/in-arbeit, bereich/katalog, bereich/auth, rolle/creator]
aliases: [Fach- und Kapitel-Eigentum]
status: in-arbeit
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: memory/geteilte-uebungs-bibliothek.md
grund: ""
ersetzt_durch: []
---

# B-13 · Fach- und Kapitel-Eigentum

`Subject`s und `Chapter`s sind global und ungeschützt — anders als Übungen (`AuthorAdultId` + Grants) und
Lehrwerk-Reihen (`OwnerAdultId`). Jeder Creator kann fremde Fächer und Kapitel ändern oder löschen.

## User Story

Als *Creator* möchte ich, dass nur ich (oder niemand, wenn es Systemstoff ist) ein Fach oder Kapitel
umbenennen oder löschen kann, damit ein fremder Creator nicht versehentlich oder mutwillig meinen
Katalog-Baum verändert, während das Anlegen und Lesen für alle frei bleibt.

## Ist-Stand am Code

- `Subject` und `Chapter` liegen beide in `Models/LearnEntities.cs` (nicht in `CurriculumEntities.cs`,
  das trägt nur die Lehrwerk-Reihen). `Subject` trägt **keinerlei** Eigentümer-Feld — nur `Id`, `Name`,
  `CreatedAt`, `Chapters`, `Categories`; `Chapter` ebenso nur `Id`, `SubjectId`, `Name`, `OrderIndex`,
  `Exercises` (`backend/Pugling.Api/Models/LearnEntities.cs:8-44`).
- `SubjectsController.Update`/`.Delete` lesen die Entity nur per `FirstOrDefaultAsync`/`FindAsync` und
  schreiben/löschen **ohne jede Eigentümer- oder Rollen-Prüfung außer dem globalen
  `[Authorize(Roles = Roles.Creator)]`** — jeder eingeloggte Creator trifft jedes Fach
  (`backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:57-94`).
- `ChaptersController.Update`/`.Delete` verhalten sich identisch — nur `subjectId`+`chapterId` werden
  geprüft, kein Ersteller (`backend/Pugling.Api/Controllers/Creator/ChaptersController.cs:73-109`).
- Beide `Create`-Actions setzen ebenfalls **keinen** Ersteller — ein neu angelegtes Fach/Kapitel trägt
  keine Spur, wer es angelegt hat (`SubjectsController.cs:44-54`, `ChaptersController.cs:55-70`).
- Der Vergleich, den die Idee einfordert, existiert **zweimal im Code**, nicht nur einmal:
  1. **Volles RWX** bei `Exercise`: `AuthorAdultId` (Attribution) + `ExerciseGrant`
     (Owner/Write/Execute-Rechte je Creator) + `ExecutePublic`-Schalter fürs kontrollierte Teilen
     (`backend/Pugling.Api/Models/LearnEntities.cs:98-141`). Durchgesetzt über
     `EnsureCanWrite`/`EnsureCanAdminister` in `ExerciseControllerBase.cs:107-119`.
  2. **Einfaches Einzel-Eigentum** bei `TextbookSeries`: ein nullable `OwnerAdultId`
     (`backend/Pugling.Api/Models/CurriculumEntities.cs:37`), durchgesetzt in
     `TextbookSeriesController.Update`/`.Delete` über
     `ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, User.CreatorId())` →
     `this.ProblemWithCode(ApiErrors.NotOwner, …)`
     (`backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:120-171`).
  `IsOwnedBy` ist **fail-closed**: fehlt der Autor (`null`, seeded/systemeigen) oder das `fid`, liefert der
  Vergleich `false` — ein ownerloser Datensatz ist dann **niemandem** zugänglich, nicht etwa allen
  (`backend/Pugling.Api/Auth/AuthAccess.cs:84-91`).
- `ApiErrors.NotOwner` (Code `not_owner`, 403) existiert bereits und wird von `TextbookSeries` und
  `Exercise` geteilt (`backend/Pugling.Api/Errors/ApiErrors.cs:36`) — für B-13 ist kein neuer Fehlercode
  nötig.
- Die Seed-Daten legen alle Subjects/Chapters ohne Eigentümer an (`new Subject { Name = "Englisch", … }`,
  `new Chapter { Name = "Unit 1 – Greetings", … }`, `backend/Pugling.Api/Data/Seed.cs:605-1152`, keine
  Owner-Zuweisung) — sie werden nach dieser Story für **jeden** Creator schreibgeschützt, nicht nur für
  fremde.
- `CatalogManagementTests.cs` legt in jedem Testfall Subject/Chapter über **denselben** `father`-Client an
  und ändert/löscht sie danach mit demselben Client (`backend/Pugling.Api.Tests/CatalogManagementTests.cs:31-140`)
  — die bestehenden Tests bleiben also grün, sobald `Create` den Aufrufer als Owner einträgt.

**Ergebnis der Recherche:** Die „Ungeprüft"-Frage der Idee („ist ein Owner überhaupt gewollt?") ist am
Code eindeutig mit *ja* zu beantworten — Übung und Lehrwerk-Reihe demonstrieren beide dasselbe Muster,
und Fach/Kapitel sind die einzigen zwei Katalog-Ebenen, die es **nicht** übernommen haben.

## Die echte Lücke

Nicht, *ob* ein Owner sinnvoll ist (er ist es, zweifach vorgemacht), sondern **welches der beiden
Muster** passt. `Exercise`s RWX-Modell existiert, weil Übungen geteiltes Miteigentum, gestaffelte Rechte
(Write vs. Owner) und einen Sichtbarkeits-Schalter (`ExecutePublic`) brauchen — keiner dieser drei Gründe
trifft auf `Subject`/`Chapter` zu: ein Fach hat keine Miturheber, keine abgestufte Schreibrolle und
lesen darf ohnehin jeder Creator schon heute uneingeschränkt. Das einfache `TextbookSeries`-Muster
(ein nullable `OwnerAdultId`, `IsOwnedBy`, `NotOwner`) trifft die Lücke exakt und ist bereits zweimal
im Code erprobt (`Exercise.AuthorAdultId` als Attribution, `TextbookSeries.OwnerAdultId` als
alleiniger Owner-Check) — es muss nur auf zwei weitere Entities kopiert werden, nicht neu erfunden.

## Offene Punkte

1. ~~Ist ein Owner an Subject/Chapter überhaupt gewollt, oder ist „Englisch" von Natur aus geteilt?~~ →
   siehe Entscheidung 1: ja, aber nur für Umbenennen/Löschen — Lesen bleibt global.

2. ~~Muss nur Umbenennen/Löschen geschützt werden, während Anlegen frei bleibt?~~ → siehe Entscheidung 2:
   ja, Anlegen bleibt ungegatet (deckt sich mit `Exercise`, wo `Create` ebenfalls nie geprüft wird).

3. Was passiert mit den Seed-Fächern/-Kapiteln, die keinen Owner haben? → siehe Entscheidung 3.

4. Braucht die API einen Weg, ein ownerloses Fach/Kapitel nachträglich zu adoptieren? → siehe
   Entscheidung 4.

5. Muss das Frontend in diesem Schnitt mitziehen (Edit/Löschen-Knöpfe an fremden Fächern ausblenden)? →
   siehe Entscheidung 5.

## Entscheidungen

1. **Owner-Modell: einfaches `OwnerAdultId` (TextbookSeries-Muster), kein volles RWX.** Begründung: Fach
   und Kapitel haben keine Miturheber, keine gestaffelten Schreibrollen und keinen
   Sichtbarkeits-Schalter — die drei Gründe, aus denen `ExerciseGrant` existiert, treffen hier nicht zu.
   Kosten: Ein Fach kann (vorerst) nicht an mehrere Creator geteilt werden — falls das später gebraucht
   wird, ist eine Migration auf ein Grant-Modell nötig (dieselbe Historie wie bei `Exercise`, das früher
   auch nur `AuthorAdultId` hatte).
2. **Anlegen bleibt ungegatet, nur Update/Delete werden geschützt.** Begründung: deckungsgleich mit
   `Exercise.Create` (nie geprüft) und dem Bedürfnis, dass jeder Creator ein neues Fach/Kapitel eröffnen
   darf, ohne auf eine Freigabe zu warten. Kosten: keine — `Create` trägt ohnehin schon keine Prüfung,
   es kommt nur die Owner-**Zuweisung** (`OwnerAdultId = User.AdultId()`) hinzu.
3. **Ownerlose Datensätze (`OwnerAdultId == null`, alle heutigen Seed-Fächer/-Kapitel) werden nach der
   Migration für ALLE Creator schreibgeschützt**, nicht automatisch dem ersten Zugreifenden zugewiesen
   und nicht weiterhin offen gelassen. Begründung: `IsOwnedBy` ist bereits fail-closed
   (`AuthAccess.cs:90-91`) — genau dieselbe Semantik, die `Exercise.AuthorAdultId == null` („seeded
   system exercise … not editable") und `TextbookSeries.OwnerAdultId == null` schon tragen. Eine
   Sonderregel nur für Subject/Chapter wäre eine dritte, abweichende Owner-Semantik im selben Katalog.
   Kosten: Bestehende Seed-Fächer wie „Englisch"/„Mathe" sind ab sofort für **niemanden** umbenennbar
   oder löschbar, auch nicht für den Seed-Vater-Account — das ist eine bewusste Verschärfung
   (das *ist* der Zweck der Story: heute kann sie jeder Creator kaputt machen, künftig niemand mehr ohne
   Weiteres). Migrationsskript setzt `OwnerAdultId` für Altdaten **nicht** nachträglich.
4. **Kein Adoptions-Endpoint in diesem Schnitt.** Begründung: `Exercise` hat für denselben Fall (ownerlose
   Übung) ebenfalls keinen "adopt"-Weg — ein Creator, der ein Seed-Fach übernehmen will, legt heute
   ohnehin ein neues Kapitel/Fach an. Kosten: Wer ein Seed-Fach zwingend umbenennen möchte, hat dafür
   (noch) keinen API-Weg; das ist eine Folge-Idee wert, falls es in der Praxis auffällt (neue `idee`,
   kein Blocker hier).
5. **Frontend bleibt in diesem Schnitt unangetastet (`wo: backend`).** Begründung: Die Story schließt eine
   Sicherheitslücke (fremder Creator ändert mein Fach), sie fügt keinen neuen Endpunkt hinzu, den es noch
   keine UI dafür gibt — dieselbe Einordnung wie bei B-80/B-81/B-82 (Tags/Report-Lecks), die ebenfalls
   `wo: backend` blieben. Kosten: Das Vater-Web zeigt Edit/Löschen-Buttons weiterhin an fremden
   Fächern/Kapiteln an; ein Klick liefert künftig `403 not_owner` statt stillschweigend zu klappen. Kein
   Regressionsrisiko (das Verhalten war vorher nie anders abgesichert), aber ein UX-Nachtrag bleibt offen
   — als eigene Folge-Idee zu erfassen, falls es beim Testen auffällt.

## Nach B-106

B-106 (abgenommen 2026-08-05) hat `Chapter` als Entität vollständig entfernt (`ChaptersController.cs`
existiert nicht mehr, `Exercise.ChapterId` ist `Exercise.SeriesUnitId`,
[LearnEntities.cs:37](../../backend/Pugling.Api/Models/LearnEntities.cs)) — die Hälfte dieser Story
(„Kapitel-Eigentum") hat ihren Gegenstand verloren, nicht nur ihren Namen.

**Der befürchtete Nachfolge-Schaden ist bereits behoben, nicht neu offen:** B-106 hat beim Bau von
`SeriesUnitsController` selbst genau das TextbookSeries-Owner-Muster übernommen, das diese Story für
Chapter vorschlug — nachgeprüft (2026-08-06): `Update`/`Delete` prüfen
`IsOwnedBy(unit.Series?.OwnerAdultId, User.CreatorId())` und liefern sonst `403 not_owner`
([SeriesUnitsController.cs:97-98,132-133](../../backend/Pugling.Api/Controllers/Creator/SeriesUnitsController.cs)),
XML-Doc der Klasse: „Any creator may read, only the owner of the series may write." Der Lücken-Fall
dieser Story trifft auf `SeriesUnit` also **nicht mehr zu** — er ist über die ohnehin vorhandene
`TextbookSeries.OwnerAdultId` bereits geschlossen, ohne dass diese Story dafür etwas bauen müsste.

**Was unverändert offen bleibt: nur `Subject`.** Nachgeprüft (2026-08-06,
[SubjectsController.cs:44-84](../../backend/Pugling.Api/Controllers/Creator/SubjectsController.cs)):
`Create` setzt weiterhin keinen Ersteller, `Update`/`Delete` prüfen weiterhin nichts außer der Existenz
— jeder Creator kann jedes Fach umbenennen oder löschen, exakt wie im ursprünglichen Ist-Stand
beschrieben. `Subject` trägt weiterhin kein Eigentümer-Feld.

**Empfehlung: neu schneiden, nicht verwerfen.** Der Kern der Idee (ein global ungeschütztes
Katalog-Objekt) trifft heute nur noch auf `Subject` zu — eine Story, die „Subject-Eigentum" statt
„Fach- und Kapitel-Eigentum" heißt, mit ungefähr einem Drittel des ursprünglichen Angriffsplans (nur
Schritt 1/3/4/5/6 für `Subject`, nichts für `Chapter`/`SeriesUnit`). Das ist eine Produktentscheidung
(ist das verbliebene Risiko — ein Creator löscht ein fremdes „Englisch" — groß genug für eine eigene
Story, oder zu klein, um sie noch zu lohnen?), die diese Nacht nicht autonom trifft.

## Akzeptanzkriterien

Der Kapitel-Anteil jedes Kriteriums ist ~~durchgestrichen~~: `Chapter` existiert seit B-106 nicht mehr, und
`SeriesUnit` trägt die Owner-Prüfung bereits (siehe „Nach B-106"). Gebaut wurde der `Subject`-Anteil.

1. `POST /api/v1/creator/subjects` ~~und `POST /api/v1/creator/subjects/{id}/chapters`~~ setzt
   `OwnerAdultId = User.CreatorId()` am neuen Datensatz; das Erstellen selbst bleibt für jeden Creator frei
   (keine 403 auf `Create`).
2. `PATCH`/`DELETE` auf ein Subject ~~oder Chapter~~ liefert `403` mit Code `not_owner`, wenn der aufrufende
   Creator nicht der `OwnerAdultId` ist — inklusive dem Fall `OwnerAdultId == null` (Seed-/Systemstoff:
   niemand darf ändern).
3. Der Owner selbst kann sein eigenes Subject ~~/Chapter~~ weiterhin ungehindert `PATCH`en und `DELETE`n
   (bestehendes Verhalten für den Ersteller bleibt erhalten).
4. `GET`/`List` auf Subjects ~~und Chapters~~ bleibt für jeden eingeloggten Creator uneingeschränkt lesbar —
   kein Verhalten ändert sich für Lesezugriffe.
5. `SubjectResponse` ~~/`ChapterResponse`~~ trägt zusätzlich `ownerAdultId` (nullable) und `isMine` (bool,
   analog `TextbookSeriesResponse`), damit ein Client die Berechtigung ohne Rateversuch anzeigen kann.
6. Die EF-Migrationskette bleibt bei Länge 1 (neu gefaltet), `SchemaGuardTests` (G1/G1b/G2) sind grün mit
   der neuen FK (`Subject.OwnerAdultId → Adult`, `SetNull`) ~~und `Chapter.OwnerAdultId → Adult`~~.
7. Bestehende `CatalogManagementTests` bleiben grün (sie legen das Subject stets über denselben
   Client an, der es danach ändert/löscht — der neue Owner-Check greift nicht ein).
8. Neue Tests belegen: fremder Creator bekommt `403 not_owner` auf `PATCH`/`DELETE` eines fremden
   Subjects ~~/Chapters~~; ein Seed-Fach (ownerlos) liefert `403 not_owner` für **jeden** Creator.

## Schätzung

**Größe: M** — zwei Entities bekommen dieselbe, im Code bereits zweimal vorgemachte Owner-Spalte
(`TextbookSeries.OwnerAdultId` ist die nächstliegende Vorlage, praktisch 1:1 kopierbar), dazu eine
Migrationsfaltung, zwei Controller-Anpassungen (Create: Owner setzen; Update/Delete: `IsOwnedBy`-Gate)
und zwei DTO-Erweiterungen (additiv). Vergleichbar mit B-65 (Vokabel-Mehrfachübersetzung, M/Migration)
mehr als mit B-01 (S, reine Testpfad-Extraktion ohne Schema-Änderung).

- **`migration: ja`** — neue Spalte `OwnerAdultId` (nullable `int`) an `Subject` und `Chapter`, je eine
  FK auf `Adult` mit `OnDelete(DeleteBehavior.SetNull)` (Muster `TextbookSeries.cs:214-215`). Die Kette
  wird neu gefaltet (`rm -rf backend/Pugling.Api/Data/Migrations` + `migrations add InitialCreate`);
  `SchemaGuardTests` G2 (die gepinnte FK-Tabelle) muss um die zwei neuen Zeilen ergänzt werden — das Tor
  ist danach bewusst kurz rot, bis die Zeilen eingetragen sind.
- **`vertragsbruch: nein`** — `SubjectResponse`/`ChapterResponse` bekommen nur **zusätzliche** Felder
  (`ownerAdultId`, `isMine`); `CreateSubjectDto`/`CreateChapterDto`/`UpdateSubjectDto`/`UpdateChapterDto`
  bleiben unverändert (Owner wird serverseitig aus dem Token gesetzt, nicht aus dem Payload). Kein
  bestehendes Feld ändert Typ oder Name, kein Client muss angepasst werden, damit er weiter kompiliert.

**Risiken:**

- Die Verschärfung für Seed-Fächer (Entscheidung 3) ist bewusst, aber **sichtbar**: Wer heute gewohnt ist,
  ein Seed-Fach umzubenennen, bekommt ab sofort `403`. Das ist der Zweck der Story, nicht ein Nebeneffekt
  — trotzdem beim Smoke-Test gezielt gegenprüfen, damit es nicht als Regressionsfund auftaucht.
- `TeacherAccountsController`/`CreatorProfilesController` referenzieren `Subject` nur lesend
  (`SubjectId`-Verweis) — keine Rückwirkung erwartet, aber beim Bauen kurz gegenprüfen, dass keine
  Stelle `Subject`/`Chapter` außerhalb der beiden Controller schreibend anfasst
  (`grep -rn "db.Subjects.Add\|db.Chapters.Add\|\.SubjectId = \|chapter.Name ="`).

**Angriffsplan** (Backend zuerst, wie immer bei diesem Repo — hier gibt es kein Frontend-Teil):

1. `Models/LearnEntities.cs`: `OwnerAdultId`/`Owner`-Property an `Subject` und `Chapter` ergänzen
   (Muster `CurriculumEntities.cs:37-38`).
2. `PuglingDbContext.OnModelCreating`: FK + `SetNull` für beide (Muster `TextbookSeries`-Konfiguration,
   `PuglingDbContext.cs:214-215`); Migrationskette neu falten.
3. `Contracts/Creator/CatalogDtos.cs`: `SubjectResponse`/`ChapterResponse` um `OwnerAdultId`/`IsMine`
   erweitern (additiv).
4. `SubjectsController`/`ChaptersController`: `Create` setzt `OwnerAdultId = User.AdultId()`;
   `Update`/`Delete` prüfen `ClaimsPrincipalExtensions.IsOwnedBy(entity.OwnerAdultId, User.CreatorId())`
   und liefern sonst `this.ProblemWithCode(ApiErrors.NotOwner, …)` (1:1 Muster
   `TextbookSeriesController.cs:120-171`); Projektionen liefern `isMine` mit.
5. `SchemaGuardTests`: neue FK-Zeilen in der G2-Tabelle eintragen.
6. Tests: `CatalogManagementTests` (oder ein neuer `CatalogOwnershipTests`) um die Fälle „fremder Creator
   → 403 not_owner" und „ownerloses Seed-Fach → 403 für jeden" ergänzen (zweiter Creator-Client analog
   `ExerciseGrantsTests`, das bereits einen zweiten Adult-Login für Fremd-Zugriffe bereitstellt).

**Testweg**: Integrationstests in `Pugling.Api.Tests` (`CatalogManagementTests.cs`, ggf. erweitert um einen
Ownership-Testfall analog `ExerciseGrantsTests.cs` für den Zweit-Creator); `SchemaGuardTests` für die
Migrationskette und die G2-FK-Tabelle; `/smoke-test` gegen einen laufenden Server als Abschluss-Check
(Fach umbenennen als Owner klappt, als Fremder liefert 403).

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand mit Belegen recherchiert (`SubjectsController.cs`,
  `ChaptersController.cs`, `LearnEntities.cs`, `TextbookSeriesController.cs`, `AuthAccess.cs`,
  `ApiErrors.cs`, `Seed.cs`, `CatalogManagementTests.cs`); beide Owner-Muster im Code verglichen.
- **2026-08-03** — gegrillt: alle offenen Punkte in nummerierte Entscheidungen überführt, autonom
  getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe M, `wo: backend`, `migration: ja`, `vertragsbruch: nein`, Angriffsplan
  und Testweg festgelegt, autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-04** — Querverweis: [B-106](B-106-lehrwerkgetriebener-katalog.md) verschmilzt `Exercise` mit
  `SeriesUnit`; sobald `Chapter` entfällt, verliert diese Story ihren Chapter-Anteil (das Owner-Muster
  wandert sinngemäß auf `SeriesUnit`) — kein Status-Wechsel hier, Neubewertung bei B-106s Bau.
- **2026-08-06** — Nachtlauf, Prämissen-Nachprüfung nach B-106s Abnahme: `Chapter` existiert nicht mehr,
  `SeriesUnit` trägt bereits eine eigene Owner-Prüfung (B-106 hat sie beim Bau selbst ergänzt). Nur der
  `Subject`-Anteil ist unverändert offen. Empfehlung „neu schneiden" ins Dokument aufgenommen, `status`
  bewusst nicht geändert — das ist eine Produktentscheidung, keine Stufenarbeit.
- **2026-08-11** — gebaut, im Umfang „nur `Subject`" (Nutzerauftrag „build b-13"; der Kapitel-Anteil hat
  seinen Gegenstand verloren, die Akzeptanzkriterien sind entsprechend durchgestrichen).
  `Subject.OwnerAdultId`/`Owner` + FK `SetNull`, Migrationskette neu gefaltet (Snapshot-Diff zeigt genau
  Spalte, Index und FK), `SubjectResponse` additiv um `ownerAdultId`/`isMine`, `SubjectsController`:
  `Create` setzt den Owner aus dem Token, `Update`/`Delete` gaten über `IsOwnedBy` → `403 not_owner` (im
  `Delete` **vor** der Verwendungsprüfung), Projektion mit Inline-Ownership statt Methodenaufruf (EF).
  Neu `FachEigentumTests` (5 Fälle: Owner-Zuweisung, Owner darf weiter, Fremder 403 auf beiden Wegen,
  Fremder liest weiter, Seed-Fach für jeden gesperrt), G2-Tabelle um eine Zeile ergänzt.
  Belege: **820/820 grün** (`dotnet test Pugling.sln -c Release`), `/smoke-test` grün (13 Checks),
  `dotnet format Pugling.sln --verify-no-changes` sauber. Rollengang gegen die laufende Instanz auf :5280
  gefahren: Anlegen liefert `ownerAdultId:1,isMine:true`, Fremd-`PATCH`/`DELETE` je `403 not_owner`,
  Fremd-`GET` weiter `200` mit `isMine:false`, Seed-Fach „Englisch" auch für den Seed-Vater `403`,
  Owner-`DELETE` `204`.
- **2026-08-11** — Fund beim Rollengang als eigene Story abgelegt: [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md)
  (das Vater-Web bietet „Umbenennen"/„Löschen" weiter an jedem Fach an und liest `isMine` nicht — die von
  Entscheidung 5 benannte Folge). B-13s Ziel ist ohne sie erfüllt.
- **2026-08-11** — bleibt auf `in-arbeit`: `pugling-reviewer` ist nicht gelaufen, weil diese Sitzung die
  Regel trägt, keine Agenten unaufgefordert zu starten. Alles andere für `abgenommen` ist belegt; es fehlt
  nur der Reviewer und der Commit.
