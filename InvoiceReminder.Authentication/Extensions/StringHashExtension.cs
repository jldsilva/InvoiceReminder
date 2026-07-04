using Konscious.Security.Cryptography;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace InvoiceReminder.Authentication.Extensions;

public static class StringHashExtension
{
    const string CERT_NOT_FOUND = "Certificado de segurança não encontrado no servidor.";
    const string NO_RSA_KEY = "O certificado não possui uma chave RSA válida.";

    public static (string Hash, string Salt) HashPassword(this string inputString, int parallelismFactor = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputString);

        var salt = RandomNumberGenerator.GetBytes(16);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(inputString))
        {
            Salt = salt,
            DegreeOfParallelism = GetMaxDegreeOfParallelism(parallelismFactor),
            Iterations = 4,
            MemorySize = 1024 * 64
        };

        var hashBytes = argon2.GetBytes(32);
        var hash = Convert.ToBase64String(hashBytes);
        var saltBase64 = Convert.ToBase64String(salt);

        return (hash, saltBase64);
    }

    public static bool VerifyPassword(this string inputString, string storedHash, string storedSalt, int parallelismFactor = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputString);

        var salt = Convert.FromBase64String(storedSalt);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(inputString))
        {
            Salt = salt,
            DegreeOfParallelism = GetMaxDegreeOfParallelism(parallelismFactor),
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

    public static string X509_Encrypt(this string inputString, string certFilePath, string password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputString);
        ArgumentException.ThrowIfNullOrWhiteSpace(certFilePath);

        if (!File.Exists(certFilePath))
        {
            throw new FileNotFoundException(CERT_NOT_FOUND, certFilePath);
        }

        using var cert = X509CertificateLoader.LoadPkcs12FromFile(certFilePath, password);
        using var rsa = cert.GetRSAPublicKey() ?? throw new InvalidDataException(NO_RSA_KEY);
        var encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(inputString), RSAEncryptionPadding.OaepSHA256);

        return Convert.ToBase64String(encryptedData);
    }

    public static string X509_Decrypt(this string encryptedString, string certFilePath, string password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedString);
        ArgumentException.ThrowIfNullOrWhiteSpace(certFilePath);

        if (!File.Exists(certFilePath))
        {
            throw new FileNotFoundException(CERT_NOT_FOUND, certFilePath);
        }

        using var cert = X509CertificateLoader.LoadPkcs12FromFile(certFilePath, password);
        using var rsa = cert.GetRSAPrivateKey() ?? throw new InvalidDataException(NO_RSA_KEY);

        try
        {
            var decryptedData = rsa.Decrypt(Convert.FromBase64String(encryptedString), RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Falha ao descriptografar: chave incorreta ou dados corrompidos.", ex);
        }
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

    private static string GetStringFromHash(byte[] hash)
    {
        var result = new StringBuilder();

        for (var i = 0; i < hash.Length; i++)
        {
            _ = result.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }

    private static int GetMaxDegreeOfParallelism(int parallelismFactor)
    {
        return Math.Max(1, Environment.ProcessorCount / parallelismFactor);
    }
}
