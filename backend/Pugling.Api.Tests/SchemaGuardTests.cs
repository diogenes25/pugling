using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Tore für die <b>Form des Datenbankschemas</b> (docs/codequalitaet-gates-plan.md). Sie prüfen das
/// EF-Modell selbst, nicht sein Verhalten – deshalb brauchen sie keinen Host und keine Datenbank:
/// Modell und Migrations-Snapshot liegen beide in der Assembly.
/// <para>
/// Jeder Test trägt einen <b>Selbstschutz gegen falsch-grün</b>: greift die Reflexion nicht (leer
/// gebauter Kontext, verschobene Migrations-Assembly), sähe sie nichts und bestünde inhaltsleer.
/// </para>
/// </summary>
public class SchemaGuardTests
{
    // Kein Host, keine Migration: `HasPendingModelChanges` und `GetMigrations` vergleichen das Modell
    // mit dem in der Assembly liegenden Snapshot – die Verbindung wird dabei nie geöffnet.
    private static PuglingDbContext Context() =>
        new(new DbContextOptionsBuilder<PuglingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);

    /// <summary>
    /// <b>G1 – kein Modell-Drift.</b> Weicht das Modell vom Snapshot der letzten Migration ab, fehlt eine
    /// Migration. Das fiel bisher <i>nirgends</i> auf: die Tests fahren <c>Migrate()</c>, und eine Spalte,
    /// die nur im Modell existiert, wird von SQLite beim Lesen einfach nicht gefunden – der Fehler landet
    /// als scheinbar fachlicher Testfehler an ganz anderer Stelle.
    /// </summary>
    [Fact]
    public void Modell_Und_Migrationen_Stimmen_Ueberein()
    {
        using var db = Context();

        // Selbstschutz: ein leer gebauter Kontext hätte kein Modell und keine Migrationen – dann wäre
        // die Zusicherung unten wertlos.
        Assert.True(db.Model.GetEntityTypes().Count() >= 55,
            $"Zu wenige Entity-Typen im Modell ({db.Model.GetEntityTypes().Count()}) – greift die Reflexion?");
        var migrations = db.Database.GetMigrations().ToList();
        Assert.True(migrations.Count >= 1, "Keine Migrationen in der Assembly gefunden – falscher Kontext?");

        Assert.False(db.Database.HasPendingModelChanges(),
            "Das EF-Modell weicht vom Snapshot der letzten Migration ab. Erzeuge eine Migration "
            + "(siehe CLAUDE.md → Befehle) – nicht auf EnsureCreated zurückfallen.");
    }

