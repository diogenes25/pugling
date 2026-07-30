using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pugling.Client;

/// <summary>DI registration of the Pugling client.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CreatorApi"/>, <see cref="SupervisorApi"/>, and <see cref="StudentApi"/> as
    /// typed HttpClients including <see cref="AuthHandler"/>. The options are bound from the given configuration
    /// section and validated on first access – a missing PIN thus surfaces at startup, not
    /// only at the first API call.
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

    /// <summary>Variant for callers who set the options in code (tests, console flags).</summary>
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
