using InvoiceReminder.Authentication.Extensions;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Authentication;

[TestClass]
public sealed class StringHashExtensionTests
{
    private readonly string _testString;
    private readonly string _expected256Hash;
    private readonly string _expected512Hash;
    private readonly string _expectedMD5Hash;

    public StringHashExtensionTests()
    {
        _testString = "TestString";
        _expectedMD5Hash = "5B56F40F8828701F97FA4511DDCD25FB";
        _expected256Hash = "6DD79F2770A0BB38073B814A5FF000647B37BE5ABBDE71EC9176C6CE0CB32A27";
        _expected512Hash = "69DFD91314578F7F329939A7EA6BE4497E6FE3909B9C8F308FE711D29D4340D90D77B7FDF359B7D0DBEED940665274F7CA514CD067895FDF59DE0CF142B62336";
    }

    #region ToSHA256 Tests

    [TestMethod]
    public void ToSHA256_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToSHA256();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expected256Hash);
    }

    #endregion

    #region ToSHA512 Tests

    [TestMethod]
    public void ToSHA512_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToSHA512();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expected512Hash);
    }

    #endregion

    #region ToMD5 Tests

    [TestMethod]
    public void ToMD5_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToMD5();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expectedMD5Hash);
    }

    #endregion

    #region HashPassword Tests

    [TestMethod]
    public void HashPassword_ShouldReturnNonEmptyHashAndSalt()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var (hash, salt) = password.HashPassword();

        // Assert
        hash.ShouldSatisfyAllConditions(() =>
        {
            hash.ShouldNotBeNullOrEmpty();
            _ = hash.ShouldBeOfType<string>();
        });

        salt.ShouldSatisfyAllConditions(() =>
        {
            salt.ShouldNotBeNullOrEmpty();
            _ = salt.ShouldBeOfType<string>();
        });
    }

    [TestMethod]
    public void HashPassword_ShouldReturnValidBase64EncodedValues()
    {
        // Arrange
        var testCases = new[] { "Password1!", "P@ssw0rd", "複雑なパスワード" };

        foreach (var password in testCases)
        {
            // Act
            var (hash, salt) = password.HashPassword();

            // Assert
            Should.NotThrow(() =>
            {
                _ = Convert.FromBase64String(hash);
                _ = Convert.FromBase64String(salt);
            });
        }
    }

    [TestMethod]
    public void HashPassword_SamePasswordMultipleTimes_ShouldProduceDifferentHashesDueToRandomSalt()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashes = new List<string>();
        var salts = new List<string>();

        // Act
        for (var i = 0; i < 5; i++)
        {
            var (hash, salt) = password.HashPassword();
            hashes.Add(hash);
            salts.Add(salt);
        }

        // Assert
        hashes.Distinct().Count().ShouldBe(5);
        salts.Distinct().Count().ShouldBe(5);
    }

    [TestMethod]
    public void HashPassword_DifferentPasswords_ShouldProduceDifferentHashes()
    {
        // Arrange
        var passwords = new[] { "Password1!", "Password2@", "Password3#" };

        // Act
        var results = passwords.Select(p => p.HashPassword()).ToList();

        // Assert
        var hashes = results.Select(r => r.Hash).Distinct().Count();
        hashes.ShouldBe(results.Count);
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("P@ssw0rd!#$%&*()[]{}")]
    [DataRow("複雑なパスワード")]
    [DataRow("x")]
    public void HashPassword_WithValidInputs_ShouldProduceValidHash(string password)
    {
        // Act
        var (hash, salt) = password.HashPassword();

        // Assert
        hash.ShouldNotBeNullOrEmpty();
        salt.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r")]
    [DataRow("   \t\n\r   ")]
    public void HashPassword_WithNullOrEmptyOrWhitespaceInput_ShouldThrowArgumentException(string password)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => password.HashPassword());

        exception.ParamName.ShouldBe("inputString");
    }

    #endregion

    #region VerifyPassword Tests

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var (hash, salt) = password.HashPassword();

        // Act
        var result = password.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBeTrue();
    }

    [TestMethod]
    public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456@";
        var (hash, salt) = password.HashPassword();

        // Act
        var result = wrongPassword.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("MySecurePassword123!", "MySecurePassword123!", true)]
    [DataRow("MySecurePassword123!", "WrongPassword456@", false)]
    [DataRow("MySecurePassword123!", "mysecurepassword123!", false)]
    [DataRow("P@ssw0rd!#$%&*()[]{}", "P@ssw0rd!#$%&*()[]{}", true)]
    [DataRow("Pässwörd123!", "Pässwörd123!", true)]
    [DataRow("abc123", "ABC123", false)]
    public void VerifyPassword_VariousScenarios_ShouldReturnExpectedResult(string originalPassword, string verifyPassword, bool expectedResult)
    {
        // Arrange
        var (hash, salt) = originalPassword.HashPassword();

        // Act
        var result = verifyPassword.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [TestMethod]
    public void VerifyPassword_ModifiedHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var (hash, salt) = password.HashPassword();

        // Modify hash by replacing a character in the middle with a different valid Base64 character
        var modifiedHash = hash[..^2] + "AB";

        // Act
        var result = password.VerifyPassword(modifiedHash, salt);

        // Assert
        result.ShouldBeFalse();
    }

    [TestMethod]
    public void VerifyPassword_ModifiedSaltWithValidBase64_ShouldReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var (hash, salt) = password.HashPassword();

        // Modify salt by replacing the last character with another valid Base64 character
        var modifiedSalt = salt[..^2] + "AB";

        // Act
        var result = password.VerifyPassword(hash, modifiedSalt);

        // Assert
        result.ShouldBeFalse();
    }

    [TestMethod]
    public void VerifyPassword_ModifiedSaltWithInvalidBase64_ShouldThrowFormatException()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var (hash, salt) = password.HashPassword();

        // Modify salt with an invalid Base64 character
        var invalidSalt = salt[..^1] + "!";

        // Act & Assert
        _ = Should.Throw<FormatException>(() => password.VerifyPassword(hash, invalidSalt));
    }

    [TestMethod]
    public void VerifyPassword_CorrectPasswordMultipleTimes_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var (hash, salt) = password.HashPassword();

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            var result = password.VerifyPassword(hash, salt);
            result.ShouldBeTrue();
        }
    }

    [TestMethod]
    public void VerifyPassword_IncorrectPasswordMultipleTimes_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456@";
        var (hash, salt) = password.HashPassword();

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            var result = wrongPassword.VerifyPassword(hash, salt);
            result.ShouldBeFalse();
        }
    }

    [TestMethod]
    public void VerifyPassword_WhitespaceAffectingPassword_ShouldReturnFalse()
    {
        // Arrange
        var testCases = new Dictionary<string, string>
        {
            { "My Password 123!", "MyPassword123!" },  // Missing spaces
            { "MyPassword123!", "My Password 123!" },  // Added spaces
            { "MyPassword123", "MyPassword123 " }      // Trailing space
        };

        foreach (var kvp in testCases)
        {
            var originalPassword = kvp.Key;
            var modifiedPassword = kvp.Value;
            var (hash, salt) = originalPassword.HashPassword();

            // Act
            var result = modifiedPassword.VerifyPassword(hash, salt);

            // Assert
            result.ShouldBeFalse($"Password '{modifiedPassword}' should not match hash of '{originalPassword}'");
        }
    }

    [TestMethod]
    public void VerifyPassword_LongPassword_ShouldVerifyCorrectly()
    {
        // Arrange
        var password = new string('x', 1000);
        var (hash, salt) = password.HashPassword();

        // Act
        var result = password.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBeTrue();
    }

    [TestMethod]
    public void VerifyPassword_SingleCharacterPassword_ShouldVerifyCorrectly()
    {
        // Arrange
        var password = "a";
        var (hash, salt) = password.HashPassword();

        // Act
        var result = password.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBeTrue();
    }

    [TestMethod]
    public void VerifyPassword_SpecialCharactersPassword_ShouldVerifyCorrectly()
    {
        // Arrange
        var password = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        var (hash, salt) = password.HashPassword();

        // Act
        var result = password.VerifyPassword(hash, salt);

        // Assert
        result.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r")]
    [DataRow("   \t\n\r   ")]
    public void VerifyPassword_WithNullOrEmptyOrWhitespaceInput_ShouldThrowArgumentException(string password)
    {
        // Arrange
        var validPassword = "ValidPassword123!";
        var (hash, salt) = validPassword.HashPassword();

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => password.VerifyPassword(hash, salt));

        exception.ParamName.ShouldBe("inputString");
    }

    #endregion
}
