using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert das übungsunabhängige Schüler-Profil ab (Grundlage für einen späteren Lehrplan-Generator):
/// die persönlichen Felder am Kind (Geschlecht/Interessen/Notiz) und die Lehrbuch-Sub-Ressource
/// (CRUD + Eigentum + Cascade beim Löschen des Kindes).
/// </summary>
public class StudentProfileTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Vater_KannProfilfelder_AnlegenUndAendern()
    {
        var father = await TestApi.FatherAsync(factory);

        var created = await (await father.PostAsJsonAsync("/api/v1/supervisor/children", new
        {
            name = "Profil-Kind",
            pin = "8100",
            grade = 9,
            gender = "Male",
            interests = new[] { "Brawl Stars", "Pokémon" },
            profileNotes = "Mag kurze Aufgaben.",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var childId = created.GetProperty("id").GetInt32();

        Assert.Equal("Male", created.GetProperty("gender").GetString());
        Assert.Equal("Mag kurze Aufgaben.", created.GetProperty("profileNotes").GetString());
        Assert.Equal(["Brawl Stars", "Pokémon"],
            created.GetProperty("interests").EnumerateArray().Select(i => i.GetString()).ToArray());

        // GET liefert dieselben Profil-Felder zurück.
        var fetched = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}");
        Assert.Equal("Male", fetched.GetProperty("gender").GetString());
        Assert.Equal(2, fetched.GetProperty("interests").GetArrayLength());

        // PATCH ersetzt die Interessen-Liste (Neuzuweisung, kein In-Place-Mutieren).
        var patched = await (await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new
        {
            interests = new[] { "Fußball" },
            gender = "Diverse",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Diverse", patched.GetProperty("gender").GetString());
        Assert.Equal(["Fußball"],
            patched.GetProperty("interests").EnumerateArray().Select(i => i.GetString()).ToArray());
    }

    [Fact]
    public async Task Vater_KannLehrbuch_Anlegen_Lesen_Aendern_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Buch-Kind", pin = "8101" }));

        // Anlegen
        var created = await (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks", new
        {
            title = "Green Line 3",
            subjectName = "Englisch",
            grade = 9,
            publisher = "Klett",
            currentChapter = "Unit 4 – Past Tense",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var bookId = created.GetProperty("id").GetInt32();
        Assert.Equal("Green Line 3", created.GetProperty("title").GetString());

        // Liste enthält das Buch
        var list = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}/textbooks");
        Assert.Contains(bookId, list.EnumerateArray().Select(b => b.GetProperty("id").GetInt32()));

        // Einzeln lesen
        var single = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}");
        Assert.Equal("Unit 4 – Past Tense", single.GetProperty("currentChapter").GetString());

        // PATCH aktualisiert den Lernstand
        var patched = await (await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks/{bookId}", new { currentChapter = "Unit 5 – Future" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unit 5 – Future", patched.GetProperty("currentChapter").GetString());

        // Löschen → erneutes Löschen ist 404
        (await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}")).EnsureSuccessStatusCode();
        var again = await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Lehrbuch_MitUngueltigemFach_Ist400()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Fach-Kind", pin = "8102" }));

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks",
            new { title = "Irgendein Buch", subjectId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Vater_KommtNichtAnLehrbuecherFremderKinder_403Oder404()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await father.GetAsync("/api/v1/supervisor/children/999999/textbooks");

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoeschenDesKindes_RaeumtLehrbuecherMitAb()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Cascade-Kind", pin = "8103" }));
        var bookId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks", new { title = "Wegwerf-Buch" }));

        (await father.DeleteAsync($"/api/v1/supervisor/children/{childId}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(db.Textbooks.Any(t => t.Id == bookId));
    }
}
