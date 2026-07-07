using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.OpenApi;
using Pugling.Api.Services;
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
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

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
        // JSON-Deserialisierungsfehler kommen mit „$"-Pfad-Keys (z. B. „$.fatherId"). Existiert ein
        // solcher, konnte der Body nicht geparst werden – der als null gebundene Body-Parameter erzeugt
        // dann einen irreführenden „field is required"-Eintrag (ohne „$"). Nur DEN unterdrücken, nicht
        // echte Route-/Query-/Feld-Fehler (die trotz Body-Parse-Fehler legitim sind).
        var hasJsonError = modelState.Keys.Any(key => key.StartsWith('$'));
        // Die (Body-)Parameter-Typen der Action, gegen die wir einen JSON-Pfad wie „$.unitType" auflösen,
        // um bei ungültigen Enum-Werten die zulässigen Werte nennen zu können.
        var parameterTypes = context.ActionDescriptor.Parameters.Select(p => p.ParameterType);
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;
            if (hasJsonError && !key.StartsWith('$')
                && entry.Errors.All(e => e.ErrorMessage.Contains("is required", StringComparison.Ordinal)))
                continue;

            // Ist der Pfad ein Enum-Feld, kennen wir dessen erlaubte Werte – unabhängig von der Rohmeldung.
            var enumType = EnumSchemaHelp.EnumTypeForJsonPath(parameterTypes, key);
            var messages = entry.Errors
                .Select(e => e.ErrorMessage.Contains("could not be converted", StringComparison.Ordinal)
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

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ApiErrors.ValidationError.Title,
            Type = ApiErrors.ValidationError.TypeUri,
        };
        // Maschinenlesbarer Code + traceId – dieser Pfad baut das ProblemDetails selbst (umgeht die
        // Factory), muss die traceId-Extension daher wie alle anderen Fehlerpfade selbst setzen.
        problem.Extensions["code"] = ApiErrors.ValidationError.Code;
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
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<OfferService>();
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
// Kindübergreifendes Tages-Dashboard des Vaters („wer hat heute was geschafft?").
builder.Services.AddScoped<ChildrenDashboardService>();
builder.Services.AddScoped<AuthAccess>();
builder.Services.AddScoped<PlanOwnershipFilter>();
builder.Services.AddScoped<ChildOwnershipFilter>();
// Betriebs-/Monitoring-Sonde: prüft, ob die API läuft UND die Datenbank erreichbar/migriert ist.
builder.Services.AddHealthChecks().AddDbContextCheck<PuglingDbContext>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ArithmeticProblemGenerator>();
builder.Services.AddSingleton<ExerciseAnswerChecker>();
builder.Services.AddSingleton<AnswerGrader>();
// Extraktion der Übungs-Inhalte aus der ConfigJson (Brücke Katalog → neuer Lehrplan-Motor, Etappe 2).
builder.Services.AddSingleton<ExerciseContentProvider>();
// DB-gestützte Auflösung (u. a. Vokabel-Store-Refs → ContentItems); scoped wegen DbContext.
builder.Services.AddScoped<ExerciseContentResolver>();
// Testmodus: Vater spielt eine Übung nebenwirkungsfrei durch (nutzt Resolver + AnswerGrader); scoped wegen Resolver.
builder.Services.AddScoped<ExercisePreviewService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    // WithExposedHeaders: sonst darf die Browser-App den Paging-Header X-Total-Count nicht lesen
    // (AllowAnyHeader gilt nur für Request-Header, nicht für die Freigabe von Response-Headern).
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()
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
            Description = "JWT aus POST /api/auth/father bzw. /api/auth/child einfügen.",
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

// Eine Zusammenfassungszeile je Request (Methode, Pfad, Status, Dauer) statt der lärmenden
// Framework-Defaults; angereichert um Identität/TraceId, damit ein 4xx/5xx sofort zuordenbar ist.
app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diag, http) =>
{
    diag.Set("TraceId", System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier);
    if (http.User.FindFirst("fid")?.Value is { } fid) diag.Set("Fid", fid);
    if (http.User.FindFirst("cid")?.Value is { } cid) diag.Set("Cid", cid);
    if (http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value is { } role) diag.Set("Role", role);
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
    db.Database.Migrate(); // wendet ausstehende EF-Migrationen an (Schema-Upgrade-Pfad)
    Seed.Run(db);
}

// OpenAPI-Dokument unter /openapi/v1.json + Swagger UI unter /swagger + Scalar UI unter /scalar/v1
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Pugling API v1");
    o.RoutePrefix = "swagger";
});
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Pugling API v1")
        .WithPreferredScheme("bearer");
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

/// <summary>Sichtbar gemacht für Integrationstests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
