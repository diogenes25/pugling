using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-429: the seeded family is the first thing a developer, tester or role walk sees, so it has to use the catalog it ships
/// with. Fresh database per class fixture - these are assertions about the seed, not about data other tests created.
/// </summary>
public class SeedFamilyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // AK 1 (book part). Red if the seeded book stays free text - the child hub then says "nicht katalogisiert" for a series
    // that lies in the same seed.
    [Fact]
    public async Task SeedBuch_HaengtAnReiheUndUnit()
    {
        var father = await TestApi.AdultAsync(factory);
        var books = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/1/textbooks");
        var book = books.EnumerateArray().Single(b => b.GetProperty("title").GetString() == "Green Line 1");

        Assert.Equal("Green Line 1", book.GetProperty("seriesName").GetString());
        Assert.Equal("Unit 1 – Greetings", book.GetProperty("currentUnitLabel").GetString());
    }

    // AK 2. Red if the starting balance is booked as a learning kind again ("Base" reads as "Wiederholung", "Achievement"
    // as "Auszeichnung") - it is a parent's gift, and the categories are what the history shows.
    [Fact]
    public async Task Startguthaben_IstEinePapaBuchung()
    {
        var father = await TestApi.AdultAsync(factory);
        var list = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/1/points?take=500");
        var entries = list.GetProperty("entries").EnumerateArray().ToList();

        Assert.Equal("Manual", entries.Single(e => e.GetProperty("reason").GetString() == "Startguthaben (Münzen)").GetProperty("kind").GetString());
        Assert.Equal("ManualGems", entries.Single(e => e.GetProperty("reason").GetString() == "Willkommens-Gems").GetProperty("kind").GetString());
    }

    private static Task SeedAsync(IServiceProvider sp, PuglingDbContext db) =>
        Seed.RunAsync(db,
            sp.GetRequiredService<ExerciseItemService>(),
            sp.GetRequiredService<AccountService>(),
            sp.GetRequiredService<InterestTagService>(),
            CancellationToken.None);

    private static Task<int> TeacherProfilesAsync(PuglingDbContext db) =>
        db.CreatorProfiles.AsNoTracking().CountAsync(p => p.Name == "Herr Schmidt – Englisch");

    /// <summary>
    /// B-445 AK 2 and 3: the subject-teacher profile is created once on a fresh database and never on one that already
    /// holds the demo unit, i.e. an existing demo database. (The family's real database is protected differently: it is
    /// not seeded at all - Production defaults to no seed and scripts/heimnetz.ps1 sets Seed__Enabled=false.) Red when the profile is created before the demo-unit
    /// lock in <c>SeedTeacherLibrary</c> or without its own existence check: then the second run adds one (AK 2) and the
    /// database whose profile was removed gets it back (AK 3).
    /// </summary>
    [Fact]
    public async Task FachlehrerProfil_Entsteht_Nur_Auf_Frischer_Datenbank()
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<PuglingDbContext>();

        // AK 2: the host seeded at startup; another run adds nothing.
        Assert.Equal(1, await TeacherProfilesAsync(db));
        await SeedAsync(sp, db);
        Assert.Equal(1, await TeacherProfilesAsync(db));

        // AK 3: the demo unit exists, the profile does not (an existing demo database) - the seed leaves it so.
        await db.CreatorProfiles.Where(p => p.Name == "Herr Schmidt – Englisch").ExecuteDeleteAsync();
        await SeedAsync(sp, db);
        Assert.Equal(0, await TeacherProfilesAsync(db));
    }
}
