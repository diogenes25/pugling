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

// Serilog as the only logging backend: console (readable while developing) + a rolling JSON file
// (machine-readable, kept for 14 days). shared:true because in the integration tests several hosts write
// into the same file in parallel. Level/overrides come from the "Serilog" section of the configuration.
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/pugling-.clef",
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true));

builder.Services.AddControllers(o =>
        // Why a convention and not an attribute per action: see SuccessResponseConvention.
        o.Conventions.Add(new SuccessResponseConvention()))
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // **Unknown fields are rejected, not swallowed.** The default `Skip` turned a mistyped or outdated
        // field into a silent nothing: the caller got 201 Created and believed their value had arrived. For an
        // API-first product whose consumers are generated clients and AI agents, that is the most expensive
        // default there is - it turns a contract error into silent data loss.
        // See docs/codequalitaet-gates-plan.md (L3/B3).
        o.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
    });

// Serve model validation errors as clean, English ProblemDetails. Two problems of the default are fixed
// here: (1) if JSON deserialization fails (e.g. a string instead of an int), the raw System.Text.Json
// message leaks the internal DTO type name; (2) the body parameter bound as null additionally produces a
// misleading "field is required". The API responses are English on purpose (i18n) - localizing is the
// client's job.
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = context =>
    {
        var modelState = context.ModelState;
        // JSON deserialization errors come with "$" path keys (e.g. "$.adultId"). If one exists, the body
        // could not be parsed - the body parameter bound as null then produces a misleading "field is
        // required" entry (without "$"). Suppress only THAT one, not real route/query/field errors (which are
        // legitimate despite a body parse error).
        var hasJsonError = modelState.Keys.Any(key => key.StartsWith('$'));
        // Only the body parameter bound as null produces that misleading "field is required" - not a genuinely
        // missing route/query parameter. So tie it to the body parameter name (instead of "every non-$ key"),
        // otherwise legitimate missing required route/query fields would be swallowed.
        var bodyParamNames = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        // The action's (body) parameter types, against which we resolve a JSON path like "$.unitType" in order
        // to name the allowed values for an invalid enum value.
        var parameterTypes = context.ActionDescriptor.Parameters.Select(p => p.ParameterType);
        var errors = new Dictionary<string, string[]>();
        // An unknown field is not a failed value check but a contract error on the caller's side - so it gets
        // its own code `unknown_field` instead of `validation_error`.
        var hasUnknownField = false;
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;
            if (hasJsonError && bodyParamNames.Contains(key)
                && entry.Errors.All(e => e.ErrorMessage.Contains("is required", StringComparison.Ordinal)))
                continue;

            // If the path is an enum field we know its allowed values - independent of the raw message.
            var enumType = EnumSchemaHelp.EnumTypeForJsonPath(parameterTypes, key);
            // Unknown field (`UnmappedMemberHandling.Disallow`): the raw System.Text.Json message names the
            // internal DTO type name and therefore must not reach the outside - as with the conversion errors.
            // It is recognized by its message text; the same route as the existing "could not be converted",
            // and pinned down by `UnknownFieldTests`.
            if (entry.Errors.Any(e => e.ErrorMessage.Contains("could not be mapped to any .NET member", StringComparison.Ordinal)))
                hasUnknownField = true;
            var messages = entry.Errors
                .Select(e => e.ErrorMessage.Contains("could not be mapped to any .NET member", StringComparison.Ordinal)
                    ? "Unknown field. The request contract has no such field – remove it or check the API documentation."
                    : e.ErrorMessage.Contains("could not be converted", StringComparison.Ordinal)
                    ? enumType is not null
                        // Invalid enum value: name the allowed values (instead of "something was wrong").
                        ? $"The value is not one of the allowed values: {string.Join(", ", EnumSchemaHelp.AllowedValues(enumType))}."
                        // Other conversion errors (e.g. string instead of int): the raw message leaks the DTO type.
                        : "The value is not of the expected type."
                    : e.ErrorMessage)
                .ToArray();
            // Strip only the leading "$." token of the JSON path (not by character set, otherwise a root key
            // "$" becomes the empty string and inner dots disappear).
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
        // Machine-readable code + traceId - this path builds the ProblemDetails itself (bypassing the
        // factory), so it has to set the traceId extension itself like every other error path.
        problem.Extensions["code"] = apiError.Code;
        ProblemDetailsStamping.ApplyTraceId(problem, context.HttpContext);
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    };
});

