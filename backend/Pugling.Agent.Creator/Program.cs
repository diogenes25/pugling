using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// Einstiegspunkt des KI-Creators. Die App bedient die Pugling-API von außen – sie ist ein
/// <b>Konsument</b> der REST-Oberfläche, kein Teil des Servers. Alles läuft lokal: die API auf
/// <c>localhost:5200</c>, das Sprachmodell in Ollama.
/// </summary>
public static class Program
{
    /// <summary>Exit-Codes: 0 = fertig, 1 = fachlich gescheitert, 2 = falsch aufgerufen, 130 = abgebrochen.</summary>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var command = CommandLine.Parse(args);
            if (string.Equals(command.Verb, "help", StringComparison.OrdinalIgnoreCase))
            {
                AgentCommands.PrintUsage();
                return 0;
            }

            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellation.Cancel();
            };

            using var host = BuildHost();
            return await host.Services.GetRequiredService<AgentCommands>().RunAsync(command, cancellation.Token);
        }
        catch (AgentUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            AgentCommands.PrintUsage();
            return 2;
        }
        catch (OptionsValidationException ex)
        {
            Console.Error.WriteLine($"Konfiguration unvollständig: {string.Join(" ", ex.Failures)}");
            Console.Error.WriteLine("Die PIN gehört nicht in appsettings.json – setze sie als Secret oder Umgebungsvariable:");
            Console.Error.WriteLine("  dotnet user-secrets set \"Pugling:Pin\" \"0000\"   (im Projektordner)");
            return 2;
        }
        catch (PuglingApiException ex)
        {
            Console.Error.WriteLine($"Die Pugling-API antwortete mit {(int)ex.StatusCode} ({ex.Code}): {ex.Message}");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Verbindung fehlgeschlagen: {ex.Message}");
            Console.Error.WriteLine("Laufen die API (dotnet run in backend/Pugling.Api) und Ollama (ollama serve)?");
            return 1;
        }
        catch (AgentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Abgebrochen.");
            return 130;
        }
    }

    /// <summary>
    /// Baut den Host. Die Argumente gehen bewusst <b>nicht</b> in die Konfiguration – der eigene Parser
    /// kennt Schalter ohne Wert (<c>--dry-run</c>), an denen der Kommandozeilen-Konfigurationsanbieter scheitert.
    /// </summary>
    private static IHost BuildHost()
    {
        // Die Konfiguration steht *vor* dem Builder: sonst ist das Logging schon verdrahtet, wenn der
        // Abschnitt "Logging" dazukommt – und die Konsole ertränkt die Ausgabe in HTTP-Protokoll.
        var configuration = new ConfigurationManager();
        configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Configuration = configuration });

        builder.Services.AddPuglingClient(builder.Configuration);

        builder.Services.AddOptions<AgentOptions>()
            .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
            .ValidateDataAnnotations();

        builder.Services.AddSingleton<IChatClient>(CreateChatClient);
        builder.Services.AddSingleton<BriefingBuilder>();
        builder.Services.AddSingleton<IExerciseStrategy, VocabularyStrategy>();
        builder.Services.AddSingleton<IExerciseStrategy, ClozeStrategy>();
        builder.Services.AddSingleton<IExerciseStrategy, TranslationStrategy>();
        builder.Services.AddSingleton<IExerciseStrategy, GrammarStrategy>();
        builder.Services.AddSingleton<CreatorPipeline>();
        builder.Services.AddSingleton<AgentCommands>();

        return builder.Build();
    }

    /// <summary>
    /// Das lokale Sprachmodell über Ollamas eigene API. Der HttpClient trägt das Zeitlimit – lokale
    /// Modelle rechnen auf CPU spürbar lange, und der Standard von 100 Sekunden reißt dabei zu früh.
    /// </summary>
    private static IChatClient CreateChatClient(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<AgentOptions>>().Value;
        var http = new HttpClient
        {
            BaseAddress = new Uri(options.Endpoint),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
        };

        return new ChatClientBuilder(new OllamaApiClient(http, options.Model))
            .UseLogging(services.GetRequiredService<ILoggerFactory>())
            .Build(services);
    }
}
