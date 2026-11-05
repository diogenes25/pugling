using System.Security.Cryptography;

namespace Pugling.Api.Auth;

/// <summary>
/// Hashes login PINs (PBKDF2/SHA-256 with a random salt) instead of storing them as plain text. Format:
/// <c>pbkdf2.{iterations}.{saltB64}.{hashB64}</c>. <see cref="Verify"/> additionally accepts legacy plain text
/// (for databases created before the switch), so nobody gets locked out; new values via
/// <see cref="Hash"/> are always salted and hashed.
/// </summary>
public static class PinHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Prefix = "pbkdf2";

    /// <summary>
    /// The shortest PIN that may be stored (B-28). The login name is a small, dense id, so the PIN is the only real
    /// barrier; four matches the seeded family and the four dots of the child's PIN pad.
    /// </summary>
    public const int MinLength = 4;

    /// <summary>The validation message for a PIN shorter than <see cref="MinLength"/>.</summary>
    public const string TooShortMessage = "The PIN must have at least 4 characters; an empty PIN disables the login.";

    /// <summary>
    /// Whether a PIN may be stored: either empty (documented as "login disabled") or at least
    /// <see cref="MinLength"/> characters.
    /// </summary>
    public static bool IsAcceptable(string pin) => pin.Length == 0 || pin.Length >= MinLength;

    /// <summary>Produces the salted PBKDF2 hash of a PIN in the documented string format.</summary>
    public static string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return string.Join('.', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    /// <summary>
    /// A fixed salt for the decoy derivation in <see cref="Verify"/>. Not secret and never compared against anything:
    /// it only exists so that the decoy costs exactly what a real check costs.
    /// </summary>
    private static readonly byte[] DecoySalt = new byte[SaltSize];

    /// <summary>
    /// Checks a PIN against the stored value. Recognizes the hash format; if the stored value is
    /// not a hash (legacy plain text), it is compared directly – so accounts created before the switch stay usable.
    /// <para>
    /// <paramref name="stored"/> may be <c>null</c> (unknown id, no account). Every path that does not reach a real
    /// hash still runs one PBKDF2 derivation, so the response time does not tell "unknown or disabled" apart from
    /// "wrong PIN" (B-282, point 4) – callers therefore verify <b>before</b> they branch on a missing row.
    /// </para>
    /// </summary>
    public static bool Verify(string pin, string? stored)
    {
        var parts = stored?.Split('.');
        if (stored is null || stored.Length == 0 || parts!.Length != 4 || parts[0] != Prefix)
        {
            Rfc2898DeriveBytes.Pbkdf2(pin, DecoySalt, Iterations, HashAlgorithmName.SHA256, KeySize);
            // An empty stored PIN means "login disabled" - not "log in with an empty input" (B-28). Legacy plaintext
            // from before hashing was introduced is still compared directly.
            return stored is { Length: > 0 } && stored == pin;
        }

        if (!int.TryParse(parts[1], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
