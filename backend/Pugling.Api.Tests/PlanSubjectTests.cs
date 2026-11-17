using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-539: a study plan carries its subject (mandatory) and optionally a unit, and a child has at most one active plan
/// <b>per subject</b> instead of one in total. Every case runs on its own child, so the shared class DB cannot mix
/// plans of two cases into one activation group.
/// </summary>
public class PlanSubjectTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Plans = "/api/v1/supervisor/study-plans";

    private sealed record Setup(HttpClient Father, int ChildId, int English, int French, int EnglishUnit, int FrenchUnit);

    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Fach-Kind") }));
        var english = await SubjectAsync(father, "Englisch-Plan");
        var french = await SubjectAsync(father, "Französisch-Plan");
        var (_, englishUnit) = await TestApi.CreateSeriesAndUnitAsync(father, english, "Unit 3 – At home");
        var (_, frenchUnit) = await TestApi.CreateSeriesAndUnitAsync(father, french, "Unité 2");
        return new Setup(father, childId, english, french, englishUnit, frenchUnit);
    }

    private static async Task<int> SubjectAsync(HttpClient father, string name) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName(name) }));

    private static Task<HttpResponseMessage> CreateAsync(Setup s, object body) => s.Father.PostAsJsonAsync(Plans, body);

    private static async Task<int> PlanAsync(Setup s, int subjectId, bool active = true) =>
        await TestApi.IdAsync(await CreateAsync(s, new { childId = s.ChildId, title = "Plan", subjectId, durationDays = 10, active }));

    private static async Task<JsonElement> ReadAsync(Setup s, int planId) =>
        await s.Father.GetFromJsonAsync<JsonElement>($"{Plans}/{planId}");

    private static async Task<bool> ActiveAsync(Setup s, int planId) =>
        (await ReadAsync(s, planId)).GetProperty("active").GetBoolean();

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    /// <summary>A legacy plan without a subject (the migration could not derive one), put straight into the DB.</summary>
    private int LegacyPlanWithoutSubject(int childId, bool active = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = childId, Title = "Altplan", StartDate = today, EndDate = today.AddDays(9), Active = active };
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return plan.Id;
    }

    // ─────────────────────────────────────────────────────────────── AK 1: the subject is mandatory

    /// <summary>AK 1. Red without the mandatory check in <c>Create</c>: the plan is created without a subject (201).</summary>
    [Fact]
    public async Task Anlegen_Ohne_Fach_Und_Ohne_Unit_Ist_Ein_Validierungsfehler()
    {
        var s = await SetupAsync();
        await AssertProblemAsync(await CreateAsync(s, new { childId = s.ChildId, title = "Ohne Fach", durationDays = 10 }),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>
    /// AK 1 + 2. Red when the unit is not stored or its series' subject is not taken over: the response then lacks the
    /// subject (and creation fails on the mandatory check) or the unit.
    /// </summary>
    [Fact]
    public async Task Anlegen_Mit_Unit_Uebernimmt_Das_Fach_Der_Reihe()
    {
        var s = await SetupAsync();
        var res = await CreateAsync(s, new { childId = s.ChildId, title = "Unit-Plan", seriesUnitId = s.EnglishUnit, durationDays = 10 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var plan = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(s.English, plan.GetProperty("subjectId").GetInt32());
        Assert.Equal(s.EnglishUnit, plan.GetProperty("seriesUnitId").GetInt32());
        Assert.Equal("Unit 3 – At home", plan.GetProperty("seriesUnitLabel").GetString());

        // The read path projects the same: list and single read carry unit and label too.
        var read = await ReadAsync(s, plan.GetProperty("id").GetInt32());
        Assert.Equal(s.EnglishUnit, read.GetProperty("seriesUnitId").GetInt32());
        Assert.Equal("Unit 3 – At home", read.GetProperty("seriesUnitLabel").GetString());
    }

    /// <summary>AK 1. Red without the consistency check: an English plan with a French unit would be created (201).</summary>
    [Fact]
    public async Task Unit_Und_Abweichendes_Fach_Ist_Ein_Validierungsfehler()
    {
        var s = await SetupAsync();
        await AssertProblemAsync(await CreateAsync(s, new
        {
            childId = s.ChildId,
            title = "Widerspruch",
            subjectId = s.English,
            seriesUnitId = s.FrenchUnit,
            durationDays = 10,
        }), HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>
    /// AK 1. A unit whose series has no subject cannot supply one: without a subject in the request it is a validation
    /// error, with one it is fine. Red when the unit is trusted to always bring a subject.
    /// </summary>
    [Fact]
    public async Task Unit_Einer_Reihe_Ohne_Fach_Braucht_Ein_Mitgegebenes_Fach()
    {
        var s = await SetupAsync();
        var seriesId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe ohne Fach") }));
        var unitId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Kapitel 1", orderIndex = 1 }));

        await AssertProblemAsync(await CreateAsync(s, new { childId = s.ChildId, title = "Nur Unit", seriesUnitId = unitId, durationDays = 10 }),
            HttpStatusCode.BadRequest, "validation_error");
        var ok = await CreateAsync(s, new { childId = s.ChildId, title = "Unit + Fach", subjectId = s.French, seriesUnitId = unitId, durationDays = 10 });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(s.French, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subjectId").GetInt32());
    }

    /// <summary>
    /// AK 1. A plan without a subject must be given one by any PATCH - except one that only switches it off. Red without
    /// the check in <c>Update</c> (the title PATCH answers 200), or when switching off fails on it (400 instead of 200).
    /// </summary>
    [Fact]
    public async Task Patch_Auf_Plan_Ohne_Fach_Braucht_Eines_Ausser_Beim_Abschalten()
    {
        var s = await SetupAsync();
        var legacy = LegacyPlanWithoutSubject(s.ChildId);

        await AssertProblemAsync(await s.Father.PatchAsJsonAsync($"{Plans}/{legacy}", new { title = "Neuer Titel" }),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await s.Father.PatchAsJsonAsync($"{Plans}/{legacy}", new { active = true }),
            HttpStatusCode.BadRequest, "validation_error");

        var off = await s.Father.PatchAsJsonAsync($"{Plans}/{legacy}", new { active = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False(await ActiveAsync(s, legacy));

        var fixedUp = await s.Father.PatchAsJsonAsync($"{Plans}/{legacy}", new { title = "Mit Fach", subjectId = s.English });
        Assert.Equal(HttpStatusCode.OK, fixedUp.StatusCode);
        Assert.Equal(s.English, (await fixedUp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subjectId").GetInt32());
    }

    // ─────────────────────────────────────────────────────────────── AK 2: the optional unit

    /// <summary>
    /// AK 2. PATCH sets a unit (taking over its subject), <c>clearSeriesUnit</c> removes it and leaves the subject, and a
    /// subject that contradicts the plan's unit is refused. Red when the unit is ignored on PATCH, when the switch is
    /// missing, or when the consistency check only runs on create.
    /// </summary>
    [Fact]
    public async Task Patch_Setzt_Und_Entfernt_Die_Unit()
    {
        var s = await SetupAsync();
        var plan = await PlanAsync(s, s.English);

        var set = await (await s.Father.PatchAsJsonAsync($"{Plans}/{plan}", new { seriesUnitId = s.FrenchUnit }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(s.FrenchUnit, set.GetProperty("seriesUnitId").GetInt32());
        Assert.Equal("Unité 2", set.GetProperty("seriesUnitLabel").GetString());
        Assert.Equal(s.French, set.GetProperty("subjectId").GetInt32());

        await AssertProblemAsync(await s.Father.PatchAsJsonAsync($"{Plans}/{plan}", new { subjectId = s.English }),
            HttpStatusCode.BadRequest, "validation_error");

        var cleared = await (await s.Father.PatchAsJsonAsync($"{Plans}/{plan}", new { clearSeriesUnit = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("seriesUnitId").ValueKind);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("seriesUnitLabel").ValueKind);
        Assert.Equal(s.French, cleared.GetProperty("subjectId").GetInt32());
    }

    // ─────────────────────────────────────────────────────────────── AK 4: one active plan per subject

    /// <summary>
    /// AK 4, first case. Red with the old <c>DeactivateSiblingPlansAsync</c>: creating the French plan switched the
    /// English one off.
    /// </summary>
    [Fact]
    public async Task Plaene_Verschiedener_Faecher_Bleiben_Beide_Aktiv()
    {
        var s = await SetupAsync();
        var english = await PlanAsync(s, s.English);
        var french = await PlanAsync(s, s.French);

        Assert.True(await ActiveAsync(s, english));
        Assert.True(await ActiveAsync(s, french));
        var listed = await s.Father.GetFromJsonAsync<JsonElement>($"{Plans}?childId={s.ChildId}");
        Assert.Equal(2, listed.EnumerateArray().Count(p => p.GetProperty("isPlayable").GetBoolean()));
    }

    /// <summary>AK 4, second case. Red without any switch-off: the first English plan would stay active.</summary>
    [Fact]
    public async Task Zweiter_Plan_Desselben_Fachs_Schaltet_Den_Ersten_Ab()
    {
        var s = await SetupAsync();
        var first = await PlanAsync(s, s.English);
        var french = await PlanAsync(s, s.French);
        var second = await PlanAsync(s, s.English);

        Assert.False(await ActiveAsync(s, first));
        Assert.True(await ActiveAsync(s, second));
        Assert.True(await ActiveAsync(s, french));
    }

    /// <summary>
    /// AK 4, PATCH <c>active: true</c>. Red when the PATCH path switches off nothing (both English plans active) or
    /// everything (the French plan off).
    /// </summary>
    [Fact]
    public async Task Aktivieren_Per_Patch_Schaltet_Nur_Dasselbe_Fach_Ab()
    {
        var s = await SetupAsync();
        var running = await PlanAsync(s, s.English);
        var french = await PlanAsync(s, s.French);
        var reviewed = await PlanAsync(s, s.English, active: false);
        Assert.True(await ActiveAsync(s, running));

        (await s.Father.PatchAsJsonAsync($"{Plans}/{reviewed}", new { active = true })).EnsureSuccessStatusCode();

        Assert.False(await ActiveAsync(s, running));
        Assert.True(await ActiveAsync(s, reviewed));
        Assert.True(await ActiveAsync(s, french));
    }

    /// <summary>
    /// AK 4, PATCH subject change into an occupied subject. Red when the switch-off reads the subject before the change
    /// (the old English plan would stay on next to the moved one).
    /// </summary>
    [Fact]
    public async Task Fachwechsel_In_Ein_Belegtes_Fach_Schaltet_Den_Anderen_Ab()
    {
        var s = await SetupAsync();
        var english = await PlanAsync(s, s.English);
        var moved = await PlanAsync(s, s.French);

        (await s.Father.PatchAsJsonAsync($"{Plans}/{moved}", new { subjectId = s.English })).EnsureSuccessStatusCode();

        Assert.False(await ActiveAsync(s, english));
        Assert.True(await ActiveAsync(s, moved));
    }

    /// <summary>
    /// AK 4, PATCH child change. Red when the switch-off runs against the old child: the target child would keep two
    /// active English plans.
    /// </summary>
    [Fact]
    public async Task Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab()
    {
        var s = await SetupAsync();
        var otherChild = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Geschwister") }));
        var siblingsEnglish = await TestApi.IdAsync(await CreateAsync(s,
            new { childId = otherChild, title = "Geschwister-Englisch", subjectId = s.English, durationDays = 10 }));
        var siblingsFrench = await TestApi.IdAsync(await CreateAsync(s,
            new { childId = otherChild, title = "Geschwister-Französisch", subjectId = s.French, durationDays = 10 }));
        var moved = await PlanAsync(s, s.English);

        (await s.Father.PatchAsJsonAsync($"{Plans}/{moved}", new { childId = otherChild })).EnsureSuccessStatusCode();

        Assert.False(await ActiveAsync(s, siblingsEnglish));
        Assert.True(await ActiveAsync(s, siblingsFrench));
        Assert.True(await ActiveAsync(s, moved));
    }

    /// <summary>
    /// AK 4, plans without a subject form their own group: activating an English plan leaves a legacy plan without a
    /// subject running. Red when "no subject" is treated as a wildcard that matches every subject.
    /// </summary>
    [Fact]
    public async Task Plaene_Ohne_Fach_Sind_Eine_Eigene_Gruppe()
    {
        var s = await SetupAsync();
        var legacy = LegacyPlanWithoutSubject(s.ChildId);
        var english = await PlanAsync(s, s.English);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.True(await db.StudyPlans.Where(p => p.Id == legacy).Select(p => p.Active).SingleAsync());
        Assert.True(await ActiveAsync(s, english));
    }
}
