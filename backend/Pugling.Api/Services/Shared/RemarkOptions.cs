namespace Pugling.Api.Services.Shared;

/// <summary>
/// Einstellungen der Test-Anmerkungen (Abschnitt <c>Remarks</c>).
/// </summary>
public class RemarkOptions
{
    /// <summary>
    /// Ob <c>?scope=all</c> jedem Erwachsenen offensteht – also der kontenübergreifende Blick, den der
    /// Nachbereitungs-Skill braucht.
    /// <para>
    /// <b>Warum das ein eigener Schalter ist und nicht die Rolle <c>Admin</c>:</b> Ein Fehler zeigt sich oft
    /// nur in einer bestimmten Konstellation – ein frisch registrierter Vater ohne Übungen deckt Dinge auf,
    /// die beim geseedeten Papa nie auffallen, weil der von Anfang an Inhalte hat. Beim Testen entstehen
    /// darum ständig Wegwerf-Konten, und jedes müsste sonst erst mit einem Flag versehen werden.
    /// <c>Admin</c> ist dafür das falsche Werkzeug: Die Rolle umgeht auch die RWX-Rechte auf Übungen
    /// (<see cref="Auth.ExercisePermissionService"/>) – mit ihr dürfte jeder Vater fremde Übungen ändern,
    /// löschen und umrechten, und <c>ExerciseGrant</c> wäre Dekoration. Zwei Dinge, die nichts miteinander
    /// zu tun haben, hingen dann an einem Schalter.
    /// </para>
    /// <para>
    /// Vorgabe ist <c>true</c> in der Entwicklung und <c>false</c> sonst (gesetzt in <c>Program.cs</c>): Auf
    /// einer Entwicklungs-Instanz gehören alle Konten demselben Menschen, in Produktion läsen sonst fremde
    /// Familien gegenseitig ihre Testnotizen – und Antworten tragen Datei- und Zeilenverweise.
    /// Ein Student bleibt in <b>jedem</b> Fall ausgeschlossen.
    /// </para>
    /// </summary>
    public bool GlobalRead { get; set; }
}
