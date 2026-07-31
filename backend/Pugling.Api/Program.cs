using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Serilog als einziges Logging-Backend: Konsole (lesbar beim Entwickeln) + rollierende JSON-Datei
// (maschinell auswertbar, 14 Tage Vorhalt). shared:true, weil bei den Integrationstests mehrere Hosts
// parallel in dieselbe Datei schreiben. Level/Overrides kommen aus dem "Serilog"-Abschnitt der Konfiguration.
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/pugling-.clef",
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // **Unbekannte Felder werden abgelehnt, nicht verschluckt.** Der Default `Skip` machte aus einem
        // vertippten oder veralteten Feld ein stilles Nichts: der Aufrufer bekam 201 Created und glaubte,
        // sein Wert sei angekommen. Für ein API-First-Produkt, dessen Konsumenten generierte Clients und
        // KI-Agenten sind, ist das die teuerste Voreinstellung überhaupt – sie verwandelt einen
        // Vertragsfehler in stillen Datenverlust. Siehe docs/codequalitaet-gates-plan.md (L3/B3).
        o.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
    });

// Modell-Validierungsfehler als sauberes, englisches ProblemDetails ausliefern. Zwei Probleme der
// Voreinstellung werden hier behoben: (1) Schlägt die JSON-Deserialisierung fehl (z. B. String statt
// int), leakt die Roh-Meldung von System.Text.Json den internen DTO-Typnamen; (2) der als null
// gebundene Body-Parameter erzeugt zusätzlich ein irreführendes „field is required". Die API-Antworten
// sind bewusst englisch (Internationalisierung) – die Lokalisierung übernimmt der Client.
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = context =>
    {
        var modelState = context.ModelState;
        // JSON-Deserialisierungsfehler kommen mit „$"-Pfad-Keys (z. B. „$.adultId"). Existiert ein
        // solcher, konnte der Body nicht geparst werden – der als null gebundene Body-Parameter erzeugt
        // dann einen irreführenden „field is required"-Eintrag (ohne „$"). Nur DEN unterdrücken, nicht
        // echte Route-/Query-/Feld-Fehler (die trotz Body-Parse-Fehler legitim sind).
        var hasJsonError = modelState.Keys.Any(key => key.StartsWith('$'));
        // Nur der als null gebundene Body-Parameter erzeugt das irreführende „field is required" – nicht
        // ein echt fehlender Route-/Query-Parameter. Darum gezielt an den Body-Parameternamen knüpfen
        // (statt „jeder nicht-$-Key"), sonst würden legitime fehlende Route-/Query-Pflichtfelder verschluckt.
        var bodyParamNames = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        // Die (Body-)Parameter-Typen der Action, gegen die wir einen JSON-Pfad wie „$.unitType" auflösen,
        // um bei ungültigen Enum-Werten die zulässigen Werte nennen zu können.
        var parameterTypes = context.ActionDescriptor.Parameters.Select(p => p.ParameterType);
        var errors = new Dictionary<string, string[]>();
        // Ein unbekanntes Feld ist keine fehlgeschlagene Wertprüfung, sondern ein Vertragsfehler beim
        // Aufrufer – es bekommt darum den eigenen Code `unknown_field` statt `validation_error`.
        var hasUnknownField = false;
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;
            if (hasJsonError && bodyParamNames.Contains(key)
                && entry.Errors.All(e => e.ErrorMessage.Contains("is required", StringComparison.Ordinal)))
                continue;

            // Ist der Pfad ein Enum-Feld, kennen wir dessen erlaubte Werte – unabhängig von der Rohmeldung.
            var enumType = EnumSchemaHelp.EnumTypeForJsonPath(parameterTypes, key);
            // Unbekanntes Feld (`UnmappedMemberHandling.Disallow`): Die Rohmeldung von System.Text.Json
            // nennt den internen DTO-Typnamen und darf darum – wie bei den Konvertierungsfehlern – nicht
            // nach außen. Erkannt wird sie am Meldungstext; das ist derselbe Weg wie beim bereits
            // bestehenden „could not be converted" und von `UnknownFieldTests` festgenagelt.
            if (entry.Errors.Any(e => e.ErrorMessage.Contains("could not be mapped to any .NET member", StringComparison.Ordinal)))
                hasUnknownField = true;
            var messages = entry.Errors
                .Select(e => e.ErrorMessage.Contains("could not be mapped to any .NET member", StringComparison.Ordinal)
                    ? "Unknown field. The request contract has no such field – remove it or check the API documentation."
                    : e.ErrorMessage.Contains("could not be converted", StringComparison.Ordinal)
                    ? enumType is not null
                        // Ungültiger Enum-Wert: die zulässigen Werte nennen (statt „irgendwas stimmte nicht").
                        ? $"The value is not one of the allowed values: {string.Join(", ", EnumSchemaHelp.AllowedValues(enumType))}."
                        // Sonstige Konvertierungsfehler (z. B. String statt int): Rohmeldung leakt den DTO-Typ.
                        : "The value is not of the expected type."
                    : e.ErrorMessage)
                .ToArray();
            // Nur das führende „$."-Token des JSON-Pfads entfernen (nicht per Zeichensatz, sonst wird
            // ein Wurzel-Key „$" zum leeren String und innere Punkte verschwinden).
            var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
            errors[name] = messages;
        }

        var apiError = hasUnknownField ? ApiErrors.UnknownField : ApiErrors.ValidationError;
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = apiError.Title,
            Type = apiError.TypeUri,
        };
        // Maschinenlesbarer Code + traceId – dieser Pfad baut das ProblemDetails selbst (umgeht die
        // Factory), muss die traceId-Extension daher wie alle anderen Fehlerpfade selbst setzen.
        problem.Extensions["code"] = apiError.Code;
        ProblemDetailsStamping.ApplyTraceId(problem, context.HttpContext);
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    };
});

