using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Exercise items as their own sub-resource (CRUD under <c>vocabulary/{id}/items</c>, author
/// protection) and the child-centric progress/history layer (per item + word rollup) fed from
/// server-authoritative practice/testing.
/// </summary>
public class ExerciseItemsAndProgressTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int s, int c, int exerciseId)> VocabWithItemsAsync(HttpClient f, params (string Front, string Back)[] items)
    {
        var vocab = items.Length > 0 ? items : [("hello", "hallo")];
        var subjectId = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Items-Fach" }));
        var s = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/creator/textbook-series", new
        {
            name = TestApi.UniqueName("Items-Reihe"),
            publisherId = (int?)null,
            subjectName = (string?)null,
            subjectId,
            schoolTypes = (string?)null,
            sourceLanguage = (string?)null,
            targetLanguage = (string?)null,
            notes = (string?)null,
        }));
        var c = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/creator/textbook-series/{s}/units", new { label = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary", new
        {
            title = "Items-Übung",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = vocab.Select(i => new { front = i.Front, back = i.Back }) },
        }));
        return (s, c, exerciseId);
    }

    // ---- /items CRUD -------------------------------------------------------------------------------

    [Fact]
    public async Task Item_MitBereitsEnthaltenerVokabel_Liefert409()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"));

        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items");
        var vocabularyId = items!.Single().GetProperty("vocabularyId").GetInt32();

        // The same store entry a second time: two items on the same word would create two competing
        // ItemProgress rows, and the progress of that same word would drift apart within one exercise.
        // The DB forbids it (unique), the controller reports it as a 409.
        var again = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items",
            new { vocabularyId });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("duplicate_vocabulary_in_exercise", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Items_CrudFullCycle_InlineUndPerStoreId()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"));
        var itemsUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items";

        // POST inline (creating in the store) + POST with an existing store id.
        var (storeId, _) = await TestApi.CreateStoreVocabAsync(father, "dog", "Hund");
        var added = await (await father.PostAsJsonAsync(itemsUrl, new { front = "cat", back = "Katze", hint = "das Tier" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cat", added.GetProperty("front").GetString());
        Assert.Equal("das Tier", added.GetProperty("hint").GetString());
        Assert.True(added.GetProperty("vocabularyId").GetInt32() > 0);
        var byId = await father.PostAsJsonAsync(itemsUrl, new { vocabularyId = storeId });
        Assert.Equal(HttpStatusCode.Created, byId.StatusCode);

        var list = await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl);
        Assert.Equal(3, list!.Count); // hello (inline seed) + cat + dog
        Assert.Contains(list!, i => i.GetProperty("front").GetString() == "dog");

        // PATCH: change the hint; DELETE: remove the item.
        var catId = added.GetProperty("id").GetInt32();
        var patched = await (await father.PatchAsJsonAsync($"{itemsUrl}/{catId}", new { hint = "" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(patched.GetProperty("hint").ValueKind == JsonValueKind.Null);

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{itemsUrl}/{catId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"{itemsUrl}/{catId}")).StatusCode);
        Assert.Equal(2, (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))!.Count);
    }

    [Fact]
    public async Task AddItem_OhneVokabelAngabe_Liefert400()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father);
        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items", new { hint = "nix" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AddItem_FremderVater_Liefert403()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father);

        // A second adult (not the author) must not change the exercise's items.
        int otherId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var other = new Adult { Name = "Fremder", Email = $"fremd-{Guid.NewGuid():N}@x.de", Pin = Pugling.Api.Auth.PinHasher.Hash("2222") };
            db.Adults.Add(other);
            db.SaveChanges();
            otherId = other.Id;
        }
        var stranger = await TestApi.AdultAsync(_factory, otherId, "2222");

        var res = await stranger.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items", new { front = "sun", back = "Sonne" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---- Child-centric progress + history + word rollup -------------------------------------

    [Fact]
    public async Task Practice_SchreibtItemFortschritt_UndHistorie_UndWortRollup()
    {
        var father = await TestApi.AdultAsync(_factory);
        // Unique words, so that the per-child shared progress/store does not collide with other tests.
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("apple", "Apfel"), ("banana", "Banane"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var scoped = $"/api/v1/student/children/1/vocabulary-progress?exerciseId={exerciseId}";

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Apfel");    // correct
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, givenAnswer: "falsch");   // wrong

        // The supervisor view (narrowed to this exercise): the progress hangs on the child, weakest first.
        var progress = await father.GetFromJsonAsync<List<JsonElement>>(scoped);
        Assert.Equal(2, progress!.Count);
        var apple = progress!.First(p => p.GetProperty("front").GetString() == "apple");
        Assert.Equal(1, apple.GetProperty("seenCount").GetInt32());
        Assert.Equal(1, apple.GetProperty("correctCount").GetInt32());
        Assert.True(apple.GetProperty("box").GetInt32() > 1);
        Assert.True(apple.GetProperty("masteryPercent").GetInt32() > 0);
        JsonAssert.True(apple, "lastCorrect");

        // onlyWeak: returns only items with mastery < 50 % (here both: banana box 1 = 0 %, apple box 2 = 25 %).
        var weak = await father.GetFromJsonAsync<List<JsonElement>>($"{scoped}&onlyWeak=true");
        Assert.Contains(weak!, p => p.GetProperty("front").GetString() == "banana");
        Assert.All(weak!, p => Assert.True(p.GetProperty("masteryPercent").GetInt32() < 50));

        // The single view per item - the same numbers as in the list, only without the filters around it.
        var itemId = apple.GetProperty("itemId").GetInt32();
        var einzeln = await father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/1/vocabulary-progress/{itemId}");
        Assert.Equal(itemId, einzeln.GetProperty("itemId").GetInt32());
        Assert.Equal("apple", einzeln.GetProperty("front").GetString());
        Assert.Equal(apple.GetProperty("box").GetInt32(), einzeln.GetProperty("box").GetInt32());
        // An item without a learning state for this child does not exist - not even as null values.
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync("/api/v1/student/children/1/vocabulary-progress/999999")).StatusCode);

        // History per item (the ItemId is globally unique → only this exercise wrote there).
        var history = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/student/children/1/vocabulary-progress/{itemId}/history");
        Assert.Single(history!);
        JsonAssert.True(history![0], "wasCorrect");
        Assert.Equal("Practice", history![0].GetProperty("source").GetString());

        // The word rollup across all exercises: it contains both words of this exercise (presence-based, collision-safe).
        var vocabIds = progress!.Select(p => p.GetProperty("vocabularyId").GetInt32()).ToHashSet();
        var byWord = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/student/children/1/vocabulary-progress/by-word");
        Assert.All(vocabIds, id => Assert.Contains(byWord!, w => w.GetProperty("vocabularyId").GetInt32() == id));

        // The child may read its own progress (ownership = itself).
        var self = await child.GetFromJsonAsync<List<JsonElement>>(scoped);
        Assert.Equal(2, self!.Count);
    }

    [Fact]
    public async Task WiederholteAntwort_TreibtBeherrschungNichtHoch_HistorieLoggtTrotzdem()
    {
        var father = await TestApi.AdultAsync(_factory);
        // Unique words (no collision with the per-child shared progress of other tests).
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("zebra", "Zebra"), ("tiger", "Tiger"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // The same card 3× correct: only the first is graded (anti-farming) - the box must not be farmed up.
        for (var i = 0; i < 3; i++)
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Zebra");

        var progress = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/student/children/1/vocabulary-progress?exerciseId={exerciseId}");
        var zebra = progress!.First(p => p.GetProperty("front").GetString() == "zebra");
        Assert.Equal(1, zebra.GetProperty("seenCount").GetInt32()); // only the graded answer counts
        Assert.Equal(2, zebra.GetProperty("box").GetInt32());        // box 2, not driven up

        // The history, by contrast, records all three answers (the ItemId is globally unique).
        var itemId = zebra.GetProperty("itemId").GetInt32();
        var history = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/student/children/1/vocabulary-progress/{itemId}/history");
        Assert.Equal(3, history!.Count);
    }

    [Fact]
    public async Task ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var itemsUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items";
        var firstId = (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))![0].GetProperty("id").GetInt32();

        // Take it into a study plan → the progress hangs on the position/item order.
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);

        // Index-shifting mutations are blocked now (409), non-shifting ones stay allowed.
        Assert.Equal(HttpStatusCode.Conflict, (await father.DeleteAsync($"{itemsUrl}/{firstId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { orderIndex = 9 })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync(itemsUrl, new { front = "sun", back = "Sonne" })).StatusCode); // appending is ok
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { hint = "Gruß" })).StatusCode);      // the hint is ok
    }
}
