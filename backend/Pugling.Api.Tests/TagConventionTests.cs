using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Tests;

/// <summary>
/// Konventions-Guard für die Swagger-Gruppierung: In den Tier-Ordnern
/// (Controllers/{Creator|Supervisor|Student}) muss jedes <c>[Tags(...)]</c> – ob auf Controller- oder
/// Action-Ebene – mit dem passenden Ebenen-Präfix beginnen. Verhindert genau die Drift, die die
/// Ebenen-Umstellung behoben hat (ein Controller im Supervisor-Ordner versehentlich als
/// „Learn – …"/„Admin – …" getaggt), ohne dass es sonst etwas bemerkt. Rein reflexiv, kein Host nötig.
/// </summary>
public class TagConventionTests
{
    private static readonly (string Namespace, string Prefix)[] Tiers =
    [
        ("Pugling.Api.Controllers.Creator", "Creator – "),
        ("Pugling.Api.Controllers.Supervisor", "Supervisor – "),
        ("Pugling.Api.Controllers.Student", "Student – "),
    ];

    [Fact]
    public void TierController_Tags_TragenDasEbenenPraefix()
    {
        var offenders = new List<string>();
        var checkedTags = 0;

        foreach (var type in typeof(Program).Assembly.GetTypes()
                     .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var tier = Tiers.FirstOrDefault(x => type.Namespace == x.Namespace);
            if (tier.Namespace is null) continue; // z. B. AuthController (Tag „Auth") – kein Tier-Ordner

            foreach (var (member, tag) in TagsOf(type))
            {
                checkedTags++;
                if (!tag.StartsWith(tier.Prefix, StringComparison.Ordinal))
                    offenders.Add($"{type.Name}.{member}: '{tag}' erwartet Praefix '{tier.Prefix}'");
            }
        }

        // Selbstschutz gegen falsch-grün: fänden wir die [Tags] gar nicht (falscher Attribut-Typ),
        // wäre die Liste leer und der Test bestünde inhaltsleer. Es gibt real ~40 Tag-Strings.
        Assert.True(checkedTags >= 25, $"Zu wenige Tags gefunden ({checkedTags}) – Reflexion greift nicht.");
        Assert.True(offenders.Count == 0,
            "Tag-Praefix passt nicht zum Tier-Ordner:\n" + string.Join("\n", offenders));
    }

    /// <summary>Alle Tag-Strings eines Controllers – Controller-Ebene und jede Action-Ebene.</summary>
    private static IEnumerable<(string Member, string Tag)> TagsOf(Type type)
    {
        foreach (var a in type.GetCustomAttributes<TagsAttribute>(inherit: true))
            foreach (var tag in a.Tags)
                yield return ("(class)", tag);

        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            foreach (var a in m.GetCustomAttributes<TagsAttribute>(inherit: false))
                foreach (var tag in a.Tags)
                    yield return (m.Name, tag);
    }
}
