namespace PuglingTower.Core;

/// <summary>
/// Engine-free bookkeeping of a run's vocabulary tokens: which are still in the pool, lying on the field or carried,
/// and which are used up by a correct assignment. A wrongly assigned token goes back into the pool and drops again
/// later (B-822 E2). The client cannot know which token fits which tower - the server keeps that - so drops are random.
/// </summary>
public sealed class TokenPool(IEnumerable<string> tokenIds, Random? random = null)
{
    private readonly List<string> _pool = [.. tokenIds];
    private readonly HashSet<string> _out = [];
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>Takes a random token out of the pool for a drop, or <c>null</c> when every token is out or used.</summary>
    public string? Draw()
    {
        if (_pool.Count == 0) return null;
        var i = _random.Next(_pool.Count);
        var id = _pool[i];
        _pool.RemoveAt(i);
        _out.Add(id);
        return id;
    }

    /// <summary>Puts a token back (it expired on the field, or was assigned wrongly).</summary>
    public void Return(string id)
    {
        if (_out.Remove(id)) _pool.Add(id);
    }

    /// <summary>Marks a token as used up by a correct assignment; it never drops again.</summary>
    public void Consume(string id) => _out.Remove(id);
}
