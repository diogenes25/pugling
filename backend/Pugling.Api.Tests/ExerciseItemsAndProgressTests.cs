using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        // Registered through the API: since B-230 the PIN lives on the login account, which only the API creates.
        var otherId = await TestApi.IdAsync(await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Fremder", email = $"fremd-{Guid.NewGuid():N}@x.de", pin = "2222" }));
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

    /// <summary>
    /// B-21: the word rollup narrows to one language pair. A child learning English and French must not get its
    /// weak French words into the required vocabulary of an English exercise (the AI creator reads exactly this).
    /// Without the parameters nothing changes. Red if the filter is dropped (the French word shows up in the
    /// English list) or applied to the wrong column (the pair en→de then finds nothing).
    /// </summary>
    [Fact]
    public async Task WortRollup_FiltertAufDasSprachpaar()
    {
        var father = await TestApi.AdultAsync(_factory);
        var child = await TestApi.ChildAsync(_factory);
        async Task<int> WeakWordAsync(string src, string tgt, string front, string back)
        {
            var exerciseId = await TestApi.CreateVocabExerciseAsync(father, src, tgt, (front, back));
            var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
            var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "falsch");
            var rows = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/student/children/1/vocabulary-progress?exerciseId={exerciseId}");
            return Assert.Single(rows!).GetProperty("vocabularyId").GetInt32();
        }
        var english = await WeakWordAsync("en", "de", "the walrus", "das Walross");
        var french = await WeakWordAsync("fr", "de", "le morse", "das Walross (fr)");
        // Same source, other target: only the targetLang filter keeps it out of en→de.
        var englishToFrench = await WeakWordAsync("en", "fr", "the walrus (fr)", "le morse");

        List<int> Ids(List<JsonElement>? rows) => [.. rows!.Select(w => w.GetProperty("vocabularyId").GetInt32())];
        const string byWord = "/api/v1/student/children/1/vocabulary-progress/by-word?onlyWeak=true&take=200";

        var all = Ids(await father.GetFromJsonAsync<List<JsonElement>>(byWord));
        Assert.Contains(english, all);
        Assert.Contains(french, all);
        // Precondition for the targetLang check below: without the filter the en→fr word IS a weak word.
        Assert.Contains(englishToFrench, all);

        var en = Ids(await father.GetFromJsonAsync<List<JsonElement>>($"{byWord}&sourceLang=en&targetLang=de"));
        Assert.Contains(english, en);
        Assert.DoesNotContain(french, en);
        Assert.DoesNotContain(englishToFrench, en);

        var fr = Ids(await father.GetFromJsonAsync<List<JsonElement>>($"{byWord}&sourceLang=fr&targetLang=de"));
        Assert.Contains(french, fr);
        Assert.DoesNotContain(english, fr);
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

        // Index-shifting mutations are blocked now (409), non-shifting ones stay allowed. The code is its own (B-205): the
        // rule differs from "cannot delete, still used", and a client translating `exercise_in_use` said the wrong thing.
        foreach (var blocked in new[]
                 {
                     await father.DeleteAsync($"{itemsUrl}/{firstId}"),
                     await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { orderIndex = 9 }),
                     // Inserting at a fixed position shifts the later items too; a new word, or the duplicate check
                     // would answer first.
                     await father.PostAsJsonAsync(itemsUrl, new { front = "moon", back = "Mond", orderIndex = 0 }),
                 })
        {
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            var problem = await blocked.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("exercise_items_locked", problem.GetProperty("code").GetString());
        }
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync(itemsUrl, new { front = "sun", back = "Sonne" })).StatusCode); // appending is ok
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { hint = "Gruß" })).StatusCode);      // the hint is ok
    }

    // The whole-exercise PUT rewrites the same item list as the single routes (B-323): the lock must hold there
    // too, and a rejected PUT must not have saved its header fields either.
    [Fact]
    public async Task UebungsPut_BeiInPlanUebung_BlocktVerschiebung_ErlaubtAnhaengen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var exerciseUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}";
        var before = (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!;
        var hello = before[0].GetProperty("vocabularyId").GetInt32();
        var bye = before[1].GetProperty("vocabularyId").GetInt32();
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);

        // The title differs from "Items-Übung", so a half-written PUT shows up below.
        object Put(params object[] items) => new
        {
            title = "Umbenannt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items },
        };
        foreach (var blocked in new[]
                 {
                     await father.PutAsJsonAsync(exerciseUrl, Put(new { vocabularyId = bye })),                                // drops the first word
                     await father.PutAsJsonAsync(exerciseUrl, Put(new { vocabularyId = bye }, new { vocabularyId = hello })), // swaps the order
                     await father.PutAsJsonAsync(exerciseUrl, Put(new { vocabularyId = hello }, new { front = "moon", back = "Mond" }, new { vocabularyId = bye })), // inserts in the middle
                 })
        {
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("exercise_items_locked", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        var unchanged = (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!;
        Assert.Equal([hello, bye], unchanged.Select(i => i.GetProperty("vocabularyId").GetInt32()));
        Assert.Equal("Items-Übung", (await father.GetFromJsonAsync<JsonElement>(exerciseUrl)).GetProperty("title").GetString());

        // Refs win over items when the server reconciles, so the check must read them first: here the items alone
        // would look like "unchanged", while the refs drop "hello".
        var refsWin = await father.PutAsJsonAsync(exerciseUrl, new
        {
            title = "Umbenannt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                direction = "front-to-back",
                sourceLang = "en",
                targetLang = "de",
                items = new[] { new { vocabularyId = hello }, new { vocabularyId = bye } },
                refs = new[] { new { vocabularyId = bye } },
            },
        });
        Assert.Equal(HttpStatusCode.Conflict, refsWin.StatusCode);
        Assert.Equal([hello, bye], (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!
            .Select(i => i.GetProperty("vocabularyId").GetInt32()));

        // Appending behind the played words shifts nothing - allowed, like POST /items without an orderIndex.
        var appended = await father.PutAsJsonAsync(exerciseUrl, Put(new { vocabularyId = hello }, new { vocabularyId = bye }, new { front = "sun", back = "Sonne" }));
        Assert.Equal(HttpStatusCode.OK, appended.StatusCode);
        var after = (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!;
        Assert.Equal(3, after.Count);
        Assert.Equal(before.Select(i => i.GetProperty("id").GetInt32()), after.Take(2).Select(i => i.GetProperty("id").GetInt32()));

        // The same append expressed as refs: red if the check ignores refs (they would count as "no words" → 409).
        var sun = after[2].GetProperty("vocabularyId").GetInt32();
        var moon = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"moon{Guid.NewGuid():N}", translation = "Mond" }));
        var refsAppend = await father.PutAsJsonAsync(exerciseUrl, new
        {
            title = "Umbenannt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", refs = new[] { hello, bye, sun, moon }.Select(id => new { vocabularyId = id }) },
        });
        Assert.Equal(HttpStatusCode.OK, refsAppend.StatusCode);

        // Refs alone, dropping a played word - the plain path of an API client (B-332). Red if the check only looks at
        // `items`: then a refs-only PUT is not checked at all and the reconcile moves the progress with a 200.
        var refsOnlyDrop = await father.PutAsJsonAsync(exerciseUrl, new
        {
            title = "Umbenannt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", refs = new[] { bye, sun, moon }.Select(id => new { vocabularyId = id }) },
        });
        Assert.Equal(HttpStatusCode.Conflict, refsOnlyDrop.StatusCode);
        Assert.Equal("exercise_items_locked", (await refsOnlyDrop.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal([hello, bye, sun, moon], (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!
            .Select(i => i.GetProperty("vocabularyId").GetInt32()));
    }

    // refs-from-tags is a snapshot over the same reconciliation (B-323): a snapshot that drops a played word
    // would move the progress of every later word one place up.
    [Fact]
    public async Task RefsFromTags_BeiInPlanUebung_BlocktVerschiebung_ErlaubtAnhaengen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var tag = $"B323-{Guid.NewGuid():N}";
        var only = $"{tag}-ohne-erstes";
        var u = Guid.NewGuid().ToString("N")[..8];
        // Snapshots sort by store key, so the word prefixes fix the order: aa < bb < zz.
        async Task StoreAsync(string word, params string[] tags) =>
            (await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
                new { sourceLanguage = "en", targetLanguage = "de", word = word + u, translation = word + u, tags })).EnsureSuccessStatusCode();
        await StoreAsync("aa", tag);
        await StoreAsync("bb", tag, only);

        var (s, c, _) = await VocabWithItemsAsync(father);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary",
            new { title = "Tag-Übung", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", refs = Array.Empty<string>() } }));
        var exerciseUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}";
        (await father.PostAsJsonAsync($"{exerciseUrl}/refs-from-tags", new { tags = new[] { tag } })).EnsureSuccessStatusCode();
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);

        var dropped = await father.PostAsJsonAsync($"{exerciseUrl}/refs-from-tags", new { tags = new[] { only } });
        Assert.Equal(HttpStatusCode.Conflict, dropped.StatusCode);
        Assert.Equal("exercise_items_locked", (await dropped.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(["aa" + u, "bb" + u], (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!
            .Select(i => i.GetProperty("front").GetString()));

        await StoreAsync("zz", tag);
        Assert.Equal(HttpStatusCode.OK, (await father.PostAsJsonAsync($"{exerciseUrl}/refs-from-tags", new { tags = new[] { tag } })).StatusCode);
        Assert.Equal(["aa" + u, "bb" + u, "zz" + u], (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!
            .Select(i => i.GetProperty("front").GetString()));
    }

    // A word twice in one payload used to pass validation, save the exercise head and then hit the unique index
    // (ExerciseId, VocabularyId) in the reconcile: a 500 with a half-written exercise (B-331). Each shape alone,
    // so every branch of the check carries its own case.
    [Fact]
    public async Task UebungsPut_MitDoppeltemWort_Liefert400_UndSchreibtNichts()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var exerciseUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}";
        var before = (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!;
        var hello = before[0].GetProperty("vocabularyId").GetInt32();
        var bye = before[1].GetProperty("vocabularyId").GetInt32();

        // A word no other test puts in the store, so the inline case really takes the key branch (new word), not the id one.
        var fresh = $"moon{Guid.NewGuid():N}";
        // The title differs from "Items-Übung", so a half-written PUT shows up below.
        object Put(object config) => new { title = "Umbenannt", orderIndex = 1, rewardPoints = 10, config };
        var shapes = new (string Name, object Config)[]
        {
            ("refs twice", new { direction = "front-to-back", refs = new[] { hello, bye, hello }.Select(id => new { vocabularyId = id }) }),
            ("item id twice", new { direction = "front-to-back", sourceLang = "en", targetLang = "de",
                items = new[] { hello, bye, hello }.Select(id => new { vocabularyId = id }) }),
            ("inline word twice", new { direction = "front-to-back", sourceLang = "en", targetLang = "de",
                items = new[] { new { front = fresh, back = "Mond" }, new { front = fresh, back = "Mond" } } }),
            // Two spellings the store key folds into one (B-338): a check on the raw strings would let them through, and the
            // reconcile would map both onto the same new store entry - the old 500.
            ("inline word twice, folded by the key", new { direction = "front-to-back", sourceLang = "en", targetLang = "de",
                items = new[] { new { front = "M" + fresh[1..], back = "Mond" }, new { front = fresh, back = "mond" } } }),
            ("id and the same word inline", new { direction = "front-to-back", sourceLang = "en", targetLang = "de",
                items = new object[] { new { vocabularyId = hello }, new { front = "hello", back = "hallo" } } }),
            ("inline vocabularyId 0", new { direction = "front-to-back", sourceLang = "en", targetLang = "de",
                items = new[] { new { vocabularyId = 0 } } }),
        };
        foreach (var (name, config) in shapes)
        {
            var response = await father.PutAsJsonAsync(exerciseUrl, Put(config));
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {(int)response.StatusCode}");
            Assert.Equal("validation_error", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            Assert.Equal([hello, bye], (await father.GetFromJsonAsync<List<JsonElement>>($"{exerciseUrl}/items"))!
                .Select(i => i.GetProperty("vocabularyId").GetInt32()));
            Assert.Equal("Items-Übung", (await father.GetFromJsonAsync<JsonElement>(exerciseUrl)).GetProperty("title").GetString());
        }
    }

    // The POST runs the same validation; a rejected one must not leave an exercise without words behind.
    [Fact]
    public async Task UebungsPost_MitDoppeltemInlineWort_Liefert400_UndLegtNichtsAn()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, _) = await VocabWithItemsAsync(father);
        var listUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary";
        var countBefore = (await father.GetFromJsonAsync<List<JsonElement>>(listUrl))!.Count;

        var response = await father.PostAsJsonAsync(listUrl, new
        {
            title = "Doppelt",
            orderIndex = 2,
            rewardPoints = 10,
            config = new
            {
                direction = "front-to-back",
                sourceLang = "en",
                targetLang = "de",
                items = new[] { new { front = "star", back = "Stern" }, new { front = "star", back = "Stern" } }
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(countBefore, (await father.GetFromJsonAsync<List<JsonElement>>(listUrl))!.Count);
    }

    // Swapping an item onto a word another item already carries hit the unique index (ExerciseId, VocabularyId): a 500
    // where AddItem answers 409 (B-334). Naming the item's own word again is no duplicate and must stay allowed - the
    // two requests differ only in whose word they name.
    [Fact]
    public async Task PatchItem_AufDasWortEinesAnderenItems_Liefert409_AufDasEigeneNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var itemsUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items";
        var items = (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))!;
        var hello = items[0].GetProperty("vocabularyId").GetInt32();
        var bye = items[1].GetProperty("vocabularyId").GetInt32();
        var byeItem = items[1].GetProperty("id").GetInt32();

        var onto = await father.PatchAsJsonAsync($"{itemsUrl}/{byeItem}", new { vocabularyId = hello });
        Assert.Equal(HttpStatusCode.Conflict, onto.StatusCode);
        Assert.Equal("duplicate_vocabulary_in_exercise", (await onto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal([hello, bye], (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))!
            .Select(i => i.GetProperty("vocabularyId").GetInt32()));

        // The same word named inline instead of by id: the check must sit after the resolution, not in the id branch.
        var ontoInline = await father.PatchAsJsonAsync($"{itemsUrl}/{byeItem}", new { front = "hello", back = "hallo" });
        Assert.Equal(HttpStatusCode.Conflict, ontoInline.StatusCode);

        var own = await father.PatchAsJsonAsync($"{itemsUrl}/{byeItem}", new { vocabularyId = bye, hint = "Abschied" });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
    }

    // An inline word is resolved before the later checks; a rejected request must not leave its new store entry behind.
    // The plan lock is the check that fires after the resolution on both routes. Red if the resolution saves on its own.
    [Fact]
    public async Task AbgewiesenesInlineItem_LegtKeinenStoreEintragAn()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var itemsUrl = $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items";
        var firstId = (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))![0].GetProperty("id").GetInt32();
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var u = Guid.NewGuid().ToString("N")[..8];

        var added = await father.PostAsJsonAsync(itemsUrl, new { front = $"zeppelin{u}", back = "Luftschiff", orderIndex = 0 });
        var patched = await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { front = $"comet{u}", back = "Komet", orderIndex = 9 });

        Assert.Equal(HttpStatusCode.Conflict, added.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, patched.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"/api/v1/creator/vocabulary/by-key/en_zeppelin{u}_de_luftschiff")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"/api/v1/creator/vocabulary/by-key/en_comet{u}_de_komet")).StatusCode);
    }

    // B-337 on the exercise side: an inline item links the entry that says this word NOW. One entry drifted away from
    // its key (it no longer says "hus"), one had its typo fixed (its key still carries the typo). Red if the inline
    // lookup goes by the stored key: the first item would link the drifted entry, the second would get a new one.
    [Fact]
    public async Task InlineItem_FolgtDemInhaltNichtDemVeraltetenSchluessel()
    {
        var father = await TestApi.AdultAsync(_factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        async Task<int> StoreAsync(string word, string translation) => await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/vocabulary", new { sourceLanguage = "en", targetLanguage = "de", word, translation }));
        var drifted = await StoreAsync($"hus{u}", "Haus");
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{drifted}", new { word = $"house{u}" })).EnsureSuccessStatusCode();
        var fixedTypo = await StoreAsync($"tre{u}", "Baum");
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{fixedTypo}", new { word = $"tree{u}" })).EnsureSuccessStatusCode();

        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ($"hus{u}", "Haus"), ($"tree{u}", "Baum"));

        var items = (await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items"))!;
        Assert.NotEqual(drifted, items[0].GetProperty("vocabularyId").GetInt32());
        Assert.Equal(fixedTypo, items[1].GetProperty("vocabularyId").GetInt32());
    }
}