// API-Versionierung über URL-Segment (/api/v1/…). Default 1.0; das Versionssegment steckt zentral
// in ApiRoutes.V1. Neue Brüche laufen künftig über eine parallele v2 statt über Abwärtskompatibilität.
builder.Services.AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
    })
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";
        o.SubstituteApiVersionInUrl = true;
    });
// Einheitliches Fehlerschema: alle Fehler (Validierung, Fach-Fehler, unbehandelte Exceptions) als
// RFC-konforme application/problem+json statt nackter Strings. Der CustomizeProblemDetails-Hook läuft
// für die Middleware-Pfade (UseExceptionHandler/UseStatusCodePages: leere 401/403/404/429, 500) und
// stempelt dort einen status-basierten Fehler-Code, falls keiner gesetzt ist.
// Vor dem ProblemDetails-Fallback: ein Client-Abbruch ist kein Serverfehler (siehe Handler).
builder.Services.AddExceptionHandler<ClientAbortExceptionHandler>();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    var status = ctx.ProblemDetails.Status ?? ctx.HttpContext.Response.StatusCode;
    if (status < 400) status = StatusCodes.Status500InternalServerError; // nie einen Erfolgsstatus stempeln
    ProblemDetailsStamping.StampFallback(ctx.ProblemDetails, status);
});
// MVC-Fehlerergebnisse (Problem()/ValidationProblem() UND die [ApiController]-Auto-Wandlung von
// NotFound()/Conflict()/… ) laufen NICHT über CustomizeProblemDetails, sondern über die
// ProblemDetailsFactory. Diese hier ersetzen, damit auch dieser Pfad einen Code stempelt.
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory,
    CodeStampingProblemDetailsFactory>();
