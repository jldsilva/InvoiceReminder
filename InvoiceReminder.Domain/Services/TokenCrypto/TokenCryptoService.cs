using System.Security.Cryptography;
using System.Text;

namespace InvoiceReminder.Domain.Services.TokenCrypto;

public static class TokenCryptoService
{
    private const int KeySize = 32; // AES-256
    private const int NonceSize = 12; // GCM padrão
    private const int TagSize = 16;

    public static (string EncryptedToken, string NonceBase64) Encrypt(string plainText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == KeySize)
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherText = new byte[plainTextBytes.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plainTextBytes, cipherText, tag);

            var combined = new byte[cipherText.Length + tag.Length];
            cipherText.CopyTo(combined, 0);
            tag.CopyTo(combined, cipherText.Length);

            return (Convert.ToBase64String(combined), Convert.ToBase64String(nonce));
        }

        throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));
    }

    public static string Decrypt(string encryptedBase64, string nonceBase64, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == KeySize)
        {
            var nonce = Convert.FromBase64String(nonceBase64);
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);

            if (nonce.Length != NonceSize || encryptedBytes.Length < TagSize)
            {
                throw new CryptographicException("Invalid nonce or encrypted data.");
            }

            var cipherTextLength = encryptedBytes.Length - TagSize;
            var cipherText = encryptedBytes[..cipherTextLength];
            var tag = encryptedBytes[cipherTextLength..];

            var plainTextBytes = new byte[cipherText.Length];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, cipherText, tag, plainTextBytes);

            return Encoding.UTF8.GetString(plainTextBytes);
        }

        throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));
    }
}
