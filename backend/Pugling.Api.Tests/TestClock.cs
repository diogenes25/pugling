namespace Pugling.Api.Tests;

/// <summary>
/// The test host's clock. By default <b>pass-through</b> (real system time) – only a test class that
/// checks a rule in the seconds range freezes it and advances it itself.
/// <para>
/// Why this exists at all: the quick-answer bonus is measured server-side from the gap to the last
/// answer and has an anti-farming <b>lower bound of one second</b>. A test that checks this lower bound
/// against the wall clock would have to get two HTTP requests through within one second; on a busy
/// runner that fails, and the failure looks like a points regression. With a frozen clock, the measured
/// time is an <i>input</i> of the test instead of a hope.
/// </para>
/// Deliberately pass-through by default: all other test classes share the same registration and must
/// not notice anything of this seam.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private readonly Lock _gate = new();
    private DateTimeOffset? _frozen;

    /// <summary>Freezes the clock at the current system time. After that only <see cref="Advance"/> moves it.</summary>
    public void FreezeNow()
    {
        lock (_gate) _frozen = System.GetUtcNow();
    }

    /// <summary>Advances the frozen clock – the jump <b>is</b> the response time measured by the server.</summary>
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
