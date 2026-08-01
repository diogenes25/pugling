# Pugling – Claude-Leitfaden

Lern-App mit Punktesystem (Leitner-Prinzip). **Vater** steuert und **erzwingt** Lernerfolg – Kernidee:
wiederkehrende **Pflichtziele** (täglich/wöchentlich) + Klausuren, und **verpasste Pflicht kostet Münzen**
(der „Stick"; siehe Positions-`PenaltyCoins` / Reward-Ökonomie). **Sohn** lernt mit Spaß. Drei Ebenen
(Creator/Supervisor/Student), Punkte, Zeitfenster, Klassenarbeiten.

## Grundprinzip: API-First

Die REST-API (**OpenAPI/Swagger**) ist das Produkt und die einzige Quelle der Wahrheit – direkt bedient
oder über die Rollen-Skills `creator`/`supervisor`/`student`, die die API je Ebene treiben und die
verifizierten [Rollen-Tutorials](docs/tutorial.md) schreiben. (Das dateibasierte `lehrplan-autor`/
`lehrplan-lerner`-Kursformat ist eine **separate** Spur und berührt die App-API nicht.) Das React-Frontend
unter [frontend/](frontend/) ist gegen die `api/v1` gebaut und funktionsfähig; Playwright-E2E unter
[frontend/e2e/](frontend/e2e/) tragen den Vater→Sohn-Durchstich.
→ **Neue Features beginnen im Backend** (API-First); das Frontend hängt an der API.
Details: [docs/architektur-entscheidung.md](docs/architektur-entscheidung.md).

## Befehle

`dotnet build` / `run` / `test` / `format` laufen aus `backend/Pugling.Api` bzw. dem Repo-Root;
Format und Build übernimmt nach `.cs`-Edits ohnehin der Hook. Nicht erratbar ist nur das:

```bash
dotnet tool restore                          # einmalig nach dem Clone (installiert dotnet-ef aus dem Manifest)
# Bei Schemaänderung: die Kette wird NEU GEFALTET, nicht verlängert (siehe Fallstricke → EF-Migrationen).
rm -rf backend/Pugling.Api/Data/Migrations
dotnet dotnet-ef migrations add InitialCreate --project backend/Pugling.Api --output-dir Data/Migrations
```

Dazu die Kommandos `/smoke-test` (lässt die echte `pugling.db` unangetastet) und `/neuer-uebungstyp`.

### Frontend

Startbefehle stehen in `frontend/package.json`. Routen-Landkarte, UI-Konventionen und die Regel
„Übungstypen kommen aus dem Server-Manifest" liegen in [frontend/CLAUDE.md](frontend/CLAUDE.md) –
die Datei lädt automatisch, sobald du unter `frontend/` arbeitest.

### KI-Creator (Konsolen-Agent)

Der Konsolen-Agent, der die Creator-Rolle übernimmt (Briefing/Entwurf/Klausur gegen die laufende API):
Aufrufe und Betriebsarten stehen im Skill `ki-creator`, die Architektur unter „Konventionen".
Details: [backend/Pugling.Agent.Creator/README.md](backend/Pugling.Agent.Creator/README.md).

## Architektur (was bei jeder Änderung gilt)

- **Drei Ebenen** (siehe [docs/grundprinzip.md](docs/grundprinzip.md)): **Creator** (Inhalte), **Supervisor**
  (Steuerung), **Student** (Lernen). Sie sind **Rollen**, entkoppelt vom Login, und schneiden API **und** Code:
  Routen `api/v1/{creator|supervisor|student}/…`, Ordner `Controllers/{Tier}` + `Services/{Creator,Supervisor,Student,Shared}`
  (Sub-Namespaces projektweit via csproj `<Using>`). Das Präfix ist die **Taxonomie**; die Auth-Wand deckt
  sich damit, wo gegated wird: schreibende Creator-/Supervisor-Endpunkte tragen `[Authorize(Roles = Roles.Creator)]`
  bzw. `Roles.Supervisor`. Die spielenden/lesenden **Student-Endpunkte** sind bewusst nur `[Authorize]` und
  trennen die Rollen inline (`IsSupervisor`/`IsStudent`), damit der Supervisor für Vorschau/Nachtrag mitlesen darf.
  **Bewusste Ausnahmen ohne Ebenen-Präfix** (kein Versehen): `auth/…` und `remarks/…` – Ressourcen, die
  keiner Ebene gehören, weil derselbe Mensch sie aus mehreren Rollen bedient.
- **`Adult` statt `Father`** ([docs/lehrer-konto-plan.md](docs/lehrer-konto-plan.md)): die fachliche Zeile
  hinter jeder Nicht-Kind-Rolle heißt **`Adult`** – sie trägt auch ein **Lehrer-Konto** ohne
  Betreuungsauftrag. An ihr hängen Autorschaft (`Exercise.AuthorAdultId`) und die RWX-Rechte
  (`ExerciseGrant.CreatorId`). „Vater" bleibt richtig, wo ein Vater gemeint ist – etwa
  `SupervisorRelation.Father` als Verwandtschaftsangabe und in der Oberfläche. Im Vertrag heißt es
  durchgehend `Adult` (`AdultResponse`, `supervisor/adults`, `auth/adult`, `…AdultId`-Felder); der
  Token-Claim heißt weiter `fid`, der Zugriff `User.AdultId()`. Auch die internen Namen sind nachgezogen
  (`EnsureForAdultAsync`, `SupervisorOwnsChildAsync`, `supervisorId`).
- **Identität/Auth** ([Auth/](backend/Pugling.Api/Auth/)): Ein `Account` (Login/PIN-Hash) trägt über
  `AccountProfile` **mehrere Rollen** (`ProfileRole` Creator/Supervisor/Student → `Adult`/`Child`-Profil);
  ein Vater ist zugleich Creator+Supervisor. PIN-Login (`auth/{adult|child}` oder konto-zentrisch `auth/login`)
  → JWT mit `aid` + je Rolle einem Ebenen-Claim (`Creator`/`Supervisor`/`Student`) + `fid`/`cid`. Gegated wird
  direkt auf die Ebenen-Rollen (kein Vater/Sohn-Alias); `LoginResponse.role` ist `Supervisor` bzw. `Student`
  fürs UI-Routing. `AuthAccess` prüft Eigentum **OR-verknüpft je Rolle** (`IsSupervisor`/`IsStudent`);
  Konten entstehen beim Anlegen/Login und – für die geseedete Familie – im Seed-Nachlauf (idempotent).
- **Multi-Supervisor** ([AdminEntities.cs](backend/Pugling.Api/Models/AdminEntities.cs)): `SupervisorLink`
  (Supervisor ⇢ Student) statt einer 1:1-Bindung Kind→Erwachsener – ein Student hat mehrere Supervisor
  (Vater/Mutter/Oma), je mit eigenem Familien-Shop. **Wallet gemeinsam**, Einlösung **ausstellergebunden**
  (`SupervisorId`-Momentaufnahme). Betreuung: `…/supervisor/children/{id}/supervisors`.

### Das Fachmodell in einem Absatz

Der **Katalog** (`Subject → Chapter → Exercise`, typisiert) liefert den Stoff. Ein **`StudyPlan`** ist ein
reiner Container aus **`PlanPosition`s**, die je eine Katalog-Übung mit **eigenem** Pflichtziel, Punkten und
Leitner-Stufe an ein Kind binden. Gespielt wird **pro Position** (Üben/Leitner + Abschlusstest); daraus
entstehen Punkte in zwei Währungen (Münzen fürs Lernen → Familien-Shop, Gems aus Boni → Skins) und der
Lernstand – positionsgebunden *und* plan-übergreifend je Vokabel-Item.

**Wo nachsehen** (das Detail ist bewusst nicht resident):

| Thema | Ort |
|---|---|
| Fachmodell im Detail: Katalog, Lehrplan/Positionen, Services, Reward-Ökonomie, Medien-Anti-Cheat, Creator-Profile, `remarks` | [backend/Pugling.Api/CLAUDE.md](backend/Pugling.Api/CLAUDE.md) (lädt dort automatisch) |
| Zusammenhänge Übung→Lehrplan→Kind→Auswertung | [docs/endpunkt-beziehungen.md](docs/endpunkt-beziehungen.md) |
| Alles Weitere, thematisch erschlossen | [docs/obsidian.md](docs/obsidian.md) (MOC) |

## Konventionen (an bestehendem Code orientieren!)

- **Code-Doku auf Englisch – ausnahmslos.** Das gilt für `/// <summary>` (fließt in Swagger) **und** für
  jeden `//`-Kommentar, in allen fünf Backend-Projekten inklusive `Pugling.Api/Models/`, `Data/` und den
  Tests. Kommentare erklären das *Warum* (Geschäftsregel, Anti-Cheat), nicht das Was – knapp und so, dass
  Mensch **und** Modell sie ohne Vorwissen lesen können: ein Gedanke je Kommentar, keine Umbau-Erzählung,
  keine Wiederholung des Codes. **Englisch ist auch, was ein rotes Tor ausgibt** – die Meldungstexte der
  reflexiven Wächter und ihrer Ausnahmelisten. Deutsch bleiben die **Markdown-Doku**, Strings mit
  Produktinhalt (Seed-/Ledger-Texte, `Capture(…)`-Titel der `DocsCaptureTests`, Enum-Werte wie `Gymnasium`,
  deutsche Beispielwörter und Testdaten) und die Laufzeit-Diagnose (Exception-/Log-Meldungen). Glossar und
  Fallstricke: [docs/translate.md](docs/translate.md).
- **Controller dünn**, Logik in Services. DTOs als `record` projizieren – nie EF-Entities zurückgeben.
- **Vertrag im eigenen Projekt** ([backend/Pugling.Contracts/](backend/Pugling.Contracts/CLAUDE.md)): *alle*
  Request-/Response-`record`s und die geteilten Basistypen liegen dort – **nicht** als verschachtelte Typen
  im Controller. Neues DTO? Ins Vertrags-Projekt, mit `/// <summary>`. Namen sind **global eindeutig** zu
  halten: der OpenAPI-Generator schlüsselt Schemas über den einfachen Typnamen, gleichnamige Records
  verschmelzen sonst still zu einem Schema.
- **Client-Bibliothek** ([backend/Pugling.Client/](backend/Pugling.Client/CLAUDE.md)): die *eine* HTTP-Schicht
  für Nicht-Browser-Konsumenten (die KI-Agenten). Neuer Endpunkt? Erst Backend, dann dort eine einzeilige
  Methode ergänzen – nie HTTP-Plumbing duplizieren.
- **KI-Creator** ([backend/Pugling.Agent.Creator/](backend/Pugling.Agent.Creator/CLAUDE.md)): Konsolen-App
  mit **deterministischer Pipeline** (C# besitzt den Ablauf, das Modell liefert nur strukturierten Inhalt –
  kein Tool-Calling). Fachliche Kernregel: **Interessen kleiden den Stoff ein, sie ersetzen ihn nie**.
- **Guard Clauses zuerst** (früh `return NotFound()/Forbid()` bzw. `this.ProblemWithCode(…)`),
  Happy Path un-eingerückt.
- **API-Versionierung**: Alle Routen unter `api/v1/…` – das Versionssegment steckt zentral in
  `ApiRoutes.V1` ([Controllers/ApiRoutes.cs](backend/Pugling.Api/Controllers/ApiRoutes.cs)), Controller
  tragen `[ApiVersion("1.0")]`. Bis zur Publikation bleiben wir bei 1.0 und ändern **frei** – ein Bruch
  danach läuft über eine parallele `v2`, nicht über Abwärtskompatibilität.
- **Mechanische Tore statt Disziplin** ([Directory.Build.props](Directory.Build.props), [.editorconfig](.editorconfig),
  `ConventionGuardTests`, `SchemaGuardTests`): Warnungen **sind Fehler** (`TreatWarningsAsErrors`, repo-weit;
  NuGet-Audit NU1901–1904 bewusst nicht, die ändern sich ohne Codeänderung). Fehlende `/// <summary>`
  (CS1591) brechen den Build in **allen** Projekten, auch in `Pugling.Api`. Ausgenommen sind dort nur zwei
  Pfade, die die `.editorconfig` namentlich freistellt: die EF-Entities unter `Models/` und die
  `DbSet`-Properties in `Data/PuglingDbContext.cs` – sie sind nur wegen EF `public` und fließen nicht in
  Swagger. Controllers, Auth, Errors, Services und Exercises sind scharf. Reflexive Wächter erzwingen die Regeln
  dieses Abschnitts mechanisch – jede unten stehende Regel hat einen Test hinter sich, plus den
  **Endpunkt-Abdeckungs-Wächter** (keine Controller-Action ohne einen Test, der sie mit Status < 400
  aufruft). Der urteilt im Assembly-Fixture, weil erst dort alle Tests durch sind; die Konsole verschluckt
  seine Meldung, sie steht in `TestResults/endpoint-coverage.txt`. Inventar und Begründungen:
  [docs/codequalitaet-gates-plan.md](docs/codequalitaet-gates-plan.md) – neue Regel scharf stellen? Erst messen.
- **Schema-Änderungen laufen gegen gepinnte Listen** (`SchemaGuardTests`, Tore G1–G9): eine neue Beziehung,
  eine neue String-Länge und eine neue „genau eines von N"-Invariante erzwingen je eine **bewusste Zeile** –
  das Tor ist dabei erst rot, und das *ist* der Zweck. Die Regeln im Einzelnen (samt der Reflexions-Fallstricke)
  stehen in [backend/Pugling.Api/CLAUDE.md](backend/Pugling.Api/CLAUDE.md) → „Schema & Migrationen".
- **`CancellationToken`** gilt hart, und zwar in drei Teilen – weil CA2016 **kein** Netz ist (in Lambdas
  schweigt der Analyzer, und ein Helfer ohne Token-Parameter verbirgt jeden Aufruf in seinem Rumpf):
  1. jede async **Action** nimmt `CancellationToken ct = default` als **letzten** Parameter – der
     Vorgabewert ist nötig, weil C# keinen erforderlichen Parameter nach den optionalen
     `[FromQuery]`-Werten erlaubt – und reicht ihn in jeden EF-/Service-Aufruf durch;
  2. ein neuer **Helfer** nimmt den Token mit, aber **ohne `= default`**: ein weggelassenes optionales
     Argument rügt CA2016 nicht, ohne Vorgabewert erzwingt der Compiler das Durchreichen;
  3. **kompensierende Schritte nach dem Commit** (aufräumen o. Ä.) nehmen bewusst `CancellationToken.None`
     – ein Client-Abbruch darf nicht entscheiden, ob aufgeräumt wird. Der Abbruch selbst endet über den
     `ClientAbortExceptionHandler` als 499 ohne Fehler-Log, nicht als 500.
- **Unbekannte Felder werden abgelehnt** (`UnmappedMemberHandling.Disallow`): ein Feld, das der Vertrag
  nicht kennt, liefert `400` mit `code: unknown_field` – nicht `201` mit stillem Datenverlust. Wer einen
  Payload schreibt (Test, Client, Frontend), muss die Feldnamen des DTOs **treffen**.
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807) mit **maschinenlesbarem `code`**: statt
  `Problem(statusCode:, detail:)` immer `return this.ProblemWithCode(ApiErrors.<Code>, "…")` nutzen
  (Registry: [Errors/ApiErrors.cs](backend/Pugling.Api/Errors/ApiErrors.cs); Status/Titel/`type`-URI kommen
  aus dem `ApiError`). Neuen fachlichen Fehler? Erst einen Code **additiv** in `ApiErrors` ergänzen; leere
  Fehler und unbehandelte 500 stempelt die `CodeStampingProblemDetailsFactory` mit einem Default-Code.
  Meldungstexte (`detail`) sind **englisch** (i18n); der `code` ist stabiler Vertragsbestandteil.
  Beispiele: [docs/api-examples/](docs/api-examples/index.md) (verifiziert von `DocsCaptureTests`).
