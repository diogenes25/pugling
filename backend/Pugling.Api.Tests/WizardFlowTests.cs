using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Deckt den Weg des Lehrplan-Assistenten serverseitig ab: der Vater legt ein Kind mit Klassenstufe
/// und Schulart an, filtert passende Katalog-Übungen und baut aus den geseedeten Französisch-Vokabeln
/// einen Lehrplan – genau das Szenario „14-jähriger Sohn mit Problemen in Französisch".
/// </summary>
public class WizardFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Kind_MitKlasseUndSchulart_WirdPersistiert()
    {
        var father = await TestApi.FatherAsync(factory);

        var create = await father.PostAsJsonAsync("/api/v1/children",
            new { name = "Léo", birthYear = 2012, grade = 8, schoolType = "Gymnasium", pin = "2468" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var childId = await TestApi.IdAsync(create);

        var got = await (await father.GetAsync($"/api/v1/children/{childId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(8, got.GetProperty("grade").GetInt32());
        Assert.Equal("Gymnasium", got.GetProperty("schoolType").GetString());
    }

    [Fact]
    public async Task Franzoesisch_Lehrplan_FuerNeuesKind_KomplettErstellbar()
    {
        var father = await TestApi.FatherAsync(factory);

        // 1) Fach Französisch ist geseedet.
        var subjects = await (await father.GetAsync("/api/v1/learn/subjects")).Content.ReadFromJsonAsync<JsonElement>();
        var french = subjects.EnumerateArray().First(s => s.GetProperty("name").GetString()!.StartsWith("Franz"));
        var subjectId = french.GetProperty("id").GetInt32();

        // 2) Kind (14 J., 8. Klasse, Gymnasium) anlegen.
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/children",
            new { name = "Léo", birthYear = 2012, grade = 8, schoolType = "Gymnasium", pin = "2468" }));

        // 3) Übungssuche über Metadaten liefert die Französisch-Übungen der 8. Klasse.
        var hits = await (await father.GetAsync(
            $"/api/v1/learn/exercises?subjectId={subjectId}&grade=8&schoolType=Gymnasium")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(hits.GetArrayLength() >= 1);

        // 4) Französische Vokabeln aus dem Store einsammeln (ASCII-Keys).
        var vocab = await (await father.GetAsync("/api/v1/learn/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        var frKeys = vocab.EnumerateArray()
            .Where(v => v.GetProperty("sourceLanguage").GetString() == "fr")
            .Select(v => v.GetProperty("key").GetString()!)
            .ToArray();
        Assert.True(frKeys.Length >= 5);
        Assert.All(frKeys, k => Assert.True(k.All(char.IsAscii), $"Key nicht ASCII: {k}"));

        // 5) Lehrplan aus den Vokabeln bauen.
        var plan = await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId,
            title = "Französisch – Unité 2",
            method = "Vocabulary",
            subjectId,
            durationDays = 14,
            newItemsPerLesson = 5,
            dailyMinutesRequired = 15,
            dailyTestPassPercent = 80,
            defaultStage = 4,
            requireTypedTest = true,
            useLeitner = true,
            contentKeys = frKeys,
        });
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode);
        var created = await plan.Content.ReadFromJsonAsync<JsonElement>();
        var planId = created.GetProperty("id").GetInt32();
        Assert.Equal(childId, created.GetProperty("childId").GetInt32());
        Assert.Equal(subjectId, created.GetProperty("subjectId").GetInt32());
        Assert.Equal(frKeys.Length, created.GetProperty("items").GetArrayLength());

        // 6) Der Sohn sieht seinen Plan (Rollen-/Eigentums-sauber).
        var son = await TestApi.ChildAsync(factory, childId, "2468");
        var mine = await (await son.GetAsync($"/api/v1/study-plans?childId={childId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(mine.EnumerateArray(), p => p.GetProperty("id").GetInt32() == planId);
    }
}
