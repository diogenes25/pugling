namespace Pugling.Api.Tests;

/// <summary>
/// Die Uhr des Testhosts. Standardmäßig <b>durchreichend</b> (echte Systemzeit) – nur eine Testklasse, die
/// eine Regel im Sekunden-Bereich prüft, friert sie ein und rückt sie selbst vor.
/// <para>
/// Warum überhaupt: der Schnelle-Antwort-Bonus wird serverseitig aus dem Abstand zur letzten Antwort
/// gemessen und hat eine Anti-Farming-<b>Untergrenze von einer Sekunde</b>. Ein Test, der diese Untergrenze
/// mit der Wanduhr prüft, muss zwei HTTP-Requests binnen einer Sekunde durchbringen; auf einem
/// ausgelasteten Runner reißt das, und der Fehlschlag sieht aus wie ein Punkte-Regress. Mit eingefrorener
/// Uhr ist die gemessene Zeit eine <i>Eingabe</i> des Tests statt einer Hoffnung.
/// </para>
/// Bewusst pass-through als Vorgabe: alle übrigen Testklassen teilen sich dieselbe Registrierung und
/// dürfen von der Naht nichts merken.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private readonly Lock _gate = new();
    private DateTimeOffset? _frozen;

    /// <summary>Hält die Uhr auf der aktuellen Systemzeit an. Danach bewegt sie nur <see cref="Advance"/>.</summary>
    public void FreezeNow()
    {
        lock (_gate) _frozen = System.GetUtcNow();
    }

    /// <summary>Rückt die eingefrorene Uhr vor – der Sprung <b>ist</b> die vom Server gemessene Antwortzeit.</summary>
    public void Advance(TimeSpan by)
    {
        lock (_gate)
        {
            if (_frozen is null) throw new InvalidOperationException($"Erst {nameof(FreezeNow)} aufrufen.");
            _frozen = _frozen.Value + by;
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) return _frozen ?? System.GetUtcNow();
    }
}
