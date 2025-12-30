using Bogus;
using InvoiceReminder.Domain.Services.TokenCrypto;
using Shouldly;
using System.Security.Cryptography;

namespace InvoiceReminder.UnitTests.Domain.Services.Configuration;

[TestClass]
public sealed class TokenCryptoServiceTests
{
    private readonly Faker _faker;
    private byte[] _validKey;

    public TestContext TestContext { get; set; }

    public TokenCryptoServiceTests()
    {
        _faker = new Faker();
    }

    [TestInitialize]
    public void Setup()
    {
        // Gera uma chave AES-256 válida (32 bytes)
        _validKey = RandomNumberGenerator.GetBytes(32);
    }

    #region Encrypt Tests

    [TestMethod]
    public void Encrypt_WithValidKeyAndPlainText_ReturnsEncryptedTokenAndNonce()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();

        // Act
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encryptedToken.ShouldNotBeNullOrEmpty();
        nonceBase64.ShouldNotBeNullOrEmpty();

        // Verifica se os valores retornados são válidos base64
        _ = Should.NotThrow(() => Convert.FromBase64String(encryptedToken));
        _ = Should.NotThrow(() => Convert.FromBase64String(nonceBase64));
    }

    [TestMethod]
    public void Encrypt_WithDifferentPlainTexts_ProducesDifferentResults()
    {
        // Arrange
        var plainText1 = _faker.Lorem.Sentence();
        var plainText2 = _faker.Lorem.Sentence();

        // Act
        var (encrypted1, nonce1) = TokenCryptoService.Encrypt(plainText1, _validKey);
        var (encrypted2, nonce2) = TokenCryptoService.Encrypt(plainText2, _validKey);

        // Assert
        encrypted1.ShouldNotBe(encrypted2);
        nonce1.ShouldNotBe(nonce2); // Nonce deve ser aleatório
    }

    [TestMethod]
    public void Encrypt_WithSamePlainTextDifferentNonce_ProducesDifferentResults()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();

        // Act
        var (encrypted1, nonce1) = TokenCryptoService.Encrypt(plainText, _validKey);
        var (encrypted2, nonce2) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encrypted1.ShouldNotBe(encrypted2); // Diferente nonce gera criptografia diferente
        nonce1.ShouldNotBe(nonce2);
    }

    [TestMethod]
    public void Encrypt_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();

        // Act && Assert
        _ = Should.Throw<ArgumentNullException>(() =>
            TokenCryptoService.Encrypt(plainText, null)
        );
    }

    [TestMethod]
    public void Encrypt_WithInvalidKeySize_ThrowsArgumentException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var invalidKey = RandomNumberGenerator.GetBytes(16); // AES-128 ao invés de AES-256

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() =>
            TokenCryptoService.Encrypt(plainText, invalidKey)
        );

        exception.Message.ShouldContain("Key must be 32 bytes");
        exception.ParamName.ShouldBe("key");
    }

    [TestMethod]
    public void Encrypt_WithEmptyPlainText_ReturnsEncryptedTokenAndNonce()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encryptedToken.ShouldNotBeNullOrEmpty();
        nonceBase64.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void Encrypt_WithLongPlainText_ReturnsEncryptedTokenAndNonce()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence(5000);

        // Act
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encryptedToken.ShouldNotBeNullOrEmpty();
        nonceBase64.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void Encrypt_WithSpecialCharacters_ReturnsEncryptedTokenAndNonce()
    {
        // Arrange
        var plainText = "!@#$%^&*()_+-={}[]|\\:;\"'<>?,./~`";

        // Act
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encryptedToken.ShouldNotBeNullOrEmpty();
        nonceBase64.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void Encrypt_WithUnicodeCharacters_ReturnsEncryptedTokenAndNonce()
    {
        // Arrange
        var plainText = "Olá, Mundo! 你好 🌍";

        // Act
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Assert
        encryptedToken.ShouldNotBeNullOrEmpty();
        nonceBase64.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Decrypt Tests

    [TestMethod]
    public void Decrypt_WithValidEncryptedDataAndKey_ReturnsOriginalPlainText()
    {
        // Arrange
        var originalPlainText = _faker.Lorem.Sentence();
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalPlainText, _validKey);

        // Act
        var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

        // Assert
        decryptedText.ShouldBe(originalPlainText);
    }

    [TestMethod]
    public void Decrypt_WithEmptyPlainText_ReturnsEmptyString()
    {
        // Arrange
        var originalPlainText = string.Empty;
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalPlainText, _validKey);

        // Act
        var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

        // Assert
        decryptedText.ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Decrypt_WithLongPlainText_ReturnsOriginalPlainText()
    {
        // Arrange
        var originalPlainText = _faker.Lorem.Sentence(5000);
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalPlainText, _validKey);

        // Act
        var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

        // Assert
        decryptedText.ShouldBe(originalPlainText);
    }

    [TestMethod]
    public void Decrypt_WithSpecialCharacters_ReturnsOriginalPlainText()
    {
        // Arrange
        var originalPlainText = "!@#$%^&*()_+-={}[]|\\:;\"'<>?,./~`";
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalPlainText, _validKey);

        // Act
        var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

        // Assert
        decryptedText.ShouldBe(originalPlainText);
    }

    [TestMethod]
    public void Decrypt_WithUnicodeCharacters_ReturnsOriginalPlainText()
    {
        // Arrange
        var originalPlainText = "Olá, Mundo! 你好 🌍";
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalPlainText, _validKey);

        // Act
        var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

        // Assert
        decryptedText.ShouldBe(originalPlainText);
    }

    [TestMethod]
    public void Decrypt_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Act && Assert
        _ = Should.Throw<ArgumentNullException>(() =>
            TokenCryptoService.Decrypt(encryptedToken, nonceBase64, null)
        );
    }

    [TestMethod]
    public void Decrypt_WithInvalidKeySize_ThrowsArgumentException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);
        var invalidKey = RandomNumberGenerator.GetBytes(16); // AES-128 ao invés de AES-256

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() =>
            TokenCryptoService.Decrypt(encryptedToken, nonceBase64, invalidKey)
        );

        exception.Message.ShouldContain("Key must be 32 bytes");
        exception.ParamName.ShouldBe("key");
    }

    [TestMethod]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);
        var wrongKey = RandomNumberGenerator.GetBytes(32);

        // Act && Assert
        _ = Should.Throw<CryptographicException>(() =>
            TokenCryptoService.Decrypt(encryptedToken, nonceBase64, wrongKey)
        );
    }

    [TestMethod]
    public void Decrypt_WithInvalidNonceSize_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (encryptedToken, _) = TokenCryptoService.Encrypt(plainText, _validKey);
        var invalidNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8)); // Nonce com tamanho inválido

        // Act && Assert
        _ = Should.Throw<CryptographicException>(() =>
            TokenCryptoService.Decrypt(encryptedToken, invalidNonce, _validKey)
        );
    }

    [TestMethod]
    public void Decrypt_WithInvalidEncryptedData_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (_, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);
        var invalidEncryptedData = Convert.ToBase64String(RandomNumberGenerator.GetBytes(10)); // Dados muito curtos

        // Act && Assert
        _ = Should.Throw<CryptographicException>(() =>
            TokenCryptoService.Decrypt(invalidEncryptedData, nonceBase64, _validKey)
        );
    }

    [TestMethod]
    public void Decrypt_WithTamperedEncryptedData_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);

        // Corrompe os dados criptografados
        var encryptedBytes = Convert.FromBase64String(encryptedToken);
        if (encryptedBytes.Length > 0)
        {
            encryptedBytes[0] ^= 0xFF; // Inverte o primeiro byte
        }
        var tamperedToken = Convert.ToBase64String(encryptedBytes);

        // Act && Assert
        _ = Should.Throw<CryptographicException>(() =>
            TokenCryptoService.Decrypt(tamperedToken, nonceBase64, _validKey)
        );
    }

    #endregion

    #region Round-Trip Tests

    [TestMethod]
    public void Encrypt_Decrypt_RoundTrip_WithRandomData_ReturnsOriginalText()
    {
        // Arrange
        var originalTexts = new[]
        {
            _faker.Lorem.Sentence(),
            _faker.Person.Email,
            _faker.Internet.Url(),
            "test-token-123",
            string.Empty
        };

        // Act && Assert
        foreach (var originalText in originalTexts)
        {
            var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(originalText, _validKey);
            var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

            decryptedText.ShouldBe(originalText);
        }
    }

    [TestMethod]
    public void Encrypt_Decrypt_MultipleRoundTrips_WithSameKey_SucceedsAllIterations()
    {
        // Arrange
        const int iterations = 10;
        var plainTexts = Enumerable.Range(0, iterations)
            .Select(_ => _faker.Lorem.Sentence())
            .ToList();

        // Act && Assert
        foreach (var plainText in plainTexts)
        {
            var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, _validKey);
            var decryptedText = TokenCryptoService.Decrypt(encryptedToken, nonceBase64, _validKey);

            decryptedText.ShouldBe(plainText);
        }
    }

    #endregion

    #region Key Handling Tests

    [TestMethod]
    public void Encrypt_WithMultipleDifferentKeys_ProducesDifferentResults()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var key1 = RandomNumberGenerator.GetBytes(32);
        var key2 = RandomNumberGenerator.GetBytes(32);

        // Act
        var (encrypted1, _) = TokenCryptoService.Encrypt(plainText, key1);
        var (encrypted2, _) = TokenCryptoService.Encrypt(plainText, key2);

        // Assert
        encrypted1.ShouldNotBe(encrypted2);
    }

    [TestMethod]
    public void Decrypt_WithDifferentKey_CannotDecryptDataEncryptedWithAnotherKey()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var key1 = RandomNumberGenerator.GetBytes(32);
        var key2 = RandomNumberGenerator.GetBytes(32);
        var (encryptedToken, nonceBase64) = TokenCryptoService.Encrypt(plainText, key1);

        // Act && Assert
        _ = Should.Throw<CryptographicException>(() =>
            TokenCryptoService.Decrypt(encryptedToken, nonceBase64, key2)
        );
    }

    [TestMethod]
    public void Encrypt_Decrypt_WithDifferentKeySizes_OnlyAccepts32ByteKeys()
    {
        // Arrange
        var plainText = _faker.Lorem.Sentence();
        var invalidKeySizes = new[] { 8, 16, 24, 40, 64 };

        // Act && Assert
        foreach (var keySize in invalidKeySizes)
        {
            var invalidKey = RandomNumberGenerator.GetBytes(keySize);

            _ = Should.Throw<ArgumentException>(() =>
                TokenCryptoService.Encrypt(plainText, invalidKey)
            );
        }
    }

    #endregion
}
