# Pugling – Claude-Leitfaden

Lern-App mit Punktesystem (Leitner-Prinzip). **Vater** steuert und **erzwingt** Lernerfolg – Kernidee:
wiederkehrende **Pflichtziele** (täglich/wöchentlich) + Klausuren, und **verpasste Pflicht kostet Münzen**
(der „Stick"; siehe Positions-`PenaltyCoins` / Reward-Ökonomie). **Sohn** lernt mit Spaß. Drei Ebenen
(Creator/Supervisor/Student), Punkte, Zeitfenster, Klassenarbeiten.

## Grundprinzip: API-First

Die REST-API (**OpenAPI/Swagger**) ist das Produkt und die einzige Quelle der Wahrheit – direkt bedient
oder über die Rollen-Skills `creator`/`supervisor`/`student`, die die API je Ebene treiben und die
verifizierten [Rollen-Tutorials](docs/tutorial.md) schreiben. (Das dateibasierte `lehrplan-autor`/
`lehrplan-lerner`-Kursformat ist eine **separate** Spur und berührt die App-API nicht.) Das React-Frontend unter [frontend/](frontend/)
wurde **neu gegen die `api/v1` gebaut und ist funktionsfähig** (Vite+React+TS+PWA): Produktseite `/`,
Sohn-Arcade-PWA `/sohn`, Vater-Web `/vater` inkl. Lehrplan-Assistent `/vater/wizard`. Zwei Playwright-E2E
tragen den Durchstich: [full-flow.spec.ts](frontend/e2e/full-flow.spec.ts) fährt den Vater→Sohn-Loop gegen
das Seed-Konto, [vater-von-null.spec.ts](frontend/e2e/vater-von-null.spec.ts) baut einen Vater **von Grund
auf** (Registrierung → Kind → Fach/Kapitel/Vokabeln/Übung → Übung korrigieren → Plan+Position mit Malus →
Shop → Lernziel → Kind-Login).
→ **Neue Features beginnen weiterhin im Backend** (API-First); das Frontend hängt an der API.
Details: [docs/architektur-entscheidung.md](docs/architektur-entscheidung.md), [frontend starten](#frontend).

## Befehle

`dotnet build` / `run` / `test` / `format` laufen aus `backend/Pugling.Api` bzw. dem Repo-Root;
Format und Build übernimmt nach `.cs`-Edits ohnehin der Hook. Nicht erratbar ist nur das:

```bash
dotnet tool restore                          # einmalig nach dem Clone (installiert dotnet-ef aus dem Manifest)
dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations   # bei Schemaänderung
```

- **Smoke-Test gegen laufende API:** `/smoke-test` (startet gegen eine Temp-DB, prüft Auth +
  Ownership + einen Plan→Test→Submit-Flow + die Anmerkungen samt Kontext-Mitschnitt, lässt die echte
  `pugling.db` unangetastet).
- **Neuen Übungstyp/Lernverfahren anlegen:** `/neuer-uebungstyp` (führt den etablierten Prozess).

### Frontend

Startbefehle stehen in `frontend/package.json`. Routen-Landkarte, UI-Konventionen und die Regel
„Übungstypen kommen aus dem Server-Manifest" liegen in [frontend/CLAUDE.md](frontend/CLAUDE.md) –
die Datei lädt automatisch, sobald du unter `frontend/` arbeitest.

### KI-Creator (Konsolen-Agent)

Der Konsolen-Agent, der die Creator-Rolle übernimmt (Briefing/Entwurf/Klausur gegen die laufende API):
Aufrufe und Betriebsarten stehen im Skill `ki-creator`, die Architektur unter „Konventionen".
Details: [backend/Pugling.Agent.Creator/README.md](backend/Pugling.Agent.Creator/README.md).

## Architektur (das produktive Modell)

- **Drei Ebenen** (siehe [docs/grundprinzip.md](docs/grundprinzip.md)): **Creator** (Inhalte), **Supervisor**
  (Steuerung), **Student** (Lernen). Sie sind **Rollen**, entkoppelt vom Login, und schneiden API **und** Code:
  Routen `api/v1/{creator|supervisor|student}/…`, Ordner `Controllers/{Tier}` + `Services/{Creator,Supervisor,Student,Shared}`
  (Sub-Namespaces projektweit via csproj `<Using>`). Das Präfix ist die **Taxonomie**; die Auth-Wand deckt
  sich damit, wo gegated wird: schreibende Creator-/Supervisor-Endpunkte tragen `[Authorize(Roles = Roles.Creator)]`
  bzw. `Roles.Supervisor`. Die spielenden/lesenden **Student-Endpunkte** sind bewusst nur `[Authorize]` und
  trennen die Rollen inline (`IsSupervisor`/`IsStudent`), damit der Supervisor für Vorschau/Nachtrag mitlesen darf.
  **Bewusste Ausnahmen ohne Ebenen-Präfix** (kein Versehen): `auth/…` und `remarks/…` – Ressourcen, die
  keiner Ebene gehören, weil derselbe Mensch sie aus mehreren Rollen bedient.
- **Anmerkungen beim Testen** (`api/v1/remarks`, Dev-Werkzeug – kein Produktfeature): Beobachtung im
  Widget erfassen (Alt+A) → **Log-Id** → in Claude Code per Skill `anmerkungen` belegt beantworten.
  Der Wert steckt im automatisch mitgeschnittenen Kontext (Route, Kind/Übung, letzte Fehler), nicht im
  Text. Ringpuffer speichert **nur Metadaten** – der Login-Request trägt die PIN im Body.
  Details: [docs/anmerkungen-plan.md](docs/anmerkungen-plan.md).
- **Identität/Auth** ([Auth/](backend/Pugling.Api/Auth/)): Ein `Account` (Login/PIN-Hash) trägt über
  `AccountProfile` **mehrere Rollen** (`ProfileRole` Creator/Supervisor/Student → `Father`/`Child`-Profil);
  ein Vater ist zugleich Creator+Supervisor. PIN-Login (`auth/{father|child}` oder konto-zentrisch `auth/login`)
  → JWT mit `aid` + je Rolle einem Ebenen-Claim (`Creator`/`Supervisor`/`Student`) + `fid`/`cid`. Das frühere
  Vater/Sohn-Alias wurde **entfernt**; gegated wird direkt auf die Ebenen-Rollen, `LoginResponse.role` ist
  `Supervisor` bzw. `Student` (fürs UI-Routing). `AuthAccess` prüft Eigentum OR-verknüpft je Rolle
  (`IsSupervisor`/`IsStudent`); Bestandsnutzer bekommen Konten per idempotentem `AccountBackfill`.
- **Multi-Supervisor** ([AdminEntities.cs](backend/Pugling.Api/Models/AdminEntities.cs)): `SupervisorLink`
  (Supervisor ⇢ Student) ersetzt die frühere 1:1-`Child.FatherId`. Ein Student hat mehrere Supervisor
  (Vater/Mutter/Oma), je mit eigenem Familien-Shop; **Wallet gemeinsam**, Einlösung **ausstellergebunden**
  (Momentaufnahme `SupervisorId` auf `ShopPurchase`/`ActivationRequest`).
  Betreuung: `…/supervisor/children/{id}/supervisors`.
- **Lern-Katalog** ([Controllers/Creator/ExerciseControllers.cs](backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)):
  `Subject → Chapter → Exercise` (typisiert, Config als JSON). Ein Controller je `ExerciseType`,
  erben CRUD aus `ExerciseControllerBase<TConfig>`. Route: `api/v1/creator/subjects/{}/chapters/{}/<typ>`.
  **Vokabelübungen** halten ihre Vokabelpaare als **eigene Ebene**: stabil identifizierte `ExerciseItem`s
  (Tabelle, nicht mehr in der ConfigJson) mit CRUD unter `…/vocabulary/{exerciseId}/items/{itemId}`. Ein Item
  ist eine positionierte Referenz auf eine Store-`Vocabulary` (Front/Back/Audio kommen live von dort) + optionaler
  lokaler Hinweis. POST akzeptiert weiterhin inline `items`/`refs` im Payload (materialisiert per `ExerciseItemService`,
  ID-erhaltend); die Config trägt danach nur noch Einstellungen (Direction/Sprachen). Der Resolver liest Vokabel-Items
  aus der Tabelle; der Engine-Index ist die Listenposition (bleibt zum Legacy-`ItemIndex` kompatibel).
- **Unterrichtsmaterial & Creator-Profile** ([CurriculumEntities.cs](backend/Pugling.Api/Models/CurriculumEntities.cs)):
  Die **Lehrwerk-Reihe** ist eine *geteilte* Katalog-Größe: `TextbookSeries` („Access", Slug-idempotent wie
  `InterestTag`) → `SeriesUnit`. Band und Unit liegen bewusst in **einer** Ebene (`Grade` = Band); der
  fachliche Wert steckt in `Topics`/`Grammar`/`VocabularyNotes` – **das** macht einen KI-Creator
  materialkundig, statt ihn den Stoff der Unit erfinden zu lassen. Route `api/v1/creator/textbook-series`
  (+ `…/{id}/units`), lesen darf jeder Creator, ändern nur der Owner (`OwnerFatherId`, Muster
  `Exercise.AuthorFatherId`, FK `SetNull`). Das **Kind** zeigt über `Textbook.SeriesId`/`CurrentUnitId`
  darauf (Titel/`CurrentChapter` bleiben Rückfallebene für unkatalogisierte Werke); eine Unit muss zur
  Reihe des Buchs gehören, sonst `validation_error` – sonst bekäme der Creator den Stoff eines fremden Werks.
  Ein **`CreatorProfile`** (`api/v1/creator/profiles`) ist der *Fachlehrer*: Fach, Schulart,
  Klassenstufen-Bereich, optional die Reihe, dazu `Persona`/`Didactics` (gehen dem festen Regelblock des
  Agenten **voran**, weichen ihn nie auf) und `DefaultTypes` (JSON-Liste → Neuzuweisung, kein
  In-Place-Mutieren). Der Angelpunkt ist `…/profiles/match?childId=` (`CreatorProfileService`):
  harte Ausschlüsse (inaktiv, Klassenstufe außerhalb, Schulart disjunkt), dann Punkte
  **Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1**, Gleichstand über die `Id` – deterministisch wie der
  `MediaSelector`, damit derselbe Datenstand denselben Lehrer liefert. Der Endpunkt liest Kind-Daten und
  trägt darum trotz Creator-Route eine **Betreuungs-Prüfung** (`AuthAccess`, sonst 403); die `Reasons` sind
  stabile Codes (`series_match` …), die Formulierung macht die Oberfläche.
  **Frontend**: `/vater/lehrwerke` (Reihen + Units samt Stoff), `/vater/fachlehrer` (Profile) und auf
  `/vater/kind/:id` die Sektion „Unterrichtsmaterial" – Buch mit Reihe/Unit plus die begründete
  Fachlehrer-Trefferliste; E2E [frontend/e2e/lehrwerke.spec.ts](frontend/e2e/lehrwerke.spec.ts).
- **Lehrplan/Training** ([StudyPlansController](backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs)):
  `StudyPlan` ist ein **reiner Container** (`ChildId, Title, Start/End, Active`). Inhalt sind
  `PlanPosition`s ([PlanPositionsController](backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)),
  die je auf eine Katalog-`Exercise` verweisen und **eigenes** Ziel (Rhythmus Tag/Woche + Schwelle),
  Punkte (Ziel-Belohnung + optionaler **Münz-Malus** `PenaltyCoins` bei gerissener Pflicht = der „Stick"),
  Stufe und Leitner tragen. Gespielt wird pro Position: `PositionPracticeController` (Üben/Leitner)
  + `PositionTestsController` (Abschlusstest); Inhalt kommt aus der Übungs-Config (`ExerciseContentProvider`),
  Leitner-Fortschritt materialisiert je Inhalts-Atom in `PositionItemProgress`. Tagesmission/Verlauf über
  `PlanOverviewController` (`…/overview` + `…/overview/progress`). Route: `api/v1/supervisor/study-plans/{planId}/…`.
  Das alte plan-weite `StudyPlanItem`/`Method`-Modell wurde vollständig entfernt (kein Legacy mehr).
  **Plan-übergreifender Item-Lernstand** (nur Vokabel): `PositionPracticeController.Review`/`PositionTestsController.Submit`
  schreiben je Antwort über `ItemProgressService` einen Stand pro `(Kind, ItemId)` (`ItemProgress`: Box/Beherrschung/Zähler)
  + eine Antwort-Historie (`ItemReviewEvent`), beide mit denormalisierter `VocabularyId`. Kind-zentrische Auswertung:
  `ChildVocabularyProgressController` unter `api/v1/student/children/{childId}/vocabulary-progress` (Liste mit `?exerciseId/?maxBox/?onlyWeak`,
  `/{itemId}`, `/{itemId}/history`, `/by-word`-Rollup für „schlecht gelernte Wörter"). Ergänzt den positionsgebundenen
  `PositionReportService` um die plan-übergreifende Sicht.
- **Tags & Klassenarbeiten** ([KlassenarbeitenController](backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs)):
  Übungen taggen, Arbeiten planen/benoten, gezielt üben/wiederholen. Route: `api/v1/supervisor/class-tests`
  (Typnamen intern weiterhin `Klassenarbeit`).
- **Medien & Interessen** ([MediaEntities.cs](backend/Pugling.Api/Models/MediaEntities.cs),
  [InterestEntities.cs](backend/Pugling.Api/Models/InterestEntities.cs), Plan:
  [docs/medien-bilder.md](docs/medien-bilder.md)): **Ein Motiv, viele Bilder.** Zwei Achsen bleiben
  getrennt: `MediaAsset` ist *eine Darstellung* („laufendes Einhorn im Comic-Stil") mit Stil-Tags und
  `ContentRating`, `MediaVariant` dieselbe Darstellung in einer Auflösung – adressiert über den
  semantischen `MediaPurpose` (Thumb/Card/Full/Hero), nicht über Pixelmaße. Bytes liegen nie in der DB,
  nur URLs. Route: `api/v1/creator/media` (+ `…/{id}/variants`, `…/{id}/tags`).
  **Der Angelpunkt ist die geteilte Taxonomie**: `InterestTag` (Slug + Facette, u. a. `Style`) wird von
  Bildern *und* Kindern referenziert (`ChildInterest` mit Gewicht **-3…+3**, negativ = Abneigung, unter
  `api/v1/supervisor/children/{}/interests`). Nur weil beide Seiten aus **einem** Vokabular schöpfen, ist
  die Bildauswahl berechenbar – deshalb läuft jedes Findet-sonst-legt-an über `InterestTagService`.
  `Child.Interests` (Freitext) bleibt daneben: es ist die Sprache des KI-Creators. `ContentRating` +
  `Child.AllowedContentRating` liegen **als int** in der DB (ordnender Vergleich – als String wäre er
  alphabetisch und damit falsch).
  **Zuordnung** über `MediaLink` – **n:m in beide Richtungen** (ein Wort trägt viele Darstellungen, ein
  Bild dient vielen Wörtern), deshalb eigene Tabelle statt Spalte am Träger wie beim 1:1-Aussprache-Audio.
  Genau *ein* Träger je Zeile (DB-Check-Constraint): `Vocabulary` = Regel für alle Übungen
  (`api/v1/creator/vocabulary/{}/media`, jeder Creator), `ExerciseItem` = übungslokale Übersteuerung und
  `Exercise` = Titelbild (beide unter `api/v1/creator/exercises/{}/…`, **Schreibrecht** nötig).
  Genauigkeits-Kaskade: Item schlägt Vokabel. Rückrichtung `media/{id}/usage`; Löschen ist bewusst *nicht*
  gesperrt (kein Platzhalter wie bei Vokabeln – die Auswahl schrumpft nur).
  **Auswahl je Kind** (`MediaSelector`): hart filtern (Eignung über Freigabe, Abneigung = negativ
  gewichteter Tag, bereits abgelehnt, keine Variante) → nach Interessen bewerten (Thema ×2, Stil ×1;
  `MediaLink.Weight` bricht nur Gleichstände, gefolgt von einem stabilen FNV-Hash – **kein** `Random`
  und **kein** `string.GetHashCode`, der ist pro Prozess randomisiert) → **einfrieren** in
  `ChildMediaPick`. Das Einfrieren ist der Kern: beim Vokabellernen ist Bildkonstanz der Merkeffekt, ein
  nachträglich hinzugefügtes Bild darf die laufende Wahl nicht kippen. „Anderes Bild" über
  `api/v1/student/children/{}/media-picks/reshuffle` (lehnt dauerhaft ab; ohne Alternative
  `409 media_no_alternative`, statt den letzten Kandidaten zu verbrennen). Kein Treffer = **kein Bild**,
  nie ein Notnagel. Weg zur Karte: `ItemsOfAsync(exercise, childId)` → `ContentItem.ImageUrl/ImageAlt` →
  `StageFacets` → `CardFacets` → `PracticeCard`/`TestItem`; `childId` ist **explizit** (nicht aus einer
  geladenen Navigation), sonst entschiede ein vergessenes `Include` über die Bebilderung.
  **Anti-Cheat:** Bild nur auf nicht-getippten Stufen (`ShowBoth`/`SelfAssess`) – schärfer als beim Audio,
  weil ein Motiv die Bedeutung in beide Richtungen zeigt; der Alt-Text folgt dem Bild. Das gilt **auch für
  „anderes Bild"** am Karten-Endpunkt: er gibt ein Bild *heraus* und trägt darum dieselben Schranken wie die
  Ausspielung (spielbarer Plan, nur Karten der Sitzung, nur wo die Karte ein Bild zeigt →
  `409 media_not_on_card`) – sonst wäre er die Hintertür um die Regel herum.
  **Frontend** (Etappe 6): `/vater/kind/:id` (gewichtete Interessen + Bild-Freigabe), `/vater/media`
  (Bibliothek), Bilder-Panel je Vokabelzeile, Bild + „anderes Bild" auf der Sohn-Karte; E2E
  [frontend/e2e/bilder.spec.ts](frontend/e2e/bilder.spec.ts).
  **Upload** (Etappe 5): `POST creator/media/upload` (multipart) → `MediaImageProcessor` skaliert auf
  Thumb/Card/Full (WebP, **nie hochskalieren, nie beschneiden** – daher kein `Hero`) und ermittelt eine
  Platzhalterfarbe; `IMediaStorage` legt ab. Der Ordner ist **nicht** `wwwroot` (das überschreibt der
  Frontend-Deploy), sondern `Media:RootPath` (Default `media-uploads`), ausgeliefert unter
  `Media:PublicPath` (`/media`); Ordnername ist die Asset-**Id**, nie der nutzergesetzte Key.
  Bildbibliothek ist **SkiaSharp** (MIT/BSD) – ImageSharp 4 bricht den Build ohne Lizenzschlüssel ab.
  **Offen**: Stufe „Bild → Wort" (7).
- **Services** ([Services/](backend/Pugling.Api/Services/)): `PositionPlayService` (Fälligkeit/Scope/Stufen +
  Leitner-Terminierung je Position), `PositionProgressService` (Ziel-„erledigt"-Regel je `ExerciseCheckMode`,
  idempotente Ziel-Punkte via `PositionGoalReward`, Tages-/Verlaufs-Rollup über Positionen; **Malus fürs
  Nicht-Lernen** via `SettleClosedPeriodsAsync`: gerissene Pflicht-Periode → negative `PointKind.GoalPenalty`,
  idempotent über `PositionGoalPenalty`, **Schuld erlaubt**; es gibt **keinen Scheduler** → lazy an Kind-Login
  und Shop-Kauf abgerechnet, Fairness bei inaktivem Plan, `ConcurrencyStamp`-Bump), `ScoringService`
  (die eine Stelle für Review-Punkte: Basis × Zeitfenster plus Ereignis-Boni wie Combo/Schnelle Antwort;
  jede Buchung trägt einen `PointKind`; `StageMechanics` hält die geteilten Stufen-/Vergleichs-Statics),
  `MetricsService` (Fortschritts-Metriken aus den Tabellen) + `GamificationService` (Missionen &
  Auszeichnungen, idempotent belohnt; Vater-CRUD unter `api/v1/supervisor/children/{}/missions|achievements`,
  Sohn-Sicht `api/v1/student/me/missions|achievements`).
- **Reward-Ökonomie** (zwei Währungen): 🪙 **Münzen** fürs Lernen → reale Vater-Belohnungen aus dem
  **Familien-Shop**, 💎 **Gems** aus Boni → **Skins** (und optionaler Gem-Anteil bei Shop-Artikeln). Währung =
  reine Funktion des `PointKind` (`PointKindCurrency`, keine Ledger-Spalte); Salden über `WalletService`.
  Der **Familien-Shop** ist der **einzige Münz-Ausgabeweg** (`ShopArticle` → `ShopListing`): Vater pflegt
  Artikel-Katalog mit `UnitType`/`ActionType` und Angebote mit Coin+Gem-Preis sowie Bestand (inkl.
  `ShopRefillKind` für automatisches Auffüllen). Kauf bucht `PointKind.ShopCoins`/`ShopGems` ab, erhöht das
  aggregierte Inventar (`ChildInventory`) des Sohns. Sohn stellt **Aktivierungsanfrage** (`ActivationRequest`),
  Vater genehmigt/lehnt ab (`ShopService`; `children/{}/shop/activations/{}/approve|reject`). Käufe/Aktivierungen
  sind **ausstellergebunden** (`SupervisorId`-Snapshot). Der Shop ist der einzige Weg, auf dem der **Sohn**
  Münzen *ausgibt*; daneben stehen zwei vater-getriebene Münz-Bewegungen: der **Malus** (`GoalPenalty`, s. o.)
  zieht ab, das **Verschenken** gibt dazu. **Verschenken/Manuell:** `POST children/{}/points` nimmt `currency`
  (Coins → `PointKind.Manual`, Gems → `PointKind.ManualGems`) – der Vater kann Münzen **und Gems** verschenken
  (Belohnung außerhalb der App + Druckventil gegen Malus-Schulden). Das frühere separate „Angebots"-System
  (`Reward`/`RewardRedemption`/`OfferService`) wurde **entfernt**; `PointKind.Reward` bleibt nur als Ledger-Tombstone
  für historische Buchungen. Details: [wiki/05-punkte-und-bonus.md](wiki/05-punkte-und-bonus.md).

## Konventionen (an bestehendem Code orientieren!)

- **Modernes C# 14 / net10, `Nullable` an.** File-scoped Namespaces, Primary Constructors für DI,
  `record`s für DTOs/Requests/Responses, Expression-bodied Members, Pattern Matching, Collection Expressions.
- **Doku auf Deutsch.** Öffentliche Typen/Members tragen `/// <summary>` (fließt in Swagger).
  Kommentare erklären das *Warum* (Geschäftsregel, Anti-Cheat), nicht das Was.
- **Controller dünn**, Logik in Services. DTOs als `record` projizieren – nie EF-Entities zurückgeben.
- **Vertrag im eigenen Projekt** ([backend/Pugling.Contracts/](backend/Pugling.Contracts/)): *alle* Request-/
  Response-`record`s und die geteilten Basistypen (Enums, `StageStep`, die Übungs-Configs) liegen dort –
  **nicht** als verschachtelte Typen im Controller. Neues DTO? Ins Vertrags-Projekt, mit `/// <summary>`.
  Namen sind **global eindeutig** zu halten (der OpenAPI-Generator schlüsselt Schemas über den einfachen
  Typnamen; gleichnamige Records verschmelzen sonst still zu einem Schema). Namespace-Aufteilung und die
  Blatt-Regel stehen in [backend/Pugling.Contracts/CLAUDE.md](backend/Pugling.Contracts/CLAUDE.md).
- **Client-Bibliothek** ([backend/Pugling.Client/](backend/Pugling.Client/)): die *eine* HTTP-Schicht für
  Nicht-Browser-Konsumenten (die KI-Agenten). Neuer Endpunkt? Erst Backend, dann dort eine einzeilige
  Methode ergänzen – nie HTTP-Plumbing duplizieren. Aufbau (AuthHandler/TokenStore/Fassaden):
  [backend/Pugling.Client/CLAUDE.md](backend/Pugling.Client/CLAUDE.md).
- **KI-Creator** ([backend/Pugling.Agent.Creator/](backend/Pugling.Agent.Creator/README.md)): Konsolen-App,
  die die Creator-Rolle übernimmt – lokal gegen Ollama, **deterministische Pipeline** (C# besitzt den Ablauf,
  das Modell liefert nur strukturierten Inhalt – kein Tool-Calling). Fachliche Kernregel: **Interessen
  kleiden den Stoff ein, sie ersetzen ihn nie**. Pipeline, Briefing-Quellen und `ExamPlanner`:
  [backend/Pugling.Agent.Creator/CLAUDE.md](backend/Pugling.Agent.Creator/CLAUDE.md).
- **Guard Clauses zuerst** (früh `return NotFound()/Forbid()` bzw. `Problem(statusCode:…, detail:…)`),
  Happy Path un-eingerückt.
- **API-Versionierung**: Alle Routen unter `api/v1/…` – das Versionssegment steckt zentral in
  `ApiRoutes.V1` ([Controllers/ApiRoutes.cs](backend/Pugling.Api/Controllers/ApiRoutes.cs)), Controller
  tragen `[ApiVersion("1.0")]`. Bis zur Publikation bleiben wir bei 1.0 und ändern frei; ein Bruch danach
  läuft über eine parallele `v2` (neue Controller/DTOs neben v1), nicht über Abwärtskompatibilität.
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807) mit **maschinenlesbarem `code`**: statt
  `Problem(statusCode:, detail:)` immer `return this.ProblemWithCode(ApiErrors.<Code>, "…")` nutzen
  (Registry: [Errors/ApiErrors.cs](backend/Pugling.Api/Errors/ApiErrors.cs); Status/Titel/`type`-URI
  kommen aus dem `ApiError`). Neuen fachlichen Fehler? Erst einen Code additiv in `ApiErrors` ergänzen.
  `AddProblemDetails(CustomizeProblemDetails)` + die `CodeStampingProblemDetailsFactory` stempeln leere
  Fehler (404/403/401/429) und unbehandelte 500 mit einem status-basierten Default-Code. Meldungstexte
  (`detail`) sind **englisch** (i18n); der `code` ist stabiler Vertragsbestandteil. Beispiele:
  [docs/api-examples/](docs/api-examples/index.md) (verifiziert von `DocsCaptureTests`).
- **PATCH-Semantik**: `null` heißt „nicht angegeben" (der Wert bleibt), **nicht** „leeren". Ein Feld
  löschbar zu machen braucht darum einen ausdrücklichen `bool Clear<Feld>`-Schalter im Update-DTO – so wie
  `UpdateKlassenarbeitDto.ClearGrade`, `UpdateChildDto.ClearBirthYear/ClearGrade`,
  `UpdateTextbookDto.ClearSeries/ClearUnit/…`, `UpdateCreatorProfileDto.ClearSubject/ClearSeries/…`.
  Im Controller **erst den Wert, dann den Schalter** anwenden, damit „leeren" gewinnt, wenn ein Formular
  beides schickt. Ohne den Schalter meldet eine Oberfläche mit „– keine Angabe –" fröhlich „Gespeichert."
  und der alte Wert steht weiter da (verifiziert von `PatchClearFieldTests`).
- **Eigentum**: Für Endpunkte unter `{planId}` den `[ServiceFilter(typeof(PlanOwnershipFilter))]`,
  für Endpunkte unter `{childId}` den `[ServiceFilter(typeof(ChildOwnershipFilter))]` nutzen
  (nicht inline wiederholen). Sonst `AuthAccess` explizit. Kindbezogene Ressourcen leben unter
  `api/v1/supervisor/children/{childId}/…`; top-level Aggregate, die nur nach Kind filtern, nehmen `?childId=`.
- **EF**: `AsNoTracking()` für Lesequeries, in DB filtern (`Where` vor `ToListAsync`), N+1 via `Include`/
  Projektion vermeiden, `async`/`Async`-Suffix, `CancellationToken` durchreichen.
- **Rolle & Selbstbetrug**: Für den Sohn serverseitig erzwingen (Stufe aus dem Fahrplan, Heartbeat clampen,
  fremde Tage nur der Vater). Neue Endpunkte immer role-/ownership-sauber.

## Fallstricke

- **EF-Migrationen** ([Program.cs](backend/Pugling.Api/Program.cs) ruft beim Start `db.Database.Migrate()`):
  Bei jeder Schemaänderung eine Migration erzeugen (`dotnet ef migrations add …`, siehe Befehle) – **nicht**
  auf `EnsureCreated` zurückfallen. Die EF-Tools laufen über die Design-Time-Factory
  ([Data/PuglingDbContextFactory.cs](backend/Pugling.Api/Data/PuglingDbContextFactory.cs)), nicht über den Web-Host.
  `*.db` ist gitignored; eine alte, per `EnsureCreated` erzeugte DB einmalig löschen (wird neu migriert + geseedet).
- **PINs sind gehasht** (`Auth/PinHasher`): `Father.Pin`/`Child.Pin` und `Account.PinHash` halten den Hash,
  nie den Klartext. Wer eine PIN setzt, muss durch `PinHasher.Hash` und den Hash **auf das Konto spiegeln**
  (sonst läuft der konto-zentrische `/auth/login` aus dem Takt) – siehe `ChildrenController`/`FathersController`.
  Der PIN-Login ist zusätzlich per `AddRateLimiter` gebremst (Policy `login`, über `RateLimiting:LoginEnabled`
  abschaltbar – der In-Process-TestServer teilt sonst eine IP-Partition und bekäme 429).
- **`TimeSlotRule`** ist das *einzige* bewusst erhaltene Legacy-Entity (Leitner-Multiplikator). Alles
  andere aus dem Ursprungs-Template wurde entfernt – **kein** `User`/`Topic`/`VocabCard`/`Points…` mehr anlegen.
- **Zeit/UTC**: Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` – nahe Mitternacht lokal ggf. anderer Kalendertag.
- **JSON-Spalten** (`Gaps`, `WordBank`, `BoxIntervalDays`, `StageSchedule`, `Noun`/`Verb`, `Interests`,
  `OwnedSkins`, `SuggestedBonus`): tragen alle einen `ValueComparer` aus
  [Data/JsonValueComparer.cs](backend/Pugling.Api/Data/JsonValueComparer.cs) – EF erkennt Änderungen also
  auch bei In-Place-Mutation. **Neue JSON-Spalte? Comparer nicht vergessen**, sonst gehen Änderungen still
  verloren, solange niemand die Liste neu zuweist.

## Arbeitsweise

- Nach `.cs`-Edits laufen automatisch `dotnet format` (nur die Datei) und `dotnet build` (API); Build-Fehler
  kommen als Feedback zurück. Bei einer Reihe zusammengehöriger Edits ruhig weiterarbeiten – der Hook meldet sich.
- Änderungen mit echtem Laufzeit-Effekt per `/smoke-test` oder gezieltem `curl` gegen `localhost:5200` prüfen,
  nicht nur kompilieren. Für nichttriviale Änderungen einen Integrationstest in `Pugling.Api.Tests` ergänzen.
- Weitere Doku unter [docs/](docs/): Architektur-Resümee, Code-Review, Tutorials, Klassenarbeiten/Tagging.
- **Erst die Wissenskarte, dann breit suchen:** Zusammenhänge/Einstieg über [docs/endpunkt-beziehungen.md](docs/endpunkt-beziehungen.md)
  (Übung→Lehrplan→Kind→Auswertung) und die MOC in [docs/obsidian.md](docs/obsidian.md) – spart Tokens ggü. Voll-Scans.
  Neue Doku nach den dortigen Konventionen taggen (`bereich/…`, `lerntechnik/…`); neue Lerntechnik = neuer `ExerciseType`
  im bestehenden Muster (kein Parallel-Stack), siehe [wiki/08-erweitern.md](wiki/08-erweitern.md).
