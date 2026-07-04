using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path des atomaren Vokabel-Stores (Anlegen/Lesen, Key-Eindeutigkeit).</summary>
public class VocabularyStoreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Create_Get_ByKey_List()
    {
        var father = await TestApi.FatherAsync(factory);
        var create = await father.PostAsJsonAsync("/api/v1/learn/vocabulary", new
        {
            key = "en_cat_de_katze",
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "cat",
            translation = "Katze",
            partOfSpeech = "Noun",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = await TestApi.IdAsync(create);

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/learn/vocabulary/{id}")).StatusCode);

        var byKey = await father.GetAsync("/api/v1/learn/vocabulary/by-key/en_cat_de_katze");
        Assert.Equal(HttpStatusCode.OK, byKey.StatusCode);
        Assert.Equal("cat", (await byKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("word").GetString());

        var list = await (await father.GetAsync("/api/v1/learn/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task DoppelterKey_Liefert409()
    {
        var father = await TestApi.FatherAsync(factory);
        var dto = new
        {
            key = "en_dog_de_hund",
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "dog",
            translation = "Hund",
            partOfSpeech = "Noun",
        };
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync("/api/v1/learn/vocabulary", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await father.PostAsJsonAsync("/api/v1/learn/vocabulary", dto)).StatusCode);
    }
}
