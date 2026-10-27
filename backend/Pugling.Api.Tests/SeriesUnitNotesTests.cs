using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-22, AC 6: a unit's grammar note is free text like its vocabulary note. The round trip alone would stay green
/// on SQLite (it enforces no length), so the test also pins the model's length - that is what a provider change
/// would turn into a truncation or a rejection.
/// </summary>
public class SeriesUnitNotesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Grammatik_Der_Unit_Ist_Freitext_Mit_2000_Zeichen()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var model = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().GetService<IDesignTimeModel>().Model;
            var unit = model.FindEntityType(typeof(SeriesUnit))!;
            // Grammar carries the free-text length; the label keeps the default - the two differ on purpose.
            Assert.Equal(2000, unit.FindProperty(nameof(SeriesUnit.Grammar))!.GetMaxLength());
            Assert.Equal(200, unit.FindProperty(nameof(SeriesUnit.Label))!.GetMaxLength());
        }

        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Notizen-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var grammar = string.Join(" ", Enumerable.Repeat("Present perfect vs. simple past, Signalwörter, Verneinung;", 12));
        Assert.True(grammar.Length > 200 && grammar.Length <= 2000, $"length {grammar.Length}");

        var patched = await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}", new { grammar });
        patched.EnsureSuccessStatusCode();
        var read = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}");
        Assert.Equal(grammar, read.GetProperty("grammar").GetString());
    }
}