// API versioning through a URL segment (/api/v1/…). Default 1.0; the version segment sits centrally in
// ApiRoutes.V1. Future breaking changes go through a parallel v2 instead of backwards compatibility.
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
// One uniform error schema: all errors (validation, domain errors, unhandled exceptions) as RFC-compliant
// application/problem+json instead of bare strings. The CustomizeProblemDetails hook runs for the
// middleware paths (UseExceptionHandler/UseStatusCodePages: empty 401/403/404/429, 500) and stamps a
// status-based error code there if none is set.
// Before the ProblemDetails fallback: a client abort is not a server error (see the handler).
builder.Services.AddExceptionHandler<ClientAbortExceptionHandler>();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    var status = ctx.ProblemDetails.Status ?? ctx.HttpContext.Response.StatusCode;
    if (status < 400) status = StatusCodes.Status500InternalServerError; // never stamp a success status
    ProblemDetailsStamping.StampFallback(ctx.ProblemDetails, status);
});
// MVC error results (Problem()/ValidationProblem() AND the [ApiController] auto-conversion of
// NotFound()/Conflict()/…) do NOT run through CustomizeProblemDetails but through the
// ProblemDetailsFactory. Replace it here so that this path stamps a code as well.
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory,
    CodeStampingProblemDetailsFactory>();
builder.Services.AddDbContext<PuglingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=pugling.db"));
// The clock as a dependency - deliberately used only where a rule works in the second range (the
// anti-farming lower bound of the fast-answer bonus). Day logic stays on `DateTime.UtcNow`: it can be
// tested with calendar days, the answer time cannot. Without this seam a test would have to push two
// requests through within one second and would be a flake on a busy runner.
builder.Services.AddSingleton(TimeProvider.System);
// The time windows of the points multiplier are configuration, not a table (E12) - which makes the service
// a pure function that needs neither a DB nor a cancellation token.
builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection(ScoringOptions.SectionName));
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<GamificationService>();
// The position-based learning engine: practice/Leitner per study plan position.
builder.Services.AddScoped<PositionPlayService>();
// Goal/points engine of the position model: the done rule per check mode + idempotent goal points.
builder.Services.AddScoped<PositionProgressService>();
// Learning report per position: "which word sits and which does not" (box/mastery + test hit rate).
builder.Services.AddScoped<PositionReportService>();
// Birkenbihl automation: tokenize a sentence + look the words up in the vocabulary store (word-for-word decoding).
builder.Services.AddScoped<BirkenbihlDecodingService>();
// Find-or-create: makes sure every word used in an exercise sits in the central store.
builder.Services.AddScoped<VocabularyStoreService>();
// Finds the knowledgeable creator for a child (series > subject > grade > school type). Deterministic, so
// that the same data yields the same teacher - where an exercise came from stays traceable.
builder.Services.AddScoped<CreatorProfileService>();
// Find-or-create for the shared interest taxonomy - the one place where text becomes a tag (the creator
// tags images, the supervisor maintains interests, the backfill takes over free text). Separate paths would
// create duplicates and make the "image ↔ child" matching run dry exactly there.
builder.Services.AddScoped<InterestTagService>();
// Markdown snapshot of the test remarks: the only bridge to the skills, which run against a throwaway DB
// and can only see the real remarks as a file in the repository.
builder.Services.AddScoped<RemarkExportService>();
// Image ⇢ carrier assignment (vocabulary/item/exercise). Three carriers, one flow - the service keeps it in
// one place; the controllers differ only in route and rights check.
builder.Services.AddScoped<MediaLinkService>();
// The place where the media store and the child profile meet: "many renditions" become ONE image (filter
// hard by suitability/dislikes, then score by interests) - and the choice is frozen, because image
// constancy is the retention effect when learning vocabulary.
builder.Services.AddScoped<MediaSelector>();
// Image upload: storage behind an interface (today the local file system, later possibly blob storage)
// plus the server-side variant generation. Both stateless → singleton.
builder.Services.AddSingleton(builder.Configuration.GetSection("Media").Get<MediaOptions>() ?? new MediaOptions());
builder.Services.AddSingleton<IMediaStorage, LocalMediaStorage>();
// Test remarks: the cross-account view (`?scope=all`) is open in **development** and closed otherwise. The
// default lives here and not in an appsettings file so that a fresh clone is served correctly without an
// extra step; an explicit `Remarks:GlobalRead` in the configuration wins.
builder.Services.AddSingleton(builder.Configuration.GetSection("Remarks").Get<RemarkOptions>()
    ?? new RemarkOptions { GlobalRead = builder.Environment.IsDevelopment() });
