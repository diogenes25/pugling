using System.Globalization;
using System.Text;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Renders remarks as a Markdown snapshot.
/// <para>
/// The export is more than a fallback for "no server running": it is the <b>only bridge to the
/// test skills</b>. <c>creator</c>/<c>supervisor</c>/<c>student</c> and <c>/smoke-test</c> run against a
/// throwaway DB and cannot read the user's remarks from the database at all – but they can read them
/// from a file in the repo.
/// </para>
/// <para>
/// The result is read by human <i>and</i> model, hence: a fixed heading structure, one entry per remark,
/// no tables (they break on long texts).
/// </para>
/// </summary>
public class RemarkExportService
{
    private static readonly Dictionary<RemarkStatus, string> StatusLabel = new()
    {
        [RemarkStatus.Open] = "offen",
        [RemarkStatus.Planned] = "eingeplant",
        [RemarkStatus.Done] = "erledigt",
        [RemarkStatus.Rejected] = "verworfen",
    };

    /// <summary>Markdown for the given remarks (already filtered and sorted).</summary>
    /// <param name="remarks">The remarks to export; <c>Comments</c> should be loaded, otherwise the history is missing.</param>
    /// <param name="filterNote">Human-readable description of the filter, for the document header.</param>
    /// <param name="generatedAt">Generation timestamp (UTC) – passed in rather than read internally, so the test can pin it down.</param>
    /// <param name="showAccounts">
    /// For the cross-account export (<c>scope=all</c>), show the account per contribution. In the normal
    /// case everything comes from one hand and the info would just be noise.
    /// </param>
    public string Render(IReadOnlyList<Remark> remarks, string filterNote, DateTime generatedAt, bool showAccounts = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Anmerkungen – Export");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"Stand: {Iso(generatedAt)} · {remarks.Count} {(remarks.Count == 1 ? "Eintrag" : "Einträge")} · Filter: {filterNote}");
        sb.AppendLine();
        // Der Hinweis steht bewusst im Dokument: Es landet im Repo und sieht dort aus wie eine
        // bearbeitbare Datei. Der Stand kommt aber aus der Datenbank – Handänderungen wären beim
        // nächsten Export weg.
        sb.AppendLine("> Erzeugt von `GET api/v1/remarks/export`. **Nicht von Hand bearbeiten** – die Quelle ist");
        sb.AppendLine("> die Datenbank. Status und Antworten ändert der Skill `anmerkungen` über die API.");
        sb.AppendLine();

        if (remarks.Count == 0)
        {
            sb.AppendLine("_Keine Anmerkungen für diesen Filter._");
            return sb.ToString();
        }

        foreach (var r in remarks)
        {
            var category = r.Category == RemarkCategory.Unspecified ? "ohne Einordnung" : r.Category.ToString();
            sb.AppendLine(CultureInfo.InvariantCulture, $"## #{r.Id} · {category} · {StatusLabel[r.Status]}");
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"- **Erfasst:** {Iso(r.CreatedAt)} von Konto {r.AccountId} ({r.AuthorRole})");

            var area = string.IsNullOrWhiteSpace(r.AppArea) ? "?" : r.AppArea;
            var route = string.IsNullOrWhiteSpace(r.Route) ? "_(keine Route)_" : $"`{Inline(r.Route)}`";
            sb.AppendLine(CultureInfo.InvariantCulture, $"- **Wo:** {route} ({area})");

            var refs = new List<string>();
            if (r.ChildId is { } c) refs.Add($"Kind {c}");
            if (r.ExerciseId is { } e) refs.Add($"Übung {e}");
            if (r.StudyPlanId is { } p) refs.Add($"Plan {p}");
            if (r.PlanPositionId is { } pos) refs.Add($"Position {pos}");
            if (refs.Count > 0) sb.AppendLine(CultureInfo.InvariantCulture, $"- **Bezug:** {string.Join(", ", refs)}");
            if (r.ParentRemarkId is { } parent) sb.AppendLine(CultureInfo.InvariantCulture, $"- **Folgt aus:** #{parent}");

            sb.AppendLine();
            // Der Text steht als normaler Absatz, nicht in einem Code-Block: Er stammt vom Menschen und
            // darf Markdown enthalten, ohne die Struktur zu sprengen.
            sb.AppendLine(r.Text.Trim());
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(r.ContextJson))
            {
                sb.AppendLine("**Zustand:**");
                AppendFenced(sb, "json", r.ContextJson!);
            }