builder.Services.AddDbContext<PuglingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=pugling.db"));
// Die Uhr als Abhängigkeit – bewusst nur dort genutzt, wo eine Regel im Sekunden-Bereich greift (die
// Anti-Farming-Untergrenze des Schnelle-Antwort-Bonus). Tageslogik bleibt bei `DateTime.UtcNow`: sie ist
// mit Kalendertagen prüfbar, die Antwortzeit nicht. Ohne diese Naht müsste ein Test zwei Requests binnen
// einer Sekunde durchbringen und wäre auf einem ausgelasteten Runner ein Flake.
builder.Services.AddSingleton(TimeProvider.System);
// Die Zeitfenster des Punkte-Multiplikators sind Konfiguration, keine Tabelle (E12) – der Service ist
// damit eine reine Funktion und braucht weder DB noch Abbruch-Token.
builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection(ScoringOptions.SectionName));
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<GamificationService>();
// Positions-basierter Lern-Motor: Üben/Leitner pro Lehrplan-Position.
builder.Services.AddScoped<PositionPlayService>();
// Ziel-/Punkte-Engine des Positions-Modells: Erledigt-Regel je CheckMode + idempotente Ziel-Punkte.
builder.Services.AddScoped<PositionProgressService>();
// Lern-Report je Position: „welche Vokabel sitzt/sitzt nicht" (Box/Beherrschung + Test-Trefferquote).
builder.Services.AddScoped<PositionReportService>();
// Birkenbihl-Automatik: Satz tokenisieren + Wörter im Vokabelspeicher nachschlagen (Wort-für-Wort-Dekodierung).
builder.Services.AddScoped<BirkenbihlDecodingService>();
// Findet-sonst-legt-an: sichert, dass jede in einer Übung genutzte Vokabel im zentralen Store liegt.
builder.Services.AddScoped<VocabularyStoreService>();
// Sucht zu einem Kind den fachkundigen Creator (Reihe > Fach > Klassenstufe > Schulart). Deterministisch,
// damit derselbe Datenstand denselben Lehrer liefert – die Herkunft einer Übung bleibt nachvollziehbar.
builder.Services.AddScoped<CreatorProfileService>();
// Findet-sonst-legt-an für die geteilte Interessen-Taxonomie – die eine Stelle, an der aus Text ein Tag
// wird (Creator taggt Bilder, Supervisor pflegt Interessen, Backfill übernimmt Freitext). Getrennte
// Wege würden Dubletten erzeugen und das Matching „Bild ↔ Kind" genau dort leerlaufen lassen.
builder.Services.AddScoped<InterestTagService>();
// Markdown-Schnappschuss der Test-Anmerkungen: die einzige Brücke zu den Skills, die gegen eine
// Wegwerf-DB laufen und die echten Anmerkungen nur als Datei im Repo sehen können.
builder.Services.AddScoped<RemarkExportService>();
// Zuordnung Bild ⇢ Träger (Vokabel/Item/Übung). Drei Träger, ein Ablauf – der Service hält ihn an einer
// Stelle; die Controller unterscheiden sich nur in Route und Rechte-Prüfung.
builder.Services.AddScoped<MediaLinkService>();
// Die Stelle, an der Medien-Store und Kind-Profil zusammenkommen: aus „viele Darstellungen" wird EIN Bild
// (hart filtern nach Eignung/Abneigung, dann nach Interessen bewerten) – und die Wahl wird eingefroren,
// weil Bildkonstanz beim Vokabellernen der Merkeffekt ist.
builder.Services.AddScoped<MediaSelector>();
// Bild-Upload: Ablage hinter einer Schnittstelle (heute lokales Dateisystem, später ggf. Blob-Storage)
// plus die serverseitige Varianten-Erzeugung. Beide zustandslos → Singleton.
builder.Services.AddSingleton(builder.Configuration.GetSection("Media").Get<MediaOptions>() ?? new MediaOptions());
builder.Services.AddSingleton<IMediaStorage, LocalMediaStorage>();
// Test-Anmerkungen: Der kontenübergreifende Blick (`?scope=all`) ist in der **Entwicklung** offen und sonst
// zu. Die Vorgabe steht hier und nicht in einer appsettings-Datei, damit sie einen frischen Clone ohne
// Zusatzschritt richtig bedient; ein ausdrückliches `Remarks:GlobalRead` in der Konfiguration gewinnt.
builder.Services.AddSingleton(builder.Configuration.GetSection("Remarks").Get<RemarkOptions>()
    ?? new RemarkOptions { GlobalRead = builder.Environment.IsDevelopment() });
