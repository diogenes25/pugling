using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Tests;

/// <summary>
/// B-940: a tag linked to a planned class test is part of the test's material, and the preparation duty reads it live. The
/// child must not shrink it - neither by removing an exercise from it nor by deleting the whole tag (the link to the test
/// cascades). Adults stay free, and the lock ends with the test (written or cancelled). Every case uses a fresh child.
/// </summary>
public class VorbereitungsBuendelSperreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Fall(HttpClient Father, HttpClient Child, int TagId, int ExerciseId, int TestId);

    /// <summary>A fresh child, one tag with one exercise, linked to a class test planned in two weeks.</summary>
    private async Task<Fall> FallAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Sperre-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ($"lock-{Guid.NewGuid():N}"[..12], "Sperre"));
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId, name = TestApi.UniqueName("Klassenarbeit") }));
        (await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds = new[] { exerciseId } }))
            .EnsureSuccessStatusCode();
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId,
            title = "Vokabeltest Unit 2",
            scheduledDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14).ToString("yyyy-MM-dd"),
            tagIds = new[] { tagId },
        });
        res.EnsureSuccessStatusCode();
        var testId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
        return new Fall(father, await TestApi.ChildAsync(factory, childId, pin), tagId, exerciseId, testId);
    }

    private void Status(int testId, KlassenarbeitStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.Klassenarbeiten.Single(k => k.Id == testId).Status = status;
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private bool Verknuepft(Fall f) => Read(db => db.ExerciseTags.Any(x => x.TagId == f.TagId && x.ExerciseId == f.ExerciseId));
    private bool TagDa(Fall f) => Read(db => db.Tags.Any(t => t.Id == f.TagId));

    private static async Task GesperrtAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApiErrors.TagLockedByClassTest.Code, body.GetProperty("code").GetString());
        // The message names the test, so the child knows which one holds the tag.
        Assert.Contains("Vokabeltest Unit 2", body.GetProperty("detail").GetString());
    }

    // AK 1. Red when UntagExercise skips the lock.
    [Fact]
    public async Task Kind_Kann_Uebung_Nicht_Aus_Schlagwort_Einer_Geplanten_Arbeit_Loesen()
    {
        var f = await FallAsync();
        await GesperrtAsync(await f.Child.DeleteAsync($"/api/v1/creator/tags/{f.TagId}/exercises/{f.ExerciseId}"));
        Assert.True(Verknuepft(f));
    }

    // AK 2. Red when Delete skips the lock - the cascade would drop the tag from the test.
    [Fact]
    public async Task Kind_Kann_Schlagwort_Einer_Geplanten_Arbeit_Nicht_Loeschen()
    {
        var f = await FallAsync();
        await GesperrtAsync(await f.Child.DeleteAsync($"/api/v1/creator/tags/{f.TagId}"));
        Assert.True(TagDa(f));
        Assert.True(Read(db => db.KlassenarbeitTags.Any(kt => kt.TagId == f.TagId && kt.KlassenarbeitId == f.TestId)));
    }

    // AK 3, one row per status and per way. Red when the lock ignores the status (locks every linked test).
    [Theory]
    [InlineData(KlassenarbeitStatus.Written, false)]
    [InlineData(KlassenarbeitStatus.Cancelled, false)]
    [InlineData(KlassenarbeitStatus.Written, true)]
    [InlineData(KlassenarbeitStatus.Cancelled, true)]
    public async Task Nach_Der_Arbeit_Ist_Das_Schlagwort_Wieder_Frei(KlassenarbeitStatus status, bool ganzesSchlagwort)
    {
        var f = await FallAsync();
        Status(f.TestId, status);
        var res = ganzesSchlagwort
            ? await f.Child.DeleteAsync($"/api/v1/creator/tags/{f.TagId}")
            : await f.Child.DeleteAsync($"/api/v1/creator/tags/{f.TagId}/exercises/{f.ExerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.False(ganzesSchlagwort ? TagDa(f) : Verknuepft(f));
    }

    // AK 4, one row per way. Red when the lock also applies to adults.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Der_Vater_Bleibt_Frei(bool ganzesSchlagwort)
    {
        var f = await FallAsync();
        var res = ganzesSchlagwort
            ? await f.Father.DeleteAsync($"/api/v1/creator/tags/{f.TagId}")
            : await f.Father.DeleteAsync($"/api/v1/creator/tags/{f.TagId}/exercises/{f.ExerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.False(ganzesSchlagwort ? TagDa(f) : Verknuepft(f));
    }
}