    /// <summary>
    /// <b>G1b – die Kette bleibt bei genau einer Migration.</b> Solange die App unveröffentlicht ist und
    /// Altdaten verzichtbar sind, wird vor jedem Etappenabschluss neu gefaltet
    /// (<c>Data/Migrations</c> löschen + <c>migrations add InitialCreate</c>). Das macht
    /// Spaltenumbenennungen und Typwechsel kostenlos – kein generierter SQLite-Tabellen-Neubau, den
    /// jemand abnehmen muss.
    /// <para>
    /// Diese Zusicherung ist <b>bewusst endlich</b>: mit der ersten Veröffentlichung braucht es einen
    /// echten Upgrade-Pfad, und dann wird sie entfernt. Dass das eine sichtbare Entscheidung ist statt
    /// einer stillen Erosion, ist ihr eigentlicher Zweck.
    /// </para>
    /// </summary>
    [Fact]
    public void Migrationskette_Besteht_Aus_Genau_Einer_Migration()
    {
        using var db = Context();
        var migrations = db.Database.GetMigrations().ToList();

        Assert.True(migrations.Count == 1,
            $"Erwartet genau eine Migration, gefunden {migrations.Count}: {string.Join(", ", migrations)}. "
            + "Falte die Kette neu (Data/Migrations löschen, `dotnet dotnet-ef migrations add InitialCreate "
            + "--project backend/Pugling.Api --output-dir Data/Migrations`) – oder entferne diese Regel, "
            + "wenn die App veröffentlicht ist und einen echten Upgrade-Pfad braucht.");
        Assert.EndsWith("InitialCreate", migrations[0], StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>G9 – nur bewusste DB-Defaults.</b> Ein SQL-<c>DEFAULT</c> ist eine Zusage an Schreiber
    /// <i>außerhalb</i> von EF; EF selbst benennt in jedem <c>INSERT</c> alle gemappten Properties und
    /// konsultiert ihn nie. Vor dem Squash trugen 15 Spalten eine solche Klausel, ohne dass sie irgendwo
    /// im Modell stand: sie waren ein Nebenprodukt davon, per <c>AddColumn(defaultValue:…)</c> angehängt
    /// worden zu sein. Zwei davon waren sogar schädlich – ein <c>ConcurrencyStamp</c> mit Vorgabewert
    /// macht die optimistische Sperre für jede nicht über EF eingefügte Zeile wirkungslos, und das an
    /// geldrelevanten Tabellen.
    /// <para>
    /// Dieser Wächter verhindert, dass sie über die nächsten Migrationen wieder nachwachsen. Ein neuer
    /// Default ist erlaubt – aber nur als <c>HasDefaultValue</c> im Modell und mit einem Eintrag hier,
    /// also als Entscheidung statt als Nebenwirkung.
    /// </para>
    /// </summary>
    [Fact]
    public void Nur_Bewusste_Datenbank_Defaults()
    {
        // Begründete Ausnahmen: Property → warum der Default in der DB stehen muss.
        var erlaubt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Schlüssel sind Tabellen-/Spaltennamen (relationales Modell), nicht Entity-/Property-Namen.
            ["Exercises.ExecutePublic"] =
                "Fail-Safe: eine Übung ohne ausdrückliche Angabe bleibt für alle Creator ausführbar "
                + "(bisheriges Verhalten). Ein fehlender Wert darf hier nicht zu 'gesperrt' werden.",
        };

        using var db = Context();

        // Gefragt wird das *relationale* Modell (Tabellen/Spalten), nicht die Property-Metadaten:
        // `IProperty.GetDefaultValue()` liefert auch dort einen Wert, wo EF gar keine DEFAULT-Klausel
        // schreibt (z. B. an jedem `CreatedAt`) – der Test hätte reihenweise Spalten gemeldet, die im
        // erzeugten DDL keinen Default haben. `IColumn.DefaultValue` ist dagegen genau das, was der
        // DDL-Generator ausgibt; gegengeprüft an der migrierten Datei (dort steht exakt ein DEFAULT).
        var tables = db.Model.GetRelationalModel().Tables.ToList();
        var mitDefault = tables
            .SelectMany(t => t.Columns
                .Where(col => col.DefaultValue is not null || col.DefaultValueSql is not null)
                .Select(col => $"{t.Name}.{col.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Selbstschutz: greift die Reflexion nicht, wäre die Menge leer und der Test bestünde inhaltsleer –
        // obendrein wüsste er dann nicht, dass ihm der eine gewollte Default fehlt.
        Assert.True(tables.Count >= 55, $"Zu wenige Tabellen im relationalen Modell ({tables.Count}).");

        Assert.Equal(erlaubt.Keys.OrderBy(n => n, StringComparer.Ordinal), mitDefault);
    }

    /// <summary>
    /// <b>G4 – jedes persistierte Enum liegt als String in der DB.</b> Der Vertrag spricht nach außen
    /// ohnehin Strings (<c>JsonStringEnumConverter</c>); waren es innen Zahlen, gab es zwei Darstellungen
    /// desselben Werts und eine stille Kopplung an die Mitglieder-Reihenfolge – ein eingeschobener
    /// Enum-Wert hätte gespeicherte Daten umgedeutet.
    /// <para>
    /// Erlaubte Ausnahmen sind <c>[Flags]</c> (eine Bit-Kombination hat keinen Namen) und die ordnend
    /// verglichenen Enums, die im DbContext namentlich mit Grund gelistet sind. Dieser Test liest genau
    /// jene Liste, damit Regel und Ausnahme nicht an zwei Orten gepflegt werden müssen.
    /// </para>
    /// </summary>
    [Fact]
    public void Persistierte_Enums_Sind_Strings()
    {
        using var db = Context();

        var alleEnums = new List<string>();
        var alsZahl = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!type.IsEnum) continue;

                var name = $"{entity.ClrType.Name}.{property.Name}";
                alleEnums.Add(name);
                if (type.IsDefined(typeof(FlagsAttribute), inherit: false)) continue;

                // Die Ausnahmeliste des DbContext ist die Quelle – hier wird sie nur gelesen.
                if (PuglingDbContext.IntEnumErlaubt(name)) continue;

                if (property.GetProviderClrType() != typeof(string)
                    && property.GetValueConverter()?.ProviderClrType != typeof(string))
                    alsZahl.Add(name);
            }
        }

        // Selbstschutz: findet die Reflexion keine Enums, bestünde der Test inhaltsleer.
        Assert.True(alleEnums.Count >= 30, $"Zu wenige Enum-Properties gefunden ({alleEnums.Count}).");

        Assert.True(alsZahl.Count == 0,
            "Diese Enum-Spalten liegen als Zahl in der DB. Entweder greift die Konvention nicht, oder sie "
            + "brauchen einen begründeten Eintrag in PuglingDbContext.IntEnumsByDesign:\n  "
            + string.Join("\n  ", alsZahl.OrderBy(n => n, StringComparer.Ordinal)));
    }
}
