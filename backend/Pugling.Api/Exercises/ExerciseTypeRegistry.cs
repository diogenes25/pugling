namespace Pugling.Api.Exercises;

/// <summary>
/// The one resolution point for exercise types: maps the registered <see cref="IExerciseType"/>s onto their
/// <see cref="IExerciseType.Key"/>, replacing the former <c>ExerciseType</c> enum + <c>switch</c>.
/// Also carries the manifest list (formerly hardcoded) and derived views for DB filters (capabilities
/// cannot be checked directly in SQL – hence the key sets).
/// </summary>
public sealed class ExerciseTypeRegistry
{
    private readonly IReadOnlyDictionary<string, IExerciseType> _byKey;

    /// <summary>Builds the registry from all <see cref="IExerciseType"/> singletons registered via DI, keyed by <see cref="IExerciseType.Key"/>.</summary>
    public ExerciseTypeRegistry(IEnumerable<IExerciseType> types) =>
        _byKey = types.ToDictionary(t => t.Key, StringComparer.Ordinal);

    /// <summary>All registered types.</summary>
    public IReadOnlyCollection<IExerciseType> All => (IReadOnlyCollection<IExerciseType>)_byKey.Values;

    /// <summary>Type for the key, or <c>null</c> if unknown.</summary>
    public IExerciseType? ByKey(string key) => _byKey.GetValueOrDefault(key);

    /// <summary>Type for the key; throws if unknown (internal consistency break, not a user error).</summary>
    public IExerciseType Require(string key) =>
        _byKey.GetValueOrDefault(key) ?? throw new InvalidOperationException($"Unknown exercise type '{key}'.");

    /// <summary>Manifests of all types – the source of truth for the <c>exercise-types</c> endpoint.</summary>
    public IReadOnlyList<ExerciseTypeManifest> Manifests => [.. _byKey.Values.Select(t => t.Manifest)];

    /// <summary>Keys of the types with cross-plan item learning progress – for in-DB filters (capability can't go into SQL).</summary>
    public IReadOnlyList<string> KeysSupportingItemProgress =>
        [.. _byKey.Values.Where(t => t.SupportsItemProgress).Select(t => t.Key)];
}

/// <summary>DI registration of the built-in exercise types + the registry.</summary>
public static class ExerciseTypeServiceCollectionExtensions
{
    /// <summary>
    /// Registers every built-in exercise type as an <see cref="IExerciseType"/> (stateless singletons) and the
    /// <see cref="ExerciseTypeRegistry"/>. A new type = one line here + one class (no enum/switch edit).
    /// (Assembly scanning/external plugins are the later stage-2 step.)
    /// </summary>
    public static IServiceCollection AddExerciseTypes(this IServiceCollection services)
    {
        services.AddSingleton<IExerciseType, VocabularyExerciseType>();
        services.AddSingleton<IExerciseType, ReadingExerciseType>();
        services.AddSingleton<IExerciseType, ClozeExerciseType>();
        services.AddSingleton<IExerciseType, EssayExerciseType>();
        services.AddSingleton<IExerciseType, ListeningExerciseType>();
        services.AddSingleton<IExerciseType, GrammarExerciseType>();
        services.AddSingleton<IExerciseType, MatchingExerciseType>();
        services.AddSingleton<IExerciseType, TranslationExerciseType>();
        services.AddSingleton<IExerciseType, ArithmeticExerciseType>();
        services.AddSingleton<IExerciseType, ArithmeticDrillExerciseType>();
        services.AddSingleton<IExerciseType, ListExerciseType>();
        services.AddSingleton<IExerciseType, BirkenbihlExerciseType>();
        services.AddSingleton<ExerciseTypeRegistry>();
        return services;
    }
}
