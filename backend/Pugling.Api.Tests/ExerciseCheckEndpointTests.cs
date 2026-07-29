using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Die <c>check</c>-/<c>generate</c>-Endpunkte der typisierten Übungen: die Naht zwischen geprüfter Logik
/// und unbelegter Ausspielung.
/// <para>
/// Angelegt beim Schließen der Abdeckungslücke (docs/codequalitaet-gates-plan.md, C3). Der Plan nennt genau
/// dieses Muster als lehrreich: <c>ArithmeticProblemGeneratorTests</c> prüft den <b>Algorithmus</b>, aber
/// den <b>Endpunkt</b>, der ihn nach außen gibt, rief nie jemand auf. Bei generiertem Code reißt es
/// typischerweise dort – die Logik stimmt, die Ausspielung nicht.
/// </para>
/// </summary>
public class ExerciseCheckEndpointTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Fach + Kapitel, unter denen die Übungen dieses Tests hängen.</summary>
    private static async Task<string> ChapterBaseAsync(HttpClient creator, string fach)
    {
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"{fach}-{Guid.NewGuid():N}"[..20] }));
        var chapterId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));
        return $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}";
    }

    [Fact]
    public async Task Rechen_Drill_Erzeugt_Aufgaben_Reproduzierbar_Und_Bewertet_Sie()
    {
        var creator = await TestApi.FatherAsync(factory);
        var basis = await ChapterBaseAsync(creator, "Drill");
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{basis}/arithmetic-drill", new
        {
            title = "Kopfrechnen",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { operations = new[] { "Addition" }, minOperand = 2, maxOperand = 9, problemCount = 4 },
        }));

        // Der Drill speichert nur die *Regeln*; die Aufgaben entstehen pro Abruf. Damit Üben und Bewerten
        // dieselbe Aufgabenmenge sehen, muss derselbe Seed dieselben Aufgaben liefern.
        var ersteAusspielung = await Json(await creator.PostAsJsonAsync($"{basis}/arithmetic-drill/{exerciseId}/generate?seed=4711", new { }));
        var zweiteAusspielung = await Json(await creator.PostAsJsonAsync($"{basis}/arithmetic-drill/{exerciseId}/generate?seed=4711", new { }));
        Assert.Equal(4, ersteAusspielung.GetProperty("problems").GetArrayLength());
        Assert.Equal(ersteAusspielung.GetProperty("problems").GetRawText(), zweiteAusspielung.GetProperty("problems").GetRawText());
        Assert.Equal(4711, ersteAusspielung.GetProperty("seed").GetInt32());

        // Bewerten: die richtigen Antworten aus derselben Ausspielung müssen alle als richtig gelten.
        var antworten = ersteAusspielung.GetProperty("problems").EnumerateArray()
            .Select((p, i) => new { index = i, value = p.GetProperty("answer").GetRawText() })
            .ToList();
        var alleRichtig = await Json(await creator.PostAsJsonAsync($"{basis}/arithmetic-drill/{exerciseId}/check",
            new { answers = antworten, seed = 4711 }));
        Assert.Equal(4, alleRichtig.GetProperty("correct").GetInt32());

        // Und eine falsche Antwort schlägt durch – sonst wäre die Bewertung ein Ja-Sager.
        var eineFalsch = await Json(await creator.PostAsJsonAsync($"{basis}/arithmetic-drill/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "99999" } }, seed = 4711 }));
        Assert.Equal(0, eineFalsch.GetProperty("correct").GetInt32());
    }

    [Fact]
    public async Task Zuordnungs_Uebung_Bewertet_Paare()
    {
        var creator = await TestApi.FatherAsync(factory);
        var basis = await ChapterBaseAsync(creator, "Matching");
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{basis}/matching", new
        {
            title = "Tier zu Laut",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { pairs = new[] { new { left = "dog", right = "Hund" }, new { left = "cat", right = "Katze" } } },
        }));

        var richtig = await Json(await creator.PostAsJsonAsync($"{basis}/matching/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "Hund" }, new { index = 1, value = "Katze" } } }));
        Assert.Equal(2, richtig.GetProperty("correct").GetInt32());

        var vertauscht = await Json(await creator.PostAsJsonAsync($"{basis}/matching/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "Katze" }, new { index = 1, value = "Hund" } } }));
        Assert.Equal(0, vertauscht.GetProperty("correct").GetInt32());
    }

    [Fact]
    public async Task Birkenbihl_Satz_Laesst_Sich_Wieder_Loeschen()
    {
        var creator = await TestApi.FatherAsync(factory);
        var basis = await ChapterBaseAsync(creator, "Birkenbihl");
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{basis}/birkenbihl", new
        {
            title = "Erste Sätze",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { learningLang = "en", nativeLang = "de" },
        }));

        var satz = await Json(await creator.PostAsJsonAsync($"{basis}/birkenbihl/{exerciseId}/sentences",
            new { learningSentence = "the dog runs", naturalTranslation = "Der Hund läuft" }));
        var sentenceId = satz.GetProperty("sentenceId").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent,
            (await creator.DeleteAsync($"{basis}/birkenbihl/{exerciseId}/sentences/{sentenceId}")).StatusCode);
        // Zweimal löschen findet nichts mehr – der Fehlerfall der Route.
        Assert.Equal(HttpStatusCode.NotFound,
            (await creator.DeleteAsync($"{basis}/birkenbihl/{exerciseId}/sentences/{sentenceId}")).StatusCode);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
