using System.Security.Cryptography;

namespace Pugling.Api.Auth;

/// <summary>
/// Hasht Login-PINs (PBKDF2/SHA-256 mit zufälligem Salt) statt sie im Klartext zu speichern. Format:
/// <c>pbkdf2.{iterations}.{saltB64}.{hashB64}</c>. <see cref="Verify"/> akzeptiert zusätzlich Alt-Klartext
/// (für vor der Umstellung angelegte Datenbanken), damit niemand ausgesperrt wird; neue Werte über
/// <see cref="Hash"/> sind immer gesalzen und gehasht.
/// </summary>
public static class PinHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Prefix = "pbkdf2";

    /// <summary>Erzeugt den gesalzenen PBKDF2-Hash einer PIN im dokumentierten String-Format.</summary>
    public static string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return string.Join('.', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    /// <summary>
    /// Prüft eine PIN gegen den gespeicherten Wert. Erkennt das Hash-Format; ist der gespeicherte Wert
    /// kein Hash (Alt-Klartext), wird direkt verglichen – so bleiben vor der Umstellung angelegte Konten nutzbar.
    /// </summary>
    public static bool Verify(string pin, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 4 || parts[0] != Prefix)
            return stored == pin; // Alt-Klartext (vor Einführung des Hashings)

        if (!int.TryParse(parts[1], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
