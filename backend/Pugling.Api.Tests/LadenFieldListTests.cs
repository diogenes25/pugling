using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Controllers.Creator;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-475: the book loader of the skill <c>lehrwerk-aufbereiten</c> checks the config fields of each exercise offline,
/// because the server rejects an unknown one with <c>400 unknown_field</c> and a run would stop halfway. Its field list
/// is a copy of the contract; this test holds the copy to the config types so it cannot drift.
/// </summary>
public partial class LadenFieldListTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [GeneratedRegex(@"# FIELDS-BEGIN\s*FIELDS\s*=\s*(\{.*?\})\s*# FIELDS-END", RegexOptions.Singleline)]
    private static partial Regex FieldsBlock();

    [GeneratedRegex(@"^ROUTES\s*=\s*\{([^}]*)\}", RegexOptions.Multiline)]
    private static partial Regex RoutesLine();

    private static string LoaderSource() =>
        File.ReadAllText(Path.Combine(ApiSurface.RepoRoot(), ".claude", "skills", "lehrwerk-aufbereiten", "laden.py"));

    private static Dictionary<string, Dictionary<string, string[]>> LoaderFields()
    {
        var match = FieldsBlock().Match(LoaderSource());
        Assert.True(match.Success, "laden.py lost its FIELDS-BEGIN/FIELDS-END block.");
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(match.Groups[1].Value)!;
    }

    private static string[] LoaderRoutes()
    {
        var match = RoutesLine().Match(LoaderSource());
        Assert.True(match.Success, "laden.py lost its ROUTES line.");
        return [.. Regex.Matches(match.Groups[1].Value, "\"([a-z]+)\"").Select(m => m.Groups[1].Value).Order()];
    }

    // Route segment -> config type, read from the controllers themselves: a controller that binds another type, or a
    // new typed route, changes this table without anyone editing the test.
    private static Dictionary<string, Type> ControllerRoutes() =>
        typeof(ExerciseControllerBase<>).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b
                && b.GetGenericTypeDefinition() == typeof(ExerciseControllerBase<>))
            .ToDictionary(
                t => t.GetCustomAttribute<RouteAttribute>()!.Template!.Split('/')[^1],
                t => t.BaseType!.GetGenericArguments()[0]);

    // Input fields of a contract type as they appear in JSON. Self is a response-only link (named "_self" by an
    // attribute or, for VocabRef, by its converter); the server would accept it, the loader rejects it, since an
    // author never writes it.
    private static Dictionary<string, PropertyInfo> JsonFields(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != "Self")
            .Select(p => (Name: p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name), Prop: p))
            .ToDictionary(x => x.Name, x => x.Prop);

    // Entry type of a list of objects; null for anything else (strings and enums carry no fields to check).
    private static Type? ObjectListEntry(PropertyInfo p)
    {
        if (!p.PropertyType.IsGenericType || p.PropertyType.GetGenericTypeDefinition() != typeof(List<>))
            return null;
        var entry = p.PropertyType.GetGenericArguments()[0];
        return entry == typeof(string) || entry.IsEnum || entry.IsPrimitive ? null : entry;
    }

    // Contract shape in the loader's notation: "" for the top level, dotted paths for lists of objects at any depth.
    private static void Shape(Type type, string key, Dictionary<string, string[]> into)
    {
        var fields = JsonFields(type);
        into[key] = [.. fields.Keys.Order()];
        foreach (var (name, prop) in fields)
            if (ObjectListEntry(prop) is { } entry)
                Shape(entry, key == "" ? name : $"{key}.{name}", into);
    }

    [Fact]
    public void Feldliste_Des_Laders_Deckt_Sich_Mit_Dem_Vertrag()
    {
        var loader = LoaderFields();
        var controllers = ControllerRoutes();
        Assert.Equal(LoaderRoutes(), loader.Keys.Order());

        foreach (var (route, fields) in loader)
        {
            Assert.True(controllers.TryGetValue(route, out var configType),
                $"laden.py names route '{route}', but no exercise controller serves it.");
            var contract = new Dictionary<string, string[]>();
            Shape(configType!, "", contract);
            var mirror = fields.ToDictionary(f => f.Key, f => f.Value.Order().ToArray());
            Assert.True(contract.Keys.Order().SequenceEqual(mirror.Keys.Order()),
                $"laden.py FIELDS[\"{route}\"] checks [{string.Join(", ", mirror.Keys)}], contract " +
                $"{configType!.Name} has the levels [{string.Join(", ", contract.Keys)}].");
            foreach (var (key, expected) in contract)
                Assert.True(expected.SequenceEqual(mirror[key]),
                    $"laden.py FIELDS[\"{route}\"][\"{key}\"] = [{string.Join(", ", mirror[key])}], contract " +
                    $"{configType.Name} has [{string.Join(", ", expected)}].");
        }
    }

    // The question the story left open: a rule task compares the typographic apostrophe like the straight one, so
    // "qu’il" needs no alternatives (which GrammarTask does not have).
    [Theory]
    [InlineData("qu’il")]
    [InlineData("qu'il")]
    public async Task Regelaufgabe_Wertet_Typografischen_Apostroph_Wie_Den_Geraden(string given)
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Französisch-Apostroph") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/grammar", new
            {
                title = TestApi.UniqueName("Elision"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new { instruction = "Verbinde.", tasks = new[] { new { prompt = "que + il", answer = "qu'il" } } },
            }));

        var res = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } }, stage = (int)TestStage.FreeText }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correct").GetInt32());
    }
}
