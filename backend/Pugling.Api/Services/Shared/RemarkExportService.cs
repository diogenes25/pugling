using System.Globalization;
using System.Text;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Rendert Anmerkungen als Markdown-Schnappschuss.
/// <para>
/// Der Export ist mehr als eine Notlösung für „kein Server läuft": Er ist die <b>einzige Brücke zu den
/// Test-Skills</b>. <c>creator</c>/<c>supervisor</c>/<c>student</c> und <c>/smoke-test</c> laufen gegen eine
/// Wegwerf-DB und können die Anmerkungen des Nutzers gar nicht aus der Datenbank lesen – wohl aber aus
/// einer Datei im Repo.
/// </para>
/// <para>
/// Gelesen wird das Ergebnis von Mensch <i>und</i> Modell, deshalb: feste Überschriftenstruktur, ein
/// Eintrag je Anmerkung, keine Tabellen (die brechen bei langen Texten).
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

    /// <summary>Markdown für die übergebenen Anmerkungen (bereits gefiltert und sortiert).</summary>
    /// <param name="remarks">Die zu exportierenden Anmerkungen.</param>
    /// <param name="filterNote">Menschenlesbare Beschreibung des Filters, für den Kopf des Dokuments.</param>
    /// <param name="generatedAt">Erzeugungszeitpunkt (UTC) – wird durchgereicht statt intern gelesen, damit der Test ihn festnageln kann.</param>
    public string Render(IReadOnlyList<Remark> remarks, string filterNote, DateTime generatedAt)
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
        }

        return sb.ToString();
    }

    private static string Iso(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    /// <summary>Entschärft Backticks und Zeilenumbrüche für die Verwendung in Inline-Code.</summary>
    private static string Inline(string value) => value.Replace('`', '\'').ReplaceLineEndings(" ");

    /// <summary>
    /// Schreibt einen Code-Block, dessen Zaun länger ist als jede Backtick-Folge im Inhalt (CommonMark).
    /// Nötig, weil der Inhalt aus dem Frontend stammt und über die API auch von Hand befüllt werden kann –
    /// ein eingebettetes ``` würde den Block sonst vorzeitig schließen und das Dokument zerlegen.
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
