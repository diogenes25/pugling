using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddDbContext<PuglingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=pugling.db"));
builder.Services.AddScoped<PointsService>();
builder.Services.AddScoped<StudyProgressService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<TestAttemptService>();
builder.Services.AddScoped<AuthAccess>();
builder.Services.AddScoped<PlanOwnershipFilter>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ArithmeticProblemGenerator>();
builder.Services.AddSingleton<ExerciseAnswerChecker>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

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
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
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
}));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
    db.Database.EnsureCreated(); // für den Start; später auf EF-Migrationen umstellen
    Seed.Run(db);
}

// OpenAPI-Dokument unter /openapi/v1.json + Swagger UI unter /swagger
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Pugling API v1");
    o.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>Sichtbar gemacht für Integrationstests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
