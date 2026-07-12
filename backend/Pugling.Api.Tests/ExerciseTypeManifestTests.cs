using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Das Übungstyp-Manifest (<c>GET api/v1/creator/exercise-types</c>): die Single Source of Truth für
/// Routing/Prüfmodus/Renderer je Typ. Sichert Vollständigkeit (kein Typ ohne Eintrag) und die
/// Invarianten je Prüfmodus ab – so fällt ein neuer, nicht eingetragener Übungstyp sofort auf.
/// </summary>
public class ExerciseTypeManifestTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public void JederUebungstyp_HatGenauEinManifest_MitStimmigenInvarianten()
    {
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        var manifests = registry.Manifests;

        // Eindeutige Keys: genau ein Manifest je registriertem Typ, und der Manifest-Key == Typ-Key.
        Assert.Equal(registry.All.Count, manifests.Count);
        Assert.Equal(manifests.Count, manifests.Select(m => m.Type).Distinct().Count());
        foreach (var t in registry.All)
            Assert.Equal(t.Key, t.Manifest.Type);

        foreach (var m in manifests)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Label));
            Assert.False(string.IsNullOrWhiteSpace(m.Renderer));
            Assert.False(string.IsNullOrWhiteSpace(m.AuthoringRoute));
            Assert.True(m.SchemaVersion >= 1);

            // Study-Plan-Test ⇔ PlayRoute und Method gesetzt; jeder andere Modus hat beides null.
            if (m.CheckMode == ExerciseCheckMode.StudyPlanTest)
            {
                Assert.False(string.IsNullOrWhiteSpace(m.PlayRoute));
                Assert.NotNull(m.Method);
            }
            else
            {
                Assert.Null(m.PlayRoute);
                Assert.Null(m.Method);
            }
        }
    }

    [Fact]
    public async Task Manifest_IstFuerBeideRollenLesbar_UndVollstaendig()
    {
        var father = await TestApi.FatherAsync(factory);
        var son = await TestApi.ChildAsync(factory);

        var res = await father.GetAsync("/api/v1/creator/exercise-types");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        Assert.Equal(registry.All.Count, arr.GetArrayLength());

        // Enums werden als Strings übertragen (globale Konvention, JsonStringEnumConverter).
        var cloze = arr.EnumerateArray().Single(e => e.GetProperty("type").GetString() == "Cloze");
        Assert.Equal("cloze", cloze.GetProperty("authoringRoute").GetString());
        Assert.Equal("StudyPlanTest", cloze.GetProperty("checkMode").GetString());
        Assert.Equal("tests", cloze.GetProperty("playRoute").GetString());
        Assert.Equal("Cloze", cloze.GetProperty("method").GetString());

        // Das kindneutrale Manifest darf auch der Sohn lesen.
        Assert.Equal(HttpStatusCode.OK, (await son.GetAsync("/api/v1/creator/exercise-types")).StatusCode);
    }

    [Fact]
    public async Task Einzelabruf_LiefertTyp_UndUnbekanntes404()
    {
        var father = await TestApi.FatherAsync(factory);

        var ok = await father.GetAsync("/api/v1/creator/exercise-types/Birkenbihl");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("birkenbihl", body.GetProperty("renderer").GetString());
        Assert.Equal("None", body.GetProperty("checkMode").GetString());

        // Unbekannter Typ-Schlüssel → der Controller-Guard liefert 404 (kein Enum-Model-Binding mehr).
        var invalid = await father.GetAsync("/api/v1/creator/exercise-types/999");
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
    }
}
