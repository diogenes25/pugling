using pugling.Application;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.Persistance;
using pugling.Models;
using pugling.Services;
using Serilog;
using Swashbuckle.AspNetCore.Filters; // Add this using directive at the top of the file

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register application services for Dependency Injection
builder.Services.AddDbServices();
builder.Services.AddScoped<IVocabularyDbService<VocabularyDto, Vocabulary>, VocabularyPersit>();
builder.Services.AddScoped<VocabularyService>();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console() // Add this line to log to the console
    .CreateLogger();

//builder.Host.UseSerilog(Log.Logger); // Use Serilog for logging
builder.Host.UseSerilog(); // Ensure Serilog is configured for the host

// Include XML comments
var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Vocabulary API",
        Version = "v1",
        Description = "API for managing vocabulary items.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Email = "huhu@huhu.com",
            Name = "Huhu",
            Url = new Uri("https://example.com")
        }
    });

    options.IncludeXmlComments(xmlPath);

    // Enable examples
    options.ExampleFilters();
});

// Register the example provider
builder.Services.AddSwaggerExamplesFromAssemblyOf<pugling.Controllers.ModelExamples.VocabularyDtoExample>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // https://localhost:7261/openapi/v1.json
    app.MapOpenApi();
    //app.UseSwagger();
    // Enable Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PugLing API V1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at the root (https://localhost:7261/)
        options.DocumentTitle = "PugLing API Documentation"; // Set the title of the Swagger UI page
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();