            if (!string.IsNullOrWhiteSpace(r.RecentErrorsJson))
            {
                sb.AppendLine("**Letzte Fehler:**");
                // Bewusst roh: Das Backend interpretiert den Puffer nirgends fachlich (deshalb ist er ein
                // string und keine gemappte JSON-Spalte). Würde hier geparst, bräche der Export, sobald
                // das Frontend ein Feld ergänzt – und ein Modell liest JSON ohnehin problemlos.
                AppendFenced(sb, "json", r.RecentErrorsJson!);
            }

            if (!string.IsNullOrWhiteSpace(r.Answer))
            {
                var who = string.IsNullOrWhiteSpace(r.AnsweredBy) ? "unbekannt" : r.AnsweredBy!;
                var when = r.AnsweredAt is { } at ? Iso(at) : "ohne Zeitstempel";
                sb.AppendLine(CultureInfo.InvariantCulture, $"**Antwort** ({who}, {when}):");
                sb.AppendLine();
                sb.AppendLine(r.Answer!.Trim());
                sb.AppendLine();
            }

            AppendComments(sb, r, showAccounts);
        }

        return sb.ToString();
    }

    /// <summary>
    /// The history, chronological, below the answer. Set as a blockquote so that when reading it stays
    /// clear what the documented resolution is (the <c>Antwort</c>/answer) and what the path there was.
    /// <para>
    /// The history is the reason an export from today still knows something about yesterday: before this,
    /// the implementation note overwrote the analysis.
    /// </para>
    /// </summary>
    private static void AppendComments(StringBuilder sb, Remark r, bool showAccounts)
    {
        if (r.Comments.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"**Verlauf** ({r.Comments.Count}):");
        sb.AppendLine();
        var ordered = r.Comments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            var who = string.IsNullOrWhiteSpace(c.AuthorLabel) ? c.Author.ToString() : c.AuthorLabel!;
            var account = showAccounts && c.AuthorAccountId is { } a ? $", Konto {a}" : "";
            sb.AppendLine(CultureInfo.InvariantCulture, $"> **{who}** · {Iso(c.CreatedAt)}{account}");
            sb.AppendLine(">");
            // Jede Zeile einzeln zitieren: Ein mehrzeiliger Beitrag bräche das Zitat sonst nach der ersten
            // Zeile auf, und der Rest stünde als gewöhnlicher Absatz da.
            foreach (var line in c.Body.Trim().ReplaceLineEndings("\n").Split('\n'))
                sb.AppendLine(CultureInfo.InvariantCulture, $"> {line}");
            // Trennzeile zwischen zwei Beiträgen bleibt Teil des Zitats (zitiertes "> "), nicht nackt –
            // sonst liest das MD028-Regelwerk sie als Blockquote-Ende mitten im Verlauf (markdownlint).
            if (i < ordered.Count - 1) sb.AppendLine(">");
        }
        sb.AppendLine();
    }

    private static string Iso(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    /// <summary>Defuses backticks and line breaks for use in inline code.</summary>
    private static string Inline(string value) => value.Replace('`', '\'').ReplaceLineEndings(" ");

    /// <summary>
    /// Writes a code block whose fence is longer than any run of backticks in the content (CommonMark).
    /// Necessary because the content comes from the frontend and can also be filled in by hand via the
    /// API – an embedded ``` would otherwise close the block prematurely and break the document apart.
    /// </summary>
    private static void AppendFenced(StringBuilder sb, string language, string content)
    {
        var longest = 0;
        var run = 0;
        foreach (var ch in content)
        {
            if (ch == '`') { run++; longest = Math.Max(longest, run); }
            else run = 0;
        }

        var fence = new string('`', Math.Max(3, longest + 1));
        sb.AppendLine();
        sb.AppendLine(fence + language);
        sb.AppendLine(content.Trim());
        sb.AppendLine(fence);
        sb.AppendLine();
    }
}
