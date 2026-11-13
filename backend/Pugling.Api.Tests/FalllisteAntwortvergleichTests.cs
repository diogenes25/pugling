using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-493: one list of cases for "are two answers the same", read by the server (here), the father's web
/// (<c>answerNormalize.test.ts</c>) and the textbook loader (<c>test_laden.py</c>). The server is the truth: a change to
/// <see cref="StageMechanics.Normalize"/> that the list does not reflect turns this red; a change to the list turns the
/// other two readers red until they follow.
/// </summary>
public class FalllisteAntwortvergleichTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Pugling.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Pugling.sln nicht gefunden.");
    }

    public static IEnumerable<object[]> Faelle()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "docs", "fallliste-antwortvergleich.json")));
        foreach (var fall in doc.RootElement.GetProperty("faelle").EnumerateArray())
            yield return [fall.GetProperty("a").GetString()!, fall.GetProperty("b").GetString()!,
                fall.GetProperty("gleich").GetBoolean(), fall.GetProperty("warum").GetString()!];
    }

    // Red if Normalize and the list disagree on a case.
    [Theory]
    [MemberData(nameof(Faelle))]
    public void Normalize_Folgt_Der_Fallliste(string a, string b, bool gleich, string warum) =>
        Assert.True(StageMechanics.Normalize(a) == StageMechanics.Normalize(b) == gleich, warum);

    // The list must not shrink unnoticed: a reader with no cases checks nothing.
    [Fact]
    public void Fallliste_Ist_Nicht_Leer() => Assert.True(Faelle().Count() > 10);
}
