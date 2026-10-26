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
    /// Checks a PIN against the stored value. Recognizes the hash format; if the stored value is
    /// not a hash (legacy plain text), it is compared directly – so accounts created before the switch stay usable.
    /// </summary>
    public static bool Verify(string pin, string stored)
    {
        // An empty stored PIN means "login disabled" - not "log in with an empty input". Without this line the
        // legacy plaintext branch below compared "" with "" and let exactly that through (B-28).
        if (stored.Length == 0) return false;
        var parts = stored.Split('.');
        if (parts.Length != 4 || parts[0] != Prefix)
            return stored == pin; // legacy plaintext (from before hashing was introduced)

        if (!int.TryParse(parts[1], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