- **PATCH-Semantik**: `null` heißt „nicht angegeben" (der Wert bleibt), **nicht** „leeren". Ein Feld
  löschbar zu machen braucht darum einen ausdrücklichen `bool Clear<Feld>`-Schalter im Update-DTO (Muster:
  `UpdateChildDto.ClearBirthYear`). Im Controller **erst den Wert, dann den Schalter** anwenden, damit
  „leeren" gewinnt, wenn ein Formular beides schickt. Ohne den Schalter meldet eine Oberfläche mit
  „– keine Angabe –" fröhlich „Gespeichert." und der alte Wert steht weiter da (reflexiv geprüft von
  `PatchClearFieldTests` gegen *alle* `Update…Dto`/`Update…Request`).
- **Eigentum**: Für Endpunkte unter `{planId}` den `[ServiceFilter(typeof(PlanOwnershipFilter))]`,
  für Endpunkte unter `{childId}` den `[ServiceFilter(typeof(ChildOwnershipFilter))]` nutzen
  (nicht inline wiederholen). Sonst `AuthAccess` explizit. Kindbezogene Ressourcen leben unter
  `api/v1/supervisor/children/{childId}/…`; top-level Aggregate, die nur nach Kind filtern, nehmen `?childId=`.
- **EF**: `AsNoTracking()` für Lesequeries, in DB filtern (`Where` vor `ToListAsync`), N+1 via `Include`/
  Projektion vermeiden, `async`/`Async`-Suffix, `CancellationToken` durchreichen.
