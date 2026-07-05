using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Pugling.Api.Data;

/// <summary>
/// Wert-Vergleicher für JSON-Spalten: vergleicht, hasht und kopiert über die JSON-Serialisierung. Dadurch
/// erkennt EF Änderungen an konvertierten Listen/Objekten AUCH bei In-Place-Mutation korrekt (nicht nur bei
/// Neuzuweisung) und legt beim Snapshot eine tiefe Kopie an. Schließt den bekannten Fallstrick der fehlenden
/// ValueComparer für die JSON-Spalten (Gaps/WordBank/StageSchedule/Noun/Verb …). Rein Modell-Metadaten –
/// keine Schemaänderung, keine Migration.
/// </summary>
public static class JsonValueComparer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Ein tiefer, serialisierungsbasierter Vergleicher für den JSON-Spaltentyp <typeparamref name="T"/>.</summary>
    public static ValueComparer<T> For<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
        v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);
}