builder.Services.AddSingleton<MediaImageProcessor>();
// Pflegt die stabil identifizierten Items einer Vokabelübung (ID-erhaltender Abgleich Config → Item-Tabelle).
builder.Services.AddScoped<ExerciseItemService>();
// Schreibt den plan-übergreifenden Lernstand je (Kind, Item) fort und protokolliert die Antwort-Historie.
builder.Services.AddScoped<ItemProgressService>();
// Kind-zentrische Drill-down-Sicht auf den Vokabel-Lernstand entlang der Katalog-Hierarchie (Fach→Kapitel→Übung→Item).
builder.Services.AddScoped<ChildLearnProgressService>();
// Ergebnis-/Beherrschungsziele je Kind auf einem Katalog-Scope; live gegen den Lernstand ausgewertet.
// „Große Ziele" (OKR-Kern): Auswertung (Lernstand + Klassenarbeits-Note), Vater-CRUD und idempotente Belohnung.
builder.Services.AddScoped<ObjectiveEvaluationService>();
builder.Services.AddScoped<ObjectiveService>();
builder.Services.AddScoped<ObjectiveRewardService>();
// Kindübergreifendes Tages-Dashboard des Vaters („wer hat heute was geschafft?").
builder.Services.AddScoped<ChildrenDashboardService>();
builder.Services.AddScoped<AuthAccess>();
builder.Services.AddScoped<ExercisePermissionService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PlanOwnershipFilter>();
builder.Services.AddScoped<ChildOwnershipFilter>();
// Betriebs-/Monitoring-Sonde: prüft, ob die API läuft UND die Datenbank erreichbar/migriert ist.
builder.Services.AddHealthChecks().AddDbContextCheck<PuglingDbContext>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ArithmeticProblemGenerator>();
builder.Services.AddSingleton<AnswerGrader>();
// Übungstypen als Plugin-Contract: jeder Typ eine Klasse (IExerciseType), aufgelöst über die Registry
// (ersetzt das frühere ExerciseType-Enum + die verstreuten switch-/Checker-Stellen).
builder.Services.AddExerciseTypes();
// Extraktion der Übungs-Inhalte aus der ConfigJson (dünne Fassade über die Registry).
builder.Services.AddSingleton<ExerciseContentProvider>();
// DB-gestützte Auflösung (u. a. Vokabel-Store-Refs → ContentItems); scoped wegen DbContext.
builder.Services.AddScoped<ExerciseContentResolver>();
// Testmodus: Vater spielt eine Übung nebenwirkungsfrei durch (nutzt Resolver + AnswerGrader); scoped wegen Resolver.
builder.Services.AddScoped<ExercisePreviewService>();
// Erlaubte Origins aus der Konfiguration (`Cors:Origins`, kommagetrennt oder als Array); Default ist der
// Vite-Dev-Server. Konfigurierbar, weil ein Prod-Deploy unter eigenem Namen läuft – dort wäre ein
// fest verdrahtetes localhost der Grund, warum die App „ohne Fehlermeldung nichts lädt".
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? builder.Configuration.GetValue<string>("Cors:Origins")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    // WithExposedHeaders: sonst darf die Browser-App den Paging-Header X-Total-Count nicht lesen
    // (AllowAnyHeader gilt nur für Request-Header, nicht für die Freigabe von Response-Headern).
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()
        .WithExposedHeaders("X-Total-Count")));

// Login-Bremse gegen PIN-Brute-Force: pro IP nur wenige Versuche je Minute (Policy "login" auf den
// Auth-Endpunkten). Per Konfiguration abschaltbar, weil der In-Process-TestServer sich sonst eine
// IP-Partition teilt und die vielen Test-Logins fälschlich 429 bekämen.
var loginRateLimitEnabled = builder.Configuration.GetValue("RateLimiting:LoginEnabled", true);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", http => loginRateLimitEnabled
        ? RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
        : RateLimitPartition.GetNoLimiter("disabled"));
});

// JWT-Authentifizierung (PIN-Login stellt die Tokens aus, siehe AuthController/TokenService).
// Fail-fast: außerhalb der Entwicklung darf NICHT mit dem Dev-Fallback-Schlüssel signiert werden.
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
    throw new InvalidOperationException("Konfiguration 'Jwt:Key' muss in Nicht-Dev-Umgebungen gesetzt sein.");