- **Rolle & Selbstbetrug**: Für den Sohn serverseitig erzwingen (Stufe aus dem Fahrplan, Heartbeat clampen,
  fremde Tage nur der Vater). Neue Endpunkte immer role-/ownership-sauber.

## Fallstricke

- **EF-Migrationen: die Kette ist genau EINE Migration** ([Program.cs](backend/Pugling.Api/Program.cs) ruft
  beim Start `db.Database.Migrate()`). Solange die App unveröffentlicht ist und Altdaten verzichtbar sind,
  wird bei jeder Schemaänderung **neu gefaltet** statt verlängert (`Data/Migrations` löschen +
  `migrations add InitialCreate`, siehe Befehle) – das macht Spaltenumbenennungen und Typwechsel kostenlos,
  weil kein SQLite-Tabellen-Neubau generiert wird, den jemand abnehmen müsste. `SchemaGuardTests` erzwingt
  beides: **kein Modell-Drift** (`HasPendingModelChanges`) und **Kettenlänge 1**. Ein zweiter
  Migrations-Eintrag ist kein Fortschritt, sondern ein rotes Tor; die Regel endet mit der ersten
  Veröffentlichung, dann wird sie *ausdrücklich* entfernt. **Nicht** auf `EnsureCreated` zurückfallen.
  Ablauf, Fallstricke der EF-Tools und die Schema-Konventionen (Enums als String, DB-Defaults,
  Eindeutigkeit, `NOCASE`) stehen in [backend/Pugling.Api/CLAUDE.md](backend/Pugling.Api/CLAUDE.md);
  Stand und offene Etappen in [docs/db-struktur-umbau-plan.md](docs/db-struktur-umbau-plan.md).
