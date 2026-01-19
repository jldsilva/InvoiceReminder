using Konscious.Security.Cryptography;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InvoiceReminder.Authentication.Extensions;

public static class StringHashExtension
{
    public static (string Hash, string Salt) HashPassword(this string inputString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputString);

        var salt = RandomNumberGenerator.GetBytes(16);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(inputString))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 1024 * 64
        };

        var hashBytes = argon2.GetBytes(32);
        var hash = Convert.ToBase64String(hashBytes);
        var saltBase64 = Convert.ToBase64String(salt);

        return (hash, saltBase64);
    }

    public static bool VerifyPassword(this string inputString, string storedHash, string storedSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputString);

        var salt = Convert.FromBase64String(storedSalt);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(inputString))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 1024 * 64
        };

        var hashBytes = argon2.GetBytes(32);
        var hash = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(storedHash),
            Convert.FromBase64String(hash)
        );
    }

    public static string ToSHA256(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = SHA256.HashData(bytes);

        return GetStringFromHash(hash);
    }

    public static string ToSHA512(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = SHA512.HashData(bytes);

        return GetStringFromHash(hash);
    }

    public static string ToMD5(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = MD5.HashData(bytes);

        return GetStringFromHash(hash);
    }

    private static string GetStringFromHash(byte[] hash)
    {
        var result = new StringBuilder();

        for (var i = 0; i < hash.Length; i++)
        {
            _ = result.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }
}
