using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Integrationstests für den Birkenbihl-Übungstyp: Happy-Path (Anlegen + typisiertes
/// Zurücklesen der Wort-Tuple und der natürlichen Übersetzung) und der Rollen-Fall
/// (nur der Vater pflegt den Katalog – ein Kind-Token wird abgewiesen).
/// </summary>
public class BirkenbihlExerciseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<HttpClient> FatherClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "0000" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpClient> ChildClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/child", new { childId = 1, pin = "1111" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Legt Fach + Kapitel an und gibt die Basis-Route der Birkenbihl-Übungen zurück.</summary>
    private static async Task<string> CreateChapterAsync(HttpClient father)
    {
        var subjectRes = await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Birkenbihl-Test" });
        var subjectId = (await subjectRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var chapterRes = await father.PostAsJsonAsync($"/api/v1/learn/subjects/{subjectId}/chapters",
            new { name = "Lektion 1", orderIndex = 1 });
        var chapterId = (await chapterRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        return $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/birkenbihl";
    }

    [Fact]
    public async Task Anlegen_UndLesen_ErhaeltWortTupelUndUebersetzung()
    {
        var father = await FatherClientAsync();
        var route = await CreateChapterAsync(father);

        var payload = new
        {
            title = "Getting to know each other",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                learningLang = "Englisch",
                nativeLang = "Deutsch",
                sentences = new[]
                {
                    new
                    {
                        text = "What is your name?",
                        decoding = new[]
                        {
                            new { word = "What", literal = "Was" },
                            new { word = "is", literal = "ist" },
                            new { word = "your", literal = "dein" },
                            new { word = "name", literal = "Name" },
                        },
                        naturalTranslation = "Wie heißt du?",
                    },
                },
            },
        };

        var createRes = await father.PostAsJsonAsync(route, payload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var exerciseId = created.GetProperty("id").GetInt32();

        // Zurücklesen: die Config muss typisiert und verlustfrei erhalten sein.
        var getRes = await father.GetAsync($"{route}/{exerciseId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var body = await getRes.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Birkenbihl", body.GetProperty("type").GetString());
        var config = body.GetProperty("config");
        Assert.Equal("Englisch", config.GetProperty("learningLang").GetString());

        var sentence = config.GetProperty("sentences").EnumerateArray().Single();
        Assert.Equal("What is your name?", sentence.GetProperty("text").GetString());
        Assert.Equal("Wie heißt du?", sentence.GetProperty("naturalTranslation").GetString());

        var decoding = sentence.GetProperty("decoding").EnumerateArray().ToArray();
        Assert.Equal(4, decoding.Length);
        Assert.Equal("What", decoding[0].GetProperty("word").GetString());
        Assert.Equal("Was", decoding[0].GetProperty("literal").GetString());
        Assert.Equal("name", decoding[3].GetProperty("word").GetString());
        Assert.Equal("Name", decoding[3].GetProperty("literal").GetString());
    }

    [Fact]
    public async Task Kind_KannKeineUebungAnlegen_Liefert403()
    {
        // Der Katalog wird ausschließlich vom Vater gepflegt ([Authorize(Roles = Vater)]).
        var father = await FatherClientAsync();
        var route = await CreateChapterAsync(father);

        var child = await ChildClientAsync();
        var res = await child.PostAsJsonAsync(route, new { title = "Hack", orderIndex = 1, rewardPoints = 999, config = new { } });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
