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
/// Entry point of the AI creator. The app drives the Pugling API from the outside - it is a
/// <b>consumer</b> of the REST surface, not part of the server. Everything runs locally: the API on
/// <c>localhost:5200</c>, the language model in Ollama.
/// </summary>
public static class Program
{
    /// <summary>Exit codes: 0 = done, 1 = failed on domain grounds, 2 = invoked incorrectly, 130 = cancelled.</summary>
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
    /// Builds the host. The arguments deliberately do <b>not</b> go into the configuration - the custom
    /// parser understands switches without a value (<c>--dry-run</c>), which the command-line configuration
    /// provider fails on.
    /// </summary>
    private static IHost BuildHost()
    {
        // The configuration comes *before* the builder: otherwise logging is already wired up when the
        // "Logging" section arrives - and the console drowns the output in HTTP logs.
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
        builder.Services.AddSingleton<ExamPlanner>();
        builder.Services.AddSingleton<AgentCommands>();

        return builder.Build();
    }

    /// <summary>
    /// The local language model via Ollama's own API. The HttpClient carries the timeout - local models
    /// take noticeably long computing on CPU, and the default of 100 seconds cuts them off too early.
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
