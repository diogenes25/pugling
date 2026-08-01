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
        // The token lives in the singleton store (one login for all facades), the handler itself is
        // created per client: a DelegatingHandler instance may only sit in one chain - the
        // HttpClientFactory rejects a shared instance on the second client.
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
        // The trailing slash is mandatory: without it Uri combination swallows the last path segment.
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    }
}