var tokenService = new TokenService(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = tokenService.SigningKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    });
builder.Services.AddAuthorization();

// OpenAPI: Bearer-Sicherheitsschema, damit Swagger UI einen "Authorize"-Button zeigt.
builder.Services.AddOpenApi(o =>
{
    o.AddOperationTransformer(new OpenApiExamplesOperationTransformer(
        OpenApiExampleCatalog.Load(builder.Environment.ContentRootPath)));

    // Enum-Felder in der Doku ausweisen: Der JsonStringEnumConverter emittiert bereits die enum-Werte im
    // Schema; hier zusätzlich die zulässigen Werte in die Beschreibung schreiben, damit Swagger/Scalar sie
    // gut lesbar zeigen (und die 400-Fehlermeldung „allowed values: …" ihr Gegenstück in der Doku hat).
    o.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            var names = EnumSchemaHelp.AllowedValues(context.JsonTypeInfo.Type);
            // Die API akzeptiert/liefert Enums als STRING (globaler JsonStringEnumConverter); der Generator
            // annotiert sie sonst als integer ohne Werteliste – also die Realität ins Schema schreiben:
            // string + explizite enum-Werte, plus die Werte in der Beschreibung für Swagger/Scalar.
            schema.Type = JsonSchemaType.String;
            schema.Enum = [.. names.Select(n => (JsonNode)JsonValue.Create(n))];
            var hint = $"Allowed values: {string.Join(", ", names)}.";
            schema.Description = string.IsNullOrEmpty(schema.Description) ? hint : $"{schema.Description}\n\n{hint}";
        }
        else if (schema.Properties is { Count: > 0 })
        {
            // required korrekt setzen: Der Generator markiert JEDEN Record-Konstruktorparameter als required –
            // auch nullbare (optionale) wie „string?"/„TEnum?". Neu berechnen anhand der Nullbarkeit, damit
            // Swagger/Scalar Pflicht- vs. optionale Felder wahrheitsgemäß ausweisen (v. a. Partial-Update-DTOs).
            schema.Required = new HashSet<string>(EnumSchemaHelp.RequiredJsonPropertyNames(context.JsonTypeInfo));
        }
        return Task.CompletedTask;
    });

    o.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Components ??= new OpenApiComponents();
        doc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT aus POST /api/auth/adult bzw. /api/auth/child einfügen.",
        };
        doc.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
            }
        ];
        return Task.CompletedTask;
    });
    // Tag-Reihenfolge in Swagger/Scalar steuern: Die Tags folgen der Ebene (Creator → Supervisor →
    // Student, Auth zuerst), innerhalb einer Ebene alphabetisch. Ohne das zeigt die UI die Gruppen in
    // zufälliger Controller-Ladereihenfolge. Rank nach Tag-Präfix, das die Rolle trägt.
    o.AddDocumentTransformer((doc, _, _) =>
    {
        if (doc.Tags is { Count: > 0 })
        {
            static int Rank(string name) => name switch
            {
                "Auth" => 0,
                _ when name.StartsWith("Creator – ", StringComparison.Ordinal) => 1,
                _ when name.StartsWith("Supervisor – ", StringComparison.Ordinal) => 2,
                _ when name.StartsWith("Student – ", StringComparison.Ordinal) => 3,
                _ => 9,
            };
            // SortedSet statt HashSet: dessen Enumerations-Reihenfolge ist vertraglich die Comparer-
            // Reihenfolge (HashSet garantiert keine), und der OpenAPI-Serializer emittiert die Tags in
            // Enumerations-Reihenfolge. Comparer = (Rang, dann Name) – ein Total-Order über eindeutige
            // Tag-Namen, sodass nichts dedupliziert wird.
            var byRankThenName = Comparer<OpenApiTag>.Create((a, b) =>
                Rank(a.Name ?? "").CompareTo(Rank(b.Name ?? "")) is var r and not 0
                    ? r
                    : string.CompareOrdinal(a.Name, b.Name));
            doc.Tags = new SortedSet<OpenApiTag>(doc.Tags, byRankThenName);
        }
        return Task.CompletedTask;
    });
    // Fehler-Codes im Schema dokumentieren: die ProblemDetails-Schemata um die maschinenlesbare
    // code-Property (mit enum aller bekannten Codes) erweitern, damit Swagger/Clients sie kennen.
    o.AddDocumentTransformer((doc, _, _) =>
    {
        if (doc.Components?.Schemas is { } schemas)
        {
            foreach (var name in new[] { "ProblemDetails", "HttpValidationProblemDetails" })
            {
                if (schemas.TryGetValue(name, out var schema) && schema is OpenApiSchema s)
                {
                    s.Properties ??= new Dictionary<string, IOpenApiSchema>();
                    s.Properties["code"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = "Stabiler, maschinenlesbarer Fehler-Code (Client-Verzweigung/-Lokalisierung).",
                        Enum = [.. ApiErrors.AllCodes.Select(c => (JsonNode)JsonValue.Create(c))],
                    };
                }
            }
        }
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Unbehandelte Exceptions → problem+json (500); leere Fehler-Antworten (z. B. 404/403/401) ebenso.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Single-Host-Deploy: dieselbe App liefert die gebaute React-PWA (frontend/dist → wwwroot) aus und
// bedient /api/* same-origin. Statische Assets sind öffentlich, daher vor der Authentifizierung.
// Lokal ist wwwroot leer (Frontend läuft über Vite :5173 mit /api-Proxy) → hier passiert nichts.
app.UseDefaultFiles();
app.UseStaticFiles();

// Hochgeladene Bilder aus einem EIGENEN Ordner ausliefern – bewusst nicht aus wwwroot: dorthin kopiert
// der Deploy das gebaute Frontend, ein Redeploy würde die Bilder der Familie mitlöschen. Öffentlich wie
// die übrigen statischen Assets (die URLs sind unratebar genug und stehen ohnehin in den Karten).
{
    var media = app.Services.GetRequiredService<MediaOptions>();
    // Über die Schnittstelle, nicht per Cast auf die lokale Ablage: eine Ablage, die ihre Dateien selbst
    // ausliefert (Blob-Storage), liefert keinen Anbieter – dann entfällt die Middleware still, statt den
    // Start mit einer InvalidCastException zu sprengen.
    if (app.Services.GetRequiredService<IMediaStorage>().CreateContentProvider() is { } mediaFiles)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = mediaFiles,
            RequestPath = media.PublicPath.TrimEnd('/'),
        });
}

