# Backend – Konventionen für alle fünf Projekte

Lädt nur bei Arbeit unter `backend/`. Der Rahmen (API-First, die drei Ebenen, die Fallstricke und die
anti-cheat-tragenden Regeln) steht in der [CLAUDE.md im Repo-Root](../CLAUDE.md); das Domänenmodell in
[Pugling.Api/CLAUDE.md](Pugling.Api/CLAUDE.md).

- **Controller dünn**, Logik in Services. DTOs als `record` projizieren – nie EF-Entities zurückgeben.
- **Vertrag im eigenen Projekt** ([Pugling.Contracts/](Pugling.Contracts/CLAUDE.md)): *alle*
  Request-/Response-`record`s und die geteilten Basistypen liegen dort – **nicht** als verschachtelte Typen
  im Controller. Neues DTO? Ins Vertrags-Projekt, mit `/// <summary>`. Namen sind **global eindeutig** zu
  halten: der OpenAPI-Generator schlüsselt Schemas über den einfachen Typnamen, gleichnamige Records
  verschmelzen sonst still zu einem Schema.
- **Client-Bibliothek** ([Pugling.Client/](Pugling.Client/CLAUDE.md)): die *eine* HTTP-Schicht
  für Nicht-Browser-Konsumenten (die KI-Agenten). Neuer Endpunkt? Erst Backend, dann dort eine einzeilige
  Methode ergänzen – nie HTTP-Plumbing duplizieren.
- **Guard Clauses zuerst** (früh `return NotFound()/Forbid()` bzw. `this.ProblemWithCode(…)`),
  Happy Path un-eingerückt.
- **API-Versionierung**: Alle Routen unter `api/v1/…` (`ApiRoutes.V1`), Controller tragen
  `[ApiVersion("1.0")]`. Bis zur Publikation bleiben wir bei 1.0 und ändern frei; ein Bruch danach
  läuft über eine parallele `v2`, nicht über Abwärtskompatibilität.
- **Schema-Änderungen laufen gegen gepinnte Listen** (`SchemaGuardTests`, Tore G1–G9): eine neue Beziehung,
  eine neue String-Länge und eine neue „genau eines von N"-Invariante erzwingen je eine **bewusste Zeile**.
  Handwerk und Konventionen im Einzelnen: [Pugling.Api/CLAUDE.md](Pugling.Api/CLAUDE.md) →
  „Schema & Migrationen" (lädt dort automatisch).
- **`CancellationToken`** gilt hart, und zwar in drei Teilen – weil CA2016 **kein** Netz ist (in Lambdas
  schweigt der Analyzer, und ein Helfer ohne Token-Parameter verbirgt jeden Aufruf in seinem Rumpf):
  1. jede async **Action** nimmt den Token als **letzten** Parameter und reicht ihn in jeden EF-/
     Service-Aufruf durch; `= default` ist nur **dort** Pflicht, wo optionale `[FromQuery]`-Werte
     vorangehen – dort erzwingt ihn ohnehin CS1737, sonst ist er frei (kein Vertragsargument: der
     `ApiExplorer` unterdrückt `CancellationToken` im OpenAPI-Dokument vollständig, [B-102](../docs/backlog/B-102-token-vorgabewert-regel-schaerfen.md));
  2. ein neuer **Helfer** nimmt den Token mit, aber **ohne `= default`**: ein weggelassenes optionales
     Argument rügt CA2016 nicht, ohne Vorgabewert erzwingt der Compiler das Durchreichen;
  3. **kompensierende Schritte nach dem Commit** (aufräumen o. Ä.) nehmen bewusst `CancellationToken.None`
     – ein Client-Abbruch darf nicht entscheiden, ob aufgeräumt wird. Der Abbruch selbst endet über den
     `ClientAbortExceptionHandler` als 499 ohne Fehler-Log, nicht als 500.
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807) mit **maschinenlesbarem `code`**: statt
  `Problem(statusCode:, detail:)` immer `return this.ProblemWithCode(ApiErrors.<Code>, "…")` nutzen
  (Registry: [Errors/ApiErrors.cs](Pugling.Api/Errors/ApiErrors.cs); Status/Titel/`type`-URI kommen
  aus dem `ApiError`). Neuen fachlichen Fehler? Erst einen Code **additiv** in `ApiErrors` ergänzen; leere
  Fehler und unbehandelte 500 stempelt die `CodeStampingProblemDetailsFactory` mit einem Default-Code.
  Meldungstexte (`detail`) sind **englisch** (i18n); der `code` ist stabiler Vertragsbestandteil.
  Beispiele: [docs/api-examples/](../docs/api-examples/index.md) (verifiziert von `DocsCaptureTests`).
- **PATCH-Semantik**: `null` heißt „nicht angegeben" (der Wert bleibt), **nicht** „leeren". Ein Feld
  löschbar zu machen braucht darum einen ausdrücklichen `bool Clear<Feld>`-Schalter im Update-DTO (Muster:
  `UpdateChildDto.ClearBirthYear`). Im Controller **erst den Wert, dann den Schalter** anwenden, damit
  „leeren" gewinnt, wenn ein Formular beides schickt. Ohne den Schalter meldet eine Oberfläche mit
  „– keine Angabe –" fröhlich „Gespeichert." und der alte Wert steht weiter da. `PatchSemanticsTests`
  prüft reflexiv über *alle* `Update…Dto`/`Update…Request`, dass **jeder** Schalter einen Fall in seiner
  Tabelle hat – ein neuer macht das Tor also erst rot (Einzelfälle: `PatchClearFieldTests`).
- **Eigentum**: Für Endpunkte unter `{planId}` den `[ServiceFilter(typeof(PlanOwnershipFilter))]`,
  für Endpunkte unter `{childId}` den `[ServiceFilter(typeof(ChildOwnershipFilter))]` nutzen
  (nicht inline wiederholen). Sonst `AuthAccess` explizit. Kindbezogene Ressourcen leben unter
  `api/v1/supervisor/children/{childId}/…`; top-level Aggregate, die nur nach Kind filtern, nehmen `?childId=`.
- **EF**: `AsNoTracking()` für Lesequeries, in DB filtern (`Where` vor `ToListAsync`), N+1 via `Include`/
  Projektion vermeiden, `async`/`Async`-Suffix, `CancellationToken` durchreichen.
