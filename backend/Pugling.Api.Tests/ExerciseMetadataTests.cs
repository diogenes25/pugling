using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Structured exercise metadata (grade level, school type, source, category) and the
/// pre-filtering built on top of it via <c>GET api/learn/exercises</c>.
/// </summary>
public class ExerciseMetadataTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Creates a subject + textbook series + series unit + a category and returns (subjectId, seriesId, seriesUnitId, categoryId).
    private static async Task<(int subjectId, int seriesId, int seriesUnitId, int categoryId)> SetupAsync(HttpClient father, string subject)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = subject }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new
            {
                name = TestApi.UniqueName($"Reihe-{subject}"),
                publisherId = (int?)null,
                subjectName = (string?)null,
                subjectId,
                schoolTypes = (string?)null,
                sourceLanguage = (string?)null,
                targetLanguage = (string?)null,
                notes = (string?)null,
            }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Kapitel 1", orderIndex = 1 }));
        var categoryId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories", new { name = "Grammatik" }));
        return (subjectId, seriesId, seriesUnitId, categoryId);
    }

    private static object ArithmeticBody(string title, int categoryId, int gradeMin, int gradeMax,
        string schoolTypes, int[]? interestTagIds = null) => new
        {
            title,
            orderIndex = 1,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
            gradeMin,
            gradeMax,
            schoolTypes,
            source = "Testbuch, Kapitel 1",
            categoryId,
            // Always sent, `null` included: the payload rejects unknown fields, so a body that leaves the
            // key out entirely is a different case - and that one has its own assertion below (AK 5).
            interestTagIds,
        };

    /// <summary>
    /// Creates a tag of the shared taxonomy and returns its id. A <b>fresh</b> one per call rather than a
    /// seeded slug: the assertions below are about the mapping, not about what the seed happens to contain,
    /// and a seeded tag would make them depend on data another story may edit.
    /// </summary>
    private static async Task<int> InterestTagIdAsync(HttpClient father, string label) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = TestApi.UniqueName(label) }));

    /// <summary>Reads the dressing of an exercise through the single-fetch path.</summary>
    private static async Task<int[]> DressingAsync(HttpClient father, string basePath, int exerciseId)
    {
        var body = await father.GetFromJsonAsync<JsonElement>($"{basePath}/{exerciseId}");
        return [.. body.GetProperty("interestTagIds").EnumerateArray().Select(x => x.GetInt32())];
    }

    /// <summary>
    /// <b>AK 1, 2 and 6 of B-46.</b> An exercise records which interests it is dressed in, gives them back
    /// on both read paths, and a PUT <b>replaces</b> the set instead of merging it - the latter is what
    /// makes a wrong tag removable at all.
    /// <para>
    /// Red probe: drop <c>SetInterestLinks</c> from <c>Create</c> and this case falls at the very first
    /// assertion; drop it from <c>Update</c> and it falls at the replacement.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Uebung_TraegtIhreEinkleidung_UndPutErsetztSie()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Einkleidung-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        var einhorn = await InterestTagIdAsync(father, "Einhorn");
        var roboter = await InterestTagIdAsync(father, "Roboter");

        var created = await father.PostAsJsonAsync(basePath,
            ArithmeticBody("Eingekleidet", categoryId, 5, 7, "Gymnasium", [roboter, einhorn]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = createdBody.GetProperty("id").GetInt32();

        // Ascending, whatever order the caller sent - that is what makes two dressings comparable (AK 6).
        int[] erwartet = [.. new[] { einhorn, roboter }.Order()];
        int[] ausDemPost = [.. createdBody.GetProperty("interestTagIds").EnumerateArray().Select(x => x.GetInt32())];
        Assert.Equal(erwartet, ausDemPost);
        Assert.Equal(erwartet, await DressingAsync(father, basePath, id));

        // And through the list path, which projects at a different place.
        var liste = await father.GetFromJsonAsync<JsonElement>(basePath);
        var inListe = liste.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);
        int[] ausDerListe = [.. inListe.GetProperty("interestTagIds").EnumerateArray().Select(x => x.GetInt32())];
        Assert.Equal(erwartet, ausDerListe);

        // PUT with only one of the two: the other is GONE, not kept. A merge would leave no way to
        // un-dress an exercise that was tagged wrongly.
        var put = await father.PutAsJsonAsync($"{basePath}/{id}",
            ArithmeticBody("Eingekleidet", categoryId, 5, 7, "Gymnasium", [einhorn]));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        int[] nurEinhorn = [einhorn];
        Assert.Equal(nurEinhorn, await DressingAsync(father, basePath, id));

        // An unknown id on the CHANGE path too, and this is the half the first version left unobserved
        // (found in the B-46 review): without the check `SetInterestLinks` would append a link with a
        // dangling FK, `SaveChangesAsync` would throw, and the caller would get a 500 - losing title,
        // config and metadata of an otherwise valid PUT instead of a clean rejection.
        var falsch = await father.PutAsJsonAsync($"{basePath}/{id}",
            ArithmeticBody("Umbenannt", categoryId, 5, 7, "Gymnasium", [einhorn, 999999]));
        Assert.Equal(HttpStatusCode.BadRequest, falsch.StatusCode);
        var problem = await falsch.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_reference", problem.GetProperty("code").GetString()); // since B-329

        // The rejected PUT wrote NOTHING - neither the dressing nor the title. This is the load-bearing
        // half: a check that rejects but has already half-written is worse than none.
        Assert.Equal(nurEinhorn, await DressingAsync(father, basePath, id));
        var unveraendert = await father.GetFromJsonAsync<JsonElement>($"{basePath}/{id}");
        Assert.Equal("Eingekleidet", unveraendert.GetProperty("title").GetString());

        // And empty means empty - the same full-representation rule as for every other field here.
        var leer = await father.PutAsJsonAsync($"{basePath}/{id}",
            ArithmeticBody("Eingekleidet", categoryId, 5, 7, "Gymnasium", []));
        Assert.Equal(HttpStatusCode.OK, leer.StatusCode);
        Assert.Empty(await DressingAsync(father, basePath, id));
    }

    /// <summary>
    /// <b>AK 2, second half, and AK 5.</b> An unknown tag id is a <c>validation_error</c>, not a silent
    /// skip - otherwise a mistyped id answers <c>201</c> and the author believes in a dressing that is not
    /// there. And an exercise created <i>without</i> the field stays readable with an empty list, which is
    /// the state every exercise written before this story is in.
    /// </summary>
    [Fact]
    public async Task UnbekannterTag_Wird_Abgelehnt_UndOhneAngabe_BleibtDieEinkleidungLeer()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Einkleidung-Fehler-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        var abgelehnt = await father.PostAsJsonAsync(basePath,
            ArithmeticBody("Falscher Tag", categoryId, 5, 7, "Gymnasium", [999999]));
        Assert.Equal(HttpStatusCode.BadRequest, abgelehnt.StatusCode);
        var problem = await abgelehnt.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_reference", problem.GetProperty("code").GetString()); // since B-329

        // Nothing was created on the way - a rejected POST must not leave half an exercise behind.
        var liste = await father.GetFromJsonAsync<JsonElement>(basePath);
        Assert.DoesNotContain(liste.EnumerateArray(), e => e.GetProperty("title").GetString() == "Falscher Tag");

        // Without the field at all: created, and the dressing is empty rather than absent (AK 5).
        var ohne = await father.PostAsJsonAsync(basePath, new
        {
            title = "Ohne Einkleidung",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
            categoryId,
        });
        Assert.Equal(HttpStatusCode.Created, ohne.StatusCode);
        var ohneBody = await ohne.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(ohneBody.GetProperty("interestTagIds").EnumerateArray());
    }

    /// <summary>
    /// <b>B-208 AK 2.</b> A PUT that leaves <c>interestTagIds</c> out entirely clears the dressing: the PUT is a
    /// full replacement (B-208 decision 1). The body deliberately omits the key rather than sending
    /// <c>null</c> - only omission is the case of the story. Pins existing behaviour (green on the original
    /// code, an intent statement); red if <c>SetInterestLinks</c> returns early on <c>null</c> ("keep").
    /// </summary>
    [Fact]
    public async Task Put_Ohne_InterestTagIds_Leert_Die_Einkleidung()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Einkleidung-Put-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";
        var einhorn = await InterestTagIdAsync(father, "Einhorn");

        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath,
            ArithmeticBody("Eingekleidet", categoryId, 5, 7, "Gymnasium", [einhorn])));
        int[] nurEinhorn = [einhorn];
        Assert.Equal(nurEinhorn, await DressingAsync(father, basePath, id));

        var put = await father.PutAsJsonAsync($"{basePath}/{id}", new
        {
            title = "Eingekleidet",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
            categoryId,
        });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Empty(await DressingAsync(father, basePath, id));
    }

    [Fact]
    public async Task Uebung_TraegtMetadaten_UndFilterFindetSie()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        // Create with metadata: grade 5-7, Gymnasium.
        var created = await father.PostAsJsonAsync(basePath, ArithmeticBody("Grammatik-Drill", categoryId, 5, 7, "Gymnasium"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, body.GetProperty("gradeMin").GetInt32());
        Assert.Equal("Grammatik", body.GetProperty("categoryName").GetString());
        Assert.Equal("Gymnasium", body.GetProperty("schoolTypes").GetString());

        // Pre-filtering: subject + grade 6 + Gymnasium + category → a hit.
        var hit = await Search(father, subjectId, grade: 6, schoolType: "Gymnasium", categoryId: categoryId);
        var treffer = hit.EnumerateArray().Single(e => e.GetProperty("title").GetString() == "Grammatik-Drill");

        // The search hit carries the points recommendation (B-45). Without this assertion the projection was
        // unobserved: replacing it with a constant kept the whole suite green, because the OpenAPI schema comes
        // from the record head and the drift gates therefore see no change. That is the same failure class the
        // recommendation itself fixes - a field written by creators and read by nobody.
        Assert.Equal(10, treffer.GetProperty("rewardPoints").GetInt32());
    }

    [Fact]
    public async Task Filter_SchliesstFalscheKlassenstufeUndSchulartAus()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";
        await father.PostAsJsonAsync(basePath, ArithmeticBody("Nur-Gym-5bis7", categoryId, 5, 7, "Gymnasium"));

        // Grade 3 is below GradeMin → no hit.
        var tooYoung = await Search(father, subjectId, grade: 3);
        Assert.Empty(tooYoung.EnumerateArray());

        // Realschule is not set → no hit.
        var wrongSchool = await Search(father, subjectId, schoolType: "Realschule");
        Assert.Empty(wrongSchool.EnumerateArray());

        // A matching grade without further filters → a hit.
        var ok = await Search(father, subjectId, grade: 6);
        Assert.Single(ok.EnumerateArray());
    }

    [Fact]
    public async Task Uebung_MitFremderArt_Wird_Abgelehnt()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, seriesId, seriesUnitId, _) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var (_, _, _, fremdeArtId) = await SetupAsync(father, $"Anderes-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        // The category belongs to another subject → BadRequest.
        var res = await father.PostAsJsonAsync(basePath, ArithmeticBody("Falsche Art", fremdeArtId, 5, 7, "Gymnasium"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// B-18: the source is a filter of its own, not part of the free-text search. A hit and a miss - the
    /// miss is the load-bearing half, because a filter that never excludes anything looks green forever.
    /// </summary>
    [Fact]
    public async Task Quelle_Filtert_AlsTeilstring_UndSchliesstFremdeAus()
    {
        var father = await TestApi.AdultAsync(factory);
        var (subjectId, seriesId, seriesUnitId, categoryId) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";
        await father.PostAsJsonAsync(basePath, ArithmeticBody("Aus-Testbuch", categoryId, 5, 7, "Gymnasium"));
        // A second exercise WITHOUT a source: it makes the filter separate inside a mixed set rather than
        // just emptying a set of one - and it is the only way the `Source != null` branch is ever taken.
        var ohneQuelle = (object)new
        {
            title = "Ohne-Quelle",
            orderIndex = 2,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "2 + 2", answer = 4, tolerance = 0 } } },
            categoryId,
        };
        await father.PostAsJsonAsync(basePath, ohneQuelle);

        // Substring of "Testbuch, Kapitel 1" - and lower case, so the LIKE path is covered too. The
        // tolerance comes from the OPERATOR, not from a collation: SQLite's LIKE folds ASCII, the `instr()`
        // that `Contains` maps to never does (B-135, SearchPattern).
        var hit = await Search(father, subjectId, source: "testbuch");
        Assert.Single(hit.EnumerateArray());
        Assert.Equal("Aus-Testbuch", hit[0].GetProperty("title").GetString());

        // A source that no exercise carries: the filter has to exclude, not merely pass everything through.
        var miss = await Search(father, subjectId, source: "Green Line");
        Assert.Empty(miss.EnumerateArray());

        // Without the parameter both come back - the addition is additive (AK 2).
        Assert.Equal(2, (await Search(father, subjectId)).GetArrayLength());
    }

    private static async Task<JsonElement> Search(HttpClient father, int subjectId,
        int? grade = null, string? schoolType = null, int? categoryId = null, string? source = null)
    {
        var query = $"?subjectId={subjectId}";
        if (grade is int g) query += $"&grade={g}";
        if (schoolType is not null) query += $"&schoolType={schoolType}";
        if (categoryId is int c) query += $"&categoryId={c}";
        if (source is not null) query += $"&source={Uri.EscapeDataString(source)}";
        var res = await father.GetAsync($"/api/v1/creator/exercises{query}");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ---- B-588: textbook page reference ----

    /// <summary>Creates a vocabulary series unit and returns the vocabulary authoring path of it.</summary>
    private static async Task<string> VocabPathAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Buchseite") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary";
    }

    private static object VocabConfig() => new
    {
        direction = "front-to-back",
        sourceLang = "en",
        targetLang = "de",
        items = new[] { new { front = "hello", back = "hallo" } },
    };

    // Always carries the key, `null` included - the body without the key is its own case (AK 4).
    private static object VocabBody(string? bookPages) =>
        new { title = "Seite", orderIndex = 1, rewardPoints = 10, config = VocabConfig(), bookPages };

    private static async Task<JsonElement?> BookPagesAsync(HttpResponseMessage res)
    {
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("bookPages");
    }

    /// <summary>
    /// B-588 AK 1: a page reference given on creation comes back in the typed response and in the catalog detail
    /// (the one the editor reads back from). Red when <c>Create</c>, <c>Map</c> or the detail drops the field.
    /// </summary>
    [Fact]
    public async Task Buchseite_Beim_Anlegen_Steht_In_Antwort_Und_Detail()
    {
        var father = await TestApi.AdultAsync(factory);
        var path = await VocabPathAsync(father);

        var created = await father.PostAsJsonAsync(path, VocabBody("42–43"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("42–43", body.GetProperty("bookPages").GetString());

        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{body.GetProperty("id").GetInt32()}");
        Assert.Equal("42–43", detail.GetProperty("bookPages").GetString());
    }

    /// <summary>
    /// B-588 AK 2: the page reference is trimmed, and a blank one is stored as none (<c>null</c>), never as <c>""</c>
    /// - the child's sheet shows a pill for every non-null value. Red without the trim or without the blank rule.
    /// </summary>
    [Fact]
    public async Task Buchseite_Wird_Getrimmt_Und_Leer_Heisst_Keine()
    {
        var father = await TestApi.AdultAsync(factory);
        var path = await VocabPathAsync(father);

        var trimmed = await BookPagesAsync(await father.PostAsJsonAsync(path, VocabBody(" 42 ")));
        Assert.Equal("42", trimmed!.Value.GetString());

        var blank = await BookPagesAsync(await father.PostAsJsonAsync(path, VocabBody("   ")));
        Assert.Equal(JsonValueKind.Null, blank!.Value.ValueKind);
    }

    /// <summary>
    /// B-588 AK 3: 20 characters are the limit, on POST and on PUT alike. Red without the length check or when it
    /// sits on only one of the two write paths.
    /// </summary>
    [Fact]
    public async Task Buchseite_Hoechstens_Zwanzig_Zeichen_Bei_Post_Und_Put()
    {
        var father = await TestApi.AdultAsync(factory);
        var path = await VocabPathAsync(father);
        var twenty = new string('7', 20);
        var twentyOne = new string('7', 21);

        var ok = await father.PostAsJsonAsync(path, VocabBody(twenty));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var id = (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var tooLongPost = await father.PostAsJsonAsync(path, VocabBody(twentyOne));
        Assert.Equal(HttpStatusCode.BadRequest, tooLongPost.StatusCode);
        Assert.Equal("validation_error", (await tooLongPost.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var tooLongPut = await father.PutAsJsonAsync($"{path}/{id}", VocabBody(twentyOne));
        Assert.Equal(HttpStatusCode.BadRequest, tooLongPut.StatusCode);
        Assert.Equal("validation_error", (await tooLongPut.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var okPut = await father.PutAsJsonAsync($"{path}/{id}", VocabBody(twenty));
        Assert.Equal(HttpStatusCode.OK, okPut.StatusCode);
    }

    /// <summary>
    /// B-588 AK 4: PUT is the full representation - a new value replaces the old one, and a PUT without the field
    /// clears it (same as <c>source</c>). Red when <c>Update</c> ignores the field or reads a missing one as "keep".
    /// </summary>
    [Fact]
    public async Task Buchseite_Put_Ersetzt_Und_Ohne_Feld_Leert()
    {
        var father = await TestApi.AdultAsync(factory);
        var path = await VocabPathAsync(father);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(path, VocabBody("42")));

        var replaced = await BookPagesAsync(await father.PutAsJsonAsync($"{path}/{id}", VocabBody("7")));
        Assert.Equal("7", replaced!.Value.GetString());

        var cleared = await father.PutAsJsonAsync($"{path}/{id}",
            new { title = "Seite", orderIndex = 1, rewardPoints = 10, config = VocabConfig() });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await BookPagesAsync(cleared))!.Value.ValueKind);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{id}");
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("bookPages").ValueKind);
    }
}
