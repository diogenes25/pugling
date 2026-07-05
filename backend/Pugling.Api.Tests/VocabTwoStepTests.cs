using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Zweistufige Vokabel-Eingabe: „einfach" (ohne Key/Wortart) → Auto-Key + Default Other; später „komplex" per PATCH.</summary>
public class VocabTwoStepTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Einfach_OhneKeyUndWortart_GeneriertKeyUndDefaultOther()
    {
        var father = await TestApi.FatherAsync(_factory);

        var res = await father.PostAsJsonAsync("/api/v1/learn/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = "cat", translation = "Katze" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var v = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("en_cat_de_katze", v.GetProperty("key").GetString());
        Assert.Equal("Other", v.GetProperty("partOfSpeech").GetString());
    }

    [Fact]
    public async Task Einfach_ZweiGleiche_ErhaltenEindeutigeKeys()
    {
        var father = await TestApi.FatherAsync(_factory);
        object body = new { sourceLanguage = "en", targetLanguage = "de", word = "dog", translation = "Hund" };

        var first = await (await father.PostAsJsonAsync("/api/v1/learn/vocabulary", body)).Content.ReadFromJsonAsync<JsonElement>();
        var secondRes = await father.PostAsJsonAsync("/api/v1/learn/vocabulary", body);
        Assert.Equal(HttpStatusCode.Created, secondRes.StatusCode);
        var second = await secondRes.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("en_dog_de_hund", first.GetProperty("key").GetString());
        Assert.Equal("en_dog_de_hund_2", second.GetProperty("key").GetString());
    }

    [Fact]
    public async Task Komplex_SpaeterPerPatch_ErgaenztNounUndWortart()
    {
        var father = await TestApi.FatherAsync(_factory);
        var created = await (await father.PostAsJsonAsync("/api/v1/learn/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = "house", translation = "Haus" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var patched = await (await father.PatchAsJsonAsync($"/api/v1/learn/vocabulary/{id}",
            new { partOfSpeech = "Noun", noun = new { article = "das", plural = "Häuser" } }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Noun", patched.GetProperty("partOfSpeech").GetString());
        Assert.Equal("das", patched.GetProperty("noun").GetProperty("article").GetString());
    }
}
