using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Die eine Auflösungsstelle für Übungstypen: bildet die registrierten <see cref="IExerciseType"/> auf ihren
/// <see cref="IExerciseType.Key"/> ab und ersetzt damit das frühere <c>ExerciseType</c>-Enum + <c>switch</c>.
/// Trägt zugleich die Manifest-Liste (früher hartkodiert) und abgeleitete Sichten für DB-Filter (Capabilities
/// lassen sich nicht direkt in SQL prüfen – daher die Key-Mengen).
/// </summary>
public sealed class ExerciseTypeRegistry
{
    private readonly IReadOnlyDictionary<string, IExerciseType> _byKey;

    public ExerciseTypeRegistry(IEnumerable<IExerciseType> types) =>
        _byKey = types.ToDictionary(t => t.Key, StringComparer.Ordinal);

    /// <summary>Alle registrierten Typen.</summary>
    public IReadOnlyCollection<IExerciseType> All => (IReadOnlyCollection<IExerciseType>)_byKey.Values;

    /// <summary>Typ zum Schlüssel oder <c>null</c>, wenn unbekannt.</summary>
    public IExerciseType? ByKey(string key) => _byKey.GetValueOrDefault(key);

    /// <summary>Typ zum Schlüssel; wirft, wenn unbekannt (interner Konsistenzbruch, kein Nutzerfehler).</summary>
    public IExerciseType Require(string key) =>
        _byKey.GetValueOrDefault(key) ?? throw new InvalidOperationException($"Unknown exercise type '{key}'.");

    /// <summary>Manifeste aller Typen – die Wahrheit für den <c>exercise-types</c>-Endpunkt.</summary>
    public IReadOnlyList<ExerciseTypeManifest> Manifests => [.. _byKey.Values.Select(t => t.Manifest)];

    /// <summary>Schlüssel der Typen mit plan-übergreifendem Item-Lernstand – für In-DB-Filter (Capability geht nicht in SQL).</summary>
    public IReadOnlyList<string> KeysSupportingItemProgress =>
        [.. _byKey.Values.Where(t => t.SupportsItemProgress).Select(t => t.Key)];
}

/// <summary>DI-Registrierung der eingebauten Übungstypen + der Registry.</summary>
public static class ExerciseTypeServiceCollectionExtensions
{
    /// <summary>
    /// Registriert jeden eingebauten Übungstyp als <see cref="IExerciseType"/> (zustandslose Singletons) und die
    /// <see cref="ExerciseTypeRegistry"/>. Ein neuer Typ = eine Zeile hier + eine Klasse (kein Enum-/Switch-Edit).
    /// (Assembly-Scan/externe Plugins sind der spätere Stufe-2-Schritt.)
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
