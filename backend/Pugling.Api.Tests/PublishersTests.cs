using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The publisher vocabulary (B-63): a shared, slug-idempotent list a <c>TextbookSeries</c> may point at -
/// no owner, because naming a publisher is not authorship (pattern <c>InterestTag</c>).
/// </summary>
public class PublishersTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Anlegen_IstIdempotent_UndLeitetDenSlugAusDemNamenAb()
    {
        var creator = await TestApi.AdultAsync(factory);

        var first = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = "Westermann" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("westermann", created.GetProperty("slug").GetString());

        // A second call: 200 instead of 409 - an agent may repeat the same catalog setup.
        var again = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = "Westermann" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(created.GetProperty("id").GetInt32(), await TestApi.IdAsync(again));
    }

    [Fact]
    public async Task Liste_Und_Einzelabruf_Und_Loeschen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers",
            new { name = "Ernst Klett Sprachen" }));

        var list = await creator.GetFromJsonAsync<JsonElement>("/api/v1/creator/publishers?search=Klett+Sprachen");
        Assert.Contains(list.EnumerateArray(), p => p.GetProperty("id").GetInt32() == id);

        var single = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/publishers/{id}");
        Assert.Equal("Ernst Klett Sprachen", single.GetProperty("name").GetString());
        // No series points at it yet.
        Assert.Equal(0, single.GetProperty("seriesCount").GetInt32());

        // A series referencing the publisher only loses the assignment on delete (SetNull) - no usage lock,
        // a publisher carries no content.
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), publisherId = id }));

        var deleted = await creator.DeleteAsync($"/api/v1/creator/publishers/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var series = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}");
        Assert.False(series.GetProperty("publisherId").ValueKind is JsonValueKind.Number);

        var afterDelete = await creator.GetAsync($"/api/v1/creator/publishers/{id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    // ── B-136: the display name is unique, not just the slug ─────────────────────────────────────────
    // Same defect class as B-124/B-133 one resource further. The slug freezes on rename, so "slug is free"
    // and "name is free" stop being the same question the moment anybody renames a publisher.

    /// <summary>
    /// The gap as a caller runs into it: rename, then post the old target name. Both slugs differ, so the
    /// slug guard waves it through and the picker of a series shows two indistinguishable entries.
    /// </summary>
    [Fact]
    public async Task Nach_Umbenennen_Entsteht_Kein_Zweiter_Verlag_Mit_Demselben_Namen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var alt = TestApi.UniqueName("Klett");
        var neu = TestApi.UniqueName("Cornelsen");
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = alt }));
        (await creator.PatchAsJsonAsync($"/api/v1/creator/publishers/{id}", new { name = neu })).EnsureSuccessStatusCode();

        // The slug is still the one derived from `alt`, so the slug of `neu` is unclaimed - and that is
        // exactly why the slug guard alone does not hold here.
        var zweiter = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = neu });

        Assert.Equal(HttpStatusCode.Conflict, zweiter.StatusCode);
        Assert.Equal("duplicate_publisher",
            (await zweiter.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// The mirror image, and the one that is worse than a duplicate: a slug hit whose row meanwhile carries
    /// a <em>different</em> name must not be handed back as an idempotent hit. A catalog agent would
    /// otherwise hang its series off a publisher it never named (the B-133 finding).
    /// </summary>
    [Fact]
    public async Task Slug_Treffer_Mit_Fremdem_Anzeigenamen_Gibt_Nicht_Die_Falsche_Zeile_Heraus()
    {
        var creator = await TestApi.AdultAsync(factory);
        var alt = TestApi.UniqueName("Diesterweg");
        var neu = TestApi.UniqueName("Schroedel");
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = alt }));
        (await creator.PatchAsJsonAsync($"/api/v1/creator/publishers/{id}", new { name = neu })).EnsureSuccessStatusCode();

        // `alt` derives to the slug this row still carries - but the row is called `neu` now.
        var wieder = await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = alt });

        Assert.Equal(HttpStatusCode.Conflict, wieder.StatusCode);
    }

    /// <summary>
    /// A rename onto a name another publisher already carries - in the only shape that the pre-existing
    /// slug guard does <b>not</b> already cover.
    /// <para>
    /// The obvious version of this test (create two, rename one onto the other) was measured green before
    /// the fix: while a row's slug still matches its name, "name taken" and "slug taken" are the same
    /// question, and the slug guard answers it. It only becomes a second question once a rename has
    /// decoupled the two - so that is what this case builds. Guarded by id, not by slug: excluding by slug
    /// would make the row collide with itself and no rename could ever go through (the B-97 trap).
    /// </para>
    /// </summary>
    [Fact]
    public async Task Umbenennen_Auf_Einen_Fremden_Namen_Wird_Abgewiesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var fremdAlt = TestApi.UniqueName("Duden");
        var fremdNeu = TestApi.UniqueName("Beltz");
        var eigen = TestApi.UniqueName("Oldenbourg");

        // The other publisher, renamed: it now answers to `fremdNeu` while still carrying the slug of
        // `fremdAlt` - so the slug `fremdNeu` derives to is unclaimed.
        var fremdId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = fremdAlt }));
        (await creator.PatchAsJsonAsync($"/api/v1/creator/publishers/{fremdId}", new { name = fremdNeu })).EnsureSuccessStatusCode();

        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers", new { name = eigen }));
        var kollision = await creator.PatchAsJsonAsync($"/api/v1/creator/publishers/{id}", new { name = fremdNeu });

        Assert.Equal(HttpStatusCode.Conflict, kollision.StatusCode);
        Assert.Equal("duplicate_publisher",
            (await kollision.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // And the counter-example that keeps the guard from being trivially "always 409": renaming to the
        // name it already has must still work, otherwise the row collides with itself.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/publishers/{id}", new { name = eigen }))
            .EnsureSuccessStatusCode();
    }
}
