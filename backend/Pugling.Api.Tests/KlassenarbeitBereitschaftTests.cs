using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-751: the child's readiness for a class test - the share of the bundle's vocabulary items that are not weak, over
/// one shared bundle rule (direct and tagged exercises, no duplicates) that the supervisor's practice view reads too.
/// </summary>
public class KlassenarbeitBereitschaftTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Readiness(int childId, int classTestId) =>
        $"/api/v1/student/children/{childId}/class-tests/{classTestId}/readiness";

    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int ClassTestId);

    /// <summary>A fresh child with one planned class test (no bundle yet).</summary>
    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Bereit-Kind"), pin }));
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/class-tests",
            new { childId, title = "Vokabeltest", scheduledDate = "2099-05-01" });
        res.EnsureSuccessStatusCode();
        var classTestId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
        return new Setup(father, await TestApi.ChildAsync(factory, childId, pin), childId, classTestId);
    }

    /// <summary>A vocabulary exercise with two words of its own, so two of them never share a store word (B-945).</summary>
    private static Task<int> OwnWordsAsync(HttpClient father) => TestApi.CreateVocabExerciseAsync(father,
        ($"w1-{Guid.NewGuid():N}"[..14], "Wort1"), ($"w2-{Guid.NewGuid():N}"[..14], "Wort2"));

    private static async Task AssignAsync(Setup s, params int[] exerciseIds) =>
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{s.ClassTestId}/exercises",
            new { exerciseIds })).EnsureSuccessStatusCode();

    /// <summary>Tags the exercises with a fresh tag of the child and links that tag to the class test.</summary>
    private static async Task TagAsync(Setup s, params int[] exerciseIds)
    {
        var tagId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = s.ChildId, name = TestApi.UniqueName("Unit") }));
        (await s.Father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds })).EnsureSuccessStatusCode();
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{s.ClassTestId}/tags/{tagId}", new { })).EnsureSuccessStatusCode();
    }

    private List<int> ItemIdsOf(int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .Select(i => i.Id).ToList();
    }

    /// <summary>Writes the child's learning state of an item straight into the database (the API only grows it by answering).</summary>
    private void SeedMastery(int childId, int itemId, int masteryPercent)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var item = db.ExerciseItems.Single(i => i.Id == itemId);
        db.ItemProgress.Add(new ItemProgress
        {
            ChildId = childId,
            ItemId = itemId,
            ExerciseId = item.ExerciseId,
            VocabularyId = item.VocabularyId,
            MasteryPercent = masteryPercent,
            SeenCount = 1,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Buendel_Aus_Direkt_Und_Tag_Ohne_Dubletten_Wie_Im_Vater_Pfad()
    {
        var s = await SetupAsync();
        var direct = await OwnWordsAsync(s.Father);
        var tagged = await OwnWordsAsync(s.Father);
        await AssignAsync(s, direct);
        await TagAsync(s, tagged, direct); // the direct one is tagged as well: it must count once

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));
        Assert.Equal(2, readiness.GetProperty("exerciseCount").GetInt32());
        Assert.Equal(4, readiness.GetProperty("totalItems").GetInt32()); // two words each

        // The supervisor's practice view reads the same bundle.
        var practice = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests/{s.ClassTestId}/practice");
        Assert.Equal(new[] { direct, tagged }.Order(),
            practice.GetProperty("exercises").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).Order());
    }

    [Fact]
    public async Task Geteilte_Buendel_Funktion_Liefert_Jede_Uebung_Einmal()
    {
        // The rule itself, not one of its readers: the readers so far filter with Contains and would hide a duplicate.
        var s = await SetupAsync();
        var direct = await TestApi.CreateVocabExerciseAsync(s.Father);
        var tagged = await TestApi.CreateVocabExerciseAsync(s.Father);
        await AssignAsync(s, direct);
        await TagAsync(s, tagged, direct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(new[] { direct, tagged }.Order(), await ClassTestBundle.ExerciseIdsAsync(db, [s.ClassTestId], CancellationToken.None));
    }

    [Fact]
    public async Task Anteil_Zaehlt_Nicht_Schwache_Ueber_Alle_Items_Auch_Ungeuebte()
    {
        var s = await SetupAsync();
        var a = await OwnWordsAsync(s.Father);
        var b = await OwnWordsAsync(s.Father);
        await AssignAsync(s, a);
        await TagAsync(s, b);
        var itemsA = ItemIdsOf(a);
        SeedMastery(s.ChildId, itemsA[0], ItemProgress.WeakBelowPercent);     // exactly at the threshold: secure
        SeedMastery(s.ChildId, itemsA[1], ItemProgress.WeakBelowPercent - 1); // just below: weak
        // b's two items were never answered: no progress row, not secure, but in the denominator.

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));

        Assert.Equal(4, readiness.GetProperty("totalItems").GetInt32());
        Assert.Equal(1, readiness.GetProperty("secureItems").GetInt32());
        Assert.Equal(2, readiness.GetProperty("vocabularyExerciseCount").GetInt32());
        Assert.Equal(50, readiness.GetProperty("weakBelowPercent").GetInt32());
    }

    [Fact]
    public async Task Fremder_Lernstand_Zaehlt_Nicht()
    {
        var s = await SetupAsync();
        var a = await TestApi.CreateVocabExerciseAsync(s.Father);
        await AssignAsync(s, a);
        // Child 1 (the seeded child) knows the word perfectly - that says nothing about this child.
        SeedMastery(1, ItemIdsOf(a)[0], 100);

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));
        Assert.Equal(0, readiness.GetProperty("secureItems").GetInt32());
    }

    [Fact]
    public async Task Buendel_Ohne_Vokabeln_Hat_Kein_Mass()
    {
        var s = await SetupAsync();
        var (_, _, arithmetic) = await TestApi.CreateArithmeticExerciseAsync(s.Father);
        await AssignAsync(s, arithmetic);

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));

        Assert.Equal(1, readiness.GetProperty("exerciseCount").GetInt32());
        Assert.Equal(0, readiness.GetProperty("vocabularyExerciseCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, readiness.GetProperty("totalItems").ValueKind);
        Assert.Equal(JsonValueKind.Null, readiness.GetProperty("secureItems").ValueKind);
    }

    /// <summary>
    /// B-945: two bundle exercises sharing one store word. Returns the setup and, per exercise, the item id of the shared
    /// word; the first exercise holds one further word of its own.
    /// </summary>
    private async Task<(Setup S, int SharedInA, int SharedInB, int OwnInA)> SharedWordAsync()
    {
        var s = await SetupAsync();
        var shared = ($"shared-{Guid.NewGuid():N}"[..14], "geteilt");
        var own = ($"own-{Guid.NewGuid():N}"[..14], "eigen");
        var a = await TestApi.CreateVocabExerciseAsync(s.Father, shared, own);
        var b = await TestApi.CreateVocabExerciseAsync(s.Father, shared);
        await AssignAsync(s, a, b);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var itemsA = db.ExerciseItems.Where(i => i.ExerciseId == a).OrderBy(i => i.OrderIndex).ToList();
        var itemB = db.ExerciseItems.Single(i => i.ExerciseId == b);
        // The premise of the case: both exercises reference the same store word.
        Assert.Equal(itemsA[0].VocabularyId, itemB.VocabularyId);
        return (s, itemsA[0].Id, itemB.Id, itemsA[1].Id);
    }

    [Fact]
    public async Task Wort_In_Zwei_Uebungen_Zaehlt_Einmal_Und_Ein_Ungeuebtes_Item_Zieht_Nur_Den_Schnitt()
    {
        var (s, sharedInA, _, _) = await SharedWordAsync();
        // Known in A, never practised in B: average (100 + 0) / 2 = 50, at the threshold - secure. "Secure in all its
        // items" would say weak here.
        SeedMastery(s.ChildId, sharedInA, 100);

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));

        Assert.Equal(2, readiness.GetProperty("totalItems").GetInt32()); // two words, not three items
        Assert.Equal(1, readiness.GetProperty("secureItems").GetInt32());
    }

    [Fact]
    public async Task Wort_Ist_Erst_Sicher_Wenn_Sein_Schnitt_Ueber_Die_Buendel_Items_Reicht()
    {
        var (s, sharedInA, sharedInB, ownInA) = await SharedWordAsync();
        // Secure in A, weak in B: average 35 - weak. "Secure in at least one item" would say secure here.
        SeedMastery(s.ChildId, sharedInA, 60);
        SeedMastery(s.ChildId, sharedInB, 10);
        SeedMastery(s.ChildId, ownInA, ItemProgress.WeakBelowPercent);

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));

        Assert.Equal(2, readiness.GetProperty("totalItems").GetInt32());
        Assert.Equal(1, readiness.GetProperty("secureItems").GetInt32()); // only the own word
    }

    [Fact]
    public async Task Ungeuebtes_Item_Zaehlt_Im_Schnitt_Als_Null_Statt_Zu_Verschwinden()
    {
        var (s, sharedInA, _, _) = await SharedWordAsync();
        // Secure in A, never practised in B: average (80 + 0) / 2 = 40 - weak. Averaging only the practised items would
        // say 80 and call a word secure the child never met in the repetition.
        SeedMastery(s.ChildId, sharedInA, 80);

        var readiness = await s.Child.GetFromJsonAsync<JsonElement>(Readiness(s.ChildId, s.ClassTestId));

        Assert.Equal(0, readiness.GetProperty("secureItems").GetInt32());
    }

    [Fact]
    public async Task Nur_Das_Kind_Der_Arbeit_Und_Sein_Betreuer()
    {
        var s = await SetupAsync();
        var other = await SetupAsync();

        // Another child: neither through its own path (the test is not its own) nor through the foreign path.
        Assert.Equal(HttpStatusCode.NotFound, (await other.Child.GetAsync(Readiness(other.ChildId, s.ClassTestId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Child.GetAsync(Readiness(s.ChildId, s.ClassTestId))).StatusCode);

        var strangerPin = "8" + Random.Shared.Next(100, 999);
        var strangerId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin = strangerPin }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, strangerPin);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Readiness(s.ChildId, s.ClassTestId))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await s.Child.GetAsync(Readiness(s.ChildId, s.ClassTestId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.Father.GetAsync(Readiness(s.ChildId, s.ClassTestId))).StatusCode);
    }
}
