using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The exercise type manifest (<c>GET api/v1/creator/exercise-types</c>): the single source of truth for
/// routing/check mode/renderer per type. Ensures completeness (no type without an entry) and the
/// invariants per check mode - so a new exercise type without an entry stands out immediately.
/// </summary>
public class ExerciseTypeManifestTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public void JederUebungstyp_HatGenauEinManifest_MitStimmigenInvarianten()
    {
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        var manifests = registry.Manifests;

        // Unique keys: exactly one manifest per registered type, and the manifest key == the type key.
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

            // Study plan test ⇔ PlayRoute and Method set; every other mode has both null.
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

    /*
     * `StageOptions` is more than a picker for the preview: the same list is the permitted set the write path
     * validates a position's stage against (PlanPositionsController.StageProblem). A value the stage enum knows
     * but this list omits would therefore become unsettable - silently, and noticed only by whoever plays the
     * position. That was exactly TestStage.ShowBoth (B-79).
     *
     * The map is pinned on purpose: a new type *with* stages has to add a line here, and a type without a stage
     * selection (matching, essay, ...) is meant to keep its empty list - the check stays off for it.
     */
    [Fact]
    public void StageOptions_Enthalten_JedenWert_DesZugehoerigenStufenEnums()
    {
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        var stageEnums = new Dictionary<string, Type>
        {
            [ExerciseTypeKeys.Vocabulary] = typeof(TestStage),
            [ExerciseTypeKeys.Cloze] = typeof(ClozeStage),
        };

        foreach (var (key, stageEnum) in stageEnums)
        {
            var type = registry.ByKey(key);
            Assert.NotNull(type);
            Assert.Equal(
                Enum.GetValues(stageEnum).Cast<int>().Order(),
                type.StageOptions.Select(o => o.Value).Order());
            Assert.All(type.StageOptions, o => Assert.False(string.IsNullOrWhiteSpace(o.Label)));
        }

        foreach (var type in registry.All.Where(t => !stageEnums.ContainsKey(t.Key)))
            Assert.Empty(type.StageOptions);
    }

    [Fact]
    public async Task Manifest_IstFuerBeideRollenLesbar_UndVollstaendig()
    {
        var father = await TestApi.AdultAsync(factory);
        var son = await TestApi.ChildAsync(factory);

        var res = await father.GetAsync("/api/v1/creator/exercise-types");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();
        Assert.Equal(registry.All.Count, arr.GetArrayLength());

        // Enums are transferred as strings (a global convention, JsonStringEnumConverter).
        var cloze = arr.EnumerateArray().Single(e => e.GetProperty("type").GetString() == "Cloze");
        Assert.Equal("cloze", cloze.GetProperty("authoringRoute").GetString());
        Assert.Equal("StudyPlanTest", cloze.GetProperty("checkMode").GetString());
        Assert.Equal("tests", cloze.GetProperty("playRoute").GetString());
        Assert.Equal("Cloze", cloze.GetProperty("method").GetString());

        // The child-neutral manifest may be read by the child too.
        Assert.Equal(HttpStatusCode.OK, (await son.GetAsync("/api/v1/creator/exercise-types")).StatusCode);
    }

    /// <summary>
    /// B-163: the two axes of the exercise search must not share vocabulary. The <b>type</b> names the method
    /// or form, the <b>category</b> ("Art") names the subject matter - so a type named after subject matter
    /// necessarily collides, and two of them did ("Vokabeln", "Grammatik").
    /// <para>
    /// This holds <b>identity</b> under <see cref="InterestSlug.From"/> (the taxonomy's own folding: case,
    /// ß, diacritics, separators), and that is deliberately where it stops: a stem or prefix rule would first
    /// flag our own chosen name "Vokabelkarten" against the category "Vokabeln". Merely similar names
    /// therefore stay a human judgement - promising a gate that cannot be drawn sharply would be worse than
    /// naming the limit. Reusing the slug rather than folding case here keeps one normalisation, not two.
    /// </para>
    /// The seeded category names are read from the <b>running instance</b>, not from a copy of the seed file:
    /// a second list would be the thing that drifts.
    /// </summary>
    [Fact]
    public async Task KeinTypLabel_HeisstWieEineGeseedeteArt()
    {
        var father = await TestApi.AdultAsync(factory);
        var registry = factory.Services.GetRequiredService<ExerciseTypeRegistry>();

        var subjects = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/subjects");
        var artNamen = new List<string>();
        foreach (var subject in subjects.EnumerateArray())
        {
            var id = subject.GetProperty("id").GetInt32();
            var arten = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/subjects/{id}/categories");
            artNamen.AddRange(arten.EnumerateArray().Select(a => a.GetProperty("name").GetString()!));
        }

        // Name the two categories this check is actually about, not just "some category exists". The input
        // arrives over two HTTP hops; should either stop yielding the seeded categories (a paging default on
        // the subject list, an ownership filter on the category read), a mere NotEmpty would still be green
        // with unrelated names in hand and would silently stop holding the rule it was written for.
        Assert.Contains("Vokabeln", artNamen);
        Assert.Contains("Grammatik", artNamen);

        var normalisierteArten = artNamen.Select(InterestSlug.From).ToHashSet();
        var kollisionen = registry.All
            .Select(t => t.Manifest.Label)
            .Where(label => normalisierteArten.Contains(InterestSlug.From(label)))
            .ToList();

        Assert.True(kollisionen.Count == 0,
            $"These type labels are named like a seeded category: {string.Join(", ", kollisionen)}. "
            + "The type axis names the method or form, the category axis names the subject matter (B-163).");
    }

    [Fact]
    public async Task Einzelabruf_LiefertTyp_UndUnbekanntes404()
    {
        var father = await TestApi.AdultAsync(factory);

        var ok = await father.GetAsync("/api/v1/creator/exercise-types/Birkenbihl");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("birkenbihl", body.GetProperty("renderer").GetString());
        Assert.Equal("None", body.GetProperty("checkMode").GetString());

        // An unknown type key → the controller guard returns 404 (no enum model binding any more).
        var invalid = await father.GetAsync("/api/v1/creator/exercise-types/999");
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
    }
}
