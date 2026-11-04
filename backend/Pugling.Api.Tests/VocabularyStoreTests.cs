using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Happy path of the atomic vocabulary store (create/read, key uniqueness).</summary>
public class VocabularyStoreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Create_Get_ByKey_List()
    {
        var father = await TestApi.AdultAsync(factory);
        var create = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
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

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/vocabulary/{id}")).StatusCode);

        var byKey = await father.GetAsync("/api/v1/creator/vocabulary/by-key/en_cat_de_katze");
        Assert.Equal(HttpStatusCode.OK, byKey.StatusCode);
        Assert.Equal("cat", (await byKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("word").GetString());

        var list = await (await father.GetAsync("/api/v1/creator/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task DoppelterKey_Liefert409()
    {
        var father = await TestApi.AdultAsync(factory);
        var dto = new
        {
            key = "en_dog_de_hund",
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "dog",
            translation = "Hund",
            partOfSpeech = "Noun",
        };
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", dto)).StatusCode);
    }

    /// <summary>
    /// B-277: <c>reuseExisting</c> lets a quick-add hit the entry that already says this word instead of creating a
    /// <c>_2</c> copy. The two spellings differ in an accented capital, which SQLite's NOCASE does not fold - only the
    /// server's folding does, so a client-side comparison could not get this case right. Without the flag the old behaviour
    /// stays (a second entry), which is what keeps the flag from being decoration.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_TrifftDenEintragDesServerSchluessels_UndErgaenztVarianten()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var original = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"École{u}",
            translation = "Schule",
            translationAlternatives = new[] { "Lehranstalt" },
        });
        var originalId = await TestApi.IdAsync(original);

        var reused = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"école{u}",
            translation = "schule",
            translationAlternatives = new[] { "Bildungsstätte", "lehranstalt" },
            tags = new[] { $"B277-{u}" },
            reuseExisting = true,
        });

        Assert.Equal(HttpStatusCode.OK, reused.StatusCode);
        var body = await reused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(originalId, body.GetProperty("id").GetInt32());
        // Merged, not replaced; the repeated variant in another case is not listed twice.
        Assert.Equal(["Bildungsstätte", "Lehranstalt"], (await VariantenAsync(father, originalId))!.Order());
        Assert.Equal($"École{u}", body.GetProperty("word").GetString());
        // Additive only: the sent spelling of the translation does not overwrite the stored one, tags join in.
        Assert.Equal("Schule", body.GetProperty("translation").GetString());
        Assert.Contains($"B277-{u}", body.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));

        var copy = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"école{u}",
            translation = "schule",
        });
        Assert.Equal(HttpStatusCode.Created, copy.StatusCode);
        Assert.EndsWith("_2", (await copy.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString());
    }

    /// <summary>
    /// The flag must not change what is valid. With an explicit key it would be ignored without a word - the caller
    /// would get a 409 duplicate_key it did not ask about; on a hit it used to skip the base-form check that a create
    /// runs, and the unknown base form came back as a 200.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_UmgehtKeinePruefung()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var key = await (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
        })).Content.ReadFromJsonAsync<JsonElement>();

        var withKey = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            key = key.GetProperty("key").GetString(),
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
            reuseExisting = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, withKey.StatusCode);
        Assert.Equal("validation_error", (await withKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var unknownBase = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
            baseFormKey = $"gibt-es-nicht-{u}",
            reuseExisting = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownBase.StatusCode);
    }

    /// <summary>
    /// B-337: a PATCH changes word or translation but not the key, so key and content drift apart. Reuse must go by what
    /// the entry says now: the entry patched away from "riesig" is not a hit for "riesig" (it would swallow the typed
    /// translation), and the entry whose typo was fixed is a hit although its key still carries the typo.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_FolgtDemInhaltNichtDemVeraltetenSchluessel()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        async Task<int> CreateAsync(string word, string translation) => await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/vocabulary", new { sourceLanguage = "en", targetLanguage = "de", word, translation }));
        async Task<HttpResponseMessage> ReuseAsync(string word, string translation) => await father.PostAsJsonAsync(
            "/api/v1/creator/vocabulary", new { sourceLanguage = "en", targetLanguage = "de", word, translation, reuseExisting = true });

        var drifted = await CreateAsync($"huge{u}", "riesig");
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{drifted}", new { translation = "gewaltig" })).EnsureSuccessStatusCode();
        var notTheDrifted = await ReuseAsync($"huge{u}", "riesig");
        Assert.Equal(HttpStatusCode.Created, notTheDrifted.StatusCode);
        Assert.Equal("riesig", (await notTheDrifted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("translation").GetString());

        var fixedTypo = await CreateAsync($"hgue{u}", "Ungeheuer");
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{fixedTypo}", new { word = $"hgu{u}e" })).EnsureSuccessStatusCode();
        // The premise: the key still carries the typo, so the hit below can only come from the content.
        Assert.Contains($"hgue{u}", (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{fixedTypo}")).GetProperty("key").GetString());
        // Upper case on purpose: the stale key cannot find it, so the content comparison has to fold the case.
        var hit = await ReuseAsync($"HGU{u}E", "ungeheuer");
        Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
        Assert.Equal(fixedTypo, (await hit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// The content comparison folds what the key folds: language case and surrounding whitespace. The entry is stored
    /// as "EN"/"DE" and its key went stale through a PATCH, so only the content can find it. Red if the lookup compares
    /// the languages exactly (SQLite's default BINARY collation) or the untrimmed word - either one yields a new 201.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_FaltetSpracheUndLeerraumWieDerSchluessel()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "EN", targetLanguage = "DE", word = $"laek{u}", translation = "See" }));
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}", new { word = $"lake{u}" })).EnsureSuccessStatusCode();

        var hit = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $" lake{u} ", translation = "See", reuseExisting = true });

        Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
        Assert.Equal(id, (await hit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// B-346: "pomme de terre" → "Kartoffel" and "pomme" → "terre de Kartoffel" join to the same key, because the target
    /// language code "de" sits at the seam. They are different words, so reuse must not hand back the first for the
    /// second. Red if the lookup compares the joined key: 200 with the first entry instead of 201.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_UnterscheidetPaareMitGleichemVerbundschluessel()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var first = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "fr", targetLanguage = "de", word = $"pomme{u} de terre", translation = "Kartoffel" }));

        var second = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"pomme{u}",
            translation = "terre de Kartoffel",
            reuseExisting = true,
        });

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(first, body.GetProperty("id").GetInt32());
        Assert.Equal("terre de Kartoffel", body.GetProperty("translation").GetString());
    }

    /// <summary>
    /// B-347: an entry whose key a PATCH left stale is found by what it says now - also where the fold goes beyond what
    /// SQLite's NOCASE folds. Each case differs from the reused spelling in exactly one such property: a diacritic,
    /// a non-ASCII capital, surrounding whitespace on the stored side, an underscore against a run of spaces. Red if the candidates come from the NOCASE word
    /// column: 201 with a second entry of the same content.
    /// </summary>
    [Theory]
    [InlineData("#café", "#cafe")]
    [InlineData("#Über", "#über")]
    [InlineData(" #lake ", "#lake")]
    [InlineData("#_lake", "#  lake")]
    public async Task ReuseExisting_FindetGepatchtesWortUeberDieGanzeFaltung(string stored, string reused)
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"kaffe{u}", translation = "x" }));
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}", new { word = stored.Replace("#", u) })).EnsureSuccessStatusCode();

        var hit = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = reused.Replace("#", u), translation = "x", reuseExisting = true });

        Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
        Assert.Equal(id, (await hit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// Two entries say the same word (the second created without the flag, so it got a <c>_2</c> key); reuse hands back
    /// the oldest. The newer one is created second on purpose, so "newest wins" picks the other id. Red if the hits are
    /// ordered the other way.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_BeiZweiGleichenEintraegenGewinntDerAeltere()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        async Task<int> CreateAsync() => await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"twin{u}", translation = "Zwilling" }));
        var older = await CreateAsync();
        var newer = await CreateAsync();
        Assert.NotEqual(older, newer);

        var hit = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"twin{u}", translation = "Zwilling", reuseExisting = true });

        Assert.Equal(older, (await hit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// An entry added but not saved yet counts for a later lookup in the same unit of work, so a caller that resolves
    /// several words before saving does not create one word twice. Red if the lookup sees only saved rows: two instances.
    /// </summary>
    [Fact]
    public async Task GetOrCreate_VorDemSpeichern_FindetDenEbenAngelegtenEintrag()
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<VocabularyStoreService>();
        var u = Guid.NewGuid().ToString("N")[..8];

        var first = await store.GetOrCreateAsync("en", $"pending{u}", "de", "offen");
        var again = await store.GetOrCreateAsync("EN", $"Pending{u} ", "de", "Offen");

        Assert.Equal(0, first.Id);
        Assert.Same(first, again);
    }

    /// <summary>
    /// B-355: two pairs of the same identity in ONE call resolve to one entry. The vocabulary POST refuses such a payload
    /// before it gets here, the translation exercise does not - so this line is the only thing between "Haus, haus " in one
    /// exercise and two store entries (<c>key</c>, <c>key_2</c>). Red if a freshly created entry is not remembered for the
    /// rest of the batch: two instances, two Added rows.
    /// </summary>
    [Fact]
    public async Task GetOrCreateMany_ZweiGleichePaareImSelbenAufruf_LegenEinenEintragAn()
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<VocabularyStoreService>();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var u = Guid.NewGuid().ToString("N")[..8];

        var result = await store.GetOrCreateManyAsync("en", "de", [($"twice{u}", "zweimal"), ($"Twice{u} ", "Zweimal")], ct: default);

        Assert.Same(result[0], result[1]);
        Assert.Single(db.ChangeTracker.Entries<Vocabulary>(), e => e.State == EntityState.Added);
    }

    /// <summary>
    /// B-355: the language filter in SQL folds the STORED side too - an entry saved as " en " is a candidate for "en", as
    /// the identity says. Red if the stored language is compared untrimmed: 201 with a second entry. Only spaces are
    /// pinned: SQLite's trim() strips nothing else, and no writer stores a tab in a language code.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_FaltetDieGespeicherteSpracheMitLeerraum()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = " en ", targetLanguage = " de ", word = $"edge{u}", translation = "Rand" }));

        var hit = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"edge{u}", translation = "Rand", reuseExisting = true });

        Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
        Assert.Equal(id, (await hit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// B-349: within one batch, an entry's result must not depend on what earlier entries of the same batch loaded. The
    /// entry patched to "Hõuse" says "house" → "Haus"; sent alone and after a line that names it by its stale key,
    /// "house" must get the same answer. Red if the candidates are narrowed to key or NOCASE word again: then only the
    /// order made the entry visible, and "alone" creates a second one. The order independence itself follows from
    /// loading every entry of the pair - no tracker state can add a candidate, so no mutant of it is observable.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_ImBatch_HaengtNichtAnDerReihenfolge()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "en", targetLanguage = "de", word = $"hus{u}", translation = "Haus" }));
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}", new { word = $"Hõuse{u}" })).EnsureSuccessStatusCode();
        async Task<JsonElement> LastAsync(params string[] words) => (await (await father.PostAsJsonAsync(
            "/api/v1/creator/vocabulary/batch",
            words.Select(w => new { sourceLanguage = "en", targetLanguage = "de", word = w, translation = "Haus", reuseExisting = true })))
            .Content.ReadFromJsonAsync<List<JsonElement>>())!.Last();

        var afterLoad = await LastAsync($"hus{u}", $"house{u}");
        var alone = await LastAsync($"house{u}");

        Assert.Equal(("Existing", id), (afterLoad.GetProperty("status").GetString(), afterLoad.GetProperty("id").GetInt32()));
        Assert.Equal(("Existing", id), (alone.GetProperty("status").GetString(), alone.GetProperty("id").GetInt32()));
    }

    /// <summary>
    /// A replayed batch line whose explicit key exists stays <c>existing</c>, even when its base form no longer
    /// resolves - the entry is there, the repeat must not read as a failure. Red if the base form is validated before
    /// the key conflict (status <c>Error</c>).
    /// </summary>
    [Fact]
    public async Task Batch_VorhandenerSchluessel_MitVerschwundenerGrundform_BleibtExisting()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var key = $"wiederholt-{u}";
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { key, sourceLanguage = "en", targetLanguage = "de", word = $"went{u}", translation = "ging" }));

        var batch = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", new[]
        {
            new { key, sourceLanguage = "en", targetLanguage = "de", word = $"went{u}", translation = "ging", baseFormKey = $"weg-{u}" },
        });

        var result = (await batch.Content.ReadFromJsonAsync<List<JsonElement>>())!.Single();
        Assert.Equal("Existing", result.GetProperty("status").GetString());
        Assert.Equal(id, result.GetProperty("id").GetInt32());
    }

    /// <summary>The batch reports a reused entry as <c>existing</c> with its id - not as <c>created</c>.</summary>
    [Fact]
    public async Task ReuseExisting_ImBatch_MeldetExisting()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"river{u}",
            translation = "Fluss",
        }));

        var batch = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", new[]
        {
            new { sourceLanguage = "en", targetLanguage = "de", word = $"River{u}", translation = "fluss", reuseExisting = true },
        });

        var result = (await batch.Content.ReadFromJsonAsync<List<JsonElement>>())!.Single();
        Assert.Equal("Existing", result.GetProperty("status").GetString());
        Assert.Equal(id, result.GetProperty("id").GetInt32());
    }

    /// <summary>
    /// B-65: the equally valid translations survive POST, PATCH and both batch routes. The single PATCH is
    /// covered by <c>PatchSemanticsTests</c>; what needs its own test here is the batch pair – it maps the
    /// fields by hand and would silently drop a forgotten one.
    /// </summary>
    [Fact]
    public async Task Uebersetzungsvarianten_UeberlebenAlleSchreibwege()
    {
        var father = await TestApi.AdultAsync(factory);

        var created = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "huge",
            translation = "riesig",
            // Blanks, duplicates and the primary translation itself are cleaned out, while a translation
            // containing a comma stays intact - that is what the repeated field in the editor is for.
            translationAlternatives = new[] { "sehr groß", " ", "sehr groß", "Riesig", "groß, wirklich groß" },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await TestApi.IdAsync(created);
        Assert.Equal(new[] { "sehr groß", "groß, wirklich groß" }, await VariantenAsync(father, id));

        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { translationAlternatives = new[] { "enorm" } })).EnsureSuccessStatusCode();
        Assert.Equal(new[] { "enorm" }, await VariantenAsync(father, id));

        var batchPatch = await father.PatchAsJsonAsync("/api/v1/creator/vocabulary/batch",
            new[] { new { id, translationAlternatives = new[] { "gigantisch" } } });
        batchPatch.EnsureSuccessStatusCode();
        Assert.Equal(new[] { "gigantisch" }, await VariantenAsync(father, id));

        var batchClear = await father.PatchAsJsonAsync("/api/v1/creator/vocabulary/batch",
            new[] { new { id, clearTranslationAlternatives = true } });
        batchClear.EnsureSuccessStatusCode();
        Assert.Null(await VariantenAsync(father, id));

        var batchCreate = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", new[]
        {
            new { sourceLanguage = "en", targetLanguage = "de", word = "tiny", translation = "winzig",
                translationAlternatives = new[] { "sehr klein" } },
        });
        batchCreate.EnsureSuccessStatusCode();
        var neu = (await batchCreate.Content.ReadFromJsonAsync<JsonElement>())[0].GetProperty("id").GetInt32();
        Assert.Equal(new[] { "sehr klein" }, await VariantenAsync(father, neu));
    }

    /// <summary>
    /// A PATCH that only moves the translation onto one of its own alternatives must not leave that word
    /// listed twice - the entry would read "riesig, also: riesig".
    /// </summary>
    [Fact]
    public async Task Uebersetzung_AufEineVarianteGesetzt_LaesstSieNichtDoppeltStehen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (id, _) = await TestApi.CreateStoreVocabAsync(father, "enormous", "riesig",
            translationAlternatives: ["sehr groß"]);

        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { translation = "sehr groß" })).EnsureSuccessStatusCode();

        Assert.Null(await VariantenAsync(father, id));
    }

    /// <summary>Reads the equally valid translations of an entry back (<c>null</c> = none declared).</summary>
    private static async Task<string[]?> VariantenAsync(HttpClient father, int id)
    {
        var entry = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{id}");
        var field = entry.GetProperty("translationAlternatives");
        return field.ValueKind == JsonValueKind.Null
            ? null
            : [.. field.EnumerateArray().Select(v => v.GetString()!)];
    }
}