// Eine Zusammenfassungszeile je Request (Methode, Pfad, Status, Dauer) statt der lärmenden
// Framework-Defaults; angereichert um Identität/TraceId, damit ein 4xx/5xx sofort zuordenbar ist.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier);
        if (http.User.FindFirst("fid")?.Value is { } fid) diag.Set("Fid", fid);
        if (http.User.FindFirst("cid")?.Value is { } cid) diag.Set("Cid", cid);
        if (http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value is { } role) diag.Set("Role", role);
    };
    // Diese Middleware liegt INNERHALB von UseExceptionHandler, sieht den Abbruch also noch als
    // Exception – ohne diese Stufe protokollierte sie ihn auf Error, obwohl der Handler ihn danach als
    // 499 abräumt. Ein weggenavigierter Nutzer soll die Fehlerliste nicht füllen.
    options.GetLevel = (http, _, ex) =>
        ex is OperationCanceledException && http.RequestAborted.IsCancellationRequested
            ? Serilog.Events.LogEventLevel.Debug
            : ex is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? Serilog.Events.LogEventLevel.Error
                : Serilog.Events.LogEventLevel.Information;
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
    // SQLite legt die DB-Datei selbst an, aber nicht deren Verzeichnis. Im Hosting (z. B. Azure App
    // Service) liegt die DB bewusst außerhalb des Deploy-Verzeichnisses (Data Source=/home/data/…),
    // damit sie Deployments überlebt – der Ordner muss vor Migrate existieren. Lokal ein No-op.
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
        db.Database.GetConnectionString()).DataSource;
    if (Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } dbDir)
        Directory.CreateDirectory(dbDir);
    // Die Migrationskette wurde zu einer einzigen `InitialCreate` zusammengefaltet (Altdaten waren
    // ausdrücklich verzichtbar). Eine DB, die noch Einträge der *alten* Kette trägt, hat damit ein
    // vollständiges Schema, aber keine der bekannten Migrationen – `Migrate()` würde die InitialCreate
    // anwenden wollen und mit `table "Adults" already exists` scheitern. Diese Meldung weist auf nichts
    // hin, also wird sie hier abgefangen und durch eine ersetzt, aus der die Handlung folgt.
    if (await db.Database.CanConnectAsync())
    {
        var known = db.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        if (applied.Count > 0 && applied.TrueForAll(m => !known.Contains(m)))
            throw new InvalidOperationException(
                $"Die Datenbank '{dataSource}' stammt aus der alten Migrationskette "
                + $"({applied.Count} angewandte, davon keine bekannt – z. B. '{applied[0]}'). "
                + "Die Kette wurde zu einer InitialCreate zusammengefaltet. Zeige den ConnectionString "
                + "auf eine neue Datei oder lösche die vorhandene – ein Upgrade-Pfad existiert bewusst nicht.");
    }
    // Durchgehend `await`: Top-Level-Statements dürfen das, und ein blockierendes
    // `GetAwaiter().GetResult()` beim Start ist genau das Muster, das anderswo Deadlocks erzeugt.
    await db.Database.MigrateAsync(); // wendet ausstehende EF-Migrationen an (Schema-Upgrade-Pfad)

    // Der Seed ist Demo-/Entwicklungsdaten und läuft darum vorgabemäßig nur in der Entwicklung – aber
    // über eine Einstellung übersteuerbar, weil die Azure-Instanz in Production läuft und die
    // Demo-Familie dort braucht (`Seed__Enabled=true`, siehe docs/db-struktur-umbau-plan.md).
    // Die früheren drei „Backfills" stecken jetzt darin: sie waren kein Altdaten-Pfad, sondern
    // Seed-Nachlauf – ohne sie hat eine frische DB Personen ohne Login und Vokabelübungen ohne Items.
    if (app.Configuration.GetValue("Seed:Enabled", app.Environment.IsDevelopment()))
        await Seed.RunAsync(db,
            scope.ServiceProvider.GetRequiredService<ExerciseItemService>(),
            scope.ServiceProvider.GetRequiredService<AccountService>(),
            scope.ServiceProvider.GetRequiredService<InterestTagService>(),
            // Bewusst None: das Säen beim Hochfahren hängt an keiner Anfrage, die abbrechen könnte.
            CancellationToken.None);
}

