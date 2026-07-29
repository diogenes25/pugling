using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Bild-Zuordnungen lesen und wieder <b>lösen</b> – an der Übung, am einzelnen Item, an der Variante, sowie
/// das Nachschlagen eines Motivs über seinen Schlüssel.
/// <para>
/// Angelegt beim Schließen der Abdeckungslücke (docs/codequalitaet-gates-plan.md, C3). Der Aufbau der
/// Zuordnungen war belegt, das Lösen nicht – und das Lösen ist die Seite, an der ein Bild ungewollt an einer
/// Karte hängen bleibt (Anti-Cheat: ein Motiv zeigt die Bedeutung in beide Richtungen, siehe
/// docs/medien-bilder.md).
/// </para>
/// </summary>
public class MediaLinkTeardownTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Eindeutig(string präfix) => $"{präfix}-{Guid.NewGuid():N}"[..20];

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Ein Motiv mit einer Karten-Variante – das Minimum, das eine Zuordnung tragen kann.</summary>
    private static async Task<(int AssetId, string Key, int VariantId)> MotivAsync(HttpClient creator)
    {
        var key = Eindeutig("m");
        var assetId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/media",
            new { description = "Ein Pferd auf der Weide", key }));
        var variantId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"/api/v1/creator/media/{assetId}/variants",
            new { purpose = "Card", url = "https://example.test/pferd.webp", width = 400, height = 300 }));
        return (assetId, key, variantId);
    }

    [Fact]
    public async Task Motiv_Ueber_Den_Schluessel_Finden_Und_Verschlagworten()
    {
        var creator = await TestApi.FatherAsync(factory);
        var (assetId, key, _) = await MotivAsync(creator);

        // Der Key ist der stabile fachliche Schlüssel eines Motivs (Autoren referenzieren ihn, nicht die Id).
        Assert.Equal(assetId, (await Json(await creator.GetAsync($"/api/v1/creator/media/by-key/{key}"))).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync("/api/v1/creator/media/by-key/gibt-es-nicht")).StatusCode);

        // Interessen-Schlagworte entscheiden über die Bildauswahl je Kind – sie werden *ergänzt*, nicht ersetzt.
        var verschlagwortet = await Json(await creator.PostAsJsonAsync($"/api/v1/creator/media/{assetId}/tags",
            new { tags = new[] { "pferde", "tiere" } }));
        var tags = verschlagwortet.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Contains("pferde", tags);
        Assert.Contains("tiere", tags);
    }

    [Fact]
    public async Task Bild_An_Uebung_Und_Item_Laesst_Sich_Wieder_Loesen()
    {
        var creator = await TestApi.FatherAsync(factory);
        var (assetId, _, _) = await MotivAsync(creator);
        var (_, vocabKey) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("horse"), "das Pferd");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(creator, vocabKey);
        var uebung = await Json(await creator.GetAsync($"/api/v1/creator/exercises/{exerciseId}"));
        var itemsUrl = $"/api/v1/creator/subjects/{uebung.GetProperty("subjectId").GetInt32()}"
            + $"/chapters/{uebung.GetProperty("chapterId").GetInt32()}/vocabulary/{exerciseId}/items";
        var itemId = (await Json(await creator.GetAsync(itemsUrl)))[0].GetProperty("id").GetInt32();

        // An der Übung …
        var uebungsMedien = $"/api/v1/creator/exercises/{exerciseId}/media";
        var linkId = await TestApi.IdAsync(await creator.PostAsJsonAsync(uebungsMedien, new { mediaAssetId = assetId, weight = 5 }));
        Assert.Contains(linkId, (await Json(await creator.GetAsync(uebungsMedien)))
            .EnumerateArray().Select(l => l.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{uebungsMedien}/{linkId}")).StatusCode);
        Assert.Empty((await Json(await creator.GetAsync(uebungsMedien))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync($"{uebungsMedien}/{linkId}")).StatusCode);

        // … und am einzelnen Item (dort hängt das Bild an *einem* Vokabelpaar, nicht an der ganzen Übung).
        var itemMedien = $"/api/v1/creator/exercises/{exerciseId}/items/{itemId}/media";
        var itemLinkId = await TestApi.IdAsync(await creator.PostAsJsonAsync(itemMedien, new { mediaAssetId = assetId }));
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{itemMedien}/{itemLinkId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync($"{itemMedien}/{itemLinkId}")).StatusCode);
    }

    [Fact]
    public async Task Bild_Variante_Laesst_Sich_Loeschen()
    {
        var creator = await TestApi.FatherAsync(factory);
        var (assetId, _, variantId) = await MotivAsync(creator);
        var url = $"/api/v1/creator/media/{assetId}/variants";

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{url}/{variantId}")).StatusCode);
        Assert.Empty((await Json(await creator.GetAsync(url))).EnumerateArray());
        // Der Fehlerfall trägt einen eigenen Code – die Variante gehört zu *diesem* Asset oder gar nicht.
        var nochmal = await creator.DeleteAsync($"{url}/{variantId}");
        Assert.Equal(HttpStatusCode.NotFound, nochmal.StatusCode);
        Assert.Equal("media_variant_not_found",
            (await nochmal.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