builder.Services.AddSingleton<MediaImageProcessor>();
// Maintains the stably identified items of a vocabulary exercise (id-preserving sync config → item table).
builder.Services.AddScoped<ExerciseItemService>();
// Rolls the cross-plan learning state per (child, item) forward and records the answer history.
builder.Services.AddScoped<ItemProgressService>();
// Child-centric drill-down view on vocabulary progress along the catalog hierarchy (subject→chapter→exercise→item).
builder.Services.AddScoped<ChildLearnProgressService>();
// Outcome/mastery goals per child on a catalog scope; evaluated live against the learning state.
// "Big goals" (the OKR core): evaluation (learning state + class test grade), supervisor CRUD and idempotent rewards.
builder.Services.AddScoped<ObjectiveEvaluationService>();
builder.Services.AddScoped<ObjectiveService>();
builder.Services.AddScoped<ObjectiveRewardService>();
// The supervisor's cross-child daily dashboard ("who managed what today?").
builder.Services.AddScoped<ChildrenDashboardService>();
builder.Services.AddScoped<AuthAccess>();
builder.Services.AddScoped<ExercisePermissionService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PlanOwnershipFilter>();
builder.Services.AddScoped<ChildOwnershipFilter>();
// Operations/monitoring probe: checks whether the API is running AND the database is reachable/migrated.
builder.Services.AddHealthChecks().AddDbContextCheck<PuglingDbContext>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ArithmeticProblemGenerator>();
builder.Services.AddSingleton<AnswerGrader>();
// Exercise types as a plugin contract: one class per type (IExerciseType), resolved through the registry
// (replaces the former ExerciseType enum and the switch/checker sites scattered around).
builder.Services.AddExerciseTypes();
// Extraction of the exercise contents from the ConfigJson (a thin facade over the registry).
builder.Services.AddSingleton<ExerciseContentProvider>();
// DB-backed resolution (vocabulary store refs → ContentItems, among others); scoped because of the DbContext.
builder.Services.AddScoped<ExerciseContentResolver>();
// Preview mode: the supervisor plays an exercise through without side effects (uses resolver + AnswerGrader); scoped because of the resolver.
builder.Services.AddScoped<ExercisePreviewService>();
// Allowed origins from the configuration (`Cors:Origins`, comma-separated or as an array); the default is
// the Vite dev server. Configurable because a prod deploy runs under its own name - a hard-wired localhost
// would be the reason the app "loads nothing without an error message" there.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? builder.Configuration.GetValue<string>("Cors:Origins")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    // WithExposedHeaders: otherwise the browser app may not read the paging header X-Total-Count
    // (AllowAnyHeader covers request headers only, not the release of response headers).
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()
        .WithExposedHeaders("X-Total-Count")));

// Login throttle against PIN brute force: only a few attempts per IP per minute (policy "login" on the auth
// endpoints). Switchable through configuration, because the in-process test server would otherwise share
// one IP partition and its many test logins would wrongly get 429.
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

// JWT authentication (the PIN login issues the tokens, see AuthController/TokenService).
// Fail fast: outside development, signing with the dev fallback key is NOT allowed.
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