- **PINs sind gehasht** (`Auth/PinHasher`): `Adult.Pin`/`Child.Pin` und `Account.PinHash` halten den Hash,
  nie den Klartext. Wer eine PIN setzt, muss durch `PinHasher.Hash` und den Hash **auf das Konto spiegeln**
  (sonst läuft der konto-zentrische `/auth/login` aus dem Takt) – siehe `ChildrenController`/`AdultsController`.
  Der PIN-Login ist zusätzlich per `AddRateLimiter` gebremst (Policy `login`, über `RateLimiting:LoginEnabled`
  abschaltbar – der In-Process-TestServer teilt sonst eine IP-Partition und bekäme 429).
- **Vom Ursprungs-Template ist nichts mehr übrig** – **kein** `User`/`Topic`/`VocabCard`/`Points…` mehr
  anlegen. Das letzte Legacy-Entity (`TimeSlotRule`, der Leitner-Multiplikator nach Tageszeit) ist seit dem
  DB-Struktur-Umbau **Konfiguration** (`Scoring:TimeSlots` in `appsettings.json`), keine Tabelle: es gab
  keine API, keinen Schreibpfad außer dem Seed und keine Überlappungsprüfung – und die Test-Suite musste
  seine Zeilen *löschen*, um deterministische Punktzahlen zu bekommen.
