using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pugling.Client;

/// <summary>DI-Registrierung des Pugling-Clients.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert <see cref="CreatorApi"/>, <see cref="SupervisorApi"/> und <see cref="StudentApi"/> als
    /// typisierte HttpClients samt <see cref="AuthHandler"/>. Die Optionen werden aus dem angegebenen Konfigurationsabschnitt
    /// gebunden und beim ersten Zugriff validiert – eine fehlende PIN fällt so beim Start auf, nicht
    /// erst beim ersten API-Aufruf.
    /// </summary>
    public static IServiceCollection AddPuglingClient(this IServiceCollection services,
        IConfiguration configuration, string sectionName = PuglingClientOptions.SectionName)
    {
        services.AddOptions<PuglingClientOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddPuglingClientCore();
    }

    /// <summary>Variante für Aufrufer, die die Optionen im Code setzen (Tests, Konsolen-Flags).</summary>
    public static IServiceCollection AddPuglingClient(this IServiceCollection services,
        Action<PuglingClientOptions> configure)
    {
        services.AddOptions<PuglingClientOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddPuglingClientCore();
    }

    private static IServiceCollection AddPuglingClientCore(this IServiceCollection services)
    {
        // Das Token liegt im Singleton-Speicher (eine Anmeldung für alle Fassaden), der Handler selbst
        // wird je Client neu erzeugt: eine DelegatingHandler-Instanz darf nur in einer Kette hängen –
        // eine geteilte Instanz lehnt die HttpClientFactory beim zweiten Client ab.
        services.AddSingleton<PuglingTokenStore>();
        services.AddTransient<AuthHandler>();

        services.AddHttpClient<CreatorApi>(ConfigureClient).AddHttpMessageHandler<AuthHandler>();
        services.AddHttpClient<SupervisorApi>(ConfigureClient).AddHttpMessageHandler<AuthHandler>();
        services.AddHttpClient<StudentApi>(ConfigureClient).AddHttpMessageHandler<AuthHandler>();
        return services;
    }

    private static void ConfigureClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PuglingClientOptions>>().Value;
        // Abschließender Slash ist Pflicht: sonst schluckt Uri-Kombination das letzte Pfadsegment.
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    }
}