// OpenAPI: a bearer security scheme so that Swagger UI shows an "Authorize" button.
builder.Services.AddOpenApi(o =>
{
    // `OpenApi:ExamplesEnabled=false` yields the **contract-pure** document: schemas, paths and status codes,
    // without the verified examples. Only the test that checks in docs/openapi/v1.json switches it off - the
    // examples are documentation, not contract, and they are not byte-stable (see OpenApiExampleCatalog.Empty).
    o.AddOperationTransformer(new OpenApiExamplesOperationTransformer(
        builder.Configuration.GetValue("OpenApi:ExamplesEnabled", true)
            ? OpenApiExampleCatalog.Load(builder.Environment.ContentRootPath)
            : OpenApiExampleCatalog.Empty));

    // Spell enum fields out in the documentation: the JsonStringEnumConverter already emits the enum values
    // in the schema; here we additionally write the allowed values into the description so that Swagger/Scalar
    // show them readably (and the 400 message "allowed values: …" has its counterpart in the docs).
    o.AddSchemaTransformer((schema, context, _) =>
    {
        // Nullable<TEnum> is NOT IsEnum - so an enum the generator reaches through a "TEnum?" field first
        // ended up as bare type integer without values, although the API sends the name.
        var enumType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (enumType.IsEnum)
        {
            var names = EnumSchemaHelp.AllowedValues(enumType);
            // The API accepts/returns enums as STRINGS (the global JsonStringEnumConverter); otherwise the
            // generator annotates them as integer without a value list - so write reality into the schema:
            // string + explicit enum values, plus the values in the description for Swagger/Scalar.
            schema.Type = JsonSchemaType.String;
            schema.Enum = [.. names.Select(n => (JsonNode)JsonValue.Create(n))];
            var hint = $"Allowed values: {string.Join(", ", names)}.";
            schema.Description = string.IsNullOrEmpty(schema.Description) ? hint : $"{schema.Description}\n\n{hint}";
        }
        else if (schema.Properties is { Count: > 0 })
        {
            // Set required correctly: the generator marks EVERY record constructor parameter as required -
            // including nullable (optional) ones such as "string?"/"TEnum?". Recompute it from the nullability
            // so that Swagger/Scalar report required vs. optional truthfully (partial-update DTOs above all).
            // SortedSet instead of HashSet - the same reason as with the tags below: only the sorted set
            // promises an enumeration order, and the serializer emits `required` in that order. Since the
            // document is checked in and diffed (docs/openapi/README.md), that order is now part of a gate;
            // HashSet happened to enumerate in insertion order, which is an implementation detail, not a
            // promise.
            // A parameter with a DEFAULT VALUE is optional, however non-nullable it may be: omitting
            // `bool ClearBirthYear = false` or `string Format = "webp"` is legal and the server fills in the
            // default. Nullability alone cannot see that; the generator has already written the default into
            // the property schema, so that is the reliable signal. An explicitly `required`/[JsonRequired]
            // member keeps precedence - otherwise this would silently contradict the promise that
            // EnumSchemaHelp.RequiredJsonPropertyNames makes.
            // KNOWN LIMIT: `required` is a statement per SCHEMA, "has a default" one about binding the
            // REQUEST. Where one schema serves both directions, a field that is always present in the
            // response now looks optional - today that is only ArithmeticProblem.Tolerance.
            var explicitlyRequired = context.JsonTypeInfo.Properties
                .Where(p => p.IsRequired)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.Ordinal);
            var withDefault = schema.Properties
                .Where(p => p.Value.Default is not null && !explicitlyRequired.Contains(p.Key))
                .Select(p => p.Key);
            schema.Required = new SortedSet<string>(
                EnumSchemaHelp.RequiredJsonPropertyNames(context.JsonTypeInfo).Except(withDefault),
                StringComparer.Ordinal);
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
    // Control the tag order in Swagger/Scalar: the tags follow the tier (creator → supervisor → student, auth
    // first), alphabetically within a tier. Without this the UI shows the groups in random controller load
    // order. Rank by the tag prefix that carries the role.
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
            // SortedSet instead of HashSet: its enumeration order is contractually the comparer order (HashSet
            // guarantees none), and the OpenAPI serializer emits the tags in enumeration order. Comparer =
            // (rank, then name) - a total order over unique tag names, so nothing gets deduplicated.
            var byRankThenName = Comparer<OpenApiTag>.Create((a, b) =>
                Rank(a.Name ?? "").CompareTo(Rank(b.Name ?? "")) is var r and not 0
                    ? r
                    : string.CompareOrdinal(a.Name, b.Name));
            doc.Tags = new SortedSet<OpenApiTag>(doc.Tags, byRankThenName);
        }
        return Task.CompletedTask;
    });
    // Document the error codes in the schema: extend the ProblemDetails schemas with the machine-readable
    // code property (with an enum of all known codes) so that Swagger/clients know them.
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

// Unhandled exceptions → problem+json (500); empty error responses (e.g. 404/403/401) likewise.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Single-host deploy: the same app serves the built React PWA (frontend/dist → wwwroot) and serves /api/*
// same-origin. Static assets are public, hence before authentication.
// Locally wwwroot is empty (the frontend runs through Vite :5173 with an /api proxy) → nothing happens here.
app.UseDefaultFiles();
app.UseStaticFiles();

// Serve uploaded images from their OWN folder - deliberately not from wwwroot: the deploy copies the built
// frontend there, and a redeploy would delete the family's images with it. Public like the other static
// assets (the URLs are unguessable enough and appear in the cards anyway).
{
    var media = app.Services.GetRequiredService<MediaOptions>();
    // Through the interface, not by casting to the local storage: a storage that serves its files itself
    // (blob storage) returns no provider - the middleware then quietly drops out instead of blowing up
    // startup with an InvalidCastException.
    if (app.Services.GetRequiredService<IMediaStorage>().CreateContentProvider() is { } mediaFiles)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = mediaFiles,
            RequestPath = media.PublicPath.TrimEnd('/'),
        });
}

