using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Übungs-Items als eigene Sub-Ressource (CRUD unter <c>vocabulary/{id}/items</c>, Autoren-Schutz) und die
/// kind-zentrische Fortschritts-/Historien-Ebene (pro Item + Wort-Rollup), die aus dem server-autoritativen
/// Üben/Testen gespeist wird.
/// </summary>
public class ExerciseItemsAndProgressTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int s, int c, int exerciseId)> VocabWithItemsAsync(HttpClient f, params (string Front, string Back)[] items)
    {
        var vocab = items.Length > 0 ? items : [("hello", "hallo")];
        var s = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Items-Fach" }));
        var c = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/learn/subjects/{s}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary", new
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
    public async Task Items_CrudFullCycle_InlineUndPerStoreId()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"));
        var itemsUrl = $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items";

        // POST inline (Store-Anlage) + POST per bestehender Store-Id.
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

        // PATCH: Hinweis ändern; DELETE: Item entfernen.
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
        var father = await TestApi.FatherAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father);
        var res = await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items", new { hint = "nix" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AddItem_FremderVater_Liefert403()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father);

        // Ein zweiter Vater (nicht der Autor) darf die Items der Übung nicht ändern.
        int otherId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var other = new Father { Name = "Fremder", Email = $"fremd-{Guid.NewGuid():N}@x.de", Pin = Pugling.Api.Auth.PinHasher.Hash("2222") };
            db.Fathers.Add(other);
            db.SaveChanges();
            otherId = other.Id;
        }
        var stranger = await TestApi.FatherAsync(_factory, otherId, "2222");

        var res = await stranger.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items", new { front = "sun", back = "Sonne" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---- Kind-zentrischer Fortschritt + Historie + Wort-Rollup -------------------------------------

    [Fact]
    public async Task Practice_SchreibtItemFortschritt_UndHistorie_UndWortRollup()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Eindeutige Wörter, damit der pro-Kind geteilte Fortschritt/Store nicht mit anderen Tests kollidiert.
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("apple", "Apfel"), ("banana", "Banane"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var scoped = $"/api/v1/children/1/vocabulary-progress?exerciseId={exerciseId}";

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Apfel");    // richtig
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, givenAnswer: "falsch");   // falsch

        // Vater-Sicht (auf diese Übung eingegrenzt): der Fortschritt hängt am Kind, schwächste zuerst.
        var progress = await father.GetFromJsonAsync<List<JsonElement>>(scoped);
        Assert.Equal(2, progress!.Count);
        var apple = progress!.First(p => p.GetProperty("front").GetString() == "apple");
        Assert.Equal(1, apple.GetProperty("seenCount").GetInt32());
        Assert.Equal(1, apple.GetProperty("correctCount").GetInt32());
        Assert.True(apple.GetProperty("box").GetInt32() > 1);
        Assert.True(apple.GetProperty("masteryPercent").GetInt32() > 0);
        JsonAssert.True(apple, "lastCorrect");

        // onlyWeak: liefert nur Items mit Beherrschung < 50 % (hier beide: banana Box 1 = 0 %, apple Box 2 = 25 %).
        var weak = await father.GetFromJsonAsync<List<JsonElement>>($"{scoped}&onlyWeak=true");
        Assert.Contains(weak!, p => p.GetProperty("front").GetString() == "banana");
        Assert.All(weak!, p => Assert.True(p.GetProperty("masteryPercent").GetInt32() < 50));

        // Historie je Item (ItemId global eindeutig → nur diese Übung schrieb dorthin).
        var itemId = apple.GetProperty("itemId").GetInt32();
        var history = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/children/1/vocabulary-progress/{itemId}/history");
        Assert.Single(history!);
        JsonAssert.True(history![0], "wasCorrect");
        Assert.Equal("Practice", history![0].GetProperty("source").GetString());

        // Wort-Rollup über alle Übungen: enthält die beiden Wörter dieser Übung (präsenzbasiert, kollisionsfest).
        var vocabIds = progress!.Select(p => p.GetProperty("vocabularyId").GetInt32()).ToHashSet();
        var byWord = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/children/1/vocabulary-progress/by-word");
        Assert.All(vocabIds, id => Assert.Contains(byWord!, w => w.GetProperty("vocabularyId").GetInt32() == id));

        // Der Sohn darf seinen eigenen Fortschritt lesen (Ownership = er selbst).
        var self = await child.GetFromJsonAsync<List<JsonElement>>(scoped);
        Assert.Equal(2, self!.Count);
    }

    [Fact]
    public async Task WiederholteAntwort_TreibtBeherrschungNichtHoch_HistorieLoggtTrotzdem()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Eindeutige Wörter (kein Kollidieren mit dem pro-Kind geteilten Fortschritt anderer Tests).
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("zebra", "Zebra"), ("tiger", "Tiger"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // Dieselbe Karte 3× richtig: nur die erste wird gewertet (Anti-Farming) – die Box darf nicht hochgefarmt werden.
        for (var i = 0; i < 3; i++)
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Zebra");

        var progress = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/children/1/vocabulary-progress?exerciseId={exerciseId}");
        var zebra = progress!.First(p => p.GetProperty("front").GetString() == "zebra");
        Assert.Equal(1, zebra.GetProperty("seenCount").GetInt32()); // nur die gewertete Antwort zählt
        Assert.Equal(2, zebra.GetProperty("box").GetInt32());        // Box 2, nicht hochgetrieben

        // Die Historie protokolliert dagegen alle drei Antworten (ItemId global eindeutig).
        var itemId = zebra.GetProperty("itemId").GetInt32();
        var history = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/children/1/vocabulary-progress/{itemId}/history");
        Assert.Equal(3, history!.Count);
    }

    [Fact]
    public async Task ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c, exerciseId) = await VocabWithItemsAsync(father, ("hello", "hallo"), ("bye", "tschuess"));
        var itemsUrl = $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items";
        var firstId = (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))![0].GetProperty("id").GetInt32();

        // In einen Lehrplan aufnehmen → Fortschritt hängt an der Position/Item-Reihenfolge.
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);

        // Index-verschiebende Mutationen sind jetzt geblockt (409), nicht-verschiebende bleiben erlaubt.
        Assert.Equal(HttpStatusCode.Conflict, (await father.DeleteAsync($"{itemsUrl}/{firstId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { orderIndex = 9 })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync(itemsUrl, new { front = "sun", back = "Sonne" })).StatusCode); // Anhängen ok
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync($"{itemsUrl}/{firstId}", new { hint = "Gruß" })).StatusCode);      // Hinweis ok
    }
}
