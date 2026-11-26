using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-424: the catalog row tells a supervisor how big an exercise is (<c>itemCount</c>) and the manifest names the unit
/// (<c>itemNoun</c>/<c>itemNounOne</c>), so "12 Karten" is decided by the server, not by a frontend table. A type without
/// countable atoms (essay, generated drill) says <c>null</c> on both sides - "0" would read as an empty, unassignable exercise.
/// </summary>
public class ExerciseItemCountTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(HttpClient father, string basePath, int seriesUnitId)> UnitAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Umfang") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return (father, $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}", seriesUnitId);
    }

    private static async Task<JsonElement> RowAsync(HttpClient father, int seriesUnitId, int exerciseId)
    {
        var rows = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises?seriesUnitId={seriesUnitId}");
        return rows.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == exerciseId);
    }

    // AK 1, vocabulary: the items live in their own table, not in the config - red if the count reads the config
    // (always empty for this type) instead of the item rows, or counts another exercise's rows.
    [Fact]
    public async Task Vokabel_ZaehltIhreItems()
    {
        var (father, basePath, unitId) = await UnitAsync();
        var three = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basePath}/vocabulary", new
        {
            title = "Drei Wörter",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "fr", targetLang = "de", items = new[] { new { front = "la ville", back = "die Stadt" }, new { front = "la rue", back = "die Straße" }, new { front = "le pont", back = "die Brücke" } } },
        }));
        var one = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basePath}/vocabulary", new
        {
            title = "Ein Wort",
            orderIndex = 2,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "fr", targetLang = "de", items = new[] { new { front = "la mer", back = "das Meer" } } },
        }));

        Assert.Equal(3, (await RowAsync(father, unitId, three)).GetProperty("itemCount").GetInt32());
        Assert.Equal(1, (await RowAsync(father, unitId, one)).GetProperty("itemCount").GetInt32());
    }

    // AK 1, inline types: the atoms come from the config. Red if inline types fall back to the item table (0 rows) or
    // to null.
    [Fact]
    public async Task InlineTypen_ZaehlenIhreAtomeAusDerConfig()
    {
        var (father, basePath, unitId) = await UnitAsync();
        var list = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basePath}/list", new
        {
            title = "Bundesländer",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { instruction = "Nenne vier.", ordered = false, items = new[] { new { value = "Bayern" }, new { value = "Hessen" }, new { value = "Berlin" }, new { value = "Bremen" } } },
        }));
        var arithmetic = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basePath}/arithmetic", new
        {
            title = "1×1",
            orderIndex = 2,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "7 × 6", answer = 42, tolerance = 0 }, new { prompt = "3 × 3", answer = 9, tolerance = 0 } } },
        }));

        Assert.Equal(4, (await RowAsync(father, unitId, list)).GetProperty("itemCount").GetInt32());
        Assert.Equal(2, (await RowAsync(father, unitId, arithmetic)).GetProperty("itemCount").GetInt32());
    }

    // AK 1, no countable atoms: an essay is one open task, not "0 items" - red if the count ignores the noun-less type and
    // reports 0, which the row would render as "leer" (not assignable) for a perfectly assignable essay.
    [Fact]
    public async Task Aufsatz_HatKeinenUmfang()
    {
        var (father, basePath, unitId) = await UnitAsync();
        var essay = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basePath}/essays", new
        {
            title = "Brief",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { prompt = "Schreibe einen Brief.", minWords = 80 },
        }));

        Assert.Equal(JsonValueKind.Null, (await RowAsync(father, unitId, essay)).GetProperty("itemCount").ValueKind);
    }

    // Review finding: a config that parses but cannot be projected (a null element, as legacy rows or direct DB writes
    // can hold) costs that row its count - it must not turn the whole search into a 500. Red if the guard in
    // ExerciseCatalogController.ItemCountOf narrows back to JsonException (the null element throws a NullReferenceException)
    // or goes (B-448 moved it there from ExerciseTypeBase.ItemCount, to log).
    [Theory]
    [InlineData("{\"text\":\"T\",\"questions\":[null]}")]
    [InlineData("{")]
    public async Task KaputteConfig_KostetNurDenUmfangDieserZeile(string brokenConfig)
    {
        var (father, _, unitId) = await UnitAsync();
        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var exercise = new Exercise { SeriesUnitId = unitId, Type = "Reading", Title = "Kaputt", OrderIndex = 1, RewardPoints = 1, ConfigJson = brokenConfig };
            db.Exercises.Add(exercise);
            db.SaveChanges();
            id = exercise.Id;
        }

        var row = await RowAsync(father, unitId, id);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("itemCount").ValueKind);
    }

    // B-448: "empty" is a size, not "no size" - a vocabulary exercise without item rows and an inline exercise without
    // atoms say 0. Red if either count turns an empty exercise into null ("is 0 ? null"), which the UI shows as an essay.
    [Theory]
    [InlineData("Vocabulary", "{\"direction\":\"front-to-back\",\"sourceLang\":\"fr\",\"targetLang\":\"de\"}")]
    [InlineData("Reading", "{\"text\":\"T\",\"questions\":[]}")]
    public async Task LeereUebung_ZaehltNull(string type, string config)
    {
        var (father, _, unitId) = await UnitAsync();
        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var exercise = new Exercise { SeriesUnitId = unitId, Type = type, Title = "Leer", OrderIndex = 1, RewardPoints = 1, ConfigJson = config };
            db.Exercises.Add(exercise);
            db.SaveChanges();
            id = exercise.Id;
        }

        Assert.Equal(0, (await RowAsync(father, unitId, id)).GetProperty("itemCount").GetInt32());
    }

    // AK 2: the noun per type, pinned like the stage options - a new type has to decide its noun here, and a type
    // without countable atoms keeps null. Red if a noun goes missing or a noun-less type suddenly gets one.
    [Fact]
    public async Task Manifest_NenntDieEinheitJeTyp()
    {
        var expected = new Dictionary<string, (string? many, string? one)>
        {
            ["Vocabulary"] = ("Karten", "Karte"),
            ["Reading"] = ("Fragen", "Frage"),
            ["Listening"] = ("Fragen", "Frage"),
            ["Essay"] = (null, null),
            ["Grammar"] = ("Aufgaben", "Aufgabe"),
            ["Translation"] = ("Sätze", "Satz"),
            ["Birkenbihl"] = ("Sätze", "Satz"),
            ["Cloze"] = ("Lücken", "Lücke"),
            ["Matching"] = ("Paare", "Paar"),
            ["Arithmetic"] = ("Aufgaben", "Aufgabe"),
            ["ArithmeticDrill"] = (null, null),
            ["List"] = ("Einträge", "Eintrag"),
            ["QuadraticEquation"] = ("Gleichungen", "Gleichung"),
        };
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        Assert.Equal(expected.Keys.OrderBy(k => k), registry.All.Select(t => t.Key).OrderBy(k => k));

        var father = await TestApi.AdultAsync(factory);
        var manifests = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/exercise-types");
        Assert.Equal(expected.Count, manifests.GetArrayLength());
        foreach (var m in manifests.EnumerateArray())
        {
            var (many, one) = expected[m.GetProperty("type").GetString()!];
            Assert.Equal(many, m.GetProperty("itemNoun").GetString());
            Assert.Equal(one, m.GetProperty("itemNounOne").GetString());
        }
    }
}