// One summary line per request (method, path, status, duration) instead of the noisy framework defaults;
// enriched with identity/traceId so that a 4xx/5xx can be attributed immediately.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier);
        if (http.User.FindFirst("fid")?.Value is { } fid) diag.Set("Fid", fid);
        if (http.User.FindFirst("cid")?.Value is { } cid) diag.Set("Cid", cid);
        if (http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value is { } role) diag.Set("Role", role);
    };
    // This middleware sits INSIDE UseExceptionHandler, so it still sees the abort as an exception - without
    // this step it logged it at Error even though the handler clears it up as a 499 afterwards. A user who
    // navigated away should not fill the error list.
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
    // SQLite creates the DB file itself, but not its directory. In hosting (e.g. Azure App Service) the DB
    // deliberately sits outside the deploy directory (Data Source=/home/data/…) so that it survives
    // deployments - the folder must exist before Migrate. Locally a no-op.
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
        db.Database.GetConnectionString()).DataSource;
    if (Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } dbDir)
        Directory.CreateDirectory(dbDir);
    // The migration chain was folded into a single `InitialCreate` (legacy data was explicitly expendable). A
    // DB still carrying entries of the *old* chain therefore has a complete schema but none of the known
    // migrations - `Migrate()` would want to apply the InitialCreate and fail with `table "Adults" already
    // exists`. That message points at nothing, so it is caught here and replaced by one an action follows from.
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
    // `await` throughout: top-level statements may do that, and a blocking `GetAwaiter().GetResult()` at
    // startup is exactly the pattern that produces deadlocks elsewhere.
    await db.Database.MigrateAsync(); // applies pending EF migrations (the schema upgrade path)

    // The seed is demo/development data and therefore runs by default in development only - but overridable
    // through a setting, because the Azure instance runs in Production and needs the demo family there
    // (`Seed__Enabled=true`, see docs/db-struktur-umbau-plan.md). The former three "backfills" now sit inside
    // it: they were not a legacy-data path but seed follow-up - without them a fresh DB has people without a
    // login and vocabulary exercises without items.
    if (app.Configuration.GetValue("Seed:Enabled", app.Environment.IsDevelopment()))
        await Seed.RunAsync(db,
            scope.ServiceProvider.GetRequiredService<ExerciseItemService>(),
            scope.ServiceProvider.GetRequiredService<AccountService>(),
            scope.ServiceProvider.GetRequiredService<InterestTagService>(),
            // Deliberately None: seeding at startup hangs on no request that could be aborted.
            CancellationToken.None);
}

// The OpenAPI document at /openapi/v1.json + Swagger UI at /swagger + Scalar UI at /scalar/v1
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Pugling API v1");
    o.RoutePrefix = "swagger";
    // Keep the bearer token entered through "Authorize" in the browser (localStorage) so that a reload while
    // trying things out does not immediately force authorizing again.
    o.EnablePersistAuthorization();
});
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Pugling API v1")
        .AddPreferredSecuritySchemes("bearer")
        // As with Swagger: keep the entered authentication across reloads.
        .EnablePersistentAuthentication();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
// After authentication: lift identity (fid/cid/role) + traceId into the log context so that every log line
// from controllers/services (the points entries above all) carries them.
app.UseMiddleware<RequestLogContextMiddleware>();
// The health endpoint is deliberately anonymous (no [Authorize]) - for load balancers/monitoring.
app.MapHealthChecks("/health");
app.MapControllers();
// Client-side routing: let every path not served by /api, /swagger, /health etc. fall back to the SPA, so
// that direct calls to /sohn, /vater and so on load index.html (React Router takes over). Only applies if
// wwwroot/index.html exists (the prod build) - locally a 404, which does not matter.
app.MapFallbackToFile("index.html");
app.Run();

/// <summary>Made visible for integration tests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