// OpenAPI-Dokument unter /openapi/v1.json + Swagger UI unter /swagger + Scalar UI unter /scalar/v1
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Pugling API v1");
    o.RoutePrefix = "swagger";
    // Den per „Authorize" eingegebenen Bearer-Token im Browser (localStorage) halten, damit ein
    // Reload beim Ausprobieren nicht sofort ein erneutes Authorisieren erzwingt.
    o.EnablePersistAuthorization();
});
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Pugling API v1")
        .AddPreferredSecuritySchemes("bearer")
        // Wie bei Swagger: eingegebene Authentifizierung über Reloads hinweg behalten.
        .EnablePersistentAuthentication();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
// Nach der Authentifizierung: Identität (Fid/Cid/Role) + TraceId in den Log-Kontext heben, damit
// jede Log-Zeile aus Controllern/Services (v. a. die Punkte-Buchungen) sie mitträgt.
app.UseMiddleware<RequestLogContextMiddleware>();
// Health-Endpunkt bewusst anonym (kein [Authorize]) – für Load-Balancer/Monitoring.
app.MapHealthChecks("/health");
app.MapControllers();
// Client-seitiges Routing: alle nicht von /api, /swagger, /health etc. bediente Pfade auf die SPA
// zurückfallen lassen, damit Direktaufrufe von /sohn, /vater usw. index.html laden (React-Router
// übernimmt). Greift nur, wenn wwwroot/index.html existiert (Prod-Build) – lokal 404 → egal.
app.MapFallbackToFile("index.html");
app.Run();

/// <summary>Made visible for integration tests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
