using System.Security.Cryptography;
using System.Text;

namespace Chatter.Helpers;

public static class PasswordHasher
{
    // Parameters for PBKDF2 - recommended to keep iterations high (increase over time)
    private const int SaltSize = 16; // 128-bit
    private const int HashSize = 32; // 256-bit
    private const int Iterations = 200_000; // work factor; increase as hardware improves
    private const string AlgorithmId = "pbkdf2_sha256";

    /// <summary>
    /// Hash a password using PBKDF2 (HMAC-SHA256) with a random salt.
    /// Returned format: {algorithm}${iterations}${saltBase64}${hashBase64}
    /// </summary>
    public static string HashPassword(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var saltB64 = Convert.ToBase64String(salt);
        var hashB64 = Convert.ToBase64String(hash);

        return string.Join("$", AlgorithmId, Iterations.ToString(), saltB64, hashB64);
    }

    /// <summary>
    /// Verify a password against a stored hash produced by HashPassword.
    /// Uses constant-time comparison to avoid timing attacks.
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || password == null) return false;

        var parts = hashedPassword.Split('$');
        if (parts.Length != 4) return false;

        var algo = parts[0];
        if (!string.Equals(algo, AlgorithmId, StringComparison.OrdinalIgnoreCase)) return false;

        if (!int.TryParse(parts[1], out var iterations)) return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    /// <summary>
    /// Indicates whether the stored hash uses weaker parameters and should be upgraded.
    /// </summary>
    public static bool NeedsUpgrade(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return true;
        var parts = hashedPassword.Split('$');
        if (parts.Length != 4) return true;
        if (!int.TryParse(parts[1], out var iterations)) return true;
        return iterations < Iterations;
    }
}