- **Zeit/UTC**: Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` – nahe Mitternacht lokal ggf. anderer Kalendertag.
- **JSON-Spalten** (`Gaps`, `WordBank`, `BoxIntervalDays`, `StageSchedule`, `Noun`/`Verb`, `Interests`,
  `OwnedSkins`, `SuggestedBonus`): tragen alle einen `ValueComparer` aus
  [Data/JsonValueComparer.cs](backend/Pugling.Api/Data/JsonValueComparer.cs) – EF erkennt Änderungen also
  auch bei In-Place-Mutation. **Neue JSON-Spalte? Comparer nicht vergessen**, sonst gehen Änderungen still
  verloren, solange niemand die Liste neu zuweist. Das fängt jetzt Tor **G7** – die Regel hing vorher an
  Disziplin, und ihr Bruch ist unsichtbar.

## Arbeitsweise

- Nach `.cs`-Edits laufen automatisch `dotnet format whitespace` (nur die geänderte Datei) und
  `dotnet build` – jeweils nur für das **besitzende Projekt**, nicht die Solution; Build-Fehler kommen als
  Feedback zurück. Bei einer Reihe zusammengehöriger Edits ruhig weiterarbeiten – der Hook meldet sich.
  Zwei Dinge deckt er bewusst **nicht** ab, dafür vor dem Commit je einen Lauf: einen Umbau in
  `Contracts`/`Client`, der einen abhängigen Nachbarn bricht (`dotnet build Pugling.sln`), und alles
  jenseits von Einrückung/Umbrüchen (`dotnet format Pugling.sln` – kostet mit Analyzern ~23 s, darum
  nicht im Hook).
- **Test-Tor am Ende der Antwort** (Stop-Hook [.claude/hooks/test-gate.sh](.claude/hooks/test-gate.sh)):
  Weichen `.cs`-Dateien von `HEAD` ab, läuft `dotnet test Pugling.sln -c Release` (~63 s) – rot blockt und
  meldet die gefallenen Tests zurück. Zweimal derselbe Stand wird nicht zweimal getestet (Fingerprint), und
  `PUGLING_SKIP_TEST_GATE=1` schaltet ab, wenn rot der beabsichtigte Zwischenstand eines Umbaus ist.
  `Release` ist Absicht: ein parallel laufender Dev-Server sperrt sonst die Debug-Ausgabe.
  Dasselbe Tor steht in CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) vor jedem Push und
  Pull Request; das Azure-Deploy hängt per `workflow_run` daran und läuft **nur bei grün** an.
  Daneben warnt (ohne zu blocken) `.claude/scripts/context-budget.sh`, wenn die dauerhaft geladenen
  Anweisungsdateien ihr Budget reißen – dann gehört etwas nach `docs/` oder in eine verschachtelte
  `CLAUDE.md`, nicht in den Startkontext.
- Änderungen mit echtem Laufzeit-Effekt per `/smoke-test` oder gezieltem `curl` gegen `localhost:5200` prüfen,
  nicht nur kompilieren. Für nichttriviale Änderungen einen Integrationstest in `Pugling.Api.Tests` ergänzen.
- **Erst die Wissenskarte, dann breit suchen:** Zusammenhänge/Einstieg über [docs/endpunkt-beziehungen.md](docs/endpunkt-beziehungen.md)
  (Übung→Lehrplan→Kind→Auswertung) und die MOC in [docs/obsidian.md](docs/obsidian.md) – spart Tokens ggü. Voll-Scans.
  Neue Doku nach den dortigen Konventionen taggen (`bereich/…`, `lerntechnik/…`); neue Lerntechnik = neuer `ExerciseType`
  im bestehenden Muster (kein Parallel-Stack), siehe [wiki/08-erweitern.md](wiki/08-erweitern.md).
- **Neue Idee? Nach [docs/backlog/](docs/backlog/README.md)** – nicht als „offen:"-Vermerk in eine Notiz
  oder ein Plandokument. `/backlog` treibt eine Story **eine** Stufe weiter (idee → ausformuliert →
  gegrillt → geschaetzt → in-arbeit → abgenommen); Eintrittsbedingungen stehen dort. Passt ein Vorhaben
  nicht in eine Sitzung, läuft `gegrillt` als **Karte** (`/wayfinder`) – Ablage und Abbildung dort unter
  „Wayfinding operations", **nicht** im mitgelieferten `.scratch/`.
- **Diese Datei ist der Startkontext** – sie wird bei *jeder* Sitzung mitgeladen. Neues Wissen gehört
  darum standardmäßig nach `docs/` oder in die verschachtelte `CLAUDE.md` des Bereichs
  ([backend/Pugling.Api/](backend/Pugling.Api/CLAUDE.md), [frontend/](frontend/CLAUDE.md), `Contracts`,
  `Client`, `Agent.Creator`). Resident wird nur, was bei einer **beliebigen** Änderung eine Entscheidung
  ändert – und dann als Regel, nicht als Umbau-Erzählung.
