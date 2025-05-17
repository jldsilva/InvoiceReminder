using InvoiceReminder.Authentication.Extensions;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Authentication;

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

    [TestMethod]
    public void ToSHA256_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToSHA256();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expected256Hash);
    }

    [TestMethod]
    public void ToSHA512_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToSHA512();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expected512Hash);
    }

    [TestMethod]
    public void ToMD5_ValidInput_ShouldReturnExpectedHash()
    {
        // Arrange & Act
        var resultHash = _testString.ToMD5();

        // Assert
        resultHash.ShouldBeEquivalentTo(_expectedMD5Hash);
    }
}
