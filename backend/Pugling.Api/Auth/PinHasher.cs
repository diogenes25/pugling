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
